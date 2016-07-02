using CoreGraphics;
using System;
namespace LayoutKit.Xamarin
{
	public struct AxisSize
	{
        public Axis axis;
        public CGSize size;

        public nfloat AxisLength {
            set {
                switch(axis) {
                case Axis.Horizontal:
                    size.Width = value;
                    break;
                case Axis.Vertical:
                    size.Height = value;
                    break;
                }
            }
            get {
                switch(axis) {
                case Axis.Horizontal:
                    return size.Width;
                    break;
                case Axis.Vertical:
                    return size.Height;
                    break;
                default:
                    return 0;
                }
            }
        }

        public nfloat CrossLength {
            set {
                switch(axis) {
                case Axis.Horizontal:
                    size.Height = value;
                    break;
                case Axis.Vertical:
                    size.Width = value;
                    break;
                }
            }
            get {
                switch(axis) {
                case Axis.Horizontal:
                    return size.Height;
                case Axis.Vertical:
                    return size.Width;
                default:
                    return 0;
                }
            }
        }

        public AxisSize(Axis axis, CGSize size)
        {
            this.axis = axis;
            this.size = size;
        }

        public AxisSize(Axis axis, float axisLength, float crossLength)
        {
            this.axis = axis;
            switch(axis) {
            case Axis.Horizontal:
                this.size = new CGSize(axisLength, crossLength);
                break;
            default:
                this.size = new CGSize(crossLength, axisLength);
                break;
            }
        }
	}
}

