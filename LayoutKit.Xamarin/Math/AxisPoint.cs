using CoreGraphics;
using System;
namespace LayoutKit.Xamarin
{
	public struct AxisPoint
	{
        public Axis axis;
        public CGPoint point;

        public nfloat AxisOffset {
            set {
                switch(axis) {
                case Axis.Horizontal:
                    point.X = value;
                    break;
                case Axis.Vertical:
                    point.Y = value;
                    break;
                }
            }
            get {
                switch (axis) {
                case Axis.Horizontal:
                    return point.X;
                case Axis.Vertical:
                    return point.Y;
                default:
                    return 0;
                }
            }
        }

        public nfloat CrossOffset {
            set {
                switch(axis) {
                case Axis.Horizontal:
                    point.Y = value;
                    break;
                case Axis.Vertical:
                    point.X = value;
                    break;
                }
            }
            get {
                switch(axis) {
                case Axis.Horizontal:
                    return point.Y;
                case Axis.Vertical:
                    return point.X;
                default:
                    return 0;
                }
            }
        }

        public AxisPoint(Axis axis, CGPoint point)
        {
            this.axis = axis;
            this.point = point;
        }

        public AxisPoint(Axis axis, float axisOffset, float crossOffset)
        {
            this.axis = axis;
            switch(axis) {
            case Axis.Horizontal:
                this.point = new CGPoint(axisOffset, crossOffset);
                break;
            default:
                this.point = new CGPoint(crossOffset, axisOffset);
                break;
            }
        }	
    }
}

