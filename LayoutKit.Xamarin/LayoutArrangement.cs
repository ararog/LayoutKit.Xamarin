using CoreGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using UIKit;

namespace LayoutKit.Xamarin
{
	public class LayoutArrangement
	{
        public CGRect Frame { get; }

        public ILayout Layout { get; }

        public LayoutArrangement[] Sublayouts { get; }

        public LayoutArrangement(ILayout layout, CGRect frame, LayoutArrangement[] sublayouts)
        {
            this.Layout = layout;
            this.Frame = frame;
            this.Sublayouts = sublayouts;
        }

        /**
         Creates the views for the layout and adds them as subviews to the provided view.
         Existing subviews of the provided view will be removed.
         If no view is provided, then a new one is created and returned.
         - parameter view: The layout's views will be added as subviews to this view, if provided.
         - parameter direction: The natural direction of the layout (default: .LeftToRight).
         If it does not match the user's language direction, then the layout's views will be flipped horizontally.
         Only provide this parameter if you want to test the flipped version of your layout, or if your layouts are declared for
         the right-to-left languages and you want them to get flipped for left-to-right languages.
         - returns: The root view. If a view was provided, the same view will be returned, otherwise, a new one will be created.
         */
        public UIView MakeViews(UIView view = null, UIUserInterfaceLayoutDirection direction = UIUserInterfaceLayoutDirection.LeftToRight) {
            var views = MakeSubviews();
            UIView rootView;

            if (view != null) {
                // We have a parent view so replace all of its subviews.
                // TODO: could be smarter and reuse views.
                foreach(var subview in view.Subviews) {
                    subview.RemoveFromSuperview();
                }
                foreach (var subview in views) {
                    view.AddSubview(subview);
                }
                rootView = view;
            } else if(views.Length == 1) {
                // We have a single view so it is our root view.
                view = views[0];
                rootView = view;
            } else {
                // We have multiple views so create a root view.
                rootView = new UIView(Frame);
                foreach(var subview in views) {
                    // Unapply the offset that was applied in makeSubviews()
                    subview.Frame.Offset(-Frame.X, -Frame.Y);
                    rootView.AddSubview(subview);
                }
            }

            HandleLayoutDirection(rootView, direction);
            return rootView;
        }

        /// Horizontally flips the view frames if direction does not match the user's language direction.
        private void HandleLayoutDirection(UIView view, UIUserInterfaceLayoutDirection direction)
        {
            if (UIApplication.SharedApplication.UserInterfaceLayoutDirection != direction) {
                FlipSubviewsHorizontally(view);
            }
        }

        /// Flips the right and left edges of the view's subviews.
        private void FlipSubviewsHorizontally(UIView view)
        {
            foreach(var subview in view.Subviews) {
                CGRect frame = subview.Frame;
                frame.X = view.Frame.Width - subview.Frame.GetMaxX();
                subview.Frame = frame;
                FlipSubviewsHorizontally(subview);
            }
        }

        /// Returns the views for the layout and all of its sublayouts.
        private UIView[] MakeSubviews() {
            var subviews = Sublayouts.SelectMany((LayoutArrangement sublayout) => {
                return sublayout.MakeSubviews();
            }).ToArray();

            var view = Layout.MakeView();
            if (view != null) {
                view.Frame = Frame;
                foreach(var subview in subviews) {
                    view.AddSubview(subview);
                }
                var views = new UIView[] { view };
                return views;
            } else {
                foreach(var subview in subviews) {
                    subview.Frame.Offset(Frame.X, Frame.Y);
                }
                return subviews;
            }
        }
    }	   
}

