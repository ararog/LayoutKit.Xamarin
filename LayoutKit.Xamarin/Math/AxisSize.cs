using CoreGraphics;
using System;
namespace LayoutKit.Xamarin
{
	public struct AxisSize
	{
        public Axis axis;

        public CGSize Size { get; set; }

        public nfloat AxisLength {
            set {
                var size = Size;
                switch(axis) {
                case Axis.Horizontal:
                    size.Width = value;
                    break;
                case Axis.Vertical:
                    size.Height = value;
                    break;
                }
                Size = size;
            }
            get {
                switch(axis) {
                case Axis.Horizontal:
                    return Size.Width;
                case Axis.Vertical:
                    return Size.Height;
                default:
                    return 0;
                }
            }
        }

        public nfloat CrossLength {
            set {
                var size = Size;
                switch(axis) {
                case Axis.Horizontal:
                    size.Height = value;
                    break;
                case Axis.Vertical:
                    size.Width = value;
                    break;
                }
                Size = size;
            }
            get {
                switch(axis) {
                case Axis.Horizontal:
                    return Size.Height;
                case Axis.Vertical:
                    return Size.Width;
                default:
                    return 0;
                }
            }
        }

        public AxisSize(Axis axis, CGSize size)
        {
            this.axis = axis;
            this.Size = size;
        }

        public AxisSize(Axis axis, nfloat axisLength, nfloat crossLength)
        {
            this.axis = axis;
            switch(axis) {
            case Axis.Horizontal:
                this.Size = new CGSize(axisLength, crossLength);
                break;
            default:
                this.Size = new CGSize(crossLength, axisLength);
                break;
            }
        }
	}
}

