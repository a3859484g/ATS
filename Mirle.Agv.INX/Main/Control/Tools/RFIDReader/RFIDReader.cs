using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Controller
{
    public class RFIDReader
    {
        public Socket socket = null;
        public bool Connected { get; set; } = false;
        public bool Error { get; set; } = false;
        protected string ipOrComport = "";

        public byte[] TestModeCommandByte = Encoding.ASCII.GetBytes("1012345678\r");
        public string ReceiveString;

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


        public bool ReadRFIDTag(ref string errorMessage)
        {
            try
            {
                byte[] commandByte = new byte[40];
                byte[] receiveByte = new byte[40];
                //string receiveString;

                //commandByte = Encoding.ASCII.GetBytes("1012345678\r");
                commandByte = Encoding.ASCII.GetBytes("01000000000C\r");
                socket.Send(commandByte);
                socket.ReceiveTimeout = 500;
                socket.Receive(receiveByte);

                //ReceiveString = Encoding.ASCII.GetString(receiveByte);
                ReceiveString = GetFeedbackString(receiveByte);
                return true;
            }
            catch (Exception ex)
            {

                errorMessage = ex.ToString();
                return false;
            }

        }

        private string GetFeedbackString(byte[] receiveBytes)
        {
            string receiveString = "";
            string reportString = "";
            string sData;
            int hexData;
            
            if (receiveBytes == null)
                return "";

            int start = -1;
            int end = -1;

            string startbyte = "0";
            string endbyte = "\r";

            if (receiveBytes[0] == startbyte[0] && receiveBytes[1] == startbyte[0])
            {
                start = 0;
                for (int i = 0; i < receiveBytes.Length; i++)
                {
                    if (receiveBytes[i] == endbyte[0])
                    {
                        end = i;
                    }
                }
                receiveString = System.Text.Encoding.ASCII.GetString(receiveBytes, start + 2, end - 1);

                for(int i = 0; i < 9; i++)
                {
                    sData = receiveString.Substring(i * 2, 2);
                    hexData = Convert.ToInt32(sData, 16);
                    reportString = string.Concat(reportString, char.ConvertFromUtf32(hexData));
                }

                return reportString;
            }
            else
                return "";

        }

    }
}
