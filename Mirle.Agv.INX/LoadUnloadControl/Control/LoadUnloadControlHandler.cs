using Mirle.Agv.INX.Control;
using Mirle.Agv.INX.Controller.Tools;
using Mirle.Agv.INX.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Controller
{
    public class LoadUnloadControlHandler
    {
        private ComputeFunction computeFunction = ComputeFunction.Instance;
        private LoggerAgent loggerAgent = LoggerAgent.Instance;
        private LocalData localData = LocalData.Instance;
        private string device = MethodInfo.GetCurrentMethod().ReflectedType.Name;
        private string normalLogName = "LoadUnload";

        private MIPCControlHandler mipcControl;
        private AlarmHandler alarmHandler;
        public LoadUnload LoadUnload { get; set; } = null;

        private Thread thread = null;
        private Thread csvThread = null;

        private EnumControlStatus Status = EnumControlStatus.Ready;

        public event EventHandler<EnumLoadUnloadComplete> ForkCompleteEvent;
        public event EventHandler ForkLoadCompleteEvent; //0407liu LULComplete修改
        public event EventHandler ForkUnloadCompleteEvent;

        private Logger commandRecordLogger = LoggerAgent.Instance.GetLooger("CommandRecord");

        public void ResetAlarm()
        {
            ResetAllAlarmCodeByControl();

            if (localData.AutoManual == EnumAutoState.Manual)
            {
                localData.LoadUnloadData.ErrorBit = false;
                ResetAlarmCode(EnumLoadUnloadControlErrorCode.拒絕取放命令_LoadUnloadControlErrorBitOn);
            }

            if (localData.LoadUnloadData.LoadUnloadCommand == null)
                LoadUnload.ResetAlarm();
        }

        public LoadUnloadControlHandler(MIPCControlHandler mipcControl, AlarmHandler alarmHandler)
        {
            this.mipcControl = mipcControl;
            this.alarmHandler = alarmHandler;

            switch (localData.MainFlowConfig.AGVType)
            {
                case EnumAGVType.Demo:
                    break;

                case EnumAGVType.AGC:
                    LoadUnload = new LoadUnload_AGC();
                    break;
                case EnumAGVType.UMTC:
                    LoadUnload = new LoadUnload_UMTC();
                    break;
                case EnumAGVType.PTI:
                    LoadUnload = new LoadUnload_PTI(); //liu++
                    break;
                case EnumAGVType.ATS:
                    if (localData.SimulateMode)                 
                        LoadUnload = new LoadUnLoad_Simulation();
                    else                    
                        LoadUnload = new LoadUnload_ATS();               
                    break;
                    
                default:
                    break;
            }

            if (LoadUnload != null)
            {
                LoadUnload.Initial(mipcControl, alarmHandler);
                thread = new Thread(LoadUnloadThread);
                thread.Start();
                csvThread = new Thread(WriteCSVThread);
                csvThread.Start();

                LoadUnload.ForkLoadCompleteEvent += LoadUnload_ForkLoadCompleteEvent;
                LoadUnload.ForkUnloadCompleteEvent += LoadUnload_ForkUnloadCompleteEvent;
            }
            else
            {
                WriteLog(5, "", String.Concat("LoadUnload Initial Fail, AGVType : ", localData.MainFlowConfig.AGVType.ToString()));
            }
        }

        private void LoadUnload_ForkUnloadCompleteEvent(object sender, EventArgs e)
        {
            if (localData.AutoManual == EnumAutoState.Auto && localData.LoadUnloadData.LoadUnloadCommand != null)
                ForkUnloadCompleteEvent?.Invoke(this, null);
        }

        private void LoadUnload_ForkLoadCompleteEvent(object sender, EventArgs e)
        {
            if (localData.AutoManual == EnumAutoState.Auto && localData.LoadUnloadData.LoadUnloadCommand != null)
                ForkLoadCompleteEvent?.Invoke(this, null);
        }

        public void CloseLoadUnloadControlHanlder()
        {
            Stopwatch closeTimer = new Stopwatch();
            Status = EnumControlStatus.Closing;

            closeTimer.Restart();

            if (localData.LoadUnloadData.LoadUnloadCommand != null)
            {
                StopCommandRequest();

                Stopwatch timer = new Stopwatch();

                timer.Restart();

                while (localData.LoadUnloadData.LoadUnloadCommand != null)
                {
                    if (timer.ElapsedMilliseconds > 5000)
                    {
                        LoadUnload.JogStop();
                        break;
                    }

                    Thread.Sleep(100);
                }
            }
            else
            {
                LoadUnload.JogStop();
            }

            Status = EnumControlStatus.WaitThreadStop;

            closeTimer.Restart();
            while (thread != null && thread.IsAlive)
            {
                if (closeTimer.ElapsedMilliseconds > 1000/*config.TimeValueConfig.TimeoutValueList[EnumTimeoutValueType.EndTimeoutValue]*/)
                {
                    // log.
                    // abort 
                    break;
                }

                Thread.Sleep(100);
            }

            Status = EnumControlStatus.Closed;
        }

        #region SendAlarmCode/WriteLog.
        private void SetAlarmCode(EnumLoadUnloadControlErrorCode alarmCode)
        {
            if (!localData.AllAlarmBit.ContainsKey((int)alarmCode))
                localData.AllAlarmBit.Add((int)alarmCode, new AlarmCodeAndSetOrReset());

            localData.AllAlarmBit[(int)alarmCode].SetAlarmOnOff(true);

            if (localData.AllAlarmBit[(int)alarmCode].Change)
            {
                WriteLog(3, "", String.Concat("[AlarmCode][Set][", ((int)alarmCode).ToString(), "] Message = ", alarmCode.ToString()));
                alarmHandler.SetAlarmCode((int)alarmCode);
            }
        }

        private void ResetAlarmCode(EnumLoadUnloadControlErrorCode alarmCode)
        {
            if (!localData.AllAlarmBit.ContainsKey((int)alarmCode))
                localData.AllAlarmBit.Add((int)alarmCode, new AlarmCodeAndSetOrReset());

            localData.AllAlarmBit[(int)alarmCode].SetAlarmOnOff(false);

            if (localData.AllAlarmBit[(int)alarmCode].Change)
            {
                WriteLog(3, "", String.Concat("[AlarmCode][Reset][", ((int)alarmCode).ToString(), "] Message = ", alarmCode.ToString()));
                alarmHandler.ResetAlarmCode((int)alarmCode);
            }
        }

        public void WriteLog(int logLevel, string carrierId, string message, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            LogFormat logFormat = new LogFormat(normalLogName, logLevel.ToString(), memberName, device, carrierId, message);

            loggerAgent.Log(logFormat.Category, logFormat);

            if (logLevel <= localData.ErrorLevel)
            {
                logFormat = new LogFormat(localData.ErrorLogName, logLevel.ToString(), memberName, device, carrierId, message);
                loggerAgent.Log(logFormat.Category, logFormat);
            }
        }
        #endregion

        public void LoadUnloadThread()
        {
            string commandRecordMessage = "";

            try
            {
                while (!localData.MIPCData.StartProcessReceiveData)
                    Thread.Sleep(500);

                while (Status != EnumControlStatus.WaitThreadStop)
                {
                    if (localData.LoadUnloadData.LoadUnloadCommand != null)
                    {
                        LoadUnload.LoadUnloadStart();

                        //Time(年月日),CommandType,CommandID,StartTime,EndTime,DeltaTime,StartSOC,StartV,EndSOC,EndV,Result,ErrorCode,是否Alarm,AutoManual

                        commandRecordMessage = String.Concat(DateTime.Now.ToString("yyyy/MM/dd"), ",",
                                                             localData.LoadUnloadData.LoadUnloadCommand.Action.ToString(), ",",
                                                             localData.LoadUnloadData.LoadUnloadCommand.CommandID, ",",
                                                             localData.LoadUnloadData.LoadUnloadCommand.CommandStartTime.ToString("HH:mm:ss"), ",",
                                                             localData.LoadUnloadData.LoadUnloadCommand.CommandEndTime.ToString("HH:mm:ss"), ",",
                                                             (localData.LoadUnloadData.LoadUnloadCommand.CommandEndTime -
                                                              localData.LoadUnloadData.LoadUnloadCommand.CommandStartTime).TotalSeconds.ToString("0.00"), "s,",
                                                             localData.LoadUnloadData.LoadUnloadCommand.StartSOC.ToString("0"), ",",
                                                             localData.LoadUnloadData.LoadUnloadCommand.EndSOC.ToString("0"), ",",
                                                             localData.LoadUnloadData.LoadUnloadCommand.StartVoltage.ToString("0.0"), ",",
                                                             localData.LoadUnloadData.LoadUnloadCommand.EndVoltage.ToString("0.0"), ",",
                                                             localData.LoadUnloadData.LoadUnloadCommand.CommandResult.ToString(), ",",
                                                             (localData.LoadUnloadData.LoadUnloadCommand.ErrorCode == EnumLoadUnloadControlErrorCode.None ?
                                                                           "" : ((int)localData.LoadUnloadData.LoadUnloadCommand.ErrorCode).ToString("0")), ",",
                                                             localData.LoadUnloadData.LoadUnloadCommand.CommandResult.ToString(), ",",
                                                             localData.LoadUnloadData.LoadUnloadCommand.CommandAutoManual.ToString());

                        if (localData.MainFlowConfig.AGVType == EnumAGVType.AGC && localData.AutoManual == EnumAutoState.Auto)
                        {
                            if (localData.LoadUnloadData.Loading)
                                localData.LoadUnloadData.CstID = localData.LoadUnloadData.LoadUnloadCommand.CommandCSTID;
                            else
                                localData.LoadUnloadData.CstID = "";
                        }

                        ForkCompleteEvent?.Invoke(this, localData.LoadUnloadData.LoadUnloadCommand.CommandResult);

                        localData.LoadUnloadData.SetCommandHisotry(localData.LoadUnloadData.LoadUnloadCommand);

                        if (LoadUnload.ClearCommand())
                            WriteLog(7, "", "命令清除成功");
                        else
                            WriteLog(7, "", "命令清除失敗");

                        commandRecordLogger.LogString(commandRecordMessage);
                    }
                    else
                    {
                        LoadUnload.UpdateForkHomeStatus();
                    }

                    if (NotSendBUSY || NotSendCOMPT || NotSendTR_REQ || NotForkBusyAction || NotSendAllOff)
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.啟用PIOTimeout測試);
                    else
                        ResetAlarmCode(EnumLoadUnloadControlErrorCode.啟用PIOTimeout測試);

                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                SetAlarmCode(EnumLoadUnloadControlErrorCode.取放貨主流程Exception);
                WriteLog(1, "", String.Concat("Exception : ", ex.ToString()));
                localData.LoadUnloadData.LoadUnloadCommand = null;
            }
        }

        public void CheckAlignmentValue(EnumStageDirection direction, int stageNumber)
        {
            if (LoadUnload != null)
                LoadUnload.CheckAlingmentValue(direction, stageNumber);
        }

        public void CheckAlingmentValueByAddressID(string addressID)
        {
            if (LoadUnload != null)
                LoadUnload.CheckAlingmentValueByAddressID(addressID);
        }

        public bool StopCommandRequest()
        {
            LoadUnloadCommandData temp = localData.LoadUnloadData.LoadUnloadCommand;

            if (temp != null)
            {
                temp.StopRequest = true;
                return true;
            }
            else
                return false;
        }

        public bool ClearCommand()
        {
            return LoadUnload.ClearCommand();
        }

        public void LoadUnloadPause()
        {
            LoadUnloadCommandData temp = localData.LoadUnloadData.LoadUnloadCommand;

            if (temp != null)
                temp.Pause = true;
        }

        public void LoadUnloadContinue()
        {
            LoadUnloadCommandData temp = localData.LoadUnloadData.LoadUnloadCommand;

            if (temp != null)
                temp.Pause = false;
        }

        public void LoadUnloadGoNext()
        {
            LoadUnloadCommandData temp = localData.LoadUnloadData.LoadUnloadCommand;

            if (temp != null)
                temp.GoNext = true;
        }

        public void LoadUnloadGoBack()
        {
            LoadUnloadCommandData temp = localData.LoadUnloadData.LoadUnloadCommand;

            if (temp != null)
                temp.GoBack = true;
        }

        public bool LoadUnloadRequest(EnumLoadUnload action, EnumStageDirection stageDir, EnumStageDirection pioDir, EnumCstInAGVLocate cstLocate,
                                     string addressID, int stageNumber, int speedPercent, bool needPIO, bool breakenStepMode, string CSTID, bool alignmentValue,
                                     string commandID = "")
        {
            try
            {
                if (localData.TheMapInfo.AllAddress.ContainsKey(addressID) &&
                    localData.TheMapInfo.AllAddress[addressID].LoadUnloadDirection != EnumStageDirection.None)
                {
                    stageNumber = localData.TheMapInfo.AllAddress[addressID].StageNumber;
                    stageDir = localData.TheMapInfo.AllAddress[addressID].LoadUnloadDirection;
                    pioDir = localData.TheMapInfo.AllAddress[addressID].LoadUnloadDirection;
                }

                if (commandID == "")
                    commandID = String.Concat("Local-", DateTime.Now.ToString("HHmmss"));

                WriteLog(7, "", String.Concat("收到取放命令 CommandID = ", commandID, ", Action = ", action.ToString(), ", StargeDir = ", stageDir.ToString(),
                                              ", PioDir = ", pioDir.ToString(), ", addressID = ", addressID,
                                              ", StrgeNumber = ", stageNumber.ToString("0"), ", SpeedPercet = ", speedPercent.ToString("0.00"),
                                              ", BreakenStepMode = ", breakenStepMode.ToString(), ", CSTID = ", CSTID, ", AlignmentValue = ", alignmentValue.ToString(), ", CstLocate = ", cstLocate));
                EnumLoadUnloadControlErrorCode returnCode = EnumLoadUnloadControlErrorCode.None;

                /// 條件未寫完成.
                if (!localData.LoadUnloadData.Ready)
                    returnCode = EnumLoadUnloadControlErrorCode.拒絕取放命令_LoadUnloadControlNotReady;
                else if (localData.LoadUnloadData.ErrorBit)
                    returnCode = EnumLoadUnloadControlErrorCode.拒絕取放命令_LoadUnloadControlErrorBitOn;
                else if (needPIO && pioDir == EnumStageDirection.None && action != EnumLoadUnload.ReadCSTID)
                {
                    WriteLog(7, "", "needPIO時, PIODir不能為None");
                    returnCode = EnumLoadUnloadControlErrorCode.拒絕取放命令_資料格式錯誤;
                }
                else if (stageDir == EnumStageDirection.None && action != EnumLoadUnload.ReadCSTID)
                {
                    WriteLog(7, "", "取放命令方向不能為None");
                    returnCode = EnumLoadUnloadControlErrorCode.拒絕取放命令_資料格式錯誤;
                }
                else if (localData.AutoManual == EnumAutoState.Auto && breakenStepMode && action != EnumLoadUnload.ReadCSTID)
                {
                    WriteLog(7, "", String.Concat("分解模式只有在Manual下可執行"));
                    returnCode = EnumLoadUnloadControlErrorCode.拒絕取放命令_資料格式錯誤;
                }
                else
                {
                    //LoadUnload.UpdateLoadingAndCSTID();
                }

                LoadUnloadCommandData newLoadUnloadCommandData = new LoadUnloadCommandData();
                newLoadUnloadCommandData.AddressID = addressID;
                newLoadUnloadCommandData.CommandID = commandID;
                newLoadUnloadCommandData.CommandAutoManual = localData.AutoManual;
                newLoadUnloadCommandData.CommandCSTID = CSTID;
                newLoadUnloadCommandData.CstLocate = cstLocate;

                newLoadUnloadCommandData.CommandStartTime = DateTime.Now;
                newLoadUnloadCommandData.StartSOC = localData.BatteryInfo.Battery_SOC;
                newLoadUnloadCommandData.StartVoltage = localData.BatteryInfo.Battery_V;
                newLoadUnloadCommandData.Action = action;
                newLoadUnloadCommandData.StageDirection = stageDir;
                newLoadUnloadCommandData.PIODirection = pioDir;
                newLoadUnloadCommandData.StageNumber = stageNumber;
                newLoadUnloadCommandData.SpeedPercent = (double)speedPercent / 100;
                newLoadUnloadCommandData.NeedPIO = needPIO;
                newLoadUnloadCommandData.BreakenStopMode = breakenStepMode;
                newLoadUnloadCommandData.UsingAlignmentValue = alignmentValue;

                if (!localData.LoadUnloadData.Ready)
                    returnCode = EnumLoadUnloadControlErrorCode.拒絕取放命令_LoadUnloadControlNotReady;

                if (returnCode == EnumLoadUnloadControlErrorCode.None)
                {
                    localData.LoadUnloadData.LoadUnloadCommand = newLoadUnloadCommandData;
                    return true;
                }
                else
                {
                    SetAlarmCode(returnCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                SetAlarmCode(EnumLoadUnloadControlErrorCode.拒絕取放命令_Exception);
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }

        private void ResetAllAlarmCodeByControl()
        {
            ResetAlarmCode(EnumLoadUnloadControlErrorCode.拒絕取放命令_LoadUnloadControlNotReady);
            ResetAlarmCode(EnumLoadUnloadControlErrorCode.拒絕取放命令_資料格式錯誤);
            ResetAlarmCode(EnumLoadUnloadControlErrorCode.拒絕取放命令_Exception);
        }

        public void SetLoading_LogicFlag()
        {
            switch (localData.MainFlowConfig.AGVType) //liu0407 雙儲位
            {
                case EnumAGVType.ATS:
                    localData.LoadUnloadData.Loading_LogicFlag_Left = localData.LoadUnloadData.Loading_Left;
                    localData.LoadUnloadData.Loading_LogicFlag_Right = localData.LoadUnloadData.Loading_Right;
                    break;
                default:
            localData.LoadUnloadData.Loading_LogicFlag = localData.LoadUnloadData.Loading;
                    break;
            }

            NotSendTR_REQ = false;
            NotSendTR_REQ = false;
            NotSendTR_REQ = false;
            NotSendTR_REQ = false;
            NotSendTR_REQ = false;
        }

        public bool NotSendTR_REQ
        {
            set
            {
                if (value)
                {
                    if (localData.AutoManual == EnumAutoState.Manual &&
                        localData.LoginLevel >= EnumLoginLevel.Admin)
                        localData.LoadUnloadData.NotSendTR_REQ = value;
                }
                else
                    localData.LoadUnloadData.NotSendTR_REQ = value;
            }

            get
            {
                return localData.LoadUnloadData.NotSendTR_REQ;
            }
        }

        public bool NotSendBUSY
        {
            set
            {
                if (value)
                {
                    if (localData.AutoManual == EnumAutoState.Manual &&
                        localData.LoginLevel >= EnumLoginLevel.Admin)
                        localData.LoadUnloadData.NotSendBUSY = value;
                }
                else
                    localData.LoadUnloadData.NotSendBUSY = value;
            }

            get
            {
                return localData.LoadUnloadData.NotSendBUSY;
            }
        }

        public bool NotForkBusyAction
        {
            set
            {
                if (value)
                {
                    if (localData.AutoManual == EnumAutoState.Manual &&
                        localData.LoginLevel >= EnumLoginLevel.Admin)
                        localData.LoadUnloadData.NotForkBusyAction = value;
                }
                else
                    localData.LoadUnloadData.NotForkBusyAction = value;
            }

            get
            {
                return localData.LoadUnloadData.NotForkBusyAction;
            }
        }

        public bool NotSendCOMPT
        {
            set
            {
                if (value)
                {
                    if (localData.AutoManual == EnumAutoState.Manual &&
                        localData.LoginLevel >= EnumLoginLevel.Admin)
                        localData.LoadUnloadData.NotSendCOMPT = value;
                }
                else
                    localData.LoadUnloadData.NotSendCOMPT = value;
            }

            get
            {
                return localData.LoadUnloadData.NotSendCOMPT;
            }
        }

        public bool NotSendAllOff
        {
            set
            {
                if (value)
                {
                    if (localData.AutoManual == EnumAutoState.Manual &&
                        localData.LoginLevel >= EnumLoginLevel.Admin)
                        localData.LoadUnloadData.NotSendAllOff = value;
                }
                else
                    localData.LoadUnloadData.NotSendAllOff = value;
            }

            get
            {
                return localData.LoadUnloadData.NotSendAllOff;
            }
        }

        public void LoadUnloadPreAction()
        {
            if (LoadUnload != null)
                LoadUnload.LoadUnloadPreAction();
        }

        public bool ChangeAutoReadCstID()
        {
            switch (localData.MainFlowConfig.AGVType)
            {
                case EnumAGVType.UMTC:
                    LoadUnloadRequest(EnumLoadUnload.ReadCSTID, EnumStageDirection.Right, EnumStageDirection.Right, EnumCstInAGVLocate.None, "", 0, 100, false, false, "", false, "ReadCSTID");

                    while (localData.LoadUnloadData.LoadUnloadCommand != null)
                        Thread.Sleep(500);

                    return localData.LoadUnloadData.Ready && !localData.LoadUnloadData.ErrorBit;

                case EnumAGVType.ATS://Allen, 更新雙儲位CSTID

                    LoadUnloadRequest(EnumLoadUnload.ReadCSTID, EnumStageDirection.Left, EnumStageDirection.Left, EnumCstInAGVLocate.Left, "", 0, 100, false, false, "", false, "ReadCSTID");

                    while (localData.LoadUnloadData.LoadUnloadCommand != null)
                        Thread.Sleep(500);

                    if (localData.LoadUnloadData.Ready && !localData.LoadUnloadData.ErrorBit)
                    {
                        LoadUnloadRequest(EnumLoadUnload.ReadCSTID, EnumStageDirection.Right, EnumStageDirection.Right, EnumCstInAGVLocate.Right, "", 0, 100, false, false, "", false, "ReadCSTID");

                        while (localData.LoadUnloadData.LoadUnloadCommand != null)
                            Thread.Sleep(500);
                        return localData.LoadUnloadData.Ready && !localData.LoadUnloadData.ErrorBit;
                    }
                    else
                    {
                        return false;
                    }
            }

            return true;
        }

        private bool MoveControlInitialEnd()
        {
            if (Status == EnumControlStatus.Closing)
                return true;

            if (localData.MoveControlData.MoveControlConfig != null &&
                localData.MoveControlData.MoveControlConfig.TimeValueConfig != null &&
                localData.MoveControlData.MoveControlConfig.TimeValueConfig.IntervalTimeList != null &&
                localData.MoveControlData.MoveControlConfig.TimeValueConfig.IntervalTimeList.ContainsKey(EnumIntervalTimeType.CSVLogInterval))
                return true;
            else
                return false;
        }

        private void WriteCSVThread()
        {
            try
            {
                Stopwatch timer = new Stopwatch();
                bool isIdle;

                while (!MoveControlInitialEnd())
                    Thread.Sleep(100);

                while (Status != EnumControlStatus.Closing)
                {
                    timer.Restart();

                    if (LoadUnload != null)
                    {
                        if (localData.AutoManual == EnumAutoState.Auto)
                            isIdle = (localData.LoadUnloadData.LoadUnloadCommand == null);
                        else
                            isIdle = (localData.LoadUnloadData.LoadUnloadCommand == null && localData.LoadUnloadData.ForkHome);

                        if (localData.MainFlowConfig.IdleNotRecordCSV && isIdle)
                        {
                        }
                        else
                            LoadUnload.WriteCSV();
                    }

                    while (timer.ElapsedMilliseconds < localData.MoveControlData.MoveControlConfig.TimeValueConfig.IntervalTimeList[EnumIntervalTimeType.CSVLogInterval])
                        Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        public bool ActionCanUse(EnumUserAction action)
        {
            switch (action)
            {
                case EnumUserAction.Fork_Jog:
                    return localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null && !localData.MoveControlData.MotionControlData.JoystickMode &&
                        !localData.MoveControlData.SpecialFlow;
                case EnumUserAction.Fork_Home:
                    return localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null && !localData.MoveControlData.MotionControlData.JoystickMode &&
                        !localData.MoveControlData.SpecialFlow;
                case EnumUserAction.Fork_LocalCommand:
                    return localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null && !localData.MoveControlData.MotionControlData.JoystickMode &&
                        !localData.MoveControlData.SpecialFlow;
                case EnumUserAction.Fork_GetAlignmentValue:
                    return localData.AutoManual == EnumAutoState.Manual;
                case EnumUserAction.Fork_PIOTest:
                    return localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null && !localData.MoveControlData.MotionControlData.JoystickMode &&
                        !localData.MoveControlData.SpecialFlow;
                case EnumUserAction.Fork_HomeSetting:
                    return localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null && !localData.MoveControlData.MotionControlData.JoystickMode &&
                           !localData.MoveControlData.SpecialFlow;

                default:
                    return true;
            }
        }
    }
}
