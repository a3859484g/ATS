using Mirle.Agv.INX.Controller.Tools;
using Mirle.Agv.INX.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Mirle.Agv.INX.Controller
{
    public class MotionControlHandler
    {
        private LoggerAgent loggerAgent = LoggerAgent.Instance;
        private ComputeFunction computeFunction = ComputeFunction.Instance;
        private LocalData localData = LocalData.Instance;

        private MIPCControlHandler mipcControl;
        private AlarmHandler alarmHandler;
        public SimulateControl SimulateControl { get; set; }

        private string device = MethodInfo.GetCurrentMethod().ReflectedType.Name;
        private string normalLogName;

        private EnumMIPCSetPostionStep SetPositionStep = EnumMIPCSetPostionStep.Step0_WaitLocateReady;

        public bool AllServoOn
        {
            get
            {
                if (localData.SimulateMode)
                    return true;
                else
                    return localData.MoveControlData.MotionControlData.AllServoStatus == localData.MoveControlData.MotionControlData.AllServoOn;
            }
        }

        public bool AllServoOff
        {
            get
            {
                if (localData.SimulateMode)
                    return true;
                else
                    return localData.MoveControlData.MotionControlData.AllServoStatus == localData.MoveControlData.MotionControlData.AllServoOff;
            }
        }

        private AxisData move = null;
        private AxisData turn = null;
        private AxisData ems = null;
        private AxisData eq = null;
        private AxisData turn_Moving = null;

        private bool resetAlarm = false;
        private EnumControlStatus status = EnumControlStatus.Ready;
        public EnumControlStatus Status
        {
            get
            {
                if (resetAlarm)
                    return EnumControlStatus.ResetAlarm;
                else
                    if (localData.SimulateMode)
                    return EnumControlStatus.Ready;
                else
                {
                    if (SetPositionStep == EnumMIPCSetPostionStep.Ready)
                        return status;
                    else
                        return EnumControlStatus.NotReady;
                }
            }
        }

        private double spinTurnStopTheta = 0;

        public MotionControlHandler(MIPCControlHandler mipcControl, AlarmHandler alarmHandler, string normalLogName)
        {
            this.mipcControl = mipcControl;
            this.alarmHandler = alarmHandler;
            move = localData.MoveControlData.CreateMoveCommandConfig.Move;
            turn_Moving = localData.MoveControlData.CreateMoveCommandConfig.Turn_Moving;
            ems = localData.MoveControlData.CreateMoveCommandConfig.EMS;
            turn = localData.MoveControlData.CreateMoveCommandConfig.Turn;
            eq = localData.MoveControlData.CreateMoveCommandConfig.EQ;
            SimulateControl = new SimulateControl("SimulateControl");

            this.normalLogName = normalLogName;
            setPositionTimer.Restart();

            spinTurnStopTheta = computeFunction.GetAccDecDistance(turn.Velocity, 0, turn.Deceleration, turn.Jerk);
        }

        public void WriteLog(int logLevel, string carrierId, string message, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            LogFormat logFormat = new LogFormat(normalLogName, logLevel.ToString(), memberName, device, carrierId, message);

            loggerAgent.Log(logFormat.Category, logFormat);

            if (logLevel <= localData.ErrorLevel)
            {
                logFormat = new LogFormat(localData.ErrorLogName, logLevel.ToString(), memberName, device, carrierId, message);
                loggerAgent.Log(logFormat.Category, logFormat);
            }
        }
        
        public void ResetAlarm()
        {

        }

        private void UpdateMotionControlData_SimulateMode()
        {
            if (localData.MoveControlData.LocateControlData.LocateAGVPosition != null)
                localData.Real = localData.MoveControlData.LocateControlData.LocateAGVPosition.AGVPosition;
        }

        private Stopwatch setPositionTimer = new Stopwatch();

        private LocateAGVPosition stepSetPositionLocateAGVPosition = null;

        private void UpdateMotionControlData_NotSimulateMode()
        {
            switch (SetPositionStep)
            {
                case EnumMIPCSetPostionStep.Step0_WaitLocateReady:
                    stepSetPositionLocateAGVPosition = localData.MoveControlData.LocateControlData.LocateAGVPosition;
                    if (stepSetPositionLocateAGVPosition != null && stepSetPositionLocateAGVPosition.AGVPosition != null && stepSetPositionLocateAGVPosition.Type == EnumAGVPositionType.Normal)
                    {
                        SetPosition(stepSetPositionLocateAGVPosition);
                        SetPositionStep = EnumMIPCSetPostionStep.Step1_WaitMIPCDataOK;
                    }
                    else if (localData.MoveControlData.LocateControlData.SlamLocateOK)
                        mipcControl.SetPosition(null, localData.MoveControlData.LocateControlData.SlamOriginPosition, false);

                    break;
                case EnumMIPCSetPostionStep.Step1_WaitMIPCDataOK:
                    LocateAGVPosition encoder = localData.MoveControlData.MotionControlData.EncoderAGVPosition;

                    if (encoder != null && encoder.AGVPosition != null)
                    {
                        if (Math.Abs(encoder.AGVPosition.Position.X - stepSetPositionLocateAGVPosition.AGVPosition.Position.X) <= localData.MoveControlData.MoveControlConfig.InPositionRange &&
                            Math.Abs(encoder.AGVPosition.Position.Y - stepSetPositionLocateAGVPosition.AGVPosition.Position.Y) <= localData.MoveControlData.MoveControlConfig.InPositionRange &&
                            Math.Abs(computeFunction.GetCurrectAngle(encoder.AGVPosition.Angle - stepSetPositionLocateAGVPosition.AGVPosition.Angle)) <= localData.MoveControlData.MoveControlConfig.SectionAllowDeltaTheta)
                        {
                            localData.MIPCData.NeedSendHeartbeat = false;
                            SetPositionStep = EnumMIPCSetPostionStep.Ready;
                        }
                    }

                    LocateAGVPosition temp = localData.MoveControlData.LocateControlData.LocateAGVPosition;
                    if (temp != null && temp.AGVPosition != null && temp.Type == EnumAGVPositionType.Normal)
                    {
                        stepSetPositionLocateAGVPosition = temp;
                        SetPosition(temp);
                    }

                    break;
                case EnumMIPCSetPostionStep.Ready:
                    if (setPositionTimer.ElapsedMilliseconds > localData.MoveControlData.MoveControlConfig.TimeValueConfig.IntervalTimeList[EnumIntervalTimeType.SetPositionInterval])
                    {
                        if (setPositionTimer.ElapsedMilliseconds < 200)
                        {
                            localData.MoveControlData.LocateControlData.SetMIPCPositionOK = true;

                            if (localData.MoveControlData.LocateControlData.LocateAGVPosition != null)
                            {
                                setPositionTimer.Restart();
                                SetPosition(localData.MoveControlData.LocateControlData.LocateAGVPosition);
                            }
                            else
                                SetPosition(null);
                            //else
                            //    mipcControl.WriteHeartBeat();
                        }
                        else
                        {
                            setPositionTimer.Restart();
                            localData.MoveControlData.LocateControlData.SetMIPCPositionOK = false;
                        }
                    }

                    LocateAGVPosition motionLocateData = localData.MoveControlData.MotionControlData.EncoderAGVPosition;

                    if (motionLocateData != null)
                    {
                        double deltaTime = (DateTime.Now - motionLocateData.GetDataTime).TotalMilliseconds + motionLocateData.ScanTime +
                                           (localData.MoveControlData.MoveControlConfig.TimeValueConfig.IntervalTimeList[EnumIntervalTimeType.MoveControlThreadInterval]) / 2;

                        double deltaLength = deltaTime / 1000 * localData.MoveControlData.MotionControlData.LineVelocity;

                        double x = motionLocateData.AGVPosition.Position.X + Math.Cos(localData.MoveControlData.MotionControlData.LineVelocityAngle / 180 * Math.PI) * deltaLength;
                        double y = motionLocateData.AGVPosition.Position.Y + Math.Sin(localData.MoveControlData.MotionControlData.LineVelocityAngle / 180 * Math.PI) * deltaLength;
                        double angle = computeFunction.GetCurrectAngle(motionLocateData.AGVPosition.Angle + deltaTime / 1000 * localData.MoveControlData.MotionControlData.ThetaVelocity);

                        MapAGVPosition newAGVPosition = new MapAGVPosition(new MapPosition(x, y), angle);

                        localData.Real = newAGVPosition;
                    }
                    break;
                default:
                    break;
            }
        }

        public void UpdateMotionControlData()
        {
            if (localData.SimulateMode)
                UpdateMotionControlData_SimulateMode();
            else
                UpdateMotionControlData_NotSimulateMode();
        }

        #region 根源.
        virtual public bool Move(MapAGVPosition end, double lineVelocity, double lineAcc, double lineDec, double lineJerk, double thetaVelocity, double thetaAcc, double thetaDec, double thetaJerk)
        {
            if (mipcControl.AGV_Move(end, lineVelocity, lineAcc, lineDec, lineJerk, thetaVelocity, thetaAcc, thetaDec, thetaJerk))
            {
                MapAGVPosition now = localData.Real;

                double distance = 10000;

                if (now != null)
                    distance = computeFunction.GetDistanceFormTwoAGVPosition(now, end) * 2;

                if (lineVelocity != eq.Velocity)
                    SimulateControl.SetMoveCommandSimulateData(distance, lineVelocity, lineAcc, lineDec, lineJerk);
                else
                    SimulateControl.SetMoveCommandSimulateData(distance, lineVelocity, lineAcc, lineDec, lineJerk, false);

                return true;
            }
            else
                return false;
        }
        #endregion

        #region Auto.
        public bool ServoOff_All()
        {
            return mipcControl.AGV_ServoOff();
        }

        public bool ServoOn_All()
        {
            return mipcControl.AGV_ServoOn();
        }

        private MapAGVPosition GetMapAGVPositionWithThetaOffset(MapAGVPosition inputData)
        {
            MapAGVPosition outputData = new MapAGVPosition(inputData);
            outputData.Angle += cycleOffset * 360;

            if (!localData.SimulateMode)
            {
                while (Math.Abs(outputData.Angle - lastSetPositionValue_Offset.AGVPosition.Angle) > 180)
                {
                    if ((outputData.Angle - lastSetPositionValue_Offset.AGVPosition.Angle) > 180)
                        outputData.Angle -= 360;
                    else
                        outputData.Angle += 360;
                }
            }

            return outputData;
        }

        public bool Move_Line(MapAGVPosition end, double lineVelocity)
        {
            MapAGVPosition temp = GetMapAGVPositionWithThetaOffset(end);

            return Move(temp, lineVelocity, move.Acceleration, move.Deceleration, move.Jerk, turn_Moving.Velocity, turn_Moving.Acceleration, turn_Moving.Deceleration, turn_Moving.Jerk);
        }

        public bool Move_EQ(MapAGVPosition end)
        {
            MapAGVPosition temp = GetMapAGVPositionWithThetaOffset(end);

            //return Move(temp, eq.Velocity, eq.Acceleration, eq.Deceleration, eq.Jerk, turn_Moving.Velocity, turn_Moving.Acceleration, turn_Moving.Deceleration, turn_Moving.Jerk);
            //return Move(temp, eq.Velocity, eq.Acceleration, eq.Deceleration, eq.Jerk, 1, 1, 1, 1);

            switch(localData.MainFlowConfig.AGVType)
            {
                case EnumAGVType.ATS:
                    return Move(temp, 10, 10, 10, 10, 1, 1, 1, 1);

                default:
                    return Move(temp, eq.Velocity, eq.Acceleration, eq.Deceleration, eq.Jerk, 1, 1, 1, 1);
                        
            }
        }

        public bool Move_SpecialFlow(MapAGVPosition end)
        {
            MapAGVPosition temp = GetMapAGVPositionWithThetaOffset(end);

            //return Move(temp, eq.Velocity, eq.Acceleration, eq.Deceleration, eq.Jerk, turn_Moving.Velocity, turn_Moving.Acceleration, turn_Moving.Deceleration, turn_Moving.Jerk);
            return Move(temp, eq.Velocity / 2, eq.Acceleration / 2, eq.Deceleration / 2, eq.Jerk / 2, 1, 1, 1, 1);
        }

        public bool Move_VelocityChange(double newVelocity)
        {
            if (mipcControl.AGV_ChangeVelocity(newVelocity))
            {
                SimulateControl.SetVChangeSimulateData(newVelocity, move.Acceleration, move.Deceleration, move.Jerk);
                return true;
            }
            else
                return false;
        }

        public bool Turn_SpinTurn(MapAGVPosition end)
        {
            MapAGVPosition temp = GetMapAGVPositionWithThetaOffset(end);

            if (Move(temp, eq.Velocity, eq.Acceleration, eq.Deceleration, eq.Jerk, turn.Velocity, turn.Acceleration, turn.Deceleration, turn.Jerk))
                return true;
            else
                return false;
        }

        public bool Turn_SpinTurnNeedStop(MapAGVPosition end)
        {
            return Math.Abs(computeFunction.GetCurrectAngle(end.Angle - localData.Real.Angle)) <= spinTurnStopTheta;
        }

        public bool Turn_STurn(MapAGVPosition start, double moveVelocity, double moveAngle, double R, double RTheta)
        {
            //MapAGVPosition temp = new MapAGVPosition(end);

            //while (Math.Abs(temp.Angle + cycleOffset * 360) > 180)
            //{
            //    if ((temp.Angle + cycleOffset * 360) > 180)
            //        cycleOffset++;
            //    else
            //        cycleOffset--;
            //}

            return false;
        }

        public bool Turn_RTurn(MapAGVPosition start, double moveVelocity, double moveAngle, double R, double RTheta)
        {
            //MapAGVPosition temp = new MapAGVPosition(end);

            //while (Math.Abs(temp.Angle + cycleOffset * 360) > 180)
            //{
            //    if ((temp.Angle + cycleOffset * 360) > 180)
            //        cycleOffset++;
            //    else
            //        cycleOffset--;
            //}

            return false;
        }

        public bool Stop_Normal()
        {
            if (localData.MoveControlData.MoveCommand != null &&
                localData.MoveControlData.MoveCommand.MoveStatus == EnumMoveStatus.SpinTurn)
            {
                if (mipcControl.AGV_Stop(move.Deceleration, move.Jerk, turn.Deceleration, turn.Jerk))
                {
                    SimulateControl.SetVChangeSimulateData(0, move.Acceleration, move.Deceleration, move.Jerk);
                    return true;
                }
                else
                    return false;
            }
            else
            {
                if (mipcControl.AGV_Stop(move.Deceleration, move.Jerk, turn_Moving.Deceleration, turn_Moving.Jerk))
                {
                    SimulateControl.SetVChangeSimulateData(0, move.Acceleration, move.Deceleration, move.Jerk);
                    return true;
                }
                else
                    return false;
            }
        }

        public bool Stop_SpinTurn()
        {
            if (mipcControl.AGV_Stop(move.Deceleration, move.Jerk, turn.Deceleration, turn.Jerk))
            {
                SimulateControl.SetVChangeSimulateData(0, move.Acceleration, move.Deceleration, move.Jerk);
                return true;
            }
            else
                return false;
        }

        public bool Stop_EMS()
        {
            if (localData.MoveControlData.MoveCommand.MoveStatus == EnumMoveStatus.SpinTurn)
            {
                if (mipcControl.AGV_Stop(move.Deceleration, move.Jerk, turn.Deceleration, turn.Jerk))
                {
                    SimulateControl.SetVChangeSimulateData(0, move.Acceleration, move.Deceleration, move.Jerk);
                    return true;
                }
                else
                    return false;
            }
            else
            {
                if (mipcControl.AGV_Stop(ems.Deceleration, ems.Jerk, turn.Deceleration, turn.Jerk))
                {
                    SimulateControl.SetVChangeSimulateData(0, move.Acceleration, ems.Deceleration, ems.Jerk);
                    return true;
                }
                else
                    return false;
            }
        }

        public bool EMO()
        {
            return false;
        }

        private LocateAGVPosition lastSetPositionValue;
        private LocateAGVPosition lastSetPositionValue_Offset;

        private int cycleOffset = 0;

        private LocateAGVPosition CheckThetaOffset(LocateAGVPosition locateAGVPosition)
        {
            LocateAGVPosition temp = new LocateAGVPosition(locateAGVPosition);

            if (lastSetPositionValue != null)
            {
                while (Math.Abs((temp.AGVPosition.Angle + cycleOffset * 360) - lastSetPositionValue_Offset.AGVPosition.Angle) > 180)
                {
                    if (((temp.AGVPosition.Angle + cycleOffset * 360) - lastSetPositionValue_Offset.AGVPosition.Angle) > 180)
                        cycleOffset--;
                    else
                        cycleOffset++;
                }

                temp.AGVPosition.Angle += cycleOffset * 360;
            }

            return temp;
        }

        private Stopwatch setPositionIntervalTimer = new Stopwatch();

        public bool SetPosition(LocateAGVPosition locateAGVPosition)
        {
            setPositionIntervalTimer.Stop();

            if (setPositionIntervalTimer.ElapsedMilliseconds > 200)
            {
                WriteLog(1, "", String.Concat("SetPosition Interval too long : ", setPositionIntervalTimer.ElapsedMilliseconds.ToString("0")));
            }

            setPositionIntervalTimer.Restart();

            if (locateAGVPosition != null)
            {
                LocateAGVPosition setPosition = CheckThetaOffset(locateAGVPosition);
                mipcControl.SetPosition(setPosition, localData.MoveControlData.LocateControlData.SlamOriginPosition, false);
                lastSetPositionValue = locateAGVPosition;
                lastSetPositionValue_Offset = setPosition;
            }
            else
            {
                mipcControl.SetPosition(null, localData.MoveControlData.LocateControlData.SlamOriginPosition, false);
            }

            return true;
        }

        public bool SetPositionFlag { get; set; } = true;

        public bool SetPosition_NoOffset(LocateAGVPosition locateAGVPosition)
        {
            if (!mipcControl.SetPosition(locateAGVPosition, localData.MoveControlData.LocateControlData.SlamOriginPosition, true))
                return false;
            else
            {
                cycleOffset = 0;
                lastSetPositionValue = locateAGVPosition;
                return true;
            }
        }
        #endregion
    }
}
