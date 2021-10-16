using Mirle.Agv.INX.Controller;
using Mirle.Agv.INX.Controller.Tools;
using Mirle.Agv.INX.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Mirle.Agv.INX.Control
{
    class LoadUnLoad_Simulation : LoadUnload
    {

        private BarcodeReader_Keyence sr1000_R = null;
        private DistanceSensor_Keyence distanceSensor_R = null;

        private BarcodeReader_Keyence cstIDReader = null;

        private RFIDReader cstIDReaderL = null;
        private RFIDReader cstIDReaderR = null;

        private bool alignmentDevicOK = false;

        private double delayTime = 0;

        private Dictionary<string, Dictionary<EnumLoadUnloadAxisCommandType, string>> axisCommandString = new Dictionary<string, Dictionary<EnumLoadUnloadAxisCommandType, string>>();

        private List<string> jogStopStringList = new List<string>();
        private List<float> jogStopValueList = new List<float>();

        private double loadUnloadZOffset = 1;

        private Dictionary<string, string> axisPosLimitSensor = new Dictionary<string, string>();
        private Dictionary<string, string> axisNagLimitSensor = new Dictionary<string, string>();
        private Dictionary<string, string> axisHomeSensor = new Dictionary<string, string>();


        private Dictionary<string, double> inPositionRange = new Dictionary<string, double>();

        private Dictionary<string, EnumAxisMoveStatus> axisPreStatus = new Dictionary<string, EnumAxisMoveStatus>();
        private Dictionary<string, Stopwatch> axisPreTimer = new Dictionary<string, Stopwatch>();
        private double axisStatusDelayTime = 1000;

        private bool logMode = true;

        private bool initialEnd = false;

        private string robotRBInitCommandTag = "RB_Running";
        private string robotCarMoveCommandTag = "RB_Running";
        private string robotRBGripperInitCommandTag = "RB_Running";
        private string robotLDULDCommandTag = "RB_Running";

        private string robotCommandFinish = "RB_Finished";
        private string robotStatus = "RB_Status";

        private string foupL1_loading = "FoupL1在席";
        private string foupL2_loading = "FoupL2在席";
        private string foupR1_loading = "FoupR1在席";
        private string foupR2_loading = "FoupR2在席";


        public DataDelayAndChange FOUPL1_loading = new DataDelayAndChange(0, EnumDelayType.OnDelay);
        public DataDelayAndChange FOUPL2_loading = new DataDelayAndChange(0, EnumDelayType.OnDelay);
        public DataDelayAndChange FOUPR1_loading = new DataDelayAndChange(0, EnumDelayType.OnDelay);
        public DataDelayAndChange FOUPR2_loading = new DataDelayAndChange(0, EnumDelayType.OnDelay);

        public override void Initial(MIPCControlHandler mipcControl, AlarmHandler alarmHandler)
        {
            this.alarmHandler = alarmHandler;
            this.mipcControl = mipcControl;
            device = MethodInfo.GetCurrentMethod().ReflectedType.Name;

            ReadPIOTimeoutCSV();
            ReadAddressOffsetCSV();
            ReadLoadUnloadOffsetConfigXML();
            ReadStargeCSV();
            ReadAxisData();
            //InitialRollerData();
            ReadStageNumberToBarcodeReaderSettingXML();
            CheckAllStringInMIPCConfig();
            ReadLoadUnloadRobotCommandCSV();   //ATS增加Robot command轉換

            RightPIO = new PIOFlow_UMTC_LoadUnloadSemi();
            RightPIO.Initial(alarmHandler, mipcControl, "Right", "R", normalLogName);
            CanRightLoadUnload = true;

            LeftPIO = new PIOFlow_UMTC_LoadUnloadSemi();
            LeftPIO.Initial(alarmHandler, mipcControl, "Left", "L", normalLogName);
            CanLeftLoadUnload = true;


            ConnectAlignmentDevice();
            initialEnd = true;

        }

        public override void CloseLoadUnload()
        {
        }

        private void CheckAllStringInMIPCConfig()
        {
            for (int i = 0; i < AxisList.Count; i++)
            {
                if (!axisDataList.ContainsKey(AxisList[i]))
                {
                    WriteLog(3, "", String.Concat("AxisDataList.csv內 無 AxisName = ", AxisList[i]));
                    configAllOK = false;
                }
            }
        }

        public bool CheckAixsIsServoOn(string axisName)
        {
            AxisFeedbackData temp = localData.LoadUnloadData.CVFeedbackData[axisName];
            AxisFeedbackData temp2 = (axisName == EnumLoadUnloadAxisName.Z軸.ToString() ? localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()] : temp);

            if (temp == null || temp2 == null)
                return false;
            else if (temp.AxisServoOnOff == EnumAxisServoOnOff.ServoOff || temp2.AxisServoOnOff == EnumAxisServoOnOff.ServoOff)
                return false;
            else
                return true;
        }

        private List<EnumLoadUnloadControlErrorCode> alarmCodeClearList = new List<EnumLoadUnloadControlErrorCode>()
        {
            EnumLoadUnloadControlErrorCode.TA1_Timeout,
            EnumLoadUnloadControlErrorCode.TA2_Timeout,
            EnumLoadUnloadControlErrorCode.TA3_Timeout,
            EnumLoadUnloadControlErrorCode.TP1_Timeout,
            EnumLoadUnloadControlErrorCode.TP2_Timeout,
            EnumLoadUnloadControlErrorCode.TP3_Timeout,
            EnumLoadUnloadControlErrorCode.TP4_Timeout,
            EnumLoadUnloadControlErrorCode.TP5_Timeout,

            EnumLoadUnloadControlErrorCode.取放貨中EQPIOOff,
            EnumLoadUnloadControlErrorCode.取放命令與EQRequest不相符,
            EnumLoadUnloadControlErrorCode.RollerStop後CV訊號異常,
            EnumLoadUnloadControlErrorCode.取放貨中CV檢知異常,
            EnumLoadUnloadControlErrorCode.取放貨中EMS,
            EnumLoadUnloadControlErrorCode.取貨_LoadingOn異常,
            EnumLoadUnloadControlErrorCode.放貨_LoadingOff異常,
            EnumLoadUnloadControlErrorCode.AlignmentNG,
            EnumLoadUnloadControlErrorCode.HomeSensor未On,
            EnumLoadUnloadControlErrorCode.Z軸上定位未On,
            EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail,
            EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange,
            EnumLoadUnloadControlErrorCode.AlignmentValueNG,
            EnumLoadUnloadControlErrorCode.取放中極限觸發,
            EnumLoadUnloadControlErrorCode.取放中軸異常,
            EnumLoadUnloadControlErrorCode.取放中安全迴路異常,
            EnumLoadUnloadControlErrorCode.RollerStopTimeout,
            EnumLoadUnloadControlErrorCode.ServoOnTimeout,
            EnumLoadUnloadControlErrorCode.Port站ES或HO_AVBLNotOn,
            EnumLoadUnloadControlErrorCode.取放貨站點資訊異常,
            EnumLoadUnloadControlErrorCode.取放貨異常_Z軸升降中CVSensor異常,

            EnumLoadUnloadControlErrorCode.回Home失敗_CVSensor狀態異常,
            EnumLoadUnloadControlErrorCode.回Home失敗_CST回CV_Timeout,
            EnumLoadUnloadControlErrorCode.回Home失敗_P軸不在Home,
            EnumLoadUnloadControlErrorCode.回Home失敗_Theta軸不再Home,
            EnumLoadUnloadControlErrorCode.回Home失敗_Z軸不在上定位,
            EnumLoadUnloadControlErrorCode.回Home失敗_ServoOn_Timeout,
            EnumLoadUnloadControlErrorCode.回Home失敗_Exception,
            EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗,
            EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止,
            EnumLoadUnloadControlErrorCode.回Home失敗_極限Sensor未On,
            EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout,
            EnumLoadUnloadControlErrorCode.回Home失敗_HomeSensor未on,
            EnumLoadUnloadControlErrorCode.回Home失敗_HomeSensor未off,
            //Allen ATS robot相關異常
            EnumLoadUnloadControlErrorCode.取放貨手臂異常,
            EnumLoadUnloadControlErrorCode.Robot定點式Mark定位失敗,
            EnumLoadUnloadControlErrorCode.Robot伺服式Mark定位失敗,
            EnumLoadUnloadControlErrorCode.Robot補償值穩定度不足,
            EnumLoadUnloadControlErrorCode.Robot姿態異常,
            EnumLoadUnloadControlErrorCode.AGV與Mark相對位置超過範圍

        };


        protected override void AlarmCodeClear()
        {
            if (localData.AutoManual == EnumAutoState.Manual)
            {
                for (int i = 0; i < alarmCodeClearList.Count; i++)
                    ResetAlarmCode(alarmCodeClearList[i]);
            }
            else
            {
                for (int i = 0; i < alarmCodeClearList.Count; i++)
                {
                    if (alarmHandler.AlarmCodeTable.ContainsKey((int)alarmCodeClearList[i]) &&
                        alarmHandler.AlarmCodeTable[(int)alarmCodeClearList[i]].Level == EnumAlarmLevel.Alarm)
                        ;
                    else
                        ResetAlarmCode(alarmCodeClearList[i]);
                }
            }
        }
        public override void ResetAlarm()
        {
            AlarmCodeClear();

            if (localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null &&
                !reconnectedAndResetAxisError)
            {
                ResetAlarm_ReConnectAndResetAxisError();
            }
        }
        private bool reconnectedAndResetAxisError = false;

        private void ResetAlarm_ReConnectAndResetAxisError()
        {
            reconnectedAndResetAxisError = true;
            ConnectAlignmentDevice();

            /// 有動力電 需要手臂復規時. 下命令的地方.
            /// 
            reconnectedAndResetAxisError = false;
        }


        #region 補正元件連線.
        private void ConnectAlignmentDevice()
        {
            if (localData.SimulateMode)
                return;

            string errorMessage = "";
            //ATS取消SR1000 Barcode補正
            /*
            if (sr1000_R == null)
            {
                sr1000_R = new BarcodeReader_Keyence();

                if (!sr1000_R.Connect("192.168.29.216", ref errorMessage))
                {
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.取放貨補正元件_BarcoderReader連線失敗);
                    WriteLog(5, "", "AlignmentDevice : SR1000 連線失敗");
                }
                else
                    WriteLog(7, "", String.Concat("連線成功!"));
            }
            else
            {
                if (!sr1000_R.Connected)
                {
                    if (!sr1000_R.Connect("192.168.29.216", ref errorMessage))
                    {
                        SendAlarmCode(EnumLoadUnloadControlErrorCode.取放貨補正元件_BarcoderReader連線失敗);
                        WriteLog(5, "", "AlignmentDevice : SR1000 連線失敗");
                    }
                    else
                        WriteLog(7, "", String.Concat("連線成功!"));
                }
                else if (sr1000_R.Error)
                {
                    sr1000_R.ResetError();

                    if (sr1000_R.Error)
                        SendAlarmCode(EnumLoadUnloadControlErrorCode.取放貨補正元件_BarcoderReader斷線);
                }
            }
            */
            // ATS OMRON cstIDReader 未接
            if (cstIDReaderL == null)
            {
                cstIDReaderL = new RFIDReader();

                if (!cstIDReaderL.ConnectSocket("192.168.29.240:7090", ref errorMessage))
                {
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.CSTIDReaderLeft_連線失敗);
                    WriteLog(5, "", "OMRON RFID Reader Left 連線失敗");
                }
                else
                    WriteLog(7, "", String.Concat("OMRON RFID Reader Left連線成功!"));

            }
            else
            {
                if (!cstIDReaderL.Connected)
                {
                    if (!cstIDReaderL.ConnectSocket("192.168.29.240:7090", ref errorMessage))
                    {
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.CSTIDReaderLeft_連線失敗);
                        WriteLog(5, "", "OMRON RFID Reader 連線失敗");
                    }
                    else
                        WriteLog(7, "", String.Concat("OMRON RFID Reader連線成功!"));

                }
            }

            if (cstIDReaderR == null)
            {
                cstIDReaderR = new RFIDReader();

                if (!cstIDReaderR.ConnectSocket("192.168.29.241:7090", ref errorMessage))
                {
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.CSTIDReaderRight_連線失敗);
                    WriteLog(5, "", "OMRON RFID Reader Right 連線失敗");

                }
                else
                    WriteLog(7, "", String.Concat("OMRON RFID Reader Right連線成功!"));
            }
            else
            {
                if (!cstIDReaderR.Connected)
                {
                    if (!cstIDReaderR.ConnectSocket("192.168.29.241:7090", ref errorMessage))
                    {
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.CSTIDReaderRight_連線失敗);
                        WriteLog(5, "", "OMRON RFID Reader Right 連線失敗");
                    }
                    else
                        WriteLog(7, "", String.Concat("OMRON RFID Reader Right連線成功!"));

                }
            }
        }
        #endregion


        private EnumSafetyLevel GetSafetySensorStatus()
        {
            if (localData.MIPCData.SafetySensorStatus == EnumSafetyLevel.EMO ||
                localData.MIPCData.SafetySensorStatus == EnumSafetyLevel.IPCEMO)
                return EnumSafetyLevel.EMO;
            else if (localData.MIPCData.SafetySensorStatus == EnumSafetyLevel.EMS)
                return EnumSafetyLevel.EMO;
            else if (localData.MIPCData.SafetySensorStatus == EnumSafetyLevel.SlowStop || localData.LoadUnloadData.LoadUnloadCommand.Pause)
                return EnumSafetyLevel.Normal;
            else
                return EnumSafetyLevel.Normal;
        }

        private EnumLoadUnloadControlErrorCode GetPIOAlarmCode()
        {
            EnumLoadUnloadControlErrorCode alarmCode = EnumLoadUnloadControlErrorCode.None;

            switch (RightPIO.Timeout)
            {
                case EnumPIOStatus.TA1:
                    alarmCode = EnumLoadUnloadControlErrorCode.TA1_Timeout;
                    break;
                case EnumPIOStatus.TA2:
                    alarmCode = EnumLoadUnloadControlErrorCode.TA2_Timeout;
                    break;
                case EnumPIOStatus.TA3:
                    alarmCode = EnumLoadUnloadControlErrorCode.TA3_Timeout;
                    break;
                case EnumPIOStatus.TP1:
                    alarmCode = EnumLoadUnloadControlErrorCode.TP1_Timeout;
                    break;
                case EnumPIOStatus.TP2:
                    alarmCode = EnumLoadUnloadControlErrorCode.TP2_Timeout;
                    break;
                case EnumPIOStatus.TP3:
                    alarmCode = EnumLoadUnloadControlErrorCode.TP3_Timeout;
                    break;
                case EnumPIOStatus.TP4:
                    alarmCode = EnumLoadUnloadControlErrorCode.TP4_Timeout;
                    break;
                case EnumPIOStatus.TP5:
                    alarmCode = EnumLoadUnloadControlErrorCode.TP5_Timeout;
                    break;
                default:
                    break;
            }
            //Allen
            switch (LeftPIO.Timeout)
            {
                case EnumPIOStatus.TA1:
                    alarmCode = EnumLoadUnloadControlErrorCode.TA1_Timeout;
                    break;
                case EnumPIOStatus.TA2:
                    alarmCode = EnumLoadUnloadControlErrorCode.TA2_Timeout;
                    break;
                case EnumPIOStatus.TA3:
                    alarmCode = EnumLoadUnloadControlErrorCode.TA3_Timeout;
                    break;
                case EnumPIOStatus.TP1:
                    alarmCode = EnumLoadUnloadControlErrorCode.TP1_Timeout;
                    break;
                case EnumPIOStatus.TP2:
                    alarmCode = EnumLoadUnloadControlErrorCode.TP2_Timeout;
                    break;
                case EnumPIOStatus.TP3:
                    alarmCode = EnumLoadUnloadControlErrorCode.TP3_Timeout;
                    break;
                case EnumPIOStatus.TP4:
                    alarmCode = EnumLoadUnloadControlErrorCode.TP4_Timeout;
                    break;
                case EnumPIOStatus.TP5:
                    alarmCode = EnumLoadUnloadControlErrorCode.TP5_Timeout;
                    break;
                default:
                    break;
            }
            //Allen
            if (LeftPIO.Timeout != EnumPIOStatus.None)
            {
                localData.LoadUnloadData.LoadUnloadCommand.PIOResult = LeftPIO.Timeout;
            }
            if (RightPIO.Timeout != EnumPIOStatus.None)
            {
                localData.LoadUnloadData.LoadUnloadCommand.PIOResult = RightPIO.Timeout;
            }

            //localData.LoadUnloadData.LoadUnloadCommand.PIOResult = RightPIO.Timeout;
            return alarmCode;
        }

        private void SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode errorCode, EnumLoadUnloadErrorLevel level)
        {
            SetAlarmCode(errorCode);

            LoadUnloadCommandData command = localData.LoadUnloadData.LoadUnloadCommand;

            if (command.ErrorLevel == level)
                return;

            switch (command.ErrorLevel)
            {
                case EnumLoadUnloadErrorLevel.None:
                    command.ErrorLevel = level;

                    break;
                case EnumLoadUnloadErrorLevel.PrePIOError:
                    if (level == EnumLoadUnloadErrorLevel.Error)
                        command.ErrorLevel = level;

                    break;
                case EnumLoadUnloadErrorLevel.AfterPIOError:
                    if (level == EnumLoadUnloadErrorLevel.Error)
                        command.ErrorLevel = EnumLoadUnloadErrorLevel.AfterPIOErrorAndActionError;

                    break;
                case EnumLoadUnloadErrorLevel.AfterPIOErrorAndActionError:
                    break;
                case EnumLoadUnloadErrorLevel.Error:
                    break;
            }
        }
        public override void LoadUnloadStart()
        {
            double waitRollerStopTimeout = 5000;
            Stopwatch timer = new Stopwatch();
            LoadUnloadCommandData command = localData.LoadUnloadData.LoadUnloadCommand;
            LoadUnloadControlData control = localData.LoadUnloadData;


            EnumSafetyLevel lastStatus = EnumSafetyLevel.Normal;
            EnumSafetyLevel nowStatus = EnumSafetyLevel.Normal;

            command.StatusStep = (int)EnumATSLoadUnloadStatus.Step1_檢查Loading;
            command.StepString = ((EnumATSLoadUnloadStatus)command.StatusStep).ToString();

            WriteLog(7, "", String.Concat("Step Change to : ", ((EnumATSLoadUnloadStatus)command.StatusStep).ToString()));

            Stopwatch rollerErrorFlowTimer = new Stopwatch();

            while (command.StatusStep != (int)EnumATSLoadUnloadStatus.Step0_Idle)
            {
                UpdateForkHomeStatus();
                nowStatus = GetSafetySensorStatus();

                switch (localData.LoadUnloadData.LoadUnloadCommand.StatusStep)
                {
                    case (int)EnumATSLoadUnloadStatus.Step1_檢查Loading:
                        #region Step1_檢查Loading，CST在席.
                        UpdateForkHomeStatus();

                        switch (command.Action)
                        {
                            case EnumLoadUnload.Load:
                                if (localData.AutoManual == EnumAutoState.Manual)
                                {
                                    //if (localData.LoadUnloadData.Loading)
                                    //{
                                    //    command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Fail;
                                    //    SetAlarmCodeAndSetCommandErrorCode(
                                    //        EnumLoadUnloadControlErrorCode.取貨_LoadingOn異常,
                                    //        EnumLoadUnloadErrorLevel.PrePIOError);

                                    //}
                                    //else
                                    //    command.StatusStep = (int)EnumATSLoadUnloadStatus.Step4_PIO_PIOStart;

                                    if (command.CstLocate == EnumCstInAGVLocate.Left)
                                    {
                                        if (localData.LoadUnloadData.Loading_Left)
                                        {
                                            command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Fail;
                                            SetAlarmCodeAndSetCommandErrorCode(
                                                EnumLoadUnloadControlErrorCode.取貨_LoadingOn異常,
                                                EnumLoadUnloadErrorLevel.PrePIOError);
                                        }
                                        else
                                        {
                                            command.StatusStep = (int)EnumATSLoadUnloadStatus.Step4_PIO_PIOStart;
                                        }
                                    }
                                    else if (command.CstLocate == EnumCstInAGVLocate.Right)
                                    {
                                        if (localData.LoadUnloadData.Loading_Right)
                                        {
                                            command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Fail;
                                            SetAlarmCodeAndSetCommandErrorCode(
                                                EnumLoadUnloadControlErrorCode.取貨_LoadingOn異常,
                                                EnumLoadUnloadErrorLevel.PrePIOError);
                                        }
                                        else
                                        {
                                            command.StatusStep = (int)EnumATSLoadUnloadStatus.Step4_PIO_PIOStart;
                                        }
                                    }
                                }
                                else
                                {
                                    //if (localData.LoadUnloadData.Loading /*|| localData.LoadUnloadData.Loading_LogicFlag*/)
                                    //{
                                    //    command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Fail;
                                    //    SetAlarmCodeAndSetCommandErrorCode(
                                    //        EnumLoadUnloadControlErrorCode.取貨_LoadingOn異常,
                                    //        EnumLoadUnloadErrorLevel.PrePIOError);
                                    //}
                                    //else
                                    //    command.StatusStep = (int)EnumATSLoadUnloadStatus.Step4_PIO_PIOStart;

                                    if (command.CstLocate == EnumCstInAGVLocate.Left)
                                    {
                                        if (localData.LoadUnloadData.Loading_Left)
                                        {
                                            command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Fail;
                                            SetAlarmCodeAndSetCommandErrorCode(
                                                EnumLoadUnloadControlErrorCode.取貨_LoadingOn異常,
                                                EnumLoadUnloadErrorLevel.PrePIOError);
                                        }
                                        else
                                        {
                                            command.StatusStep = (int)EnumATSLoadUnloadStatus.Step4_PIO_PIOStart;
                                        }
                                    }
                                    else if (command.CstLocate == EnumCstInAGVLocate.Right)
                                    {
                                        if (localData.LoadUnloadData.Loading_Right)
                                        {
                                            command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Fail;
                                            SetAlarmCodeAndSetCommandErrorCode(
                                                EnumLoadUnloadControlErrorCode.取貨_LoadingOn異常,
                                                EnumLoadUnloadErrorLevel.PrePIOError);
                                        }
                                        else
                                        {
                                            command.StatusStep = (int)EnumATSLoadUnloadStatus.Step4_PIO_PIOStart;
                                        }
                                    }
                                }
                                break;
                            case EnumLoadUnload.Unload:
                                if (localData.AutoManual == EnumAutoState.Manual)
                                {
                                    //if (!localData.LoadUnloadData.Loading)
                                    //{
                                    //    command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Fail;
                                    //    SetAlarmCodeAndSetCommandErrorCode(
                                    //        EnumLoadUnloadControlErrorCode.放貨_LoadingOff異常,
                                    //        EnumLoadUnloadErrorLevel.PrePIOError);
                                    //}
                                    //else
                                    //    command.StatusStep = (int)EnumATSLoadUnloadStatus.Step4_PIO_PIOStart;
                                    ////改到robot準備取放貨的初始位置


                                    if (command.CstLocate == EnumCstInAGVLocate.Left)
                                    {
                                        if (!localData.LoadUnloadData.Loading_Left)
                                        {
                                            SetAlarmCodeAndSetCommandErrorCode(
                                                EnumLoadUnloadControlErrorCode.放貨_LoadingOff異常,
                                                EnumLoadUnloadErrorLevel.PrePIOError);
                                        }
                                        else
                                        {
                                            command.StatusStep = (int)EnumATSLoadUnloadStatus.Step4_PIO_PIOStart;
                                        }
                                    }
                                    else if (command.CstLocate == EnumCstInAGVLocate.Right)
                                    {
                                        if (!localData.LoadUnloadData.Loading_Right)
                                        {
                                            SetAlarmCodeAndSetCommandErrorCode(
                                                EnumLoadUnloadControlErrorCode.放貨_LoadingOff異常,
                                                EnumLoadUnloadErrorLevel.PrePIOError);
                                        }
                                        else
                                        {
                                            command.StatusStep = (int)EnumATSLoadUnloadStatus.Step4_PIO_PIOStart;
                                        }
                                    }
                                }
                                else
                                {
                                    //if (!localData.LoadUnloadData.Loading /*|| !localData.LoadUnloadData.Loading_LogicFlag*/)
                                    //{
                                    //    command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Fail;
                                    //    SetAlarmCodeAndSetCommandErrorCode(
                                    //        EnumLoadUnloadControlErrorCode.放貨_LoadingOff異常,
                                    //        EnumLoadUnloadErrorLevel.PrePIOError);

                                    //}
                                    //else
                                    //    command.StatusStep = (int)EnumATSLoadUnloadStatus.Step4_PIO_PIOStart;
                                    ////改到robot準備取放貨的初始位置
                                    ///
                                    if (command.CstLocate == EnumCstInAGVLocate.Left)
                                    {
                                        if (!localData.LoadUnloadData.Loading_Left)
                                        {
                                            SetAlarmCodeAndSetCommandErrorCode(
                                                EnumLoadUnloadControlErrorCode.放貨_LoadingOff異常,
                                                EnumLoadUnloadErrorLevel.PrePIOError);
                                        }
                                        else
                                        {
                                            command.StatusStep = (int)EnumATSLoadUnloadStatus.Step4_PIO_PIOStart;
                                        }
                                    }
                                    else if (command.CstLocate == EnumCstInAGVLocate.Right)
                                    {
                                        if (!localData.LoadUnloadData.Loading_Right)
                                        {
                                            SetAlarmCodeAndSetCommandErrorCode(
                                                EnumLoadUnloadControlErrorCode.放貨_LoadingOff異常,
                                                EnumLoadUnloadErrorLevel.PrePIOError);
                                        }
                                        else
                                        {
                                            command.StatusStep = (int)EnumATSLoadUnloadStatus.Step4_PIO_PIOStart;
                                        }
                                    }
                                }

                                break;
                                //case EnumLoadUnload.ReadCSTID:
                                //    if (!localData.LoadUnloadData.Loading)
                                //    {
                                //        command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Fail;
                                //        command.ErrorCode = EnumLoadUnloadControlErrorCode.放貨_LoadingOff異常;
                                //    }
                                //    else
                                //        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step3_ResetPIOAndCheckESAndHO;
                                //    break;
                        }

                        if (command.NeedPIO)
                        {
                            if (command.PIODirection == EnumStageDirection.Left)
                            {
                                LeftPIO.ResetPIO();
                            }
                            else if (command.PIODirection == EnumStageDirection.Right)
                            {
                                RightPIO.ResetPIO();
                            }
                        }

                        #endregion
                        break;

                    case (int)EnumATSLoadUnloadStatus.Step2_Robot_Move_Initial:
                        if (localData.AutoManual == EnumAutoState.Auto)
                        {
                            RobotStartRBInitPosition();
                            command.StatusStep = (int)EnumATSLoadUnloadStatus.Step3_Wait_Robot_Initial;
                        }
                        else
                        {

                        }
                        break;

                    case (int)EnumATSLoadUnloadStatus.Step3_Wait_Robot_Initial:
                        //讀取finished=12，表示robot到位
                        if (localData.MIPCData.GetDataByMIPCTagName(robotCommandFinish) == 12)
                        //確定現在位置和Initial設定位置相符
                        {
                            command.StatusStep = (int)EnumATSLoadUnloadStatus.Step4_PIO_PIOStart;
                        }

                        break;

                    case (int)EnumATSLoadUnloadStatus.Step4_PIO_PIOStart:
                        #region Step4_PIO_PIOStart
                        //要區分左右PIO
                        if (command.NeedPIO)
                        {
                            if (command.PIODirection == EnumStageDirection.Left)
                            {
                                LeftPIO.PIOFlow_Load_UnLoad(command.Action);
                            }
                            else if (command.PIODirection == EnumStageDirection.Right)
                            {
                                RightPIO.PIOFlow_Load_UnLoad(command.Action);
                            }

                        }
                        command.StatusStep = (int)EnumATSLoadUnloadStatus.Step5_PIO_WaitReady;

                        #endregion
                        break;

                    case (int)EnumATSLoadUnloadStatus.Step5_PIO_WaitReady:
                        #region Step5_PIO_WaitReady
                        //要區分左右PIO
                        if (command.NeedPIO)
                        {
                            if (command.PIODirection == EnumStageDirection.Left)
                            {
                                if (LeftPIO.Status == EnumPIOStatus.TP2)
                                {
                                    command.StatusStep = (int)EnumATSLoadUnloadStatus.Step6_Robot_Move_LDULD;
                                    if (command.BreakenStopMode)
                                        command.WaitFlag = true;
                                }
                                else
                                {
                                    if (LeftPIO.Timeout != EnumPIOStatus.None)
                                    {
                                        //command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step20_Busy_P軸Theta軸回Home;
                                        command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Fail;
                                        SetAlarmCodeAndSetCommandErrorCode(
                                            GetPIOAlarmCode(),
                                            EnumLoadUnloadErrorLevel.PrePIOError);

                                        if (command.BreakenStopMode)
                                            command.GoNext = false;
                                    }
                                }

                            }
                            else if (command.PIODirection == EnumStageDirection.Right)
                            {
                                if (RightPIO.Status == EnumPIOStatus.TP2)
                                {
                                    command.StatusStep = (int)EnumATSLoadUnloadStatus.Step6_Robot_Move_LDULD;
                                    if (command.BreakenStopMode)
                                        command.WaitFlag = true;
                                }
                                else
                                {
                                    if (RightPIO.Timeout != EnumPIOStatus.None)
                                    {
                                        //command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step20_Busy_P軸Theta軸回Home;
                                        command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Fail;
                                        SetAlarmCodeAndSetCommandErrorCode(
                                            GetPIOAlarmCode(),
                                            EnumLoadUnloadErrorLevel.PrePIOError);

                                        if (command.BreakenStopMode)
                                            command.GoNext = false;
                                    }
                                }

                            }

                        }
                        else
                        {
                            command.StatusStep = (int)EnumATSLoadUnloadStatus.Step6_Robot_Move_LDULD;
                            if (command.BreakenStopMode)
                                command.GoNext = false;
                        }
                        #endregion
                        break;

                    case (int)EnumATSLoadUnloadStatus.Step6_Robot_Move_LDULD:

                        //if (localData.MIPCData.GetDataByMIPCTagName(robotStatus) == (float)EnumATSRobotStatus.Robot_ready)
                        if (localData.SimulateMode)
                            {
                            if (command.Action == EnumLoadUnload.Load)
                            {
                                //區分左右CST
                                if (command.CstLocate == EnumCstInAGVLocate.Left)
                                {
                                    control.NowCommand = control.RobotCommand[command.AddressID].Port1Load;
                                }
                                else if (command.CstLocate == EnumCstInAGVLocate.Right)
                                {
                                    control.NowCommand = control.RobotCommand[command.AddressID].Port2Load;
                                }
                                else
                                {
                                    control.NowCommand = control.RobotCommand[command.AddressID].Port1Load; //雙CST測試OK後移除
                                }

                            }
                            else if (command.Action == EnumLoadUnload.Unload)
                            {
                                //區分左右CST
                                if (command.CstLocate == EnumCstInAGVLocate.Left)
                                {
                                    control.NowCommand = control.RobotCommand[command.AddressID].Port1Unload;
                                }
                                else if (command.CstLocate == EnumCstInAGVLocate.Right)
                                {
                                    control.NowCommand = control.RobotCommand[command.AddressID].Port2Unload;

                                }
                                else
                                {
                                    control.NowCommand = control.RobotCommand[command.AddressID].Port1Unload; //雙CST測試OK後移除
                                }
                                //control.NowCommand = control.RobotCommand[command.AddressID].Port1Unload;
                            }
                            RobotStartLDULDPosition();
                            if(localData.SimulateMode)
                                command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_ReadCSTID;
                            else
                                command.StatusStep = (int)EnumATSLoadUnloadStatus.Step7_Wait_Robot_LDULD;
                            
                        }
                        else
                        {
                            //robot not ready 
                        }
                        break;
                    case (int)EnumATSLoadUnloadStatus.Step7_Wait_Robot_LDULD:
                        //要區分左右PIO
                        if (command.NeedPIO)
                            {
                            if (command.PIODirection == EnumStageDirection.Left)
                            {
                                ((PIOFlow_UMTC_LoadUnloadSemi)LeftPIO).SendBusy = true;
                            }
                            else if (command.PIODirection == EnumStageDirection.Right)
                            {
                                ((PIOFlow_UMTC_LoadUnloadSemi)RightPIO).SendBusy = true;
                            }
                            else
                            {
                                ((PIOFlow_UMTC_LoadUnloadSemi)RightPIO).SendBusy = true; // 雙CST測試OK後移除
                            }

                        }

                        
                        //讀取finished=取放命令，表示robot到位
                        //確定現在位置和取放位置相符

                        if (localData.MIPCData.GetDataByMIPCTagName(robotCommandFinish) == control.NowCommand)
                        {
                            command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_PIO_PIOContinue;
                        }
                        break;

                    case (int)EnumATSLoadUnloadStatus.Step_PIO_PIOContinue:
                        #region Step15_PIO_PIOContinue.
                        //要區分左右PIO
                        if (command.NeedPIO)
                        {
                            if (command.PIODirection == EnumStageDirection.Left)
                            {
                                if (RightPIO.Status == EnumPIOStatus.T6)
                                {
                                    ((PIOFlow_UMTC_LoadUnloadSemi)LeftPIO).SendComplete = true;
                                    command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_PIO_WaitEnd;
                                }
                                else if (RightPIO.Timeout != EnumPIOStatus.None)
                                {
                                    command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Fail;
                                    SetAlarmCodeAndSetCommandErrorCode(
                                        GetPIOAlarmCode(),
                                        EnumLoadUnloadErrorLevel.Error);
                                }

                            }
                            else if (command.PIODirection == EnumStageDirection.Right)
                            {
                                if (RightPIO.Status == EnumPIOStatus.T6)
                                {
                                    ((PIOFlow_UMTC_LoadUnloadSemi)RightPIO).SendComplete = true;
                                    command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_PIO_WaitEnd;
                                }
                                else if (RightPIO.Timeout != EnumPIOStatus.None)
                                {
                                    command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Fail;
                                    SetAlarmCodeAndSetCommandErrorCode(
                                        GetPIOAlarmCode(),
                                        EnumLoadUnloadErrorLevel.Error);
                                }
                            }

                        }
                        else
                        {
                            Thread.Sleep(3000);
                            command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_PIO_WaitEnd;
                        }
                        #endregion
                        break;

                    case (int)EnumATSLoadUnloadStatus.Step_PIO_WaitEnd:
                        #region Step_PIO_WaitEnd.
                        //要區分左右PIO
                        if (command.NeedPIO)
                        {
                            if (command.PIODirection == EnumStageDirection.Left)
                            {
                                if (LeftPIO.Status == EnumPIOStatus.Complete)
                                {
                                    command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_ReadCSTID;
                                    if (command.BreakenStopMode)
                                        command.GoNext = false;
                                }
                                else if (LeftPIO.Status == EnumPIOStatus.NG)
                                {
                                    if (alarmHandler.AlarmCodeTable.ContainsKey((int)command.ErrorCode) &&
                                        alarmHandler.AlarmCodeTable[(int)command.ErrorCode].Level == EnumAlarmLevel.Warn)
                                    {
                                        SetAlarmCodeAndSetCommandErrorCode(
                                            GetPIOAlarmCode(),
                                            EnumLoadUnloadErrorLevel.AfterPIOError);
                                    }
                                    command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Fail;
                                }
                            }
                            else if (command.PIODirection == EnumStageDirection.Right)
                            {
                                if (RightPIO.Status == EnumPIOStatus.Complete)
                                {
                                    command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_ReadCSTID;
                                    if (command.BreakenStopMode)
                                        command.GoNext = false;
                                }
                                else if (RightPIO.Status == EnumPIOStatus.NG)
                                {
                                    if (alarmHandler.AlarmCodeTable.ContainsKey((int)command.ErrorCode) &&
                                        alarmHandler.AlarmCodeTable[(int)command.ErrorCode].Level == EnumAlarmLevel.Warn)
                                    {
                                        SetAlarmCodeAndSetCommandErrorCode(
                                            GetPIOAlarmCode(),
                                            EnumLoadUnloadErrorLevel.AfterPIOError);
                                    }
                                    command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Fail;
                                }

                            }
                        }
                        else
                        {
                            command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_ReadCSTID;
                            if (command.BreakenStopMode)
                                command.GoNext = false;
                        }
                        #endregion
                        break;

                    case (int)EnumATSLoadUnloadStatus.Step_ReadCSTID:
                        #region Step_ReadCSTID.
                        UpdateLoadingAndCSTID(); //改寫為RFID Reader   
                        switch (command.Action)
                        {
                            case EnumLoadUnload.Load:
                                switch (command.CstLocate)
                                {
                                    case EnumCstInAGVLocate.Left:
                                        localData.LoadUnloadData.CstID_Left = command.CommandCSTID;
                                        localData.LoadUnloadData.Loading_Left = true;//模擬強制將FOUP sesnor改寫有探測
                                        break;
                                    case EnumCstInAGVLocate.Right:
                                        localData.LoadUnloadData.CstID_Right = command.CommandCSTID;
                                        localData.LoadUnloadData.Loading_Right = true;//模擬強制將FOUP sesnor改寫有探測;
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case EnumLoadUnload.Unload:
                                switch (command.CstLocate)
                                {
                                    case EnumCstInAGVLocate.Left:
                                        localData.LoadUnloadData.CstID_Left = "";
                                        localData.LoadUnloadData.Loading_Left = false;//模擬強制將FOUP sesnor改寫無探測
                                        break;
                                    case EnumCstInAGVLocate.Right:
                                        localData.LoadUnloadData.CstID_Right = "";
                                        localData.LoadUnloadData.Loading_Right = false;//模擬強制將FOUP sesnor改寫無探測
                                        break;
                                }
                                break;
                            default:
                                break;
                        }
                        //前進到下個站點，robot到CARPos，若是同站有第二個命令，可取消視覺任務節省時間   
                        //command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_Robot_Move_CARPos;
                        command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Scuess;
                        #endregion
                        break;

                    case (int)EnumATSLoadUnloadStatus.Step_Robot_Move_CARPos:
                        #region
                        if (localData.MIPCData.GetDataByMIPCTagName(robotStatus) == (float)EnumATSRobotStatus.Robot_ready)
                        {
                            RobotStartMoveCarPostion();
                            command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_Wait_Robot_CARPos;
                        }
                        else
                        {

                        }
                        #endregion
                        break;

                    case (int)EnumATSLoadUnloadStatus.Step_Wait_Robot_CARPos:
                        //讀取finished=Car_POS命令，表示robot到位
                        //確定現在位置和Car_POS位置相符
                        command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Scuess;
                        break;

                    case (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Scuess:
                        #region Step_LoadUnload_Scuess.
                        command.CommandEndTime = DateTime.Now;

                        switch (command.Action)
                        {
                            case EnumLoadUnload.Load:
                                SendLoadCompleteEventToMiddler();
                                break;
                            case EnumLoadUnload.Unload:
                                SendUnloadCompleteEventToMiddler();
                                break;
                        }

                        if (command.CommandResult != EnumLoadUnloadComplete.Error)
                            command.CommandResult = EnumLoadUnloadComplete.End;
                        //要區分左右PIO
                        if (command.NeedPIO)
                        {
                            if (command.PIODirection == EnumStageDirection.Left)
                            {
                                command.PIOResult = LeftPIO.Timeout;
                            }
                            else if (command.PIODirection == EnumStageDirection.Right)
                            {
                                command.PIOResult = RightPIO.Timeout;
                            }
                        }
                        //command.PIOResult = RightPIO.Timeout;

                        if (localData.MIPCData.GetDataByMIPCTagName(robotStatus) == (float)EnumATSRobotStatus.Robot_ready)
                        {
                            RobotSendNoActonCommand();
                        }
                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step0_Idle;
                        #endregion
                        break;

                    case (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Fail:
                        #region Step_LoadUnload_Fail.
                        //JogStop(); 改成robot異常處理

                        command.CommandEndTime = DateTime.Now;
                        command.CommandResult = EnumLoadUnloadComplete.Error;
                        //要區分左右PIO
                        if (command.NeedPIO)
                            command.PIOResult = RightPIO.Timeout;

                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step0_Idle;
                        #endregion
                        break;
                }

                lastStatus = nowStatus;

                #region StopRequest.
                if (localData.LoadUnloadData.LoadUnloadCommand.StopRequest || nowStatus == EnumSafetyLevel.EMO)
                {
                    if (command.StatusStep != (int)EnumATSLoadUnloadStatus.Step0_Idle &&
                        command.StatusStep != (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Scuess &&
                        command.StatusStep != (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Fail)
                    {
                        JogStop();

                        if (command.NeedPIO)
                            RightPIO.StopPIO();

                        command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Fail;
                        command.ErrorCode = EnumLoadUnloadControlErrorCode.取放貨中EMS;
                    }
                }
                else if (localData.MIPCData.AllDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.SafetyRelay.ToString()) &&
                     localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.SafetyRelay.ToString()) == 0)
                {
                    JogStop();

                    if (command.NeedPIO)
                        RightPIO.StopPIO();

                    command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Fail;
                    command.ErrorCode = EnumLoadUnloadControlErrorCode.取放中安全迴路異常;
                }
                //else if (AllAxisSensorData[EnumLoadUnloadAxisName.P軸.ToString()][axisPosLimitSensor[EnumLoadUnloadAxisName.P軸.ToString()]].Data ||
                //         AllAxisSensorData[EnumLoadUnloadAxisName.Theta軸.ToString()][axisNagLimitSensor[EnumLoadUnloadAxisName.Theta軸.ToString()]].Data ||
                //         AllAxisSensorData[EnumLoadUnloadAxisName.Z軸.ToString()][axisPosLimitSensor[EnumLoadUnloadAxisName.Z軸.ToString()]].Data)
                //{
                //    JogStop();
                //    command.StatusStep = (int)EnumATSLoadUnloadStatus.Step_LoadUnload_Fail;
                //    command.ErrorCode = EnumLoadUnloadControlErrorCode.取放中極限觸發;
                //}
                #endregion

                if (command.StepString != ((EnumATSLoadUnloadStatus)command.StatusStep).ToString())
                {
                    if (logMode)
                        WriteLog(7, "", String.Concat("Step Change to : ", ((EnumATSLoadUnloadStatus)command.StatusStep).ToString()));

                    command.StepString = ((EnumATSLoadUnloadStatus)command.StatusStep).ToString();
                }
                else
                    Thread.Sleep(sleepTime);
            }


        }


        #region ATS Robot Command Test
        private bool RobotStartRBInitPosition()
        {
            return mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { robotRBInitCommandTag }, new List<float> { 12 });
        }

        private bool RobotStartMoveCarPostion()
        {
            return mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { robotCarMoveCommandTag }, new List<float> { 13 });
        }

        private bool RobotStartRBGripperInitPosition()
        {
            return mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { robotRBGripperInitCommandTag }, new List<float> { 1 });
        }

        private bool RobotSendNoActonCommand()
        {
            return mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { robotLDULDCommandTag }, new List<float> { 2 });
        }

        private bool RobotStartLDULDPosition()
        {
            //未完成雙CST，先以Port1測試
            LoadUnloadControlData temp = localData.LoadUnloadData;

            return mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { robotLDULDCommandTag }, new List<float> { /*11020*/temp.NowCommand });
        }



        #endregion


        public override bool ClearCommand()
        {
            RightPIO.ResetPIO();
            LeftPIO.ResetPIO();

            LoadUnloadCommandData temp = localData.LoadUnloadData.LoadUnloadCommand;

            if (temp == null)
                return true;
            else
            {
                if (temp.StatusStep == (int)EnumUMTCLoadUnloadStatus.Step0_Idle)
                {
                    localData.LoadUnloadData.LoadUnloadCommand = null;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        #region AlignmentValue.
        private int lastRightStageNumber = -1;
        private string sr1000_R_Mode = "";

        private void GetBarcodeXAndY(EnumStageDirection direction, int stageNumber, ref string barcodeID,
                                     ref double barcodeX, ref double barcodeY, ref double originX, ref double originY)
        {
            string barcodeString = "";
            string errorMessage = "";

            if (sr1000_R.ReadBarcode(ref barcodeString, 200, ref errorMessage))
            {
                string[] splitResult = Regex.Split(barcodeString, "[: / ,]+", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));

                if (splitResult.Length == 4 && BarcodeReaderIDToRealDistance.ContainsKey(splitResult[0]))
                {
                    if (Int32.Parse(Regex.Replace(splitResult[3], "[^0-9]", "")) < 200)
                    {
                        barcodeID = splitResult[0];
                        originX = double.Parse(splitResult[1]);
                        originY = double.Parse(splitResult[2]);

                        barcodeY = (double.Parse(splitResult[2]) - alignmentDeviceOffset.BarcodeReader_Right.Y) / RightStageBarocdeReaderSetting[stageNumber].PixelToMM_Y;

                        barcodeX = (double.Parse(splitResult[1]) - alignmentDeviceOffset.BarcodeReader_Right.X) / RightStageBarocdeReaderSetting[stageNumber].PixelToMM_X +
                                  BarcodeReaderIDToRealDistance[barcodeID];
                    }
                    else
                        WriteLog(5, "", String.Concat("Scant Time > 100 不使用資料"));
                }
                else
                    WriteLog(5, "", String.Concat("SR1000 Split.Length != 4, data : ", barcodeString, ", Length : ", splitResult.Length.ToString("0")));
            }
            else
            {
                sr1000_R.StopReadBarcode(ref barcodeString, ref errorMessage);
                WriteLog(5, "", String.Concat("SR1000 Read Fail : ", errorMessage));
            }
        }

        private void GetLaserValue(EnumStageDirection direction, ref double laser_F, ref double laser_B)
        {
            string data = "";

            if (distanceSensor_R.GetDistanceSensorData(ref data))
            {
                //WriteLog(7, "", data);

                string[] splitResult = Regex.Split(data, ",", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));

                if (splitResult.Length == 5)
                {
                    if (splitResult[1] == "02" || splitResult[1] == "01" || splitResult[1] == "04")
                        laser_B = 300 - (double.Parse(splitResult[2]) / 100) - alignmentDeviceOffset.LaserB_Right;
                    //laser_F = 300 - (double.Parse(splitResult[2]) / 100);

                    if (splitResult[3] == "02" || splitResult[3] == "01" || splitResult[3] == "04")
                        laser_F = 300 - (double.Parse(splitResult[4]) / 100) - alignmentDeviceOffset.LaserF_Right; ;
                    //laser_B = 300 - (double.Parse(splitResult[4]) / 100);
                }
            }
        }

        private void GetAlignmentValue(EnumStageDirection direction, int stageNumber)
        {
            AlignmentValueData temp = new AlignmentValueData();
            double barcodeX = 0;
            double barcodeY = 0;
            string barcodeID = "";
            double laser_F = 0;
            double laser_B = 0;
            double originX = 0;
            double originY = 0;

            GetBarcodeXAndY(direction, stageNumber, ref barcodeID, ref barcodeX, ref barcodeY, ref originX, ref originY);
            GetLaserValue(direction, ref laser_F, ref laser_B);

            temp.LaserF = laser_F;
            temp.LaserB = laser_B;
            temp.BarcodePosition = new MapPosition(barcodeX, barcodeY);
            temp.OriginBarodePosition = new MapPosition(originX, originY);
            temp.BarcodeNumber = barcodeID;
            temp.Direction = direction;
            temp.StageNumber = stageNumber;

            if (laser_F == 0)
                WriteLog(5, "", String.Concat("DistanceSensor F read fail"));

            if (laser_B == 0)
                WriteLog(5, "", String.Concat("DistanceSensor B read fail"));

            if (barcodeID != "" && laser_F != 0 && laser_B != 0)
            {
                double theta = Math.Atan((laser_B - laser_F) /
                               Math.Abs(alignmentDeviceOffset.LaserB_Right_Locate - alignmentDeviceOffset.LaserF_Right_Locate)) * 180 / Math.PI;

                if (Math.Abs(theta) < 3)
                {
                    double p = barcodeX - Math.Sin(theta / 180 * Math.PI) * ((laser_F + laser_B) / 2 + alignmentDeviceOffset.BasedDistance) - RightStageDataList[stageNumber].Benchmark_P;
                    double y = Math.Cos(theta / 180 * Math.PI) * ((laser_F + laser_B) / 2 + alignmentDeviceOffset.BasedDistance) - alignmentDeviceOffset.BasedDistance - RightStageDataList[stageNumber].Benchmark_Y;
                    double z = barcodeY - RightStageDataList[stageNumber].Benchmark_Z;
                    theta -= RightStageDataList[stageNumber].Benchmark_Theta;

                    temp.P = p;
                    temp.Y = y;
                    temp.Theta = theta;
                    temp.Z = z;

                    if (AllAddressOffset.ContainsKey(checkAddressID))
                    {
                        temp.P -= AllAddressOffset[checkAddressID].P;
                        temp.Theta -= AllAddressOffset[checkAddressID].Theta;
                        temp.Y -= AllAddressOffset[checkAddressID].Y;
                        temp.Z = -AllAddressOffset[checkAddressID].Z;
                    }

                    temp.AlignmentVlaue = true;
                }
                else
                {
                    WriteLog(7, "", String.Concat("Theta過大 : ", theta.ToString()));
                }
            }

            localData.LoadUnloadData.AlignmentValue = temp;
        }

        private void CheckAlignmentValueOrigin(EnumStageDirection direction, int stageNumber)
        {
            try
            {
                string errorMessage = "";

                switch (direction)
                {
                    case EnumStageDirection.Left:
                        WriteLog(7, "", "UMTC沒有右補正計算功能");
                        localData.LoadUnloadData.AlignmentValue = null;

                        break;
                    case EnumStageDirection.Right:
                        if (lastRightStageNumber != stageNumber)
                        {
                            if (RightStageBarocdeReaderSetting.ContainsKey(stageNumber))
                            {
                                lastRightStageNumber = stageNumber;

                                if (RightStageBarocdeReaderSetting[stageNumber].BarcodeReaderMode != sr1000_R_Mode)
                                {
                                    sr1000_R_Mode = RightStageBarocdeReaderSetting[stageNumber].BarcodeReaderMode;

                                    if (sr1000_R != null)
                                    {
                                        if (!sr1000_R.ChangeMode(sr1000_R_Mode, ref errorMessage))
                                            WriteLog(3, "", String.Concat("Change Mode Fail ErrorMessage : ", errorMessage));
                                    }
                                }
                            }
                            else
                                WriteLog(3, "", String.Concat("Fatal Error, RightStageBarocdeReaderSetting 中沒有StatgeNumber = ", stageNumber.ToString()));
                        }

                        GetAlignmentValue(direction, stageNumber);

                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        public override void CheckAlingmentValue(EnumStageDirection direction, int stageNumber)
        {
            checkAddressID = "";
            CheckAlignmentValueOrigin(direction, stageNumber);
        }

        private void SwitchAlingmentValue(EnumStageDirection direction, int stageNumber)
        {
            try
            {
                string errorMessage = "";

                switch (direction)
                {
                    case EnumStageDirection.Left:
                        WriteLog(7, "", "UMTC沒有右補正計算功能");
                        localData.LoadUnloadData.AlignmentValue = null;

                        break;
                    case EnumStageDirection.Right:
                        if (lastRightStageNumber != stageNumber)
                        {
                            if (RightStageBarocdeReaderSetting.ContainsKey(stageNumber))
                            {
                                lastRightStageNumber = stageNumber;

                                if (RightStageBarocdeReaderSetting[stageNumber].BarcodeReaderMode != sr1000_R_Mode)
                                {
                                    sr1000_R_Mode = RightStageBarocdeReaderSetting[stageNumber].BarcodeReaderMode;

                                    if (sr1000_R != null)
                                    {
                                        if (!sr1000_R.ChangeMode(sr1000_R_Mode, ref errorMessage))
                                            WriteLog(3, "", String.Concat("Change Mode Fail ErrorMessage : ", errorMessage));
                                    }
                                }
                            }
                            else
                                WriteLog(3, "", String.Concat("Fatal Error, RightStageBarocdeReaderSetting 中沒有StatgeNumber = ", stageNumber.ToString()));
                        }

                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        public override void SwitchAlignmentValueByAddressID(string addressID)
        {
            if (localData.TheMapInfo.AllAddress.ContainsKey(addressID))
                SwitchAlingmentValue(localData.TheMapInfo.AllAddress[addressID].LoadUnloadDirection, localData.TheMapInfo.AllAddress[addressID].StageNumber);
        }

        public override void CheckAlingmentValueByAddressID(string addressID)
        {
            if (localData.TheMapInfo.AllAddress.ContainsKey(addressID))
            {
                checkAddressID = addressID;

                if (localData.TheMapInfo.AllAddress[addressID].LoadUnloadDirection != EnumStageDirection.None)
                    CheckAlignmentValueOrigin(localData.TheMapInfo.AllAddress[addressID].LoadUnloadDirection, localData.TheMapInfo.AllAddress[addressID].StageNumber);
                else if (localData.TheMapInfo.AllAddress[addressID].ChargingDirection != EnumStageDirection.None)
                    CheckAlignmentValueOrigin(localData.TheMapInfo.AllAddress[addressID].ChargingDirection, localData.TheMapInfo.AllAddress[addressID].StageNumber);
            }
        }
        #endregion

        #region GetMoveStatusByCVAxisName.
        private EnumAxisMoveStatus GetMoveStatusByCVAxisName(string axisNameString)
        {
            AxisFeedbackData temp = localData.LoadUnloadData.CVFeedbackData[axisNameString];

            if (temp == null)
                return EnumAxisMoveStatus.None;
            else
            {
                switch (axisPreStatus[axisNameString])
                {
                    case EnumAxisMoveStatus.PreMove:
                        if (temp.AxisMoveStaus == EnumAxisMoveStatus.Move || axisPreTimer[axisNameString].ElapsedMilliseconds > axisStatusDelayTime)
                        {
                            axisPreStatus[axisNameString] = EnumAxisMoveStatus.None;
                            return temp.AxisMoveStaus;
                        }
                        else
                            return axisPreStatus[axisNameString];

                    case EnumAxisMoveStatus.PreStop:
                        if (temp.AxisMoveStaus == EnumAxisMoveStatus.Stop || axisPreTimer[axisNameString].ElapsedMilliseconds > axisStatusDelayTime)
                        {
                            axisPreStatus[axisNameString] = EnumAxisMoveStatus.None;
                            return temp.AxisMoveStaus;
                        }
                        else
                            return axisPreStatus[axisNameString];

                    default:
                        axisPreStatus[axisNameString] = EnumAxisMoveStatus.None;
                        return temp.AxisMoveStaus;
                }
            }
        }
        #endregion

        private bool CVPtoP(string axisNameString, double encoder, double speedPercent, bool waitStop, double timeoutValue)
        {
            AxisFeedbackData temp = localData.LoadUnloadData.CVFeedbackData[axisNameString];
            AxisFeedbackData temp2 = (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString() ? localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()] : temp);

            if (temp == null || temp2 == null)
                return false;
            else if (temp.AxisServoOnOff == EnumAxisServoOnOff.ServoOff || temp2.AxisServoOnOff == EnumAxisServoOnOff.ServoOff)
                return false;

            if (mipcControl.SendMIPCDataByMIPCTagName(
                    new List<string>() {
                        axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.PlausUnit] ,
                        axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.Velocity] ,
                        axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.Acceleration],
                        axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.Deceleration],
                        axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.TargetPosition],
                        axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.PositionCommand]
                    },
                    new List<float>() {
                        (float)(axisDataList[axisNameString].PlausUnit),
                        (float)(axisDataList[axisNameString].AutoVelocity * speedPercent),
                        (float)(axisDataList[axisNameString].AutoAcceleration /** speedPercent*/),
                        (float)(axisDataList[axisNameString].AutoDeceleration /** speedPercent*/),
                        (float)(encoder + localData.LoadUnloadData.CVHomeOffsetValue[axisNameString] + localData.LoadUnloadData.CVEncoderOffsetValue[axisNameString]),
                        1
                    }))
            {
                axisPreTimer[axisNameString].Restart();
                axisPreStatus[axisNameString] = EnumAxisMoveStatus.PreMove;

                if (waitStop)
                {
                    Stopwatch timer = new Stopwatch();

                    while (GetMoveStatusByCVAxisName(axisNameString) != EnumAxisMoveStatus.Stop)
                    {
                        if (timer.ElapsedMilliseconds > timeoutValue)
                            return false;

                        Thread.Sleep(sleepTime);
                    }

                    return true;
                }
                else
                    return true;
            }
            else
                return false;
        }

        /// AxisJog
        ///     參數 : 軸名稱, 方向, 速度
        ///     return : boolean ( true : MIPC 指令傳送成功, false : MIPC 指令傳送失敗 or Not Servo On).
        #region AxisJog.
        private bool AxisJog(string axisNameString, bool direction, double speedPercent)
        {
            AxisFeedbackData temp = localData.LoadUnloadData.CVFeedbackData[axisNameString];
            AxisFeedbackData temp2 = (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString() ? localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()] : temp);

            if (temp == null || temp2 == null)
                return false;
            else if (temp.AxisServoOnOff == EnumAxisServoOnOff.ServoOff || temp2.AxisServoOnOff == EnumAxisServoOnOff.ServoOff)
                return false;

            if (mipcControl.SendMIPCDataByMIPCTagName(
                    new List<string>() {
                        axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.PlausUnit] ,
                        axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.Velocity] ,
                        axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.Acceleration],
                        axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.Deceleration],
                        axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.VelocityCommand]
                    },
                    new List<float>() {
                        (float)(axisDataList[axisNameString].PlausUnit),
                        (float)(axisDataList[axisNameString].AutoVelocity * ( direction ? 1 : -1 ) *  (float)(speedPercent)),
                        (float)(axisDataList[axisNameString].AutoAcceleration /** speedPercent*/),
                        (float)(axisDataList[axisNameString].AutoDeceleration /** speedPercent*/),
                        1
                    }))
            {
                axisPreTimer[axisNameString].Restart();
                axisPreStatus[axisNameString] = EnumAxisMoveStatus.PreMove;
                return true;
            }
            else
                return false;
        }
        #endregion

        /// ServoOn
        ///     參數 : 軸名稱, 是否等待ServoOn完成, timeout數值
        ///     return : boolean ( true : 如果不等待ServoOn完成則代表 MIPC 指令傳送成功, 等待ServoOn完成代表現在是ServoOn狀態).
        ///     如果是ServoOn Z軸, 則會一起ServoOn Z軸_Slave.
        #region ServoOn
        private bool ServoOn(string axisNameString, bool waitServoOn, double timeout)
        {
            if (localData.LoadUnloadData.CVFeedbackData == null ||
                !localData.LoadUnloadData.CVFeedbackData.ContainsKey(axisNameString) ||
                localData.LoadUnloadData.CVFeedbackData[axisNameString] == null)
                return false;

            List<string> tagNameList = new List<string>();
            List<float> valueList = new List<float>();

            if (logMode)
                WriteLog(7, "", String.Concat(axisNameString, "Servo on, waitServoOn : ", waitServoOn.ToString()));

            tagNameList.Add(axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.ServoOn]);
            valueList.Add((float)((int)EnumMIPCServoOnOffValue.ServoOn));

            if (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString())
            {
                tagNameList.Add(String.Concat(EnumLoadUnloadAxisName.Z軸_Slave.ToString(), "_", EnumLoadUnloadAxisCommandType.ServoOn));
                valueList.Add((float)((int)EnumMIPCServoOnOffValue.ServoOn));
            }

            if (!mipcControl.SendMIPCDataByMIPCTagName(tagNameList, valueList))
            {
                WriteLog(5, "", String.Concat("CV : ", axisNameString, " ServoOn Send MIPC Fail"));
                return false;
            }

            if (waitServoOn)
            {
                if (logMode)
                    WriteLog(7, "", "wait servo on");

                Stopwatch timer = new Stopwatch();
                timer.Restart();

                if (localData.LoadUnloadData.CVFeedbackData.ContainsKey(axisNameString))
                {
                    if (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString() &&
                        !localData.LoadUnloadData.CVFeedbackData.ContainsKey(EnumLoadUnloadAxisName.Z軸_Slave.ToString()))
                    {
                        WriteLog(5, "", String.Concat("CV : ", axisNameString, " FeedbackData not in CVFeedbackData"));
                        return false;
                    }

                    AxisFeedbackData temp = localData.LoadUnloadData.CVFeedbackData[axisNameString];
                    AxisFeedbackData temp2 = null;

                    if (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString())
                        temp2 = localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()];
                    else
                        temp2 = temp;

                    while (!(temp.AxisServoOnOff == EnumAxisServoOnOff.ServoOn && temp2.AxisServoOnOff == EnumAxisServoOnOff.ServoOn))
                    {
                        if (timer.ElapsedMilliseconds > timeout)
                        {
                            WriteLog(5, "", String.Concat("CV : ", axisNameString, " Wait ServoOn Timeout"));
                            return false;
                        }

                        Thread.Sleep(sleepTime);

                        temp = localData.LoadUnloadData.CVFeedbackData[axisNameString];

                        if (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString())
                            temp2 = localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()];
                        else
                            temp2 = temp;
                    }
                }
                else
                {
                    WriteLog(5, "", String.Concat("CV : ", axisNameString, " FeedbackData not in CVFeedbackData"));
                    return false;
                }
            }

            return true;
        }

        private bool ServoOff(string axisNameString, bool waitServoOn, double timeout)
        {
            List<string> tagNameList = new List<string>();
            List<float> valueList = new List<float>();


            if (logMode)
                WriteLog(7, "", String.Concat(axisNameString, "Servo off, waitServoOn : ", waitServoOn.ToString()));

            tagNameList.Add(axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.ServoOn]);
            valueList.Add((float)((int)EnumMIPCServoOnOffValue.ServoOn));

            if (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString())
            {
                tagNameList.Add(String.Concat(EnumLoadUnloadAxisName.Z軸_Slave.ToString(), "_", EnumLoadUnloadAxisCommandType.ServoOn));
                valueList.Add((float)((int)EnumMIPCServoOnOffValue.ServoOff));
            }

            if (!mipcControl.SendMIPCDataByMIPCTagName(tagNameList, valueList))
            {
                WriteLog(5, "", String.Concat("CV : ", axisNameString, " ServoOn Send MIPC Fail"));
                return false;
            }

            if (waitServoOn)
            {
                if (logMode)
                    WriteLog(7, "", "wait servo on");

                Stopwatch timer = new Stopwatch();
                timer.Restart();

                if (localData.LoadUnloadData.CVFeedbackData.ContainsKey(axisNameString))
                {
                    if (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString() &&
                        !localData.LoadUnloadData.CVFeedbackData.ContainsKey(EnumLoadUnloadAxisName.Z軸_Slave.ToString()))
                    {
                        WriteLog(5, "", String.Concat("CV : ", axisNameString, " FeedbackData not in CVFeedbackData"));
                        return false;
                    }

                    AxisFeedbackData temp = localData.LoadUnloadData.CVFeedbackData[axisNameString];
                    AxisFeedbackData temp2 = null;

                    if (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString())
                        temp2 = localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()];
                    else
                        temp2 = temp;

                    while (!(temp.AxisServoOnOff == EnumAxisServoOnOff.ServoOn && temp2.AxisServoOnOff == EnumAxisServoOnOff.ServoOn))
                    {
                        if (timer.ElapsedMilliseconds > timeout)
                        {
                            WriteLog(5, "", String.Concat("CV : ", axisNameString, " Wait ServoOn Timeout"));
                            return false;
                        }

                        Thread.Sleep(sleepTime);

                        temp = localData.LoadUnloadData.CVFeedbackData[axisNameString];

                        if (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString())
                            temp2 = localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()];
                        else
                            temp2 = temp;
                    }
                }
                else
                {
                    WriteLog(5, "", String.Concat("CV : ", axisNameString, " FeedbackData not in CVFeedbackData"));
                    return false;
                }
            }

            return true;
        }
        #endregion

        public override void Jog(int indexOfAxis, bool direction)
        {
            #region 測試robot命令
            switch (indexOfAxis)
            {
                case 0:
                    RobotStartRBInitPosition();
                    break;
                case 1:
                    RobotStartRBGripperInitPosition();
                    break;
                case 2:
                    RobotStartMoveCarPostion();
                    break;

            }

            #endregion
        }

        public override void Jog_相對(string axisName, double deltaEncoder)
        {
            try
            {
                if (!configAllOK)
                    return;

                if (!CheckAixsIsServoOn(axisName))
                    ServoOn(axisName, false, 0);
                else
                {
                    if (!CVPtoP(axisName, localData.LoadUnloadData.CVFeedbackData[axisName].Position + deltaEncoder, 0.1, false, 0))
                        WriteLog(5, "", String.Concat("CV Jog Fail"));
                }
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        public override double GetDeltaZ
        {
            get
            {
                if (localData.LoadUnloadData.CVFeedbackData != null &&
                    localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()] != null &&
                    localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()] != null)
                    return Math.Abs(localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].Position - localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()].Position);
                else
                    return 0;
            }
        }

        public double GetDeltaZValue
        {
            get
            {
                if (localData.LoadUnloadData.CVFeedbackData != null &&
                    localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()] != null &&
                    localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()] != null)
                    return localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].Position - localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()].Position;
                else
                    return 0;
            }
        }

        private bool Stop(EnumLoadUnloadAxisName axis)
        {
            if (!configAllOK)
                return false; ;

            if (mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { axisCommandString[axis.ToString()][EnumLoadUnloadAxisCommandType.StopCommand] }, new List<float>() { 1 }))
            {
                axisPreTimer[axis.ToString()].Restart();
                axisPreStatus[axis.ToString()] = EnumAxisMoveStatus.PreStop;
                return true;
            }
            else
            {
                WriteLog(5, "", String.Concat("Stop Fail"));
                return false;
            }
        }

        public override bool JogStop()
        {
            return true;
        }

        private bool CheckAxisError(EnumLoadUnloadAxisName axisName)
        {
            if (localData.LoadUnloadData != null && localData.LoadUnloadData.CVFeedbackData[axisName.ToString()] != null)
                return localData.LoadUnloadData.CVFeedbackData[axisName.ToString()].MF != 0;
            else
                return false;
        }

        bool 確認CVEncoder = false;

        private void CheckCVEncoderHome()
        {

        }

        public override void UpdateLoadingAndCSTID()
        {
            //if (localData.SimulateMode)
            //    return;

            #region ATS在席sensor L1, L2, R1, R2



            //FOUPL1_loading.Data = (localData.MIPCData.GetDataByMIPCTagName(foupL1_loading) != 0);
            //if (FOUPL1_loading.Change)
            //    WriteLog(7, "", String.Concat(foupL1_loading, " Change to ", (FOUPL1_loading.data ? "on" : "off")));

            //FOUPL2_loading.Data = (localData.MIPCData.GetDataByMIPCTagName(foupL2_loading) != 0);
            //if (FOUPL2_loading.Change)
            //    WriteLog(7, "", String.Concat(foupL2_loading, " Change to ", (FOUPL2_loading.data ? "on" : "off")));


            //FOUPR1_loading.Data = (localData.MIPCData.GetDataByMIPCTagName(foupR1_loading) != 0);
            //if (FOUPR1_loading.Change)
            //    WriteLog(7, "", String.Concat(foupR1_loading, " Change to ", (FOUPR1_loading.data ? "on" : "off")));

            // FOUPR2_loading.Data = (localData.MIPCData.GetDataByMIPCTagName(foupR2_loading) != 0);
            //if (FOUPR2_loading.Change)
            //    WriteLog(7, "", String.Concat(foupR2_loading, " Change to ", (FOUPR2_loading.data ? "on" : "off")));
           
            //if (FOUPL1_loading.Data/* && FOUPL2_loading.Data*/)
            //{
                //localData.LoadUnloadData.Loading = true;
            //    localData.LoadUnloadData.Loading_Left = true;
                //Loading狀態區分L和R?
            //}
            //else if (!FOUPL1_loading.Data/* && !FOUPL2_loading.Data*/)
            //{
                //localData.LoadUnloadData.Loading = false;
            //    localData.LoadUnloadData.Loading_Left = false;
            //}
            //localData.LoadUnloadData.Loading = localData.LoadUnloadData.Loading_Left;  //Allen, local loadunload use

            //if (FOUPR1_loading.Data)
            //{
            //    localData.LoadUnloadData.Loading_Right = true;
            //}
            //else if (!FOUPR1_loading.Data)
            //{
            //    localData.LoadUnloadData.Loading_Right = false;
            //}

            //else
            //{
            //    localData.LoadUnloadData.Loading = false;
            //}

            //FOUPR1_loading.Data = (localData.MIPCData.GetDataByMIPCTagName(foupR1_loading) == 0);
            //if (FOUPR1_loading.Change)
            //    WriteLog(7, "", String.Concat(foupR1_loading, " Change to ", (FOUPR1_loading.data ? "on" : "off")));

            //FOUPR2_loading.Data = (localData.MIPCData.GetDataByMIPCTagName(foupR2_loading) == 0);
            //if (FOUPR2_loading.Change)
            //    WriteLog(7, "", String.Concat(foupR2_loading, " Change to ", (FOUPR2_loading.data ? "on" : "off")));

            //if (FOUPR1_loading.Data && FOUPR2_loading.Data)
            //{
            //    localData.LoadUnloadData.Loading = true;
            //    //Loading狀態區分L和R?
            //}
            //else if (!FOUPR1_loading.Data && !FOUPR2_loading.Data)
            //{
            //    localData.LoadUnloadData.Loading = false;
            //}
            //else
            //{
            //    localData.LoadUnloadData.Loading = false;
            //}

            //localData.LoadUnloadData.CstID = "CST01";
            if (cstIDReaderL != null) //區分左右RFID Reader
            {
                string errorMessage = "";
                //讀取RFID
                if (localData.LoadUnloadData.LoadUnloadCommand != null)
                {
                    if (localData.LoadUnloadData.LoadUnloadCommand.CstLocate == EnumCstInAGVLocate.Left)
                    {
                        if (localData.LoadUnloadData.Loading_Left)
                        {
                            if (cstIDReaderL.ReadRFIDTag(ref errorMessage))
                            {
                                localData.LoadUnloadData.CstID_Left = cstIDReaderL.ReceiveString;
                                WriteLog(7, "", String.Concat("Left CSTID Read Scuess : ", localData.LoadUnloadData.CstID_Left));
                            }
                            else
                            {
                                localData.LoadUnloadData.CstID_Left = "";
                                WriteLog(7, "", "Left CSTID Read Fail");
                            }
                        }
                        else
                        {
                            localData.LoadUnloadData.CstID_Left = "";
                        }
                    }

                }


                //if (localData.LoadUnloadData.LoadUnloadCommand != null)
                //{
                //    if (localData.LoadUnloadData.LoadUnloadCommand.CstLocate == EnumCstInAGVLocate.Left)
                //    {
                //        if (localData.LoadUnloadData.Loading_Left)
                //        {
                //            localData.LoadUnloadData.CstID = "CST01";
                //            localData.LoadUnloadData.CstID_Left = "CST01";
                //            localData.LoadUnloadData.CstID_Left = localData.LoadUnloadData.LoadUnloadCommand.CSTID;
                //        }
                //        else
                //        {
                //            localData.LoadUnloadData.CstID = "";
                //            localData.LoadUnloadData.CstID_Left = "";
                //        }

                //    }
                //    else if (localData.LoadUnloadData.LoadUnloadCommand.CstLocate == EnumCstInAGVLocate.Right)
                //    {
                //        if (localData.LoadUnloadData.Loading_Right)
                //        {
                //            localData.LoadUnloadData.CstID = "CST02";
                //            localData.LoadUnloadData.CstID_Right = "CST02";
                //            localData.LoadUnloadData.CstID_Right = localData.LoadUnloadData.LoadUnloadCommand.CSTID;
                //        }
                //        else
                //        {
                //            localData.LoadUnloadData.CstID = "";
                //            localData.LoadUnloadData.CstID_Right = "";
                //        }

                //    }
                //}



                //if (localData.LoadUnloadData.Loading_Left)
                //{
                //    //localData.LoadUnloadData.CstID = "CST01";
                //    localData.LoadUnloadData.CstID_Left = "CST01";
                //}
                //else
                //{
                //    //localData.LoadUnloadData.CstID = "";
                //    localData.LoadUnloadData.CstID_Left = "";
                //}

                //if (localData.LoadUnloadData.Loading_Right)
                //{
                //    //localData.LoadUnloadData.CstID = "CST02";
                //    localData.LoadUnloadData.CstID_Right = "CST02";
                //}
                //else
                //{
                //    //localData.LoadUnloadData.CstID = "";
                //    localData.LoadUnloadData.CstID_Right = "";
                //}


            }

            if (cstIDReaderR != null) //區分左右RFID Reader
            {
                string errorMessage = "";
                //讀取RFID
                if (localData.LoadUnloadData.LoadUnloadCommand != null)
                {
                    if (localData.LoadUnloadData.LoadUnloadCommand.CstLocate == EnumCstInAGVLocate.Right)
                    {
                        if (localData.LoadUnloadData.Loading_Right)
                        {
                            if (cstIDReaderR.ReadRFIDTag(ref errorMessage))
                            {
                                localData.LoadUnloadData.CstID_Right = cstIDReaderR.ReceiveString;
                                WriteLog(7, "", String.Concat("Right CSTID Read Scuess : ", localData.LoadUnloadData.CstID_Right));
                            }
                            else
                            {
                                localData.LoadUnloadData.CstID_Right = "";
                                WriteLog(7, "", "Right CSTID Read Fail");
                            }
                        }
                        else
                        {
                            localData.LoadUnloadData.CstID_Right = "";
                        }

                    }

                }


                //if (localData.LoadUnloadData.LoadUnloadCommand != null)
                //{
                //    if (localData.LoadUnloadData.LoadUnloadCommand.CstLocate == EnumCstInAGVLocate.Left)
                //    {
                //        if (localData.LoadUnloadData.Loading_Left)
                //        {
                //            //localData.LoadUnloadData.CstID = "CST01";
                //            //localData.LoadUnloadData.CstID_Left = "CST01";
                //            localData.LoadUnloadData.CstID_Left = localData.LoadUnloadData.LoadUnloadCommand.CSTID;
                //        }
                //        else
                //        {
                //            //localData.LoadUnloadData.CstID = "";
                //            localData.LoadUnloadData.CstID_Left = "";
                //        }

                //    }
                //    else if (localData.LoadUnloadData.LoadUnloadCommand.CstLocate == EnumCstInAGVLocate.Right)
                //    {
                //        if (localData.LoadUnloadData.Loading_Right)
                //        {
                //            //localData.LoadUnloadData.CstID = "CST02";
                //            //localData.LoadUnloadData.CstID_Right = "CST02";
                //            localData.LoadUnloadData.CstID_Right = localData.LoadUnloadData.LoadUnloadCommand.CSTID;
                //        }
                //        else
                //        {
                //            //localData.LoadUnloadData.CstID = "";
                //            localData.LoadUnloadData.CstID_Right = "";
                //        }

                //    }
                //}


            }

            #endregion

        }

        //更新狀態
        public override void UpdateForkHomeStatus()
        {
            if (localData.SimulateMode)
                return;

            //更新在席sensorL,R狀態
            FOUPL1_loading.Data = (localData.MIPCData.GetDataByMIPCTagName(foupL1_loading) != 0);
            if (FOUPL1_loading.Change)
                WriteLog(7, "", String.Concat(foupL1_loading, " Change to ", (FOUPL1_loading.data ? "on" : "off")));

            FOUPL2_loading.Data = (localData.MIPCData.GetDataByMIPCTagName(foupL2_loading) != 0);
            if (FOUPL2_loading.Change)
                WriteLog(7, "", String.Concat(foupL2_loading, " Change to ", (FOUPL2_loading.data ? "on" : "off")));

            FOUPR1_loading.Data = (localData.MIPCData.GetDataByMIPCTagName(foupR1_loading) != 0);
            if (FOUPR1_loading.Change)
                WriteLog(7, "", String.Concat(foupR1_loading, " Change to ", (FOUPR1_loading.data ? "on" : "off")));

            FOUPR2_loading.Data = (localData.MIPCData.GetDataByMIPCTagName(foupR2_loading) != 0);
            if (FOUPR2_loading.Change)
                WriteLog(7, "", String.Concat(foupR2_loading, " Change to ", (FOUPR2_loading.data ? "on" : "off")));

            //if (FOUPL1_loading.Data /*&& FOUPL2_loading.Data*/)
            //{
            //    localData.LoadUnloadData.Loading = true;
            //    //Loading狀態區分L和R?
            //}
            //else if (!FOUPL1_loading.Data /*&& !FOUPL2_loading.Data*/)
            //{
            //    localData.LoadUnloadData.Loading = false;
            //}

            if (FOUPL1_loading.Data)
            {
                localData.LoadUnloadData.Loading_Left = true;
            }
            else if (!FOUPL1_loading.Data)
            {
                localData.LoadUnloadData.Loading_Left = false;
            }

            if (FOUPR1_loading.Data)
            {
                localData.LoadUnloadData.Loading_Right = true;
            }
            else if (!FOUPR1_loading.Data)
            {
                localData.LoadUnloadData.Loading_Right = false;
            }


            /*else
            {
                localData.LoadUnloadData.Loading = false;
            }*/

            /*FOUPR1_loading.Data = (localData.MIPCData.GetDataByMIPCTagName(foupR1_loading) == 0);
            if (FOUPR1_loading.Change)
                WriteLog(7, "", String.Concat(foupR1_loading, " Change to ", (FOUPR1_loading.data ? "on" : "off")));

            FOUPR2_loading.Data = (localData.MIPCData.GetDataByMIPCTagName(foupR2_loading) == 0);
            if (FOUPR2_loading.Change)
                WriteLog(7, "", String.Concat(foupR2_loading, " Change to ", (FOUPR2_loading.data ? "on" : "off")));

            if (FOUPR1_loading.Data && FOUPR2_loading.Data)
            {
                localData.LoadUnloadData.Loading = true;
                //Loading狀態區分L和R?
            }
            else if (!FOUPR1_loading.Data && !FOUPR2_loading.Data)
            {
                localData.LoadUnloadData.Loading = false;
            }
            else
            {
                localData.LoadUnloadData.Loading = false;
            }
            */
            //TM_Robot 待機狀態
            //localData.LoadUnloadData.ForkHome = true;  //Ready bit true，要改對應開機狀態

            localData.LoadUnloadData.ForkHome = localData.MIPCData.GetDataByMIPCTagName(robotStatus) == (float)EnumATSRobotStatus.Robot_ready;

        }



        public Thread homeThread = null;


        #region 找尋HomeSensor Offset.
        public override void FindHomeSensorOffsetByEncoderInHome()
        {
        }
        #endregion

        #region CSV.
        public override void WriteCSV()
        {
            if (initialEnd)
            {
                string csvLog = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff");

                LoadUnloadCommandData temp = localData.LoadUnloadData.LoadUnloadCommand;

                csvLog = String.Concat(csvLog, ",", temp == null ? "" : temp.StepString);

                AxisFeedbackData axisData = null;

                string axisNamString = "";

                for (int i = 0; i < FeedbackAxisList.Count; i++)
                {
                    axisNamString = FeedbackAxisList[i];
                    axisData = localData.LoadUnloadData.CVFeedbackData[axisNamString];

                    if (axisData != null)
                    {
                        csvLog = String.Concat(csvLog, ",", axisData.Position.ToString("0.000"));
                        csvLog = String.Concat(csvLog, ",", axisData.Velocity.ToString("0.0"));
                        csvLog = String.Concat(csvLog, ",", axisData.AxisMoveStaus.ToString());

                        if (axisNamString == EnumLoadUnloadAxisName.Z軸_Slave.ToString())
                            csvLog = String.Concat(csvLog, ",", GetMoveStatusByCVAxisName(EnumLoadUnloadAxisName.Z軸.ToString()).ToString());
                        else
                            csvLog = String.Concat(csvLog, ",", GetMoveStatusByCVAxisName(axisNamString).ToString());

                        csvLog = String.Concat(csvLog, ",", axisData.AxisServoOnOff.ToString());
                        csvLog = String.Concat(csvLog, ",", axisData.V.ToString("0.000"));
                        csvLog = String.Concat(csvLog, ",", axisData.DA.ToString("0.000"));
                        csvLog = String.Concat(csvLog, ",", axisData.QA.ToString("0.000"));
                        csvLog = String.Concat(csvLog, ",", axisData.EC.ToString("0.000"));
                        csvLog = String.Concat(csvLog, ",", axisData.MF.ToString("0.000"));
                        //csvLog = String.Concat(csvLog, ",", axisData.郭協要的溫度啦.ToString("0.000"));
                    }
                    else
                    {
                        csvLog = String.Concat(csvLog, ",");
                        csvLog = String.Concat(csvLog, ",");
                        csvLog = String.Concat(csvLog, ",");
                        csvLog = String.Concat(csvLog, ",");
                        csvLog = String.Concat(csvLog, ",");
                        csvLog = String.Concat(csvLog, ",");
                        csvLog = String.Concat(csvLog, ",");
                        csvLog = String.Concat(csvLog, ",");
                        csvLog = String.Concat(csvLog, ",");
                        csvLog = String.Concat(csvLog, ",");
                        csvLog = String.Concat(csvLog, ",");
                    }
                }

                logger.LogString(csvLog);
            }
        }
        #endregion


    }
}
