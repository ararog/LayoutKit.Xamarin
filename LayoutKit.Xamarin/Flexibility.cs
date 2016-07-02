using System;
using Flex = System.Nullable<System.Int32>;
namespace LayoutKit.Xamarin
{
	public struct Flexibility
	{
        /**
         The inflexible flex value.
         */
        public static Flex inflexibleFlex = null;

        /**
         The default flex value.
         */
        public static Flex defaultFlex = 0;

        /**
         The maximum flex value.
         */
        public static Flex maxFlex = Int32.MaxValue;

        /**
         The minimum flex value that is still flexible.
         */
        public static Flex minFlex = Int32.MinValue;

        /**
         A flex value that is higer than the default.
         It is the midpoint between the default flex value and the maximum flex value.
         */
        public static Flex highFlex = Int32.MaxValue / 2;

        /**
         A flex value that is lower than the default.
         It is the midpoint between the default flex value and the minimum flex value.
         */
        public static Flex lowFlex = Int32.MinValue / 2;

        /**
         Not flexible, even if there is excess space.
         Even inflexible layouts MAY be compressed when there is insufficient space.
         */
        public static Flexibility inflexible = new Flexibility(inflexibleFlex, inflexibleFlex);

        /**
         The default flexibility.
         */
        public static Flexibility flexible = new Flexibility(defaultFlex, defaultFlex);

        /**
         More flexible than the default flexibility.
         */
        public static Flexibility high = new Flexibility(highFlex, highFlex);

        /**
         Less flexible than the default flexibility.
         */
        public static Flexibility low = new Flexibility(lowFlex, lowFlex);

        /**
         The minimum flexibility that is still flexible.
         */
        public static Flexibility min = new Flexibility(minFlex, minFlex);

        /**
         The maximum flexibility.
         */
        public static Flexibility max = new Flexibility(maxFlex, maxFlex);

        public Flex vertical;
        public Flex horizontal;

        public Flexibility(Flex horizontal, Flex vertical) {
            this.horizontal = horizontal;
            this.vertical = vertical;
        }

        /**
         Returns the flex along an axis.
         */
        public Flex Flex(Axis axis) {
            switch (axis) {
                case Axis.Vertical:
                    return vertical;
                case Axis.Horizontal:
                    return horizontal;
            }
        }

        public static Flex Max(Flex left, Flex right) {
            if(left == null) {
                return right;
            }
            if(right == null) {
                return left;
            }
            return Math.Max(left, right);
        }

        public static Flex Min(Flex left, Flex right)  {
            if(left == null || right == null) {
                // One of them is inflexible so return nil flex (inflexible)
                return null;
            }

            return Math.Min(left, right);
        }
	}
}

