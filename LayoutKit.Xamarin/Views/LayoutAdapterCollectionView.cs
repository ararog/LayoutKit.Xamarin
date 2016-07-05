using Foundation;
using System;
using UIKit;

namespace LayoutKit.Xamarin
{
	public class LayoutAdapterCollectionView : UICollectionView, ReloadableView
	{
        public ReloadableViewLayoutAdapter LayoutAdapter
        {
            get
            {
                var adapter = new ReloadableViewLayoutAdapter(this);
                this.DataSource = adapter;
                this.Delegate = adapter;
                return adapter;
            }
        }

        public Axis ScrollAxis()  {
            var flowLayout = CollectionViewLayout as UICollectionViewFlowLayout;
            if (flowLayout != null) {
                switch (flowLayout.ScrollDirection) {
                case UICollectionViewScrollDirection.Vertical:
                    return Axis.Vertical;
                case UICollectionViewScrollDirection.Horizontal:
                    return Axis.Horizontal;
                }
            }
            return Axis.Vertical;
        }

        public void ReloadDataSync()
        {
            ReloadData();
            LayoutIfNeeded();
        }

        public void RegisterViews(string reuseIdentifier)
        {
            RegisterClassForCell(typeof(UICollectionViewCell), reuseIdentifier);
            RegisterClassForSupplementaryView(typeof(UICollectionReusableView), UICollectionElementKindSection.Header, reuseIdentifier);
            RegisterClassForSupplementaryView(typeof(UICollectionReusableView), UICollectionElementKindSection.Footer, reuseIdentifier);
        }

        public void Insert(NSIndexPath[] indexPaths)
        {
            InsertItems(indexPaths);
        }

        public void Insert(NSIndexSet sections)
        {
            InsertSections(sections);
        }
    }
}

