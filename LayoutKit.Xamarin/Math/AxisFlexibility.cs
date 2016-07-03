using System;
namespace LayoutKit.Xamarin
{
	public struct AxisFlexibility
	{
        public Axis axis;
        public Flexibility flexibility;

        public Int32? axisFlex {
            get {
                switch(axis){
                case Axis.Horizontal:
                    return flexibility.horizontal;
                case Axis.Vertical:
                    return flexibility.vertical;
                default:
                    return 0;
                }
            }
        }

        public Int32? crossFlex {
            get {
                switch(axis) {
                case Axis.Horizontal:
                    return flexibility.vertical;
                case Axis.Vertical:
                    return flexibility.horizontal;
                default:
                    return 0;
                };
            }
        }

        public AxisFlexibility(Axis axis, Flexibility flexibility)
        {
            this.axis = axis;
            this.flexibility = flexibility;
        }

        public AxisFlexibility(Axis axis, Int32? axisFlex, Int32? crossFlex)
        {
            this.axis = axis;
            switch(axis) {
            case Axis.Horizontal:
                this.flexibility = new Flexibility(axisFlex, crossFlex);
                break;
            default:
                this.flexibility = new Flexibility(crossFlex, axisFlex);
                break;
            }
        }
	}
}

