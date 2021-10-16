using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Controller
{
    public class DistanceSensor_Keyence : DistanceSensor
    {
        private Socket socket;
        private string ip = "";

        private NetworkStream stream;
        //private StreamReader sr;
        //private StreamWriter sw;

        public override void Initial(string ip)
        {
            this.ip = ip;
        }

        public override bool Connect()
        {
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(ip, 64000);

                if (socket.Connected)
                {
                    Connected = true;
                    return true;
                }
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        public override bool GetDistanceSensorData(ref string data)
        {
            try
            {
                if (Connected)
                {
                    stream = new NetworkStream(socket);
                    StreamReader sr = new StreamReader(stream);
                    StreamWriter sw = new StreamWriter(stream);
                    sw.Write("MS\r\n");
                    sw.Flush();
                    data = sr.ReadLine();
                    return true;
                }
                else
                {
                    data = "連線失敗";
                    return false;
                }
            }
            catch (Exception ex)
            {
                data = ex.ToString();
                return false;
            }
        }
    }
}
