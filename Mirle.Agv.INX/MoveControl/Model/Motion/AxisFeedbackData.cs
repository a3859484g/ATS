using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class AxisFeedbackData
    {
        public uint Count { get; set; } = 0;
        public double Velocity { get; set; } = 0;
        public double ErrorPosition { get; set; } = 0;
        public double Torque { get; set; } = 0;
        public string Mode { get; set; } = "";

        public double VelocityCommand { get; set; } = 0;
        public double VelocityFeedback { get; set; } = 0;
        public double PWM { get; set; } = 0;

        public double OriginPosition { get; set; } = 0;
        public double Position { get; set; } = 0;
        public double DA { get; set; } = 0;
        public double QA { get; set; } = 0;
        public double V { get; set; } = 0;
        public string ErrorCode { get; set; } = "";
        public double EC { get; set; } = 0;
        public double MF { get; set; } = 0;
        public double SR { get; set; } = 0;
        public double IP { get; set; } = 0;
        public double HM1 { get; set; } = 0;
        public double HM7 { get; set; } = 0;
        public double GetwayError { get; set; } = 0;
        public double Temperature { get; set; } = 0;
        public double Driver_Encoder { get; set; } = 0;
        public double Driver_RPM { get; set; } = 0;
        public float PositionCommand { get; set; } = 0;
        public EnumAxisMoveStatus AxisMoveStaus { get; set; } = EnumAxisMoveStatus.Stop;
        public EnumAxisServoOnOff AxisServoOnOff { get; set; } = EnumAxisServoOnOff.ServoOff;
        public EnumAxisStatus AxisStatus { get; set; } = EnumAxisStatus.Normal;
        public DateTime GetDataTime { get; set; } = DateTime.Now;
    }
}   

