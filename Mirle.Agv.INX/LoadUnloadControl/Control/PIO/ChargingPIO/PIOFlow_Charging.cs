using Mirle.Agv.INX.Controller;
using Mirle.Agv.INX.Controller.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Control
{
    public class PIOFlow_Charging : PIOFlow
    {
        public string ConfirmSensor { get; set; } = "";
        public string ChargingSaftey { get; set; } = "";
        public EnumChargingStatus ChargingStep { get; set; } = EnumChargingStatus.Idle;
        protected Logger commandRecordLogger = LoggerAgent.Instance.GetLooger("CommandRecord");

        public bool Charging { get; set; } = false;

        public bool ManualChargingSafetyOn { get; set; } = false;

        public virtual void ChargingSafetyOnOff(bool OnOff)
        {

        }

        public virtual bool GetChargingSafetyOnOff
        {
            get
            {
                return false;
            }
        }

        public virtual bool GetConfirmSensorOnOff
        {
            get
            {
                return false;
            }
        }
    }
}
