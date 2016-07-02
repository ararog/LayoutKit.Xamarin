using CoreGraphics;
using System;
using UIKit;

namespace LayoutKit.Xamarin
{
	public interface ILayout
	{
        /**
         Measures the minimum size of the layout and its sublayouts.
         It MAY be run on a background thread.
         
         - parameter maxSize: The maximum size available to the layout.
         - returns: The minimum size required by the layout and its sublayouts given a maximum size.
         The size of the layout MUST NOT exceed `maxSize`.
         */
        LayoutMeasurement Measurement(CGSize maxSize);

        /**
         Returns the arrangement of frames for the layout inside a given rect.
         The frames SHOULD NOT overflow rect, otherwise they may overlap with adjacent layouts.
         The layout MAY choose to not use the entire rect (and instead align itself in some way inside of the rect),
         but the caller SHOULD NOT reallocate unused space to other layouts because this could break the layout's desired alignment and padding.
         Space allocation SHOULD happen during the measure pass.
         MAY be run on a background thread.
         
         - parameter rect: The rectangle that the layout must position itself in.
         - parameter measurement: A measurement which has size less than or equal to `rect.size` and greater than or equal to `measurement.maxSize`.
         - returns: A complete set of frames for the layout.
         */
        LayoutArrangement Arrangement(CGRect rect, LayoutMeasurement measurement);

        /**
         Returns a UIView for the layout or nil if the layout does not require a view (i.e. it merely positions sublayouts).
         MUST be run on the main thread.
         */
        UIView MakeView();

        /**
         The flexibility of the layout.
         If a layout has a single sublayout, it SHOULD inherit the flexiblity of its sublayout.
         If a layout has no sublayouts (e.g. LabelLayout), it SHOULD allow its flexibility to be configured.
         All layouts SHOULD provide a default flexiblity.
         TODO: figure out how to assert if inflexible layouts are compressed.
         */
        Flexibility Flexibility { get; }
    }
}

