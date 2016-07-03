using CoreGraphics;
using Foundation;
using System;
using System.Collections;
using System.Linq;
using UIKit;

namespace LayoutKit.Xamarin
{
    public class StackView : UIView
    {
        /// The axis along which arranged views are stacked.
        public Axis axis;

        /**
         The distance in points between adjacent edges of sublayouts along the axis.
         For Distribution.EqualSpacing, this is a minimum spacing. For all other distributions it is an exact spacing.
         */
        public float spacing;

        /// The distribution of space along the stack's axis.
        public Distribution distribution;

        /// The distance that the arranged views are inset from the stack view. Defaults to 0.
        public UIEdgeInsets contentInsets;

        /// The stack's alignment inside its parent.
        public Alignment alignment;

        /// The stack's flexibility.
        public Flexibility? flexibility;

        private UIView[] arrangedSubviews = new UIView[] { };

        public StackView(Axis axis,
            float spacing = 0,
            Distribution distribution = Distribution.Leading,
            UIEdgeInsets? contentInsets = null,
            Alignment? alignment = null,
            Flexibility? flexibility = null) : base(CGRect.Empty)
        {

            this.axis = axis;
            this.spacing = spacing;
            this.distribution = distribution;
            this.flexibility = flexibility;

            if(contentInsets == null)
                this.contentInsets = UIEdgeInsets.Zero;

            if (alignment == null)
                this.alignment = Alignment.fill;
        }

        public StackView(NSCoder aDecoder) {
            FatalError("init(coder:) has not been implemented");
        }

        /**
         Adds a subview to the stack.
 
         Subviews MUST implement sizeThatFits so StackView can allocate space correctly.
         If a subview uses Auto Layout, then the subview can implement sizeThatFits by calling systemLayoutSizeFittingSize.
         */
        public void AddArrangedSubviews(UIView[] subviews)
        {
            arrangedSubviews.AppendContentsOf(subviews);
            foreach (var subview in subviews) {
                AddSubview(subview);
            }
            InvalidateIntrinsicContentSize();
            SetNeedsLayout();
        }

        public override CGSize SizeThatFits(CGSize size) {
            return stackLayout.Measurement(size).Size;
        }

        public override CGSize IntrinsicContentSize() {
            return SizeThatFits(new CGSize(float.MaxValue, float.MaxValue));
        }

        public override void LayoutSubviews()
        {
            stackLayout.Measurement(Bounds.Size).Arrangement(Bounds).MakeViews(this);
        }

        private ILayout stackLayout {

            get {
                var sublayouts = arrangedSubviews.Select(view => {
                    return new ViewLayout(view);
                }).ToArray();

                var stack = new StackLayout
                (
                    axis,
                    sublayouts,
                    spacing,
                    distribution,
                    alignment,
                    flexibility,
                    null
                );

                return new InsetLayout(contentInsets, stack);
            }
        }
	}

    /// Wraps a UIView so that it conforms to the Layout protocol.
    public struct ViewLayout : ILayout
    {
        UIView view;

        public ViewLayout(UIView view){
            this.view = view;
        }

        public LayoutMeasurement Measurement(CGSize maxSize) {
            var size = view.SizeThatFits(maxSize);
            return new LayoutMeasurement(this, size, maxSize, new LayoutMeasurement[] { });
        }

        public LayoutArrangement Arrangement(CGRect rect, LayoutMeasurement measurement) {
            return new LayoutArrangement(this, rect, new LayoutArrangement[] { });
        }

        public UIView MakeView() {
            return view;
        }

        public Flexibility Flexibility
        {
            get {
                var horizontal = FlexForAxis(UILayoutConstraintAxis.Horizontal);
                var vertical = FlexForAxis(UILayoutConstraintAxis.Vertical);
                return new Flexibility(horizontal, vertical);
            }
        }

        private Int32? FlexForAxis(UILayoutConstraintAxis axis) {
            if (view.ContentHuggingPriority(UILayoutConstraintAxis.Horizontal) == (float)UILayoutPriority.Required)
            {
                return null;
            }
            else
            {
                return 0;
            }
        }
    }
}

