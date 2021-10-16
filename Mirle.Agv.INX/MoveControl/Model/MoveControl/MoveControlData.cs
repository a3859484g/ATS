using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class MoveControlData
    {
        public MoveCommandData MoveCommand { get; set; } = null;

        public string CommnadID
        {
            get
            {
                MoveCommandData temp = MoveCommand;

                return (temp == null ? "" : temp.CommandID);
            }
        }

        public bool CreateCommanding { get; set; } = false;
        public object CreateCommandingLockObjcet { get; set; } = new object();

        public string MoveControlCantAutoReason { get; set; } = "";
        public string MoveControlNotReadyReason { get; set; } = "";
        public bool MoveControlCanAuto { get; set; } = false;

        public bool NeedResetMotionAlarm { get; set; } = false;

        private bool ready = false;
        public bool Ready
        {
            get
            {
                if (CreateCommanding || SpecialFlow || MotionControlData.JoystickMode || LocalData.Instance.MIPCData.BypassIO)
                    return false;
                else
                    return ready;
            }

            set
            {
                ready = value;
            }
        }

        public bool SpecialFlow { get; set; } = false;

        public List<MoveCommandRecord> MoveCommandRecordList { get; set; } = new List<MoveCommandRecord>();

        public string LastCommandID { get; set; } = "";

        private int maxOfCommandRecordList = 20;

        public object LockMoveCommandRecordObject { get; } = new object();

        public string MoveCommandRecordString { get; set; } = "";

        private int maxOfMoveCOmmandRecordStringLength = 5000;

        public void AddMoveCommandRecordList(MoveCommandData command, EnumMoveComplete report)
        {
            lock (LockMoveCommandRecordObject)
            {
                LastCommandID = command.CommandID;
                MoveCommandRecordList.Insert(0, new MoveCommandRecord(command, report));

                while (MoveCommandRecordList.Count > maxOfCommandRecordList)
                    MoveCommandRecordList.RemoveAt(MoveCommandRecordList.Count - 1);

                MoveCommandRecordString = String.Concat(MoveCommandRecordList[0].LogString, "\r\n", MoveCommandRecordString);

                if (MoveCommandRecordString.Length > maxOfMoveCOmmandRecordStringLength)
                    MoveCommandRecordString = MoveCommandRecordString.Substring(0, maxOfMoveCOmmandRecordStringLength);
            }
        }

        public bool ErrorBit { get; set; } = false;

        public MoveControlConfig MoveControlConfig { get; set; }
        public CreateMoveCommandListConfig CreateMoveCommandConfig { get; set; }

        //public Dictionary<string, AGVTurnParameter> TurnParameter { get; set; } = new Dictionary<string, AGVTurnParameter>();

        public LocateControlData LocateControlData { get; set; } = new LocateControlData();
        public MotionControlData MotionControlData { get; set; } = new MotionControlData();

        public ThetaSectionDeviation ThetaSectionDeviation { get; set; } = null;

        public MoveControlSensorStatus SensorStatus { get; set; } = new MoveControlSensorStatus();

        public bool SimulateBypassLog { get; set; } = false;

        private Stopwatch resultTimer = new Stopwatch();
        private double showTime = 3000;

        private string reviseAndSetPositionResult = "";

        public string ReviseAndSetPositionResult
        {
            get
            {
                if (reviseAndSetPositionResult != "" && resultTimer.ElapsedMilliseconds >= showTime)
                {
                    reviseAndSetPositionResult = "";
                    resultTimer.Reset();
                }

                return reviseAndSetPositionResult;
            }

            set
            {
                resultTimer.Restart();
                reviseAndSetPositionResult = value;
            }
        }

        public string ReviseAndSetPositionData { get; set; } = "";

        public EnumProfaceStringTag ReviseAndSetPositionStatus
        {
            get
            {
                if (ReviseAndSetPosition)
                {
                    if (SpecialFlow)
                        return EnumProfaceStringTag.補正中;
                    else
                        return EnumProfaceStringTag.重定位;
                }
                else
                {
                    if (LocateControlData.SlamLocateOK)
                        return EnumProfaceStringTag.定位OK;
                    else
                        return EnumProfaceStringTag.定位NG;
                }
            }
        }

        public bool ReviseAndSetPosition { get; set; } = false;
    }
}
