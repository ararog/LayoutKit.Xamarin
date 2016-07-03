using System;
using UIKit;

namespace LayoutKit.Xamarin
{
	public class PositioningLayout<T> where T : UIView
	{
        public delegate void ConfigDelegate(T view);

        public ConfigDelegate config;

        public PositioningLayout(ConfigDelegate config) {
            this.config = config;
        }

        public T MakeView() {
            if(config == null) {
                // Nothing needs to be configured, so this layout doesn't require a UIView.
                return null;
            }
            var view = Activator.CreateInstance<T>();
            config(view);
            return view;
        }
	}
}

