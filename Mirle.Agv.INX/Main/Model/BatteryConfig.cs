using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    [Serializable]
    public class BatteryConfig
    {
        public double HighBattery_SOC { get; set; } = 95;
        public double HighBattery_Voltage { get; set; } = 54;

        public double LowBattery_SOC { get; set; } = 40;
        public double LowBattery_Voltage { get; set; } = 48;
        public double Battery_WarningTemperature { get; set; } = 38;

        public double ShutDownBattery_SOC { get; set; } = 35;
        public double ShutDownBattery_Voltage { get; set; } = 47;
        public double Battery_ShowDownTemperature { get; set; } = 39;

        public double ChargingMaxCurrent { get; set; } = 21;
        public double ChargingMaxTemperature { get; set; } = 38;

        public double AlarmDelayTime { get; set; } = 15000;
        public double ShutDownDelayTime { get; set; } = 30000;

        public BatteryConfig()
        {
            if (LocalData.Instance.MainFlowConfig != null)
            {
                switch (LocalData.Instance.MainFlowConfig.AGVType)
                {
                    case EnumAGVType.UMTC:
                        HighBattery_SOC = 101;
                        HighBattery_Voltage = 54;
                        LowBattery_SOC = 35;
                        LowBattery_Voltage = 48.5;
                        Battery_WarningTemperature = 45;
                        ShutDownBattery_SOC = 25;
                        ShutDownBattery_Voltage = 47.5;
                        Battery_ShowDownTemperature = 46;
                        ChargingMaxCurrent = 50;
                        ChargingMaxTemperature = 45;
                        break;
                }
            }
        }
    }
}
