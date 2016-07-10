using CoreGraphics;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace LayoutKit.Xamarin.Internal
{
    public static class CGSize_Extensions
    {
        public static CGSize SizeDecreasedByInsets(this CGSize size, UIEdgeInsets insets) {
            return new CGSize(size.Width - insets.Left - insets.Right, size.Height - insets.Top - insets.Bottom);
        }

        public static CGSize SizeIncreasedByInsets(this CGSize size, UIEdgeInsets insets) {
            return new CGSize(size.Width + insets.Left + insets.Right, size.Height + insets.Top + insets.Bottom);
        }

        public static CGSize SizeDecreasedToSize(this CGSize size, CGSize maxSize) {
            var width = Math.Min(size.Width, maxSize.Width);
            var height = Math.Min(size.Height, maxSize.Height);
            return new CGSize(width, height);
        }

        public static CGSize SizeIncreasedToSize(this CGSize size, CGSize minSize)  {
            var width = Math.Min(size.Width, minSize.Width);
            var height = Math.Min(size.Height, minSize.Height);
            return new CGSize(width, height);
        }
    }
}
