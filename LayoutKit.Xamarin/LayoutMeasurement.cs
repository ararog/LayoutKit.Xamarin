using CoreGraphics;
using System;
using System.Collections.Generic;

namespace LayoutKit.Xamarin
{
	public class LayoutMeasurement
	{
        public ILayout Layout { get; set; }

        /// The minimum size of the layout given the maximum size constraint.
        public CGSize Size { get; set; }

        /// The maximum size constraint used during measurement.
        public CGSize MaxSize { get; set; }

        /// The measurements of the layout's sublayouts.
        public LayoutMeasurement[] Sublayouts { get; set; }

        public LayoutMeasurement(ILayout layout, CGSize size, CGSize maxSize, LayoutMeasurement[] sublayouts)
        {
            this.Layout = layout;
            this.Size = size;
            this.MaxSize = maxSize;
            this.Sublayouts = sublayouts;
        }

        /// Convenience method to position this measured layout.
        public LayoutArrangement Arrangement(CGRect rect) {
            return Layout.Arrangement(rect, this);
        }
    }
}

