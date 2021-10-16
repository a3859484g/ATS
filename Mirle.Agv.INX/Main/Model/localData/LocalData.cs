using Mirle.Agv.INX.Model.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class LocalData
    {
        private static readonly LocalData localData = new LocalData();
        public static LocalData Instance { get { return localData; } }

        public Dictionary<int, AlarmCodeAndSetOrReset> AllAlarmBit = new Dictionary<int, AlarmCodeAndSetOrReset>();

        public string ConfigPath { get; set; } = @"D:\MecanumConfigs";
        public MapConfig MapConfig { get; set; }

        public MainFlowConfig MainFlowConfig { get; set; }

        public string ErrorLogName { get; set; } = "Error";
        public int ErrorLevel { get; set; } = 5;

        private EnumLoginLevel loginLevel { get; set; } = EnumLoginLevel.User;

        public EnumLoginLevel LoginLevel
        {
            get
            {
                if (SimulateMode)
                    return EnumLoginLevel.MirleAdmin;
                else
                    return loginLevel;
            }
            set
            {
                loginLevel = value;
            }
        }

        public EnumLanguage Language { get; set; } = EnumLanguage.None;
        public bool SimulateMode { get; set; } = false;

        public MapInfo TheMapInfo { get; set; } = new MapInfo();

        public bool AGVCOnline { get; set; } = false;

        public BatteryInfo BatteryInfo { get; set; } = new BatteryInfo();

        public BatteryConfig BatteryConfig { get; set; }

        public MapAGVPosition Real { get; set; } = null;

        public double MoveDirectionAngle { get; set; } = 0;

        public MapAGVPosition LastAGVPosition { get; set; } = null;
        public VehicleLocation Location { get; set; } = new VehicleLocation();

        public EnumAutoState AutoManual { get; set; } = EnumAutoState.Manual;

        public MoveControlData MoveControlData { get; set; } = new MoveControlData();
        public LoadUnloadControlData LoadUnloadData { get; set; } = new LoadUnloadControlData();
        public MIPCControlData MIPCData { get; set; } = new MIPCControlData();

        public CommunicationData MiddlerStatus { get; set; }
        public CommunicationData AGVCStatus { get; set; }

        public double LocalVersion { get; set; } = 210518.0;
        public double MIPCVersion { get; set; } = 0;
        public double MotionVersion { get; set; } = 0;
        public double MiddlerVersion { get; set; } = 0;

        public double MIPCData_MotionVersion { get; set; } = 210316.1;
        public double MIPCData_MIPCVersion { get; set; } = 210308.0;

        public int PingTestIntervalTime { get; set; } = 1000;

        public bool ReserveStatus_forView = false;

        public bool CoverMIPCBug { get; set; } = true;

        public Dictionary<EnumLanguage, Dictionary<string, string>> AllLanguageProfaceString = new Dictionary<EnumLanguage, Dictionary<string, string>>();

        public string GetProfaceString(EnumLanguage language, string tag)
        {
            if (AllLanguageProfaceString.ContainsKey(language) && AllLanguageProfaceString[language] != null &&
                AllLanguageProfaceString[language].ContainsKey(tag))
                return AllLanguageProfaceString[language][tag];
            else if (AllLanguageProfaceString[EnumLanguage.None].ContainsKey(tag))
                return AllLanguageProfaceString[EnumLanguage.None][tag];
            else
                return tag;
        }

        public string TestString { get; set; } = "";
    }
}