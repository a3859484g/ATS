using Mirle.Agv.INX.Controller;
using Mirle.Agv.INX.Controller.Tools;
using Mirle.Agv.INX.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Mirle.Agv.INX.Control
{
    public class LoadUnload
    {
        protected ComputeFunction computeFunction = ComputeFunction.Instance;
        protected LoggerAgent loggerAgent = LoggerAgent.Instance;
        protected LocalData localData = LocalData.Instance;
        protected MIPCControlHandler mipcControl = null;
        protected string device = "";
        protected string normalLogName = "LoadUnload";

        protected Logger logger = LoggerAgent.Instance.GetLooger("LoadUnloadCSV");

        public PIOFlow LeftPIO = null;
        public PIOFlow RightPIO = null;

        protected bool configAllOK = true;

        public bool CanLeftLoadUnload = false;
        public bool CanRightLoadUnload = false;

        public bool BreakenStepMode = false;
        public bool CanGoBack = false;
        public bool CanPause = false;


        public List<double> PIOTimeoutList = new List<double>();
        public int TimeoutNumber = 10;
        protected AlarmHandler alarmHandler = null;

        protected int sleepTime = 5;

        public List<string> AxisList { get; set; } = new List<string>();
        public List<string> FeedbackAxisList { get; set; } = new List<string>();
        public List<bool> AxisCanJog { get; set; } = new List<bool>();
        public List<string> AxisPosName { get; set; } = new List<string>();
        public List<string> AxisNagName { get; set; } = new List<string>();

        public Dictionary<string, List<string>> AxisSensorList { get; set; } = new Dictionary<string, List<string>>();
        // 使用人機顯示內容

        public Dictionary<string, List<DataDelayAndChange>> AxisSensorDataList = new Dictionary<string, List<DataDelayAndChange>>();

        public Dictionary<string, Dictionary<string, DataDelayAndChange>> AllAxisSensorData = new Dictionary<string, Dictionary<string, DataDelayAndChange>>();

        public EnumLoadUnloadJogSpeed JogSpeed { get; set; } = EnumLoadUnloadJogSpeed.Normal;
        public bool JogByPass { get; set; } = false;


        public bool LoadingByPass { get; set; } = false;
        public bool DoubleStorageSensorByPass { get; set; } = false;

        protected EnumControlStatus Status = EnumControlStatus.Ready;

        public string HomeText { get; set; } = "";

        private string localPath = @"D:\MecanumConfigs\MIPCControl";
        protected Dictionary<string, LoadUnloadAxisData> axisDataList = new Dictionary<string, LoadUnloadAxisData>();
        protected LoadUnloadOffset alignmentDeviceOffset = null;
        public Dictionary<int, StageData> LeftStageDataList = new Dictionary<int, StageData>();
        public Dictionary<int, StageData> RightStageDataList = new Dictionary<int, StageData>();

        public Dictionary<int, StageNumberToBarcodeReaderSetting> LeftStageBarocdeReaderSetting = new Dictionary<int, StageNumberToBarcodeReaderSetting>();
        public Dictionary<int, StageNumberToBarcodeReaderSetting> RightStageBarocdeReaderSetting = new Dictionary<int, StageNumberToBarcodeReaderSetting>();
        public Dictionary<string, double> BarcodeReaderIDToRealDistance = new Dictionary<string, double>();

        public bool DoubleStoregeL = false;
        public bool DoubleStoregeR = false;
        protected string checkAddressID = "";

        public Dictionary<string, AddressAlignmentValueOffset> AllAddressOffset = new Dictionary<string, AddressAlignmentValueOffset>();
        public bool AllAddressOffsetChange { get; set; } = false;

        public event EventHandler ForkLoadCompleteEvent; //0407liu LULComplete修改
        public event EventHandler ForkUnloadCompleteEvent;

        protected void SendLoadCompleteEventToMiddler()
        {
            ForkLoadCompleteEvent?.Invoke(this, null);
        }

        protected void SendUnloadCompleteEventToMiddler()
        {
            ForkUnloadCompleteEvent?.Invoke(this, null);
        }

        public virtual void Initial(MIPCControlHandler mipcControl, AlarmHandler alarmHandler)
        {
            this.alarmHandler = alarmHandler;
            this.mipcControl = mipcControl;
        }

        public virtual double GetDeltaZ
        {
            get
            {
                return 0;
            }
        }

        public virtual void CloseLoadUnload()
        {
        }

        private List<int> resetAlarmCodeList = new List<int>();

        protected virtual void AlarmCodeClear()
        {
        }

        protected void SetAlarmCode(EnumLoadUnloadControlErrorCode alarmCode)
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

        protected void ResetAlarmCode(EnumLoadUnloadControlErrorCode alarmCode)
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

        #region ReadXML.CSV.
        protected MapPosition ReadMapPositionXML(XmlElement element)
        {
            MapPosition temp = new MapPosition();

            foreach (XmlNode item in element.ChildNodes)
            {
                switch (item.Name)
                {
                    case "X":
                        temp.X = double.Parse(item.InnerText);
                        break;
                    case "Y":
                        temp.Y = double.Parse(item.InnerText);
                        break;
                    default:
                        break;
                }
            }

            return temp;
        }

        protected void ReadLoadUnloadOffsetConfigXML()
        {
            XmlHandler xmlHandler = new XmlHandler();
            string path = "";

            try
            {
                path = Path.Combine(localPath, localData.MainFlowConfig.AGVType.ToString(), "LoadUnloadOffsetConfig.xml");
                alignmentDeviceOffset = xmlHandler.ReadXml<LoadUnloadOffset>(path);
            }
            catch
            {
                WriteLog(5, "", "開始讀取Backup檔案");

                try
                {
                    path = Path.Combine(localPath, localData.MainFlowConfig.AGVType.ToString(), "LoadUnloadOffsetConfig_Backup.xml");
                    alignmentDeviceOffset = xmlHandler.ReadXml<LoadUnloadOffset>(path);

                    WriteLog(7, "", "Backup讀取成功");
                }
                catch
                {
                    WriteLog(3, "", "Backup讀取失敗");

                }
            }
        }

        protected void WriteAlignmentDeviceOffset()
        {
            XmlHandler xmlHandler = new XmlHandler();

            try
            {
                xmlHandler.WriteXml(alignmentDeviceOffset,
                    Path.Combine(localPath, localData.MainFlowConfig.AGVType.ToString(), "LoadUnloadOffsetConfig.xml"));

                xmlHandler.WriteXml(alignmentDeviceOffset,
                    Path.Combine(localPath, localData.MainFlowConfig.AGVType.ToString(), "LoadUnloadOffsetConfig_Backup.xml"));
            }
            catch { }
        }

        protected void ReadStargeCSV()
        {
            string path = Path.Combine(localPath, localData.MainFlowConfig.AGVType.ToString(), "StageConfig.csv");

            if (!File.Exists(path))
                return;

            try
            {
                string[] allRows = File.ReadAllLines(path);

                if (allRows == null || allRows.Length < 1)
                {
                    WriteLog(5, "", String.Concat("StageConfig.csv line == 0"));
                    return;
                }

                string[] titleRow = allRows[0].Split(',');
                allRows = allRows.Skip(1).ToArray();

                int nRows = allRows.Length;
                int nColumns = titleRow.Length;

                Dictionary<string, int> dicHeaderIndexes = new Dictionary<string, int>();

                for (int i = 0; i < nColumns; i++)
                {
                    string keyword = titleRow[i].Trim();

                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        if (dicHeaderIndexes.ContainsKey(keyword))
                        {
                            WriteLog(3, "", String.Concat("Title : ", keyword, " repeat"));
                            return;
                        }
                        else
                            dicHeaderIndexes.Add(keyword, i);
                    }
                }

                StageData stageData;
                EnumStageDirection tempDirection;
                for (int i = 0; i < nRows; i++)
                {
                    string[] getThisRow = allRows[i].Split(',');
                    stageData = new StageData();

                    if (Enum.TryParse(getThisRow[dicHeaderIndexes["Direction"]], out tempDirection))
                    {
                        stageData.Name = getThisRow[dicHeaderIndexes["Name"]];
                        stageData.Direction = tempDirection;
                        stageData.StargeNumber = Int32.Parse(getThisRow[dicHeaderIndexes["StargeNumber"]]);

                        stageData.Benchmark_P = double.Parse(getThisRow[dicHeaderIndexes["Benchmark_P"]]);
                        stageData.Benchmark_Y = double.Parse(getThisRow[dicHeaderIndexes["Benchmark_Y"]]);
                        stageData.Benchmark_Theta = double.Parse(getThisRow[dicHeaderIndexes["Benchmark_Theta"]]);
                        stageData.Benchmark_Z = double.Parse(getThisRow[dicHeaderIndexes["Benchmark_Z"]]);

                        stageData.Encoder_P = double.Parse(getThisRow[dicHeaderIndexes["Encoder_P"]]);
                        stageData.Encoder_Y = double.Parse(getThisRow[dicHeaderIndexes["Encoder_Y"]]);
                        stageData.Encoder_Theta = double.Parse(getThisRow[dicHeaderIndexes["Encoder_Theta"]]);
                        stageData.Encoder_Z = double.Parse(getThisRow[dicHeaderIndexes["Encoder_Z"]]);

                        if (tempDirection == EnumStageDirection.Left)
                        {
                            if (LeftStageDataList.ContainsKey(stageData.StargeNumber))
                                LeftStageDataList[stageData.StargeNumber] = stageData;
                            else
                                LeftStageDataList.Add(stageData.StargeNumber, stageData);
                        }
                        else if (tempDirection == EnumStageDirection.Right)
                        {
                            if (RightStageDataList.ContainsKey(stageData.StargeNumber))
                                RightStageDataList[stageData.StargeNumber] = stageData;
                            else
                                RightStageDataList.Add(stageData.StargeNumber, stageData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        public virtual void ResetAlarm()
        {

        }

        protected void ReadAxisData(string path = "")
        {
            if (path == "")
                path = Path.Combine(localPath, localData.MainFlowConfig.AGVType.ToString(), "LoadUnloadAxisData.csv");

            try
            {
                string[] allRows = File.ReadAllLines(path);

                if (allRows == null || allRows.Length < 1)
                {
                    WriteLog(5, "", String.Concat("LoadUnloadAxisData.csv line == 0"));
                    return;
                }

                string[] titleRow = allRows[0].Split(',');
                allRows = allRows.Skip(1).ToArray();

                int nRows = allRows.Length;
                int nColumns = titleRow.Length;

                Dictionary<string, int> dicHeaderIndexes = new Dictionary<string, int>();

                for (int i = 0; i < nColumns; i++)
                {
                    string keyword = titleRow[i].Trim();

                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        if (dicHeaderIndexes.ContainsKey(keyword))
                        {
                            WriteLog(3, "", String.Concat("Title : ", keyword, " repeat"));
                            return;
                        }
                        else
                            dicHeaderIndexes.Add(keyword, i);
                    }
                }

                LoadUnloadAxisData axisData;
                for (int i = 0; i < nRows; i++)
                {
                    string[] getThisRow = allRows[i].Split(',');
                    axisData = new LoadUnloadAxisData();

                    axisData.Name = getThisRow[dicHeaderIndexes["Name"]];
                    axisData.HomeOffset = double.Parse(getThisRow[dicHeaderIndexes["HomeOffset"]]);
                    axisData.PlausUnit = double.Parse(getThisRow[dicHeaderIndexes["PlausUnit"]]);
                    axisData.AutoVelocity = double.Parse(getThisRow[dicHeaderIndexes["AutoVelocity"]]);
                    axisData.AutoAcceleration = double.Parse(getThisRow[dicHeaderIndexes["AutoAcceleration"]]);
                    axisData.AutoDeceleration = double.Parse(getThisRow[dicHeaderIndexes["AutoDeceleration"]]);
                    axisData.HomeVelocity = double.Parse(getThisRow[dicHeaderIndexes["HomeVelocity"]]) / 100;
                    axisData.HomeVelocity_High = double.Parse(getThisRow[dicHeaderIndexes["HomeVelocity_High"]]) / 100;
                    axisData.PosLimit = double.Parse(getThisRow[dicHeaderIndexes["PosLimit"]]);
                    axisData.NagLimit = double.Parse(getThisRow[dicHeaderIndexes["NagLimit"]]);

                    axisData.JogSpeed.Add(EnumLoadUnloadJogSpeed.High, double.Parse(getThisRow[dicHeaderIndexes["JogHigh"]]) / 100);
                    axisData.JogSpeed.Add(EnumLoadUnloadJogSpeed.Normal, double.Parse(getThisRow[dicHeaderIndexes["JogNormal"]]) / 100);
                    axisData.JogSpeed.Add(EnumLoadUnloadJogSpeed.Low, double.Parse(getThisRow[dicHeaderIndexes["JogLow"]]) / 100);

                    axisDataList.Add(axisData.Name, axisData);
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));

                if (path == Path.Combine(localPath, localData.MainFlowConfig.AGVType.ToString(), "LoadUnloadAxisData.csv"))
                    ReadAxisData(Path.Combine(localPath, localData.MainFlowConfig.AGVType.ToString(), "LoadUnloadAxisData_Backup.csv"));
                else
                    configAllOK = false;
            }
        }

        protected void WriteAxisData()
        {
            string path = Path.Combine(localPath, localData.MainFlowConfig.AGVType.ToString(), "LoadUnloadAxisData.csv");

            string path_backup = Path.Combine(localPath, localData.MainFlowConfig.AGVType.ToString(), "LoadUnloadAxisData_Backup.csv");
            try
            {
                List<string> outputList = new List<string>();

                outputList.Add("Name,HomeOffset,PlausUnit,AutoVelocity,AutoAcceleration,AutoDeceleration,HomeVelocity,HomeVelocity_High,JogHigh,JogNormal,JogLow,PosLimit,NagLimit");

                foreach (LoadUnloadAxisData axisData in axisDataList.Values)
                {
                    outputList.Add(
                        String.Concat(
                            axisData.Name, ",",
                            axisData.HomeOffset.ToString("0.0000"), ",",
                            axisData.PlausUnit.ToString("0.0000"), ",",
                            axisData.AutoVelocity.ToString("0.0000"), ",",
                            axisData.AutoAcceleration.ToString("0.0000"), ",",
                            axisData.AutoDeceleration.ToString("0.0000"), ",",
                            (axisData.HomeVelocity * 100).ToString("0.0"), ",",
                            (axisData.HomeVelocity_High * 100).ToString("0.0"), ",",
                            (axisData.JogSpeed[EnumLoadUnloadJogSpeed.High] * 100).ToString("0.0"), ",",
                            (axisData.JogSpeed[EnumLoadUnloadJogSpeed.Normal] * 100).ToString("0.0"), ",",
                            (axisData.JogSpeed[EnumLoadUnloadJogSpeed.Low] * 100).ToString("0.0"), ",",
                            axisData.PosLimit.ToString("0.0000"), ",",
                            axisData.NagLimit.ToString("0.0000")
                            ));
                }

                using (StreamWriter outputFile = new StreamWriter(path))
                {
                    for (int i = 0; i < outputList.Count; i++)
                        outputFile.WriteLine(outputList[i]);
                }

                using (StreamWriter outputFile = new StreamWriter(path_backup))
                {
                    for (int i = 0; i < outputList.Count; i++)
                        outputFile.WriteLine(outputList[i]);
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                configAllOK = false;
            }
        }

        private void ReadBarcodeIDToRealDistanceXML(XmlElement element)
        {
            string tempString;
            bool readDouble;
            double tempDouble = 0;

            foreach (XmlNode item2 in element.ChildNodes)
            {
                tempString = "";
                readDouble = false;

                foreach (XmlNode item in ((XmlElement)item2).ChildNodes)
                {
                    switch (item.Name)
                    {
                        case "BarcodeID":
                            tempString = item.InnerText;
                            break;
                        case "Locate":
                            readDouble = true;
                            tempDouble = double.Parse(item.InnerText);
                            break;
                        default:
                            break;
                    }
                }

                if (readDouble && tempString != "")
                {
                    if (BarcodeReaderIDToRealDistance.ContainsKey(tempString))
                        BarcodeReaderIDToRealDistance[tempString] = tempDouble;
                    else
                        BarcodeReaderIDToRealDistance.Add(tempString, tempDouble);
                }
            }
        }

        private Dictionary<string, double> ReadBarcodeIDToLocateXML(XmlElement element)
        {
            Dictionary<string, double> temp = new Dictionary<string, double>();

            string tempString;
            bool readDouble;
            double tempDouble = 0;


            foreach (XmlNode item2 in element.ChildNodes)
            {
                tempString = "";
                readDouble = false;

                foreach (XmlNode item in ((XmlElement)item2).ChildNodes)
                {
                    switch (item.Name)
                    {
                        case "BarcodeID":
                            tempString = item.InnerText;
                            break;
                        case "Locate":
                            readDouble = true;
                            tempDouble = double.Parse(item.InnerText);
                            break;
                        default:
                            break;
                    }
                }

                if (readDouble && tempString != "")
                {
                    if (temp.ContainsKey(tempString))
                        WriteLog(3, "", "BarcodeID repeat : ", tempString);
                    else
                        temp.Add(tempString, tempDouble);
                }
            }

            return temp;
        }

        private StageNumberToBarcodeReaderSetting ReadStargeNumberToBarcodeReaderSettingXML(XmlElement element)
        {
            StageNumberToBarcodeReaderSetting temp = new StageNumberToBarcodeReaderSetting();

            foreach (XmlNode item in element.ChildNodes)
            {
                switch (item.Name)
                {
                    case "StageNumber":
                        temp.StageNumber = Int32.Parse(item.InnerText);
                        break;
                    case "BarcodeReaderMode":
                        temp.BarcodeReaderMode = item.InnerText;
                        break;
                    case "PixelToMM_X":
                        temp.PixelToMM_X = double.Parse(item.InnerText);
                        break;
                    case "PixelToMM_Y":
                        temp.PixelToMM_Y = double.Parse(item.InnerText);
                        break;
                }
            }

            if (temp.StageNumber != -1)
                return temp;
            else
                return null;
        }

        private void ReadLeftOrRightStageNumberToBarcodeReaderSettingXML_L(XmlElement element)
        {
            StageNumberToBarcodeReaderSetting temp;

            foreach (XmlNode item in element.ChildNodes)
            {
                temp = ReadStargeNumberToBarcodeReaderSettingXML((XmlElement)item);

                if (LeftStageBarocdeReaderSetting.ContainsKey(temp.StageNumber))
                    LeftStageBarocdeReaderSetting[temp.StageNumber] = temp;
                else
                    LeftStageBarocdeReaderSetting.Add(temp.StageNumber, temp);
            }
        }

        private void ReadLeftOrRightStageNumberToBarcodeReaderSettingXML_R(XmlElement element)
        {
            StageNumberToBarcodeReaderSetting temp;

            foreach (XmlNode item in element.ChildNodes)
            {
                temp = ReadStargeNumberToBarcodeReaderSettingXML((XmlElement)item);

                if (RightStageBarocdeReaderSetting.ContainsKey(temp.StageNumber))
                    RightStageBarocdeReaderSetting[temp.StageNumber] = temp;
                else
                    RightStageBarocdeReaderSetting.Add(temp.StageNumber, temp);
            }
        }

        protected void ReadStageNumberToBarcodeReaderSettingXML()
        {
            string path = Path.Combine(localPath, localData.MainFlowConfig.AGVType.ToString(), "StageNumberToBarcodeReaderSetting.xml");

            try
            {
                XmlDocument doc = new XmlDocument();

                if (!File.Exists(path))
                {
                    WriteLog(1, "", "找不到StageNumberToBarcodeReaderSetting.xml.");
                    return;
                }

                doc.Load(path);

                XmlElement rootNode = doc.DocumentElement;

                foreach (XmlNode item in rootNode.ChildNodes)
                {
                    switch (item.Name)
                    {
                        case "BarcodeReaderIDToRealDistance":
                            ReadBarcodeIDToRealDistanceXML((XmlElement)item);
                            break;
                        case "Left":
                            ReadLeftOrRightStageNumberToBarcodeReaderSettingXML_L((XmlElement)item);
                            break;
                        case "Right":
                            ReadLeftOrRightStageNumberToBarcodeReaderSettingXML_R((XmlElement)item);
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                configAllOK = false;
            }
        }
        #endregion

        public virtual void LoadUnloadStart()
        {
        }

        public virtual bool ClearCommand()
        {
            return false;
        }

        public virtual void CheckAlingmentValue(EnumStageDirection direction, int stageNumber)
        {
        }

        public virtual void CheckAlingmentValueByAddressID(string addressID)
        {
        }

        public virtual void SwitchAlignmentValueByAddressID(string addressID)
        {
        }

        public virtual void Home()
        {
        }

        public virtual void Home_Initial()
        {
        }

        public virtual void FindHomeByAlignmentValue()
        {
        }

        public virtual void FindHomeSensorOffsetByEncoderInHome()
        {
        }

        public virtual void Jog(int indexOfAxis, bool direction)
        {
        }

        public virtual void Jog_相對(string axisName, double deltaEncoder)
        {
        }

        public virtual void Jog_NoSafety(string axisName, bool direction)
        {
        }

        public virtual void Jog_相對_NoSafety(string axisName, double deltaEncoder)
        {
        }

        public virtual bool JogStop()
        {
            return false;
        }

        public virtual void UpdateLoadingAndCSTID()
        {
        }

        public virtual void UpdateForkHomeStatus()
        {
        }

        public virtual void SetAlignmentDeviceToZero(EnumStageDirection direction)
        {
        }

        public virtual void WriteCSV()
        {
        }

        public virtual void SetAddressAlignmentOffset(string addressID)
        {
        }

        protected void ReadAddressOffsetCSV()
        {
            string path = Path.Combine(localData.MapConfig.FileDirectory, "AddressOffset.csv");

            if (!ReadAddresOffset(path))
            {
                WriteLog(5, "", String.Concat("讀取失敗"));

                path = Path.Combine(localData.MapConfig.FileDirectory, "AddressOffset_Backup.csv");

                if (!ReadAddresOffset(path))
                    WriteLog(5, "", String.Concat("Backup讀取失敗"));
            }
        }

        private bool ReadAddresOffset(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    WriteLog(5, "", "檔案不存在");
                    return false;
                }

                string[] allRows = File.ReadAllLines(path);

                string[] titleRow = allRows[0].Split(',');
                allRows = allRows.Skip(1).ToArray();

                Dictionary<string, int> dicHeaderIndexes = new Dictionary<string, int>();

                int nRows = allRows.Length;
                int nColumns = titleRow.Length;

                for (int i = 0; i < nColumns; i++)
                {
                    string keyword = titleRow[i].Trim();

                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        if (dicHeaderIndexes.ContainsKey(keyword))
                        {
                            WriteLog(3, "", String.Concat("Title : ", keyword, " repeat"));
                            return false;
                        }
                        else
                            dicHeaderIndexes.Add(keyword, i);
                    }
                }

                AddressAlignmentValueOffset temp;

                for (int i = 0; i < nRows; i++)
                {
                    string[] getThisRow = allRows[i].Split(',');
                    temp = new AddressAlignmentValueOffset();

                    temp.P = double.Parse(getThisRow[dicHeaderIndexes["P"]]);
                    temp.Y = double.Parse(getThisRow[dicHeaderIndexes["Y"]]);
                    temp.Z = double.Parse(getThisRow[dicHeaderIndexes["Z"]]);
                    temp.Theta = double.Parse(getThisRow[dicHeaderIndexes["Theta"]]);

                    if (!AllAddressOffset.ContainsKey(getThisRow[dicHeaderIndexes["AddressID"]]))
                        AllAddressOffset.Add(getThisRow[dicHeaderIndexes["AddressID"]], temp);
                }

                return true;
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }

        protected void WriteAddressOffsetCSV()
        {
            try
            {
                string path = Path.Combine(localData.MapConfig.FileDirectory, "AddressOffset.csv");

                string path_backup = Path.Combine(localData.MapConfig.FileDirectory, "AddressOffset_Backup.csv");
                List<string> writeList = new List<string>();
                writeList.Add("AddressID,P,Y,Theta,Z");

                foreach (var temp in AllAddressOffset)
                {
                    writeList.Add(
                        String.Concat(temp.Key, ",",
                        temp.Value.P.ToString("0.000"), ",",
                        temp.Value.Y.ToString("0.000"), ",",
                        temp.Value.Theta.ToString("0.000"), ",",
                        temp.Value.Z.ToString("0.000")
                        ));
                }

                using (StreamWriter outputFile = new StreamWriter(path))
                {
                    for (int i = 0; i < writeList.Count; i++)
                        outputFile.WriteLine(writeList[i]);
                }

                using (StreamWriter outputFile = new StreamWriter(path_backup))
                {
                    for (int i = 0; i < writeList.Count; i++)
                        outputFile.WriteLine(writeList[i]);
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }

        }

        protected void ReadLoadUnloadRobotCommandCSV()
        {
            string path = Path.Combine(localPath, localData.MainFlowConfig.AGVType.ToString(), "LoadUnloadRobotCommand.csv");

            try
            {
                string[] allRows = File.ReadAllLines(path);

                if (allRows == null || allRows.Length < 1)
                {
                    WriteLog(5, "", String.Concat("LoadUnloadRobotCommand.csv line == 0"));
                    return;
                }

                string[] titleRow = allRows[0].Split(',');
                allRows = allRows.Skip(1).ToArray();

                int nRows = allRows.Length;
                int nColumns = titleRow.Length;

                Dictionary<string, int> dicHeaderIndexes = new Dictionary<string, int>();

                for (int i = 0; i < nColumns; i++)
                {
                    var keyword = titleRow[i].Trim();

                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        if (dicHeaderIndexes.ContainsKey(keyword))
                            WriteLog(3, "", String.Concat("Title repeat : ", keyword));
                        else
                            dicHeaderIndexes.Add(keyword, i);
                    }
                }
                //未實作處理欄位不符合錯誤

                for (int i = 0; i < nRows; i++)
                {
                    LoadUnloadRobotCommand temp = new LoadUnloadRobotCommand();

                    string[] getThisRow = allRows[i].Split(',');
                    if (getThisRow.Length != dicHeaderIndexes.Count)
                        WriteLog(3, "", String.Concat("line : ", (i + 2).ToString(), " 和Title數量不吻合"));
                    else
                    {

                        temp.Id = getThisRow[dicHeaderIndexes["Id"]];
                        temp.Name = getThisRow[dicHeaderIndexes["Name"]];
                        temp.Port1Load = float.Parse(getThisRow[dicHeaderIndexes["Port1Load"]]);
                        temp.Port1Unload = float.Parse(getThisRow[dicHeaderIndexes["Port1Unload"]]);
                        temp.Port2Load = float.Parse(getThisRow[dicHeaderIndexes["Port2Load"]]);
                        temp.Port2Unload = float.Parse(getThisRow[dicHeaderIndexes["Port2Unload"]]);
                        if (!localData.LoadUnloadData.RobotCommand.ContainsKey(temp.Id))
                        {
                            localData.LoadUnloadData.RobotCommand.Add(temp.Id, temp);
                        }
                        else
                        {
                            WriteLog(3, "", String.Concat("LoadUnloadRobotCommandCSV重複ID:" + temp.Id));
                        }
                    }
                }

                WriteLog(7, "", String.Concat("LoadUnloadRobotCommandCSV載入成功"));
            }
            catch (Exception ex)
            {


            }

        }

        protected void ReadPIOTimeoutCSV()
        {
            string path = Path.Combine(localPath, localData.MainFlowConfig.AGVType.ToString(), "PIOTimeout.csv");

            if (!ReadPIOTimeoutCSV(path))
            {
                WriteLog(5, "", String.Concat("讀取失敗"));

                path = Path.Combine(localPath, localData.MainFlowConfig.AGVType.ToString(), "PIOTimeout_Backup.csv");

                if (!ReadPIOTimeoutCSV(path))
                    WriteLog(5, "", String.Concat("Backup讀取失敗"));
            }
        }

        private bool ReadPIOTimeoutCSV(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    WriteLog(5, "", "檔案不存在");
                    return false;
                }

                string[] allRows = File.ReadAllLines(path);

                Dictionary<string, int> dicHeaderIndexes = new Dictionary<string, int>();

                int nRows = allRows.Length;

                EnumPIOStatus yo;

                for (int i = 0; i < nRows; i++)
                {
                    string[] getThisRow = allRows[i].Split(',');

                    if (getThisRow.Length != 2)
                    {
                        WriteLog(3, "", "資料應該為兩筆, timeout Tag 以及 timeout時間");
                        return false;
                    }

                    if (Enum.TryParse(getThisRow[0], out yo))
                    {
                        localData.LoadUnloadData.PIOTimeoutTageList.Add(yo);
                        localData.LoadUnloadData.PIOTimeoutList.Add(double.Parse(getThisRow[1]));
                    }
                    else
                    {
                        WriteLog(3, "", "Timeout tag 轉換失敗 GG私密打");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }

        public virtual void LoadUnloadPreAction()
        {
        }

        public void WritePIOTimeoutCSV()
        {
            try
            {
                string path = Path.Combine(localPath, localData.MainFlowConfig.AGVType.ToString(), "PIOTimeout.csv");

                string path_backup = Path.Combine(localPath, localData.MainFlowConfig.AGVType.ToString(), "PIOTimeout_Backup.csv");


                if (localData.LoadUnloadData.PIOTimeoutList.Count != localData.LoadUnloadData.PIOTimeoutTageList.Count)
                    return;

                List<string> writeList = new List<string>();

                for (int i = 0; i < localData.LoadUnloadData.PIOTimeoutList.Count; i++)
                    writeList.Add(String.Concat(localData.LoadUnloadData.PIOTimeoutTageList[i].ToString(), ",",
                                                localData.LoadUnloadData.PIOTimeoutList[i].ToString("0")));

                using (StreamWriter outputFile = new StreamWriter(path))
                {
                    for (int i = 0; i < writeList.Count; i++)
                        outputFile.WriteLine(writeList[i]);
                }

                using (StreamWriter outputFile = new StreamWriter(path_backup))
                {
                    for (int i = 0; i < writeList.Count; i++)
                        outputFile.WriteLine(writeList[i]);
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }
    }
}