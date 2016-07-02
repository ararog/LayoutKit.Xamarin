using System;
using UIKit;

namespace LayoutKit.Xamarin
{
	public class PositioningLayout
	{
        public delegate void ConfigDelegate(UIView view);

        public ConfigDelegate config;

        public PositioningLayout(ConfigDelegate config) {
            this.config = config;
        }

        public UIView MakeView() {
            if(config == null) {
                // Nothing needs to be configured, so this layout doesn't require a UIView.
                return null;
            }
            var view = new UIView();
            config(view);
            return view;
        }
	}
}

