using Foundation;
using System;
using UIKit;

namespace LayoutKit.Xamarin
{
    public class LayoutAdapterTableView : UITableView, ReloadableView
    {
        public ReloadableViewLayoutAdapter LayoutAdapter  {
            get {
                var adapter = new ReloadableViewLayoutAdapter(this);
                this.DataSource = adapter;
                this.Delegate = adapter;
                return adapter;
            }
        }

        public Axis ScrollAxis() {
            return Axis.Vertical;
        }

        public void ReloadDataSync()
        {
            ReloadData();
        }

        public void RegisterViews(string reuseIdentifier)
        {
            RegisterClassForCellReuse(typeof(UITableViewCell), reuseIdentifier);
            RegisterClassForHeaderFooterViewReuse(typeof(UITableViewHeaderFooterView), reuseIdentifier);
        }

        public void Insert(NSIndexSet sections)
        {
            InsertSections(sections, UITableViewRowAnimation.None);
        }

        public void Insert(NSIndexPath[] indexPaths)
        {
            InsertRows(indexPaths, UITableViewRowAnimation.None);
        }
    }
}

