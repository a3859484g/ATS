using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.MiddlePackage.Umtc.Model
{
    public class MoveStatus
    {
        public EnumMoveState EnumMoveState { get; set; } = EnumMoveState.Idle;
        public int HeadAngle { get; set; } = 0;
        public int MovingDirection { get; set; } = 0;
        public MapSection LastSection { get; set; } = new MapSection();
        public MapAddress LastAddress { get; set; } = new MapAddress();
        public MapPosition LastMapPosition { get; set; } = new MapPosition();
        public int Speed { get; set; } = 0;
        public bool IsMoveEnd { get; set; } = true;
        public bool InAddress { get; set; } = false;

        public MoveStatus() { }

        public MoveStatus(MoveStatus moveStatus)
        {
            this.LastMapPosition = moveStatus.LastMapPosition;
            this.EnumMoveState = moveStatus.EnumMoveState;
            this.HeadAngle = moveStatus.HeadAngle;
            this.MovingDirection = moveStatus.MovingDirection;
            this.Speed = moveStatus.Speed;
            this.LastSection = moveStatus.LastSection;
            this.LastAddress = moveStatus.LastAddress;
            this.IsMoveEnd = moveStatus.IsMoveEnd;
            this.InAddress = moveStatus.InAddress;
        }       
    }
}
