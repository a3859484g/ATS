using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class LoadUnloadControlData
    {
        public EnumCommandStatus CommandStatus { get; set; } = EnumCommandStatus.Idle;

        public LoadUnloadCommandData LoadUnloadCommand { get; set; } = null;

        public string CommnadID
        {
            get
            {
                LoadUnloadCommandData temp = LoadUnloadCommand;

                return (temp == null ? "" : temp.CommandID);
            }
        }

        public string ResetAlignmentDeviceToZeroMessage { get; set; } = "";
        public bool ResetAlignmentDeviceToZeroCanNextStep { get; set; } = false;
        public bool ResetAlignmentDeviceToZeroGoNext { get; set; } = false;

        public bool CreateCommanding { get; set; } = false;
        public object CreateCommandingLockObjcet { get; set; } = new object();

        public string LoadUnloadCantAutoReason { get; set; } = "";
        public string LoadUnloadNotReadyReason { get; set; } = "";
        public bool LoadUnloadCanAuto { get; set; } = false;

        private bool forkHome { get; set; } = false;

        public bool ForkHome
        {
            get
            {
                if (LocalData.Instance.MainFlowConfig.SimulateMode)
                    return true;
                else if (平衡Z軸 || LocalData.Instance.MIPCData.BypassIO)
                    return false;
                else
                    return forkHome;
            }

            set
            {
                forkHome = value;
            }
        }

        public bool Homing { get; set; } = false;
        public bool ResetZero { get; set; } = false;
        public bool HomeStop { get; set; } = false;

        public bool Ready
        {
            get
            {
                        //return ForkHome && LoadUnloadCommand == null;    // ATS Allen
                        return ForkHome && LoadUnloadCommand == null && !平衡Z軸 && !Homing;
            }
        }

        public AlignmentValueData AlignmentValue { get; set; } = null;

        public bool ErrorBit { get; set; } = false;

        public bool Loading { get; set; } = false;
        public bool Loading_LogicFlag { get; set; } = false;
        public string CstID { get; set; } = "";


        public bool Loading_Left { get; set; } = false;
        public bool Loading_LogicFlag_Left { get; set; } = false;
        public string CstID_Left { get; set; } = "";


        public bool Loading_Right { get; set; } = false;
        public bool Loading_LogicFlag_Right { get; set; } = false;
        public string CstID_Right { get; set; } = "";

        public Dictionary<EnumCstInAGVLocate, bool> AllLoading { get; set; } = new Dictionary<EnumCstInAGVLocate, bool>();
        public Dictionary<EnumCstInAGVLocate, bool> AllLoadingFlag { get; set; } = new Dictionary<EnumCstInAGVLocate, bool>();
        public Dictionary<EnumCstInAGVLocate, string> AllCstID { get; set; } = new Dictionary<EnumCstInAGVLocate, string>();

        public bool DoubleStoregeL { get; set; } = false;
        public bool DoubleStoregeR { get; set; } = false;

        public Dictionary<string, AxisFeedbackData> CVFeedbackData = new Dictionary<string, AxisFeedbackData>();

        public Dictionary<string, double> CVEncoderOffsetValue { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> CVHomeOffsetValue { get; set; } = new Dictionary<string, double>();

        //ATS LoadUnload command
        public Dictionary<string, LoadUnloadRobotCommand> RobotCommand { get; set; } = new Dictionary<string, LoadUnloadRobotCommand>();
        public float NowCommand { get; set; } = 0;

        public bool 平衡Z軸 { get; set; } = false;
        public bool Z軸主從誤差過大 { get; set; } = false;
        public double Z軸主從誤差 { get; set; } = 0;

        public bool EncoderOffset讀取 { get; set; } = false;

        public bool Encoder已回Home { get; set; } = false;
        public double Z軸EncoderOffset { get; set; } = 0;
        public double Z軸_SlaveEncoderOffset { get; set; } = 0;
        public double P軸EncoderOffset { get; set; } = 0;
        public double Theta軸EncoderOffset { get; set; } = 0;
        public double Y軸EncoderOffset { get; set; } = 0;

        public bool NotSendTR_REQ { get; set; } = false;
        public bool NotSendBUSY { get; set; } = false;
        public bool NotForkBusyAction { get; set; } = false;
        public bool NotSendCOMPT { get; set; } = false;
        public bool NotSendAllOff { get; set; } = false;

        public bool z軸EncoderHome { get; set; } = false;
        public bool p軸EncoderHome { get; set; } = false;
        public bool theta軸EncoderHome { get; set; } = false;

        public List<double> PIOTimeoutList { get; set; } = new List<double>();
        public List<EnumPIOStatus> PIOTimeoutTageList { get; set; } = new List<EnumPIOStatus>();

        public Dictionary<EnumPIOStatus, int> PIOStatusToFlowIndex = new Dictionary<EnumPIOStatus, int>();

        public int HomeRepeatTatolCount { get; set; } = 0;
        public int HomeRepeatNowCount { get; set; } = 0;
        public bool StopHomeRepeat { get; set; } = false;
        public List<AlignmentValueData> HomeRepeatData = new List<AlignmentValueData>();

        public List<LoadUnloadCommandData> CommandHistory { get; set; } = new List<LoadUnloadCommandData>();

        private int maxLoadUnloadCommandHistoryCount = 30;

        public void SetCommandHisotry(LoadUnloadCommandData command)
        {
            List<LoadUnloadCommandData> newList = new List<LoadUnloadCommandData>();

            newList.Add(command);

            for (int i = 0; i < CommandHistory.Count && newList.Count < maxLoadUnloadCommandHistoryCount; i++)
                newList.Add(CommandHistory[i]);

            CommandHistory = newList;
        }
    }
}
