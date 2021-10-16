using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;

namespace Mirle.Agv.INX.Controller
{
    public class RFIDReader_OMRON : BarcodeReader
    {
        private Socket socket = null;
        private bool triggering = false;

        override public bool Connect(string ip, ref string errorMessage)
        {
            try
            {
                if (Connected)
                {
                    if (!Disconnect(ref errorMessage))
                        return false;
                }

                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 500);
                string[] split = Regex.Split(ip, ":", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));
                socket.Connect(IPAddress.Parse(split[0]), int.Parse(split[1]));
                if (socket.Connected)
                {
                    ipOrComport = ip;
                    Connected = true;
                    return true;
                }
                else
                {
                    errorMessage = String.Concat("連線失敗, IP : ", ip, ", ErrorCode : ", "Not Connect" );
                    return false;
                }
                

            }
            catch (Exception ex)
            {
                errorMessage = String.Concat("連線失敗 : ", ex.ToString());
                return false;
            }
        }

        override public bool Disconnect(ref string errorMessage)
        {
            try
            {
                if (!Connected)
                    return true;
                else
                {
                    string message = "";

                    if (triggering)
                        StopReadBarcode(ref message, ref errorMessage);

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

        public override void ResetError()
        {
            string errorMessage = "";

            if (Error)
            {
                if (Connect(ipOrComport, ref errorMessage))
                    Error = false;
            }
        }

        override public bool ReadBarcode(ref string message, int timeout, ref string errorMessage)
        {
            try
            {
                //return false;
                if (!Connected)
                {
                    errorMessage = "RFIDReader 尚未連線";
                    return false;
                }
                else if (Error)
                {
                    errorMessage = "RFIDReader Error中";
                    return false;
                }
                else if (!triggering)
                {
                    byte[] commandByte = new byte[11];
                    byte[] receiveByte = new byte[14];
                    triggering = true;
                    try
                    {
                        do
                        {
                            socket.ReceiveTimeout = 500;
                            socket.Receive(receiveByte);
                            Thread.Sleep(500);
                        } while (true);
                    }
                    catch (Exception ex)
                    {

                    }
                    receiveByte = new byte[14];
                    message = "";
                    //commandByte = Encoding.ASCII.GetBytes("1012345678\r");
                    commandByte = Encoding.ASCII.GetBytes("010000000004\r");
                    socket.Send(commandByte);
                    socket.ReceiveTimeout = 500;
                    socket.Receive(receiveByte);
                    if (receiveByte[0] == 48 && receiveByte[1] == 48)
                    {
                        message = Encoding.ASCII.GetString(receiveByte);
                        string ans = "";
                        for (int i = 0; i < 14; i++)
                        {
                            string c = message.Substring(i, 2);
                            i++;
                            switch (c)
                            {
                                case "00":
                                break;
                                #region 數字 
                                case "30":
                                    ans += 0;
                                    break;
                                case "31":
                                    ans += 1;
                                    break;
                                case "32":
                                    ans += 2;
                                    break;
                                case "33":
                                    ans += 3;
                                    break;
                                case "34":
                                    ans += 4;
                                    break;
                                case "35":
                                    ans += 5;
                                    break;
                                case "36":
                                    ans += 6;
                                    break;
                                case "37":
                                    ans += 7;
                                    break;
                                case "38":
                                    ans += 8;
                                    break;
                                case "39":
                                    ans += 9;
                                    break;
                                #endregion
                                #region 字元
                                case "41":
                                    ans += "A";
                                    break;
                                case "42":
                                    ans += "B";
                                    break;
                                case "43":
                                    ans += "C";
                                    break;
                                case "44":
                                    ans += "D";
                                    break;
                                case "45":
                                    ans += "E";
                                    break;
                                case "46":
                                    ans += "F";
                                    break;
                                case "47":
                                    ans += "G";
                                    break;
                                case "48":
                                    ans += "H";
                                    break;
                                case "49":
                                    ans += "I";
                                    break;
                                case "4A":
                                    ans += "J";
                                    break;
                                case "4B":
                                    ans += "K";
                                    break;
                                case "4C":
                                    ans += "L";
                                    break;
                                case "4D":
                                    ans += "M";
                                    break;
                                case "4E":
                                    ans += "N";
                                    break;
                                case "4F":
                                    ans += "O";
                                    break;
                                case "50":
                                    ans += "P";
                                    break;
                                case "51":
                                    ans += "Q";
                                    break;
                                case "52":
                                    ans += "R";
                                    break;
                                case "53":
                                    ans += "S";
                                    break;
                                case "54":
                                    ans += "T";
                                    break;
                                case "55":
                                    ans += "U";
                                    break;
                                case "56":
                                    ans += "V";
                                    break;
                                case "57":
                                    ans += "W";
                                    break;
                                case "58":
                                    ans += "X";
                                    break;
                                case "59":
                                    ans += "Y";
                                    break;
                                case "5A":
                                    ans += "Z";
                                    break;
                                #endregion
                                default:
                                    message = "";
                                    triggering = false;
                                    return false;
                            }
                            //byte[] aaa =  Encoding.ASCII.GetBytes(d);
                        }
                        message = ans;
                        triggering = false;
                        return true;
                    }
                    else
                    {
                        message = "";
                        triggering = false;
                        return false;
                    }
                    // = reader.ExecCommand("LON", timeout);

                    //if (message == null || message == "")
                    //{
                    //    if (reader.LastErrorInfo == ErrorCode.Closed)
                    //        Error = true;

                    //    errorMessage = String.Concat("讀取失敗, ErrorCode : ", reader.LastErrorInfo.ToString());
                    //    return false;
                    //}
                    //else
                    //    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                errorMessage = String.Concat("LON Exception : ", ex.ToString());
                triggering = false;
                return false;
            }
        }

        //override public bool StopReadBarcode(ref string message, ref string errorMessage)
        //{
        //    try
        //    {
        //        if (!Connected)
        //        {
        //            errorMessage = "Keyence尚未連線";
        //            return false;
        //        }
        //        else if (!triggering)
        //            return true;
        //        else
        //        {
        //            message = reader.ExecCommand("LOFF");
        //            return true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        errorMessage = String.Concat("LOff Exception : ", ex.ToString());
        //        return false;
        //    }
        //}

        //override public bool ChangeMode(string loadNum, ref string errorMessage)
        //{
        //    try
        //    {
        //        if (!Connected)
        //        {
        //            errorMessage = "Keyence尚未連線";
        //            return false;
        //        }
        //        else
        //        {
        //            reader.ExecCommand(String.Concat("BLOAD,", loadNum));
        //            return true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        errorMessage = String.Concat("ChangeMode Exception : ", ex.ToString());
        //        return false;
        //    }
        //}

        //override public bool SaveMode(string loadNum, ref string errorMessage)
        //{
        //    try
        //    {
        //        if (!Connected)
        //        {
        //            errorMessage = "Keyence尚未連線";
        //            return false;
        //        }
        //        else
        //        {
        //            reader.ExecCommand(String.Concat("BSAVE,", loadNum));
        //            return true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        errorMessage = String.Concat("Save Mode Exception : ", ex.ToString());
        //        return false;
        //    }
        //}
    }
}
