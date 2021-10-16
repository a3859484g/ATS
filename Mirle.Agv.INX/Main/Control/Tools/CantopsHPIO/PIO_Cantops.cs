using Mirle.Agv.INX.Model;
using Mirle.Agv.INX.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Ports;

namespace Mirle.Agv.INX.Controller
{
    public class PIO_Cantops
    {
        protected LocalData localData = LocalData.Instance;
        protected LoggerAgent loggerAgent = LoggerAgent.Instance;

        private SerialPort rs232Port = new SerialPort();
        private string rs232Data;
        private enum Type { IR, RF_2G, RF_5G }
        private enum Port { Port0, Port1, Port2, Port3, Port4 }

        //ATS改socket
        public Socket socket = null;
        public bool Connected { get; set; } = false;
        public bool Error { get; set; } = false;
        protected string ipOrComport = "";

        //private enum Cmd
        //{
        //    M, // 通信媒介的设定/确认 : IR, RF 通信设定/确认
        //    C, // CH 设定/确认 : 频段 CH 设定/确认
        //    A, // ID 设定/确认 : 固有 NO. ID 设定/确认
        //    N, // Port 设定/确认 : 设备Port 设定 及 确认
        //    Y, // 可发送数据状态确认
        //    G, // GO OFF 状态确认 
        //    P, // RF 输出功率的设定及确认
        //    R, // IR/RF 通信 Retry 次数设定及确认
        //    W, // RF 通信确认次数设定及确认
        //    T, // PIO 时间设定/确认 : PIO 实时时间设定及确认
        //    L, // 通信 Data Download
        //    S, // 通信 Data 状态Download
        //    V, // Firmware Version 确认
        //    O, // OHT 机号设定/确认 : 固有机号 ID 设定/确认
        //    BC, // 通信媒介, CH, ID, PORT, OHT 机号统合命令语的设定及确认
        //    DI, // 栋/层 区分代码设定/确认
        //}


        public bool ConnectSocket(string ip, ref string errorMessage)
        {
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 500);
                string[] split = Regex.Split(ip, ":", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));

                socket.Connect(IPAddress.Parse(split[0]), int.Parse(split[1]));


                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.ToString();
                return false;
            }

        }

        public bool Connect(string ip, ref string errorMessage)
        {
            try
            {
                if (Connected)
                {
                    if (!Disconnect(ref errorMessage))
                        return false;
                }

                if (ConnectSocket(ip, ref errorMessage))
                {
                    ipOrComport = ip;
                    Connected = true;
                    return true;
                }
                else
                {
                    errorMessage = String.Concat("連線失敗, IP : ", ip, ", errorMessage : ", errorMessage);
                    return false;
                }

            }
            catch (Exception ex)
            {
                errorMessage = String.Concat("連線失敗 : ", ex.ToString());
                return false;
            }

        }

        public bool Disconnect(ref string errorMessage)
        {
            try
            {
                if (!Connected)
                    return true;
                else
                {
                    socket.Disconnect(true);
                    socket.Dispose();

                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = String.Concat("斷線失敗 : ", ex.ToString());
                return false;
            }

        }

        public bool Fun_Rs232_OpenPort(int comPortNum, int baudRate)
        {
            try
            {
                string comPortName = "";
                string cmdData = "";

                comPortName = "COM" + comPortNum.ToString();
                rs232Port = new SerialPort(comPortName, baudRate, Parity.None, 8, StopBits.One);

                if (rs232Port.IsOpen == false)
                {
                    rs232Port.Open();
                    cmdData = "<T54>";
                    Fun_Rs232_SendData(cmdData);
                    SpinWait.SpinUntil(() => { return false; }, 1);
                    rs232Data = "";
                    rs232Data = rs232Port.ReadExisting();
                    if (rs232Data == "" || rs232Data.Substring(1, 1) == "N")
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                //log
                return false;

            }
        }

        public bool Fun_Rs232_OpenPort(int comPortNum)
        {
            try
            {
                string comPortName = "";

                comPortName = "COM" + comPortNum.ToString();
                rs232Port = new SerialPort(comPortName, 38400, Parity.None, 8, StopBits.One);

                if (rs232Port.IsOpen == false)
                {
                    rs232Port.Open();
                    /* cmdData = "<T54>";
                     Fun_Rs232_SendData(cmdData);
                     SpinWait.SpinUntil(() => { return false; }, 1);
                     */
                    rs232Data = "";
                    rs232Data = rs232Port.ReadExisting();
                    if (rs232Data == "" || rs232Data.Substring(1, 1) == "N")
                    {
                        return false;
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                //log
                return false;
            }
        }

        public void Fun_Rs232_ClosePort()
        {
            rs232Port.Close();
        }

        public void Fun_Rs232_ReceiveData()
        {
            if (rs232Port.IsOpen == true)
            {
                rs232Data = rs232Port.ReadExisting();
            }
        }
        public void Fun_Rs232_SendData(string data)
        {
            rs232Port.Write(data);
        }

        public bool Fun_RFPIO_SetCH(string channel) //channel : 000~255 (3-bits ascii)
        {// 2.4GHz CH0~122,  5GHz CH128-255
            string cmdData = "";
            string checksum;

            //加入Select ON輸出


            rs232Data = rs232Port.ReadExisting();
            SpinWait.SpinUntil(() => { return false; }, 1);

            cmdData = "C=" + channel;
            checksum = Fun_RFPIO_GetCmdCheckSum(cmdData);
            cmdData = "<" + cmdData + checksum + ">";

            Fun_Rs232_SendData(cmdData);
            SpinWait.SpinUntil(() => { return false; }, 1);

            rs232Data = "";
            rs232Data = rs232Port.ReadExisting();

            if (rs232Data == "")
            {
                //Log 通訊失敗
                return false;
            }

            if (rs232Data != ("[C=" + channel + checksum + "]"))
            {
                //Log 發送與接收不相同
                return false;
            }
            return true;
        }

        public bool Fun_RFPIO_SetID(string address)
        {
            string cmdData = "";
            string checksum;

            //加入Select ON輸出

            rs232Data = rs232Port.ReadExisting();
            SpinWait.SpinUntil(() => { return false; }, 1);

            cmdData = "A=" + address;
            checksum = Fun_RFPIO_GetCmdCheckSum(cmdData);
            cmdData = "<" + cmdData + checksum + ">";

            Fun_Rs232_SendData(cmdData);
            SpinWait.SpinUntil(() => { return false; }, 1);

            rs232Data = "";
            rs232Data = rs232Port.ReadExisting();

            if (rs232Data == "")
            {
                //Log 通訊失敗
                return false;
            }
            return true;
        } //address : 000000~FFFFFF (6-bits ascii)
        public bool Fun_RFPIO_SetCH_ID(string channel, string address)
        {
            string cmdData = "";
            string checksum;
            Type type = Type.RF_5G;
            Port port = Port.Port0;

            //加入Select ON輸出

            rs232Data = rs232Port.ReadExisting();
            SpinWait.SpinUntil(() => { return false; }, 1);

            //id = eq.OHT.Param.System.VehicleID.Substring(eq.OHT.Param.System.VehicleID.Length - 2);

            //cmdData = "BC=" + (int)type + ":" + address + ":" + channel + ":" + port + ":" + "OHT0" + id;
            cmdData = "BC=" + (int)type + ":" + address + ":" + channel + ":" + port + ":" + "OHT123";
            checksum = Fun_RFPIO_GetCmdCheckSum(cmdData);
            cmdData = "<" + cmdData + checksum + ">";

            Fun_Rs232_SendData(cmdData);
            Thread.Sleep(500);

            rs232Data = "";
            rs232Data = rs232Port.ReadExisting();

            if (rs232Data == "")
            {
                //Log 通訊失敗
                return false;
            }
            return true;
        }


        public bool Fun_RFPIO_Socket_SetCH_ID(string channel, string address)
        {
            string cmdData = "";
            string checksum;

            byte[] commandByte = new byte[40];
            byte[] receiveByte = new byte[40];

            Type type = Type.RF_5G;
            Port port = Port.Port0;


            //id = eq.OHT.Param.System.VehicleID.Substring(eq.OHT.Param.System.VehicleID.Length - 2);

            //cmdData = "BC=" + (int)type + ":" + address + ":" + channel + ":" + port + ":" + "OHT0" + id;
            cmdData = "BC=" + (int)type + ":" + address + ":" + channel + ":" + port + ":" + "OHT123";
            checksum = Fun_RFPIO_GetCmdCheckSum(cmdData);
            cmdData = "<" + cmdData + checksum + ">";

            commandByte = Encoding.ASCII.GetBytes(cmdData);
            socket.Send(commandByte);
            socket.ReceiveTimeout = 500;
            socket.Receive(receiveByte);
            Thread.Sleep(500);


            if (Encoding.ASCII.GetString(receiveByte) == "")
            {
                //Log 通訊失敗
                return false;
            }
            return true;
        }

        private string Fun_RFPIO_GetCmdCheckSum(string cmdData)
        {
            int sum = 0;
            string s;
            string checksum;
            char[] tmp;

            tmp = cmdData.ToCharArray();

            for (int i = 0; i < tmp.Length; i++)
            {
                sum = sum + Convert.ToInt32(tmp[i]);
            }

            s = Convert.ToString(sum, 16);
            checksum = s.Substring(s.Length - 2).ToUpper();

            return checksum;
        }



    }
}
