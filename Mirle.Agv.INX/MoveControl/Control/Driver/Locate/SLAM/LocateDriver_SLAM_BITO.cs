using Mirle.Agv.INX.Controller.Tools;
using Mirle.Agv.INX.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace Mirle.Agv.INX.Controller
{
    public class LocateDriver_SLAM_BITO : LocateDriver_SLAM
    {
        private LocateDriver_SLAM_SickConfig config = new LocateDriver_SLAM_SickConfig();

        private Socket socketPort19000 = null;
        private Socket socketPort19001 = null;
        private Socket socketPort19002 = null;

        private MapAGVPosition BitoData = null;

        private int BITO_Status = 0;
        private string BITO_StatusString = "";

        private double BITO_Confidence = 0;

        private uint count = 0;
        private Thread pingThread = null;

        private uint count_LocatePackage = 0;
        private uint count_信心度Package = 0;
        private uint count_StatusPackage = 0;

        private DataDelayAndChange 信心度訊號含延遲 = new DataDelayAndChange(5000, EnumDelayType.OffDelay);

        ///╔══════╦══════════╦═══╦═══════════╗
        ///║    名稱    ║        內容        ║ byte ║        描述          ║
        ///╠══════╬══════════╬═══╬═══════════╣
        ///║   Header   ║0xAA 0x55 0xAA 0x55 ║   4  ║       固定內容       ║
        ///╠══════╬══════════╬═══╬═══════════╣
        ///║  協議版本  ║        0x01        ║   1  ║      協議版本號      ║
        ///╠══════╬══════════╬═══╬═══════════╣
        ///║ 協定頭長度 ║        0x14        ║   1  ║協議頭長度取決協議版本║
        ///╠══════╬══════════╬═══╬═══════════╣
        ///║   SeqNo    ║                    ║   2  ║        流水號        ║
        ///╠══════╬══════════╬═══╬═══════════╣
        ///║Command Type║ EnumSLAMBITOCommand║   1  ║協議頭長度取決協議版本║
        ///╠══════╬══════════╬═══╬═══════════╣
        ///║ Data Length║                    ║   4  ║     取決Data長度     ║
        ///╠══════╬══════════╬═══╬═══════════╣
        ///║   保留區   ║                    ║   6  ║         空白         ║
        ///╠══════╬══════════╬═══╬═══════════╣
        ///║    Data    ║                    ║ 不定 ║         資料         ║
        ///╠══════╬══════════╬═══╬═══════════╣
        ///║     CRC    ║                    ║   4  ║         資料         ║
        ///╚══════╩══════════╩═══╩═══════════╝

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
                config.TransferAddressFileName = "BITOAddress.csv";

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
                        case "FeedbackPort":
                            config.FeedbackPort = Int32.Parse(item.InnerText);
                            break;
                        case "LogMode":
                            config.LogMode = bool.Parse(item.InnerText);
                            break;
                        case "UseOdometry":
                            config.UseOdometry = bool.Parse(item.InnerText);
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
                socketPort19000.Dispose();
                socketPort19001.Dispose();
                socketPort19002.Dispose();
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
                case EnumControlStatus.Error:
                    break;
                default:
                    break;
            }
        }

        #region 送/收資料.

        private bool SendCommand(Socket socket, Byte[] byteArray)
        {
            try
            {
                WriteLog(7, "", String.Concat("Send Command :\r\n", BitConverter.ToString(byteArray)));
                socket.Send(byteArray, 0, byteArray.GetLength(0), SocketFlags.None);
                return true;
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Socket Exception : ", ex.ToString()));
                return false;
            }
        }

        private bool ReceiveCommand(Socket socket)
        {
            try
            {
                byte[] receiveData = new byte[1024];

                socket.ReceiveTimeout = 30000;

                socket.Receive(receiveData, 1024, SocketFlags.None);

                string textData = System.Text.Encoding.ASCII.GetString(receiveData);
                textData = textData.Substring(textData.IndexOf("{"), (textData.IndexOf("}") - textData.IndexOf("{")) + 1);
                Dictionary<string, string> Jsonstring = JsonConvert.DeserializeObject<Dictionary<string, string>>(textData);

                return Jsonstring.ContainsKey("confirm") && Jsonstring["confirm"] == "true";
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("ReceiveCommand Exception : ", ex.ToString()));
                return false;
            }
        }
        #endregion

        private bool ConnectAndStart()
        {
            if (Status == EnumControlStatus.Initial)
            {
                if (Connect_19000() && Connect_19001() /*&& Connect_19002()*/ && Command_SendGetAngle() && Command_SendGetStatus() && Command_SendGetPosition() && StartThread())
                {
                    status = EnumControlStatus.Ready;
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }

        override public void ConnectDriver()
        {
            if (Status != EnumControlStatus.NotInitial)
            {
                if (ConnectAndStart())
                    ResetAlarmCode(EnumMoveCommandControlErrorCode.LocateDriver_SLAM_連線失敗);
                else
                    SetAlarmCode(EnumMoveCommandControlErrorCode.LocateDriver_SLAM_連線失敗);
            }
        }

        #region Connect Socket.
        private bool connect_19000 = false;
        private bool connect_19001 = false;
        private bool connect_19002 = false;

        private SocketException lastSocketException_19000 = null;
        private SocketException lastSocketException_19001 = null;
        private SocketException lastSocketException_19002 = null;

        private Exception lastException_19000 = null;
        private Exception lastException_19001 = null;
        private Exception lastException_19002 = null;

        private bool Connect_19000()
        {
            try
            {
                if (connect_19000)
                    return true;

                socketPort19000 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.socketPort19000.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 5000);

                this.socketPort19000.Connect(IPAddress.Parse(config.IP), 19000);
                connect_19000 = true;
                return true;
            }
            catch (SocketException ex)
            {
                if (lastSocketException_19000 == null || lastSocketException_19000 != ex)
                    WriteLog(5, "", String.Concat("Socke Exception : ", ex.ToString()));

                lastSocketException_19000 = ex;
                return false;
            }
            catch (Exception ex)
            {
                if (lastException_19000 == null || lastException_19000 != ex)
                    WriteLog(5, "", String.Concat("Exception : ", ex.ToString()));

                lastException_19000 = ex;
                return false;
            }
        }

        private bool Connect_19001()
        {
            try
            {
                if (connect_19001)
                    return true;

                socketPort19001 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.socketPort19001.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 5000);

                this.socketPort19001.Connect(IPAddress.Parse(config.IP), 19001);
                connect_19001 = true;
                return true;
            }
            catch (SocketException ex)
            {
                if (lastSocketException_19001 == null || lastSocketException_19001 != ex)
                    WriteLog(5, "", String.Concat("Socke Exception : ", ex.ToString()));

                lastSocketException_19001 = ex;
                return false;
            }
            catch (Exception ex)
            {
                if (lastException_19001 == null || lastException_19001 != ex)
                    WriteLog(5, "", String.Concat("Exception : ", ex.ToString()));

                lastException_19001 = ex;
                return false;
            }
        }

        private bool Connect_19002()
        {
            try
            {
                if (connect_19002)
                    return true;

                socketPort19002 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.socketPort19002.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 5000);

                this.socketPort19002.Connect(IPAddress.Parse(config.IP), 19002);
                connect_19002 = true;
                return true;
            }
            catch (SocketException ex)
            {
                if (lastSocketException_19002 == null || lastSocketException_19002 != ex)
                    WriteLog(5, "", String.Concat("Socke Exception : ", ex.ToString()));

                lastSocketException_19002 = ex;
                return false;
            }
            catch (Exception ex)
            {
                if (lastException_19002 == null || lastException_19002 != ex)
                    WriteLog(5, "", String.Concat("Exception : ", ex.ToString()));

                lastException_19002 = ex;
                return false;
            }
        }
        #endregion

        #region Sick Command.
        private bool Command_SendGetPosition()
        {
            try
            {
                byte[] byteArray = new byte[24]{ 0x55, 0xaa, 0x55, 0xaa, 0x01, 0x14,
                                                 0x01, 0x00, 0x02, 0x00, 0x04, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x6b, 0x2a, 0x9f, 0xfe };

                if (SendCommand(socketPort19000, byteArray))
                    return true;
                else
                {
                    WriteLog(5, "", "Send Get Status Fail");
                    return false;
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Socket Exception : ", ex.ToString()));
                return false;
            }
        }

        private bool Command_SendGetAngle()
        {
            try
            {
                byte[] byteArray = new byte[24]{ 0x55, 0xaa, 0x55, 0xaa, 0x01, 0x14,
                                                 0x01, 0x00, 0x07, 0x00, 0x04, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x7b, 0x5d, 0x3c, 0x66};

                if (SendCommand(socketPort19000, byteArray))
                    return true;
                else
                {
                    WriteLog(5, "", "Send Get Status Fail");
                    return false;
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Socket Exception : ", ex.ToString()));
                return false;
            }
        }

        private bool Command_SendGetStatus()
        {
            try
            {
                byte[] byteArray = new byte[24]{ 0x55, 0xaa, 0x55, 0xaa, 0x01, 0x14,
                                                 0x01, 0x00, 0x03, 0x00, 0x04, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x04, 0x66, 0x3a, 0x65};

                if (SendCommand(socketPort19000, byteArray))
                    return true;
                else
                {
                    WriteLog(5, "", "Send Get Status Fail");
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

        private string last_ret_code = "Empty";
        private string last_err_msg = "Empty";

        private byte[] buffer = new byte[0];
        private byte[] receiveBytes = new byte[0];

        private int FindFirstHaderIndex()
        {
            for (int i = 0; i < receiveBytes.Length - 4; i++)
            {
                if (receiveBytes[i] == 0x55 && receiveBytes[i + 1] == 0xAA && receiveBytes[i + 2] == 0x55 && receiveBytes[i + 3] == 0xAA)
                    return i;
            }

            return -1;
        }

        private bool SplitTCPIPPackage()
        {
            int index = FindFirstHaderIndex();

            if (index == -1)
            {
                buffer = new byte[0];
                return false;
            }
            else
            {
                if (index + 10 + 4 > receiveBytes.Length)
                {
                    buffer = receiveBytes;
                    return false;
                }

                UInt32 length = BitConverter.ToUInt32(receiveBytes, index + 10);

                byte[] onePackage = new byte[9 + 6 + 1 + length + 4];

                if (index + onePackage.Length > receiveBytes.Length)
                {
                    buffer = receiveBytes;
                    return false;
                }

                Array.Copy(receiveBytes, index, onePackage, 0, onePackage.Length);

                byte[] newReceiveByte = new byte[receiveBytes.Length - index - onePackage.Length];
                Array.Copy(receiveBytes, index + onePackage.Length, newReceiveByte, 0, newReceiveByte.Length);

                receiveBytes = newReceiveByte;

                bool dataOK = true;

                if (CheckCRC16(onePackage))
                {
                    string textData = System.Text.Encoding.ASCII.GetString(onePackage);
                    textData = textData.Substring(textData.IndexOf("{"), (textData.IndexOf("}") - textData.IndexOf("{")) + 1);
                    Dictionary<string, string> Jsonstring = JsonConvert.DeserializeObject<Dictionary<string, string>>(textData);

                    if (Jsonstring.ContainsKey("ret_code"))
                    {
                        if (last_ret_code != Jsonstring["ret_code"])
                        {
                            last_ret_code = Jsonstring["ret_code"];
                            WriteLog(3, "", String.Concat("ret_code : ", last_ret_code));
                        }
                    }

                    if (Jsonstring.ContainsKey("err_msg"))
                    {
                        if (last_err_msg != Jsonstring["err_msg"])
                        {
                            last_err_msg = Jsonstring["err_msg"];
                            WriteLog(3, "", String.Concat("err_msg : ", last_err_msg));
                        }
                    }

                    switch (onePackage[8])
                    {
                        case 03:
                            BITO_Status = Convert.ToInt32(Jsonstring["robot_localization_state"]);

                            #region 設定狀態.
                            switch (BITO_Status)
                            {
                                case 0:
                                    BITO_StatusString = EnumBITOStatus.正常.ToString();
                                    break;
                                case 1:
                                    BITO_StatusString = EnumBITOStatus.重定位.ToString();
                                    break;
                                case 2:
                                    BITO_StatusString = EnumBITOStatus.未準備好.ToString();
                                    break;
                                case 3:
                                    BITO_StatusString = EnumBITOStatus.初始化中.ToString();
                                    break;
                                case 4:
                                    BITO_StatusString = EnumBITOStatus.未取得Lidar資料.ToString();
                                    break;
                                default:
                                    BITO_StatusString = BITO_Status.ToString();
                                    break;
                            }
                            #endregion

                            count_StatusPackage++;
                            break;
                        case 02:
                            pollingTimer.Restart();
                            BitoData = new MapAGVPosition();
                            BitoData.Position.X = Convert.ToDouble(Jsonstring["x"]) * 1000;
                            BitoData.Position.Y = Convert.ToDouble(Jsonstring["y"]) * 1000;

                            double qx = Convert.ToDouble(Jsonstring["qx"]);
                            double qy = Convert.ToDouble(Jsonstring["qy"]);
                            double qz = Convert.ToDouble(Jsonstring["qz"]);
                            double qw = Convert.ToDouble(Jsonstring["qw"]);

                            BitoData.Angle = Math.Atan(2 * (qw * qz + qx * qy) / (1 - 2 * (qy * qy + qz * qz))) * 180 / Math.PI;

                            if ((1 - 2 * (qy * qy + qz * qz)) < 0)
                                BitoData.Angle -= 180;

                            BitoData.Angle = computeFunction.GetCurrectAngle(BitoData.Angle);
                            count_LocatePackage++;
                            break;
                        case 07:
                            BITO_Confidence = Convert.ToDouble(Jsonstring["match_score"]) * 100;

                            信心度訊號含延遲.Data = (BITO_Confidence >= 20);

                            if (信心度訊號含延遲.Data)
                                ResetAlarmCode(EnumMoveCommandControlErrorCode.LocateDriver_SLAM_信心度低下);
                            else
                                SetAlarmCode(EnumMoveCommandControlErrorCode.LocateDriver_SLAM_信心度低下);

                            count_信心度Package++;
                            break;
                        default:
                            dataOK = false;

                            WriteLog(3, "", String.Concat("onePackage[8] 出現預料外的資料 : ", onePackage[8].ToString("0")));
                            break;
                    }

                    if (dataOK)
                    {
                        LocateAGVPosition newLocateAGVPosition = new LocateAGVPosition(BitoData, BITO_Confidence, 50, DateTime.Now, count, EnumAGVPositionType.Normal, device, DriverConfig.Order);

                        newLocateAGVPosition.Status = BITO_StatusString;

                        if (localData.MoveControlData.LocateControlData.SlamLocateOK)
                        {
                            if (setPositionEnd)
                                setPositionEnd = false;
                            else
                            {
                                if (lastOriginPosition != null && BitoData != null)
                                {
                                    if (!setPositioning &&
                                        computeFunction.GetDistanceFormTwoAGVPosition(lastOriginPosition, BitoData) > 1000 ||
                                        Math.Abs(computeFunction.GetCurrectAngle(lastOriginPosition.Angle - BitoData.Angle)) > 30)
                                    {
                                        WriteLog(7, "", "飄動超過1M或角度誤差過大 SlamLocateOK = false");
                                        localData.MoveControlData.LocateControlData.SlamLocateOK = false;
                                        SetAlarmCode(EnumMoveCommandControlErrorCode.LocateDriver_SLAM_定位資料位移過大);
                                    }

                                    if (localData.MoveControlData.LocateControlData.SlamLocateOK)
                                        ResetAlarmCode(EnumMoveCommandControlErrorCode.LocateDriver_SLAM_定位資料位移過大);
                                }
                            }

                            if (BitoData != null)
                                lastOriginPosition = BitoData;
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

                        WriteCSV();
                        count++;
                    }

                    return true;
                }
                else
                {
                    if (FindFirstHaderIndex() == -1)
                    {
                        buffer = new byte[0];
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
        }

        private bool CheckCRC16(byte[] onePackage)
        {
            Crc32 crc32 = new Crc32();

            byte[] newArray = new byte[onePackage.Length - 4];

            Array.Copy(onePackage, 0, newArray, 0, newArray.Length);

            crc32.AddData(newArray);

            UInt32 crcValue = BitConverter.ToUInt32(onePackage, onePackage.Length - 4);

            return crc32.Crc32Value == BitConverter.ToUInt32(onePackage, onePackage.Length - 4);
        }

        #region GetData/GetDataThread.
        private int readSize = 4096;

        private Exception lastGetDataException = null;

        private void GetData()
        {
            try
            {
                receiveBytes = new byte[buffer.Length + readSize];
                buffer.CopyTo(receiveBytes, 0);
                this.socketPort19000.ReceiveTimeout = 1000;
                this.socketPort19000.Receive(receiveBytes, buffer.Length, readSize, SocketFlags.None);

                while (SplitTCPIPPackage())
                {
                }

                lastGetDataException = null;
            }
            catch (SocketException ex)
            {
                if (lastGetDataException == null || lastGetDataException.ToString() != ex.ToString())
                    WriteLog(3, "", String.Concat("SocketException : ", ex.ToString()));

                lastGetDataException = ex;
                nowAGVPosition = null;
            }
            catch (Exception ex)
            {
                if (lastGetDataException == null || lastGetDataException.ToString() != ex.ToString())
                    WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));

                lastGetDataException = ex;
                nowAGVPosition = null;
            }
        }

        private void PingTestThread()
        {
            try
            {
                bool lastPingOK = true;
                bool nowPingOK = true;

                while (Status != EnumControlStatus.Closing)
                {
                    nowPingOK = PingTest();

                    if (lastPingOK != nowPingOK)
                    {
                        WriteLog(3, "", String.Concat("Bito Ping 狀態改變成 ", (nowPingOK ? "OK" : "NG")));
                        lastPingOK = nowPingOK;
                    }

                    Thread.Sleep(localData.PingTestIntervalTime);
                }
            }
            catch (Exception ex)
            {
                WriteLog(1, "", String.Concat("PingThread Exception : ", ex.ToString()));
            }
        }

        private bool PingTest()
        {
            try
            {
                Ping ping = new Ping();
                PingReply result = ping.Send("192.168.29.80", 10000);

                if (result.RoundtripTime > 10)
                    WriteLog(3, "", String.Concat("Bito Ping Time = ", result.RoundtripTime.ToString("0")));

                return (result.Status == IPStatus.Success);
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Ping Excepion : ", ex.ToString()));
                return false;
            }
        }

        private void GetDataThread()
        {
            pollingTimer.Restart();

            bool moving = false;
            double 未取得定位資料時間 = 200;
            double idle切換至迷航時間 = 30 * 60 * 1000;
            double 移動中切換至迷航時間 = 3 * 1000;

            while (Status != EnumControlStatus.Closing)
            {
                GetData();

                if (!setPositioning)
                {
                    #region 真實應該寫法..........
                    /*
                    if (pollingTimer.ElapsedMilliseconds > 200)
                    {
                        BitoData = null;
                        originAGVPosition = null;
                        offsetAGVPosition = null;
                        nowAGVPosition = null;
                        localData.MoveControlData.LocateControlData.SlamLocateOK = false;
                        SetAlarmCode(EnumMoveCommandControlErrorCode.LocateDriver_SLAM_BITO未取得定位資料);
                    }
                    else
                    {
                        ResetAlarmCode(EnumMoveCommandControlErrorCode.LocateDriver_SLAM_BITO未取得定位資料);
                    }
                    */
                    #endregion

                    if (pollingTimer.ElapsedMilliseconds > 未取得定位資料時間)
                    {
                        moving = (localData.MoveControlData.MoveCommand != null || localData.MoveControlData.MotionControlData.JoystickMode || localData.MIPCData.BreakRelease);

                        if (pollingTimer.ElapsedMilliseconds > idle切換至迷航時間 && !moving)
                        {
                            if (localData.MoveControlData.LocateControlData.SlamLocateOK)
                            {
                                BitoData = null;
                                originAGVPosition = null;
                                offsetAGVPosition = null;
                                nowAGVPosition = null;
                                WriteLog(3, "", "超過30分鐘沒有Slam資料, 變為迷航(不管停止/移動)");
                                localData.MoveControlData.LocateControlData.SlamLocateOK = false;
                            }
                        }
                        else if (pollingTimer.ElapsedMilliseconds > 移動中切換至迷航時間 && moving)
                        {
                            if (localData.MoveControlData.LocateControlData.SlamLocateOK)
                            {
                                BitoData = null;
                                originAGVPosition = null;
                                offsetAGVPosition = null;
                                nowAGVPosition = null;
                                WriteLog(3, "", "超過3秒沒有Slam資料, 變為迷航(走行中或搖桿移動中)");
                                localData.MoveControlData.LocateControlData.SlamLocateOK = false;
                            }
                        }

                        nowAGVPosition = null;

                        SetAlarmCode(EnumMoveCommandControlErrorCode.LocateDriver_SLAM_未取得定位資料);
                    }
                    else
                        ResetAlarmCode(EnumMoveCommandControlErrorCode.LocateDriver_SLAM_未取得定位資料);
                }

                if (!localData.MoveControlData.LocateControlData.SlamLocateOK)
                    SetAlarmCode(EnumMoveCommandControlErrorCode.LocateDriver_SLAM_精度迷航);
                else
                    ResetAlarmCode(EnumMoveCommandControlErrorCode.LocateDriver_SLAM_精度迷航);

                Thread.Sleep(config.SleepTime);
            }

            pollingTimer.Reset();
            status = EnumControlStatus.Closed;
        }

        private bool StartThread()
        {
            try
            {
                pollingThread = new Thread(GetDataThread);
                pollingThread.Start();

                pingThread = new Thread(PingTestThread);
                pingThread.Start();

                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region 重定位以及確認資料.
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

            if (Command_SendSetPosition(GetOrigionPositionBySlamPositionAndOffsetData(setPosition), distanceRange))
            {
                Stopwatch timer = new Stopwatch();
                timer.Restart();

                while (timer.ElapsedMilliseconds < 3 * 1000)
                    Thread.Sleep(100);

                while (BITO_StatusString == EnumBITOStatus.重定位.ToString() && timer.ElapsedMilliseconds < 180 * 1000)
                    Thread.Sleep(100);

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
                localData.MoveControlData.ReviseAndSetPositionData = "Bito重定位失敗";
                errorMessage = "Command_SendSetPosition return false";
            }

            localData.MoveControlData.LocateControlData.SlamLocateOK = returnBoolean;
            WriteLog(7, "", String.Concat("結果 = ", returnBoolean.ToString()));
            WriteLog(7, "", "SetPosition End");
            setPositioning = false;
            setPositionEnd = true;
            return returnBoolean;
        }
        #endregion

        #region 重定位命令.
        private bool Command_SendSetPosition(MapAGVPosition setPosition, double range)
        {
            try
            {
                double roll = 0;// Math.PI/2;
                double pitch = 0;
                double yaw = setPosition.Angle / 180 * Math.PI;
                double phi = roll / 2.0;
                double the = pitch / 2.0;
                double psi = yaw / 2.0;
                double qx = Math.Sin(phi) * Math.Cos(the) * Math.Cos(psi) - Math.Cos(phi) * Math.Sin(the)
                    * Math.Sin(psi);
                double qy = Math.Cos(phi) * Math.Sin(the) * Math.Cos(psi) + Math.Sin(phi) * Math.Cos(the)
                    * Math.Sin(psi);
                double qz = Math.Cos(phi) * Math.Cos(the) * Math.Sin(psi) - Math.Sin(phi) * Math.Sin(the)
                    * Math.Cos(psi);
                double qw = Math.Cos(phi) * Math.Cos(the) * Math.Cos(psi) + Math.Sin(phi) * Math.Sin(the)
                    * Math.Sin(psi);

                string sendMessage = String.Concat("{",
                                                   "\"x\":", (setPosition.Position.X / 1000).ToString("0.000"),
                                                   ",\"y\":", (setPosition.Position.Y / 1000).ToString("0.000"),
                                                   ",\"z\":", "0",
                                                   ",\"qx\":", qx.ToString("0.000"),
                                                   ",\"qy\":", qy.ToString("0.000"),
                                                   ",\"qz\":", qz.ToString("0.000"),
                                                   ",\"qw\":", qw.ToString("0.000"),
                                                   "}");

                WriteLog(7, "", String.Concat("Json : ", sendMessage));

                byte[] tmp_byteArray = Encoding.ASCII.GetBytes(sendMessage);
                byte[] byteArrayTitle = { 0x55, 0xaa, 0x55, 0xaa, 0x01, 0x14, 0x01, 0x00, 0x01, 0x00 };

                byte[] zeroArray = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

                byte[] byteArray = new byte[byteArrayTitle.Length + 4 + 6 + tmp_byteArray.Length + 4];

                byteArrayTitle.CopyTo(byteArray, 0);

                Int32 length = 4 + tmp_byteArray.Length;

                byte[] lengthArray = BitConverter.GetBytes(length);

                lengthArray.CopyTo(byteArray, byteArrayTitle.Length);

                zeroArray.CopyTo(byteArray, byteArrayTitle.Length + 4);

                tmp_byteArray.CopyTo(byteArray, byteArrayTitle.Length + 4 + 6);

                Crc32 crc32 = new Crc32();

                byte[] newArray = new byte[byteArray.Length - 4];

                Array.Copy(byteArray, 0, newArray, 0, newArray.Length);

                crc32.AddData(newArray);

                byte[] byteArrayCRC = BitConverter.GetBytes(crc32.Crc32Value);

                byteArrayCRC.CopyTo(byteArray, byteArrayTitle.Length + 4 + 6 + tmp_byteArray.Length);

                if (SendCommand(socketPort19001, byteArray) && ReceiveCommand(socketPort19001))
                    return true;
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }
        #endregion

        #region WriteCSV.
        private void WriteCSV()
        {
            string csvLog = "";

            if (config.LogMode)
            {
                if (offsetAGVPosition == null)
                    csvLog = String.Concat(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), ",", count_LocatePackage.ToString(), ",", "", ",", "", ",", "", ",", "");
                else if (offsetAGVPosition.AGVPosition == null)
                    csvLog = String.Concat(offsetAGVPosition.GetDataTime.ToString("yyyy/MM/dd HH:mm:ss.fff"), ",", count_LocatePackage.ToString(), ",,,,", offsetAGVPosition.ScanTime.ToString());
                else
                    csvLog = String.Concat(offsetAGVPosition.GetDataTime.ToString("yyyy/MM/dd HH:mm:ss.fff"), ",",
                                           count_LocatePackage.ToString(), ",", offsetAGVPosition.AGVPosition.Position.X.ToString("0.0"), ",",
                                           offsetAGVPosition.AGVPosition.Position.Y.ToString("0.0"), ",", offsetAGVPosition.AGVPosition.Angle.ToString("0.00"), ",",
                                           offsetAGVPosition.ScanTime.ToString());

                if (nowAGVPosition == null)
                    csvLog = String.Concat(csvLog, ",", "", ",", "", ",", "", ",", "");
                else if (nowAGVPosition.AGVPosition == null)
                    csvLog = String.Concat(csvLog, ",", "", ",", "", ",", "", ",", "");
                else
                    csvLog = String.Concat(csvLog, ",", nowAGVPosition.AGVPosition.Position.X.ToString("0.0"), ",",
                                                        nowAGVPosition.AGVPosition.Position.Y.ToString("0.0"), ",",
                                                        nowAGVPosition.AGVPosition.Angle.ToString("0.00"), ",",
                                                        nowAGVPosition.Tag);

                csvLog = String.Concat(csvLog, ",", count_信心度Package.ToString());
                csvLog = String.Concat(csvLog, ",", BITO_Confidence.ToString());

                csvLog = String.Concat(csvLog, ",", count_StatusPackage.ToString());

                if (originAGVPosition == null)
                    csvLog = String.Concat(csvLog, ",", "");
                else
                    csvLog = String.Concat(csvLog, ",", originAGVPosition.Status);

                VehicleLocation nowLocate = localData.Location;

                if (localData.MoveControlData.LocateControlData.SlamLocateOK && nowLocate != null && nowLocate.InAddress)
                    csvLog = String.Concat(csvLog, ",", nowLocate.LastAddress);
                else
                    csvLog = String.Concat(csvLog, ",", "");

                logger.LogString(csvLog);
            }
        }
        #endregion
    }
}
