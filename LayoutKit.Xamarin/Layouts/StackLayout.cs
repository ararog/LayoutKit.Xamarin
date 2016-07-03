using CoreGraphics;
using System;
using System.Linq;
using UIKit;

namespace LayoutKit.Xamarin
{
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

    public class StackLayout : PositioningLayout<UIView>, ILayout
    {
        /**
         Specifies how excess space along the axis is allocated.
        */

        private struct DistributionConfig
        {
            public nfloat InitialAxisOffset { get; set; }
            public nfloat AxisSpacing { get; set; }
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
            this.Flexibility = flexibility ?? DefaultFlexibility(Axis, Sublayouts);
            this.Sublayouts = sublayouts;
        }

        public LayoutMeasurement Measurement(CGSize maxSize)  {

            var availableSize = new AxisSize(Axis, maxSize);
            var sublayoutMeasurements = new LayoutMeasurement[Sublayouts.Length];
            var usedSize = new AxisSize(Axis, CGSize.Empty);

            nfloat? sublayoutLengthForEqualSizeDistribution;
            if(Distribution == Distribution.FillEqualSize) {
                sublayoutLengthForEqualSizeDistribution = SublayoutSpaceForEqualSizeDistribution(availableSize.AxisLength, Sublayouts.Length);
            } else {
                sublayoutLengthForEqualSizeDistribution = null;
            }

            foreach(var dynamic in SublayoutsByAxisFlexibilityAscending())
            {
                if(availableSize.AxisLength <= 0 || availableSize.CrossLength <= 0) {
                    // There is no more room in the stack so don't bother measuring the rest of the sublayouts.
                    break;
                }

                CGSize sublayoutMasurementAvailableSize;
                if (sublayoutLengthForEqualSizeDistribution != null) {
                    sublayoutMasurementAvailableSize = new AxisSize(Axis,
                                                        sublayoutLengthForEqualSizeDistribution.Value,
                                                        availableSize.CrossLength).Size;
                } 
                else {
                    sublayoutMasurementAvailableSize = availableSize.Size;
                }

                var sublayoutMeasurement = dynamic.Sublayout.Measurement(sublayoutMasurementAvailableSize);
                sublayoutMeasurements[dynamic.Index] = sublayoutMeasurement;
                var sublayoutAxisSize = new AxisSize(Axis, sublayoutMeasurement.size);

                if (sublayoutAxisSize.AxisLength > 0) {
                    // If we are the first sublayout in the stack, then no leading spacing is required.
                    // Otherwise account for the spacing.
                    var leadingSpacing = (usedSize.AxisLength > 0) ? Spacing : 0;
                    usedSize.AxisLength += leadingSpacing + sublayoutAxisSize.AxisLength;
                    usedSize.CrossLength = Math.Max(usedSize.CrossLength, sublayoutAxisSize.CrossLength);

                    // Reserve spacing for the next sublayout.
                    availableSize.AxisLength -= sublayoutAxisSize.AxisLength + Spacing;
                }
            }

            var nonNilMeasuredSublayouts = sublayoutMeasurements.SelectMany( i => i.Sublayouts).ToArray();

            if(Distribution == Distribution.FillEqualSize && nonNilMeasuredSublayouts.Length != 0) {
                var maxAxisLength = nonNilMeasuredSublayouts.Select(subLayout => new AxisSize(Axis, subLayout.Size).AxisLength).Max<nfloat>();
                usedSize.AxisLength = (maxAxisLength + Spacing) * nonNilMeasuredSublayouts.Length - Spacing;
            }

            return new LayoutMeasurement(this, usedSize.Size, maxSize, nonNilMeasuredSublayouts);
        }

        public LayoutArrangement Arrangement(CGRect rect, LayoutMeasurement measurement) {
            var frame = Alignment.Position(measurement.Size, rect);
            var availableSize = new AxisSize(Axis, frame.Size);
            var excessAxisLength = availableSize.AxisLength - new AxisSize(Axis, measurement.Size).AxisLength;
            var config = GetDistributionConfig(excessAxisLength);

            var nextOrigin = new AxisPoint(Axis, config.InitialAxisOffset, 0);
            var sublayoutArrangements = new LayoutArrangement[] { };
            foreach(var dynamic in measurement.Sublayouts.GetEnumerator()) {
                var sublayoutAvailableSize = new AxisSize(Axis, dynamic.Sublayout.Size);
                sublayoutAvailableSize.CrossLength = availableSize.CrossLength;
                if (Distribution == Distribution.FillEqualSize) {
                    sublayoutAvailableSize.AxisLength = SublayoutSpaceForEqualSizeDistribution(
                        new AxisSize(Axis, frame.Size).AxisLength,
                        measurement.Sublayouts.Length);
                } else if (config.StretchIndex == dynamic.Index) {
                    sublayoutAvailableSize.AxisLength += excessAxisLength;
                }
                var sublayoutArrangement = dynamic.Sublayout.Arrangement(new CGRect(nextOrigin.Point, sublayoutAvailableSize.Size));
                sublayoutArrangements.Append(sublayoutArrangement);
                nextOrigin.AxisOffset += sublayoutAvailableSize.AxisLength;
                if (sublayoutAvailableSize.AxisLength > 0) {
                    // Only add spacing below a view if it was allocated non-zero height.
                    nextOrigin.AxisOffset += config.AxisSpacing;
                }
            }
            return new LayoutArrangement(this, frame, sublayoutArrangements);
        }

        private nfloat SublayoutSpaceForEqualSizeDistribution(nfloat totalAvailableSpace, int sublayoutCount) {
            if(sublayoutCount <= 0) {
                return totalAvailableSpace;
            }
            if(Spacing == 0) {
                return totalAvailableSpace / sublayoutCount;
            }
            // Note: we don't actually need to check for zero spacing above, because division by zero produces a valid result for floating point values.
            // We check anyway for the sake of clarity.
            var maxSpacings = Math.Floor(totalAvailableSpace / Spacing);
            var visibleSublayoutCount = Math.Min(sublayoutCount, maxSpacings + 1);
            var spaceAvailableForSublayouts = totalAvailableSpace - (visibleSublayoutCount - 1) * Spacing;
            return spaceAvailableForSublayouts / visibleSublayoutCount;
        }

        private DistributionConfig GetDistributionConfig(nfloat excessAxisLength) {
            nfloat initialAxisOffset = 0;
            nfloat axisSpacing = 0;
            int? stretchIndex = null;
            switch(Distribution)
            {
                case Distribution.Leading:
                    initialAxisOffset = 0;
                    axisSpacing = Spacing;
                    break;
                case Distribution.Trailing:
                    initialAxisOffset = excessAxisLength;
                    axisSpacing = Spacing;
                    break;
                case Distribution.Center:
                    initialAxisOffset = excessAxisLength / 2.0f;
                    axisSpacing = Spacing;
                    break;
                case Distribution.FillEqualSpacing:
                    initialAxisOffset = 0;
                    axisSpacing = Math.Max(Spacing, excessAxisLength / Sublayouts.Length - 1);
                    break;
                case Distribution.FillEqualSize:
                    initialAxisOffset = 0;
                    axisSpacing = Spacing;
                    break;
                case Distribution.FillFlexing:
                    axisSpacing = Spacing;
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

        /**
        Returns the sublayouts sorted by flexibility ascending.
        */
        private dynamic[] SublayoutsByAxisFlexibilityAscending() {
            return Sublayouts.Sort(CompareLayoutsByFlexibilityAscending);
        }

        /**
         Returns the index of the most flexible sublayout.
         It returns nil if there are no flexible sublayouts.
         */
        private int? StretchableSublayoutIndex() {
            dynamic tuple = Sublayouts.MaxElement(CompareLayoutsByFlexibilityAscending);
            if (tuple == null) {
                return null;
            }
            if(Flexibility.Flex(Axis) == null) {
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
        private bool CompareLayoutsByFlexibilityAscending(dynamic left, dynamic right) {
            var leftFlex = left.Layout.Flexibility.Flex(Axis);
            var rightFlex = right.Layout.Flexibility.Flex(Axis);
            if(leftFlex == rightFlex) {
                return left.Index < right.Index;
            }
            // nil is less than all integers
            return leftFlex < rightFlex;
        }

        /**
         Inherit the maximum flexibility of sublayouts along the axis and minimum flexibility of sublayouts across the axis.
         */
        private Flexibility DefaultFlexibility(Axis axis, ILayout[] sublayouts) {
            var initial = new AxisFlexibility(axis, null, Flexibility.max);
            return sublayouts.reduce(initial) { (AxisFlexibility flexibility, ILayout sublayout) -> AxisFlexibility in
                var subflex = new AxisFlexibility(axis, sublayout.Flexibility);
                var axisFlex = Flexibility.Max(flexibility.AxisFlex, subflex.AxisFlex);
                var crossFlex = Flexibility.Min(flexibility.CrossFlex, subflex.CrossFlex);
                return new AxisFlexibility(axis, axisFlex, crossFlex);
            }.Flexibility;
        }
    }
}