using System;
using UIKit;

namespace LayoutKit.Xamarin
{
	public class LayoutAdapterTableView : UITableView
	{
        public ReloadableViewLayoutAdapter layoutAdapter = {
            let adapter = new ReloadableViewLayoutAdapter(this);
            this.dataSource = adapter;
            this.delegate = adapter;
            return adapter;
        }();
	}
}

