using Foundation;
using System;
using System.Diagnostics;
using CoreFoundation;
using UIKit;
using CoreGraphics;

namespace LayoutKit.Xamarin
{
    public class ReloadableViewLayoutAdapter : IUITableViewDataSource, IUITableViewDelegate, IUICollectionViewDataSource, IUICollectionViewDelegateFlowLayout
    {
        string reuseIdentifier = typeof(ReloadableViewLayoutAdapter).Name;

        /// The current layout arrangement.
        private Section<LayoutArrangement[], LayoutArrangement>[] currentArrangement = new Section <LayoutArrangement[], LayoutArrangement>[]{};

        public delegate void LoggerDelegate(string);

        public delegate void CompletionDelegate();

        /// Logs messages.
        public LoggerDelegate logger = null;

        /// The queue that layouts are computed on.
        NSOperationQueue backgroundLayoutQueue;

        ReloadableView reloadableView;

        public IntPtr Handle
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ReloadableViewLayoutAdapter(ReloadableView reloadableView)
        {
            var queue = new NSOperationQueue();
            queue.Name = typeof(ReloadableViewLayoutAdapter).Name;
            // The queue is serial so we can do streaming properly.
            // If a new layout request comes it, the existing request will be cancelled and terminate as quickly as possible.
            queue.MaxConcurrentOperationCount = 1;
            queue.QualityOfService = NSQualityOfService.UserInitiated;
            this.backgroundLayoutQueue = queue;
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
        public void Reload<T, U, Z>
            (
            Func<T> layoutProvider,
            float width  = 0,
            float height = 0,
            bool synchronous = false,
            CompletionDelegate completion = null) 
            where U : Layout
            where Z : Section<U[], U>
        {
            //Assert(NSThread.IsMain, "reload must be called on the main thread");

            // All previous layouts are invalid.
            backgroundLayoutQueue.CancelAllOperations();

            if( width <= 0 && height <= 0) {
                return;
            }

            if(reloadableView == null) {
                return;
            }

            var axis = reloadableView.ScrollAxis(); // avoid capturing the reloadableView in the layout function.

            Func<Layout, LayoutArrangement > layout = (Layout l) =>
            {
                switch (axis) {
                case Axis.Vertical:
                    return l.Arrangement(null, width, null);
                default:
                    return l.Arrangement(null, null, height);
                }
            };

            if(synchronous) {
                ReloadSynchronously<T, U, Z>(layout, layoutProvider, completion);
            } else {
                ReloadAsynchronously<T, U, Z>(layout, layoutProvider, completion);
            }
        }

        /**
         Reloads the view with a precomputed layout.
         It must be called on the main thread.
         This is useful if you want to precompute the layout for this collection view as part of another layout.
         One example is nested collection/table views (see NestedCollectionViewController.swift in the sample app).
         */
        public void Reload(Section<LayoutArrangement[], LayoutArrangement>[] arrangement)
        {
            Debug.Assert(NSThread.IsMain, "reload must be called on the main thread");
            backgroundLayoutQueue.CancelAllOperations();
            currentArrangement = arrangement;
            reloadableView.ReloadDataSync();
        }

        private void ReloadSynchronously<T, U, Z> 
             (
            Func<ILayout, LayoutArrangement> layoutFunc,
            Func<T> layoutProvider,
            CompletionDelegate completion = null) 
            where U : ILayout
            where Z : Section<U[], U>
        {
            var start = CFAbsoluteTimeGetCurrent();
            currentArrangement = layoutProvider().Select((sectionLayout) =>
            {
                return sectionLayout.Select(layoutFunc);
            });
            reloadableView.ReloadDataSync();
            var end = CFAbsoluteTimeGetCurrent();
            logger("user: ((end-start).ms)");
            completion();
        }

        private void ReloadAsynchronously<T, U, Z>(
            Func<ILayout, LayoutArrangement> layoutFunc,
            Func<T> layoutProvider,
            CompletionDelegate completion = null)
            where U : ILayout
            where Z : Section<U[], U>
        {
            /*
            var start = CFAbsoluteTimeGetCurrent();
            CFAbsoluteTime timeOnMainThread = 0;
            defer {
                timeOnMainThread += CFAbsoluteTimeGetCurrent() - start;
            }

            // Only do incremental rendering if there is currently no data.
            // Otherwise wait for layout to complete before updating the view.
            var incremental = currentArrangement.IsEmpty;

            var operation = new NSBlockOperation();
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
                var header = sectionLayout.Header.Select(layoutFunc);
                var footer = sectionLayout.Footer.Select(layoutFunc);
                var items = new LayoutArrangement[] { };

                foreach((itemIndex, itemLayout) in sectionLayout.Items.enumerate()) {
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
                    self?.logger?("user: ((end-start).ms) (main: ((timeOnMainThread + end - startMain).ms))");
                    completion?();
                });
            }   

            backgroundLayoutQueue.addOperation(operation);
            */
        }

        private void Update(Section<LayoutArrangement[], LayoutArrangement>[] pendingArrangement,
            NSIndexPath[] insertedIndexPaths, ReloadableView reloadableView, bool incremental)
        {

            var empty = currentArrangement.Length == 0;
            var previousSectionCount = currentArrangement.Length;
            currentArrangement = pendingArrangement;

            if(empty || !incremental) {
                reloadableView.ReloadDataSync();
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

        #region UITableViewDelegate

        public nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath) {
            return currentArrangement[indexPath.Section].Items[indexPath.Item].Frame.Height;
        }

        public nfloat GetHeightForHeader(UITableView tableView, nint section) {
            return currentArrangement[section].Header.Frame.Height ?? 0;
        }

        public nfloat GetHeightForFooter(UITableView tableView, nint section) {
            return currentArrangement[section].Footer.Frame.Height ?? 0;
        }

        public UIView GetViewForHeader(UITableView tableView, nint section) {
            return RenderLayout(currentArrangement[section].Header, tableView);
        }

        public UIView GetViewForFooter(UITableView tableView, nint section) {
            return RenderLayout(currentArrangement[section].Footer, tableView);
        }

        private UIView RenderLayout(LayoutArrangement layout, UITableView tableView) {
            if(layout == null) {
                return null;
            }

            var view = DequeueHeaderFooterView(tableView);
            layout.MakeViews(view);
            return view;
        }

        private UITableViewHeaderFooterView DequeueHeaderFooterView(UITableView tableView) {
            var view = tableView.DequeueReusableHeaderFooterView(reuseIdentifier);
            if (view != null) {
                return view
            } else {
                return new UITableViewHeaderFooterView(new NSString(reuseIdentifier));
            }
        }

        #endregion

        #region UITableViewDataSource

        public nint NumberOfSections(UITableView tableView, nint section)
        {
            return currentArrangement.Length;
        }

        public nint RowsInSection(UITableView tableView, nint section)
        {
            return currentArrangement[section].Items.Length;
        }

        public UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var item = currentArrangement[indexPath.Section].Items[indexPath.Item];
            var cell = tableView.DequeueReusableCell(reuseIdentifier, indexPath);
            item.MakeViews(cell.ContentView);
            return cell;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region UICollectionViewDelegateFlowLayout

        public CGSize GetSizeForItem(UICollectionView collectionView, UICollectionViewLayout collectionViewLayout, NSIndexPath indexPath) {
            return currentArrangement[indexPath.Section].Items[indexPath.Item].Frame.Size;
        }

        public CGSize GetReferenceSizeForHeader(UICollectionView collectionView, UICollectionViewLayout collectionViewLayout, int section) {
            return currentArrangement[section].Header?.Frame.Size ?? CGSize.Empty;
        }

        public CGSize GetReferenceSizeForFooter(UICollectionView collectionView, UICollectionViewLayout collectionViewLayout, int section) {
            return currentArrangement[section].Footer?.Frame.Size ?? CGSize.Empty;
        }

        #endregion

        #region UICollectionViewDatasource

        public nint GetItemsCount(UICollectionView collectionView, nint section)
        {
            return currentArrangement[section].Items.Length;
        }

        public nint NumberOfSections(UICollectionView collectionView) {
            return currentArrangement.Length;
        }

        public UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
        {
            var item = currentArrangement[indexPath.Section].Items[indexPath.Item];
            var cell = collectionView.DequeueReusableCell(reuseIdentifier, indexPath) as UICollectionViewCell;
            item.MakeViews(cell.ContentView);
            return cell;
        }

        public UICollectionReusableView GetViewForSupplementaryElement(UICollectionView collectionView, NSString kind, NSIndexPath indexPath) {
            var view = collectionView.DequeueReusableSupplementaryView(kind, reuseIdentifier, indexPath);
            LayoutArrangement arrangement = null;
            if (kind == UICollectionElementKindSectionKey.Header) {
                arrangement = currentArrangement[indexPath.Section].Header;
            }
            else if (kind == UICollectionElementKindSectionKey.Footer) {
                arrangement = currentArrangement[indexPath.Section].Footer;
            }
            else { 
                arrangement = null;
                Debug.Fail("unknown supplementary view kind (kind)");
            }
            arrangement?.MakeViews(view);

            return view;
        }

        #endregion
    }

    public class Section<C, T> {

        public T Header { get; }
        public C Items { get; }
        public T Footer { get; }

        public Section(T header, C items, T footer)
        {
            this.Header = header;
            this.Items = items;
            this.Footer = footer;
        }

        public Section<U[], U> Map<U>(Func<T, U> mapper) {
            var header = this.Header.Select(mapper);
            var items = this.Items.Select(mapper);
            var footer = this.Footer.Select(mapper);
            return new Section<U[], U>(header, items, footer);
        }
    }
}

