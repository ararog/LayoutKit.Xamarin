using CoreGraphics;
using System;
namespace LayoutKit.Xamarin
{
	public struct AxisPoint
	{
        public Axis axis;

        public CGPoint Point { get; set; } 

        public nfloat AxisOffset {
            set {
                var point = Point;
                switch (axis) {
                case Axis.Horizontal:
                    point.X = value;
                    break;
                case Axis.Vertical:
                    point.Y = value;
                    break;
                }
                Point = point;
            }
            get {
                switch (axis) {
                case Axis.Horizontal:
                    return Point.X;
                case Axis.Vertical:
                    return Point.Y;
                default:
                    return 0;
                }
            }
        }

        public nfloat CrossOffset {
            set {
                var point = Point;
                switch(axis) {
                case Axis.Horizontal:
                    point.Y = value;
                    break;
                case Axis.Vertical:
                    point.X = value;
                    break;
                }
                Point = point;
            }
            get {
                switch(axis) {
                case Axis.Horizontal:
                    return Point.Y;
                case Axis.Vertical:
                    return Point.X;
                default:
                    return 0;
                }
            }
        }

        public AxisPoint(Axis axis, CGPoint point)
        {
            this.axis = axis;
            this.Point = point;
        }

        public AxisPoint(Axis axis, nfloat axisOffset, nfloat crossOffset)
        {
            this.axis = axis;
            switch(axis) {
            case Axis.Horizontal:
                this.Point = new CGPoint(axisOffset, crossOffset);
                break;
            default:
                this.Point = new CGPoint(crossOffset, axisOffset);
                break;
            }
        }	
    }
}

