using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.MiddlePackage.Umtc.Model
{
    public class BatteryStatus
    {
        public double Ah { get; set; } = 0;
        public double Voltage { get; set; } = 0;
        public int Percentage { get; set; } = 70;
        public int Temperature { get; set; } = 0;
        public bool IsCharging { get; set; } = false;

        public BatteryStatus() { }        

        public BatteryStatus(BatteryStatus batteryStatus)
        {
            this.Ah = batteryStatus.Ah;
            this.Voltage = batteryStatus.Voltage;
            this.Percentage = batteryStatus.Percentage;
            this.Temperature = batteryStatus.Temperature;
            this.IsCharging = batteryStatus.IsCharging;
        }
    }
}
