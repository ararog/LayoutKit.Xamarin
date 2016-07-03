using CoreGraphics;
using Foundation;
using System;
namespace LayoutKit.Xamarin
{
	public interface ReloadableView
	{
        /// The bounds rectangle, which describes the view’s location and size in its own coordinate system.
        CGRect Bounds { get; }

        /// Returns whether the user has touched the content to initiate scrolling.
        bool Tracking { get; }

        /// Returns whether the content is moving in the scroll view after the user lifted their finger.
        bool Decelerating { get; }

        /**
         The axis which is scrollable.
         */
        Axis ScrollAxis();

        /**
         Reloads the data synchronously.
         This means that it must be safe to immediately call other operations such as `insert`.
         */
        void ReloadDataSync();

        /**
         Registers views for the reuse identifier.
         */
        void RegisterViews(string reuseIdentifier);

        /// Inserts sections into the reloadable view.
        void Insert(NSIndexSet sections);

        /// Inserts index paths into the reloadable view.
        void Insert(NSIndexPath[] indexPaths);
	}
}

