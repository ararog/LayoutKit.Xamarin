using System;
using UIKit;

namespace LayoutKit.Xamarin
{
	public class LayoutAdapterCollectionView : UICollectionView
	{
        public ReloadableViewLayoutAdapter layoutAdapter = {
            var adapter = new ReloadableViewLayoutAdapter(this);
            this.dataSource = adapter;
            this.delegate = adapter;
            return adapter;
        }();
	}
}

