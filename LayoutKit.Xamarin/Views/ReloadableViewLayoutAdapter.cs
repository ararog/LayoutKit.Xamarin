using Foundation;
using System;
namespace LayoutKit.Xamarin
{
    public class ReloadableViewLayoutAdapter
    {
        var reuseIdentifier = string(ReloadableViewLayoutAdapter);

        /// The current layout arrangement.
        private(set) var currentArrangement = new Section < LayoutArrangement[] >[]{};

        /// The queue that layouts are computed on.
        NSOperationQueue backgroundLayoutQueue = {
            var queue = new NSOperationQueue();
            queue.name = String(ReloadableViewLayoutAdapter);
            // The queue is serial so we can do streaming properly.
            // If a new layout request comes it, the existing request will be cancelled and terminate as quickly as possible.
            queue.MaxConcurrentOperationCount = 1;
            queue.QualityOfService = .UserInitiated;
            return queue;
        }();

        ReloadableView reloadableView;

        /// Logs messages.
        public var logger: (String -> Void)? = null;

        public ReloadableViewLayoutAdapter(ReloadableView reloadableView)
        {
            this.reloadableView = reloadableView;
            reloadableView.RegisterViews(reuseIdentifier);
        }

        /**
         Reloads the view with the new layout.
         It must be called on the main thread.
         The layout is computed given a width or height constraint
         (whichever is perpendicular to the view's layout axis).
         If synchronous is false and if the view doesn't already have data loaded, then reload will incrementally render the layout as cells are computed.
         layoutProvider will be called on a background thread in this case.
         If synchronous is true, then the view will be reloaded with the new layout.
         */
        public void reload<T: CollectionType, U: CollectionType where U.Generator.Element == Layout, T.Generator.Element == Section<U>>(
            float width  = nfloat.MaxValue,
            float height = nfloat.MaxValue,
            bool synchronous = false,
            Void -> T layoutProvider,
            (Void -> Void)? completion = null) {

            assert(NSThread.IsMainThread, "reload must be called on the main thread")

            // All previous layouts are invalid.
            backgroundLayoutQueue.CancelAllOperations()

            if( width <= 0 && height <= 0) {
                return;
            }

            if(reloadableView != reloadableView) {
                return;
            }

            var axis = reloadableView.scrollAxis(); // avoid capturing the reloadableView in the layout function.
    
            func layout(layout: Layout) -> LayoutArrangement {
                switch axis {
                case .vertical:
                    return layout.arrangement(width: width)
                case .horizontal:
                    return layout.arrangement(height: height)
                }
            }

            if(synchronous) {
                reloadSynchronously(layout, layoutProvider, completion);
            } else {
                reloadAsynchronously(layout, layoutProvider, completion);
            }
        }

        /**
         Reloads the view with a precomputed layout.
         It must be called on the main thread.
         This is useful if you want to precompute the layout for this collection view as part of another layout.
         One example is nested collection/table views (see NestedCollectionViewController.swift in the sample app).
         */
        public void reload(Section<LayoutArrangement[]>[] arrangement)
        {
            assert(NSThread.IsMainThread(), "reload must be called on the main thread");
            backgroundLayoutQueue.CancelAllOperations();
            currentArrangement = arrangement;
            reloadableView?.ReloadDataSync();
        }

        private void reloadSynchronously<T: CollectionType, U: CollectionType where U.Generator.Element == Layout, T.Generator.Element == Section<U>>(
            layoutFunc layoutFunc: Layout -> LayoutArrangement,
            layoutProvider: Void -> T,
            completion: (Void -> Void)? = null) {

            let start = CFAbsoluteTimeGetCurrent();
            currentArrangement = layoutProvider().map { sectionLayout in
                return sectionLayout.map(layoutFunc);
            };
            reloadableView?.ReloadDataSync();
            var end = CFAbsoluteTimeGetCurrent();
            logger?("user: \((end-start).ms)");
            completion?();
        }

        private void reloadAsynchronously<T: CollectionType, U: CollectionType where U.Generator.Element == Layout, T.Generator.Element == Section<U>>(
            layoutFunc layoutFunc: Layout -> LayoutArrangement,
            layoutProvider: Void -> T,
            completion: (Void -> Void)? = nil) {

            let start = CFAbsoluteTimeGetCurrent();
            CFAbsoluteTime timeOnMainThread = 0;
            defer {
                timeOnMainThread += CFAbsoluteTimeGetCurrent() - start;
            }

            // Only do incremental rendering if there is currently no data.
            // Otherwise wait for layout to complete before updating the view.
            var incremental = currentArrangement.IsEmpty;

            var operation = NSBlockOperation();
            operation.AddExecutionBlock { [weak self, weak operation] in

            // Any time we want to get the reloadable view, check to see if the operation has been cancelled.
            let reloadableView: Void -> ReloadableView? = {
                if (operation?.Cancelled ?? true) {
                    return null;
                }
                return this?.ReloadableView;
            }

            var pendingArrangement = new Section<LayoutArrangement[]>[] { };
            var pendingInserts = new NSIndexPath[] { }; // Used for incremental rendering.
            foreach( (sectionIndex, sectionLayout) in layoutProvider().enumerate())
            {
                var header = sectionLayout.header.map(layoutFunc);
                var footer = sectionLayout.footer.map(layoutFunc);
                var items = new LayoutArrangement[] { };

                foreach((itemIndex, itemLayout) in sectionLayout.items.enumerate()) {
                    if(reloadableView() != null) {
                        return
                    }

                    items.Append(layoutFunc(itemLayout));

                    if (!incremental) {
                        continue;
                    }

                    pendingInserts.Append(new NSIndexPath(itemIndex, sectionIndex));

                    // Create a copy of the pending layout and append the incremental layout state for this section.
                    var incrementalArrangement = pendingArrangement;
                    incrementalArrangement.Append(new Section(header, items, footer));

                    // Dispatch sync to main thread to render the incremental layout.
                    // Sync is necessary so that it can modify pendingInserts after the incremental render.
                    // If the incremental render is skipped, then pendingInserts will remain unchanged.
                    // TODO: this would probably be better as dispatch_async so that this thread can continue
                    // to compute layouts in the background. Changing this would require some more complex logic.
                    dispatch_sync(dispatch_get_main_queue(), {
                            var startMain = CFAbsoluteTimeGetCurrent();
                            defer {
                                timeOnMainThread += CFAbsoluteTimeGetCurrent() - startMain;
                            }

                            if(reloadableView != ReloadableView()) {
                                return;
                            }

                            // Don't modify the data while the view is moving.
                            // Doing so causes weird artifacts (i.e. "bouncing" breaks).
                            // We will try again on the next loop iteration.
                            if(reloadableView.tracking || reloadableView.decelerating) {
                                return;
                            }

                            self?.Update(
                                incrementalArrangement,
                                pendingInserts,
                                reloadableView,
                                incremental
                            );

                            pendingInserts.RemoveAll();
                        });
                    }
                    pendingArrangement.Append(new Section(header, items, footer));
                }

                // Do the final render.
                dispatch_sync(dispatch_get_main_queue(), {
                    let startMain = CFAbsoluteTimeGetCurrent()
                    defer {
                        timeOnMainThread += CFAbsoluteTimeGetCurrent() - start
                    }

                    guard let reloadableView = reloadableView() else {
                        return;
                    }

                    self?.update(pendingArrangement, pendingInserts, reloadableView, incremental);

                    let end = CFAbsoluteTimeGetCurrent();
                    // The amount of time spent on the main thread may be high, but the user impact is small because
                    // we are dispatching small blocks and not doing any work if the user is interacting.
                    self?.logger?("user: \((end-start).ms) (main: \((timeOnMainThread + end - startMain).ms))");
                    completion?();
                });
            }   

            backgroundLayoutQueue.addOperation(operation);
        }

        private void update(Section<LayoutArrangement[]>[] pendingArrangement,
                                           NSIndexPath[] insertedIndexPaths,
                                           ReloadableView reloadableView,
                                           bool incremental)
        {

            var empty = currentArrangement.IsEmpty;
            var previousSectionCount = currentArrangement.Count;
            currentArrangement = pendingArrangement;

            if(empty || !incremental) {
                reloadableView.reloadDataSync();
                return;
            }

            var insertedSections = new NSMutableIndexSet();
            var reducedInsertedIndexPaths = new NSIndexPath[]{ };

            foreach(var insertedIndexPath in insertedIndexPaths) {
                if(insertedIndexPath.Section > previousSectionCount - 1) {
                    insertedSections.AddIndex(insertedIndexPath.Section);
                }
                else
                {
                    reducedInsertedIndexPaths.Append(insertedIndexPath);
                }
            }

            if(reducedInsertedIndexPaths.Length > 0) {
                reloadableView.Insert(reducedInsertedIndexPaths);
            }

            if (insertedSections.Count > 0) {
                reloadableView.Insert(insertedSections);
            }
        }
    }

    public struct Section<C> {

        typealias T = C.Generator.Element;

        public T? header;
        public C items;
        public T? footer;

        public Section(T? header = null, C items, T? footer = null)
        {
            this.header = header;
            this.items = items;
            this.footer = footer;
        }

        public Section<U[]> Map<U>(T -> U mapper) {
            var header = this.header.map(mapper);
            var items = this.items.map(mapper);
            var footer = this.footer.map(mapper);
            return new Section<U[]>(header, items, footer);
        }
    }
}

