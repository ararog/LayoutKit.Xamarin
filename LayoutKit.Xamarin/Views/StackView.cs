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
        public StackLayout.Distribution distribution;

        /// The distance that the arranged views are inset from the stack view. Defaults to 0.
        public UIEdgeInsets contentInsets;

        /// The stack's alignment inside its parent.
        public Alignment alignment;

        /// The stack's flexibility.
        public Flexibility flexibility;

        private UIView[] arrangedSubviews = new UIView[] { };

        public StackView(Axis axis,
            float spacing = 0,
            StackLayout.Distribution distribution = StackLayout.Distribution.Leading,
            UIEdgeInsets contentInsets = UIEdgeInsets.Zero,
            Alignment alignment = Alignment.fill,
            Flexibility flexibility = null)
        {

            this.axis = axis;
            this.spacing = spacing;
            this.distribution = distribution;
            this.flexibility = flexibility;
            this.contentInsets = contentInsets;
            this.alignment = alignment;
            base(CGRect.Empty);
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
                });

                var stack = new StackLayout
                (
                    axis,
                    spacing,
                    distribution,
                    alignment,
                    flexibility,
                    sublayouts,
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

        LayoutMeasurement Measurement(CGSize maxSize) {
            var size = view.SizeThatFits(maxSize);
            return new LayoutMeasurement(this, size, maxSize, new LayoutMeasurement[] { });
        }

        LayoutArrangement Arrangement(CGRect rect, LayoutMeasurement measurement) {
            return new LayoutArrangement(this, rect, []);
        }

        UIView MakeView() {
            return view;
        }

        var Flexibility flexibility
        {
            var horizontal = flexForAxis(.Horizontal);
            var vertical = flexForAxis(.Vertical);
            return new Flexibility(horizontal, vertical);
        }

        private Flexibility.Flex FlexForAxis(UILayoutConstraintAxis axis) {
            switch (view.ContentHuggingPriorityForAxis(.Horizontal)) {
            case UILayoutPriorityRequired:
                return null;
            case let priority:
                return -Int32(priority);
            }
        }
    }
}

