using Mirle.Agv.INX.Controller.Tools;
using Mirle.Agv.INX.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Mirle.Agv.INX.Controller
{
    public class LocateControlHandler
    {
        private ComputeFunction computeFunction = ComputeFunction.Instance;
        private LoggerAgent loggerAgent = LoggerAgent.Instance;
        private LocalData localData = LocalData.Instance;
        private AlarmHandler alarmHandler;
        private string device = MethodInfo.GetCurrentMethod().ReflectedType.Name;
        private string normalLogName;

        public List<LocateDriver> LocateControlDriverList { get; set; } = new List<LocateDriver>();

        private string path = @"D:\MecanumConfigs\MoveControl\LocateControl\LocateControlConfig.xml";
        private LocateControlConfig config = new LocateControlConfig();

        private Thread resetAlarmThread = null;

        private bool resetAlarm = false;
        private EnumControlStatus status = EnumControlStatus.NotInitial;
        public EnumControlStatus Status
        {
            get
            {
                if (resetAlarm)
                    return EnumControlStatus.ResetAlarm;
                else
                {
                    if (localData.SimulateMode)
                        return EnumControlStatus.Ready;
                    else
                        return status;
                }
            }
        }

        private bool setAGVPositionByAdmin = false;
        private LocateAGVPosition setAGVPosition = null;

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

        public void SendAlarmCode(EnumMoveCommandControlErrorCode code)
        {

        }

        public LocateControlHandler(AlarmHandler alarmHandler, string normalLogName, LoadUnloadControlHandler loadUnloadControl)
        {
            this.alarmHandler = alarmHandler;
            this.normalLogName = normalLogName;

            string errorMessage = "";

            if (ReadLocateControlConfig(ref errorMessage))
            {   //ReadResetPointCSV();
                status = EnumControlStatus.Initial;
                InitailControlDriver(loadUnloadControl);
            }
            else
            {
                WriteLog(1, "", errorMessage);
                SendAlarmCode(EnumMoveCommandControlErrorCode.LocateControl初始化失敗);
            }
        }

        private void ResetAlarmThread()
        {
            resetAlarm = true;

            if (!localData.SimulateMode)
            {
                for (int i = 0; i < LocateControlDriverList.Count; i++)
                {
                    if (LocateControlDriverList[i].Status != EnumControlStatus.Ready)
                    {
                        LocateControlDriverList[i].ResetAlarm();
                        Thread.Sleep(10);
                    }
                }
            }

            resetAlarm = false;
        }

        public void ResetAlarm()
        {
            if (resetAlarmThread == null || !resetAlarmThread.IsAlive)
            {
                resetAlarmThread = new Thread(ResetAlarmThread);
                resetAlarmThread.Start();
            }
        }

        #region Read XML.
        private LocateDriverConfig ReadLocateControlData(XmlElement element, string path)
        {
            LocateDriverConfig temp = new LocateDriverConfig();
            foreach (XmlNode item in element.ChildNodes)
            {
                switch (item.Name)
                {
                    case "Device":
                        temp.Device = item.InnerText;
                        break;
                    case "LocateType":
                        temp.LocateDriverType = (EnumLocateDriverType)Enum.Parse(typeof(EnumLocateDriverType), item.InnerText);
                        break;
                    case "Path":
                        temp.Path = item.InnerText;
                        temp.Path = Path.Combine(new DirectoryInfo(path).Parent.FullName, item.InnerText);
                        break;
                    default:
                        break;
                }
            }

            return temp;
        }

        public bool ReadLocateControlConfig(ref string errorMessage)
        {
            try
            {
                XmlDocument doc = new XmlDocument();

                if (!File.Exists(path))
                {
                    errorMessage = "找不到Config!";
                    return false;
                }

                doc.Load(path);
                var rootNode = doc.DocumentElement;
                LocateDriverConfig temp;
                int order = 0;

                foreach (XmlNode item in rootNode.ChildNodes)
                {
                    switch (item.Name)
                    {
                        case "OrderLowerDelay":
                            config.OrderLowerDelay = (int)double.Parse(item.InnerText);
                            break;
                        case "Driver":
                            temp = new LocateDriverConfig();
                            temp = ReadLocateControlData((XmlElement)item, path);
                            temp.Order = order;
                            order++;
                            config.Driver.Add(temp);
                            break;
                        default:
                            break;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = String.Concat("Exception : ", ex.ToString());
                return false;
            }
        }

        #endregion

        #region 初始化.
        private void InitailControlDriver(LoadUnloadControlHandler loadUnloadControl)
        {
            LocateDriver temp = new LocateDriver();

            bool noSlam = true;

            for (int i = 0; i < config.Driver.Count; i++)
            {
                switch (config.Driver[i].LocateDriverType)
                {
                    case EnumLocateDriverType.AlignmentValue:
                        temp = new LocateDriver_AlignmentValue();

                        if (loadUnloadControl != null && loadUnloadControl.LoadUnload != null)
                            ((LocateDriver_AlignmentValue)temp).LoadUnload = loadUnloadControl.LoadUnload;

                        break;
                    case EnumLocateDriverType.BarcodeMapSystem:
                        temp = new LocateDriver_BarcodeMapSystem();
                        break;
                    case EnumLocateDriverType.SLAM_Sick:
                        temp = new LocateDriver_SLAM_Sick();
                        break;
                    case EnumLocateDriverType.SLAM_BITO:
                        temp = new LocateDriver_SLAM_BITO();
                        break;
                    default:
                        break;
                }

                temp.InitialDriver(config.Driver[i], alarmHandler, normalLogName);
                LocateControlDriverList.Add(temp);

                if (temp.LocateType == EnumLocateType.SLAM)
                    noSlam = false;

                if (temp.Status == EnumControlStatus.Initial && !localData.SimulateMode)
                    temp.ConnectDriver();
            }

            if (noSlam)
            {
                WriteLog(7, "", "SlamLocateOK = true");
                localData.MoveControlData.LocateControlData.SlamLocateOK = true;
            }
        }
        #endregion

        private Thread autoSetPositionThread = null;

        private int retryCount = 0;
        private int maxRetryCount = 3;

        private void AutoSetPositionThread()
        {
            WriteLog(7, "", "AutoSetPosition Change to WaitResult");
            localData.MoveControlData.LocateControlData.AutoSetPositionStatus = EnumSlamAutoSetPosition.WaitResult;

            for (int i = 0; i < LocateControlDriverList.Count; i++)
            {
                if (LocateControlDriverList[i].LocateType == EnumLocateType.SLAM)
                {
                    string errorMessage = "";
                    if (((LocateDriver_SLAM)LocateControlDriverList[i]).SetPositionByMapAGVPosition(localData.MoveControlData.LocateControlData.AutoSetSlamPositionData, 500, 5, ref errorMessage))
                    {
                        localData.MoveControlData.LocateControlData.SlamLocateOK = true;
                        WriteLog(7, "", "AutoSetPosition Change to End(Success)");
                        localData.MoveControlData.LocateControlData.AutoSetPositionStatus = EnumSlamAutoSetPosition.End;
                        return;
                    }
                    else
                    {
                        retryCount++;

                        if (retryCount < maxRetryCount)
                        {
                            WriteLog(7, "", "AutoSetPosition Change to SetPosition(Retry)");
                            localData.MoveControlData.LocateControlData.AutoSetPositionStatus = EnumSlamAutoSetPosition.SetPosition;
                        }
                        else
                        {
                            WriteLog(7, "", "AutoSetPosition Change to End(Failed)");
                            localData.MoveControlData.LocateControlData.AutoSetPositionStatus = EnumSlamAutoSetPosition.End;
                        }

                        return;
                    }
                }
            }

            WriteLog(7, "", "AutoSetPosition Change to SetPosition");
            localData.MoveControlData.LocateControlData.AutoSetPositionStatus = EnumSlamAutoSetPosition.SetPosition;
        }

        public void UpdateLocateControlData()
        {
            LocateAGVPosition newLocateAGVPosition = null;
            LocateAGVPosition slamOriginPosition = null;

            int order;

            if (localData.MoveControlData.LocateControlData.SelectOrder == null)
                order = LocateControlDriverList.Count - 1;
            else
                order = localData.MoveControlData.LocateControlData.SelectOrder.Order +
                        (int)((DateTime.Now - localData.MoveControlData.LocateControlData.SelectOrder.GetDataTime).TotalMilliseconds / config.OrderLowerDelay);

            EnumControlStatus newStatus = EnumControlStatus.Ready;

            bool firstSlam = true;

            for (int i = 0; i < LocateControlDriverList.Count && i <= order; i++)
            {
                if (firstSlam && LocateControlDriverList[i].LocateType == EnumLocateType.SLAM)
                {
                    firstSlam = false;
                    slamOriginPosition = ((LocateDriver_SLAM)LocateControlDriverList[i]).GetOriginAGVPosition;

                    if (slamOriginPosition == null || slamOriginPosition.AGVPosition == null)
                        localData.MoveControlData.LocateControlData.SlamOriginPosition = null;
                    else
                    {
                        localData.MoveControlData.LocateControlData.SlamOriginPosition = slamOriginPosition.AGVPosition;

                        if (localData.MoveControlData.LocateControlData.AutoSetPositionStatus == EnumSlamAutoSetPosition.WaitSlamDataOK)
                        {
                            localData.MoveControlData.LocateControlData.AutoSetPositionStatus = EnumSlamAutoSetPosition.SetPosition;
                            WriteLog(7, "", "AutoSetPosition Change to SetPosition");
                        }
                    }
                }

                if (newLocateAGVPosition == null)
                    newLocateAGVPosition = LocateControlDriverList[i].GetLocateAGVPosition;

                if ((int)newStatus < (int)LocateControlDriverList[i].Status)
                    newStatus = LocateControlDriverList[i].Status;
            }

            if (!localData.MoveControlData.LocateControlData.SlamLocateOK &&
                localData.MoveControlData.LocateControlData.AutoSetPositionStatus == EnumSlamAutoSetPosition.SetPosition)
            {
                if (autoSetPositionThread == null || !autoSetPositionThread.IsAlive)
                {
                    autoSetPositionThread = new Thread(AutoSetPositionThread);
                    autoSetPositionThread.Start();
                }
            }

            status = newStatus;

            if (setAGVPositionByAdmin)
            {
                if (setAGVPosition != null)
                {
                    setAGVPositionByAdmin = false;
                    newLocateAGVPosition = setAGVPosition;
                }
            }

            if (!localData.MoveControlData.LocateControlData.SlamLocateOK && localData.MainFlowConfig.SimulateMode)
            {
                WriteLog(7, "", "SlamLocateOK = true");
                localData.MoveControlData.LocateControlData.SlamLocateOK = true;
            }

            localData.MoveControlData.LocateControlData.LocateAGVPosition = newLocateAGVPosition;
        }

        public void SetAGVPosition(MapAGVPosition agvPosition)
        {
            LocateAGVPosition newLocateAGVPosition = new LocateAGVPosition();
            newLocateAGVPosition.AGVPosition = agvPosition;
            newLocateAGVPosition.GetDataTime = DateTime.Now;
            newLocateAGVPosition.Type = EnumAGVPositionType.Normal;
            newLocateAGVPosition.Device = "Admin set";
            newLocateAGVPosition.Order = 0;

            setAGVPosition = newLocateAGVPosition;
            setAGVPositionByAdmin = true;
        }

        public void SetSLAMPositionByAddressID(string addressID, double distanceRange, double angleRange)
        {
            if (localData.TheMapInfo.AllAddress.ContainsKey(addressID))
            {
                WriteLog(3, "", String.Concat("address ID : ", addressID));
                string errorMessage = "";

                for (int i = 0; i < LocateControlDriverList.Count; i++)
                {
                    if (LocateControlDriverList[i].LocateType == EnumLocateType.SLAM)
                    {
                        if (!((LocateDriver_SLAM)LocateControlDriverList[i]).SetPositionByAddressID(addressID, distanceRange, angleRange, ref errorMessage))
                            WriteLog(3, "", String.Concat(LocateControlDriverList[i].DriverConfig.Device, " set slamPosition fail : ", errorMessage));
                        else
                            WriteLog(3, "", String.Concat(LocateControlDriverList[i].DriverConfig.Device, " set slamPosition scuess"));
                    }
                }
            }
            else
                WriteLog(3, "", String.Concat("收到不存在的address ID : ", addressID));
        }

        public bool SetSlamPositionAndReplace(string address, int averageCount)
        {
            try
            {
                bool result = true;

                for (int i = 0; i < LocateControlDriverList.Count; i++)
                {
                    if (LocateControlDriverList[i].LocateType == EnumLocateType.SLAM)
                    {
                        if (!((LocateDriver_SLAM)LocateControlDriverList[i]).SetSlamPositionAndReplace(address, averageCount))
                            result = false;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                WriteLog(5, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }

        public bool WriteSlamPositionAll()
        {
            bool result = true;

            for (int i = 0; i < LocateControlDriverList.Count; i++)
            {
                if (LocateControlDriverList[i].LocateType == EnumLocateType.SLAM)
                {
                    if (!((LocateDriver_SLAM)LocateControlDriverList[i]).WirteSlamPosition_All())
                        result = false;
                }
            }

            return result;
        }

        public void SwitchAlignmentValueAddressID(string addressID)
        {
            for (int i = 0; i < LocateControlDriverList.Count; i++)
            {
                if (LocateControlDriverList[i].DriverConfig.LocateDriverType == EnumLocateDriverType.AlignmentValue)
                    ((LocateDriver_AlignmentValue)LocateControlDriverList[i]).SwitchAlignmentValueAdress(addressID);
            }
        }

        public void SetAlignmentValueAddressID(string addressID)
        {
            for (int i = 0; i < LocateControlDriverList.Count; i++)
            {
                if (LocateControlDriverList[i].DriverConfig.LocateDriverType == EnumLocateDriverType.AlignmentValue)
                    ((LocateDriver_AlignmentValue)LocateControlDriverList[i]).SetPollingAlignmentValueAddress(addressID);
            }
        }

        public void SetAlignmentValueOff()
        {
            for (int i = 0; i < LocateControlDriverList.Count; i++)
            {
                if (LocateControlDriverList[i].DriverConfig.LocateDriverType == EnumLocateDriverType.AlignmentValue)
                    LocateControlDriverList[i].PollingOnOff = false;
            }
        }

        public void GetFirstNotAlignmentValueData(ref LocateAGVPosition data, ref string tagName)
        {
            for (int i = 0; i < LocateControlDriverList.Count; i++)
            {
                if (LocateControlDriverList[i].DriverConfig.LocateDriverType != EnumLocateDriverType.AlignmentValue)
                {
                    if (LocateControlDriverList[i].LocateType == EnumLocateType.SLAM)
                    {
                        tagName = String.Concat(LocateControlDriverList[i].DriverConfig.Device, "_Origin");
                        data = ((LocateDriver_SLAM)LocateControlDriverList[i]).GetOriginAGVPosition;
                        return;
                    }
                    else
                    {
                        tagName = LocateControlDriverList[i].DriverConfig.Device;
                        data = LocateControlDriverList[i].GetLocateAGVPosition;
                        return;
                    }
                }
            }
        }

        public void 踹踹看內插法YO(string addressA, string addressB)
        {
            if (localData.MoveControlData.MoveCommand != null ||
                localData.LoadUnloadData.LoadUnloadCommand != null)
                return;

            if (localData.AutoManual == EnumAutoState.Manual &&
                localData.LoginLevel >= EnumLoginLevel.Admin)
            {
                for (int i = 0; i < LocateControlDriverList.Count; i++)
                {
                    if (LocateControlDriverList[i].LocateType == EnumLocateType.SLAM)
                        ((LocateDriver_SLAM)LocateControlDriverList[i]).踹踹看內插法YO(addressA, addressB);
                }
            }
        }
    }
}