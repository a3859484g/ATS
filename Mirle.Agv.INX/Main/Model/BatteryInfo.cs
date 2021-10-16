using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class BatteryInfo
    {
        private double battery_SOC { get; set; } = 0;
        public double BatteryAlarm { get; set; } = 0;

        public double Battery_SOC
        {
            get
            {
                if (LocalData.Instance.MainFlowConfig.SimulateMode)
                    return 90;
                else
                    return battery_SOC;
            }

            set
            {
                battery_SOC = value;
            }
        }

        public double Battery_V { get; set; } = 0;
        public double Battery_A { get; set; } = 0;
        public double Battery_溫度1 { get; set; } = -1;
        public double Battery_溫度2 { get; set; } = -1;
        public double Meter_V { get; set; } = 0;
        public double Meter_A { get; set; } = 0;
        public double Meter_W { get; set; } = 0;
        public double Meter_WH { get; set; } = 0;
        public double[] CellArray { get; set; } = new double[15];

        public BatteryInfo()
        {
            for (int i = 0; i < CellArray.Length; i++)
                CellArray[i] = 0;
        }

        #region bool LowV_Warn.
        private bool lowV_Warn = false;
        private Stopwatch lowV_Warn_Timer = new Stopwatch();

        public bool LowV_Warn
        {
            get
            {
                if (lowV_Warn && lowV_Warn_Timer.ElapsedMilliseconds >= LocalData.Instance.BatteryConfig.AlarmDelayTime)
                {
                    lowV_Warn_Timer.Stop();
                    return true;
                }
                else
                    return false;
            }

            set
            {
                if (lowV_Warn != value)
                {
                    if (value)
                        lowV_Warn_Timer.Restart();
                    else
                        lowV_Warn_Timer.Stop();

                    lowV_Warn = value;
                }
            }
        }
        #endregion

        #region bool LowSOC_Warn.
        private bool lowSOC_Warn = false;
        private Stopwatch lowSOC_Warn_Timer = new Stopwatch();

        public bool LowSOC_Warn
        {
            get
            {
                if (lowSOC_Warn && lowSOC_Warn_Timer.ElapsedMilliseconds >= LocalData.Instance.BatteryConfig.AlarmDelayTime)
                {
                    lowSOC_Warn_Timer.Stop();
                    return true;
                }
                else
                    return false;
            }

            set
            {
                if (lowSOC_Warn != value)
                {
                    if (value)
                        lowSOC_Warn_Timer.Restart();
                    else
                        lowSOC_Warn_Timer.Stop();

                    lowSOC_Warn = value;
                }
            }
        }
        #endregion

        #region bool ShutDownV_Alarm.
        private bool shutDownV_Alarm = false;
        private Stopwatch shutDownV_Timer = new Stopwatch();

        public bool ShutDownV_Alarm
        {
            get
            {
                if (shutDownV_Alarm && shutDownV_Timer.ElapsedMilliseconds >= LocalData.Instance.BatteryConfig.AlarmDelayTime)
                {
                    shutDownV_Timer.Stop();
                    return true;
                }
                else
                    return false;
            }

            set
            {
                if (shutDownV_Alarm != value)
                {
                    if (value)
                        shutDownV_Timer.Restart();
                    else
                        shutDownV_Timer.Stop();

                    shutDownV_Alarm = value;
                }
            }
        }
        #endregion

        #region bool ShutDownSOC_Alarm.
        private bool shutDownSOC_Alarm = false;
        private Stopwatch shutDownSOC_Timer = new Stopwatch();

        public bool ShutDownSOC_Alarm
        {
            get
            {
                if (shutDownSOC_Alarm && shutDownSOC_Timer.ElapsedMilliseconds >= LocalData.Instance.BatteryConfig.AlarmDelayTime)
                {
                    shutDownSOC_Timer.Stop();
                    return true;
                }
                else
                    return false;
            }

            set
            {
                if (shutDownSOC_Alarm != value)
                {
                    if (value)
                        shutDownSOC_Timer.Restart();
                    else
                        shutDownSOC_Timer.Stop();

                    shutDownSOC_Alarm = value;
                }
            }
        }
        #endregion

        #region bool Battery_DisConnect.
        private bool battery_DisConnect = false;
        private Stopwatch battery_DisConnect_Timer = new Stopwatch();

        public bool Battery_DisConnect
        {
            get
            {
                if (battery_DisConnect && battery_DisConnect_Timer.ElapsedMilliseconds >= LocalData.Instance.BatteryConfig.AlarmDelayTime / 3)
                {
                    battery_DisConnect_Timer.Stop();
                    return true;
                }
                else
                    return false;
            }

            set
            {
                if (battery_DisConnect != value)
                {
                    if (value)
                        battery_DisConnect_Timer.Restart();
                    else
                        battery_DisConnect_Timer.Stop();

                    battery_DisConnect = value;
                }
            }
        }
        #endregion

        #region bool ShutDownAction.
        private bool shutDownAction = false;
        private Stopwatch shutDownAction_Timer = new Stopwatch();

        public bool ShutDownAction
        {
            get
            {
                if (shutDownAction && shutDownAction_Timer.ElapsedMilliseconds >= LocalData.Instance.BatteryConfig.ShutDownDelayTime)
                {
                    shutDownAction_Timer.Stop();
                    return true;
                }
                else
                    return false;
            }

            set
            {
                if (shutDownAction != value)
                {
                    if (value)
                        shutDownAction_Timer.Restart();
                    else
                        shutDownAction_Timer.Stop();

                    shutDownAction = value;
                }
            }
        }
        #endregion
    }
}
