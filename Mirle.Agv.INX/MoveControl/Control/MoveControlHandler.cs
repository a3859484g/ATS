using Mirle.Agv.INX.Controller.Tools;
using Mirle.Agv.INX.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml;

namespace Mirle.Agv.INX.Controller
{
    public class MoveControlHandler
    {
        private ComputeFunction computeFunction = ComputeFunction.Instance;
        private LoggerAgent loggerAgent = LoggerAgent.Instance;
        private LocalData localData = LocalData.Instance;
        private string device = MethodInfo.GetCurrentMethod().ReflectedType.Name;
        private string normalLogName = "MoveControl";
        public EnumControlStatus Status { get; set; } = EnumControlStatus.Ready;

        private Logger logger = LoggerAgent.Instance.GetLooger("MoveControlCSV");
        private Logger preCheckRecordLogger = LoggerAgent.Instance.GetLooger("PreCheckRecord");
        private Logger commandRecordLogger = LoggerAgent.Instance.GetLooger("CommandRecord");

        public CreateMoveControlList CreateMoveCommandList { get; set; }
        public MotionControlHandler MotionControl { get; set; }
        public LocateControlHandler LocateControl { get; set; }
        private MIPCControlHandler mipcControl;
        private AlarmHandler alarmHandler;
        private UpdateControlHandler updateControl;
        private SensorSafetyControl sensorSafetyControl;

        private Stopwatch mainThreadSleepTimer = new Stopwatch();
        private Thread thread;
        private Thread csvThread;

        private MoveCommandData preCommand = null;

        public event EventHandler<EnumMoveComplete> MoveCompleteEvent;
        public event EventHandler<string> PassAddressEvent;
        public event EventHandler CallLoadUnloadPreAction;

        private const int debugFlowLogMaxLength = 10000;
        public string DebugFlowLog { get; set; }
        public double LoopTime { get; set; } = 0;
        private MoveControlConfig config;
        private bool resetAlarm = false;
        public WallSettingControl WallSetting { get; set; } = null;
        
        private string ServoReset = "ServoReset";

        public void StartCommand()
        {
            MoveCommandData temp = localData.MoveControlData.MoveCommand;

            if (temp != null && temp.CommandStatus == EnumMoveCommandStartStatus.WaitStart)
            {
                temp.CommandStatus = EnumMoveCommandStartStatus.Start;
                ChangeBuzzerType(EnumBuzzerType.Moving);
            }
        }

        #region Read-XML.
        private void ReadTimeoutValueListXML(XmlElement element)
        {
            EnumTimeoutValueType temp;
            int value;

            foreach (XmlNode item in element.ChildNodes)
            {
                if (int.TryParse(item.InnerText, out value) && Enum.TryParse(item.Name, out temp))
                    config.TimeValueConfig.TimeoutValueList[temp] = value;
                else
                    WriteLog(3, "", String.Concat("TryPase fail, Name : ", item.Name, ", Value : ", item.InnerText));
            }
        }

        private void ReadIntervalTimeListXML(XmlElement element)
        {
            EnumIntervalTimeType temp;
            int value;

            foreach (XmlNode item in element.ChildNodes)
            {
                if (int.TryParse(item.InnerText, out value) && Enum.TryParse(item.Name, out temp))
                    config.TimeValueConfig.IntervalTimeList[temp] = value;
                else
                    WriteLog(3, "", String.Concat("TryPase fail, Name : ", item.Name, ", Value : ", item.InnerText));
            }
        }

        private void ReadDelayTimeListXML(XmlElement element)
        {
            EnumDelayTimeType temp;
            int value;

            foreach (XmlNode item in element.ChildNodes)
            {
                if (int.TryParse(item.InnerText, out value) && Enum.TryParse(item.Name, out temp))
                    config.TimeValueConfig.DelayTimeList[temp] = value;
                else
                    WriteLog(3, "", String.Concat("TryPase fail, Name : ", item.Name, ", Value : ", item.InnerText));
            }
        }

        private void ReadTimeValueConfigXML(string path)
        {
            if (path == null || path == "")
            {
                WriteLog(1, "", "TimeValueConfig.xml 路徑錯誤為null或空值,請檢查程式內部的string.");
                return;
            }

            XmlDocument doc = new XmlDocument();

            if (!File.Exists(path))
            {
                WriteLog(1, "", "找不到TimeValueConfig.xml.");
                return;
            }

            doc.Load(path);
            XmlElement rootNode = doc.DocumentElement;

            string locatePath = new DirectoryInfo(path).Parent.FullName;

            foreach (XmlNode item in rootNode.ChildNodes)
            {
                switch (item.Name)
                {
                    case "TimeoutValueList":
                        ReadTimeoutValueListXML((XmlElement)item);
                        break;
                    case "IntervalTimeList":
                        ReadIntervalTimeListXML((XmlElement)item);
                        break;
                    case "DelayTimeList":
                        ReadDelayTimeListXML((XmlElement)item);
                        break;
                    default:
                        break;
                }
            }
        }

        private void ReadMoveControlConfigXML(string path)
        {
            try
            {
                config = new MoveControlConfig();
                localData.MoveControlData.MoveControlConfig = config;

                if (path == null || path == "")
                {
                    WriteLog(1, "", "MoveControlConfig 路徑錯誤為null或空值,請檢查程式內部的string.");
                    return;
                }

                XmlDocument doc = new XmlDocument();

                if (!File.Exists(path))
                {
                    WriteLog(1, "", "找不到moveControlConfig.xml.");
                    return;
                }

                doc.Load(path);
                XmlElement rootNode = doc.DocumentElement;

                string locatePath = new DirectoryInfo(path).Parent.FullName;

                foreach (XmlNode item in rootNode.ChildNodes)
                {
                    switch (item.Name)
                    {
                        case "LosePositionSetNullAddressSection":
                            config.LosePositionSetNullAddressSection = bool.Parse(item.InnerText);
                            break;
                        case "InPositionRange":
                            config.InPositionRange = double.Parse(item.InnerText);
                            break;
                        case "SectionWidthRange":
                            config.SectionWidthRange = double.Parse(item.InnerText);
                            break;
                        case "SectionRange":
                            config.SectionRange = double.Parse(item.InnerText);
                            break;
                        case "SectionAllowDeltaTheta":
                            config.SectionAllowDeltaTheta = double.Parse(item.InnerText);
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        private SafetyData ReadSafetyDataXML(XmlElement element)
        {
            SafetyData temp = new SafetyData();

            foreach (XmlNode item in element.ChildNodes)
            {
                switch (item.Name)
                {
                    case "Enable":
                        temp.Enable = bool.Parse(item.InnerText);
                        break;
                    case "Range":
                        temp.Range = double.Parse(item.InnerText);
                        break;
                    default:
                        break;
                }
            }

            return temp;
        }

        private void ReadSafetyXML(XmlElement element)
        {
            SafetyData temp;
            EnumMoveControlSafetyType enumTemp;

            foreach (XmlNode item in element.ChildNodes)
            {
                if (Enum.TryParse(item.Name, out enumTemp))
                {
                    temp = new SafetyData();
                    temp = ReadSafetyDataXML((XmlElement)item);
                    config.Safety[enumTemp] = temp;
                }
            }
        }

        private void ReadSensorByBpassXML(XmlElement element)
        {
            EnumSensorSafetyType enumTemp;

            foreach (XmlNode item in element.ChildNodes)
            {
                if (Enum.TryParse(item.Name, out enumTemp))
                    config.SensorByPass[enumTemp] = (string.Compare(item.InnerText, "enable", true) == 0);
            }
        }

        private void ReadSensorSafetyConfigXML(string path)
        {
            try
            {
                if (path == null || path == "")
                {
                    WriteLog(1, "", "MoveControlConfig 路徑錯誤為null或空值,請檢查程式內部的string.");
                    return;
                }

                XmlDocument doc = new XmlDocument();

                if (!File.Exists(path))
                {
                    WriteLog(1, "", "找不到moveControlConfig.xml.");
                    return;
                }

                doc.Load(path);
                XmlElement rootNode = doc.DocumentElement;

                string locatePath = new DirectoryInfo(path).Parent.FullName;

                foreach (XmlNode item in rootNode.ChildNodes)
                {
                    switch (item.Name)
                    {
                        case "Safety":
                            ReadSafetyXML((XmlElement)item);
                            break;
                        case "SensorByPass":
                            ReadSensorByBpassXML((XmlElement)item);
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        private void ReadXML()
        {
            ReadMoveControlConfigXML(@"D:\MecanumConfigs\MoveControl\MoveControlConfig.xml");
            ReadSensorSafetyConfigXML(@"D:\MecanumConfigs\MoveControl\SensorSafetyConfig.xml");
            ReadTimeValueConfigXML(@"D:\MecanumConfigs\MoveControl\TimeValueConfig.xml");
        }
        #endregion

        public MoveControlHandler(MIPCControlHandler mipcControl, AlarmHandler alarmHandler, LoadUnloadControlHandler loadUnloadControl)
        {
            this.alarmHandler = alarmHandler;
            this.mipcControl = mipcControl;

            localData.MoveControlData.SimulateBypassLog = true;

            ReadXML();
            LocateControl = new LocateControlHandler(alarmHandler, "Locate", loadUnloadControl);
            updateControl = new UpdateControlHandler(alarmHandler, normalLogName);

            CreateMoveCommandList = new CreateMoveControlList(alarmHandler, normalLogName);
            MotionControl = new MotionControlHandler(mipcControl, alarmHandler, "Motion");
            CreateMoveCommandList.SimulateControl = MotionControl.SimulateControl;
            sensorSafetyControl = new SensorSafetyControl(normalLogName);

            mipcControl.SendMIPCDataByIPCTagName(new List<EnumMecanumIPCdefaultTag>() { EnumMecanumIPCdefaultTag.Slam數值補正走行關閉 }, new List<float>() { 0 });

            WallSetting = new WallSettingControl(alarmHandler, normalLogName);
            mainThreadSleepTimer.Restart();
            thread = new Thread(MoveControlThread);
            thread.Start();
            csvThread = new Thread(WriteLogCSV);
            csvThread.Start();
        }

        #region Close.
        public void CloseMoveControlHandler()
        {
            Status = EnumControlStatus.Closing;

            Stopwatch closeTimer = new Stopwatch();
            closeTimer.Restart();

            VehicleStop();

            while (localData.MoveControlData.MoveCommand != null)
            {
                if (closeTimer.ElapsedMilliseconds > config.TimeValueConfig.TimeoutValueList[EnumTimeoutValueType.EndTimeoutValue])
                {
                    // log.
                    // System EMS.
                    break;
                }

                Thread.Sleep(10);
            }

            Status = EnumControlStatus.WaitThreadStop;

            closeTimer.Restart();
            while (thread != null && thread.IsAlive)
            {
                if (closeTimer.ElapsedMilliseconds > config.TimeValueConfig.TimeoutValueList[EnumTimeoutValueType.EndTimeoutValue])
                {
                    // log.
                    // abort 
                    break;
                }

                Thread.Sleep(10);
            }

            Status = EnumControlStatus.Closed;
        }
        #endregion

        #region AlarmCode/WritleLog.
        private List<EnumMoveCommandControlErrorCode> alarmCodeClearList = new List<EnumMoveCommandControlErrorCode>()
        {
            EnumMoveCommandControlErrorCode.超過觸發區間,
            EnumMoveCommandControlErrorCode.Move_EnableTimeout,
            EnumMoveCommandControlErrorCode.SlowStop_Timeout,

            EnumMoveCommandControlErrorCode.STurn_入彎超速,
            EnumMoveCommandControlErrorCode.STurn_入彎過慢,
            EnumMoveCommandControlErrorCode.STurn_未開啟TR中停止,
            EnumMoveCommandControlErrorCode.STurn_未開啟STurn中重新啟動,
            EnumMoveCommandControlErrorCode.STurn_流程Timeout,

            EnumMoveCommandControlErrorCode.RTurn_入彎超速,
            EnumMoveCommandControlErrorCode.RTurn_入彎過慢,
            EnumMoveCommandControlErrorCode.RTurn_未開啟RTurn中停止,
            EnumMoveCommandControlErrorCode.RTurn_未開啟RTurn中重新啟動,
            EnumMoveCommandControlErrorCode.RTurn_流程Timeout,

            EnumMoveCommandControlErrorCode.SpinTurn_Timeout,

            EnumMoveCommandControlErrorCode.End_SecondCorrectionTimeout,
            EnumMoveCommandControlErrorCode.End_ServoOffTimeout,

            EnumMoveCommandControlErrorCode.MoveMethod層_DriverReturnFalse,

            EnumMoveCommandControlErrorCode.命令分解失敗,

            EnumMoveCommandControlErrorCode.拒絕移動命令_資料格式錯誤,
            EnumMoveCommandControlErrorCode.拒絕移動命令_MoveControlNotReady,
            EnumMoveCommandControlErrorCode.拒絕移動命令_MoveControlErrorBitOn,
            EnumMoveCommandControlErrorCode.拒絕移動命令_充電中,
            EnumMoveCommandControlErrorCode.拒絕移動命令_Fork不在Home點,
            EnumMoveCommandControlErrorCode.拒絕移動命令_迷航中,
            EnumMoveCommandControlErrorCode.拒絕移動命令_移動命令中,
            EnumMoveCommandControlErrorCode.拒絕移動命令_不在Section上,

            EnumMoveCommandControlErrorCode.安全保護停止_Fork不在Home點,
            EnumMoveCommandControlErrorCode.安全保護停止_充電中,
            EnumMoveCommandControlErrorCode.安全保護停止_角度偏差過大,
            EnumMoveCommandControlErrorCode.安全保護停止_軌道偏差過大,
            EnumMoveCommandControlErrorCode.安全保護停止_出彎過久沒取得定位資料,
            EnumMoveCommandControlErrorCode.安全保護停止_直線過久沒取得定位資料,
            EnumMoveCommandControlErrorCode.安全保護停止_速度變化異常,
            EnumMoveCommandControlErrorCode.安全保護停止_定位Control異常,
            EnumMoveCommandControlErrorCode.安全保護停止_人為控制,
            EnumMoveCommandControlErrorCode.安全保護停止_Bumper觸發,
            EnumMoveCommandControlErrorCode.安全保護停止_EMO停止,
            EnumMoveCommandControlErrorCode.安全保護停止_SafetySensorAlarm,
            EnumMoveCommandControlErrorCode.安全保護停止_MotionAlarm,
            EnumMoveCommandControlErrorCode.安全保護停止_走行中安全迴路異常,
            EnumMoveCommandControlErrorCode.安全保護停止_走行中定位資料過久未更新
        };

        private void AlarmCodeClear()
        {
            if (localData.AutoManual == EnumAutoState.Manual)
            {
                for (int i = 0; i < alarmCodeClearList.Count; i++)
                    ResetAlarmCode(alarmCodeClearList[i]);
            }
            else
            {
                for (int i = 0; i < alarmCodeClearList.Count; i++)
                {
                    if (alarmHandler.AlarmCodeTable.ContainsKey((int)alarmCodeClearList[i]) &&
                        alarmHandler.AlarmCodeTable[(int)alarmCodeClearList[i]].Level == EnumAlarmLevel.Alarm)
                    {
                    }
                    else
                        ResetAlarmCode(alarmCodeClearList[i]);
                }
            }
        }

        private void SetAlarmCode(EnumMoveCommandControlErrorCode alarmCode)
        {
            if (!localData.AllAlarmBit.ContainsKey((int)alarmCode))
                localData.AllAlarmBit.Add((int)alarmCode, new AlarmCodeAndSetOrReset());

            localData.AllAlarmBit[(int)alarmCode].SetAlarmOnOff(true);

            if (localData.AllAlarmBit[(int)alarmCode].Change)
            {
                WriteLog(3, "", String.Concat("[AlarmCode][Set][", ((int)alarmCode).ToString(), "] Message = ", alarmCode.ToString()));
                alarmHandler.SetAlarmCode((int)alarmCode);
            }
        }

        private void ResetAlarmCode(EnumMoveCommandControlErrorCode alarmCode)
        {
            if (!localData.AllAlarmBit.ContainsKey((int)alarmCode))
                localData.AllAlarmBit.Add((int)alarmCode, new AlarmCodeAndSetOrReset());

            localData.AllAlarmBit[(int)alarmCode].SetAlarmOnOff(false);

            if (localData.AllAlarmBit[(int)alarmCode].Change)
            {
                WriteLog(3, "", String.Concat("[AlarmCode][Reset][", ((int)alarmCode).ToString(), "] Message = ", alarmCode.ToString()));
                alarmHandler.ResetAlarmCode((int)alarmCode);
            }
        }

        private void SetDebugFlowLog(string functionName, string message)
        {
            DebugFlowLog = String.Concat(DateTime.Now.ToString("HH:mm:ss.fff"), "\t", functionName, "\t", message, "\r\n", DebugFlowLog);

            if (DebugFlowLog.Length > debugFlowLogMaxLength)
                DebugFlowLog = DebugFlowLog.Substring(0, debugFlowLogMaxLength);
        }

        public void WriteLog(int logLevel, string carrierId, string message, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            LogFormat logFormat = new LogFormat(normalLogName, logLevel.ToString(), memberName, device, carrierId, message);

            loggerAgent.Log(logFormat.Category, logFormat);
            SetDebugFlowLog(memberName, message);

            if (logLevel <= localData.ErrorLevel)
            {
                logFormat = new LogFormat(localData.ErrorLogName, logLevel.ToString(), memberName, device, carrierId, message);
                loggerAgent.Log(logFormat.Category, logFormat);
            }
        }
        #endregion

        #region ResetAlarm.
        public void ResetAlarm()
        {
            WriteLog(7, "", "收到ResetAlarm");
            resetAlarm = true;
        }

        private void CheckResetAlarm()
        {
            if (resetAlarm)
            {
                resetAlarm = false;

                AlarmCodeClear();

                if (localData.MoveControlData.MoveCommand != null && localData.MoveControlData.MoveCommand.EMSResetStatus == EnumEMSResetFlow.EMS_WaitReset)
                {
                    localData.MoveControlData.MoveCommand.EMSResetStatus = EnumEMSResetFlow.EMS_WaitStart;
                    WriteLog(7, "", String.Concat("EMSResetStatus 切換成 ", localData.MoveControlData.MoveCommand.EMSResetStatus.ToString()));
                }

                if (localData.AutoManual == EnumAutoState.Manual)
                {
                    if (localData.MoveControlData.MoveCommand == null)
                    {
                        ChangeMovingDirection(EnumMovingDirection.None);
                        ChangeBuzzerType(EnumBuzzerType.None);
                        ChangeDirectionLight(EnumDirectionLight.None);
                    }

                    MotionControl.ResetAlarm();
                    LocateControl.ResetAlarm();

                    localData.MoveControlData.ErrorBit = false;
                }

                if (localData.MoveControlData.ErrorBit)
                    SetAlarmCode(EnumMoveCommandControlErrorCode.MoveControl_ErrorBitOn);
                else
                    ResetAlarmCode(EnumMoveCommandControlErrorCode.MoveControl_ErrorBitOn);
            }

            if (localData.MoveControlData.MoveCommand != null && localData.MoveControlData.MoveCommand.EMSResetStatus == EnumEMSResetFlow.EMS_WaitStart && localData.MIPCData.Start)
            {
                localData.MoveControlData.MoveCommand.EMSResetStatus = EnumEMSResetFlow.None;
                WriteLog(7, "", String.Concat("EMSResetStatus 切換成 ", localData.MoveControlData.MoveCommand.EMSResetStatus.ToString()));
            }
        }
        #endregion

        public bool ActionCanUse(EnumUserAction action)
        {
            switch (action)
            {
                case EnumUserAction.Move_Jog:
                    if (localData.MoveControlData.MotionControlData.JoystickMode)
                        return true;
                    else
                        return localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null &&
                               localData.LoadUnloadData.LoadUnloadCommand == null && !localData.MoveControlData.SpecialFlow &&
                               (!localData.MoveControlData.MoveControlConfig.SensorByPass[EnumSensorSafetyType.ForkHomeCheck] || localData.LoadUnloadData.ForkHome) &&
                               (!localData.MoveControlData.MoveControlConfig.SensorByPass[EnumSensorSafetyType.ChargingCheck] || !localData.MIPCData.Charging);

                case EnumUserAction.Move_Jog_SettingAccDec:
                    return true;

                case EnumUserAction.Move_SpecialFlow_ReviseByTarget:
                    return localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null &&
                           localData.LoadUnloadData.LoadUnloadCommand == null && !localData.MoveControlData.SpecialFlow &&
                           !localData.MoveControlData.MotionControlData.JoystickMode &&
                           (!localData.MoveControlData.MoveControlConfig.SensorByPass[EnumSensorSafetyType.ForkHomeCheck] || localData.LoadUnloadData.ForkHome) &&
                           (!localData.MoveControlData.MoveControlConfig.SensorByPass[EnumSensorSafetyType.ChargingCheck] || !localData.MIPCData.Charging);

                case EnumUserAction.Move_SpecialFlow_ToSectionCenter:
                    return localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null &&
                           localData.LoadUnloadData.LoadUnloadCommand == null && !localData.MoveControlData.SpecialFlow &&
                           localData.MoveControlData.LocateControlData.SlamLocateOK &&
                           !localData.MoveControlData.MotionControlData.JoystickMode &&
                           (!localData.MoveControlData.MoveControlConfig.SensorByPass[EnumSensorSafetyType.ForkHomeCheck] || localData.LoadUnloadData.ForkHome) &&
                           (!localData.MoveControlData.MoveControlConfig.SensorByPass[EnumSensorSafetyType.ChargingCheck] || !localData.MIPCData.Charging);

                case EnumUserAction.Move_SpecialFlow_ReviseByTargetOrLocateData:
                    return localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null &&
                           localData.LoadUnloadData.LoadUnloadCommand == null && !localData.MoveControlData.SpecialFlow &&
                           localData.MoveControlData.LocateControlData.SlamLocateOK &&
                           !localData.MoveControlData.MotionControlData.JoystickMode &&
                           (!localData.MoveControlData.MoveControlConfig.SensorByPass[EnumSensorSafetyType.ForkHomeCheck] || localData.LoadUnloadData.ForkHome) &&
                           (!localData.MoveControlData.MoveControlConfig.SensorByPass[EnumSensorSafetyType.ChargingCheck] || !localData.MIPCData.Charging);

                case EnumUserAction.Move_LocalCommand:
                    return localData.AutoManual == EnumAutoState.Manual && !localData.MoveControlData.SpecialFlow &&
                           !localData.MoveControlData.MotionControlData.JoystickMode &&
                           (!localData.MoveControlData.MoveControlConfig.SensorByPass[EnumSensorSafetyType.ForkHomeCheck] || localData.LoadUnloadData.ForkHome);

                case EnumUserAction.Move_SetSlamAddressPosition:
                    return localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.LocateControlData.SlamLocateOK &&
                           localData.MoveControlData.MoveCommand == null;

                case EnumUserAction.Move_LocateDriver_TriggerChange:
                    return true;

                case EnumUserAction.Move_SetPosition:
                    return localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null;

                case EnumUserAction.Move_SpecialFlow_ActionBeforeAuto_MoveToAddressIfClose:
                    return localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null &&
                           localData.LoadUnloadData.LoadUnloadCommand == null && !localData.MoveControlData.SpecialFlow &&
                           !localData.MoveControlData.MotionControlData.JoystickMode && localData.LoadUnloadData.ForkHome;

                case EnumUserAction.Move_ForceSetSlamDataOK:
                    return localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null;

                default:
                    return true;
            }
        }

        #region Update MoveControl Ready & Command.
        private void UpdateCanAuto()
        {
            if (localData.AutoManual == EnumAutoState.Auto)
            {
                localData.MoveControlData.MoveControlCanAuto = true;
                localData.MoveControlData.MoveControlCantAutoReason = "";
            }
            else if (!localData.MoveControlData.Ready)
            {
                localData.MoveControlData.MoveControlCanAuto = false;
                localData.MoveControlData.MoveControlCantAutoReason = localData.MoveControlData.MoveControlNotReadyReason;
            }
            else if (localData.MoveControlData.ErrorBit)
            {
                localData.MoveControlData.MoveControlCanAuto = false;
                localData.MoveControlData.MoveControlCantAutoReason = EnumProfaceStringTag.MoveControl_ErrorBit_On.ToString();
            }
            else if (localData.Real == null)
            {
                localData.MoveControlData.MoveControlCanAuto = false;
                localData.MoveControlData.MoveControlCantAutoReason = EnumProfaceStringTag.MoveControl_迷航中_Real.ToString();
            }
            else
            {
                VehicleLocation temp = localData.Location;

                if (temp == null)
                {
                    localData.MoveControlData.MoveControlCanAuto = false;
                    localData.MoveControlData.MoveControlCantAutoReason = EnumProfaceStringTag.MoveControl_迷航中_AddressOrSection.ToString();
                }
                else if (!temp.InSection)
                {
                    localData.MoveControlData.MoveControlCanAuto = false;
                    localData.MoveControlData.MoveControlCantAutoReason = EnumProfaceStringTag.MoveControl_偏離路線.ToString();
                }
                else if (localData.TheMapInfo.AllAddress.ContainsKey(temp.LastAddress) && localData.TheMapInfo.AllSection.ContainsKey(temp.NowSection))
                {
                    if (localData.TheMapInfo.AllSection[temp.NowSection].FromVehicleAngle != localData.TheMapInfo.AllSection[temp.NowSection].ToVehicleAngle && !temp.InAddress)
                    {
                        localData.MoveControlData.MoveControlCanAuto = false;
                        localData.MoveControlData.MoveControlCantAutoReason = EnumProfaceStringTag.MoveControl_在RTurn路線上.ToString();
                    }
                    else
                    {
                        ThetaSectionDeviation revise = localData.MoveControlData.ThetaSectionDeviation;

                        if (revise == null)
                        {
                            localData.MoveControlData.MoveControlCanAuto = false;
                            localData.MoveControlData.MoveControlCantAutoReason = EnumProfaceStringTag.MoveControl_迷航中_AddressOrSection.ToString();
                        }
                        else
                        {
                            if (Math.Abs(revise.Theta) <= config.Safety[EnumMoveControlSafetyType.OntimeReviseTheta].Range / 2 &&
                                Math.Abs(revise.SectionDeviation) <= config.Safety[EnumMoveControlSafetyType.OntimeReviseSectionDeviationLine].Range / 2)
                            {
                                localData.MoveControlData.MoveControlCanAuto = true;
                                localData.MoveControlData.MoveControlCantAutoReason = "";
                            }
                            else
                            {
                                localData.MoveControlData.MoveControlCanAuto = false;
                                localData.MoveControlData.MoveControlCantAutoReason = EnumProfaceStringTag.MoveControl_偏離路線.ToString();
                            }
                        }
                    }
                }
                else
                {
                    localData.MoveControlData.MoveControlCanAuto = false;
                    localData.MoveControlData.MoveControlCantAutoReason = EnumProfaceStringTag.MoveControl_迷航中_AddressOrSection.ToString();
                }
            }
        }

        private void UpdateMoveControlReadyAndCommand(bool isMainForLoopCall)
        {
            bool resetPreCommand = false;

            if (isMainForLoopCall)
            {
                if (localData.MoveControlData.MoveCommand != null && localData.MoveControlData.MoveCommand.CommandStatus == EnumMoveCommandStartStatus.End)
                {
                    WriteLog(7, "", String.Concat("移動完成上報結束, 切回無命令狀態"));
                    localData.MoveControlData.MoveCommand = null;
                }
                else if (localData.MoveControlData.MoveCommand == null && preCommand != null)
                {
                    WriteLog(7, "", String.Concat("Command : ", preCommand.CommandID, " 切換至moveCommand"));
                    localData.MoveControlData.MoveCommand = preCommand;
                    preCommand.WaitReserveIndex = preCommand.CommandList[0].ReserveNumber;
                    preCommand.ReserveStop = !preCommand.ReserveList[preCommand.CommandList[0].ReserveNumber].GetReserve;
                    preCommand = null;
                    resetPreCommand = true;
                }

                if (localData.MoveControlData.MoveCommand != null && localData.MoveControlData.MoveCommand.CommandStatus == EnumMoveCommandStartStatus.WaitStart && localData.MoveControlData.MoveCommand.VehicleStopFlag)
                {
                    WriteLog(7, "", String.Concat("WaitStart 因VehcileStop flag, 切回無命令狀態"));
                    localData.MoveControlData.MoveCommand = null;
                }
            }

            if (localData.MoveControlData.MoveCommand != null)
            {
                localData.MoveControlData.Ready = false;
                localData.MoveControlData.MoveControlNotReadyReason = EnumProfaceStringTag.命令移動中.ToString();
            }
            else if (localData.MoveControlData.MotionControlData.JoystickMode)
            {
                localData.MoveControlData.Ready = false;
                localData.MoveControlData.MoveControlNotReadyReason = EnumProfaceStringTag.搖桿控制中.ToString();
            }
            else if (localData.MoveControlData.SensorStatus.Charging)
            {
                localData.MoveControlData.Ready = false;
                localData.MoveControlData.MoveControlNotReadyReason = EnumProfaceStringTag.充電中.ToString();
            }
            else if (!localData.MoveControlData.SensorStatus.ForkReady)
            {
                localData.MoveControlData.Ready = false;
                localData.MoveControlData.MoveControlNotReadyReason = EnumProfaceStringTag.Fork不在原點.ToString();
            }
            else if (Status != EnumControlStatus.Ready)
            {
                localData.MoveControlData.Ready = false;
                localData.MoveControlData.MoveControlNotReadyReason = EnumProfaceStringTag.程式關閉中.ToString();
            }
            else if (LocateControl.Status != EnumControlStatus.Ready)
            {
                localData.MoveControlData.Ready = false;
                localData.MoveControlData.MoveControlNotReadyReason = EnumProfaceStringTag.定位裝置Not_Ready.ToString();
            }
            else if (MotionControl.Status != EnumControlStatus.Ready)
            {
                localData.MoveControlData.Ready = false;
                localData.MoveControlData.MoveControlNotReadyReason = EnumProfaceStringTag.Motion_Not_Ready.ToString();
            }
            else
            {
                localData.MoveControlData.Ready = true;
                localData.MoveControlData.MoveControlNotReadyReason = "";
            }

            if (resetPreCommand)
                localData.MoveControlData.CreateCommanding = false;
        }
        #endregion

        private void PollingAllData(bool isMainForLoopCall)
        {
            mipcControl.MoveControlHeartBeat++;
            LocateControl.UpdateLocateControlData();
            MotionControl.UpdateMotionControlData();

            UpdateMoveControlReadyAndCommand(isMainForLoopCall);
            UpdateCanAuto();
            updateControl.UpdateAllData((localData.MoveControlData.MoveCommand == null ? null : localData.MoveControlData.MoveCommand.SectionLineList[localData.MoveControlData.MoveCommand.IndexOflisSectionLine]));
        }

        private bool ServoOffAndWait()
        {
            bool retryServoFlag = false;
            switch (localData.MainFlowConfig.AGVType)
            {
                case EnumAGVType.ATS:

                    if (!MotionControl.AllServoOff)
                    {
                        Stopwatch timer = new Stopwatch();
                        timer.Restart();

                        MotionControl.ServoOff_All();

                        while (!MotionControl.AllServoOff)
                        {
                            if (timer.ElapsedMilliseconds > 5000)
                            {
                                // mipc reset command
                                mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { ServoReset }, new List<float> { 1 });
                                WriteLog(7, "", String.Concat(" 發送ServoReset"));
                                retryServoFlag = true;
                                break;

                            }
                            ////break; //liu for pti暫改
                            //return false;

                            IntervalSleepAndPollingAllData();
                        }
                    }

                    if (retryServoFlag)
                    {
                        Stopwatch RetryTimer = new Stopwatch();
                        RetryTimer.Restart();
                        WriteLog(7, "", String.Concat("Servo : ", MotionControl.AllServoOff));
                        // Retry Servo on

                        while (RetryTimer.ElapsedMilliseconds < 12000)
                        {
                            IntervalSleepAndPollingAllData();

                        }

                        if (!MotionControl.AllServoOff)
                        {
                            MotionControl.ServoOff_All();
                            WriteLog(7, "", String.Concat(" Retry Servo OFF"));
                            RetryTimer.Restart();

                            while (!MotionControl.AllServoOff)
                            {
                                if (RetryTimer.ElapsedMilliseconds > 5000)
                                {
                                    WriteLog(7, "", String.Concat(" ServoOFF重啟失敗"));
                                    return false;

                                }
                                IntervalSleepAndPollingAllData();
                            }
                        }
                        IntervalSleepAndPollingAllData();
                        //MotionControl.ServoOn_All(); //liu for PTI暫改
                        return true;


                    }
                    else
                    {
                        WriteLog(7, "", String.Concat(" ServoOFF完成"));
                        return true;
                    }

                default:

                    if (!MotionControl.AllServoOff)
                    {
                        Stopwatch timer = new Stopwatch();
                        timer.Restart();

                        MotionControl.ServoOff_All();

                        while (!MotionControl.AllServoOff)
                        {
                            if (timer.ElapsedMilliseconds > config.TimeValueConfig.TimeoutValueList[EnumTimeoutValueType.DisableTimeoutValue])
                                //break; //liu for pti暫改
                                return false;

                            IntervalSleepAndPollingAllData();
                        }
                    }

                    return true;

            }





            //if (!MotionControl.AllServoOff)
            //{
            //    Stopwatch timer = new Stopwatch();
            //    timer.Restart();

            //    MotionControl.ServoOff_All();

            //    while (!MotionControl.AllServoOff)
            //    {
            //        if (timer.ElapsedMilliseconds > config.TimeValueConfig.TimeoutValueList[EnumTimeoutValueType.DisableTimeoutValue])
            //            //break; //liu for pti暫改
            //            return false;

            //        IntervalSleepAndPollingAllData();
            //    }
            //}

            //return true;
        }

        private bool ServoOnAndWait()
        {
            bool retryServoFlag = false;
            switch (localData.MainFlowConfig.AGVType)
            {
                case EnumAGVType.ATS:

                    if (!MotionControl.AllServoOn)
                    {
                        Stopwatch timer = new Stopwatch();
                        timer.Restart();

                        MotionControl.ServoOn_All();

                        while (!MotionControl.AllServoOn)
                        {
                            if (timer.ElapsedMilliseconds > 5000)
                            {
                                // mipc reset command
                                mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { ServoReset }, new List<float> { 1 });
                                WriteLog(7, "", String.Concat(" 發送ServoReset"));
                                retryServoFlag = true;
                                break;
                            }
                            //return false;
                            // timeout設為4000，若等不到servo on，下Servo Reset指令(motion待定)後break,重下一次ServoOn_All()，逾時才發Enable_timeout
                            IntervalSleepAndPollingAllData();
                        }
                    }
                    if (retryServoFlag)
                    {
                        Stopwatch RetryTimer = new Stopwatch();
                        RetryTimer.Restart();
                        WriteLog(7, "", String.Concat("Servo : ", MotionControl.AllServoOn));
                        // Retry Servo on

                        while (RetryTimer.ElapsedMilliseconds < 12000)
                        {
                            IntervalSleepAndPollingAllData();

                        }

                        if (!MotionControl.AllServoOn)
                        {
                            MotionControl.ServoOn_All();
                            WriteLog(7, "", String.Concat(" Retry Servo ON"));
                            RetryTimer.Restart();

                            while (!MotionControl.AllServoOn)
                            {
                                if (RetryTimer.ElapsedMilliseconds > 5000)
                                {
                                    WriteLog(7, "", String.Concat(" ServoON重啟失敗"));
                                    return false;

                                }
                                IntervalSleepAndPollingAllData();
                            }
                        }
                        IntervalSleepAndPollingAllData();
                        return true;

                    }
                    else
                    {
                        WriteLog(7, "", String.Concat(" ServoON完成"));
                        return true;
                    }

                default:
                    if (!MotionControl.AllServoOn)
                    {
                        Stopwatch timer = new Stopwatch();
                        timer.Restart();

                        MotionControl.ServoOn_All();

                        while (!MotionControl.AllServoOn)
                        {
                            if (timer.ElapsedMilliseconds > config.TimeValueConfig.TimeoutValueList[EnumTimeoutValueType.EnableTimeoutValue])
                                return false;

                            IntervalSleepAndPollingAllData();
                        }
                    }

                    //MotionControl.ServoOn_All(); //liu for PTI暫改

                    return true;

            }
            
            
            
            //if (!MotionControl.AllServoOn)
            //{
            //    Stopwatch timer = new Stopwatch();
            //    timer.Restart();

            //    MotionControl.ServoOn_All();

            //    while (!MotionControl.AllServoOn)
            //    {
            //        if (timer.ElapsedMilliseconds > config.TimeValueConfig.TimeoutValueList[EnumTimeoutValueType.EnableTimeoutValue])
            //            return false;

            //        IntervalSleepAndPollingAllData();
            //    }
            //}

            ////MotionControl.ServoOn_All(); //liu for PTI暫改

            //return true;
        }

        #region CommandControl
        private void CommandControl_Move(Command data)
        {
            EnumMoveStartType moveType = (EnumMoveStartType)data.Type;
            WriteLog(7, "", String.Concat("目前位置 : ", computeFunction.GetMapAGVPositionStringWithAngle(data.TriggerAGVPosition),
                                          ", 目標位置 : ", computeFunction.GetMapAGVPositionStringWithAngle(data.EndAGVPosition),
                                           ", vel : ", data.Velocity.ToString("0"), ", moveType : ", moveType.ToString()));

            if (data.Velocity < localData.MoveControlData.CreateMoveCommandConfig.EQ.Velocity)
                WriteLog(3, "", String.Concat("Fatal Error : Move Velcotiy < EQ.Velocity : ", data.Velocity.ToString("0.0")));

            localData.MoveControlData.MoveCommand.EndAGVPosition = data.EndAGVPosition;

            SetMovingDirectionByEndAGVPosition(data.EndAGVPosition);
            SetBuzzerTypeByEndAGVPosition(data.EndAGVPosition);
            SetDirectionLightByEndAGVPosition(data.EndAGVPosition);

            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

            timer.Restart();

            if (moveType == EnumMoveStartType.ChangeDirFlagMove)
                CommandControl_ChangeSection();

            localData.MoveControlData.MoveCommand.MoveStatus = EnumMoveStatus.Moving;

            if (!ServoOnAndWait())
            {
                EMSControl(EnumMoveCommandControlErrorCode.Move_EnableTimeout);
                return;
            }

            if (moveType == EnumMoveStartType.FirstMove)
            {
                while (timer.ElapsedMilliseconds < config.TimeValueConfig.DelayTimeList[EnumDelayTimeType.CommandStartDelayTime])
                    IntervalSleepAndPollingAllData();
            }

            localData.MoveControlData.MoveCommand.SensorStatus = sensorSafetyControl.GetSensorState();

            if (moveType != EnumMoveStartType.SensorStopMove)
                localData.MoveControlData.MoveCommand.NormalVelocity = data.Velocity;

            if (localData.MoveControlData.MoveCommand.SensorStatus == EnumVehicleSafetyAction.SlowStop)
            {
                localData.MoveControlData.MoveCommand.NowVelocity = 0;
                WriteLog(7, "", "由於啟動時 Sensor State 為SlowStop,因此不啟動!");
            }
            else if (localData.MoveControlData.MoveCommand.SensorStatus == EnumVehicleSafetyAction.EMS)
            {
                localData.MoveControlData.MoveCommand.EMSResetStatus = EnumEMSResetFlow.EMS_WaitReset;
                WriteLog(7, "", String.Concat("EMSResetStatus 切換成 ", localData.MoveControlData.MoveCommand.EMSResetStatus.ToString()));
                localData.MoveControlData.MoveCommand.NowVelocity = 0;
                WriteLog(7, "", "由於啟動時 Sensor State 為EMS,因此不啟動!");
            }
            else
            {
                double vChangeVelocity = data.Velocity;

                if (localData.MoveControlData.MoveCommand.SensorStatus == EnumVehicleSafetyAction.LowSpeed_High)
                {
                    if (vChangeVelocity > localData.MoveControlData.CreateMoveCommandConfig.LowVelocity_High)
                        vChangeVelocity = localData.MoveControlData.CreateMoveCommandConfig.LowVelocity_High;
                }
                else if (localData.MoveControlData.MoveCommand.SensorStatus == EnumVehicleSafetyAction.LowSpeed_Low)
                {
                    if (vChangeVelocity > localData.MoveControlData.CreateMoveCommandConfig.LowVelocity_High)
                        vChangeVelocity = localData.MoveControlData.CreateMoveCommandConfig.LowVelocity_High;
                }

                if (!MotionControl.Move_Line(data.EndAGVPosition, data.Velocity))
                {
                    EMSControl(EnumMoveCommandControlErrorCode.MoveMethod層_DriverReturnFalse);
                    return;
                }

                localData.MoveControlData.MoveCommand.NowVelocity = vChangeVelocity;
            }

            WriteLog(7, "", "end");
        }

        private void InsertByTriggerEncoder(Command insertCommand)
        {
            MoveCommandData moveCmd = localData.MoveControlData.MoveCommand;

            for (int i = moveCmd.IndexOfCommandList; i < moveCmd.CommandList.Count; i++)
            {
                if (moveCmd.CommandList[i].TriggerAGVPosition == null || insertCommand.TriggerEncoder <= moveCmd.CommandList[i].TriggerEncoder)
                {
                    WriteLog(7, "", String.Concat("InserCommand to list Index = ", i.ToString()));
                    moveCmd.CommandList.Insert(i, insertCommand);
                    return;
                }
            }

            WriteLog(7, "", "InserCommand to list Fatal Error");
        }

        private void CommandControl_VChange(Command data)
        {
            EnumVChangeType vChangeType = (EnumVChangeType)data.Type;
            WriteLog(7, "", String.Concat("start, Velocity : ", data.Velocity.ToString("0")));

            #region 如果降速類別為終點站或反折點,避免因為BeamSensor降速過但降速點不變造成80速度走行過長的狀況
            MoveCommandData moveCmd = localData.MoveControlData.MoveCommand;

            if ((moveCmd.SensorStatus != EnumVehicleSafetyAction.SlowStop &&
                 moveCmd.SensorStatus != EnumVehicleSafetyAction.EMS) &&
                (vChangeType == EnumVChangeType.EQ || vChangeType == EnumVChangeType.SlowStop))
            {
                if (data.NowVelocity != 0 && data.NowVelocity > moveCmd.NowVelocity)
                {
                    double oldDistance = CreateMoveCommandList.GetAccDecDistanceFormMove(data.NowVelocity, data.Velocity);
                    double newDistance = CreateMoveCommandList.GetAccDecDistanceFormMove(moveCmd.NowVelocity, data.Velocity);

                    double triggerEncoder = data.TriggerEncoder + Math.Abs(oldDistance - newDistance);
                    WriteLog(7, "", String.Concat("vChange Type : ", vChangeType.ToString(), " 時因為目前Velcotiy比預計的低,因此延後 ", Math.Abs(oldDistance - newDistance), "mm 進行降速!"));
                    Command temp = CreateMoveCommandList.NewVChangeCommand(data.TriggerAGVPosition, triggerEncoder, data.Velocity, vChangeType);
                    temp.NowVelocity = moveCmd.NowVelocity;
                    InsertByTriggerEncoder(temp);

                    moveCmd.KeepsLowSpeedStateByEQVChange = moveCmd.SensorStatus;
                    moveCmd.NormalVelocity = moveCmd.NowVelocity;
                    WriteLog(7, "", "end");
                    return;
                }
                else
                    moveCmd.KeepsLowSpeedStateByEQVChange = EnumVehicleSafetyAction.SlowStop;
            }
            #endregion

            if (vChangeType != EnumVChangeType.SensorSlow)
                localData.MoveControlData.MoveCommand.NormalVelocity = data.Velocity;

            #region 下變速指令.
            if (localData.MoveControlData.MoveCommand.SensorStatus != EnumVehicleSafetyAction.SlowStop)
            {
                double vChangeVelocity = data.Velocity;

                if (localData.MoveControlData.MoveCommand.SensorStatus == EnumVehicleSafetyAction.LowSpeed_High)
                {
                    if (vChangeVelocity > localData.MoveControlData.CreateMoveCommandConfig.LowVelocity_High)
                        vChangeVelocity = localData.MoveControlData.CreateMoveCommandConfig.LowVelocity_High;
                }
                else if (localData.MoveControlData.MoveCommand.SensorStatus == EnumVehicleSafetyAction.LowSpeed_Low)
                {
                    if (vChangeVelocity > localData.MoveControlData.CreateMoveCommandConfig.LowVelocity_Low)
                        vChangeVelocity = localData.MoveControlData.CreateMoveCommandConfig.LowVelocity_Low;
                }

                if (vChangeVelocity != localData.MoveControlData.MoveCommand.NowVelocity)
                {
                    double finalVelocity = vChangeVelocity;

                    if (vChangeVelocity > localData.MoveControlData.MoveCommand.NowVelocity &&
                        vChangeVelocity > localData.MoveControlData.CreateMoveCommandConfig.EQ.Velocity)
                        finalVelocity = CreateMoveCommandList.GetVChangeVelocity(localData.MoveControlData.MoveCommand.NowVelocity, vChangeVelocity, true);

                    if (!MotionControl.Move_VelocityChange(finalVelocity))
                    {
                        EMSControl(EnumMoveCommandControlErrorCode.MoveMethod層_DriverReturnFalse);
                        return;
                    }

                    WriteLog(7, "", String.Concat("VChange Command : ", finalVelocity.ToString("0")));
                    localData.MoveControlData.MoveCommand.NowVelocity = finalVelocity;
                }
            }
            #endregion

            #region 方向燈/BeamSensor/其他東西.
            switch (vChangeType)
            {
                case EnumVChangeType.EQ:
                    if (swtichStageNumberThread == null || !swtichStageNumberThread.IsAlive)
                    {
                        swtichStageNumberThread = new Thread(SwtichStageNumberThread);
                        swtichStageNumberThread.Start();
                    }
                    break;
                default:
                    break;
            }
            #endregion

            WriteLog(7, "", "end");
        }

        private Thread swtichStageNumberThread = null;

        private void SwtichStageNumberThread()
        {
            LocateControl.SwitchAlignmentValueAddressID(localData.MoveControlData.MoveCommand.EndAddress.Id);

            //if (localData.TheMapInfo.AllAddress.ContainsKey(localData.MoveControlData.MoveCommand.EndAddress.Id))
            //{
            //    switch (localData.TheMapInfo.AllAddress[localData.MoveControlData.MoveCommand.EndAddress.Id].ChargingDirection)
            //    {
            //        case EnumStageDirection.Left:
            //            if (localData.MIPCData.CanLeftCharging)
            //                localData.MIPCData.LeftChargingPIO.ChangeRFPIOChannelByAddressID(localData.MoveControlData.MoveCommand.EndAddress.Id);
            //            break;
            //        case EnumStageDirection.Right:
            //            if (localData.MIPCData.CanRightCharging)
            //                localData.MIPCData.RightChargingPIO.ChangeRFPIOChannelByAddressID(localData.MoveControlData.MoveCommand.EndAddress.Id);
            //            break;
            //    }
            //}
        }

        private void CommandControl_ChangeSection()
        {
            WriteLog(7, "", String.Concat("SectionLine : ",
                  localData.MoveControlData.MoveCommand.SectionLineList[localData.MoveControlData.MoveCommand.IndexOflisSectionLine].Section.Id, " cahnge to ",
                  localData.MoveControlData.MoveCommand.SectionLineList[localData.MoveControlData.MoveCommand.IndexOflisSectionLine + 1].Section.Id));

            if (localData.MoveControlData.MoveCommand.IsAutoCommand)
                PassAddressEvent?.Invoke(this, localData.MoveControlData.MoveCommand.SectionLineList[localData.MoveControlData.MoveCommand.IndexOflisSectionLine].End.Id);

            localData.MoveControlData.MoveCommand.IndexOflisSectionLine++;
        }

        private void CommandControl_Stop(Command data)
        {
            if (data.ReserveNumber >= localData.MoveControlData.MoveCommand.ReserveList.Count)
                WriteLog(3, "", "Reserve_指令動作的ReserveIndex超過ReserveList範圍");
            else if (localData.MoveControlData.MoveCommand.ReserveList[data.ReserveNumber].GetReserve)
                WriteLog(7, "", "取得下段Reserve點, 因此直接通過");
            else
            {
                localData.MoveControlData.MoveCommand.WaitReserveIndex = data.ReserveNumber;
                localData.MoveControlData.MoveCommand.ReserveStop = true;
                WriteLog(7, "", String.Concat("因未取得Reserve index = ", data.ReserveNumber.ToString(), ", 因此停車 !"));
            }
        }

        private void CommandControl_SlowStop(Command data)
        {
            EnumSlowStopType slowStopType = (EnumSlowStopType)data.Type;
            WriteLog(7, "", String.Concat("start, SensorState : ", localData.MoveControlData.MoveCommand.SensorStatus.ToString(), ", type : ", slowStopType.ToString(), ", NowVelocity : ", localData.MoveControlData.MotionControlData.LineVelocity.ToString("0")));

            MotionControl.Stop_Normal();

            #region 等待停止.
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Restart();


            if (localData.SimulateMode)
            {
                while (localData.MoveControlData.MotionControlData.LineVelocity != 0)
                {
                    if (timer.ElapsedMilliseconds > config.TimeValueConfig.TimeoutValueList[EnumTimeoutValueType.SlowStopTimeoutValue])
                    {
                        EMSControl(EnumMoveCommandControlErrorCode.SlowStop_Timeout);
                        break;
                    }

                    IntervalSleepAndPollingAllData();
                }
            }
            else
            {
                while (localData.MoveControlData.MotionControlData.MoveStatus != EnumAxisMoveStatus.Stop)
                {
                    if (timer.ElapsedMilliseconds > config.TimeValueConfig.TimeoutValueList[EnumTimeoutValueType.SlowStopTimeoutValue])
                    {
                        EMSControl(EnumMoveCommandControlErrorCode.SlowStop_Timeout);
                        return;
                    }

                    IntervalSleepAndPollingAllData();
                }
            }
            #endregion

            #region 如果是變向前的緩停, 檢查是否有超過預停位置且換SectionLine.
            switch (slowStopType)
            {
                case EnumSlowStopType.ChangeMovingAngle:
                    if (Math.Abs(data.EndEncoder - localData.MoveControlData.MoveCommand.CommandEncoder) > localData.MoveControlData.CreateMoveCommandConfig.SafteyDistance[EnumCommandType.Move] / 2)
                    {
                        EMSControl(EnumMoveCommandControlErrorCode.超過觸發區間);
                        return;
                    }

                    //CommandControl_ChangeSection();
                    break;
                default:
                case EnumSlowStopType.End:
                    break;
            }
            #endregion
            WriteLog(7, "", "end");
        }

        private void RecordPreCheckData(LocateAGVPosition originLocateAGVPosition, LocateAGVPosition alignmentAGVPosition, double spendTime)
        {
            try
            {
                if (localData.TheMapInfo.AllAddress.ContainsKey(localData.MoveControlData.MoveCommand.EndAddress.Id) &&
                    localData.TheMapInfo.AllAddress[localData.MoveControlData.MoveCommand.EndAddress.Id].LoadUnloadDirection != EnumStageDirection.None ||
                    localData.TheMapInfo.AllAddress[localData.MoveControlData.MoveCommand.EndAddress.Id].ChargingDirection != EnumStageDirection.None &&
                    HaveAlignmentLocateDriver())
                {
                    AlignmentValueData alignmentValue = localData.LoadUnloadData.AlignmentValue;

                    string preCheckCsvString = String.Concat(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                                                        ",", localData.MoveControlData.MoveCommand.EndAddress.Id,
                                                        ",", spendTime.ToString("0.0"));

                    if (originLocateAGVPosition != null)
                    {
                        preCheckCsvString = String.Concat(preCheckCsvString, ",", originLocateAGVPosition.Device.ToString());

                        if (originLocateAGVPosition.AGVPosition != null)
                            preCheckCsvString = String.Concat(preCheckCsvString, ",", originLocateAGVPosition.AGVPosition.Position.X.ToString("0.00"),
                                                                                 ",", originLocateAGVPosition.AGVPosition.Position.Y.ToString("0.00"),
                                                                                 ",", originLocateAGVPosition.AGVPosition.Angle.ToString("0.00"));
                        else
                            preCheckCsvString = String.Concat(preCheckCsvString, ",,,");
                    }
                    else
                    {
                        preCheckCsvString = String.Concat(preCheckCsvString, ",,,,");
                    }

                    if (alignmentAGVPosition != null && alignmentAGVPosition.Device == EnumLocateDriverType.AlignmentValue.ToString())
                    {
                        preCheckCsvString = String.Concat(preCheckCsvString, ",", alignmentAGVPosition.Device.ToString());

                        if (originLocateAGVPosition.AGVPosition != null)
                            preCheckCsvString = String.Concat(preCheckCsvString, ",", alignmentAGVPosition.AGVPosition.Position.X.ToString("0.00"),
                                                                                 ",", alignmentAGVPosition.AGVPosition.Position.Y.ToString("0.00"),
                                                                                 ",", alignmentAGVPosition.AGVPosition.Angle.ToString("0.00"));
                        else
                            preCheckCsvString = String.Concat(preCheckCsvString, ",,,");
                    }
                    else
                    {
                        preCheckCsvString = String.Concat(preCheckCsvString, ",,,,");
                    }

                    if (alignmentValue != null && alignmentValue.AlignmentVlaue)
                    {
                        preCheckCsvString = String.Concat(preCheckCsvString, ",OK",
                            ",", alignmentValue.P.ToString("0.00"),
                            ",", alignmentValue.Y.ToString("0.00"),
                            ",", alignmentValue.Theta.ToString("0.00"),
                            ",", alignmentValue.Z.ToString("0.00"));
                    }
                    else
                        preCheckCsvString = String.Concat(preCheckCsvString, ",NG,,,,");

                    LocateAGVPosition newLocate = new LocateAGVPosition();
                    string tagName = "";

                    LocateControl.GetFirstNotAlignmentValueData(ref newLocate, ref tagName);

                    if (newLocate != null && newLocate.AGVPosition != null)
                    {
                        preCheckCsvString = String.Concat(preCheckCsvString, ",", tagName, ",", newLocate.AGVPosition.Position.X.ToString("0.0"),
                                                                                           ",", newLocate.AGVPosition.Position.Y.ToString("0.0"),
                                                                                           ",", newLocate.AGVPosition.Angle.ToString("0.00"));
                    }
                    else
                        preCheckCsvString = String.Concat(preCheckCsvString, ",", tagName, ",,,");

                    preCheckRecordLogger.LogString(preCheckCsvString);
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        private void CommandControl_End(Command data)
        {
            WriteLog(7, "", String.Concat("start, nowEncoder : ", localData.MoveControlData.MoveCommand.CommandEncoder.ToString("0"), ", endEncoder : ", data.EndEncoder.ToString("0")));

            Stopwatch timer = new Stopwatch();

            LocateAGVPosition locateAGVPosition = localData.MoveControlData.LocateControlData.LocateAGVPosition;

            localData.MoveControlData.MoveCommand.EndAGVPosition = data.EndAGVPosition;

            #region 如果是終點有Target, 設定資料.
            if (localData.TheMapInfo.AllAddress.ContainsKey(localData.MoveControlData.MoveCommand.EndAddress.Id) &&
                localData.TheMapInfo.AllAddress[localData.MoveControlData.MoveCommand.EndAddress.Id].LoadUnloadDirection != EnumStageDirection.None ||
                localData.TheMapInfo.AllAddress[localData.MoveControlData.MoveCommand.EndAddress.Id].ChargingDirection != EnumStageDirection.None &&
                HaveAlignmentLocateDriver())
            {
                LocateControl.SetAlignmentValueAddressID(localData.MoveControlData.MoveCommand.EndAddress.Id);

                LocateAGVPosition checkAlignmentLocateOK = localData.MoveControlData.LocateControlData.LocateAGVPosition;
                timer.Restart();

                while (checkAlignmentLocateOK == null || checkAlignmentLocateOK.Device != EnumLocateDriverType.AlignmentValue.ToString())
                {
                    if (timer.ElapsedMilliseconds > 200)
                    {
                        WriteLog(5, "", "定位異常 讀不到Target數值");
                        break;
                    }

                    IntervalSleepAndPollingAllData();
                    checkAlignmentLocateOK = localData.MoveControlData.LocateControlData.LocateAGVPosition;
                }

                if (checkAlignmentLocateOK != null && checkAlignmentLocateOK.Device == EnumLocateDriverType.AlignmentValue.ToString())
                    ChangeMovingDirection(EnumMovingDirection.None);
            }
            #endregion

            LocateAGVPosition alignmentAGVPosition = localData.MoveControlData.LocateControlData.LocateAGVPosition;

            #region 二修.
            timer.Restart();

            if (!AutoMoveWithEQVelocity(data.EndAGVPosition, localData.MoveControlData.MoveControlConfig.TimeValueConfig.TimeoutValueList[EnumTimeoutValueType.EndTimeoutValue]))
                return;

            if (!AutoMoveWithEQVelocity(data.EndAGVPosition, localData.MoveControlData.MoveControlConfig.TimeValueConfig.TimeoutValueList[EnumTimeoutValueType.EndTimeoutValue]))
                return;

            double spendTime = timer.ElapsedMilliseconds;

            WriteLog(7, "", "二修完畢");
            #endregion

            if (localData.TheMapInfo.AllAddress.ContainsKey(localData.MoveControlData.MoveCommand.EndAddress.Id) &&
                localData.TheMapInfo.AllAddress[localData.MoveControlData.MoveCommand.EndAddress.Id].LoadUnloadDirection != EnumStageDirection.None)
                CallLoadUnloadPreAction?.Invoke(this, null);

            if (!ServoOffAndWait())
            {
                EMSControl(EnumMoveCommandControlErrorCode.End_ServoOffTimeout);
                return;
            }

            RecordPreCheckData(locateAGVPosition, alignmentAGVPosition, spendTime);
            localData.MoveControlData.MoveCommand.CommandStatus = EnumMoveCommandStartStatus.Reporting;
            VehicleLocation newLocation = new VehicleLocation();
            newLocation.DistanceFormSectionHead = localData.Location.DistanceFormSectionHead;
            newLocation.LastAddress = localData.MoveControlData.MoveCommand.SectionLineList[localData.MoveControlData.MoveCommand.SectionLineList.Count - 1].End.Id;
            newLocation.InAddress = true;
            newLocation.NowSection = localData.Location.NowSection;

            localData.Location = newLocation;
            ReportMoveCommandResult(EnumMoveComplete.End);
            WriteLog(7, "", "end, Move Compelete !");
        }

        private void CommandControl_SpinTurn(Command data)
        {
            WriteLog(7, "", String.Concat("SpinTurn 目標位置 : ", computeFunction.GetMapAGVPositionStringWithAngle(data.EndAGVPosition)));
            localData.MoveControlData.MoveCommand.MoveStatus = EnumMoveStatus.SpinTurn;
            DateTime startTime = DateTime.Now;

            Stopwatch timer = new Stopwatch();
            timer.Restart();

            //double turnAngle = Math.Abs(computeFunction.GetCurrectAngle(localData.Real.Angle - data.EndAGVPosition.Angle));

            EnumVehicleSafetyAction lastStatus = localData.MoveControlData.MoveCommand.SensorStatus;

            ChangeMovingDirection(EnumMovingDirection.SpinTurn);
            ChangeBuzzerType(EnumBuzzerType.SpinTurn);
            ChangeDirectionLight(EnumDirectionLight.SpinTurn);

            timer.Restart();

            if (!ServoOnAndWait())
            {
                EMSControl(EnumMoveCommandControlErrorCode.Move_EnableTimeout);
                return;
            }

            if (localData.MoveControlData.MoveCommand.IndexOfCommandList == 1)
            {
                #region 喊前進Config ms.
                while (timer.ElapsedMilliseconds < config.TimeValueConfig.DelayTimeList[EnumDelayTimeType.CommandStartDelayTime])
                    IntervalSleepAndPollingAllData();
                #endregion
            }

            timer.Restart();

            localData.MoveControlData.MoveCommand.EndAGVPosition = data.EndAGVPosition;

            int coverCount = 0;
            LocateAGVPosition nowLocate;
            MapAGVPosition now;
            Stopwatch delayNextTimeTimer = new Stopwatch();

            if (!localData.SimulateMode)
            {
                while (Math.Abs(computeFunction.GetCurrectAngle(localData.Real.Angle - data.EndAGVPosition.Angle)) > config.Safety[EnumMoveControlSafetyType.OntimeReviseTheta].Range * 0.87)
                {
                    MotionControl.Turn_SpinTurn(data.EndAGVPosition);

                    while (!(localData.MoveControlData.MotionControlData.MoveStatus == EnumAxisMoveStatus.Stop && localData.MoveControlData.MoveCommand.SensorStatus != EnumVehicleSafetyAction.SlowStop))
                    {
                        if (localData.CoverMIPCBug)
                        {
                            nowLocate = localData.MoveControlData.LocateControlData.LocateAGVPosition;
                            now = (nowLocate == null ? null : nowLocate.AGVPosition);

                            if (localData.MoveControlData.MotionControlData.MoveStatus != EnumAxisMoveStatus.Stop &&
                                localData.MoveControlData.MotionControlData.MoveStatus != EnumAxisMoveStatus.PreStop &&
                                now != null && Math.Abs(localData.MoveControlData.MotionControlData.ThetaVelocity) < 1 &&
                                computeFunction.GetTwoPositionDistance(data.EndAGVPosition, now) <
                                config.Safety[EnumMoveControlSafetyType.OntimeReviseSectionDeviationLine].Range / 2 &&
                                Math.Abs(computeFunction.GetCurrectAngle(now.Angle - data.EndAGVPosition.Angle)) < config.Safety[EnumMoveControlSafetyType.OntimeReviseTheta].Range * 0.5)
                            {
                                WriteLog(5, "", "SpinTurn Cover Send Stop");
                                MotionControl.Stop_Normal();
                            }

                            if (timer.ElapsedMilliseconds > 5000)
                            {
                                if (coverCount < 5)
                                {
                                    WriteLog(5, "", "SpinTurn Cover Send Command Again");
                                    MotionControl.Turn_SpinTurn(data.EndAGVPosition);

                                    timer.Restart();
                                    coverCount++;
                                }
                                else
                                {
                                    WriteLog(7, "", "已經Cover重複下5次還是定不了位我也沒辦法");
                                    EMSControl(EnumMoveCommandControlErrorCode.SpinTurn_Timeout);
                                    return;
                                }
                            }
                        }
                        else
                        {
                            if (timer.ElapsedMilliseconds > localData.MoveControlData.MoveControlConfig.TimeValueConfig.TimeoutValueList[EnumTimeoutValueType.SpinTurnFlowTimeoutValue])
                            {
                                EMSControl(EnumMoveCommandControlErrorCode.SpinTurn_Timeout);
                                return;
                            }
                        }

                        SensorSafety();

                        if (localData.MoveControlData.MoveCommand.MoveStatus == EnumMoveStatus.Error)
                            return;

                        if (lastStatus != localData.MoveControlData.MoveCommand.SensorStatus)
                        {
                            if (lastStatus == EnumVehicleSafetyAction.SlowStop || lastStatus == EnumVehicleSafetyAction.EMS)
                                timer.Start();
                            else if (localData.MoveControlData.MoveCommand.SensorStatus == EnumVehicleSafetyAction.SlowStop ||
                                     localData.MoveControlData.MoveCommand.SensorStatus == EnumVehicleSafetyAction.EMS)
                                timer.Stop();
                        }

                        lastStatus = localData.MoveControlData.MoveCommand.SensorStatus;

                        delayNextTimeTimer.Restart();

                        if (Math.Abs(computeFunction.GetCurrectAngle(localData.Real.Angle - data.EndAGVPosition.Angle)) <= config.Safety[EnumMoveControlSafetyType.OntimeReviseTheta].Range * 0.87)
                        {
                            while (delayNextTimeTimer.ElapsedMilliseconds < 100)
                                IntervalSleepAndPollingAllData();
                        }

                        IntervalSleepAndPollingAllData();
                    }

                    if (!localData.CoverMIPCBug)
                        break;
                }
            }
            else
            {
                while (timer.ElapsedMilliseconds < localData.MoveControlData.MoveControlConfig.TimeValueConfig.TimeoutValueList[EnumTimeoutValueType.SpinTurnFlowTimeoutValue] / 2)
                    IntervalSleepAndPollingAllData();
            }

            DateTime endTime = DateTime.Now;

            WriteLog(7, "", "end");
        }

        private void SensorStopControl(EnumVehicleSafetyAction status)
        {
            WriteLog(7, "", String.Concat("start, status : ", status.ToString()));

            localData.MoveControlData.MoveCommand.SensorStatus = status;

            if (localData.MoveControlData.MoveCommand.MoveStatus == EnumMoveStatus.Stop)
            {
                WriteLog(3, "", String.Concat("狀態已經是Stop, 流程不應該進到這邊."));
            }
            else
            {
                if (status == EnumVehicleSafetyAction.EMS)
                {
                    if (!MotionControl.Stop_EMS())
                        EMSControl(EnumMoveCommandControlErrorCode.MoveMethod層_DriverReturnFalse);
                    else
                    {
                        localData.MoveControlData.MoveCommand.EMSResetStatus = EnumEMSResetFlow.EMS_Stopping;
                        WriteLog(7, "", String.Concat("EMSResetStatus 切換成 ", localData.MoveControlData.MoveCommand.EMSResetStatus.ToString()));
                    }
                }
                else
                {
                    if (localData.MoveControlData.MotionControlData.MoveStatus == EnumAxisMoveStatus.Move ||
                        localData.MoveControlData.MotionControlData.MoveStatus == EnumAxisMoveStatus.PreMove)
                    {
                        if (!MotionControl.Stop_Normal())
                            EMSControl(EnumMoveCommandControlErrorCode.MoveMethod層_DriverReturnFalse);
                    }
                }
            }

            WriteLog(7, "", "end");
        }

        private void SendEMO()
        {

            MotionControl.EMO();
        }

        private void EMSControl(EnumMoveCommandControlErrorCode errorCode)
        {
            WriteLog(7, "", "start, EMS Stop : " + errorCode.ToString());
            localData.MoveControlData.MoveCommand.ErrorReason = errorCode;

            localData.MoveControlData.MoveCommand.MoveStatus = EnumMoveStatus.Error;

            double startStopEncoder = localData.MoveControlData.MoveCommand.CommandEncoder;

            if (!MotionControl.Stop_EMS())
                SendEMO();

            while (localData.MoveControlData.MotionControlData.MoveStatus != EnumAxisMoveStatus.Stop &&
                   Math.Abs(localData.MoveControlData.MotionControlData.SimulateLineVelocity -
                            localData.MoveControlData.MotionControlData.LineVelocity) <= config.Safety[EnumMoveControlSafetyType.VChangeSafetyDistance].Range)
                IntervalSleepAndPollingAllData();

            if (localData.MoveControlData.MotionControlData.MoveStatus != EnumAxisMoveStatus.Stop)
            {
                SendEMO();
            }
            else
            {
                if (!ServoOffAndWait())
                {
                    SetAlarmCode(EnumMoveCommandControlErrorCode.End_ServoOffTimeout);
                    WriteLog(3, "", "EMS ServoOff timeout 斷電");
                    mipcControl.SetMIPCReady(false);
                }
            }

            SetAlarmCode(errorCode);

            if (alarmHandler.AlarmCodeTable.ContainsKey((int)(errorCode)) && alarmHandler.AlarmCodeTable[(int)(errorCode)].Level == EnumAlarmLevel.Alarm)
                localData.MoveControlData.ErrorBit = true;

            localData.MoveControlData.MoveCommand.CommandStatus = EnumMoveCommandStartStatus.Reporting;
            ReportMoveCommandResult(EnumMoveComplete.Error);
            WriteLog(7, "", "end");
        }
        #endregion

        #region 上報.
        private void ReportMoveCommandResult(EnumMoveComplete status)
        {
            ChangeMovingDirection(EnumMovingDirection.None);
            ChangeBuzzerType(EnumBuzzerType.None);
            ChangeDirectionLight(EnumDirectionLight.None);
            LocateControl.SetAlignmentValueOff();

            localData.MoveControlData.AddMoveCommandRecordList(localData.MoveControlData.MoveCommand, status);

            MoveCommandData mCMD = localData.MoveControlData.MoveCommand;
            //Time(年月日),CommandType,CommandID,StartTime,EndTime,DeltaTime,StartSOC,StartV,EndSOC,EndV,Result,ErrorCode,是否Alarm,AutoManual
            commandRecordLogger.LogString(String.Concat(DateTime.Now.ToString("yyyy/MM/dd"), ",", "MoveCommand", ",",
                                                        mCMD.CommandID, ",", mCMD.StartTime.ToString("HH:mm:ss"), ",", DateTime.Now.ToString("HH:mm:ss"), ",",
                                                        (DateTime.Now - mCMD.StartTime).TotalSeconds.ToString("0.00"), "s,",
                                                        mCMD.StartSOC.ToString("0"), ",", localData.BatteryInfo.Battery_SOC.ToString("0"), ",",
                                                        mCMD.StartVoltage.ToString("0.0"), ",", localData.BatteryInfo.Battery_V.ToString("0.0"), ",",
                                                        status.ToString(), ",",
                                                        (mCMD.ErrorReason == EnumMoveCommandControlErrorCode.None ? "" : ((int)mCMD.ErrorReason).ToString("0")), ",",
                                                        (status == EnumMoveComplete.Error ? "Error" : "Normal"), ",", mCMD.AutoManual.ToString()));

            if (localData.MoveControlData.MoveCommand.IsAutoCommand)
            {
                // agvc的命令會由此Event等待MainFlow上報Middler結束後切成 End狀態(同下).
                MoveCompleteEvent?.Invoke(this, status);

                while (localData.MoveControlData.MoveCommand.CommandStatus == EnumMoveCommandStartStatus.Reporting)
                {
                    IntervalSleepAndPollingAllData();
                }
            }
            else
            {
                localData.MoveControlData.MoveCommand.CommandStatus = EnumMoveCommandStartStatus.End;
            }
        }
        #endregion

        #region AutoMove:EQ.
        public bool AutoMoveWithEQVelocity(MapAGVPosition end, double timeoutValue)
        {
            if (localData.SimulateMode)
                return true;

            localData.MoveControlData.MoveCommand.NormalVelocity = localData.MoveControlData.CreateMoveCommandConfig.EQ.Velocity;
            localData.MoveControlData.MoveCommand.NowVelocity = localData.MoveControlData.CreateMoveCommandConfig.EQ.Velocity;

            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

            if (!ServoOnAndWait())
            {
                EMSControl(EnumMoveCommandControlErrorCode.Move_EnableTimeout);
                return false;
            }

            localData.MoveControlData.MoveCommand.EndAGVPosition = end;
            MotionControl.Move_EQ(end);

            timer.Restart();

            EnumVehicleSafetyAction lastStatus = localData.MoveControlData.MoveCommand.SensorStatus;

            while (!(localData.MoveControlData.MotionControlData.MoveStatus == EnumAxisMoveStatus.Stop && localData.MoveControlData.MoveCommand.SensorStatus != EnumVehicleSafetyAction.SlowStop))
            {
                if (timer.ElapsedMilliseconds > timeoutValue)
                {
                    timer.Restart();
                    SetAlarmCode(EnumMoveCommandControlErrorCode.End_SecondCorrectionTimeout);

                    if (!MotionControl.Stop_Normal())
                    {
                        EMSControl(EnumMoveCommandControlErrorCode.MoveMethod層_DriverReturnFalse);
                        return false;
                    }
                }

                SensorSafety_EQVelocity();

                if (localData.MoveControlData.MoveCommand.MoveStatus == EnumMoveStatus.Error)
                    return false;

                if (lastStatus != localData.MoveControlData.MoveCommand.SensorStatus)
                {
                    if (lastStatus == EnumVehicleSafetyAction.SlowStop)
                        timer.Start();
                    else if (localData.MoveControlData.MoveCommand.SensorStatus == EnumVehicleSafetyAction.SlowStop)
                        timer.Stop();
                }

                lastStatus = localData.MoveControlData.MoveCommand.SensorStatus;
                IntervalSleepAndPollingAllData();
            }

            return true;
        }
        #endregion

        #region 檢查觸發.
        private bool TriggerCommand(Command cmd)
        {
            if (localData.MoveControlData.MoveCommand.SensorStatus == EnumVehicleSafetyAction.SlowStop &&
                (cmd.CmdType == EnumCommandType.Move || cmd.CmdType == EnumCommandType.End || cmd.CmdType == EnumCommandType.SpinTurn))
            {
                return false;
            }

            if (cmd.TriggerAGVPosition == null)
            {
                if ((cmd.CmdType == EnumCommandType.Move || cmd.CmdType == EnumCommandType.SpinTurn) && cmd.ReserveNumber != -1 &&
                    !localData.MoveControlData.MoveCommand.ReserveList[cmd.ReserveNumber].GetReserve)
                {
                    localData.MoveControlData.MoveCommand.WaitReserveIndex = cmd.ReserveNumber;
                    localData.MoveControlData.MoveCommand.ReserveStop = true;
                    return false;
                }
                else
                {
                    WriteLog(7, "", String.Concat("Command : " + cmd.CmdType.ToString() + ", 觸發,為立即觸發"));
                    return true;
                }
            }
            else
            {
                if (localData.MoveControlData.MoveCommand.CommandEncoder > cmd.TriggerEncoder + cmd.SafetyDistance)
                {
                    WriteLog(3, "", (String.Concat("Command : ", cmd.CmdType.ToString(), ", 超過Triiger觸發區間,EMS.. ",
                                ", Encoder : ", localData.MoveControlData.MoveCommand.CommandEncoder.ToString("0.0"),
                                ", triggerEncoder : ", cmd.TriggerEncoder.ToString("0.0"))));
                    EMSControl(EnumMoveCommandControlErrorCode.超過觸發區間);
                    return false;
                }
                else if (localData.MoveControlData.MoveCommand.CommandEncoder > cmd.TriggerEncoder)
                {
                    if ((cmd.CmdType == EnumCommandType.Move || cmd.CmdType == EnumCommandType.SpinTurn) && cmd.ReserveNumber != -1 &&
                        !localData.MoveControlData.MoveCommand.ReserveList[cmd.ReserveNumber].GetReserve)
                    {
                        localData.MoveControlData.MoveCommand.WaitReserveIndex = cmd.ReserveNumber;
                        localData.MoveControlData.MoveCommand.ReserveStop = true;
                        return false;
                    }
                    else
                    {
                        WriteLog(7, "", String.Concat("Command : ", cmd.CmdType.ToString(), ", 觸發, Encoder : ", localData.MoveControlData.MoveCommand.CommandEncoder.ToString("0.0"),
                                  ", triggerEncoder : ", cmd.TriggerEncoder.ToString("0.0")));
                        return true;
                    }
                }
            }

            return false;
        }
        #endregion

        private void ExecuteCommandList()
        {
            if (localData.MoveControlData.MoveCommand.IndexOfCommandList >= localData.MoveControlData.MoveCommand.CommandList.Count)
            {
                WriteLog(3, "", "IndexOfCommandList == CommandList.Count, 不該觸發");
            }
            else if (TriggerCommand(localData.MoveControlData.MoveCommand.CommandList[localData.MoveControlData.MoveCommand.IndexOfCommandList]))
            {
                WriteLog(7, "", String.Concat("real : ", computeFunction.GetMapAGVPositionStringWithAngle(localData.Real),
                                              ",LocateAGVPosition : ", computeFunction.GetLocateAGVPositionStringWithAngle(localData.MoveControlData.LocateControlData.LocateAGVPosition)));

                localData.MoveControlData.MoveCommand.IndexOfCommandList++;

                switch (localData.MoveControlData.MoveCommand.CommandList[localData.MoveControlData.MoveCommand.IndexOfCommandList - 1].CmdType)
                {
                    case EnumCommandType.Move:
                        CommandControl_Move(localData.MoveControlData.MoveCommand.CommandList[localData.MoveControlData.MoveCommand.IndexOfCommandList - 1]);
                        break;
                    case EnumCommandType.Vchange:
                        CommandControl_VChange(localData.MoveControlData.MoveCommand.CommandList[localData.MoveControlData.MoveCommand.IndexOfCommandList - 1]);
                        break;
                    case EnumCommandType.ChangeSection:
                        CommandControl_ChangeSection();
                        break;
                    case EnumCommandType.SpinTurn:
                        CommandControl_SpinTurn(localData.MoveControlData.MoveCommand.CommandList[localData.MoveControlData.MoveCommand.IndexOfCommandList - 1]);
                        break;
                    case EnumCommandType.Stop:
                        CommandControl_Stop(localData.MoveControlData.MoveCommand.CommandList[localData.MoveControlData.MoveCommand.IndexOfCommandList - 1]);
                        break;
                    case EnumCommandType.SlowStop:
                        CommandControl_SlowStop(localData.MoveControlData.MoveCommand.CommandList[localData.MoveControlData.MoveCommand.IndexOfCommandList - 1]);
                        break;
                    case EnumCommandType.End:
                        CommandControl_End(localData.MoveControlData.MoveCommand.CommandList[localData.MoveControlData.MoveCommand.IndexOfCommandList - 1]);
                        break;
                    default:
                        break;
                }
            }
        }

        private void IntervalSleepAndPollingAllData(bool isMainForLoopCall = false)
        {
            CheckResetAlarm();

            if (mainThreadSleepTimer.ElapsedMilliseconds > 2 * config.TimeValueConfig.IntervalTimeList[EnumIntervalTimeType.MoveControlThreadInterval])
            {
                WriteLog(5, "", String.Concat("Sleep Lag : ", mainThreadSleepTimer.ElapsedMilliseconds.ToString()));
            }

            while (mainThreadSleepTimer.ElapsedMilliseconds < config.TimeValueConfig.IntervalTimeList[EnumIntervalTimeType.MoveControlThreadInterval])
                Thread.Sleep(1);

            LoopTime = mainThreadSleepTimer.ElapsedMilliseconds;

            if (localData.SimulateMode && localData.MoveControlData.SimulateBypassLog)
            {
                if (LoopTime > config.TimeValueConfig.IntervalTimeList[EnumIntervalTimeType.MoveControlThreadInterval] * 2)
                    WriteLog(3, "", String.Concat("PollingAddData interval : ", LoopTime.ToString("0")));
            }

            mainThreadSleepTimer.Restart();
            PollingAllData(isMainForLoopCall);
        }

        private void MoveControlThread()
        {
            try
            {
                while (Status != EnumControlStatus.WaitThreadStop)
                {
                    if (localData.MoveControlData.MoveCommand != null)
                    {
                        if (localData.MoveControlData.MoveCommand.CommandStatus == EnumMoveCommandStartStatus.Start)
                        {
                            ExecuteCommandList();
                            SensorSafety();

                            if (localData.MoveControlData.MoveCommand.Cancel && localData.MoveControlData.MotionControlData.MoveStatus == EnumAxisMoveStatus.Stop)
                                ReportMoveCommandResult(EnumMoveComplete.Cancel);
                        }

                        if (localData.MoveControlData.MoveCommand.EMSResetStatus == EnumEMSResetFlow.EMS_Stopping && localData.MoveControlData.MotionControlData.MoveStatus == EnumAxisMoveStatus.Stop)
                        {
                            localData.MoveControlData.MoveCommand.EMSResetStatus = EnumEMSResetFlow.EMS_WaitReset;
                            WriteLog(7, "", String.Concat("EMSResetStatus 切換成 ", localData.MoveControlData.MoveCommand.EMSResetStatus.ToString()));
                        }
                    }

                    IntervalSleepAndPollingAllData(true);
                }
            }
            catch (Exception ex)
            {
                SetAlarmCode(EnumMoveCommandControlErrorCode.MoveControl主Thread跳Exception);
                WriteLog(1, "", String.Concat("Exception : ", ex.ToString()));
                localData.MoveControlData.MoveCommand = null;
            }
        }

        #region Safety.
        private bool servoOffDelaing = false;
        private Stopwatch servoOffTimer = new Stopwatch();

        private void CheckServoOff()
        {
            bool stopFlag = localData.MoveControlData.MoveCommand != null &&
                            (localData.MoveControlData.MoveCommand.SensorStatus == EnumVehicleSafetyAction.SlowStop ||
                             localData.MoveControlData.MoveCommand.SensorStatus == EnumVehicleSafetyAction.EMS) &&
                            localData.MoveControlData.MotionControlData.MoveStatus == EnumAxisMoveStatus.Stop;

            if (stopFlag)
            {
                if (servoOffDelaing)
                {
                    if (servoOffTimer.ElapsedMilliseconds > 3000 && !MotionControl.AllServoOff)
                    {
                        if (!ServoOffAndWait())
                            WriteLog(5, "", "停止ServoOff Failed");
                    }
                }
                else
                {
                    servoOffDelaing = true;
                    servoOffTimer.Restart();
                }
            }
            else
            {
                servoOffDelaing = false;
                servoOffTimer.Stop();
            }
        }

        private void SensorSafety()
        {
            EnumMoveCommandControlErrorCode errorCode = sensorSafetyControl.SensorSafety(MotionControl.Status, LocateControl.Status);
            CheckServoOff();

            if (errorCode != EnumMoveCommandControlErrorCode.None)
                EMSControl(errorCode);
            else
                SensorAction(sensorSafetyControl.UpdateSensorState());
        }

        private void SensorSafety_EQVelocity()
        {
            EnumMoveCommandControlErrorCode errorCode = sensorSafetyControl.SensorSafety(MotionControl.Status, LocateControl.Status);
            CheckServoOff();

            if (errorCode != EnumMoveCommandControlErrorCode.None)
                EMSControl(errorCode);
            else
                SensorAction_EQ(sensorSafetyControl.UpdateSensorState());
        }

        private void SensorAction(EnumVehicleSafetyAction newSensorStatus)
        {
            if (newSensorStatus == localData.MoveControlData.MoveCommand.SensorStatus)
                return;
            else
                WriteLog(7, "", String.Concat("SensorStatus 從 ", localData.MoveControlData.MoveCommand.SensorStatus.ToString(), " 變更為 ", newSensorStatus.ToString()));

            IntervalSleepAndPollingAllData();

            switch (newSensorStatus)
            {
                case EnumVehicleSafetyAction.Normal:
                    SensorActionToNormal();
                    break;
                case EnumVehicleSafetyAction.LowSpeed_Low:
                case EnumVehicleSafetyAction.LowSpeed_High:
                    SensorActionToSlow(newSensorStatus);
                    break;
                case EnumVehicleSafetyAction.SlowStop:
                case EnumVehicleSafetyAction.EMS:
                    SensorStopControl(newSensorStatus);
                    break;
                default:
                    break;
            }
        }

        private void SensorAction_EQ(EnumVehicleSafetyAction newSensorStatus)
        {
            if (newSensorStatus == localData.MoveControlData.MoveCommand.SensorStatus)
                return;
            else
                WriteLog(7, "", String.Concat("SensorStatus 從 ", localData.MoveControlData.MoveCommand.SensorStatus.ToString(), " 變更為 ", newSensorStatus.ToString()));

            IntervalSleepAndPollingAllData();

            if ((localData.MoveControlData.MoveCommand.SensorStatus == EnumVehicleSafetyAction.SlowStop ||
                 localData.MoveControlData.MoveCommand.SensorStatus == EnumVehicleSafetyAction.EMS) &&
                (newSensorStatus != EnumVehicleSafetyAction.SlowStop &&
                 newSensorStatus != EnumVehicleSafetyAction.EMS))
            {   // 停止到啟動.
                if (!ServoOnAndWait())
                {
                    EMSControl(EnumMoveCommandControlErrorCode.Move_EnableTimeout);
                    return;
                }

                MotionControl.Move_EQ(localData.MoveControlData.MoveCommand.EndAGVPosition);

                localData.MoveControlData.MoveCommand.SensorStatus = newSensorStatus;
            }
            else if ((localData.MoveControlData.MoveCommand.SensorStatus != EnumVehicleSafetyAction.SlowStop &&
                      localData.MoveControlData.MoveCommand.SensorStatus != EnumVehicleSafetyAction.EMS) &&
                     (newSensorStatus == EnumVehicleSafetyAction.SlowStop ||
                      newSensorStatus == EnumVehicleSafetyAction.EMS))
            {   // 停止.
                SensorStopControl(newSensorStatus);
            }

            localData.MoveControlData.MoveCommand.SensorStatus = newSensorStatus;
        }

        private void SensorActionToNormal()
        {
            switch (localData.MoveControlData.MoveCommand.SensorStatus)
            {
                case EnumVehicleSafetyAction.LowSpeed_High:
                    localData.MoveControlData.MoveCommand.SensorStatus = EnumVehicleSafetyAction.Normal;
                    if (localData.MoveControlData.MoveCommand.NormalVelocity > localData.MoveControlData.CreateMoveCommandConfig.LowVelocity_High)
                    {
                        WriteLog(7, "", String.Concat("Sensor切換至Normal,插入升速, velocityCommand : ", localData.MoveControlData.MoveCommand.NormalVelocity.ToString("0")));
                        CommandControl_VChange(CreateMoveCommandList.NewVChangeCommand(null, 0, localData.MoveControlData.MoveCommand.NormalVelocity));
                    }

                    break;
                case EnumVehicleSafetyAction.LowSpeed_Low:
                    localData.MoveControlData.MoveCommand.SensorStatus = EnumVehicleSafetyAction.Normal;
                    if (localData.MoveControlData.MoveCommand.NormalVelocity > localData.MoveControlData.CreateMoveCommandConfig.LowVelocity_Low)
                    {
                        WriteLog(7, "", String.Concat("Sensor切換至Normal,插入升速, velocityCommand : ", localData.MoveControlData.MoveCommand.NormalVelocity.ToString("0")));
                        CommandControl_VChange(CreateMoveCommandList.NewVChangeCommand(null, 0, localData.MoveControlData.MoveCommand.NormalVelocity));
                    }

                    break;
                case EnumVehicleSafetyAction.SlowStop:
                case EnumVehicleSafetyAction.EMS:
                    localData.MoveControlData.MoveCommand.SensorStatus = EnumVehicleSafetyAction.Normal;
                    // 加入啟動.
                    if (localData.MoveControlData.MoveCommand.MoveStatus == EnumMoveStatus.STurn)
                        SensorStartMoveSTurnAction();
                    else if (localData.MoveControlData.MoveCommand.MoveStatus == EnumMoveStatus.RTurn)
                        SensorStartMoveRTurnAction();
                    else if (localData.MoveControlData.MoveCommand.MoveStatus == EnumMoveStatus.SpinTurn)
                        SensorStartMoveSpinTurnAction();
                    else
                        SensorStartMove(EnumVehicleSafetyAction.Normal);
                    break;
                default:
                    break;
            }
        }

        private void SensorActionToSlow(EnumVehicleSafetyAction action)
        {
            switch (localData.MoveControlData.MoveCommand.SensorStatus)
            {
                case EnumVehicleSafetyAction.Normal:
                    localData.MoveControlData.MoveCommand.SensorStatus = action;
                    // 加入降速.
                    if (localData.MoveControlData.MoveCommand.NowVelocity > localData.MoveControlData.CreateMoveCommandConfig.LowVelocity_Low)
                    {
                        WriteLog(7, "", String.Concat("Sensor切換至LowSpeed, 降速至", localData.MoveControlData.CreateMoveCommandConfig.LowVelocity_Low.ToString("0")));
                        CommandControl_VChange(CreateMoveCommandList.NewVChangeCommand(null, 0, localData.MoveControlData.CreateMoveCommandConfig.LowVelocity_Low, EnumVChangeType.SensorSlow));
                    }
                    else
                        WriteLog(7, "", String.Concat("Sensor切換至LowSpeed,但目前速度小於等於", localData.MoveControlData.CreateMoveCommandConfig.LowVelocity_Low.ToString("0"), ", 不做降速"));

                    break;
                case EnumVehicleSafetyAction.SlowStop:
                    localData.MoveControlData.MoveCommand.SensorStatus = action;
                    // 加入啟動且降速.
                    if (localData.MoveControlData.MoveCommand.MoveStatus == EnumMoveStatus.STurn)
                        SensorStartMoveSTurnAction();
                    else if (localData.MoveControlData.MoveCommand.MoveStatus == EnumMoveStatus.RTurn)
                        SensorStartMoveRTurnAction();
                    else if (localData.MoveControlData.MoveCommand.MoveStatus == EnumMoveStatus.SpinTurn)
                        SensorStartMoveSpinTurnAction();
                    else
                        SensorStartMove(action);
                    break;
                default:
                    break;
            }
        }

        #region SensorStart.
        private void SensorStartMoveSTurnAction()
        {
            EMSControl(EnumMoveCommandControlErrorCode.SlowStop_Timeout);
        }

        private void SensorStartMoveRTurnAction()
        {
            EMSControl(EnumMoveCommandControlErrorCode.SlowStop_Timeout);
        }

        private void SensorStartMoveSpinTurnAction()
        {
            if (!ServoOnAndWait())
            {
                EMSControl(EnumMoveCommandControlErrorCode.Move_EnableTimeout);
                return;
            }
            MotionControl.Turn_SpinTurn(localData.MoveControlData.MoveCommand.EndAGVPosition);
        }

        private bool NextCommandIsMoveOrSpinTurn(EnumCommandType type, ref int index)
        {
            for (int i = localData.MoveControlData.MoveCommand.IndexOfCommandList; i < localData.MoveControlData.MoveCommand.CommandList.Count; i++)
            {
                if (localData.MoveControlData.MoveCommand.CommandList[i].CmdType == type)
                {
                    index = i;
                    return true;
                }
                else if (localData.MoveControlData.MoveCommand.CommandList[i].CmdType == EnumCommandType.Move ||
                         localData.MoveControlData.MoveCommand.CommandList[i].CmdType == EnumCommandType.SpinTurn)
                    return false;
                else if (localData.MoveControlData.MoveCommand.CommandList[i].TriggerAGVPosition != null)
                    return false;
            }

            return false;
        }

        private bool NextCommandIsXXX(EnumCommandType type, ref int index)
        {
            for (int i = localData.MoveControlData.MoveCommand.IndexOfCommandList; i < localData.MoveControlData.MoveCommand.CommandList.Count; i++)
            {
                if (localData.MoveControlData.MoveCommand.CommandList[i].CmdType == type)
                {
                    index = i;
                    return true;
                }
                else if (localData.MoveControlData.MoveCommand.CommandList[i].TriggerAGVPosition != null)
                    return false;
            }

            return false;
        }

        private void SensorStartMove(EnumVehicleSafetyAction nowAction)
        {
            ///狀況1. 下筆命令是移動且無法觸發..當作二修.
            /// 
            ///狀況2. 下筆命令是移動且可以觸發..不做事情. 
            /// 
            ///狀況3. 其他狀況..直接下動令+VChange.
            ///
            ///如果是 SlowVelocity 要加入降速.
            ///
            int index = 0;

            if (NextCommandIsMoveOrSpinTurn(EnumCommandType.Move, ref index))
            {
                MapAGVPosition newEnd = new MapAGVPosition(localData.MoveControlData.MoveCommand.CommandList[index].TriggerAGVPosition);

                if (newEnd.Angle != localData.MoveControlData.MoveCommand.EndAGVPosition.Angle)
                {
                    string errorMessage = "微修正, 下筆命令應該為SpinTurn判定為Move Command, 銃沙挖歌";
                    WriteLog(1, "", errorMessage);
                    WriteLog(1, "", errorMessage);
                    WriteLog(1, "", errorMessage);
                    WriteLog(1, "", errorMessage);
                    WriteLog(1, "", errorMessage);
                }

                newEnd.Angle = localData.MoveControlData.MoveCommand.EndAGVPosition.Angle;

                localData.MoveControlData.MoveCommand.EndAGVPosition = newEnd;
                WriteLog(7, "", String.Concat("下筆命令為移動命令, Index = ", index.ToString(),
                                              ",因此進行微修, 終點座標 ",
                                              computeFunction.GetMapAGVPositionStringWithAngle(newEnd, "0")));
                AutoMoveWithEQVelocity(newEnd, 30000);
            }
            else if (NextCommandIsMoveOrSpinTurn(EnumCommandType.SpinTurn, ref index))
            {
                MapAGVPosition newEnd = new MapAGVPosition(localData.MoveControlData.MoveCommand.CommandList[index].EndAGVPosition);
                newEnd.Angle = localData.MoveControlData.MoveCommand.EndAGVPosition.Angle;

                localData.MoveControlData.MoveCommand.EndAGVPosition = newEnd;
                WriteLog(7, "", String.Concat("下筆移動為SpinTurn, Index = ", index.ToString(),
                                              ",因此進行微修, 終點座標 ",
                                              computeFunction.GetMapAGVPositionStringWithAngle(newEnd, "0")));
                AutoMoveWithEQVelocity(newEnd, 30000);
            }
            else if (!NextCommandIsXXX(EnumCommandType.End, ref index))
            {   // 狀況3.
                WriteLog(7, "", String.Concat("一般情況,插入移動命令 : 終點座標 : ", computeFunction.GetMapAGVPositionStringWithAngle(localData.MoveControlData.MoveCommand.EndAGVPosition, "0")));

                double vChangeVelocity = localData.MoveControlData.MoveCommand.NormalVelocity;

                if (nowAction != EnumVehicleSafetyAction.Normal)
                {
                    if (nowAction == EnumVehicleSafetyAction.LowSpeed_High)
                    {
                        if (vChangeVelocity > localData.MoveControlData.CreateMoveCommandConfig.LowVelocity_High)
                            vChangeVelocity = localData.MoveControlData.CreateMoveCommandConfig.LowVelocity_High;
                    }
                    else if (nowAction == EnumVehicleSafetyAction.LowSpeed_Low)
                    {
                        if (vChangeVelocity > localData.MoveControlData.CreateMoveCommandConfig.LowVelocity_Low)
                            vChangeVelocity = localData.MoveControlData.CreateMoveCommandConfig.LowVelocity_Low;
                    }
                }

                double finalVelocity = (vChangeVelocity == localData.MoveControlData.CreateMoveCommandConfig.EQ.Velocity ? vChangeVelocity : CreateMoveCommandList.GetVChangeVelocity(0, vChangeVelocity));

                CommandControl_Move(CreateMoveCommandList.NewMoveCommand(null, localData.MoveControlData.MoveCommand.EndAGVPosition, 0, vChangeVelocity, EnumMoveStartType.SensorStopMove));
            }
            else
            {
                WriteLog(7, "", "二修可觸發,不做事情!");
            }
        }

        #endregion
        #endregion

        #region 外部FunctionCall.
        private bool CheckCanMove(ref string errorMessage)
        {
            if (!localData.MoveControlData.Ready || localData.MIPCData.MotionAlarm)
            {
                if (localData.MoveControlData.MoveCommand != null)
                {
                    errorMessage = "MoveControl not Ready, 拒絕移動命令_命令中";
                    SetAlarmCode(EnumMoveCommandControlErrorCode.拒絕移動命令_移動命令中);
                }
                else
                {
                    errorMessage = "MoveControl not Ready, 拒絕移動命令";
                    SetAlarmCode(EnumMoveCommandControlErrorCode.拒絕移動命令_MoveControlNotReady);
                }

                return false;
            }
            else if (localData.MoveControlData.ErrorBit)
            {
                errorMessage = "MoveControl ErrorBit on, 拒絕移動命令";
                SetAlarmCode(EnumMoveCommandControlErrorCode.拒絕移動命令_MoveControlErrorBitOn);
                return false;
            }
            else if (localData.MIPCData.Charging)
            {
                errorMessage = "charging中,因此無視agvm move命令~!";
                SetAlarmCode(EnumMoveCommandControlErrorCode.拒絕移動命令_充電中);
                return false;
            }
            else if (config.SensorByPass[EnumSensorSafetyType.ForkHomeCheck] && !localData.LoadUnloadData.ForkHome)
            {
                errorMessage = "Fork不在Home點,因此無視AGVM Move命令~!";
                SetAlarmCode(EnumMoveCommandControlErrorCode.拒絕移動命令_Fork不在Home點);
                return false;
            }
            else if (localData.Real == null)
            {
                errorMessage = "AGV迷航中(不知道目前在哪),因此無法接受AGVM Move命令!";
                SetAlarmCode(EnumMoveCommandControlErrorCode.拒絕移動命令_迷航中);
                return false;
            }
            else if (localData.AutoManual == EnumAutoState.Manual)
            {
                VehicleLocation now = localData.Location;

                if (now == null || !now.InSection)
                {
                    errorMessage = "AGV不在Section上(角度偏差過大/軌道偏差過大/超出Section頭或尾)";
                    SetAlarmCode(EnumMoveCommandControlErrorCode.拒絕移動命令_不在Section上);
                    return false;
                }
            }

            return true;
        }

        public bool VehicleMove(MoveCmdInfo moveCmdInfo, ref string errorMessage)
        {
            if (!SetVehicleMove(moveCmdInfo, ref errorMessage))
                return false;

            StartCommand();

            Stopwatch delayTimer = new Stopwatch();
            delayTimer.Restart();

            while (delayTimer.ElapsedMilliseconds < 1000 && localData.MoveControlData.MoveCommand == null)
                Thread.Sleep(10);

            return true;
        }

        public bool SetVehicleMove(MoveCmdInfo moveCmdInfo, ref string errorMessage)
        {
            try
            {
                lock (localData.MoveControlData.CreateCommandingLockObjcet)
                {
                    if (!CheckCanMove(ref errorMessage))
                        return false;

                    localData.MoveControlData.CreateCommanding = true;
                }

                MoveCommandData command = CreateMoveCommandList.CreateMoveCommand(moveCmdInfo, ref errorMessage);

                if (command != null)
                {
                    command.StartSOC = localData.BatteryInfo.Battery_SOC;
                    command.StartVoltage = localData.BatteryInfo.Battery_V;
                    command.AutoManual = localData.AutoManual;
                    preCommand = command;

                    Stopwatch timer = new Stopwatch();
                    timer.Restart();

                    while (localData.MoveControlData.CreateCommanding)
                    {
                        if (timer.ElapsedMilliseconds > 1000)
                        {
                            WriteLog(3, "", String.Concat("等待PreCommand -> Command逾時.."));
                            break;
                        }

                        Thread.Sleep(10);
                    }

                    return true;
                }
                else
                {
                    SetAlarmCode(EnumMoveCommandControlErrorCode.命令分解失敗);
                    localData.MoveControlData.CreateCommanding = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                errorMessage = String.Concat("Exception : ", ex.ToString());
                localData.MoveControlData.CreateCommanding = false;
                return false;
            }
        }

        public bool VehicleMove_DebugForm(MoveCmdInfo moveCmdInfo, ref string errorMessage)
        {
            if (!SetVehicleMove(moveCmdInfo, ref errorMessage))
                return false;

            return true;
        }

        public void VehiclePause()
        {
            MoveCommandData temp = localData.MoveControlData.MoveCommand;

            if (temp == null)
            {
                WriteLog(7, "", String.Concat("收到VehiclePause命令, 但不是命令執行中"));
            }
            else
            {
                WriteLog(7, "", String.Concat("收到VehiclePause命令"));
                temp.AGVPause = EnumVehicleSafetyAction.SlowStop;
            }
        }

        public void VehicleContinue()
        {
            MoveCommandData temp = localData.MoveControlData.MoveCommand;

            if (temp == null)
            {
                WriteLog(7, "", String.Concat("收到VehicleConitnue命令, 但不是命令執行中"));
            }
            else
            {
                WriteLog(7, "", String.Concat("收到VehicleConitnue命令"));
                temp.AGVPause = EnumVehicleSafetyAction.Normal;
            }
        }

        public void VehicleCancel()
        {
            MoveCommandData temp = localData.MoveControlData.MoveCommand;

            if (temp == null)
            {
                WriteLog(7, "", String.Concat("收到VehicleCancel命令, 但不是命令執行中"));
            }
            else
            {
                WriteLog(7, "", String.Concat("收到VehicleCancel命令"));
                temp.Cancel = true;
            }
        }

        public void VehicleStop()
        {
            MoveCommandData temp = localData.MoveControlData.MoveCommand;

            if (temp == null)
            {
                WriteLog(7, "", String.Concat("收到VehicleStop命令, 但不是命令執行中"));
            }
            else
            {
                WriteLog(7, "", String.Concat("收到VehicleStop命令"));
                temp.VehicleStopFlag = true;
            }
        }

        public void AddReserve(string sectionID)
        {
            MoveCommandData temp = localData.MoveControlData.MoveCommand;

            if (temp != null)
            {
                if (temp.IndexOfReserveList >= temp.ReserveList.Count)
                    WriteLog(5, "", String.Concat("Reserve已經全部取得,但收到Reserve, Section ID : ", sectionID));
                else if (temp.ReserveList[temp.IndexOfReserveList].SectionID != sectionID)
                    WriteLog(5, "", String.Concat("Reserve Section ID mismuch, Section ID : ", sectionID));
                else
                {
                    temp.ReserveList[temp.IndexOfReserveList].GetReserve = true;

                    if (temp.ReserveStop && temp.IndexOfReserveList == temp.WaitReserveIndex)
                        temp.ReserveStop = false;

                    temp.IndexOfReserveList++;
                    WriteLog(7, "", String.Concat("取得Reserve, Section ID : ", sectionID));
                }
            }
            else
                WriteLog(3, "", String.Concat("在沒有命令的情況收到Reserve, Section ID : ", sectionID));
        }

        public void AddReservedIndexForDebugModeTest(int index)
        {
            MoveCommandData temp = localData.MoveControlData.MoveCommand;

            if (temp != null && index < temp.ReserveList.Count && !temp.ReserveList[index].GetReserve)
                AddReserve(temp.ReserveList[index].SectionID);
        }
        #endregion

        private void SetMovingDirectionByEndAGVPosition(MapAGVPosition end)
        {
            VehicleLocation now = localData.Location;

            if (localData.TheMapInfo.AllSection.ContainsKey(now.NowSection))
            {
                MapAGVPosition nowPosition = computeFunction.GetAGVPositionByVehicleLocation(now);
                double nowToEndAngle = computeFunction.ComputeAngle(nowPosition, end);
                double movingAngle = computeFunction.GetCurrectAngle(nowToEndAngle - nowPosition.Angle);

                if (movingAngle > -10 && movingAngle < 10)
                    ChangeMovingDirection(EnumMovingDirection.Front);
                else if (movingAngle > 170 || movingAngle < -170)
                    ChangeMovingDirection(EnumMovingDirection.Back);
                else if (movingAngle > 80 && movingAngle < 100)
                    ChangeMovingDirection(EnumMovingDirection.Right);
                else if (movingAngle > -100 && movingAngle < -80)
                    ChangeMovingDirection(EnumMovingDirection.Left);
                else if (movingAngle > 0 && movingAngle < 90)
                    ChangeMovingDirection(EnumMovingDirection.FrontRight);
                else if (movingAngle < 0 && movingAngle > -90)
                    ChangeMovingDirection(EnumMovingDirection.FrontLeft);
                else if (movingAngle > 90 && movingAngle < 180)
                    ChangeMovingDirection(EnumMovingDirection.BackRight);
                else if (movingAngle < -90 && movingAngle > -180)
                    ChangeMovingDirection(EnumMovingDirection.BackLeft);
                else
                {
                    WriteLog(5, "", String.Concat("MovingAngle else.. change to none"));
                    ChangeMovingDirection(EnumMovingDirection.None);
                }
            }
            else
            {
                WriteLog(5, "", String.Concat("Now Section Not find, change to none"));
                ChangeMovingDirection(EnumMovingDirection.None);
            }
        }

        private void ChangeMovingDirection(EnumMovingDirection newDirection)
        {
            if (localData.MIPCData.MoveControlDirection != newDirection)
                WriteLog(7, "", String.Concat("Change MovingDirection : ", newDirection.ToString()));

            localData.MIPCData.MoveControlDirection = newDirection;
        }

        private void SetBuzzerTypeByEndAGVPosition(MapAGVPosition end)
        {
            VehicleLocation now = localData.Location;

            if (localData.TheMapInfo.AllSection.ContainsKey(now.NowSection))
            {
                MapAGVPosition nowPosition = computeFunction.GetAGVPositionByVehicleLocation(now);
                double nowToEndAngle = computeFunction.ComputeAngle(nowPosition, end);
                double movingAngle = computeFunction.GetCurrectAngle(nowToEndAngle - nowPosition.Angle);

                if ((movingAngle > -10 && movingAngle < 10) || movingAngle > 170 || movingAngle < -170)
                    ChangeBuzzerType(EnumBuzzerType.Moving);
                else
                    ChangeBuzzerType(EnumBuzzerType.MoveShift);
            }
            else
            {
                WriteLog(5, "", String.Concat("Now Section Not find, change to Moving"));
                ChangeBuzzerType(EnumBuzzerType.Moving);
            }
        }

        private void ChangeBuzzerType(EnumBuzzerType buzzerType)
        {
            if (localData.MIPCData.MoveControlBuzzerType != buzzerType)
                WriteLog(7, "", String.Concat("Change BuzzerType : ", buzzerType.ToString()));

            localData.MIPCData.MoveControlBuzzerType = buzzerType;
        }

        private void SetDirectionLightByEndAGVPosition(MapAGVPosition end)
        {
            VehicleLocation now = localData.Location;

            if (localData.TheMapInfo.AllSection.ContainsKey(now.NowSection))
            {
                MapAGVPosition nowPosition = computeFunction.GetAGVPositionByVehicleLocation(now);
                double nowToEndAngle = computeFunction.ComputeAngle(nowPosition, end);
                double movingAngle = computeFunction.GetCurrectAngle(nowToEndAngle - nowPosition.Angle);

                if (movingAngle > -10 && movingAngle < 10)
                    ChangeDirectionLight(EnumDirectionLight.Front);
                else if (movingAngle > 170 || movingAngle < -170)
                    ChangeDirectionLight(EnumDirectionLight.Back);
                else if (movingAngle > 80 && movingAngle < 100)
                    ChangeDirectionLight(EnumDirectionLight.Right);
                else if (movingAngle > -100 && movingAngle < -80)
                    ChangeDirectionLight(EnumDirectionLight.Left);
                else if (movingAngle > 0 && movingAngle < 90)
                    ChangeDirectionLight(EnumDirectionLight.FrontRight);
                else if (movingAngle < 0 && movingAngle > -90)
                    ChangeDirectionLight(EnumDirectionLight.FrontLeft);
                else if (movingAngle > 90 && movingAngle < 180)
                    ChangeDirectionLight(EnumDirectionLight.BackRight);
                else if (movingAngle < -90 && movingAngle > -180)
                    ChangeDirectionLight(EnumDirectionLight.BackLeft);
                else
                {
                    WriteLog(5, "", String.Concat("MovingAngle else.. change to none"));
                    ChangeDirectionLight(EnumDirectionLight.None);
                }
            }
            else
            {
                WriteLog(5, "", String.Concat("Now Section Not find, change to none"));
                ChangeDirectionLight(EnumDirectionLight.None);
            }
        }

        private void ChangeDirectionLight(EnumDirectionLight newDirection)
        {
            if (localData.MIPCData.MoveControlDirectionLight != newDirection)
                WriteLog(7, "", String.Concat("Change DirectionLight : ", newDirection.ToString()));

            localData.MIPCData.MoveControlDirectionLight = newDirection;
        }

        #region Auto CycleRun.
        public bool StopAutoCycleRun { get; set; } = false;
        List<MapAddress> EndList = new List<MapAddress>();

        private MapAddress GetMapAddressByRandon()
        {
            if (localData.Real == null || EndList.Count == 0)
                return null;

            Random randon = new Random();
            int index;

            while (true)
            {
                index = randon.Next(0, EndList.Count);

                if (!computeFunction.IsSamePosition(localData.Real.Position, EndList[index].AGVPosition.Position))
                {
                    if (computeFunction.GetTwoPositionDistance(localData.Real, EndList[index].AGVPosition) > 30)
                        return EndList[index];
                }
            }
        }

        private void ProcessEndAddresList()
        {
            if (EndList.Count == 0)
            {
                foreach (MapAddress address in localData.TheMapInfo.AllAddress.Values)
                {
                    EndList.Add(address);
                }
            }
        }

        public void MoveControlLocalAutoCycleRun()
        {
            StopAutoCycleRun = false;
            Random randon = new Random();
            MapAddress nextEnd;
            int num;

            string errorMessage = "";
            MoveCmdInfo moveCmdInfo;

            try
            {
                ProcessEndAddresList();

                while (!StopAutoCycleRun)
                {
                    if (localData.MoveControlData.ErrorBit)
                    {
                        WriteLog(5, "", " 結束AutoCycleRun流程 : ErrorBit On");
                        break;
                    }
                    else if (localData.MoveControlData.Ready)
                    {
                        Thread.Sleep(1000);
                        num = randon.Next(0, 3);

                        nextEnd = GetMapAddressByRandon();

                        if (nextEnd == null)
                        {
                            WriteLog(3, "", "結束AutoCycleRun流程 : GetNextMove End Address return null");
                            break;
                        }

                        moveCmdInfo = new MoveCmdInfo();
                        moveCmdInfo.CommandID = String.Concat("LocalCycleRun ", DateTime.Now.ToString("HH:mm:ss"));
                        if (!CreateMoveCommandList.Step0_CheckMovingAddress(new List<string> { nextEnd.Id }, ref moveCmdInfo, ref errorMessage))
                        {
                            WriteLog(3, "", String.Concat("結束AutoCycleRun流程 : Step0_CheckMovingAddress return false, EndAddress ID : ", nextEnd.Id));
                            break;
                        }

                        if (VehicleMove(moveCmdInfo, ref errorMessage))
                        {
                            //num = randon.Next(0, 3);

                            //if (num == 0)
                            //    AddReservedIndexForDebugModeTest(randon.Next(0, localData.MoveControlData.MoveCommand.ReserveList.Count));
                            //else // 正常命令.
                            for (int i = 0; i < localData.MoveControlData.MoveCommand.ReserveList.Count; i++)
                                AddReservedIndexForDebugModeTest(i);
                        }
                        else
                        {
                            WriteLog(3, "", "結束AutoCycleRun流程 : VehicleMove return false!");
                            break;
                        }
                    }
                }

                WriteLog(3, "", "結束AutoCycleRun流程");
            }
            catch (Exception ex)
            {
                WriteLog(5, "", String.Concat("Exceptrion : ", ex.StackTrace));
                WriteLog(5, "", String.Concat("Exceptrion : ", ex.ToString()));
            }

        }
        #endregion

        #region Write-CSV.
        private void WriteLogCSV()
        {
            List<EnumDefaultAxisName> indexToEnumDefaultAxisName = new List<EnumDefaultAxisName>();
            indexToEnumDefaultAxisName.Add(EnumDefaultAxisName.XFL);
            indexToEnumDefaultAxisName.Add(EnumDefaultAxisName.XFR);
            indexToEnumDefaultAxisName.Add(EnumDefaultAxisName.XRL);
            indexToEnumDefaultAxisName.Add(EnumDefaultAxisName.XRR);


            #region 幫忙抓高速時Encoder瞬間反向log.
            EnumEncoderAndVelocityMismach tempEnum;
            bool 幫忙抓高速時Encoder瞬間反向log = true;

            double 開始偵測速度 = 1200;
            List<double> lastEncoder = new List<double>();
            List<EnumEncoderAndVelocityMismach> axisVelocityStatus = new List<EnumEncoderAndVelocityMismach>();

            if (幫忙抓高速時Encoder瞬間反向log)
            {
                lastEncoder.Add(0);
                lastEncoder.Add(0);
                lastEncoder.Add(0);
                lastEncoder.Add(0);
                axisVelocityStatus.Add(EnumEncoderAndVelocityMismach.None);
                axisVelocityStatus.Add(EnumEncoderAndVelocityMismach.None);
                axisVelocityStatus.Add(EnumEncoderAndVelocityMismach.None);
                axisVelocityStatus.Add(EnumEncoderAndVelocityMismach.None);
            }
            #endregion

            string csvLog;
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch sleepTimer = new System.Diagnostics.Stopwatch();

            MapAGVPosition logAGVPosition;
            LocateAGVPosition locateAGVPosition;
            ThetaSectionDeviation logThetaDeviation;
            DateTime now;

            MoveCommandData logCommand;
            AxisFeedbackData tempFeedbackData = null;
            VehicleLocation tempVehicleLocation = null;
            bool action = false;

            try
            {
                while (Status != EnumControlStatus.WaitThreadStop)
                {
                    timer.Restart();

                    now = DateTime.Now;
                    logCommand = localData.MoveControlData.MoveCommand;

                    // Time.
                    csvLog = now.ToString("yyyy/MM/dd HH:mm:ss.fff");

                    // CommandStatus.
                    action = true;

                    if (logCommand != null)
                        csvLog = String.Concat(csvLog, ",", logCommand.CommandStatus.ToString());
                    else if (localData.MoveControlData.MotionControlData.JoystickMode)
                        csvLog = String.Concat(csvLog, ",JoystickMode");
                    else if (localData.MoveControlData.SpecialFlow)
                        csvLog = String.Concat(csvLog, ",SpecailFlow");
                    else
                    {
                        csvLog = String.Concat(csvLog, ",");
                        action = !MotionControl.AllServoOff;
                    }

                    // MoveStatus.
                    if (logCommand != null)
                        csvLog = String.Concat(csvLog, ",", logCommand.MoveStatus.ToString());
                    else
                        csvLog = String.Concat(csvLog, ",");

                    //  RealEncoder
                    if (logCommand != null)
                        csvLog = String.Concat(csvLog, ",", logCommand.CommandEncoder.ToString("0.0"));
                    else
                        csvLog = String.Concat(csvLog, ",");

                    //  SimulateVelocity
                    csvLog = String.Concat(csvLog, ",", localData.MoveControlData.MotionControlData.SimulateLineVelocity.ToString("0.0"));

                    //  RealMovingVelocity
                    csvLog = String.Concat(csvLog, ",", localData.MoveControlData.MotionControlData.LineVelocity.ToString("0.0"));

                    //  RealMovingVelocityAngle
                    csvLog = String.Concat(csvLog, ",", localData.MoveControlData.MotionControlData.LineVelocityAngle.ToString("0.0"));

                    //  ThetaVelocity
                    csvLog = String.Concat(csvLog, ",", localData.MoveControlData.MotionControlData.ThetaVelocity.ToString("0.0"));

                    //  Move/Stop.
                    csvLog = String.Concat(csvLog, ",", (localData.MoveControlData.MotionControlData.MoveStatus != EnumAxisMoveStatus.Stop ? "Move" : "Stop"));

                    // location
                    tempVehicleLocation = localData.Location;

                    if (tempVehicleLocation != null)
                        csvLog = String.Concat(csvLog, ",", tempVehicleLocation.LastAddress, ",", tempVehicleLocation.InAddress.ToString(), ",", tempVehicleLocation.NowSection, ",", tempVehicleLocation.DistanceFormSectionHead.ToString("0"));
                    else
                        csvLog = String.Concat(csvLog, ",,,,");

                    // real x y theta.
                    logAGVPosition = localData.Real;

                    if (logAGVPosition != null)
                        csvLog = String.Concat(csvLog, ",", logAGVPosition.Position.X.ToString("0.0"),
                                                       ",", logAGVPosition.Position.Y.ToString("0.0"),
                                                       ",", logAGVPosition.Angle.ToString("0.0"));
                    else
                        csvLog = String.Concat(csvLog, ",,,");

                    // mipc x y theta.
                    locateAGVPosition = localData.MoveControlData.MotionControlData.EncoderAGVPosition;

                    if (locateAGVPosition != null && locateAGVPosition.AGVPosition != null)
                    {
                        csvLog = String.Concat(csvLog, ",", locateAGVPosition.AGVPosition.Position.X.ToString("0.0"),
                                                       ",", locateAGVPosition.AGVPosition.Position.Y.ToString("0.0"),
                                                       ",", locateAGVPosition.AGVPosition.Angle.ToString("0.0"));
                    }
                    else
                        csvLog = String.Concat(csvLog, ",,,");

                    //  LocateDriver
                    locateAGVPosition = localData.MoveControlData.LocateControlData.LocateAGVPosition;

                    if (locateAGVPosition != null && locateAGVPosition.AGVPosition != null)
                    {
                        csvLog = String.Concat(csvLog, ",", locateAGVPosition.Device.ToString(),
                                                       ",", locateAGVPosition.AGVPosition.Position.X.ToString("0.0"),
                                                       ",", locateAGVPosition.AGVPosition.Position.Y.ToString("0.0"),
                                                       ",", locateAGVPosition.AGVPosition.Angle.ToString("0.0"));
                    }
                    else
                        csvLog = String.Concat(csvLog, ",,,,");

                    // ThetaSectionDeviation
                    logThetaDeviation = localData.MoveControlData.ThetaSectionDeviation;

                    if (logThetaDeviation != null)
                    {
                        csvLog = String.Concat(csvLog, ",", logThetaDeviation.Theta.ToString("0.0"),
                                                       ",", logThetaDeviation.SectionDeviation.ToString("0.0"));
                    }
                    else
                        csvLog = String.Concat(csvLog, ",,");

                    // 晟淇要的東西 報價20W NT
                    csvLog = String.Concat(csvLog, ",", localData.MoveControlData.MotionControlData.X_VelocityCommand.ToString("0.00"));
                    csvLog = String.Concat(csvLog, ",", localData.MoveControlData.MotionControlData.X_VelocityFeedback.ToString("0.00"));

                    csvLog = String.Concat(csvLog, ",", localData.MoveControlData.MotionControlData.Y_VelocityCommand.ToString("0.00"));
                    csvLog = String.Concat(csvLog, ",", localData.MoveControlData.MotionControlData.Y_VelocityFeedback.ToString("0.00"));

                    csvLog = String.Concat(csvLog, ",", localData.MoveControlData.MotionControlData.Theta_VelocityCommand.ToString("0.00"));
                    csvLog = String.Concat(csvLog, ",", localData.MoveControlData.MotionControlData.Theta_VelocityFeedback.ToString("0.00"));

                    // Velocity Error.
                    //csvLog = String.Concat(csvLog, ",", localData.MoveControlData.MotionControlData.XVelocityError.ToString("0.0"));
                    //csvLog = String.Concat(csvLog, ",", localData.MoveControlData.MotionControlData.YVelocityError.ToString("0.0"));
                    //csvLog = String.Concat(csvLog, ",", localData.MoveControlData.MotionControlData.ThetaVelocityError.ToString("0.0"));

                    //csvLog = String.Concat(csvLog, ",", localData.MoveControlData.MotionControlData.Slam_XVelocityError.ToString("0.0"));
                    //csvLog = String.Concat(csvLog, ",", localData.MoveControlData.MotionControlData.Slam_YVelocityError.ToString("0.0"));
                    //csvLog = String.Concat(csvLog, ",", localData.MoveControlData.MotionControlData.Slam_ThetaVelocityError.ToString("0.0"));

                    #region MotionDriver.
                    for (int i = 0; i < 4; i++)
                    {
                        if (localData.MoveControlData.MotionControlData.AllAxisFeedbackData != null)
                        {
                            if (i < indexToEnumDefaultAxisName.Count &&
                                localData.MoveControlData.MotionControlData.AllAxisFeedbackData.ContainsKey(indexToEnumDefaultAxisName[i]))
                                tempFeedbackData = localData.MoveControlData.MotionControlData.AllAxisFeedbackData[indexToEnumDefaultAxisName[i]];
                            else
                                tempFeedbackData = null;
                        }
                        else
                            tempFeedbackData = null;

                        if (tempFeedbackData != null)
                        {
                            csvLog = String.Concat(csvLog, ",", tempFeedbackData.Position.ToString("0.0"));
                            csvLog = String.Concat(csvLog, ",", tempFeedbackData.Driver_Encoder.ToString("0.0"));
                            //csvLog = String.Concat(csvLog, ",", tempFeedbackData.Velocity.ToString("0.0"));
                            //csvLog = String.Concat(csvLog, ",", tempFeedbackData.Driver_RPM.ToString("0.0"));
                            csvLog = String.Concat(csvLog, ",", tempFeedbackData.AxisServoOnOff.ToString());
                            csvLog = String.Concat(csvLog, ",", tempFeedbackData.V.ToString("0.0"));
                            csvLog = String.Concat(csvLog, ",", tempFeedbackData.DA.ToString("0.0"));
                            csvLog = String.Concat(csvLog, ",", tempFeedbackData.QA.ToString("0.0"));
                            csvLog = String.Concat(csvLog, ",", tempFeedbackData.Temperature.ToString("0.0"));
                            csvLog = String.Concat(csvLog, ",", tempFeedbackData.VelocityCommand.ToString("0.00"));
                            csvLog = String.Concat(csvLog, ",", tempFeedbackData.VelocityFeedback.ToString("0.00"));
                            csvLog = String.Concat(csvLog, ",", tempFeedbackData.PWM.ToString("0.00"));
                        }
                        else
                            csvLog = String.Concat(csvLog, ",,,,,,,,,,");

                        #region 幫忙抓高速時Encoder瞬間反向log.
                        if (幫忙抓高速時Encoder瞬間反向log && tempFeedbackData != null &&
                            i < axisVelocityStatus.Count && i < lastEncoder.Count)
                        {
                            if (tempFeedbackData.Velocity >= 開始偵測速度)
                                tempEnum = EnumEncoderAndVelocityMismach.VelocityPos;
                            else if (tempFeedbackData.Velocity <= -開始偵測速度)
                                tempEnum = EnumEncoderAndVelocityMismach.VelocityNag;
                            else
                                tempEnum = EnumEncoderAndVelocityMismach.None;

                            if (axisVelocityStatus[i] == tempEnum &&
                                tempEnum != EnumEncoderAndVelocityMismach.None)
                            {
                                if (tempEnum == EnumEncoderAndVelocityMismach.VelocityPos)
                                {   // 正轉吧?.
                                    if (tempFeedbackData.Position < lastEncoder[i])
                                        WriteLog(1, "", String.Concat("[MIPC][Motion] Axis = ", indexToEnumDefaultAxisName[i].ToString(), " 正轉時, Encodcer瞬間反向"));
                                }
                                else
                                {   // 反轉??.
                                    if (tempFeedbackData.Position > lastEncoder[i])
                                        WriteLog(1, "", String.Concat("[MIPC][Motion] Axis = ", indexToEnumDefaultAxisName[i].ToString(), " 逆轉時, Encodcer瞬間反向"));
                                }
                            }

                            axisVelocityStatus[i] = tempEnum;
                            lastEncoder[i] = tempFeedbackData.Position;
                        }
                        #endregion
                    }
                    #endregion

                    //for (int i = 0; i < localData.MIPCData.MIPCTestArray.Length; i++)
                    //    csvLog = String.Concat(csvLog, ",", localData.MIPCData.MIPCTestArray[i].ToString("0.000"));

                    if (!localData.MainFlowConfig.IdleNotRecordCSV || action)
                        logger.LogString(csvLog);

                    sleepTimer.Restart();

                    while (timer.ElapsedMilliseconds < config.TimeValueConfig.IntervalTimeList[EnumIntervalTimeType.CSVLogInterval])
                        Thread.Sleep(1);

                    sleepTimer.Stop();

                    if (localData.SimulateMode && localData.MoveControlData.SimulateBypassLog)
                    {
                        if (sleepTimer.ElapsedMilliseconds > config.TimeValueConfig.IntervalTimeList[EnumIntervalTimeType.CSVLogInterval] * 2)
                            WriteLog(3, "", String.Concat("CSVThreadSleep time : ", sleepTimer.ElapsedMilliseconds.ToString("0")));
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(1, "", ex.ToString());
                WriteLog(1, "", "WriteLogCSV Excption!");
            }
        }
        #endregion

        private bool HaveAlignmentLocateDriver()
        {
            for (int i = 0; i < LocateControl.LocateControlDriverList.Count; i++)
            {
                if (LocateControl.LocateControlDriverList[i].DriverConfig.LocateDriverType == EnumLocateDriverType.AlignmentValue)
                    return true;
            }

            return false;
        }

        private void LocateDriverAllOnOff(bool onOff)
        {
            for (int i = 0; i < LocateControl.LocateControlDriverList.Count; i++)
                LocateControl.LocateControlDriverList[i].PollingOnOff = onOff;
        }

        public bool SpecialFlowStop { get; set; } = false;
        private string autoMoveByTargetAddressID = "";
        private Thread specialFlowThread = null;

        #region 移動至Section中央.
        public void SpecailFlow_MoveToSectionCenter()
        {
            if (ActionCanUse(EnumUserAction.Move_SpecialFlow_ToSectionCenter))
            {
                if (specialFlowThread == null || !specialFlowThread.IsAlive)
                {
                    SpecialFlowStop = false;
                    specialFlowThread = new Thread(SpecailFlow_MoveToSectionCenter_Thread);
                    specialFlowThread.Start();
                }
            }
        }

        private void SpecailFlow_MoveToSectionCenter_Thread()
        {
            localData.MoveControlData.SpecialFlow = true;

            MotionControl.ServoOn_All();

            Stopwatch timer = new Stopwatch();
            timer.Restart();

            try
            {
                MapAGVPosition end = computeFunction.GetAGVPositionByVehicleLocation_SectionAngle(localData.Location);

                while (!MotionControl.AllServoOn)
                {
                    if (timer.ElapsedMilliseconds > 1000)
                        break;

                    Thread.Sleep(100);
                }

                if (MotionControl.AllServoOn && end != null && MotionControl.Move_SpecialFlow(end))
                {
                    while (localData.MoveControlData.MotionControlData.MoveStatus != EnumAxisMoveStatus.Stop)
                    {
                        if (SpecialFlowStop)
                            MotionControl.Stop_Normal();

                        Thread.Sleep(500);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(5, "", String.Concat("Exception : ", ex.ToString()));
            }

            MotionControl.ServoOff_All();
            localData.MoveControlData.SpecialFlow = false;
        }
        #endregion

        #region 修正使用Target或定位資訊.
        public void SpecailFlow_ReviseByTargetOrLocateData(string addressID)
        {
            if (localData.TheMapInfo.IsPortOrChargingStation(addressID) &&
                ActionCanUse(EnumUserAction.Move_SpecialFlow_ReviseByTargetOrLocateData))
            {
                if (specialFlowThread == null || !specialFlowThread.IsAlive)
                {
                    SpecialFlowStop = false;
                    autoMoveByTargetAddressID = addressID;
                    specialFlowThread = new Thread(SpecailFlow_ReviseByTargetOrLocateData_Thread);
                    specialFlowThread.Start();
                }
            }
        }

        private void SpecailFlow_ReviseByTargetOrLocateData_Thread()
        {
            localData.MoveControlData.SpecialFlow = true;

            MotionControl.ServoOn_All();

            try
            {
                LocateControl.SetAlignmentValueAddressID(autoMoveByTargetAddressID);

                Stopwatch timer = new Stopwatch();
                timer.Restart();

                while (!MotionControl.AllServoOn)
                {
                    if (timer.ElapsedMilliseconds > 1000)
                        break;

                    Thread.Sleep(100);
                }

                if (MotionControl.AllServoOn)
                {
                    Thread.Sleep(500);

                    if (MotionControl.Move_SpecialFlow(localData.TheMapInfo.AllAddress[autoMoveByTargetAddressID].AGVPosition))
                    {
                        while (localData.MoveControlData.MotionControlData.MoveStatus != EnumAxisMoveStatus.Stop)
                        {
                            if (SpecialFlowStop)
                                MotionControl.Stop_Normal();

                            Thread.Sleep(500);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(5, "", String.Concat("Exception : ", ex.ToString()));
            }

            LocateControl.SetAlignmentValueOff();
            MotionControl.ServoOff_All();

            localData.MoveControlData.SpecialFlow = false;
        }
        #endregion

        #region 修正使用Target.
        public void SpecailFlow_ReviseByTarget(string addressID)
        {
            if (localData.TheMapInfo.IsPortOrChargingStation(addressID) &&
                ActionCanUse(EnumUserAction.Move_SpecialFlow_ReviseByTargetOrLocateData))
            {
                if (specialFlowThread == null || !specialFlowThread.IsAlive)
                {
                    SpecialFlowStop = false;
                    autoMoveByTargetAddressID = addressID;
                    specialFlowThread = new Thread(SpecailFlow_ReviseByTarget_Thread);
                    specialFlowThread.Start();
                }
            }
        }

        private void SpecailFlow_ReviseByTarget_Thread()
        {
            localData.MoveControlData.SpecialFlow = true;

            LocateDriverAllOnOff(false);
            MotionControl.ServoOn_All();

            try
            {
                LocateControl.SetAlignmentValueAddressID(autoMoveByTargetAddressID);

                LocateAGVPosition checkAlignmentLocateOK = localData.MoveControlData.LocateControlData.LocateAGVPosition;
                Stopwatch timer = new Stopwatch();
                timer.Restart();

                bool okToRun = true;

                while (checkAlignmentLocateOK == null || checkAlignmentLocateOK.Device != EnumLocateDriverType.AlignmentValue.ToString())
                {
                    if (timer.ElapsedMilliseconds > 5000 || SpecialFlowStop)
                        break;

                    checkAlignmentLocateOK = localData.MoveControlData.LocateControlData.LocateAGVPosition;
                }

                if (checkAlignmentLocateOK == null || checkAlignmentLocateOK.Device != EnumLocateDriverType.AlignmentValue.ToString())
                    okToRun = false;

                timer.Restart();

                while (!MotionControl.AllServoOn)
                {
                    if (timer.ElapsedMilliseconds > 1000)
                        break;

                    Thread.Sleep(100);
                }

                if (okToRun)
                {
                    Thread.Sleep(500);

                    if (MotionControl.Move_SpecialFlow(localData.TheMapInfo.AllAddress[autoMoveByTargetAddressID].AGVPosition))
                    {
                        while (localData.MoveControlData.MotionControlData.MoveStatus != EnumAxisMoveStatus.Stop)
                        {
                            if (SpecialFlowStop)
                                MotionControl.Stop_Normal();

                            Thread.Sleep(500);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(5, "", String.Concat("Exception : ", ex.ToString()));
            }

            LocateDriverAllOnOff(true);
            LocateControl.SetAlignmentValueOff();
            MotionControl.ServoOff_All();

            localData.MoveControlData.SpecialFlow = false;
        }
        #endregion

        #region Auto前,自動移動.
        public bool BeforeAutoAction_MoveToAddress()
        {
            if (!ActionCanUse(EnumUserAction.Move_SpecialFlow_ActionBeforeAuto_MoveToAddressIfClose))
                return false;

            localData.MoveControlData.SpecialFlow = true;

            bool result = false;

            try
            {
                bool needMove = false;
                string targetAddressID = "";

                VehicleLocation now = localData.Location;

                if (!localData.MIPCData.Charging /*&& !now.InAddress*/)
                {
                    double distanceToHeadAddress = now.RealDistanceFormSectionHead;
                    double distanceToTailAddress = localData.TheMapInfo.AllSection[now.NowSection].Distance - now.RealDistanceFormSectionHead;

                    bool headClose = distanceToHeadAddress <= 300;
                    bool tailClose = distanceToTailAddress <= 300;

                    if (headClose && tailClose)
                    {
                        needMove = true;

                        bool headIsPortOrChargingStation = localData.TheMapInfo.IsPortOrChargingStation(localData.TheMapInfo.AllSection[now.NowSection].FromAddress.Id);
                        bool tailIsPortOrChargingStation = localData.TheMapInfo.IsPortOrChargingStation(localData.TheMapInfo.AllSection[now.NowSection].ToAddress.Id);

                        if ((headIsPortOrChargingStation && tailIsPortOrChargingStation) ||
                            (!headIsPortOrChargingStation && !tailIsPortOrChargingStation))
                        {
                            if (distanceToHeadAddress <= distanceToTailAddress)
                                targetAddressID = localData.TheMapInfo.AllSection[now.NowSection].FromAddress.Id;
                            else
                                targetAddressID = localData.TheMapInfo.AllSection[now.NowSection].ToAddress.Id;
                        }
                        else if (headIsPortOrChargingStation)
                            targetAddressID = localData.TheMapInfo.AllSection[now.NowSection].FromAddress.Id;
                        else
                            targetAddressID = localData.TheMapInfo.AllSection[now.NowSection].ToAddress.Id;
                    }
                    else if (headClose)
                    {
                        targetAddressID = localData.TheMapInfo.AllSection[now.NowSection].FromAddress.Id;
                        needMove = true;
                    }
                    else if (tailClose)
                    {
                        targetAddressID = localData.TheMapInfo.AllSection[now.NowSection].ToAddress.Id;
                        needMove = true;
                    }
                }

                if (needMove)
                {
                    if (!localData.TheMapInfo.IsPortOrChargingStation(targetAddressID))
                        needMove = false;
                }

                if (needMove)
                {
                    if (!localData.MIPCData.Charging)
                    {
                        MotionControl.ServoOn_All();

                        LocateControl.SetAlignmentValueAddressID(targetAddressID);

                        Stopwatch timer = new Stopwatch();
                        timer.Restart();

                        timer.Restart();

                        while (!MotionControl.AllServoOn)
                        {
                            if (timer.ElapsedMilliseconds > 1000)
                                break;

                            Thread.Sleep(100);
                        }

                        if (MotionControl.AllServoOn)
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                if (MotionControl.Move_SpecialFlow(localData.TheMapInfo.AllAddress[targetAddressID].AGVPosition))
                                {
                                    while (localData.MoveControlData.MotionControlData.MoveStatus != EnumAxisMoveStatus.Stop)
                                    {
                                        if (SpecialFlowStop)
                                            MotionControl.Stop_Normal();

                                        Thread.Sleep(500);
                                    }
                                }
                            }
                        }

                        LocateControl.SetAlignmentValueOff();
                        result = true;
                    }
                }
                else
                    result = true;
            }
            catch (Exception ex)
            {
                WriteLog(5, "", String.Concat("Exception : ", ex.ToString()));
                MotionControl.Stop_EMS();
            }

            MotionControl.ServoOff_All();

            localData.MoveControlData.SpecialFlow = false;
            return result;
        }
        #endregion

        #region 補正(如果有Target且在Port/ChargingStation).
        private Thread reviseAndSetPositionByAddressID = null;

        private string setPositionAddressID = "";

        public void ReviseAndSetPositionByAddressID(string addressID)
        {
            if (ActionCanUse(EnumUserAction.Move_SetPosition) &&
                ActionCanUse(EnumUserAction.Move_SpecialFlow_ReviseByTarget) &&
                !localData.MoveControlData.ReviseAndSetPosition)
            {
                if (reviseAndSetPositionByAddressID == null || !reviseAndSetPositionByAddressID.IsAlive)
                {
                    setPositionAddressID = addressID;
                    reviseAndSetPositionByAddressID = new Thread(ReviseAndSetPositionByAddressIDThread);
                    reviseAndSetPositionByAddressID.Start();
                }
            }
        }

        private void ReviseAndSetPositionByAddressIDThread()
        {
            localData.MoveControlData.ReviseAndSetPosition = true;
            localData.MoveControlData.ReviseAndSetPositionData = "";

            try
            {
                double range = 500;
                double angleRange = 5;
                bool 補正OK = true;

                if (HaveAlignmentLocateDriver() && localData.TheMapInfo.IsPortOrChargingStation(setPositionAddressID))
                {
                    localData.MoveControlData.SpecialFlow = true;

                    try
                    {
                        #region Target 補正.
                        LocateDriverAllOnOff(false);
                        MotionControl.ServoOn_All();
                        LocateControl.SetAlignmentValueAddressID(setPositionAddressID);

                        LocateAGVPosition checkAlignmentLocateOK = localData.MoveControlData.LocateControlData.LocateAGVPosition;
                        Stopwatch timer = new Stopwatch();
                        timer.Restart();


                        while (checkAlignmentLocateOK == null || checkAlignmentLocateOK.Device != EnumLocateDriverType.AlignmentValue.ToString())
                        {
                            if (timer.ElapsedMilliseconds > 5000 || SpecialFlowStop)
                                break;

                            checkAlignmentLocateOK = localData.MoveControlData.LocateControlData.LocateAGVPosition;
                        }

                        if (checkAlignmentLocateOK == null || checkAlignmentLocateOK.Device != EnumLocateDriverType.AlignmentValue.ToString())
                            補正OK = false;

                        timer.Restart();

                        while (!MotionControl.AllServoOn)
                        {
                            if (timer.ElapsedMilliseconds > 1000)
                                break;

                            Thread.Sleep(100);
                        }

                        if (補正OK)
                        {
                            Thread.Sleep(500);

                            if (MotionControl.Move_SpecialFlow(localData.TheMapInfo.AllAddress[setPositionAddressID].AGVPosition))
                            {
                                while (localData.MoveControlData.MotionControlData.MoveStatus != EnumAxisMoveStatus.Stop)
                                {
                                    if (SpecialFlowStop)
                                    {
                                        MotionControl.Stop_Normal();
                                        補正OK = false;
                                    }

                                    Thread.Sleep(500);
                                }
                            }
                        }

                        LocateDriverAllOnOff(true);
                        LocateControl.SetAlignmentValueOff();
                        MotionControl.ServoOff_All();
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                    }

                    localData.MoveControlData.SpecialFlow = false;
                    range = 100;
                }

                if (補正OK)
                {
                    LocateControl.SetSLAMPositionByAddressID(setPositionAddressID, range, angleRange);

                    if (localData.MoveControlData.LocateControlData.SlamLocateOK)
                        localData.MoveControlData.ReviseAndSetPositionResult = EnumProfaceStringTag.重定位成功.ToString();
                    else
                        localData.MoveControlData.ReviseAndSetPositionResult = EnumProfaceStringTag.重定位失敗_差距過大.ToString();
                }
                else
                    localData.MoveControlData.ReviseAndSetPositionResult = EnumProfaceStringTag.補正異常_距離過遠.ToString();
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }

            localData.MoveControlData.ReviseAndSetPosition = false;
        }
        #endregion
    }
}