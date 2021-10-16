using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Controller
{
    public class DistanceSensor
    {
        public bool Connected { get; set; } = false;

        public virtual void Initial(string ip)
        {
        }

        public virtual bool Connect()
        {
            return false;
        }

        public virtual void Disconnect()
        {
        }

        public virtual bool GetDistanceSensorData(ref string data)
        {
            return false;
        }
    }
}
