using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.MiddlePackage.Umtc.Battery
{
    interface IBatteryHandler : Tools.IMessageHandler, IMidBatteryAgent
    {
        event EventHandler<Model.BatteryStatus> OnUpdateBatteryStatusEvent;

        void SetPercentageTo(int percentage);
        void StopCharge();
        void StartCharge(EnumAddressDirection chargeDirection);
        void GetBatteryAndChargeStatus();
    }
}
