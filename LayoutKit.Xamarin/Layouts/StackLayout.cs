using CoreGraphics;
using System;
using System.Linq;
namespace LayoutKit.Xamarin
{
	public class StackLayout : PositioningLayout, ILayout
    {
        /**
         Specifies how excess space along the axis is allocated.
        */

        public struct DistributionConfig
        {
            public float InitialAxisOffset { get; set; }
            public float AxisSpacing { get; set; }
            public int? StretchIndex { get; set; }
        }

        /// The axis along which sublayouts are stacked.
        public Axis Axis { get; set; } 

        /**
         The distance in points between adjacent edges of sublayouts along the axis.
         For Distribution.EqualSpacing, this is a minimum spacing. For all other distributions it is an exact spacing.
         */
        public float Spacing { get; set; }

        /// The distribution of space along the stack's axis.
        public Distribution Distribution { get; set; }

        /// The stack's alignment inside its parent.
        public Alignment Alignment { get; set; }

        /// The stack's flexibility.
        public Flexibility Flexibility { get; set; }

        /// The stacked layouts.
        public ILayout[] Sublayouts { get; set; }

        public StackLayout(Axis axis,
                ILayout[] sublayouts,
                float spacing = 0,
                Distribution distribution = Distribution.FillFlexing,
                Alignment? alignment = null,
                Flexibility? flexibility = null,
                ConfigDelegate config = null) : base(config)
        {
            if(! alignment.HasValue)
                this.Alignment = Alignment.fill;

            this.Axis = axis;
            this.Spacing = spacing;
            this.Distribution = distribution;
            this.Flexibility = flexibility ?? StackLayout.DefaultFlexibility(axis, sublayouts);
            this.Sublayouts = sublayouts;
        }

        public LayoutMeasurement Measurement(CGSize maxSize)  {

            var availableSize = new AxisSize(axis, maxSize);
            var sublayoutMeasurements = [LayoutMeasurement ?](sublayouts.count, null);
            var usedSize = new AxisSize(axis, CGSize.Empty);

            float sublayoutLengthForEqualSizeDistribution;
            if(distribution == .fillEqualSize) {
                sublayoutLengthForEqualSizeDistribution = sublayoutSpaceForEqualSizeDistribution(availableSize.axisLength, sublayouts.count);
            } else {
                sublayoutLengthForEqualSizeDistribution = null;
            }

            foreach((index, sublayout) in sublayoutsByAxisFlexibilityAscending())
            {
                if(availableSize.AxisLength <= 0 || availableSize.CrossLength <= 0) {
                    // There is no more room in the stack so don't bother measuring the rest of the sublayouts.
                    break;
                }

                CGSize sublayoutMasurementAvailableSize;
                if let sublayoutLengthForEqualSizeDistribution = sublayoutLengthForEqualSizeDistribution {
                    sublayoutMasurementAvailableSize = new AxisSize(axis,
                                                        sublayoutLengthForEqualSizeDistribution,
                                                        availableSize.CrossLength).Size;
                } 
                else {
                    sublayoutMasurementAvailableSize = availableSize.Size;
                }

                var sublayoutMeasurement = sublayout.measurement(within: sublayoutMasurementAvailableSize);
                sublayoutMeasurements[index] = sublayoutMeasurement
                var sublayoutAxisSize = new AxisSize(axis, sublayoutMeasurement.size);

                if (sublayoutAxisSize.AxisLength > 0) {
                    // If we are the first sublayout in the stack, then no leading spacing is required.
                    // Otherwise account for the spacing.
                    let leadingSpacing = (usedSize.AxisLength > 0) ? spacing : 0;
                    usedSize.AxisLength += leadingSpacing + sublayoutAxisSize.AxisLength;
                    usedSize.CrossLength = max(usedSize.CrossLength, sublayoutAxisSize.CrossLength);

                    // Reserve spacing for the next sublayout.
                    availableSize.AxisLength -= sublayoutAxisSize.axisLength + spacing;
                }
            }

            let nonNilMeasuredSublayouts = sublayoutMeasurements.flatMap { $0 };

            if(distribution == .fillEqualSize && !nonNilMeasuredSublayouts.isEmpty) {
                let maxAxisLength = nonNilMeasuredSublayouts.map({ AxisSize(axis, $0.size).AxisLength }).maxElement() ?? 0;
                usedSize.AxisLength = (maxAxisLength + spacing) * nonNilMeasuredSublayouts.Count - spacing;
            }

            return new LayoutMeasurement(this, usedSize.size, maxSize, nonNilMeasuredSublayouts);
        }

        public LayoutArrangement Arrangement(CGRect rect, LayoutMeasurement measurement) {
            var frame = alignment.Position(measurement.Size, rect);
            var availableSize = new AxisSize(axis, frame.size);
            var excessAxisLength = availableSize.axisLength - AxisSize(axis, measurement.Size).AxisLength;
            var config = distributionConfig(excessAxisLength)

            var nextOrigin = new AxisPoint(axis, config.initialAxisOffset, 0);
            var sublayoutArrangements = new LayoutArrangement[] { };
            foreach((index, sublayout) in measurement.Sublayouts.GetEnumerator()) {
                var sublayoutAvailableSize = new AxisSize(axis, sublayout.size);
                sublayoutAvailableSize.CrossLength = availableSize.CrossLength;
                if (distribution == .fillEqualSize) {
                    sublayoutAvailableSize.AxisLength = SublayoutSpaceForEqualSizeDistribution(
                        new AxisSize(axis, frame.size).AxisLength,
                        measurement.Sublayouts.Length);
                } else if (config.stretchIndex == index) {
                    sublayoutAvailableSize.AxisLength += excessAxisLength;
                }
                var sublayoutArrangement = sublayout.Arrangement(new CGRect(nextOrigin.Point, sublayoutAvailableSize.Size));
                sublayoutArrangements.Append(sublayoutArrangement);
                nextOrigin.AxisOffset += sublayoutAvailableSize.AxisLength;
                if (sublayoutAvailableSize.AxisLength > 0) {
                    // Only add spacing below a view if it was allocated non-zero height.
                    nextOrigin.AxisOffset += config.AxisSpacing;
                }
            }
            return new LayoutArrangement(this, frame, sublayoutArrangements);
        }

        private float SublayoutSpaceForEqualSizeDistribution(float totalAvailableSpace, int sublayoutCount) {
            if(sublayoutCount <= 0) {
                return totalAvailableSpace;
            }
            if(spacing == 0) {
                return totalAvailableSpace / sublayoutCount;
            }
            // Note: we don't actually need to check for zero spacing above, because division by zero produces a valid result for floating point values.
            // We check anyway for the sake of clarity.
            var maxSpacings = Math.Floor(totalAvailableSpace / spacing);
            var visibleSublayoutCount = Math.Min(sublayoutCount, maxSpacings + 1);
            var spaceAvailableForSublayouts = totalAvailableSpace - (visibleSublayoutCount - 1) * spacing;
            return spaceAvailableForSublayouts / visibleSublayoutCount;
        }
    }

    public enum Distribution
    {
        Leading,

        /**
         Sublayouts are positioned starting at the bottom edge of vertical stacks or at the the trailing edge of horizontal stacks.
         */
        Trailing,

        /**
         Sublayouts are positioned so that they are centered along the stack's axis.
         */
        Center,

        /**
         Distributes excess axis space by increasing the spacing between each sublayout by an equal amount.
         The sublayouts and the adjusted spacing consume all of the available axis space.
         */
        FillEqualSpacing,

        /**
         Distributes axis space equally among the sublayouts.
         The spacing between the sublayouts remains equal to the spacing parameter.
         */
        FillEqualSize,

        /**
         Distributes excess axis space by growing the most flexible sublayout along the axis.
         */
        FillFlexing,
    }

    #region Distribution

    public static class StackLayout_Distribution
    {
        private static StackLayout.DistributionConfig DistributionConfig(this StackLayout stackLayout, float excessAxisLength) {
            float initialAxisOffset = 0;
            float axisSpacing = 0;
            int? stretchIndex = null;
            switch(stackLayout.Distribution)
            {
                case Distribution.Leading:
                    initialAxisOffset = 0;
                    axisSpacing = stackLayout.Spacing;
                    break;
                case Distribution.Trailing:
                    initialAxisOffset = excessAxisLength;
                    axisSpacing = stackLayout.Spacing;
                    break;
                case Distribution.Center:
                    initialAxisOffset = excessAxisLength / 2.0f;
                    axisSpacing = stackLayout.Spacing;
                    break;
                case Distribution.FillEqualSpacing:
                    initialAxisOffset = 0;
                    axisSpacing = Math.Max(stackLayout.Spacing, excessAxisLength / stackLayout.Sublayouts.Length - 1);
                    break;
                case Distribution.FillEqualSize:
                    initialAxisOffset = 0;
                    axisSpacing = stackLayout.Spacing;
                    break;
                case Distribution.FillFlexing:
                    axisSpacing = stackLayout.Spacing;
                    initialAxisOffset = 0;
                    if(excessAxisLength > 0){
                        stretchIndex = StretchableSublayoutIndex();
                    }
                    break;
            }
            return new StackLayout.DistributionConfig
            {
                InitialAxisOffset = initialAxisOffset,
                AxisSpacing = axisSpacing,
                StretchIndex = stretchIndex
            };
        }
    }

    #endregion Distribution

    #region Flexing

    public static class StackLayout_Flexing
    {
        /**
        Returns the sublayouts sorted by flexibility ascending.
        */
        private static dynamic[] SublayoutsByAxisFlexibilityAscending(this StackLayout stackLayout) {
            return stackLayout.Sublayouts.GetEnumerator().Sort(CompareLayoutsByFlexibilityAscending);
        }

        /**
         Returns the index of the most flexible sublayout.
         It returns nil if there are no flexible sublayouts.
         */
        private static int? StretchableSublayoutIndex(this StackLayout stackLayout) {
            dynamic tuple = stackLayout.Sublayouts.GetEnumerator().maxElement(CompareLayoutsByFlexibilityAscending);
            if (tuple == null) {
                return null;
            }
            if(sublayout.Flexibility.Flex(stackLayout.Axis) == null) {
                // The most flexible sublayout is still not flexible, so don't stretch it.
                return null;
            }
            return index;
        }

        /**
         Returns true iff the left layout is less flexible than the right layout.
         If two sublayouts have the same flexibility, then sublayout with the higher index is considered more flexible.
         Inflexible layouts are sorted before all flexible layouts.
         */
        private static bool CompareLayoutsByFlexibilityAscending(this StackLayout stackLayout, dynamic left, dynamic right) {
            var leftFlex = left.Layout.Flexibility.Flex(stackLayout.Axis);
            var rightFlex = right.Layout.Flexibility.Flex(stackLayout.Axis);
            if(leftFlex == rightFlex) {
                return left.Index < right.Index;
            }
            // nil is less than all integers
            return leftFlex < rightFlex;
        }

        /**
         Inherit the maximum flexibility of sublayouts along the axis and minimum flexibility of sublayouts across the axis.
         */
        private static Flexibility DefaultFlexibility(Axis axis, ILayout[] sublayouts) {
            var initial = new AxisFlexibility(axis, null, .max);
            return sublayouts.reduce(initial) { (AxisFlexibility flexibility, ILayout sublayout) -> AxisFlexibility in
                var subflex = new AxisFlexibility(axis, sublayout.flexibility);
                var axisFlex = Flexibility.Max(flexibility.axisFlex, subflex.axisFlex);
                var crossFlex = Flexibility.Min(flexibility.crossFlex, subflex.crossFlex);
                return new AxisFlexibility(axis, axisFlex, crossFlex);
            }.Flexibility;
        }
    }
}