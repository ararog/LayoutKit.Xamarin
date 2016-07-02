using CoreGraphics;
using System;
namespace LayoutKit.Xamarin
{
    public struct OffsetAndLength
    {
        public nfloat Offset { get; set; }
        public nfloat Length { get; set; }

        public OffsetAndLength(nfloat offset, nfloat length)
        {
            this.Offset = offset;
            this.Length = length;
        }
    }

    public static class VerticalExtensions
    {
        public static OffsetAndLength Align(this Alignment.Vertical vertical, nfloat length, nfloat availableLength, nfloat offset)
        {
            // To avoid implementing the math twice, we just convert to a horizontal alignment and call its apply method.
            Alignment.Horizontal horizontal = 0;
            switch (vertical)
            {
                case Alignment.Vertical.Top:
                    horizontal = Alignment.Horizontal.Leading;
                    break;
                case Alignment.Vertical.Bottom:
                    horizontal = Alignment.Horizontal.Trailing;
                    break;
                case Alignment.Vertical.Center:
                    horizontal = Alignment.Horizontal.Center;
                    break;
                case Alignment.Vertical.Fill:
                    horizontal = Alignment.Horizontal.Fill;
                    break;
            }
            return horizontal.Align(length, availableLength, offset);
        }
    }

    public static class HorizontalExtensions
    {
        public static OffsetAndLength Align(this Alignment.Horizontal horizontal, nfloat length, nfloat availableLength, nfloat offset)
        {
            var excessLength = availableLength - length;
            nfloat clampedLength = (nfloat) Math.Min(availableLength, length);
            nfloat alignedLength = 0;
            nfloat alignedOffset = 0;
            switch (horizontal)
            {
                case Alignment.Horizontal.Leading:
                    alignedOffset = 0;
                    alignedLength = clampedLength;
                    break;
                case Alignment.Horizontal.Trailing:
                    alignedOffset = excessLength;
                    alignedLength = clampedLength;
                    break;
                case Alignment.Horizontal.Center:
                    alignedOffset = excessLength / 2.0f;
                    alignedLength = clampedLength;
                    break;
                case Alignment.Horizontal.Fill:
                    alignedOffset = 0;
                    alignedLength = availableLength;
                    break;
            }
            return new OffsetAndLength(offset + alignedOffset, alignedLength);
        }
    }

    public struct Alignment
	{
        public static Alignment center = new Alignment(Vertical.Center, Horizontal.Center);
        public static Alignment centerLeading = new Alignment(Vertical.Center, Horizontal.Leading);
        public static Alignment centerTrailing = new Alignment(Vertical.Center, Horizontal.Trailing);

        public static Alignment fill = new Alignment(Vertical.Fill, Horizontal.Fill);
        public static Alignment fillLeading = new Alignment(Vertical.Fill, Horizontal.Leading);
        public static Alignment fillTrailing = new Alignment(Vertical.Fill, Horizontal.Trailing);

        public static Alignment topLeading = new Alignment(Vertical.Top, Horizontal.Leading);
        public static Alignment topTrailing = new Alignment(Vertical.Top, Horizontal.Trailing);
        public static Alignment topCenter = new Alignment(Vertical.Top, Horizontal.Center);
        public static Alignment topFill = new Alignment(Vertical.Top, Horizontal.Fill);

        public static Alignment bottomLeading = new Alignment(Vertical.Bottom, Horizontal.Leading);
        public static Alignment bottomTrailing = new Alignment(Vertical.Bottom, Horizontal.Trailing);
        public static Alignment bottomCenter = new Alignment(Vertical.Bottom, Horizontal.Center);
        public static Alignment bottomFill = new Alignment(Vertical.Bottom, Horizontal.Fill);

        /// Scales down a size to fit inside of a rect while maintaining the original aspect ratio.
        /// The scaled down size is then centered in the available space.
        public static Alignment aspectFit = new Alignment((CGSize size, CGRect rect) => {
            var sizeRatio = size.Width / size.Height;
            var rectRatio = rect.Size.Width / rect.Size.Height;

            CGSize scaledSize;
            if (rectRatio > sizeRatio) {
                scaledSize = new CGSize(rect.Size.Height * sizeRatio, rect.Size.Height);
            } else {
                scaledSize = new CGSize(rect.Size.Width, rect.Size.Width / sizeRatio);
            }
            return Alignment.center.Position(scaledSize, rect);
        });

        /// Alignment behavior along the vertical dimension.
        public enum Vertical
        {
        
            /// The layout is aligned to the top edge.
            Top,

            /// The layout is aligned to the bottom edge.
            Bottom,

            /// The layout is centered in the available vertical space.
            Center,

            /// The layout's height is set to be equal to the available height.
            Fill
        }

        /// Alignment behavior along the horizontal dimension.
        public enum Horizontal
        {

            /// The layout is aligned to the leading edge (left for left-to-right languages and right for right-to-left languages).
            Leading,

            /// The layout is aligned to the trailing edge (right for left-to-right languages and left for right-to-left languages).
            Trailing,

            /// The layout is centered in the available horizontal space.
            Center,

            /// The layout's width is set to be equal to the available width.
            Fill
        }

        /// A function that aligns size in rect.
        public delegate CGRect Aligner(CGSize size, CGRect rect);

        private Aligner aligner;

        public Alignment(Aligner aligner) {
            this.aligner = aligner;
        }

        public Alignment(Vertical vertical, Horizontal horizontal)
        {
            this.aligner = (CGSize size, CGRect rect) =>
            {
                OffsetAndLength h = horizontal.Align(size.Width, rect.Width, rect.X);
                OffsetAndLength v = vertical.Align(size.Height, rect.Height, rect.Y);
                return new CGRect(h.Offset, v.Offset, h.Length, v.Length);
            };
        }

        /// Positions a rect of the given size inside the given rect using the alignment spec.
        public CGRect Position(CGSize size, CGRect rect) {
            return aligner(size, rect);
        }
    }
}

