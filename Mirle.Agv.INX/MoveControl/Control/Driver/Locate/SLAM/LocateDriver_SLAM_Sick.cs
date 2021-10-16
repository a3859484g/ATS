using Mirle.Agv.INX.Controller;
using Mirle.Agv.INX.Controller.Tools;
using Mirle.Agv.INX.Model;
using Mirle.Agv.INX.Model.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Net;

namespace Mirle.Agv.INX.Controller
{
    public class LocateDriver_SLAM_Sick : LocateDriver_SLAM
    {
        private LocateDriver_SLAM_SickConfig config = new LocateDriver_SLAM_SickConfig();

        private Socket sendCommandSocket = null;
        private Socket getDataSocket = null;

        private uint count = 0;

        private string startByte = "\x2";
        private string endByte = "\x3";

        //private string ask = "sRN";
        //private string askFeedback = "sRA";
        private string cmd = "sMN";
        private string cmdFeedback = "sAN";
        private Dictionary<string, double> lastEncoder = new Dictionary<string, double>();

        private TimeStampData timeStampData = null;
        private const long overflowValue = 4294967296;

        #region Read XML.
        private bool ReadXML(string path)
        {
            if (path == null || path == "")
            {
                WriteLog(3, "", String.Concat(device, "Config 路徑錯誤為null或空值."));
                return false;
            }
            else if (!File.Exists(path))
            {
                WriteLog(3, "", String.Concat("找不到 ", device, "Config.xml."));
                return false;
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);
                var rootNode = doc.DocumentElement;
                config.TransferAddressFileName = "SLAM_SickAddress.csv";

                foreach (XmlNode item in rootNode.ChildNodes)
                {
                    switch (item.Name)
                    {
                        case "ID":
                            config.ID = item.InnerText;
                            break;
                        case "IP":
                            config.IP = item.InnerText;
                            break;
                        case "TransferAddressFileName":
                            config.TransferAddressFileName = item.InnerText;
                            break;
                        case "CommandPort":
                            config.CommandPort = Int32.Parse(item.InnerText);
                            break;
                        case "SleepTime":
                            config.SleepTime = Int32.Parse(item.InnerText);
                            break;
                        case "PercentageStandard":
                            config.PercentageStandard = Int32.Parse(item.InnerText);
                            break;
                        case "FeedbackPort":
                            config.FeedbackPort = Int32.Parse(item.InnerText);
                            break;
                        case "LogMode":
                            config.LogMode = bool.Parse(item.InnerText);
                            break;
                        case "UseOdometry":
                            config.UseOdometry = bool.Parse(item.InnerText);
                            break;
                        case "SetPositionRange":
                            config.SetPositionRange = double.Parse(item.InnerText);
                            break;
                        case "SetPositionForceUpdateCount":
                            config.SetPositionForceUpdateCount = Int32.Parse(item.InnerText);
                            break;
                        case "SectionDistanceMagnification":
                            config.SectionDistanceMagnification = double.Parse(item.InnerText);
                            break;
                        case "SectionDistanceConstant":
                            config.SectionDistanceConstant = double.Parse(item.InnerText);
                            break;
                        default:
                            break;
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
        #endregion

        private bool CheckSumOK(byte[] data)
        {
            return data[0] == 0x53 && data[1] == 0x49 && data[2] == 0x43 && data[3] == 0x4B;
        }


        override protected void InitialConfig(string path)
        {
            if (ReadXML(path) && ReadSLAMAddress(Path.Combine(localData.MapConfig.FileDirectory, config.TransferAddressFileName)))
            {
                CheckSectionDistance(config.SectionDistanceMagnification, config.SectionDistanceConstant);
                status = EnumControlStatus.Initial;

                logger = LoggerAgent.Instance.GetLooger(config.ID);
            }
            else
                SetAlarmCode(EnumMoveCommandControlErrorCode.LocateDriver_SLAM_初始化失敗);
        }

        override public void CloseDriver()
        {
            if (Status == EnumControlStatus.NotInitial || Status == EnumControlStatus.Initial)
                return;

            WriteLog(7, "", "CloseDriver!");

            resetAlarm = false;

            status = EnumControlStatus.Closing;

            try
            {
                sendCommandSocket.Close();
                getDataSocket.Close();
            }
            catch
            {
            }
        }

        override public void ResetAlarm()
        {
            switch (Status)
            {
                case EnumControlStatus.Initial:
                    ConnectDriver();
                    break;
                default:
                    break;
            }
        }

        #region 送/收資料.
        private string GetFeedbackString(byte[] receiveBytes)
        {
            if (receiveBytes == null)
                return "";

            int start = -1;
            int end = -1;

            for (int i = 0; i < receiveBytes.Length; i++)
            {
                if (receiveBytes[i] == startByte[0])
                    start = i;
                else if (receiveBytes[i] == endByte[0])
                    end = i;

                if (start != -1 && end != -1)
                    break;
            }

            if (start != -1 && end != -1 && end > start)
                return System.Text.Encoding.ASCII.GetString(receiveBytes, start + 1, end - 1);
            else
                return "";

            ///300
        }

        private string[] SendCommandAndGetReceiveString(Socket socket, string command)
        {
            try
            {
                string commandString = String.Concat(startByte, command, endByte);
                WriteLog(7, "", String.Concat("Socket Command : ", commandString));

                byte[] ASCIIbytes = Encoding.ASCII.GetBytes(commandString);
                byte[] receiveBytes = new byte[256];
                socket.Send(ASCIIbytes, 0, ASCIIbytes.GetLength(0), SocketFlags.None);
                socket.ReceiveTimeout = 1000;
                socket.Receive(receiveBytes, 0, 256, SocketFlags.None);

                string reciveString = GetFeedbackString(receiveBytes);
                WriteLog(7, "", String.Concat("Receive Data : ", startByte.ToString(), reciveString, endByte.ToString()));

                string[] splitResult = Regex.Split(reciveString, " ", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));
                return splitResult;
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Socket Exception : ", ex.ToString()));
                return null;
            }
        }
        #endregion

        private bool ConnectToSLAMSick()
        {
            if (Status == EnumControlStatus.Initial)
            {
                // 之後要再修改.. 需要判定起頭特殊字元x02結束字元x03等等資料...
                if (ConnectToSick() && Command_Start() && Command_SetMode() && SetTimeStamp() && StartThread())
                    status = EnumControlStatus.Ready;
            }

            return true;
        }

        override public void ConnectDriver()
        {
            if (Status != EnumControlStatus.NotInitial)
            {
                if (ConnectToSLAMSick())
                    ResetAlarmCode(EnumMoveCommandControlErrorCode.LocateDriver_SLAM_連線失敗);
                else
                    SetAlarmCode(EnumMoveCommandControlErrorCode.LocateDriver_SLAM_連線失敗);
            }
        }

        private bool ConnectToSick()
        {
            try
            {
                sendCommandSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.sendCommandSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 500);

                this.sendCommandSocket.Connect(IPAddress.Parse(config.IP), config.CommandPort);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #region Sick Command.
        private bool Command_Start()
        {
            try
            {
                string command = "LocStartLocalizing";
                string[] reciveData = SendCommandAndGetReceiveString(sendCommandSocket, String.Concat(cmd, " ", command));

                if (reciveData == null || reciveData.Length == 1)
                    return false;

                if (reciveData.Length != 3)
                    return false;
                else
                {
                    if (reciveData[0] == cmdFeedback &&
                        reciveData[1] == command)
                    {
                        if (reciveData[2] == "1" || reciveData[2] == "0")
                            return true;
                        else
                            return false;
                    }
                    else
                        return false;
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Socket Exception : ", ex.ToString()));
                return false;
            }
        }

        private bool Command_Close()
        {
            try
            {
                string command = "LocStop ";
                string[] reciveData = SendCommandAndGetReceiveString(sendCommandSocket, String.Concat(cmd, " ", command));

                if (reciveData == null || reciveData.Length == 1)
                    return false;

                if (reciveData.Length != 3)
                    return false;
                else
                {
                    if (reciveData[0] == cmdFeedback &&
                        reciveData[1] == command)
                    {
                        if (reciveData[2] == "1")
                            return true;
                        else
                            return false;
                    }
                    else
                        return false;
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Socket Exception : ", ex.ToString()));
                return false;
            }
        }

        private bool Command_SetMode()
        {
            try
            {
                string command = "LocSetResultMode";
                string mode = "0";
                string[] reciveData = SendCommandAndGetReceiveString(sendCommandSocket, String.Concat(cmd, " ", command, " ", mode));

                if (reciveData == null || reciveData.Length == 1)
                    return false;

                if (reciveData.Length != 3)
                    return false;
                else
                {
                    if (reciveData[0] == cmdFeedback &&
                        reciveData[1] == command)
                    {
                        if (reciveData[2] == "1")
                            return true;
                        else
                            return false;
                    }
                    else
                        return false;
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Socket Exception : ", ex.ToString()));
                return false;
            }
        }

        public bool Command_LocForceUpdate()
        {
            try
            {
                string command = "LocForceUpdate";
                string[] reciveData = SendCommandAndGetReceiveString(sendCommandSocket, String.Concat(cmd, " ", command));

                if (reciveData.Length != 3)
                    return false;
                else
                {
                    if (reciveData[0] == cmdFeedback &&
                        reciveData[1] == command)
                    {
                        if (reciveData[2] == "1")
                            return true;
                        else
                            return false;
                    }
                    else
                        return false;
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Socket Exception : ", ex.ToString()));
                return false;
            }
        }

        private bool Command_SetPosition(MapAGVPosition now, double distanceRange)
        {
            try
            {
                if (now == null || now.Position == null)
                    return false;

                string command = "LocSetPose";
                string x = String.Concat((now.Position.X > 0 ? "+" : ""), now.Position.X.ToString("0"));
                string y = String.Concat((now.Position.Y > 0 ? "+" : ""), now.Position.Y.ToString("0"));
                string theta = String.Concat((now.Angle > 0 ? "+" : ""), (now.Angle * 1000).ToString("0"));

                string range;

                if (distanceRange > config.SetPositionRange)
                    range = String.Concat("+", distanceRange.ToString("0"));
                else
                    range = String.Concat("+", config.SetPositionRange.ToString("0"));

                string[] reciveData = SendCommandAndGetReceiveString(sendCommandSocket, String.Concat(cmd, " ", command, " ", x, " ", y, " ", theta, " ", range));

                if (reciveData.Length != 3)
                    return false;
                else
                {
                    if (reciveData[0] == cmdFeedback &&
                        reciveData[1] == command)
                    {
                        if (reciveData[2] == "1")
                            return true;
                        else
                            return false;
                    }
                    else
                        return false;
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Socket Exception : ", ex.ToString()));
                return false;
            }
        }
        #endregion

        #region Byte to data.
        private Int32 ByteToInt32(byte[] data, int index, EnumByteChangeType type)
        {
            if (type == EnumByteChangeType.LittleEndian)
                return BitConverter.ToInt32(data, index);
            else
            {
                byte[] byteData = new byte[4] { data[index + 3], data[index + 2], data[index + 1], data[index] };
                return BitConverter.ToInt32(byteData, 0);
            }
        }

        private UInt32 ByteToUInt32(byte[] data, int index, EnumByteChangeType type)
        {
            if (type == EnumByteChangeType.LittleEndian)
                return BitConverter.ToUInt32(data, index);
            else
            {
                byte[] byteData = new byte[4] { data[index + 3], data[index + 2], data[index + 1], data[index] };
                return BitConverter.ToUInt32(byteData, 0);
            }
        }

        private UInt16 ByteToUInt16(byte[] data, int index, EnumByteChangeType type)
        {
            if (type == EnumByteChangeType.LittleEndian)
                return BitConverter.ToUInt16(data, index);
            else
            {
                byte[] byteData = new byte[2] { data[index + 1], data[index] };
                return BitConverter.ToUInt16(byteData, 0);
            }
        }
        #endregion

        #region GetData/GetDataThread.
        private void GetData()
        {
            byte[] ReceiveBytes = new byte[256];

            try
            {
                this.getDataSocket.Receive(ReceiveBytes, 0, 256, SocketFlags.None);

                if (!CheckSumOK(ReceiveBytes))
                {
                    WriteLog(3, "", String.Concat("CRC16 CheckSum Error : ", BitConverter.ToString(ReceiveBytes)));
                    originAGVPosition = null;
                    nowAGVPosition = null;
                }
                else
                {
                    EnumByteChangeType byteType = (ReceiveBytes[8] == 0x06 && ReceiveBytes[8] == 0xC2) ? EnumByteChangeType.LittleEndian : EnumByteChangeType.BigEndian;
                    DateTime now = DateTime.Now;
                    double scanTime = GetScanTimeAndProcessOverflow(now, ReceiveBytes, byteType);
                    Int32 x = ByteToInt32(ReceiveBytes, 62, byteType);
                    Int32 y = ByteToInt32(ReceiveBytes, 66, byteType);
                    Int32 theta = ByteToInt32(ReceiveBytes, 70, byteType);
                    MapPosition position = new MapPosition((double)x, (double)y);
                    int percent = (int)ReceiveBytes[82];

                    LocateAGVPosition newLocateAGVPosition = new LocateAGVPosition(position, (double)theta / 1000, percent, (int)scanTime, DateTime.Now, count, EnumAGVPositionType.Normal, device, DriverConfig.Order);

                    MapAGVPosition newAGVPosition = new MapAGVPosition(position, (double)theta / 1000);

                    int errorCode = ByteToUInt16(ReceiveBytes, 52, byteType);

                    if (errorCode != 0)
                    {
                        WriteLog(5, "", String.Concat("ErrorCode : ", errorCode.ToString()));
                        nowAGVPosition = null;
                    }
                    else if (percent == 0 && x == 0 && y == 0 && theta == 0)
                    {
                        WriteLog(3, "", "All Zero!");
                        nowAGVPosition = null;
                    }

                    if (percent < config.PercentageStandard)
                    {
                        if (localData.MoveControlData.LocateControlData.SlamLocateOK)
                        {
                            WriteLog(3, "", String.Concat("信心度 : ", percent.ToString(), ",  設為迷航"));
                            localData.MoveControlData.LocateControlData.SlamLocateOK = false;
                        }

                        SetAlarmCode(EnumMoveCommandControlErrorCode.LocateDriver_SLAM_信心度低下);
                    }
                    else
                        ResetAlarmCode(EnumMoveCommandControlErrorCode.LocateDriver_SLAM_信心度低下);

                    if (localData.MoveControlData.LocateControlData.SlamLocateOK)
                    {
                        if (setPositionEnd)
                            setPositionEnd = false;
                        else
                        {
                            if (lastOriginPosition != null)
                            {
                                if (!setPositioning &&
                                    computeFunction.GetDistanceFormTwoAGVPosition(lastOriginPosition, newAGVPosition) > 1000 ||
                                    Math.Abs(computeFunction.GetCurrectAngle(lastOriginPosition.Angle - newAGVPosition.Angle)) > 30)
                                {
                                    WriteLog(7, "", "飄動超過1M或角度誤差過大 SlamLocateOK = false");
                                    localData.MoveControlData.LocateControlData.SlamLocateOK = false;
                                    SetAlarmCode(EnumMoveCommandControlErrorCode.LocateDriver_SLAM_定位資料位移過大);
                                }

                                if (localData.MoveControlData.LocateControlData.SlamLocateOK)
                                    ResetAlarmCode(EnumMoveCommandControlErrorCode.LocateDriver_SLAM_定位資料位移過大);
                            }
                        }
                    }

                    if (!changeSlamPosition && PollingOnOff && newLocateAGVPosition.AGVPosition != null &&
                        localData.MoveControlData.LocateControlData.SlamLocateOK)
                    {
                        originAGVPosition = newLocateAGVPosition;
                        SLAMPositionOffset();
                        TransferToMapPosition(config.SectionRange);
                    }
                    else if (!localData.MoveControlData.LocateControlData.SlamLocateOK)
                    {
                        originAGVPosition = newLocateAGVPosition;
                        SLAMPositionOffset();
                        nowAGVPosition = null;
                    }
                    else
                    {
                        originAGVPosition = null;
                        nowAGVPosition = null;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Socket Exception : ", ex.ToString()));
                originAGVPosition = null;
                nowAGVPosition = null;
            }

            WriteCSV();
            count++;
        }

        private void GetDataThread()
        {
            TimeStampData nowTimStampData = null;

            pollingTimer.Restart();

            while (Status != EnumControlStatus.Closing)
            {
                GetData();

                nowTimStampData = timeStampData;

                if (nowTimStampData != null && nowTimStampData.UpdateTime.Day != DateTime.Now.Day)
                    SetTimeStamp();

                #region 未取得定位資料/精度迷航 Set/Reset Alarm.
                if (!setPositioning)
                {
                    if (pollingTimer.ElapsedMilliseconds > 200)
                    {
                        originAGVPosition = null;
                        offsetAGVPosition = null;
                        nowAGVPosition = null;
                        localData.MoveControlData.LocateControlData.SlamLocateOK = false;
                        SetAlarmCode(EnumMoveCommandControlErrorCode.LocateDriver_SLAM_未取得定位資料);
                    }
                    else
                    {
                        ResetAlarmCode(EnumMoveCommandControlErrorCode.LocateDriver_SLAM_未取得定位資料);
                    }
                }

                if (!localData.MoveControlData.LocateControlData.SlamLocateOK)
                    SetAlarmCode(EnumMoveCommandControlErrorCode.LocateDriver_SLAM_精度迷航);
                else
                    ResetAlarmCode(EnumMoveCommandControlErrorCode.LocateDriver_SLAM_精度迷航);
                #endregion

                Thread.Sleep(config.SleepTime);
            }

            pollingTimer.Reset();
            status = EnumControlStatus.Closed;
        }
        #endregion

        #region 計算TimeStamp.
        private double GetScanTimeAndProcessOverflow(DateTime now, byte[] ReceiveBytes, EnumByteChangeType byteType)
        {
            TimeStampData nowTimeStampData = timeStampData;

            double scanTime = (now - nowTimeStampData.GetTime).TotalMilliseconds - ByteToUInt32(ReceiveBytes, 58, byteType);

            while (scanTime > overflowValue)
            {
                WriteLog(7, "", "simTimeStamp_GetData overflow!");
                nowTimeStampData.GetTime = nowTimeStampData.GetTime.AddMilliseconds(overflowValue);
                scanTime -= overflowValue;
            }

            if (scanTime < 0)
            {
                WriteLog(1, "", "TimeStampError < 0!");
                return 0;
            }
            else if (scanTime > 100)
            {
                WriteLog(1, "", "TimeStampError > 100!");
                return 0;
            }

            return scanTime;
        }

        private bool Command_RequestTimestamp(ref UInt32 timeStamp)
        {
            try
            {
                string command = "LocRequestTimestamp";
                string[] reciveData = SendCommandAndGetReceiveString(sendCommandSocket, String.Concat(cmd, " ", command));

                if (reciveData.Length != 3)
                    return false;
                else
                {
                    if (reciveData[0] == cmdFeedback &&
                        reciveData[1] == command)
                    {
                        timeStamp = UInt32.Parse(reciveData[2], System.Globalization.NumberStyles.HexNumber);

                        return true;
                    }
                    else
                        return false;
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Socket Exception : ", ex.ToString()));
                return false;
            }
        }

        private bool SetTimeStamp()
        {
            DateTime beforeSend = DateTime.Now;
            UInt32 simTimeStamp = 0;

            if (!Command_RequestTimestamp(ref simTimeStamp))
                return false;

            DateTime afterSend = DateTime.Now;

            double socketTime = ((afterSend - beforeSend).TotalMilliseconds / 2);

            //WriteLog(7, "", String.Concat("beforeSend : ", beforeSend.ToString("HH:mm:ss.fff")));
            //WriteLog(7, "", String.Concat("afterSend : ", afterSend.ToString("HH:mm:ss.fff")));
            //WriteLog(7, "", String.Concat("Sim TimeStap : ", simTimeStamp.ToString()));

            TimeStampData newTimeStampData = new TimeStampData();

            newTimeStampData.Time = afterSend.AddMilliseconds(-socketTime - simTimeStamp);
            newTimeStampData.GetTime = newTimeStampData.Time;
            newTimeStampData.SendTime = newTimeStampData.Time;

            TimeStampData oldTimeStampData = timeStampData;

            if (oldTimeStampData == null)
                WriteLog(7, "", String.Concat("newTimeStamp : ", newTimeStampData.Time.ToString("HH:mm:ss.fff")));
            else
                WriteLog(7, "", String.Concat("newTimeStamp : ", newTimeStampData.Time.ToString("HH:mm:ss.fff"),
                                              ", delta time : ", (oldTimeStampData.GetTime - newTimeStampData.GetTime).TotalMilliseconds, " ms"));

            timeStampData = newTimeStampData;
            return true;
        }
        #endregion

        #region 重定位以及確認資料.
        private bool StartThread()
        {
            try
            {
                getDataSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.getDataSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 500);

                this.getDataSocket.Connect(config.IP, config.FeedbackPort);

                pollingThread = new Thread(GetDataThread);
                pollingThread.Start();
                return true;
            }
            catch
            {
                return false;
            }
        }

        protected override bool SetPositionAndWait(MapAGVPosition setPosition, double distanceRange, double angleRange, ref string errorMessage)
        {
            setPositioning = true;
            WriteLog(7, "", "SetPosition Start");

            LocateAGVPosition locateTemp = offsetAGVPosition;
            MapAGVPosition temp = (locateTemp == null ? null : locateTemp.AGVPosition);

            double tempDistance = 0;
            double tempAngle = 0;

            if (temp != null)
            {
                tempDistance = computeFunction.GetDistanceFormTwoAGVPosition(setPosition, temp);
                tempAngle = Math.Abs(computeFunction.GetCurrectAngle(setPosition.Angle - temp.Angle));

                if (tempDistance < distanceRange && tempAngle < angleRange)
                {
                    localData.MoveControlData.ReviseAndSetPositionData = String.Concat(tempDistance.ToString("0"), "mm/", tempAngle.ToString("0.0"), "°");
                    localData.MoveControlData.LocateControlData.SlamLocateOK = true;
                    setPositioning = false;
                    WriteLog(7, "", localData.MoveControlData.ReviseAndSetPositionData);
                    WriteLog(7, "", "目前就在此點上,略過SetPosition直接認可位置");
                    WriteLog(7, "", "SetPosition End");
                    return true;
                }
            }

            bool returnBoolean = false;

            if (Command_SetPosition(GetOrigionPositionBySlamPositionAndOffsetData(setPosition), distanceRange))
            {
                for (int i = 0; i < config.SetPositionForceUpdateCount; i++)
                {
                    Command_LocForceUpdate();
                    Thread.Sleep(100);
                }

                locateTemp = offsetAGVPosition;
                temp = (locateTemp == null ? null : locateTemp.AGVPosition);

                if (temp != null)
                {
                    tempDistance = computeFunction.GetDistanceFormTwoAGVPosition(setPosition, temp);
                    tempAngle = Math.Abs(computeFunction.GetCurrectAngle(setPosition.Angle - temp.Angle));

                    localData.MoveControlData.ReviseAndSetPositionData = String.Concat(tempDistance.ToString("0"), "mm/", tempAngle.ToString("0.0"), "°");
                    WriteLog(7, "", localData.MoveControlData.ReviseAndSetPositionData);

                    if (tempDistance < distanceRange && tempAngle < angleRange)
                        returnBoolean = true;
                }

                if (!returnBoolean)
                    errorMessage = "SlamPosition 和Set數值差異過大且等不到相近";
            }
            else
            {
                localData.MoveControlData.ReviseAndSetPositionData = "Sick 重定位失敗";
                errorMessage = "Command_SetPosition return false";
            }

            localData.MoveControlData.LocateControlData.SlamLocateOK = returnBoolean;
            WriteLog(7, "", String.Concat("結果 = ", returnBoolean.ToString()));
            WriteLog(7, "", "SetPosition End");
            setPositioning = false;
            setPositionEnd = true;
            return returnBoolean;
        }
        #endregion

        #region WriteCSV.
        private void WriteCSV()
        {
            string csvLog = "";

            if (config.LogMode)
            {
                if (originAGVPosition == null)
                    csvLog = String.Concat(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), ",", count.ToString(), ",,,,");
                else
                    csvLog = String.Concat(originAGVPosition.GetDataTime.ToString("yyyy/MM/dd HH:mm:ss.fff"), ",",
                                           originAGVPosition.Count.ToString(), ",", originAGVPosition.AGVPosition.Position.X.ToString("0.0"), ",",
                                           originAGVPosition.AGVPosition.Position.Y.ToString("0.0"), ",", originAGVPosition.AGVPosition.Angle.ToString("0.00"), ",",
                                           originAGVPosition.ScanTime.ToString());

                if (nowAGVPosition == null)
                    csvLog = String.Concat(csvLog, ",,,");
                else
                    csvLog = String.Concat(csvLog, ",", nowAGVPosition.AGVPosition.Position.X.ToString("0.0"), ",",
                                                        nowAGVPosition.AGVPosition.Position.Y.ToString("0.0"), ",", nowAGVPosition.AGVPosition.Angle.ToString("0.00"));

                if (originAGVPosition == null)
                    csvLog = String.Concat(csvLog, ",");
                else
                    csvLog = String.Concat(csvLog, ",", originAGVPosition.Value);

                logger.LogString(csvLog);
            }
        }
        #endregion
    }
}