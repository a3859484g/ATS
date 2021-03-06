using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class MotionControlData
    {
        public LocateAGVPosition EncoderAGVPosition { get; set; } = null;

        public bool ErrorBit { get; set; } = false;

        public uint AllServoStatus { get; set; } = 0;
        public uint AllServoOn { get; set; } = 0b1111;
        public uint AllServoOff { get; set; } = 0b0000;

        public double X_VelocityCommand { get; set; } = 0;
        public double X_VelocityFeedback { get; set; } = 0;
        public double Y_VelocityCommand { get; set; } = 0;
        public double Y_VelocityFeedback { get; set; } = 0;
        public double Theta_VelocityCommand { get; set; } = 0;
        public double Theta_VelocityFeedback { get; set; } = 0;

        public double XVelocityError { get; set; } = 0;
        public double YVelocityError { get; set; } = 0;
        public double ThetaVelocityError { get; set; } = 0;

        public double Slam_XVelocityError { get; set; } = 0;
        public double Slam_YVelocityError { get; set; } = 0;
        public double Slam_ThetaVelocityError { get; set; } = 0;

        private Stopwatch preMoveStatusDelayTimer = new Stopwatch();
        private double preMoveStatusDelayTime = 1000;

        public EnumAxisMoveStatus moveStatus { get; set; } = EnumAxisMoveStatus.Stop;
        public EnumAxisMoveStatus MoveStatus
        {
            get
            {
                if (preMoveStatus == EnumAxisMoveStatus.None)
                    return moveStatus;
                else
                {
                    if (preMoveStatusDelayTimer.ElapsedMilliseconds > preMoveStatusDelayTime)
                    {
                        preMoveStatus = EnumAxisMoveStatus.None;
                        return moveStatus;
                    }
                    else
                        return preMoveStatus;
                }
            }

            set
            {
                if (preMoveStatus == EnumAxisMoveStatus.PreMove)
                {
                    if (value == EnumAxisMoveStatus.Move)
                        preMoveStatus = EnumAxisMoveStatus.None;
                }
                else if (preMoveStatus == EnumAxisMoveStatus.PreStop)
                {
                    if (value == EnumAxisMoveStatus.Stop)
                        preMoveStatus = EnumAxisMoveStatus.None;
                }

                moveStatus = value;
            }
        }

        private EnumAxisMoveStatus preMoveStatus = EnumAxisMoveStatus.None;
        public EnumAxisMoveStatus PreMoveStatus
        {
            set
            {
                if (value == EnumAxisMoveStatus.PreMove || value == EnumAxisMoveStatus.PreStop)
                {
                    if (preMoveStatus == EnumAxisMoveStatus.PreMove && value == EnumAxisMoveStatus.PreStop)
                        preMoveStatus = EnumAxisMoveStatus.PreStop_Force;
                    else
                        preMoveStatus = value;

                    preMoveStatusDelayTimer.Restart();
                }
            }
        }

        public double LineVelocity { get; set; } = 0;
        public double LineVelocityAngle { get; set; } = 0;
        public double LineAcc { get; set; } = 0;
        public double LineDec { get; set; } = 0;
        public double LineJerk { get; set; } = 0;

        public double ThetaVelocity { get; set; } = 0;
        public double ThetaAcc { get; set; } = 0;
        public double ThetaDec { get; set; } = 0;
        public double ThetaJerk { get; set; } = 0;

        public double SimulateLineVelocity { get; set; } = 0;
        public bool SimulateIsIsokinetic { get; set; } = true;

        public bool JoystickMode { get; set; } = false;

        public AxisData JoystickLineAxisData { get; set; } = new AxisData();
        public AxisData JoystickThetaAxisData { get; set; } = new AxisData();

        public Dictionary<EnumDefaultAxisName, AxisFeedbackData> AllAxisFeedbackData = new Dictionary<EnumDefaultAxisName, AxisFeedbackData>();
    }
}
