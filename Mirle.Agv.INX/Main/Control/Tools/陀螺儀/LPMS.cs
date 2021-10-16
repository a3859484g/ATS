using Mirle.Agv.INX.Controller.Tools;
using Mirle.Agv.INX.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Controller
{
    public class LPMS
    {
        public LPMSData LPMSData { get; set; } = null;

        private LoggerAgent loggerAgent = LoggerAgent.Instance;
        private Logger logger = LoggerAgent.Instance.GetLooger("LPMSCSV");
        private SerialPort port = null;
        private bool connected = false;

        private byte startByte = 0x3A;
        private byte endByte_1 = 0x0D;
        private byte endByte_2 = 0x0A;

        private int commandReceive = -1;

        private double waitResultTimeout = 100;

        private double interval = 100;
        private double sleepTime = 0;

        private Thread pollingThread = null;
        private Thread initialSettingThread = null;


        private string normalLogName = "LPMS";

        public LPMS(int comportNumber)
        {
            port = new SerialPort(String.Concat("COM", comportNumber.ToString()), 115200, Parity.None, 8, StopBits.One);

            Connect();
        }

        private void Connect()
        {
            try
            {
                if (port.IsOpen)
                    return;

                port.Open();

                if (port.IsOpen)
                {
                    port.ReadTimeout = 500;
                    port.WriteTimeout = 1000;
                    connected = true;

                    initialSettingThread = new Thread(InitialSetting);
                    initialSettingThread.Start();
                }
                else
                    WriteLog(5, "", "Open Fail");
            }
            catch (Exception ex)
            {
                WriteLog(5, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        public void Disconnect()
        {
            try
            {
                if (!port.IsOpen)
                    return;

                port.Dispose();
                connected = false;
            }
            catch { }
        }

        public void Reset()
        {
            try
            {
                if (!port.IsOpen)
                    Connect();
            }
            catch { }
        }

        public void WriteLog(int logLevel, string carrierId, string message, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            LogFormat logFormat = new LogFormat(normalLogName, logLevel.ToString(), "", "", "", message);

            loggerAgent.Log(logFormat.Category, logFormat);
        }

        private void InitialSetting()
        {
            sleepTime = 0.8 * 10;
            //sleepTime = 0.8 * interval;

            pollingThread = new Thread(PollingDataThread);
            pollingThread.Start();

            if (Command_ChangeMode_CommandMode() &&
                 Command_ChangePollingInterval_HZ() &&
                 Command_ChangeMode_PollingMode())
                sleepTime = 0.8 * interval;
        }

        private void PollingDataThread()
        {
            Stopwatch timer = new Stopwatch();

            while (connected)
            {
                timer.Restart();
                GetReceiveData();

                while (timer.ElapsedMilliseconds < sleepTime)
                    Thread.Sleep(1);
            }
        }

        private const int byteArraySize = 300;

        private byte[] receiveDataArray = new byte[0];
        private byte[] bufferDataArray = new byte[0];

        //private byte[] receiveDataArray = new byte[byteArraySize];

        private void GetReceiveData()
        {
            try
            {
                byte[] readArray = new byte[byteArraySize];

                int readLength = port.Read(readArray, 0, byteArraySize);

                if (readLength < 0)
                    readLength = 0;

                receiveDataArray = new byte[readLength + bufferDataArray.Length];

                if (bufferDataArray.Length > 0)
                    bufferDataArray.CopyTo(receiveDataArray, 0);

                if (readLength > 0)
                    Array.Copy(readArray, 0, receiveDataArray, bufferDataArray.Length, readLength);

                if (readLength > 0)
                {
                    while (GetData())
                        ;
                }
            }
            catch (Exception ex)
            {
                WriteLog(5, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        private void ProcessReceiveDataByIndex(int index)
        {
            if (index + 1 < receiveDataArray.Length)
            {
                byte[] newByteArray = new byte[receiveDataArray.Length - (index + 1)];

                Array.Copy(receiveDataArray, index + 1, newByteArray, 0, newByteArray.Length);
                receiveDataArray = newByteArray;
            }
            else
            {
                bufferDataArray = new byte[0];
            }
        }

        private bool CheckCRC(byte[] dataArray)
        {
            if (dataArray[0] == startByte &&
                dataArray[dataArray.Length - 2] == endByte_1 &&
                dataArray[dataArray.Length - 1] == endByte_2)
            {
                int crc = 0;

                for (int i = 1; i < dataArray.Length - 4; i++)
                    crc += dataArray[i];

                UInt16 dataCrc = BitConverter.ToUInt16(dataArray, dataArray.Length - 4);

                return dataCrc == crc;
            }
            else
                return false;
        }

        private void ProcessData(byte[] dataArray)
        {
            float funCode = BitConverter.ToUInt16(dataArray, 3);

            //WriteLog(7, "", String.Concat("FunctionCode : ", funCode.ToString()));

            switch (funCode)
            {
                case 0:
                    commandReceive = 1;
                    break;
                case 1:
                    commandReceive = 0;
                    break;
                case 4:
                    //status = QString("config recv");
                    //chk_config(cut, len);
                    break;
                case 5:
                    //status = QString("status recv");
                    //chk_status(cut, len);
                    break;
                case 9:
                    LPMSData lpmsData = new LPMSData();

                    lpmsData.Timestamp = BitConverter.ToSingle(dataArray, 7);

                    lpmsData.Gyroscope = new Vector3(dataArray, 11);
                    lpmsData.Acceleromete = new Vector3(dataArray, 23);
                    lpmsData.Magnetometer = new Vector3(dataArray, 35);

                    lpmsData.Orientation = new Vector4(dataArray, 47);
                    lpmsData.EulerAngle = new Vector3(dataArray, 63);
                    lpmsData.LinearAccelerationa = new Vector3(dataArray, 75);

                    LPMSData = lpmsData;
                    WriteCSVData();
                    break;
                default:
                    break;
            }
        }

        // return true -> findNextData. return false -> nextReceive.
        private bool GetData()
        {
            int index = Array.IndexOf(receiveDataArray, startByte);

            if (index == -1)
            {
                bufferDataArray = receiveDataArray;
                return false;
            }

            if (index + 6 >= receiveDataArray.Length)
            {
                bufferDataArray = receiveDataArray;
                return false;
            }

            UInt16 len = BitConverter.ToUInt16(receiveDataArray, index + 5);

            if (index + 10 + len >= receiveDataArray.Length)
            {
                bufferDataArray = receiveDataArray;

                if (len > 300)
                {
                    ProcessReceiveDataByIndex(index);
                    return true;
                }
                else
                    return false;
            }

            if (receiveDataArray[index + 9 + len] == endByte_1 &&
                receiveDataArray[index + 10 + len] == endByte_2)
            {
                byte[] dataArray = new byte[len + 11];

                Array.Copy(receiveDataArray, index, dataArray, 0, dataArray.Length);

                if (CheckCRC(dataArray))// crc ok
                {
                    ProcessData(dataArray);
                    ProcessReceiveDataByIndex(index + 9 + len);
                    return true;
                }
                else
                {
                    ProcessReceiveDataByIndex(index);
                    return true;
                }
            }
            else
            {
                ProcessReceiveDataByIndex(index);
                return true;
            }
        }

        private void WriteCSVData()
        {
            logger.LogString(String.Concat(LPMSData.Time.ToString("yyyy/MM/dd HH:mm:ss.fff"), ",", LPMSData.Timestamp.ToString("0.000"),
                 ",", LPMSData.Gyroscope.X.ToString("0.000"), ",", LPMSData.Gyroscope.Y.ToString("0.000"), ",", LPMSData.Gyroscope.Z.ToString("0.000"),
                 ",", LPMSData.Acceleromete.X.ToString("0.000"), ",", LPMSData.Acceleromete.Y.ToString("0.000"), ",", LPMSData.Acceleromete.Z.ToString("0.000"),
                 ",", LPMSData.Magnetometer.X.ToString("0.000"), ",", LPMSData.Magnetometer.Y.ToString("0.000"), ",", LPMSData.Magnetometer.Z.ToString("0.000"),
                 ",", LPMSData.Orientation.X.ToString("0.000"), ",", LPMSData.Orientation.Y.ToString("0.000"), ",", LPMSData.Orientation.Z.ToString("0.000"), ",", LPMSData.Orientation.W.ToString("0.000"),
                 ",", LPMSData.EulerAngle.X.ToString("0.000"), ",", LPMSData.EulerAngle.Y.ToString("0.000"), ",", LPMSData.EulerAngle.Z.ToString("0.000"),
                 ",", LPMSData.LinearAccelerationa.X.ToString("0.000"), ",", LPMSData.LinearAccelerationa.Y.ToString("0.000"), ",", LPMSData.LinearAccelerationa.Z.ToString("0.000")));
        }

        private bool Command_ChangeMode_CommandMode()
        {
            try
            {
                commandReceive = -1;

                SendByteArray(GetByteArray(1, 6, new byte[4] { 0x0A, 0x00, 0x00, 0x00 }));

                Stopwatch timer = new Stopwatch();

                while (timer.ElapsedMilliseconds < waitResultTimeout && commandReceive == -1)
                    Thread.Sleep(1);

                return commandReceive == 1;
            }
            catch
            {
                return false;
            }
        }

        private bool Command_ChangeMode_PollingMode()
        {
            try
            {
                commandReceive = -1;

                SendByteArray(GetByteArray(1, 7, new byte[4] { 0x0A, 0x00, 0x00, 0x00 }));

                Stopwatch timer = new Stopwatch();

                while (timer.ElapsedMilliseconds < waitResultTimeout && commandReceive == -1)
                    Thread.Sleep(1);

                return commandReceive == 1;
            }
            catch
            {
                return false;
            }
        }

        private void Command_ReadDataInCommandMode()
        {
            // 我懶得寫.
            /*
            uint16_t len = 0, temp_crc = 0;
            DWORD dwBytesRead = 0;

            cmd.Packet_start = 0x3A;

            cmd.OpenMATID = 0x01;
            cmd.OpenMATID2 = 0x00;
            cmd.Command = 0x09;
            cmd.Command2 = 0x00;
            cmd.data_length = len & 0x00FF;
            cmd.data_length2 = (len & 0xFF00) >> 8;

            temp_crc = crc(cmd, len);
            cmd.data[len] = temp_crc & 0x00FF; //crc
            cmd.data[len + 1] = (temp_crc & 0xFF00) >> 8;

            cmd.data[len + 2] = 0x0D; //Termination
            cmd.data[len + 3] = 0x0A;

            WriteFile(serialHandle, (uint8_t*)&cmd, 11, &dwBytesRead, NULL);
            */
        }

        private void Command_ReadConfig()
        {
            // 我懶得寫.
            /*
            uint16_t len = 0, temp_crc = 0;
            DWORD dwBytesRead = 0;

            cmd.Packet_start = 0x3A;

            cmd.OpenMATID = 0x01;
            cmd.OpenMATID2 = 0x00;
            cmd.Command = 0x04;
            cmd.Command2 = 0x00;
            cmd.data_length = len & 0x00FF;
            cmd.data_length2 = (len & 0xFF00) >> 8;

            temp_crc = crc(cmd, len);
            cmd.data[len] = temp_crc & 0x00FF; //crc
            cmd.data[len + 1] = (temp_crc & 0xFF00) >> 8;

            cmd.data[len + 2] = 0x0D; //Termination
            cmd.data[len + 3] = 0x0A;

            WriteFile(serialHandle, (uint8_t*)&cmd, 11, &dwBytesRead, NULL);
            */
        }

        // 0, 正常, 其他異常.
        private bool Command_GetStatus()
        {
            try
            {
                commandReceive = -1;

                SendByteArray(GetByteArray(1, 5, new byte[4] { 0x0A, 0x00, 0x00, 0x00 }));

                Stopwatch timer = new Stopwatch();

                while (timer.ElapsedMilliseconds < waitResultTimeout && commandReceive == -1)
                    Thread.Sleep(1);

                return commandReceive == 1;
            }
            catch
            {
                return false;
            }
        }

        private bool Command_ChangePollingInterval_HZ()
        {
            try
            {
                commandReceive = -1;

                // 10HZ
                SendByteArray(GetByteArray(1, 11, new byte[4] { 0x0A, 0x00, 0x00, 0x00 }));

                Stopwatch timer = new Stopwatch();

                while (timer.ElapsedMilliseconds < waitResultTimeout && commandReceive == -1)
                    Thread.Sleep(1);

                return commandReceive == 1;
            }
            catch
            {
                return false;
            }
        }

        // 歸0.
        private bool Command_Offset()
        {
            try
            {
                commandReceive = -1;

                // mode 0 1 2 >>> 0x01~2, 0x00, 0x00, 0x00.
                SendByteArray(GetByteArray(1, 18, new byte[4] { 0x00, 0x00, 0x00, 0x00 }));

                Stopwatch timer = new Stopwatch();

                while (timer.ElapsedMilliseconds < waitResultTimeout && commandReceive == -1)
                    Thread.Sleep(1);

                return commandReceive == 1;
            }
            catch
            {
                return false;
            }
        }

        private bool Command_Save()
        {
            try
            {
                commandReceive = -1;
                SendByteArray(GetByteArray(1, 15, new byte[0]));

                Stopwatch timer = new Stopwatch();

                while (timer.ElapsedMilliseconds < waitResultTimeout && commandReceive == -1)
                    Thread.Sleep(1);

                return commandReceive == 1;
            }
            catch
            {
                return false;
            }
        }

        private bool Command_ResetDefault()
        {
            try
            {
                commandReceive = -1;
                SendByteArray(GetByteArray(1, 1, new byte[0]));

                Stopwatch timer = new Stopwatch();

                while (timer.ElapsedMilliseconds < waitResultTimeout && commandReceive == -1)
                    Thread.Sleep(1);

                return commandReceive == 1;
            }
            catch
            {
                return false;
            }
        }

        private void SendByteArray(byte[] sendData)
        {
            port.Write(sendData, 0, sendData.Length);
        }

        #region byte[] GetByteArray(UInt16 matID, UInt16 cmd, byte[] dataArray).
        private byte[] GetByteArray(UInt16 matID, UInt16 cmd, byte[] dataArray)
        {
            UInt16 len = (UInt16)dataArray.Length;
            byte[] returnByteArray = new byte[3 + 6 + 2 + dataArray.Length];
            byte[] matIDArray = BitConverter.GetBytes(matID);
            byte[] cmdArray = BitConverter.GetBytes(cmd);
            byte[] lenArray = BitConverter.GetBytes(len);

            returnByteArray[0] = startByte;
            matIDArray.CopyTo(returnByteArray, 1);
            cmdArray.CopyTo(returnByteArray, 3);
            lenArray.CopyTo(returnByteArray, 5);

            dataArray.CopyTo(returnByteArray, 7);

            UInt16 crc = 0;

            for (int i = 1; i < returnByteArray.Length - 4; i++)
                crc += returnByteArray[i];

            byte[] crcArray = BitConverter.GetBytes(crc);

            crcArray.CopyTo(returnByteArray, returnByteArray.Length - 4);
            returnByteArray[returnByteArray.Length - 2] = endByte_1;
            returnByteArray[returnByteArray.Length - 1] = endByte_2;

            return returnByteArray;
        }
        #endregion
    }
}
