using CoreGraphics;
using System;
using System.Linq;

namespace LayoutKit.Xamarin
{
	public class SizeLayout : PositioningLayout, ILayout
	{
        public nfloat? width;
        public nfloat? height;
        public Alignment? alignment;
        public Flexibility? flexibility;
        public ILayout sublayout;

        /**
         Creates a SizeLayout that measures to a specific width and/or height.
     
         If a dimension is nil, then
         - The measurement along that dimension is inherited from the sublayout, or zero if there is no sublayout.
         - The alignment along that dimension defaults to .fill.
         - The flexiblity along that dimension defaults to .defaultFlex.
         If a dimension is not nil, then
         - The alignment along that dimension defaults to .center.
         - The flexibility along that dimension defaults to .inflexible.
         */
        public SizeLayout(nfloat? width = null,
                    nfloat? height = null,
                    Alignment? alignment = null,
                    Flexibility? flexibility = null,
                    ILayout sublayout = null,
                    ConfigDelegate config = null) : base(config)
        {
            this.width = width;
            this.height = height;
            this.alignment = alignment ?? SizeLayout.DefaultAlignment(width, height);
            this.flexibility = flexibility ?? SizeLayout.DefaultFlexibility(width, height);
            this.sublayout = sublayout;
        }

        private static Alignment DefaultAlignment(nfloat? width, nfloat? height) {
            return new Alignment(height == null ? Alignment.Vertical.Fill : Alignment.Vertical.Center,
                             width == null ? Alignment.Horizontal.Fill : Alignment.Horizontal.Center);
        }

        private static Flexibility DefaultFlexibility(nfloat? width, nfloat? height) {
            return new Flexibility(width == null ? Flexibility.defaultFlex : Flexibility.inflexibleFlex,
                           height == null ? Flexibility.defaultFlex : Flexibility.inflexibleFlex);
        }
    
        /**
         Creates a SizeLayout that measures to the provided size.
         By default it centers itself the available space and is inflexible.
         */
        public SizeLayout(CGSize size,
                    Alignment? alignment = null,
                    Flexibility? flexibility = null,
                    ILayout sublayout = null,
                    ConfigDelegate config = null) : base(config) {

            this.width = size.Width;
            this.height = size.Height;
            this.alignment = alignment;
            this.flexibility = flexibility;
            this.sublayout = sublayout;
        }

        public LayoutMeasurement Measurement(CGSize maxSize) {
            var size = new CGSize(width ?? .max, height ?? .max);
            var constrainedSize = size.SizeDecreasedToSize(maxSize);

            // If at least one dimension is nil, then we need to measure the sublayout to inherit its value (zero if there is no sublayout).
            if(width == null || height == null) {
                var subsize = sublayout.Measurement(constrainedSize).Size ?? CGSize.Empty;
                if(width == null) {
                    constrainedSize.Width = subsize.Width;
                }
                if(height == null) {
                    constrainedSize.Height = subsize.Height;
                }
            }

            return new LayoutMeasurement(this, constrainedSize, maxSize, new LayoutMeasurement[]{ });
        }

        public LayoutArrangement Arrangement(CGRect rect, LayoutMeasurement measurement) {
            var frame = alignment.Value.Position(measurement.Size, rect);
            var sublayouts = sublayout.Select((layout) => {
                return layout.Arrangement(frame.Size.Width, frame.Size.Height);
            }).ToArray();
            return new LayoutArrangement(this, frame, sublayouts ?? new LayoutMeasurement[] { });
        }
	}
}

