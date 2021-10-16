using Mirle.Agv.INX.Controller;
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

namespace Mirle.Agv.INX.Control
{
    public class SafetySensorControlHandler
    {
        private ComputeFunction computeFunction = ComputeFunction.Instance;
        private LoggerAgent loggerAgent = LoggerAgent.Instance;
        private LocalData localData = LocalData.Instance;
        private AlarmHandler alarmHandler;
        private MIPCControlHandler mipcControl;
        private string device = MethodInfo.GetCurrentMethod().ReflectedType.Name;
        private string normalLogName = "SafetySensor";

        private SafetySensorConfig config;

        private int safetyLevelCount = Enum.GetNames(typeof(EnumSafetyLevel)).Count();

        public List<SafetySensor> AllSafetySensor = new List<SafetySensor>();

        public uint AllStatus { get; set; } = 0;

        private uint allByPassAlarm = 0;
        private uint allByPassStatus = 0;

        private EnumMovingDirection lastDirection = EnumMovingDirection.Initial;

        private bool InitialSuccess = false;

        public SafetySensorControlHandler(MIPCControlHandler mipcControl, AlarmHandler alarmHandler, string path)
        {
            this.mipcControl = mipcControl;
            this.alarmHandler = alarmHandler;
            ReadXML(path);
            ProcessConfig();
        }

        public void InitialSafetySensor()
        {
            SafetySensor temp;

            for (int i = 0; i < config.SafetySensorList.Count; i++)
            {
                temp = null;

                switch (config.SafetySensorList[i].DeviceType)
                {
                    case EnumDeviceType.Tim781:
                        temp = new SafetySensor_Tim781();
                        break;

                    case EnumDeviceType.Bumper:
                        temp = new SafetySensor_Bumper();
                        break;
                    case EnumDeviceType.EMO:
                        temp = new SafetySensor_EMO();
                        break;
                    case EnumDeviceType.Sensor:
                        temp = new SafetySensor_Sensor();
                        break;
                    case EnumDeviceType.None:
                    default:
                        WriteLog(3, "", String.Concat("Device : ", config.SafetySensorList[i].Device, ", DeviceType : ", config.SafetySensorList[i].DeviceType.ToString(), " Initial未實作"));
                        break;
                }

                if (temp != null)
                {
                    temp.Initial(mipcControl, alarmHandler, config.SafetySensorList[i]);
                    AllSafetySensor.Add(temp);
                }
            }

            InitialSuccess = true;
        }

        private void SendMIPCChangeAreaSensorDirection()
        {
            #region SafetySensor區域設定.
            EnumMovingDirection tempDirection = localData.MIPCData.MoveControlDirection;
            bool 方向Bypass = false;

            switch (tempDirection)
            {
                case EnumMovingDirection.Front:
                    if (localData.MIPCData.BypassFront)
                    {
                        方向Bypass = true;
                        tempDirection = EnumMovingDirection.None;
                    }
                    break;
                case EnumMovingDirection.Back:
                    if (localData.MIPCData.BypassBack)
                    {
                        方向Bypass = true;
                        tempDirection = EnumMovingDirection.None;
                    }
                    break;
                case EnumMovingDirection.Left:
                    if (localData.MIPCData.BypassLeft)
                    {
                        方向Bypass = true;
                        tempDirection = EnumMovingDirection.None;
                    }
                    break;
                case EnumMovingDirection.Right:
                    if (localData.MIPCData.BypassRight)
                    {
                        方向Bypass = true;
                        tempDirection = EnumMovingDirection.None;
                    }
                    break;
                case EnumMovingDirection.FrontLeft:
                    if (localData.MIPCData.BypassFront && localData.MIPCData.BypassLeft)
                    {
                        方向Bypass = true;
                        tempDirection = EnumMovingDirection.None;
                    }
                    else if (localData.MIPCData.BypassFront)
                    {
                        方向Bypass = true;
                        tempDirection = EnumMovingDirection.Left;
                    }
                    else if (localData.MIPCData.BypassLeft)
                    {
                        方向Bypass = true;
                        tempDirection = EnumMovingDirection.Front;
                    }
                    break;
                case EnumMovingDirection.FrontRight:
                    if (localData.MIPCData.BypassFront && localData.MIPCData.BypassRight)
                    {
                        方向Bypass = true;
                        tempDirection = EnumMovingDirection.None;
                    }
                    else if (localData.MIPCData.BypassFront)
                    {
                        方向Bypass = true;
                        tempDirection = EnumMovingDirection.Right;
                    }
                    else if (localData.MIPCData.BypassRight)
                    {
                        方向Bypass = true;
                        tempDirection = EnumMovingDirection.Front;
                    }
                    break;
                case EnumMovingDirection.BackLeft:
                    if (localData.MIPCData.BypassBack && localData.MIPCData.BypassLeft)
                    {
                        方向Bypass = true;
                        tempDirection = EnumMovingDirection.None;
                    }
                    else if (localData.MIPCData.BypassBack)
                    {
                        方向Bypass = true;
                        tempDirection = EnumMovingDirection.Left;
                    }
                    else if (localData.MIPCData.BypassLeft)
                    {
                        方向Bypass = true;
                        tempDirection = EnumMovingDirection.Back;
                    }
                    break;
                case EnumMovingDirection.BackRight:
                    if (localData.MIPCData.BypassBack && localData.MIPCData.BypassRight)
                    {
                        方向Bypass = true;
                        tempDirection = EnumMovingDirection.None;
                    }
                    else if (localData.MIPCData.BypassBack)
                    {
                        方向Bypass = true;
                        tempDirection = EnumMovingDirection.Right;
                    }
                    else if (localData.MIPCData.BypassRight)
                    {
                        方向Bypass = true;
                        tempDirection = EnumMovingDirection.Back;
                    }
                    break;
                default:
                    break;
            }

            localData.MIPCData.AreaSensorDirection = tempDirection;
            localData.MIPCData.方向Bypass = 方向Bypass;

            if (tempDirection != lastDirection)
            {
                localData.MIPCData.ChangeAreaSensorDirectionTagList = new List<string>();
                localData.MIPCData.ChangeAreaSensorDirectionValueList = new List<float>();

                WriteLog(7, "", String.Concat("Change to : ", tempDirection));

                for (int i = 0; i < AllSafetySensor.Count; i++)
                    AllSafetySensor[i].ChangeMovingDirection_AddList(tempDirection);

                if (localData.MIPCData.ChangeAreaSensorDirectionTagList.Count == 0 && localData.MIPCData.ChangeAreaSensorDirectionValueList.Count == 0)
                    lastDirection = tempDirection;
                else
                {
                    if (mipcControl.SendMIPCDataByMIPCTagName(localData.MIPCData.ChangeAreaSensorDirectionTagList, localData.MIPCData.ChangeAreaSensorDirectionValueList))
                        lastDirection = tempDirection;
                    else
                        WriteLog(5, "", String.Concat("Change to : ", tempDirection, " 失敗!"));
                }
            }
            #endregion
        }

        private Thread changeAreaSensorDirectionThread = null;

        private int waitThreadCount = 0;

        public void UpdateAllSafetySensor()
        {
            if (!InitialSuccess)
                return;

            if (changeAreaSensorDirectionThread == null || !changeAreaSensorDirectionThread.IsAlive)
            {
                changeAreaSensorDirectionThread = new Thread(SendMIPCChangeAreaSensorDirection);
                changeAreaSensorDirectionThread.Start();
                waitThreadCount = 0;
            }
            else
            {
                waitThreadCount++;

                if (waitThreadCount > 1)
                    WriteLog(3, "", String.Concat("changeAreaSensorDirectionThread lag, waitThreadCount = ", waitThreadCount.ToString()));
            }

            uint tempStatus = 0;

            uint tempAllByPassAlarm = 0;
            uint tempAllByPassStatus = 0;

            for (int i = 0; i < AllSafetySensor.Count; i++)
            {
                AllSafetySensor[i].UpdateStatus();
                tempStatus = tempStatus | AllSafetySensor[i].Status;
                tempAllByPassAlarm = (tempAllByPassAlarm + AllSafetySensor[i].ByPassAlarm);
                tempAllByPassStatus = (tempAllByPassStatus + AllSafetySensor[i].ByPassStatus);
            }

            allByPassAlarm = tempAllByPassAlarm;
            allByPassStatus = tempAllByPassStatus;

            if (allByPassAlarm != 0)
                SetAlarmCode(EnumMIPCControlErrorCode.SensorSafety_AlarmByPass);
            else
                ResetAlarmCode(EnumMIPCControlErrorCode.SensorSafety_AlarmByPass);

            if (allByPassStatus != 0)
                SetAlarmCode(EnumMIPCControlErrorCode.SensorSafety_SafetyByPass);
            else
                ResetAlarmCode(EnumMIPCControlErrorCode.SensorSafety_SafetyByPass);

            AllStatus = tempStatus;

            EnumSafetyLevel newLevel = EnumSafetyLevel.Normal;

            for (int i = safetyLevelCount - 1; i >= 0; i--)
            {
                if ((tempStatus & (1 << i)) != 0 && ((EnumSafetyLevel)i) != EnumSafetyLevel.Warn)
                {
                    newLevel = ((EnumSafetyLevel)i);
                    break;
                }
            }

            if (newLevel != localData.MIPCData.safetySensorStatus)
            {
                if (newLevel == EnumSafetyLevel.SlowStop && localData.MoveControlData.MoveCommand != null)
                {
                    stopAndMovingFlagOn = true;
                    stopSignalTimer.Restart();
                }
                else
                    stopAndMovingFlagOn = false;

                WriteLog(7, "", String.Concat("safetySensorStatus 從 ", localData.MIPCData.safetySensorStatus.ToString(), " 切換至 ", newLevel.ToString()));
                localData.MIPCData.SafetySensorStatus = newLevel;
            }
            else if (newLevel == EnumSafetyLevel.SlowStop)
            {
                if (localData.MoveControlData.MoveCommand != null)
                {
                    if (!stopAndMovingFlagOn)
                    {
                        stopAndMovingFlagOn = true;
                        stopSignalTimer.Restart();
                    }
                }
                else
                    stopAndMovingFlagOn = false;
            }

            if (localData.MoveControlData.MoveControlConfig != null && localData.MoveControlData.MoveControlConfig.TimeValueConfig != null &&
                localData.MoveControlData.MoveControlConfig.TimeValueConfig.TimeoutValueList.ContainsKey(EnumTimeoutValueType.SlowStopTimeoutValue))
            {
                if (stopAndMovingFlagOn && stopSignalTimer.ElapsedMilliseconds > localData.MoveControlData.MoveControlConfig.TimeValueConfig.TimeoutValueList[EnumTimeoutValueType.SafetySensorStopTimeout])
                    SetAlarmCode(EnumMIPCControlErrorCode.SensorSafety_停止訊號Timeout);
                else
                    ResetAlarmCode(EnumMIPCControlErrorCode.SensorSafety_停止訊號Timeout);
            }
        }

        private Stopwatch stopSignalTimer = new Stopwatch();
        private bool stopAndMovingFlagOn = false;

        public void ResetByPass()
        {
            for (int i = 0; i < AllSafetySensor.Count; i++)
            {
                AllSafetySensor[i].ByPassAlarm = 0;
                AllSafetySensor[i].ByPassStatus = 0;
            }
        }

        public void ResetAlarm()
        {
        }

        #region WriteLog/SendAlarmCode.
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

        private void SetAlarmCode(EnumMIPCControlErrorCode alarmCode)
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

        private void ResetAlarmCode(EnumMIPCControlErrorCode alarmCode)
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
        #endregion

        #region XML.
        private List<EnumSafetyLevel> ReadSafetyLevelList(XmlElement element)
        {
            List<EnumSafetyLevel> returnList = new List<EnumSafetyLevel>();

            return returnList;
        }

        private List<string> ReadTagList(XmlElement element)
        {
            List<string> returnStringList = new List<string>();

            foreach (XmlNode item in element.ChildNodes)
            {
                if (item.Name == "Tag")
                {
                    if (localData.MIPCData.AllDataByMIPCTagName.ContainsKey(item.InnerText))
                    {
                        returnStringList.Add(item.InnerText);
                    }
                    else
                    {
                        WriteLog(1, "", String.Concat("Tag : ", item.InnerText, " not find in AllDataByMIPCTagName"));
                        return new List<string>();
                    }
                }
                else
                    WriteLog(1, "", String.Concat("TagList Config must be <Tag>"));
            }

            return returnStringList;
        }

        private void ReadInputDataXML(ref SafetySensorData temp, XmlElement element, EnumSafetySensorType deviceType)
        {
            bool a = true;
            bool readLevel = false;
            EnumSafetyLevel level = EnumSafetyLevel.Normal;

            bool readTag = false;
            string tag = "";

            if (deviceType == EnumSafetySensorType.EMO ||
                deviceType == EnumSafetySensorType.Bumper)
            {
                readLevel = true;
                level = EnumSafetyLevel.EMO;
            }

            foreach (XmlNode item in element.ChildNodes)
            {
                switch (item.Name)
                {
                    case "MIPCTagName":
                        if (localData.MIPCData.AllDataByMIPCTagName.ContainsKey(item.InnerText))
                        {
                            tag = item.InnerText;
                            readTag = true;
                        }
                        else
                            WriteLog(1, "", String.Concat("Tag : ", item.InnerText, " not find in AllDataByMIPCTagName"));

                        break;
                    case "SafetyLevel":
                        if (Enum.TryParse(item.InnerText, out level))
                            readLevel = true;
                        else
                            WriteLog(3, "", String.Concat("無此SafetyLevel : ", item.InnerText));

                        break;
                    case "AB":
                        if (item.InnerText == "B")
                            a = false;

                        break;
                    default:
                        break;
                }
            }

            if (readTag && readLevel)
            {
                temp.InputSafetyLevelList.Add(level);
                temp.MIPCTagNmaeInput.Add(tag);
                temp.ABList.Add(a);
            }
        }

        private void ReadInputAndSafetyLevel(ref SafetySensorData temp, XmlElement element, EnumSafetySensorType deviceType)
        {
            foreach (XmlNode item in element.ChildNodes)
            {
                ReadInputDataXML(ref temp, (XmlElement)item, deviceType);
            }
        }

        private Dictionary<EnumMovingDirection, string> ReadMovingDirectionXML(XmlElement element, int inputCount)
        {
            Dictionary<EnumMovingDirection, string> returnData = new Dictionary<EnumMovingDirection, string>();
            EnumMovingDirection type;

            foreach (XmlNode item in element.ChildNodes)
            {
                if (Enum.TryParse(item.Name, out type))
                {
                    if (item.InnerText.Length == inputCount)
                    {
                        if (returnData.ContainsKey(type))
                        {
                            WriteLog(1, "", String.Concat("EnumMovingDirection type : ", item.Name, " 重複"));
                            return new Dictionary<EnumMovingDirection, string>();
                        }
                        else
                            returnData.Add(type, item.InnerText);
                    }
                    else
                    {
                        WriteLog(1, "", String.Concat("EnumMovingDirection type : ", item.Name, ", uint : ", item.InnerText, " 和input數量不相符"));
                        return new Dictionary<EnumMovingDirection, string>();
                    }
                }
                else
                {
                    WriteLog(1, "", String.Concat("EnumMovingDirection 中無此種type : ", item.Name));
                    return new Dictionary<EnumMovingDirection, string>();
                }
            }

            return returnData;
        }

        private void ReadSafetySensorXML(XmlElement element)
        {
            SafetySensorData temp = new SafetySensorData();

            foreach (XmlNode item in element.ChildNodes)
            {
                switch (item.Name)
                {
                    case "Device":
                        temp.Device = item.InnerText;
                        break;

                    case "Type":
                        EnumSafetySensorType type;

                        if (Enum.TryParse(item.InnerText, out type))
                            temp.Type = type;
                        else
                            WriteLog(1, "", String.Concat("Device : ", temp.Device, ", Type TypeParse Fail : ", item.InnerText));
                        break;
                    case "DeviceType":
                        EnumDeviceType deviceType;

                        if (Enum.TryParse(item.InnerText, out deviceType))
                            temp.DeviceType = deviceType;
                        else
                            WriteLog(1, "", String.Concat("Device : ", temp.Device, ", DeviceType TypeParse Fail : ", item.InnerText));

                        break;
                    case "BeamSensorDircetion":
                        if (temp.Type == EnumSafetySensorType.BeamSensor || temp.Type == EnumSafetySensorType.Bumper)
                        {
                            EnumMovingDirection beamSensorDircetion;

                            if (Enum.TryParse(item.InnerText, out beamSensorDircetion))
                                temp.BeamSensorDircetion = beamSensorDircetion;
                            else
                                WriteLog(1, "", String.Concat("Device : ", temp.Device, " BeamSensorDircetion Read Fail"));
                        }
                        else
                            WriteLog(1, "", String.Concat("Device : ", temp.Device, " type != BeamSensor 不應該有BeamSensorDircetion"));
                        break;

                    case "MIPCTagNameOutput":
                        if (temp.Type == EnumSafetySensorType.AreaSensor)
                            temp.MIPCTagNameOutput = ReadTagList((XmlElement)item);
                        else
                            WriteLog(1, "", String.Concat("Device : ", temp.Device, " type != AreaSensor 不應該有MIPCTagNmaeInput"));
                        break;

                    case "MIPCTagNmaeInput":
                        ReadInputAndSafetyLevel(ref temp, (XmlElement)item, temp.Type);
                        break;

                    case "AreaSensorChangeDircetion":
                        if (temp.Type == EnumSafetySensorType.AreaSensor)
                            temp.AreaSensorChangeDircetion = ReadMovingDirectionXML((XmlElement)item, temp.MIPCTagNameOutput.Count);
                        else
                            WriteLog(1, "", String.Concat("Device : ", temp.Device, " type != AreaSensor 不應該有AreaSensorChangeDircetion"));
                        break;

                    default:
                        break;
                }
            }

            config.SafetySensorList.Add(temp);
        }

        private void ReadXML(string path)
        {
            try
            {
                config = new SafetySensorConfig();

                if (path == null || path == "")
                {
                    WriteLog(3, "", "SafetySensorConfig 路徑錯誤為null或空值");
                    return;
                }
                else if (!File.Exists(path))
                {
                    WriteLog(1, "", "找不到SafetySensorConfig.xml.");
                    return;
                }

                XmlDocument doc = new XmlDocument();
                doc.Load(path);
                XmlElement rootNode = doc.DocumentElement;

                string locatePath = new DirectoryInfo(path).Parent.FullName;

                foreach (XmlNode item in rootNode.ChildNodes)
                {
                    switch (item.Name)
                    {
                        case "SafetySensor":
                            ReadSafetySensorXML((XmlElement)item);
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
        #endregion

        public void ProcessConfig()
        {
            List<float> ioList;

            for (int i = 0; i < config.SafetySensorList.Count; i++)
            {
                // output.
                switch (config.SafetySensorList[i].Type)
                {
                    case EnumSafetySensorType.AreaSensor:
                        foreach (var temp in config.SafetySensorList[i].AreaSensorChangeDircetion)
                        {
                            ioList = new List<float>();

                            for (int j = 0; j < temp.Value.Length; j++)
                                ioList.Add(temp.Value[j] == '0' ? 0 : 1);

                            config.SafetySensorList[i].AreaSensorChangeDircetionInputIOList.Add(temp.Key, ioList);
                        }

                        break;
                    case EnumSafetySensorType.BeamSensor:
                        WriteLog(1, "", "未實作");
                        break;
                    default:
                        break;
                }

                // input.
                for (int j = 0; j < config.SafetySensorList[i].MIPCTagNmaeInput.Count; j++)
                {
                    if (!config.SafetySensorList[i].MIPCInputTagNameToBit.ContainsKey(config.SafetySensorList[i].MIPCTagNmaeInput[j]))
                        config.SafetySensorList[i].MIPCInputTagNameToBit.Add(config.SafetySensorList[i].MIPCTagNmaeInput[j], (int)config.SafetySensorList[i].InputSafetyLevelList[j]);
                    else
                        WriteLog(1, "", String.Concat("Device : ", config.SafetySensorList[i].Device, " , mipcTagNmae 重複(bit), TageName : ", config.SafetySensorList[i].MIPCTagNmaeInput[j]));

                    if (!config.SafetySensorList[i].MIPCInputTagNameToAB.ContainsKey(config.SafetySensorList[i].MIPCTagNmaeInput[j]))
                        config.SafetySensorList[i].MIPCInputTagNameToAB.Add(config.SafetySensorList[i].MIPCTagNmaeInput[j], config.SafetySensorList[i].ABList[j]);
                    else
                        WriteLog(1, "", String.Concat("Device : ", config.SafetySensorList[i].Device, " , mipcTagNmae 重複(AB), TageName : ", config.SafetySensorList[i].MIPCTagNmaeInput[j]));
                }
            }
        }
    }
}
