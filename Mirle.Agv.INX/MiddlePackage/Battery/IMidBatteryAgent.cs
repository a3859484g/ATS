using Mirle.Agv.MiddlePackage.Umtc.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.MiddlePackage.Umtc.Battery
{
    interface IMidBatteryAgent
    {
        event EventHandler<ChargeArgs> StartChargeEvent;
        event EventHandler<MidRequestArgs> StopChargeEvent;
        event EventHandler<BatteryStatus> OnBatteryStatusRequestEvent;

        void SetBatteryStatus(BatteryStatus batteryStatus);
        void SetHighPercentageThreshold(int thd);
    }

    public class ChargeArgs : EventArgs
    {
        public MidRequestArgs RequestArgs { get; set; } = new MidRequestArgs();
        public string AddressId { get; set; } = "";
    }
}
