using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model.Configs
{
    [Serializable]
    public class MainFlowConfig
    {
        //public string BatteryLogPath { get; set; }
        //public string BatteryBackupLogPath { get; set; }
        public EnumAGVType AGVType { get; set; }
        public bool PowerSavingMode { get; set; } = false;
        public bool HomeInUpOrDownPosition { get; set; } = false;
        public bool CheckPassLineSensor { get; set; } = false;
        public bool IdleNotRecordCSV { get; set; } = false;
        public bool SimulateMode { get; set; }
        public int LPMSComport { get; set; }
    }
}
