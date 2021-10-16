using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class LoadUnloadCommandData
    {
        public string CommandID { get; set; }
        public EnumAutoState CommandAutoManual { get; set; } = EnumAutoState.Manual;
        public DateTime CommandStartTime { get; set; } = DateTime.Now;
        public double StartSOC { get; set; } = 0;
        public double StartVoltage { get; set; } = 0;

        public DateTime CommandEndTime { get; set; } = DateTime.Now;
        public EnumLoadUnloadErrorLevel ErrorLevel = EnumLoadUnloadErrorLevel.None;
        public double EndSOC { get; set; } = 0;
        public double EndVoltage { get; set; } = 0;

        public EnumCstInAGVLocate CstLocate { get; set; } = EnumCstInAGVLocate.None;

        public string AddressID { get; set; }
        public int StatusStep { get; set; } = 0;

        public bool IsLowspeed { get; set; } = false;
        public string CommandCSTID { get; set; } = "";

        public string StepString { get; set; } = "";

        public AlignmentValueData CommandStartAlignmentValue { get; set; } = null;
        public AlignmentValueData CommandEndAlignmentValue { get; set; } = null;

        public EnumLoadUnload Action { get; set; }
        public EnumStageDirection StageDirection { get; set; }
        public EnumStageDirection PIODirection { get; set; }
        public int StageNumber { get; set; }
        public double SpeedPercent { get; set; }
        public bool NeedPIO { get; set; }
        public bool BreakenStopMode { get; set; }
        public bool UsingAlignmentValue { get; set; }
        public bool WaitFlag { get; set; } = false;
        public bool GoNext { get; set; } = false;
        public bool GoBack { get; set; } = false;
        public int GoBackInt { get; set; } = 0;

        public bool Pause { get; set; } = false;
        public bool CancelRequest { get; set; } = false;
        public bool StopRequest { get; set; } = false;

        public EnumPIOStatus PIOResult { get; set; } = EnumPIOStatus.None;

        public EnumLoadUnloadComplete CommandResult { get; set; } = EnumLoadUnloadComplete.None;
        public EnumLoadUnloadControlErrorCode ErrorCode { get; set; } = EnumLoadUnloadControlErrorCode.None;
        
        public bool ReadFail { get; set; } = false;
        public string CSTID { get; set; } = "";

        public double Enocder_P { get; set; } = 0;
        public double Encoder_Y { get; set; } = 0;
        public double Encoder_Z { get; set; } = 0;
        public double Encoder_Theta { get; set; } = 0;
        public List<PIODataAndTime> PIOHistory { get; set; } = new List<PIODataAndTime>();
    }
}
