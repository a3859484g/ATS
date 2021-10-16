using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.MiddlePackage.Umtc.Model
{
    public class AddressArrivalArgs : EventArgs
    {       
        public EnumMoveState EnumMoveState { get; set; } = EnumMoveState.Idle;
        public EnumAddressArrival EnumAddressArrival { get; set; } = EnumAddressArrival.Fail;
        public MapPosition MapPosition { get; set; } = new MapPosition();
        public int HeadAngle { get; set; } = 0;
        public int MovingDirection { get; set; } = 0;
        public int Speed { get; set; } = 0;
        public string MapAddressID { get; set; } = "";
        public string MapSectionID { get; set; } = "";
        public double VehicleDistanceSinceHead { get; set; } = 0;          
        public bool InAddress { get; set; } = false;

        public AddressArrivalArgs() { }

        public AddressArrivalArgs(AddressArrivalArgs addressArrivalArgs)
        {
            this.EnumMoveState = addressArrivalArgs.EnumMoveState;
            this.EnumAddressArrival = addressArrivalArgs.EnumAddressArrival;
            this.MapPosition = addressArrivalArgs.MapPosition;
            this.HeadAngle = addressArrivalArgs.HeadAngle;
            this.MovingDirection = addressArrivalArgs.MovingDirection;
            this.Speed = addressArrivalArgs.Speed;
            this.MapAddressID = addressArrivalArgs.MapAddressID;
            this.MapSectionID = addressArrivalArgs.MapSectionID;
            this.VehicleDistanceSinceHead = addressArrivalArgs.VehicleDistanceSinceHead;
            this.InAddress = addressArrivalArgs.InAddress;
        }
    }
}
