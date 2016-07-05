using CoreGraphics;
using System;
using System.Linq;
using UIKit;

namespace LayoutKit.Xamarin
{
	public class InsetLayout: PositioningLayout<UIView>, Layout
	{
        public UIEdgeInsets insets;
        public Alignment alignment;
        public ILayout sublayout;

        public InsetLayout(UIEdgeInsets insets, ILayout sublayout, Alignment? alignment = null, ConfigDelegate config = null) : base(config)
        {
            if (! alignment.HasValue)
                this.alignment = Alignment.fill;
            this.insets = insets;
            this.sublayout = sublayout;
        }

        public InsetLayout(float inset, ILayout sublayout, Alignment? alignment = null, ConfigDelegate config = null) : base(config)
        {
            var insets = new UIEdgeInsets(inset, inset, inset, inset);
            if (!alignment.HasValue)
                this.alignment = Alignment.fill;
            this.insets = insets;
            this.sublayout = sublayout;
        }

        public LayoutMeasurement Measurement(CGSize maxSize) {
            var insetMaxSize = maxSize.SizeDecreasedByInsets(insets);
            var sublayoutMeasurement = sublayout.Measurement(insetMaxSize);
            var size = sublayoutMeasurement.Size.SizeIncreasedByInsets(insets);
            return new LayoutMeasurement(this, size, maxSize, [sublayoutMeasurement]);
        }

        public LayoutArrangement Arrangement(CGRect rect, LayoutMeasurement measurement) {
            var frame = alignment.Position(measurement.Size, rect);
            var insetOrigin = new CGPoint(insets.Left, insets.Top);
            var insetSize = frame.Size.SizeDecreasedByInsets(insets);
            var sublayoutRect = new CGRect(insetOrigin, insetSize);
            var sublayouts = measurement.Sublayouts.Select( (LayoutMeasurement m) => {
                return m.Arrangement(sublayoutRect);
            }).ToArray();
            return new LayoutArrangement(this, frame, sublayouts);
        }

        public Flexibility Flexibility {
            get { return sublayout.Flexibility; }
        }
	}
}

