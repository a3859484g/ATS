using Mirle.Agv.INX.Controller.Tools;
using Mirle.Agv.INX.Model;
using Mirle.Agv.INX.Model.Configs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Diagnostics;
using Mirle.Agv.MiddlePackage.Umtc.Move;
using Mirle.Agv.MiddlePackage.Umtc.Model;

namespace Mirle.Agv.INX.Controller
{
    public class MainFlowHandler
    {
        public LPMS LPMS { get; set; } = null;

        protected Logger commandRecordLogger = LoggerAgent.Instance.GetLooger("CommandRecord");
        private string normalLogName = "MainFlow";
        private string device = MethodInfo.GetCurrentMethod().ReflectedType.Name;

        #region Configs
        private MainFlowConfig mainFlowConfig;

        private string batteryLogFileName = "BatteryConfig.xml";
        private string batteryBackupLogFileName = "BatteryConfig_Backup.xml";
        #endregion

        private LoggerAgent loggerAgent = LoggerAgent.Instance;
        private ComputeFunction computeFunction = ComputeFunction.Instance;
        public AlarmHandler AlarmHandler { get; set; }
        public MapHandler MapControl { get; set; }
        public MIPCControlHandler MipcControl { get; set; }
        public MoveControlHandler MoveControl { get; set; }
        public LoadUnloadControlHandler LoadUnloadControl { get; set; }
        public UserAgent UserLoginout { get; set; }

        public event EventHandler<InitialEventArgs> OnComponentIntialDoneEvent;

        private LocalData localData = LocalData.Instance;

        public Mirle.Agv.MiddlePackage.Umtc.Controller.MainFlowHandler MiddlerContorlHandler = null;// new MiddlePackage.Umtc.Controller.MainFlowHandler();

        private Dictionary<EnumUserAction, EnumLoginLevel> allUserActionLoginLevel = new Dictionary<EnumUserAction, EnumLoginLevel>();

        private void InitialMiddlerEvent()
        {
            MipcControl.AutoManualEvent += ChangeAutoManualEvent;
            MipcControl.CallForkHome += MipcControl_CallForkHome;
            MoveControl.CallLoadUnloadPreAction += MoveControl_CallLoadUnloadPreAction;

            try
            {
                MiddlerContorlHandler = new MiddlePackage.Umtc.Controller.MainFlowHandler();
                MiddlerContorlHandler.OnComponentIntialDoneEvent += MiddlerContorlHandler_OnComponentIntialDoneEvent;
                MiddlerContorlHandler.InitialMainFlowHandler();

                localData.MiddlerVersion = MiddlerContorlHandler.MiddlerVersion;
                WriteLog(7, "", String.Concat("Middler Version : ", localData.MiddlerVersion.ToString("0.0")));
            }
            catch
            {
                MiddlerContorlHandler = null;
                WriteLog(5, "", String.Concat("Middleer初始化失敗"));
            }

            if (MiddlerContorlHandler != null)
            {
                this.AlarmHandler.SendMiddlerAlarm += AlarmHandler_SendMiddlerAlarm;
                MiddlerContorlHandler.AlarmHandler.OnMiddlePackageSetAlarmEvent += AlarmHandler_OnMiddlePackageSetAlarmEvent;
                MiddlerContorlHandler.AlarmHandler.OnMiddlePackageResetAlarmEvent += AlarmHandler_OnMiddlePackageResetAlarmEvent;

                #region 走行.
                MiddlerContorlHandler.MoveHandler.ResumeMoveEvent += MoveHandler_ResumeMoveEvent;
                MiddlerContorlHandler.MoveHandler.CancelMoveEvent += MoveHandler_CancelMoveEvent;
                MiddlerContorlHandler.MoveHandler.PauseMoveEvent += MoveHandler_PauseMoveEvent;
                MiddlerContorlHandler.MoveHandler.IsReadyForMoveCommandRequestEvent += MoveHandler_IsReadyForMoveCommandRequestEvent;
                MiddlerContorlHandler.MoveHandler.OnAddressArrivalArgsRequestEvent += MoveHandler_OnAddressArrivalArgsRequestEvent;
                MiddlerContorlHandler.MoveHandler.SetupMoveCommandInfoEvent += MoveHandler_SetupMoveCommandInfoEvent;
                MiddlerContorlHandler.MoveHandler.ReservePartMoveEvent += MoveHandler_ReservePartMoveEvent;
                MoveControl.MoveCompleteEvent += MoveControl_MoveComplete;
                MoveControl.PassAddressEvent += MoveControl_PassAddress;
                #endregion

                MiddlerContorlHandler.MoveHandler.InitialPosition();

                #region 取放.
                MiddlerContorlHandler.RobotHandler.DoRobotCommandEvent += RobotHandler_DoRobotCommandEvent;
                MiddlerContorlHandler.RobotHandler.ClearRobotCommandEvent += RobotHandler_ClearRobotCommandEvent;
                MiddlerContorlHandler.RobotHandler.OnCarrierRenameEvent += RobotHandler_OnCarrierRenameEvent;

                MiddlerContorlHandler.RobotHandler.OnCarrierSlotStatusRequestEvent += RobotHandler_OnCarrierSlotStatusRequestEvent;
                MiddlerContorlHandler.RobotHandler.OnRobotStatusRequestEvent += RobotHandler_OnRobotStatusRequestEvent;
                MiddlerContorlHandler.RobotHandler.IsReadyForRobotCommandRequestEvent += RobotHandler_IsReadyForRobotCommandRequestEvent;

                LoadUnloadControl.ForkCompleteEvent += LoadUnloadControl_ForkCompleteEvent;
                LoadUnloadControl.ForkLoadCompleteEvent += LoadUnloadControl_ForkLoadCompleteEvent; //0407lin LULCompete修改
                LoadUnloadControl.ForkUnloadCompleteEvent += LoadUnloadControl_ForkUnloadCompleteEvent;
                //void ForkComplete(EnumRobotEndType robotEndType);
                #endregion

                #region 充電.
                MiddlerContorlHandler.BatteryHandler.StartChargeEvent += BatteryHandler_StartChargeEvent;
                MiddlerContorlHandler.BatteryHandler.StopChargeEvent += BatteryHandler_StopChargeEvent;
                MiddlerContorlHandler.BatteryHandler.OnBatteryStatusRequestEvent += BatteryHandler_OnBatteryStatusRequestEvent;
                MipcControl.ChargingStatusChange += MipcControl_ChargingStatusChange;
                #endregion
            }
        }

        private void MipcControl_CallForkHome(object sender, EventArgs e)
        {
            if (LoadUnloadControl.LoadUnload != null)
                LoadUnloadControl.LoadUnload.Home();
        }

        #region 充電相關.
        private void MipcControl_ChargingStatusChange(object sender, bool e)
        {
            try
            {
                Task.Factory.StartNew(() =>
                {
                    MiddlePackage.Umtc.Model.BatteryStatus data = new MiddlePackage.Umtc.Model.BatteryStatus();
                    data.Voltage = localData.BatteryInfo.Battery_V;
                    data.Percentage = (int)localData.BatteryInfo.Battery_SOC;
                    data.IsCharging = e;
                    WriteLog(7, "", String.Concat("[Middler][Battery][Info][Send]", " { SOC = ", data.Percentage.ToString("0"),
                                                                                    ", V = ", data.Voltage.ToString("0.0"),
                                                                                    ", Charging = ", data.IsCharging.ToString(), " }"));
                    MiddlerContorlHandler.BatteryHandler.SetBatteryStatus(data);
                });
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
            }
        }

        private void BatteryHandler_OnBatteryStatusRequestEvent(object sender, MiddlePackage.Umtc.Model.BatteryStatus e)
        {
            try
            {
                e.Voltage = localData.BatteryInfo.Battery_V;
                e.Percentage = (int)localData.BatteryInfo.Battery_SOC;
                e.IsCharging = localData.MIPCData.Charging;
                WriteLog(7, "", String.Concat("[Middler][Battery][Info]", " { SOC = ", e.Percentage.ToString("0"),
                                                                              ", V = ", e.Voltage.ToString("0.0"),
                                                                              ", Charging = ", e.IsCharging.ToString(), " }"));
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
            }
        }

        private void BatteryHandler_StopChargeEvent(object sender, MiddlePackage.Umtc.Model.MidRequestArgs e)
        {
            try
            {
                WriteLog(7, "", String.Concat("[Middler][Battery][Command] Stop Charging"));
                MipcControl.StopCharging();
                e.IsOk = true;
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
            }
        }

        private void BatteryHandler_StartChargeEvent(object sender, MiddlePackage.Umtc.Battery.ChargeArgs e)
        {
            try
            {
                WriteLog(7, "", String.Concat("[Middler][Battery][Command] Start Charging { Address ID = ", e.AddressId, " }"));

                if (e != null && localData.TheMapInfo.AllAddress.ContainsKey(e.AddressId) &&
                    localData.TheMapInfo.AllAddress[e.AddressId].ChargingDirection != EnumStageDirection.None)
                {
                    if (localData.TheMapInfo.AllAddress[e.AddressId].ChargingDirection == EnumStageDirection.Left)
                        MipcControl.StartCharging(EnumStageDirection.Left);
                    else
                        MipcControl.StartCharging(EnumStageDirection.Right);

                    if (localData.MIPCData.Charging)
                        e.RequestArgs.IsOk = true;
                    else
                        e.RequestArgs.IsOk = false;
                }
                else
                {
                    e.RequestArgs.IsOk = false;
                    e.RequestArgs.ErrorMsg = "e == null 或 e.AddressID 不存在圖資內 或 e.AddressID不是充電站";
                    WriteLog(3, "", String.Concat("e == null 或 e.AddressID 不存在圖資內 或 e.AddressID不是充電站"));
                }
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
            }
        }
        #endregion

        #region Alarm.
        private void AlarmHandler_OnMiddlePackageResetAlarmEvent(object sender, EventArgs e)
        {
            try
            {
                WriteLog(7, "", String.Concat("[Middler][Alarm][Command] ResetAlarm"));
                ResetAlarm(false);
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
            }
        }

        private double waitActionMaxTime = 3000;

        private Thread resetAlarmThread = null;

        private bool temp_isLocalResetAlarm = false;

        private void ResetChangeAutoAlarm()
        {
            ResetAlarmCode(EnumMainFlowErrorCode.None);
            ResetAlarmCode(EnumMainFlowErrorCode.無法Auto_MoveControlNotReady);
            ResetAlarmCode(EnumMainFlowErrorCode.無法Auto_Fork不在Home點上);
            ResetAlarmCode(EnumMainFlowErrorCode.無法Auto_MiddlerInitialFail);
            ResetAlarmCode(EnumMainFlowErrorCode.無法Auto_尚有Alarm);
            ResetAlarmCode(EnumMainFlowErrorCode.無法Auto_ResetAlarm中);
            ResetAlarmCode(EnumMainFlowErrorCode.無法Auto_讀取CSTID異常);
            ResetAlarmCode(EnumMainFlowErrorCode.無法Auto_定位資料異常);
            ResetAlarmCode(EnumMainFlowErrorCode.無法Auto_移動中);
            ResetAlarmCode(EnumMainFlowErrorCode.無法Auto_搖桿控制中);
            ResetAlarmCode(EnumMainFlowErrorCode.無法Auto_AGV不在Section上);
        }

        private void ResetAlarmThread()
        {
            localData.MIPCData.BuzzOff = false;
            ResetChangeAutoAlarm();
            AlarmHandler.ResetAllMiddlerAlarmCode();
            Stopwatch timer = new Stopwatch();

            if (LPMS != null)
                LPMS.Reset();

            if (localData.AutoManual == EnumAutoState.Manual)
            {
                #region SafetyRelay.
                bool safetyRelayGG = !localData.MIPCData.U動力電;

                if (safetyRelayGG)
                {
                    timer.Restart();

                    while (timer.ElapsedMilliseconds < waitActionMaxTime && !localData.MIPCData.U動力電)
                        Thread.Sleep(100);
                }
                #endregion
            }

            LoadUnloadControl.ResetAlarm();

            switch (localData.MainFlowConfig.AGVType)
            {
                case EnumAGVType.AGC:
                case EnumAGVType.UMTC:
                case EnumAGVType.ATS:
                case EnumAGVType.PTI:	
                    if (localData.AutoManual == EnumAutoState.Manual)
                    {
                        #region 走行驅動器異常.
                        if (localData.MoveControlData.NeedResetMotionAlarm &&
                           (localData.LoadUnloadData.ForkHome || !localData.MoveControlData.MoveControlConfig.SensorByPass[EnumSensorSafetyType.ForkHomeCheck]) &&
                           localData.MoveControlData.MoveCommand == null &&
                           !localData.MoveControlData.MotionControlData.JoystickMode && !localData.MIPCData.Charging)
                        {
                            MipcControl.AGV_ServoOn();

                            timer.Restart();

                            while (localData.MoveControlData.MotionControlData.AllServoStatus != localData.MoveControlData.MotionControlData.AllServoOn)
                            {
                                if (timer.ElapsedMilliseconds > 3000)
                                {
                                    WriteLog(7, "", "wait all servo On timeout");
                                    break;
                                }

                                Thread.Sleep(100);
                            }

                            MipcControl.AGV_ServoOff();

                            timer.Restart();

                            while (localData.MoveControlData.MotionControlData.AllServoStatus != localData.MoveControlData.MotionControlData.AllServoOff)
                            {
                                if (timer.ElapsedMilliseconds > 3000)
                                {
                                    WriteLog(7, "", "wait all servo Off timeout");
                                    break;
                                }

                                Thread.Sleep(100);
                            }
                        }
                        #endregion
                    }
                    break;
            }

            MoveControl.ResetAlarm();
            MipcControl.ResetAlarm();

            if (temp_isLocalResetAlarm && MiddlerContorlHandler != null)
            {
                try
                {
                    WriteLog(7, "", String.Concat("[Middler][Alarm][Command][Send] ResetAlarm"));

                    MiddlerContorlHandler.AlarmHandler.ResetAlarmFromLocalPackage();
                }
                catch (Exception ex)
                {
                    WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
                }
            }

            Thread.Sleep(1000);

            AlarmHandler.SendMiddlerOldLocalAlarm();
        }

        public void ResetAlarm(bool isLocalResetAlarm = true)
        {
            if (resetAlarmThread == null || !resetAlarmThread.IsAlive)
            {
                temp_isLocalResetAlarm = isLocalResetAlarm;
                resetAlarmThread = new Thread(ResetAlarmThread);
                resetAlarmThread.Start();
            }
        }

        private void AlarmHandler_OnMiddlePackageSetAlarmEvent(object sender, MiddlePackage.Umtc.Model.AlarmArgs e)
        {
            try
            {
                WriteLog(7, "", String.Concat("[Middler][Alarm][Info] SetAlarm : ", e.ErrorCode.ToString("0")));
                this.AlarmHandler.SetAlarmCodeByMiddler(e.ErrorCode);
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
            }
        }

        private void AlarmHandler_SendMiddlerAlarm(object sender, int e)
        {
            try
            {
                Task.Factory.StartNew(() =>
                {
                    WriteLog(7, "", String.Concat("[Middler][Alarm][Info][Send] SetAlarm : ", e.ToString("0")));
                    MiddlePackage.Umtc.Model.AlarmArgs alarm = new MiddlePackage.Umtc.Model.AlarmArgs();
                    alarm.ErrorCode = e;
                    alarm.IsAlarm = false;

                    if (this.AlarmHandler.AlarmCodeTable.ContainsKey(e) && this.AlarmHandler.AlarmCodeTable[e].Level == EnumAlarmLevel.Alarm)
                        alarm.IsAlarm = true;

                    MiddlerContorlHandler.AlarmHandler.SetAlarmFromLocalPackage(alarm);
                });
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
            }
        }
        #endregion

        #region 取放相關.
        // 取放完成
        private void LoadUnloadControl_ForkCompleteEvent(object sender, EnumLoadUnloadComplete e)
        {
            Task.Factory.StartNew(() =>
            {
                if (localData.AutoManual == EnumAutoState.Auto)
                {
                    MiddlePackage.Umtc.EnumRobotEndType result = MiddlePackage.Umtc.EnumRobotEndType.Finished;

                    try
                    {
                        switch (e)
                        {
                            case EnumLoadUnloadComplete.End:
                                result = MiddlePackage.Umtc.EnumRobotEndType.Finished;
                                break;
                            case EnumLoadUnloadComplete.Error:
                                result = MiddlePackage.Umtc.EnumRobotEndType.RobotError;
                                break;
                            case EnumLoadUnloadComplete.Interlock:
                                result = MiddlePackage.Umtc.EnumRobotEndType.InterlockError;
                                break;
                            case EnumLoadUnloadComplete.DoubleStorage: //liu++
                                result = MiddlePackage.Umtc.EnumRobotEndType.DoubleStorage;
                                break;
                            case EnumLoadUnloadComplete.EmptyRetrival:
                                result = MiddlePackage.Umtc.EnumRobotEndType.EmptyRetrival;
                                break;
                            default:
                                result = MiddlePackage.Umtc.EnumRobotEndType.RobotError;
                                break;
                        }

                        WriteLog(7, "", String.Concat("[Middler][LoadUnload][Info] Command Result : ", result.ToString()));
                        MiddlerContorlHandler.RobotHandler.ForkComplete(result);
                    }
                    catch (Exception ex)
                    {
                        WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
                    }
                }
            });
        }

        private void LoadUnloadControl_ForkLoadCompleteEvent(object sender, object e)//0407lin LULCompete修改
        {
            MiddlerContorlHandler.RobotHandler.LoadComplete();
        }

        private void LoadUnloadControl_ForkUnloadCompleteEvent(object sender, object e)
        {
            MiddlerContorlHandler.RobotHandler.UnloadComplete();
        }

        // 是否可以取放
        private void RobotHandler_IsReadyForRobotCommandRequestEvent(object sender, MiddlePackage.Umtc.Model.MidRequestArgs e)
        {
            try
            {
                e.IsOk = localData.LoadUnloadData.Ready && !localData.LoadUnloadData.ErrorBit;
                WriteLog(7, "", String.Concat("[Middler][LoadUnload][Info] { ForkReady = ", e.IsOk.ToString(), " }"));
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
            }
        }

        private void RobotHandler_OnRobotStatusRequestEvent(object sender, MiddlePackage.Umtc.Model.RobotStatus e)
        {
            try
            {
                e.IsHome = localData.LoadUnloadData.ForkHome;

                if (localData.LoadUnloadData.ErrorBit)
                    e.EnumRobotState = MiddlePackage.Umtc.EnumRobotState.Error;
                else if (localData.LoadUnloadData.LoadUnloadCommand != null)
                    e.EnumRobotState = MiddlePackage.Umtc.EnumRobotState.Busy;
                else
                    e.EnumRobotState = MiddlePackage.Umtc.EnumRobotState.Idle;

                WriteLog(7, "", String.Concat("[Middler][LoadUnload][Info] { ForkHome = ", e.IsHome.ToString(),
                                                                            ",RobotState = ", e.EnumRobotState.ToString(), " }"));
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
            }
        }

        private void RobotHandler_OnCarrierSlotStatusRequestEvent(object sender, MiddlePackage.Umtc.Model.CarrierSlotStatus e)
        {
            try
            {
                LoadUnloadControl.LoadUnload.UpdateLoadingAndCSTID();

                switch (localData.MainFlowConfig.AGVType) //liu0407 雙儲位
                {
                    case EnumAGVType.ATS:
                        switch (e.SlotNumber)
                        {
                            case MiddlePackage.Umtc.EnumSlotNumber.L:
                                if (localData.LoadUnloadData.Loading_Left)
                                {
                                    if (localData.LoadUnloadData.CstID_Left == "")
                                    {
                                        e.CarrierId = "ERROR";
                                        e.EnumCarrierSlotState = MiddlePackage.Umtc.EnumCarrierSlotState.ReadFail;
                                    }
                                    else
                                    {
                                        e.CarrierId = localData.LoadUnloadData.CstID_Left;
                                        e.EnumCarrierSlotState = MiddlePackage.Umtc.EnumCarrierSlotState.Loading;
                                    }
                                }
                                else
                                {
                                    e.CarrierId = "";
                                    e.EnumCarrierSlotState = MiddlePackage.Umtc.EnumCarrierSlotState.Empty;
                                }

                                WriteLog(7, "", String.Concat("[Middler][LoadUnload][Info] { Left, Loading = ", e.EnumCarrierSlotState.ToString(), ", CSTID = ", e.CarrierId, " }"));
                                
                                break;
                            case MiddlePackage.Umtc.EnumSlotNumber.R:
                                if (localData.LoadUnloadData.Loading_Right)
                                {
                                    if (localData.LoadUnloadData.CstID_Right == "")
                                    {
                                        e.CarrierId = "";
                                        e.EnumCarrierSlotState = MiddlePackage.Umtc.EnumCarrierSlotState.ReadFail;
                                    }
                                    else
                                    {
                                        e.CarrierId = localData.LoadUnloadData.CstID_Right;
                                        e.EnumCarrierSlotState = MiddlePackage.Umtc.EnumCarrierSlotState.Loading;
                                    }
                                }
                                else
                                {
                                    e.CarrierId = "";
                                    e.EnumCarrierSlotState = MiddlePackage.Umtc.EnumCarrierSlotState.Empty;
                                }

                                WriteLog(7, "", String.Concat("[Middler][LoadUnload][Info] { Right, Loading = ", e.EnumCarrierSlotState.ToString(), ", CSTID = ", e.CarrierId, " }"));

                                break;
                        }                                        
                        break;
                    default:
                        switch (e.SlotNumber)
                        {
                            case MiddlePackage.Umtc.EnumSlotNumber.L:
                if (localData.LoadUnloadData.Loading)
                {
                    if (localData.LoadUnloadData.CstID == "")
                    {
                        e.CarrierId = "";
                        e.EnumCarrierSlotState = MiddlePackage.Umtc.EnumCarrierSlotState.ReadFail;
                    }
                    else
                    {
                        e.CarrierId = localData.LoadUnloadData.CstID;
                        e.EnumCarrierSlotState = MiddlePackage.Umtc.EnumCarrierSlotState.Loading;
                    }
                }
                else
                {
                    e.CarrierId = "";
                    e.EnumCarrierSlotState = MiddlePackage.Umtc.EnumCarrierSlotState.Empty;
                }

                WriteLog(7, "", String.Concat("[Middler][LoadUnload][Info] { Loading = ", e.EnumCarrierSlotState.ToString(),
                                                                           ", CSTID = ", e.CarrierId, " }"));

                                break;
                            case MiddlePackage.Umtc.EnumSlotNumber.R:
                                break;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
            }
        }

        private void RobotHandler_OnCarrierRenameEvent(object sender, MiddlePackage.Umtc.Model.CarrierSlotStatus e)
        {
            try
            {
                WriteLog(7, "", String.Concat("[Middler][LoadUnload][Command] CSTID ReName { CSTID = ", e.CarrierId, " }"));

                switch (localData.MainFlowConfig.AGVType) //liu0407 雙儲位
                {
                    case EnumAGVType.ATS:
                        switch (e.SlotNumber)
                        {
                            case MiddlePackage.Umtc.EnumSlotNumber.L:
                                localData.LoadUnloadData.CstID_Left = e.CarrierId;

                                if (!localData.LoadUnloadData.Loading_Left)
                                    WriteLog(1, "", "???車上好像沒有CST喔");
                                break;
                            case MiddlePackage.Umtc.EnumSlotNumber.R:
                                localData.LoadUnloadData.CstID_Right = e.CarrierId;

                                if (!localData.LoadUnloadData.Loading_Right)
                                    WriteLog(1, "", "???車上好像沒有CST喔");
                                break;
                        }
                        break;
                    default:
                        switch (e.SlotNumber)
                        {
                            case MiddlePackage.Umtc.EnumSlotNumber.L:
                localData.LoadUnloadData.CstID = e.CarrierId;

                if (!localData.LoadUnloadData.Loading)
                    WriteLog(1, "", "???車上好像沒有CST喔");
                                break;
                            case MiddlePackage.Umtc.EnumSlotNumber.R:
                                break;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
            }
        }

        private void RobotHandler_DoRobotCommandEvent(object sender, MiddlePackage.Umtc.Model.TransferSteps.RobotCommand e) //liu 0405 加入儲位資訊
        {
            try
            {
                WriteLog(7, "", String.Concat("[Middler][LoadUnload][Commnad] StartCommnad { AddressID = ", e.PortAddressId, " }"));

                if (localData.AutoManual == EnumAutoState.Auto)
                {
                    VehicleLocation now = localData.Location;
                    EnumLoadUnload type = EnumLoadUnload.Load;
                    EnumCstInAGVLocate CstInAGVLocate = EnumCstInAGVLocate.None;

                    if (e.GetTransferStepType() == MiddlePackage.Umtc.EnumTransferStepType.Load)
                        type = EnumLoadUnload.Load;
                    else if (e.GetTransferStepType() == MiddlePackage.Umtc.EnumTransferStepType.Unload)
                        type = EnumLoadUnload.Unload;
                    else
                    {
                        /// 黑人問號???
                        /// 

                        return;
                    }

                    if (e.SlotNumber == MiddlePackage.Umtc.EnumSlotNumber.L)
                        CstInAGVLocate = EnumCstInAGVLocate.Left;
                    else if (e.SlotNumber == MiddlePackage.Umtc.EnumSlotNumber.R)
                        CstInAGVLocate = EnumCstInAGVLocate.Right;
                    else
                    {
                        return;
                    }


                    if (now == null)
                    {
                        /// 0.0???
                    }
                    else if (now.LastAddress != e.PortAddressId)
                    {
                        /// 0.0???????
                    }
                    else if (localData.TheMapInfo.AllAddress[now.LastAddress].LoadUnloadDirection == EnumStageDirection.None)
                    {
                        /// 0.0???????
                    }
                    else
                    {
                        LoadUnloadControl.LoadUnloadRequest(type,
                        localData.TheMapInfo.AllAddress[now.LastAddress].LoadUnloadDirection,
                        localData.TheMapInfo.AllAddress[now.LastAddress].LoadUnloadDirection,
                        CstInAGVLocate,
                        now.LastAddress,
                        localData.TheMapInfo.AllAddress[now.LastAddress].StageNumber,
                        100,
                        localData.TheMapInfo.AllAddress[now.LastAddress].NeedPIO,
                        false,
                        e.CassetteId,
                        true, e.CmdId);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
            }
        }

        private void RobotHandler_ClearRobotCommandEvent(object sender, MiddlePackage.Umtc.Model.MidRequestArgs e)
        {
            try
            {
                WriteLog(7, "", String.Concat("[Middler][LoadUnload][Commnad] StopCommnad"));

                if (localData.AutoManual == EnumAutoState.Auto)
                    LoadUnloadControl.StopCommandRequest();
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
            }
        }
        #endregion

        #region Move相關.
        // 移動命令.
        private void MoveHandler_SetupMoveCommandInfoEvent(object sender, MiddlePackage.Umtc.Model.MoveCommandArgs e)
        {
            try
            {
                if (localData.AutoManual == EnumAutoState.Manual)
                {
                    WriteLog(3, "", "Manual 不收移動命令");
                    e.RequestArgs.IsOk = false;
                    e.RequestArgs.ErrorMsg = "Manual 不收移動命令";
                    return;
                }

                string errorMessage = "";

                MoveCmdInfo middlerMoveCommand = new MoveCmdInfo();
                middlerMoveCommand.IsAutoCommand = true;
                middlerMoveCommand.CommandID = e.CommandId;

                #region Section.
                for (int i = 0; i < e.SectionIds.Count; i++)
                {
                    if (localData.TheMapInfo.AllSection.ContainsKey(e.SectionIds[i]))
                        middlerMoveCommand.MovingSections.Add(localData.TheMapInfo.AllSection[e.SectionIds[i]]);
                    else
                    {
                        e.RequestArgs.IsOk = false;
                        e.RequestArgs.ErrorMsg = String.Concat("Section ID : ", e.SectionIds[i], " 不在圖資內");
                        WriteLog(7, "", String.Concat("[Middler][Move][Command][Receive]Move { Result = ", e.RequestArgs.IsOk.ToString(), ", ErrorMessage = ", e.RequestArgs.ErrorMsg, " }"));
                        return;
                    }
                }
                #endregion

                #region Address.
                for (int i = 0; i < e.AddressIds.Count; i++)
                {
                    if (localData.TheMapInfo.AllAddress.ContainsKey(e.AddressIds[i]))
                    {
                        if (i == 0)
                            middlerMoveCommand.StartAddress = localData.TheMapInfo.AllAddress[e.AddressIds[i]];
                        else if (i == e.AddressIds.Count - 1)
                        {
                            middlerMoveCommand.EndAddress = localData.TheMapInfo.AllAddress[e.AddressIds[i]];

                            if (localData.TheMapInfo.AllAddress[e.AddressIds[i]].ChargingDirection != EnumStageDirection.None)
                            {
                                middlerMoveCommand.IsMoveEndDoLoadUnload = true;
                                middlerMoveCommand.StageDirection = localData.TheMapInfo.AllAddress[e.AddressIds[i]].ChargingDirection;
                            }
                            else if (localData.TheMapInfo.AllAddress[e.AddressIds[i]].LoadUnloadDirection != EnumStageDirection.None)
                            {
                                middlerMoveCommand.IsMoveEndDoLoadUnload = true;
                                middlerMoveCommand.StageDirection = localData.TheMapInfo.AllAddress[e.AddressIds[i]].ChargingDirection;
                            }
                        }

                        middlerMoveCommand.MovingAddress.Add(localData.TheMapInfo.AllAddress[e.AddressIds[i]]);
                    }
                    else
                    {
                        e.RequestArgs.IsOk = false;
                        e.RequestArgs.ErrorMsg = String.Concat("Address ID : ", e.AddressIds[i], " 不在圖資內");
                        WriteLog(7, "", String.Concat("[Middler][Move][Command][Receive]Move { Result = ", e.RequestArgs.IsOk.ToString(), ", ErrorMessage = ", e.RequestArgs.ErrorMsg, " }"));
                        return;
                    }
                }
                #endregion

                e.RequestArgs.IsOk = MoveControl.VehicleMove(middlerMoveCommand, ref errorMessage);
                e.RequestArgs.ErrorMsg = errorMessage;
                WriteLog(7, "", String.Concat("[Middler][Move][Command]Move { Result = ", e.RequestArgs.IsOk.ToString(), ", ErrorMessage = ", e.RequestArgs.ErrorMsg, " }"));
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
            }
        }

        // GetReserve.
        private void MoveHandler_ReservePartMoveEvent(object sender, string e)
        {
            try
            {
                WriteLog(7, "", String.Concat("[Middler][Move][Reserve] { Section ID = ", e, " }"));
                MoveControl.AddReserve(e);
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
            }
        }

        // 詢問資料.
        private void MoveHandler_OnAddressArrivalArgsRequestEvent(object sender, MiddlePackage.Umtc.Model.AddressArrivalArgs e)
        {
            try
            {
                WriteLog(7, "", String.Concat("[Middler][Move][Info] 先不印內容"));

                e.Speed = (int)localData.MoveControlData.MotionControlData.LineVelocity;

                double movingAngle = 0;

                switch (localData.MIPCData.MoveControlDirection)
                {
                    case EnumMovingDirection.Front:
                        movingAngle = 0;
                        break;
                    case EnumMovingDirection.Back:
                        movingAngle = 180;
                        break;
                    case EnumMovingDirection.Left:
                        movingAngle = -90;
                        break;
                    case EnumMovingDirection.Right:
                        movingAngle = 90;
                        break;

                }

                e.MovingDirection = (int)localData.MoveControlData.MotionControlData.LineVelocityAngle;

                MapAGVPosition now = localData.Real;

                if (now != null)
                {
                    e.HeadAngle = (int)now.Angle;
                    e.MapPosition = new MiddlePackage.Umtc.Model.MapPosition(now.Position.X, now.Position.Y);
                }

                e.MovingDirection = (int)computeFunction.GetCurrectAngle(movingAngle + e.HeadAngle);

                MoveCommandData moveCommnad = localData.MoveControlData.MoveCommand;

                if (moveCommnad != null)
                {
                    if (moveCommnad.MoveStatus == EnumMoveStatus.Error)
                        e.EnumMoveState = MiddlePackage.Umtc.EnumMoveState.Error;
                    else if (moveCommnad.AGVPause == EnumVehicleSafetyAction.SlowStop &&
                             localData.MoveControlData.MotionControlData.MoveStatus == EnumAxisMoveStatus.Stop)
                        e.EnumMoveState = MiddlePackage.Umtc.EnumMoveState.Pause;
                    else if (moveCommnad.ReserveStop && localData.MoveControlData.MotionControlData.MoveStatus == EnumAxisMoveStatus.Stop)
                        e.EnumMoveState = MiddlePackage.Umtc.EnumMoveState.ReserveStop;
                    else if ((localData.MIPCData.SafetySensorStatus == EnumSafetyLevel.SlowStop ||
                              localData.MIPCData.SafetySensorStatus == EnumSafetyLevel.EMS) &&
                             localData.MoveControlData.MotionControlData.MoveStatus == EnumAxisMoveStatus.Stop)
                        e.EnumMoveState = MiddlePackage.Umtc.EnumMoveState.Block;
                    else
                        e.EnumMoveState = MiddlePackage.Umtc.EnumMoveState.Busy;
                }
                else
                {
                    if (localData.MoveControlData.ErrorBit)
                        e.EnumMoveState = MiddlePackage.Umtc.EnumMoveState.Error;
                    else
                        e.EnumMoveState = MiddlePackage.Umtc.EnumMoveState.Idle;
                }
                if (e.EnumMoveState == MiddlePackage.Umtc.EnumMoveState.ReserveStop)
                {
                    localData.ReserveStatus_forView = true;
                }
                else if (e.EnumMoveState == MiddlePackage.Umtc.EnumMoveState.Idle ||
                    e.EnumMoveState == MiddlePackage.Umtc.EnumMoveState.Busy)
                {
                    localData.ReserveStatus_forView = false;
                }
                e.EnumAddressArrival = MiddlePackage.Umtc.EnumAddressArrival.Arrival;

                VehicleLocation nowLocation = localData.Location;

                if (nowLocation != null)
                {
                    e.MapAddressID = nowLocation.LastAddress;
                    e.MapSectionID = nowLocation.NowSection;
                    e.InAddress = nowLocation.InAddress;
                    e.VehicleDistanceSinceHead = nowLocation.DistanceFormSectionHead;
                }
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
            }
        }

        // 是否可以移動.
        private void MoveHandler_IsReadyForMoveCommandRequestEvent(object sender, MiddlePackage.Umtc.Model.MidRequestArgs e)
        {
            try
            {
                e.IsOk = localData.MoveControlData.Ready && !localData.MoveControlData.ErrorBit;
                WriteLog(7, "", String.Concat("[Middler][Move][Info] { Ready = ", e.IsOk.ToString(), " }"));
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
            }
        }

        // MovePause.
        private void MoveHandler_PauseMoveEvent(object sender, MiddlePackage.Umtc.Model.MidRequestArgs e)
        {
            try
            {
                if (localData.AutoManual == EnumAutoState.Auto)
                {
                    e.IsOk = localData.MoveControlData.MoveCommand != null;
                    MoveControl.VehiclePause();
                }
                else
                    e.IsOk = false;

                WriteLog(7, "", String.Concat("[Middler][Move][Command] VehiclePause = ", e.IsOk.ToString()));
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
            }
        }

        // MoveContinue.
        private void MoveHandler_ResumeMoveEvent(object sender, MiddlePackage.Umtc.Model.MidRequestArgs e)
        {
            try
            {
                if (localData.AutoManual == EnumAutoState.Auto)
                {
                    e.IsOk = localData.MoveControlData.MoveCommand != null;
                    MoveControl.VehicleContinue();
                }
                else
                    e.IsOk = false;

                WriteLog(7, "", String.Concat("[Middler][Move][Command] VehicleContinue = ", e.IsOk.ToString()));
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
            }
        }

        // MoveCancel.
        private void MoveHandler_CancelMoveEvent(object sender, MiddlePackage.Umtc.Model.MidRequestArgs e)
        {
            try
            {
                if (localData.AutoManual == EnumAutoState.Auto)
                {
                    e.IsOk = localData.MoveControlData.MoveCommand != null;
                    MoveControl.VehicleCancel();
                }
                else
                    e.IsOk = false;

                WriteLog(7, "", String.Concat("[Middler][Move][Command] VehicleCancel = ", e.IsOk.ToString()));
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
            }
        }

        // 移動完成.
        public void MoveControl_MoveComplete(object sender, EnumMoveComplete status)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    Mirle.Agv.MiddlePackage.Umtc.Model.AddressArrivalArgs data = new MiddlePackage.Umtc.Model.AddressArrivalArgs();
                    data.Speed = 0;
                    data.MovingDirection = 0;

                    MapAGVPosition now = localData.Real;

                    if (now != null)
                    {
                        data.HeadAngle = (int)now.Angle;
                        data.MapPosition = new MiddlePackage.Umtc.Model.MapPosition(now.Position.X, now.Position.Y);
                    }

                    switch (status)
                    {
                        case EnumMoveComplete.Cancel:
                            data.EnumMoveState = MiddlePackage.Umtc.EnumMoveState.Idle;
                            data.EnumAddressArrival = MiddlePackage.Umtc.EnumAddressArrival.Arrival;
                            break;
                        case EnumMoveComplete.End:
                            data.EnumMoveState = MiddlePackage.Umtc.EnumMoveState.Idle;
                            data.EnumAddressArrival = MiddlePackage.Umtc.EnumAddressArrival.EndArrival;
                            break;
                        case EnumMoveComplete.Error:
                        default:
                            data.EnumMoveState = MiddlePackage.Umtc.EnumMoveState.Idle;
                            data.EnumAddressArrival = MiddlePackage.Umtc.EnumAddressArrival.Fail;
                            break;
                    }

                    VehicleLocation nowLocation = localData.Location;

                    if (nowLocation != null)
                    {
                        data.MapAddressID = nowLocation.LastAddress;
                        data.MapSectionID = nowLocation.NowSection;
                        data.VehicleDistanceSinceHead = nowLocation.DistanceFormSectionHead;
                    }

                    MiddlerContorlHandler.MoveHandler.MoveComplete(data);

                    localData.MoveControlData.MoveCommand.CommandStatus = EnumMoveCommandStartStatus.End;

                    WriteLog(7, "", String.Concat("[Middler][Move][Info][Send] { status = ", status.ToString(), " } 先不印其他內容"));
                }
                catch (Exception ex)
                {
                    WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
                    localData.MoveControlData.MoveCommand.CommandStatus = EnumMoveCommandStartStatus.End;
                }
            });
        }

        // 過站.
        public void MoveControl_PassAddress(object sender, string addressID)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    MiddlerContorlHandler.MoveHandler.PassAddress(addressID);
                    WriteLog(7, "", String.Concat("[Middler][Move][PassAddress][Send] { Address = ", addressID, " }"));
                }
                catch (Exception ex)
                {
                    WriteLog(7, "", String.Concat("[Middler][Exception] ", ex.ToString()));
                }
            });
        }
        #endregion

        private void MoveControl_CallLoadUnloadPreAction(object sender, EventArgs e)
        {
            LoadUnloadControl.LoadUnloadPreAction();
        }

        #region Language.
        private string 換行字串 = "↓←";

        private void InitialLanguageTagCSV()
        {
            try
            {
                localData.AllLanguageProfaceString.Add(EnumLanguage.None, new Dictionary<string, string>());

                foreach (EnumProfaceStringTag tag in (EnumProfaceStringTag[])Enum.GetValues(typeof(EnumProfaceStringTag)))
                {
                    localData.AllLanguageProfaceString[EnumLanguage.None].Add(tag.ToString(), (tag.ToString()).Replace("_", " "));
                }

                string path = Path.Combine(Environment.CurrentDirectory, "LanguageTag.csv");

                if (!File.Exists(path))
                    return;

                string[] allRows = File.ReadAllLines(path, Encoding.UTF8);

                if (allRows == null || allRows.Length < 1)
                {
                    WriteLog(5, "", String.Concat("LanguageTag.csv line == 0"));
                    return;
                }

                string[] titleRow = allRows[0].Split(',');
                allRows = allRows.Skip(1).ToArray();

                int nRows = allRows.Length;
                int nColumns = titleRow.Length;

                Dictionary<int, EnumLanguage> dicHeaderIndexes = new Dictionary<int, EnumLanguage>();

                EnumLanguage tempLanguage;

                for (int i = 0; i < nColumns; i++)
                {
                    string keyword = titleRow[i].Trim();

                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        if (i != 0 && Enum.TryParse(keyword, out tempLanguage))
                        {
                            if (!localData.AllLanguageProfaceString.ContainsKey(tempLanguage))
                            {
                                dicHeaderIndexes.Add(i, tempLanguage);
                                localData.AllLanguageProfaceString.Add(tempLanguage, new Dictionary<string, string>());
                            }
                        }
                    }
                }

                EnumProfaceStringTag tempTag;

                for (int i = 0; i < nRows; i++)
                {
                    string[] getThisRow = allRows[i].Split(',');

                    if (getThisRow.Length > 0 && Enum.TryParse(getThisRow[0], out tempTag))
                    {
                        for (int j = 1; j < getThisRow.Length && dicHeaderIndexes.ContainsKey(j); j++)
                        {
                            if (!localData.AllLanguageProfaceString[dicHeaderIndexes[j]].ContainsKey(tempTag.ToString()))
                                localData.AllLanguageProfaceString[dicHeaderIndexes[j]].Add(tempTag.ToString(), getThisRow[j].Replace(換行字串, "\r\n"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(5, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        private void WriteLanguageTagCSV()
        {

            try
            {
                string message = "Tag";
                List<string> dataList = new List<string>();

                List<EnumLanguage> allLanguage = new List<EnumLanguage>();

                foreach (EnumLanguage la in localData.AllLanguageProfaceString.Keys)
                {
                    if (la != EnumLanguage.None)
                    {
                        message = String.Concat(message, ",", la.ToString());
                        allLanguage.Add(la);
                    }
                }

                dataList.Add(message);

                foreach (EnumProfaceStringTag tag in (EnumProfaceStringTag[])Enum.GetValues(typeof(EnumProfaceStringTag)))
                {
                    message = tag.ToString();

                    for (int i = 0; i < allLanguage.Count; i++)
                    {
                        if (localData.AllLanguageProfaceString[allLanguage[i]].ContainsKey(tag.ToString()))
                            message = String.Concat(message, ",", localData.AllLanguageProfaceString[allLanguage[i]][tag.ToString()].Replace("\r\n", 換行字串));
                        else
                            message = String.Concat(message, ",");
                    }

                    dataList.Add(message);
                }

                string path = Path.Combine(Environment.CurrentDirectory, "LanguageTag.csv");


                using (StreamWriter outputFile = new StreamWriter(path))
                {
                    for (int i = 0; i < dataList.Count; i++)
                        outputFile.WriteLine(dataList[i]);
                }
            }
            catch (Exception ex)
            {
                WriteLog(5, "", String.Concat("Exception : ", ex.ToString()));
            }
        }
        #endregion

        public void InitialMainFlowHander()
        {

            if (!InitailXML())
                return;
            else if (!ControllersInitial())
                return;

            InitialLanguageTagCSV();

            if (localData.SimulateMode)
                WriteLanguageTagCSV();

            SetProfaceFunctionLoginLevel();
            InitialMiddlerEvent();
            ResetAlarmThread();

            localData.MIPCData.StartProcessReceiveData = true;
            OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs("MainFlow初始化成功", true, true));
        }

        public void WriteLog(int logLevel, string carrierId, string message, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            LogFormat logFormat = new LogFormat(normalLogName, logLevel.ToString(), "", "", "", message);

            loggerAgent.Log(logFormat.Category, logFormat);

            if (logLevel <= localData.ErrorLevel)
            {
                logFormat = new LogFormat(localData.ErrorLogName, logLevel.ToString(), memberName, device, carrierId, message);
                loggerAgent.Log(logFormat.Category, logFormat);
            }
        }

        private void SetAlarmCode(EnumMainFlowErrorCode alarmCode)
        {
            if (!localData.AllAlarmBit.ContainsKey((int)alarmCode))
                localData.AllAlarmBit.Add((int)alarmCode, new AlarmCodeAndSetOrReset());

            localData.AllAlarmBit[(int)alarmCode].SetAlarmOnOff(true);

            if (localData.AllAlarmBit[(int)alarmCode].Change)
            {
                WriteLog(3, "", String.Concat("[AlarmCode][Set][", ((int)alarmCode).ToString(), "] Message = ", alarmCode.ToString()));
                AlarmHandler.SetAlarmCode((int)alarmCode);
            }
        }

        private void ResetAlarmCode(EnumMainFlowErrorCode alarmCode)
        {
            if (!localData.AllAlarmBit.ContainsKey((int)alarmCode))
                localData.AllAlarmBit.Add((int)alarmCode, new AlarmCodeAndSetOrReset());

            localData.AllAlarmBit[(int)alarmCode].SetAlarmOnOff(false);

            if (localData.AllAlarmBit[(int)alarmCode].Change)
            {
                WriteLog(3, "", String.Concat("[AlarmCode][Reset][", ((int)alarmCode).ToString(), "] Message = ", alarmCode.ToString()));
                AlarmHandler.ResetAlarmCode((int)alarmCode);
            }
        }

        public void WriteMainFlowConfig()
        {
            try
            {
                XmlHandler xmlHandler = new XmlHandler();

                xmlHandler.WriteXml(localData.MainFlowConfig, Path.Combine(localData.ConfigPath, "MainFlow.xml"));
                xmlHandler.WriteXml(localData.MainFlowConfig, Path.Combine(localData.ConfigPath, "MainFlow_Backup.xml"));
            }
            catch { }
        }

        private bool InitailXML()
        {
            string xmlTarget = "";

            try
            {
                XmlHandler xmlHandler = new XmlHandler();

                xmlTarget = "MainFlow.xml";

                try
                {
                    mainFlowConfig = xmlHandler.ReadXml<MainFlowConfig>(Path.Combine(localData.ConfigPath, "MainFlow.xml")); ;
                }
                catch
                {
                    OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(String.Concat("讀取 ", xmlTarget, " 失敗")));

                    xmlTarget = "MainFlow_Backup.xml";
                    mainFlowConfig = xmlHandler.ReadXml<MainFlowConfig>(Path.Combine(localData.ConfigPath, "MainFlow_Backup.xml"));
                }

                localData.MainFlowConfig = mainFlowConfig;
                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(String.Concat("讀取 ", xmlTarget, " 成功")));
                localData.SimulateMode = mainFlowConfig.SimulateMode;

                xmlTarget = "MapConfig.xml";
                localData.MapConfig = xmlHandler.ReadXml<MapConfig>(Path.Combine(Environment.CurrentDirectory, "MapConfig.xml"));

                xmlTarget = batteryLogFileName;

                try
                {
                    localData.BatteryConfig = new BatteryConfig();

                    localData.BatteryConfig = xmlHandler.ReadXml<BatteryConfig>(Path.Combine(localData.ConfigPath, batteryLogFileName));
                    OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(String.Concat("讀取 ", xmlTarget, " 成功")));
                }
                catch
                {
                    OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(String.Concat("讀取 ", xmlTarget, " 失敗")));
                    xmlTarget = batteryBackupLogFileName;

                    try
                    {
                        localData.BatteryConfig = xmlHandler.ReadXml<BatteryConfig>(Path.Combine(localData.ConfigPath, batteryBackupLogFileName));
                        OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(String.Concat("讀取 ", xmlTarget, " 成功")));
                    }
                    catch
                    {
                        OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(String.Concat("讀取 ", xmlTarget, " 失敗")));
                        localData.BatteryConfig = new BatteryConfig();
                        WriteBatteryConfig();
                    }
                }

                return true;
            }
            catch (Exception)
            {
                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(String.Concat("讀取 ", xmlTarget, " 失敗"), true));
                return false;
            }
        }

        public void WriteBatteryConfig()
        {
            try
            {
                XmlHandler xmlHandler = new XmlHandler();

                xmlHandler.WriteXml(localData.BatteryConfig, Path.Combine(localData.ConfigPath, batteryLogFileName));
                xmlHandler.WriteXml(localData.BatteryConfig, Path.Combine(localData.ConfigPath, batteryBackupLogFileName));
            }
            catch
            {
                WriteLog(5, "", "Battery Config 寫黨失敗");
            }
        }

        private bool ControllersInitial()
        {
            string xmlTarget = "";

            try
            {
                UserLoginout = new UserAgent();

                xmlTarget = "AlarmHandler";
                AlarmHandler = new AlarmHandler();
                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(String.Concat(xmlTarget, " 初始化成功")));

                xmlTarget = "MapHandler";
                MapControl = new MapHandler(normalLogName);
                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(String.Concat(xmlTarget, " 初始化成功")));

                xmlTarget = "MIPCControlHandler";
                MipcControl = new MIPCControlHandler(this, AlarmHandler);
                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(String.Concat(xmlTarget, " 初始化成功")));

                xmlTarget = "LoadUnloadControlHandler";
                LoadUnloadControl = new LoadUnloadControlHandler(MipcControl, AlarmHandler);
                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(String.Concat(xmlTarget, " 初始化成功")));

                xmlTarget = "MoveControlHandler";
                MoveControl = new MoveControlHandler(MipcControl, AlarmHandler, LoadUnloadControl);
                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(String.Concat(xmlTarget, " 初始化成功")));

                if (localData.MainFlowConfig.LPMSComport > 0)
                    LPMS = new LPMS(localData.MainFlowConfig.LPMSComport);

                return true;
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat(xmlTarget, " 初始化Exception : ", ex.ToString()));
                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(String.Concat(xmlTarget, " 初始化失敗"), true));
                return false;
            }
        }

        private void MiddlerContorlHandler_OnComponentIntialDoneEvent(object sender, MiddlePackage.Umtc.Model.InitialEventArgs e)
        {
            if (!e.IsOk)
                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(String.Concat("Middler : ", e.ItemName, "初始化失敗")));
        }

        public void CloseMainFlowHandler()
        {
            if (localData.MIPCData.Charging)
                MipcControl.StopCharging();

            MoveControl.CloseMoveControlHandler();
            LoadUnloadControl.CloseLoadUnloadControlHanlder();
            MipcControl.CloseMipcControlHandler();

            AlarmHandler.CloseAlarmHandler();
        }

        private Thread changeAutoManualThread = null;

        private void ChangeAutoManualThread()
        {
            DateTime startTime = DateTime.Now;
            bool result = ChangeAutoManual(changeAutoManual);
            DateTime endTime = DateTime.Now;
            commandRecordLogger.LogString(String.Concat(DateTime.Now.ToString("yyyy/MM/dd"), ",", "Change", changeAutoManual.ToString(), ",,",
                startTime.ToString("HH:mm:ss"), ",", endTime.ToString("HH:mm:ss"), ",,,,,,", (result ? "Success" : "Failed"), ",,,"));
        }

        private EnumAutoState changeAutoManual = EnumAutoState.Manual;

        private void ChangeAutoManualEvent(object sender, EnumAutoState state)
        {
            if (changeAutoManualThread == null || !changeAutoManualThread.IsAlive)
            {
                changeAutoManual = state;
                changeAutoManualThread = new Thread(ChangeAutoManualThread);
                changeAutoManualThread.Start();
            }
        }

        public void ChangeAutoManual_MainForm()
        {
            if (changeAutoManualThread == null || !changeAutoManualThread.IsAlive)
            {
                switch (localData.AutoManual)
                {
                    case EnumAutoState.Auto:
                        changeAutoManual = EnumAutoState.Manual;
                        break;
                    case EnumAutoState.Manual:
                        changeAutoManual = EnumAutoState.Auto;
                        break;
                    default:
                        changeAutoManual = EnumAutoState.Manual;
                        break;
                }

                changeAutoManualThread = new Thread(ChangeAutoManualThread);
                changeAutoManualThread.Start();
            }
        }

        private object changeAutoManualObject = new object();

        public bool ChangeAutoManual(EnumAutoState state)
        {
            lock (changeAutoManualObject)
            {
                EnumAutoState nowState = localData.AutoManual;

                if (state == nowState)
                    return true;

                WriteLog(7, "", String.Concat("嘗試切換成", state.ToString()));

                switch (localData.AutoManual)
                {
                    case EnumAutoState.Auto:
                        if (localData.MoveControlData.MoveCommand != null)
                            MoveControl.VehicleStop();
                        else if (localData.LoadUnloadData.LoadUnloadCommand != null)
                            LoadUnloadControl.StopCommandRequest();

                        localData.AutoManual = EnumAutoState.Manual;

                        if (MiddlerContorlHandler != null)
                            MiddlerContorlHandler.RemoteModeHandler.SetAutoState(MiddlePackage.Umtc.EnumAutoState.Manual);

                        return true;
                    case EnumAutoState.Manual:
                        if (MiddlerContorlHandler == null)
                            SetAlarmCode(EnumMainFlowErrorCode.無法Auto_MiddlerInitialFail);
                        else if (localData.MIPCData.HasAlarm)
                            SetAlarmCode(EnumMainFlowErrorCode.無法Auto_尚有Alarm);
                        else if (!localData.MoveControlData.MoveControlCanAuto)
                        {
                            if (localData.MoveControlData.MoveCommand != null)
                            {
                                SetAlarmCode(EnumMainFlowErrorCode.無法Auto_移動中);
                                MoveControl.VehicleStop();
                            }
                            else if (localData.MoveControlData.MotionControlData.JoystickMode)
                                SetAlarmCode(EnumMainFlowErrorCode.無法Auto_搖桿控制中);
                            else if (!localData.MoveControlData.LocateControlData.SetMIPCPositionOK)
                                SetAlarmCode(EnumMainFlowErrorCode.無法Auto_定位資料異常);
                            else
                            {
                                VehicleLocation now = localData.Location;

                                if (now != null && !now.InSection)
                                    SetAlarmCode(EnumMainFlowErrorCode.無法Auto_AGV不在Section上);
                                else
                                    SetAlarmCode(EnumMainFlowErrorCode.無法Auto_MoveControlNotReady);
                            }
                        }
                        else if (!localData.LoadUnloadData.Ready)
                            SetAlarmCode(EnumMainFlowErrorCode.無法Auto_Fork不在Home點上);
                        else if (resetAlarmThread != null && resetAlarmThread.IsAlive)
                            SetAlarmCode(EnumMainFlowErrorCode.無法Auto_ResetAlarm中);
                        else
                        {
                            if (!MoveControl.BeforeAutoAction_MoveToAddress())
                                WriteLog(5, "", "BeforeAutoAction_MoveToAddress reture false");


                            switch (localData.MainFlowConfig.AGVType) //liu0407 雙儲位
                            {
                                case EnumAGVType.ATS:
                                    if ((localData.LoadUnloadData.Loading_Left && localData.LoadUnloadData.CstID_Left == "") ||
                                        (!localData.LoadUnloadData.Loading_Left && localData.LoadUnloadData.CstID_Left != ""))    //Allen,
                                    {
                                        WriteLog(5, "", "Know CstID_Left, 自動讀取");

                                        if (!LoadUnloadControl.ChangeAutoReadCstID())
                                        {
                                            SetAlarmCode(EnumMainFlowErrorCode.無法Auto_讀取CSTID異常);
                                            return false;
                                        }
                                    }
                                    if ((localData.LoadUnloadData.Loading_Right && localData.LoadUnloadData.CstID_Right == "") ||
                                        (!localData.LoadUnloadData.Loading_Right && localData.LoadUnloadData.CstID_Right != ""))   //Allen,
                                    {
                                        WriteLog(5, "", "Know CstID_Left, 自動讀取");

                                        if (!LoadUnloadControl.ChangeAutoReadCstID())
                                        {
                                            SetAlarmCode(EnumMainFlowErrorCode.無法Auto_讀取CSTID異常);
                                            return false;
                                        }
                                    }
                                    break;
                                default:
                            if (localData.LoadUnloadData.Loading && localData.LoadUnloadData.CstID == "")
                            {
                                WriteLog(5, "", "Know CstID, 自動讀取");

                                if (!LoadUnloadControl.ChangeAutoReadCstID())
                                {
                                    SetAlarmCode(EnumMainFlowErrorCode.無法Auto_讀取CSTID異常);
                                    return false;
                                }
                            }
                                    break;
                            }

                            localData.MIPCData.BypassIO = false;
                            LoadUnloadControl.SetLoading_LogicFlag();
                            localData.AutoManual = EnumAutoState.Auto;

                            MiddlerContorlHandler.RemoteModeHandler.SetAutoState(MiddlePackage.Umtc.EnumAutoState.Auto);
                            return true;
                        }

                        break;
                }
            }

            return false;
        }

        private void UpdateInternetData()
        {
        }

        public bool LocalTestCommandStop { get; set; } = false;

        public bool ForByAddressIDAndAction_ManualTest(string addreeID, EnumLoadUnload action)
        {
            if (!ActionCanUse(EnumUserAction.Fork_LocalCommand))
                return false;

            switch (localData.MainFlowConfig.AGVType)
            {
                case EnumAGVType.ATS:
                    if (!LoadUnloadControl.LoadUnloadRequest(action,
                                localData.TheMapInfo.AllAddress[addreeID].LoadUnloadDirection,
                                localData.TheMapInfo.AllAddress[addreeID].LoadUnloadDirection,
                                EnumCstInAGVLocate.Left,
                                addreeID,
                                localData.TheMapInfo.AllAddress[addreeID].StageNumber,
                                100,
                                localData.TheMapInfo.AllAddress[addreeID].NeedPIO,
                                false,
                                "",
                                true))
                        return false;

                    break;
                default:
                    if (!LoadUnloadControl.LoadUnloadRequest(action,
                                localData.TheMapInfo.AllAddress[addreeID].LoadUnloadDirection,
                                localData.TheMapInfo.AllAddress[addreeID].LoadUnloadDirection,
                                EnumCstInAGVLocate.None,
                                addreeID,
                                localData.TheMapInfo.AllAddress[addreeID].StageNumber,
                                100,
                                localData.TheMapInfo.AllAddress[addreeID].NeedPIO,
                                false,
                                "",
                                true))
                        return false;

                    break;

            }

            while (localData.LoadUnloadData.LoadUnloadCommand != null)
            {
                if (LocalTestCommandStop)
                    return false;

                Thread.Sleep(10);
            }

            if (localData.LoadUnloadData.ErrorBit || !localData.LoadUnloadData.Ready)
                return false;

            if (action == EnumLoadUnload.Load && !localData.LoadUnloadData.Loading)
                return false;
            else if (action == EnumLoadUnload.Unload && localData.LoadUnloadData.Loading)
                return false;

            return true;
        }

        int localMoveCommandIndex = 0;

        public bool MoveByAddressID_ManualTest(string addressID)
        {
            string errorMessage = "";
            MoveCmdInfo moveCmdInfo = new MoveCmdInfo();
            moveCmdInfo.CommandID = String.Concat("LocalCycleRun ", localMoveCommandIndex.ToString());
            localMoveCommandIndex++;

            VehicleLocation nowLocation = localData.Location;
            MapAGVPosition now = localData.Real;

            if (nowLocation != null && localData.TheMapInfo.AllSection.ContainsKey(nowLocation.NowSection) &&
                (localData.TheMapInfo.AllSection[nowLocation.NowSection].FromAddress.Id == addressID ||
                 localData.TheMapInfo.AllSection[nowLocation.NowSection].ToAddress.Id == addressID))
            {
                if (now != null &&
                    computeFunction.GetDistanceFormTwoAGVPosition(now, localData.TheMapInfo.AllAddress[addressID].AGVPosition) < 100 &&
                    Math.Abs(computeFunction.GetCurrectAngle(now.Angle - localData.TheMapInfo.AllAddress[addressID].AGVPosition.Angle)) < 3)
                    return true;
            }

            if (!MoveControl.CreateMoveCommandList.Step0_CheckMovingAddress(new List<string> { addressID }, ref moveCmdInfo, ref errorMessage))
                return false;

            if (!MoveControl.VehicleMove_DebugForm(moveCmdInfo, ref errorMessage))
                return false;

            MoveCommandData temp = localData.MoveControlData.MoveCommand;

            if (temp != null)
            {
                MoveControl.StartCommand();

                for (int i = 0; i < temp.ReserveList.Count; i++)
                    MoveControl.AddReservedIndexForDebugModeTest(i);
            }
            else
                return false;

            while (localData.MoveControlData.MoveCommand != null)
            {
                if (LocalTestCommandStop)
                {
                    MoveControl.VehicleStop();
                    return false;
                }

                Thread.Sleep(10);
            }

            if (localData.MoveControlData.ErrorBit)
                return false;

            return true;
        }

        private void SetProfaceFunctionLoginLevel()
        {
            EnumLoginLevel tempLevel = EnumLoginLevel.User;

            foreach (EnumUserAction item in (EnumUserAction[])Enum.GetValues(typeof(EnumUserAction)))
            {
                switch (item)
                {
                    #region 主業面.
                    case EnumUserAction.Main_Hide:
                        tempLevel = EnumLoginLevel.Admin;
                        break;
                    #endregion

                    #region 走行.
                    case EnumUserAction.Move_Jog_SettingAccDec:
                        tempLevel = EnumLoginLevel.MirleAdmin;
                        break;
                    case EnumUserAction.Move_LocalCommand:
                        tempLevel = EnumLoginLevel.Engineer;
                        break;
                    case EnumUserAction.Move_SetSlamAddressPosition:
                        tempLevel = EnumLoginLevel.MirleAdmin;
                        break;
                    case EnumUserAction.Move_LocateDriver_TriggerChange:
                        tempLevel = EnumLoginLevel.MirleAdmin;
                        break;
                    case EnumUserAction.Move_ForceSetSlamDataOK:
                        tempLevel = EnumLoginLevel.Admin;
                        break;
                    #endregion

                    #region 取放.
                    case EnumUserAction.Fork_PIOTest:
                        tempLevel = EnumLoginLevel.MirleAdmin;
                        break;
                    case EnumUserAction.Fork_HomeSetting:
                        tempLevel = EnumLoginLevel.Admin;
                        break;
                    #endregion

                    #region 充電.
                    case EnumUserAction.Charging_ChargingTest:
                        tempLevel = EnumLoginLevel.MirleAdmin;
                        break;
                    case EnumUserAction.Charging_PIOTest:
                        tempLevel = EnumLoginLevel.MirleAdmin;
                        break;
                    #endregion

                    #region IO.
                    case EnumUserAction.IO_IOTest:
                        tempLevel = EnumLoginLevel.MirleAdmin;
                        break;
                    case EnumUserAction.IO_ChangeAreaSensorDirection:
                        tempLevel = EnumLoginLevel.MirleAdmin;
                        break;
                    #endregion

                    #region 參數.
                    case EnumUserAction.Parameter_SafetySensor_ByPassAlarm:
                        tempLevel = EnumLoginLevel.Engineer;
                        break;
                    case EnumUserAction.Parameter_BatteryConfig:
                        tempLevel = EnumLoginLevel.Admin;
                        break;
                    case EnumUserAction.Parameter_PIOTimeoutConfig:
                        tempLevel = EnumLoginLevel.Admin;
                        break;
                    case EnumUserAction.Parameter_MoveConfig:
                        tempLevel = EnumLoginLevel.Admin;
                        break;
                    case EnumUserAction.Parameter_MainConfig:
                        tempLevel = EnumLoginLevel.Admin;
                        break;
                    #endregion

                    default:
                        tempLevel = EnumLoginLevel.User;
                        break;
                }

                allUserActionLoginLevel.Add(item, tempLevel);
            }
        }

        public bool ActionCanUse(EnumUserAction action)
        {
            if (localData.LoginLevel >= allUserActionLoginLevel[action])
            {
                switch (action)
                {
                    case EnumUserAction.Main_Hide:
                        return true;
                    case EnumUserAction.Main_IPCPowerOff:
                        return localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null && localData.LoadUnloadData.LoadUnloadCommand == null && !localData.MIPCData.Charging;

                    case EnumUserAction.Move_Jog:
                    case EnumUserAction.Move_Jog_SettingAccDec:
                    case EnumUserAction.Move_SpecialFlow_ReviseByTarget:
                    case EnumUserAction.Move_SpecialFlow_ToSectionCenter:
                    case EnumUserAction.Move_SpecialFlow_ReviseByTargetOrLocateData:
                    case EnumUserAction.Move_SpecialFlow_ActionBeforeAuto_MoveToAddressIfClose:
                    case EnumUserAction.Move_LocalCommand:
                    case EnumUserAction.Move_SetSlamAddressPosition:
                    case EnumUserAction.Move_LocateDriver_TriggerChange:
                    case EnumUserAction.Move_SetPosition:
                    case EnumUserAction.Move_ForceSetSlamDataOK:
                        return MoveControl.ActionCanUse(action);

                    case EnumUserAction.Fork_Jog:
                    case EnumUserAction.Fork_Home:
                    case EnumUserAction.Fork_LocalCommand:
                    case EnumUserAction.Fork_GetAlignmentValue:
                    case EnumUserAction.Fork_PIOTest:
                    case EnumUserAction.Fork_HomeSetting:
                        return LoadUnloadControl.ActionCanUse(action);

                    case EnumUserAction.Charging_LocalCommand:
                    case EnumUserAction.Charging_ChargingTest:
                    case EnumUserAction.Charging_PIOTest:
                        return localData.AutoManual == EnumAutoState.Manual;

                    case EnumUserAction.IO_IOTest:
                    case EnumUserAction.IO_ChangeAreaSensorDirection:
                        return localData.AutoManual == EnumAutoState.Manual;

                    case EnumUserAction.Parameter_SafetySensor_ByPassSafety:
                        return true;
                    case EnumUserAction.Parameter_SafetySensor_ByPassAlarm:
                        return true;

                    case EnumUserAction.Parameter_BatteryConfig:
                        return true;
                    case EnumUserAction.Parameter_PIOTimeoutConfig:
                        return true;
                    case EnumUserAction.Parameter_MoveConfig:
                        return true;
                    case EnumUserAction.Parameter_MainConfig:
                        return true;
                    default:
                        return true;
                }
            }
            else
                return false;
        }

        public bool TabPageEnable(EnumProfacePageIndex pageTag)
        {
            switch (pageTag)
            {
                case EnumProfacePageIndex.Main:
                    return true;
                case EnumProfacePageIndex.Move_Select:
                    return true;
                case EnumProfacePageIndex.Move_Jog:
                    return localData.AutoManual == EnumAutoState.Manual;
                case EnumProfacePageIndex.Move_Map:
                    return true;
                case EnumProfacePageIndex.Move_DataInfo:
                    return true;
                case EnumProfacePageIndex.Move_AxisData:
                    return true;
                case EnumProfacePageIndex.Move_LocateDriver:
                    return true;
                case EnumProfacePageIndex.Move_CommandRecord:
                    return true;
                case EnumProfacePageIndex.Move_SetSlamPosition:
                    return true;

                case EnumProfacePageIndex.Fork_Select:
                    return true;
                case EnumProfacePageIndex.Fork_Jog:
                    return localData.AutoManual == EnumAutoState.Manual;
                case EnumProfacePageIndex.Fork_Home:
                    return localData.AutoManual == EnumAutoState.Manual;
                case EnumProfacePageIndex.Fork_Command:
                    return localData.AutoManual == EnumAutoState.Manual;
                case EnumProfacePageIndex.Fork_Alignment:
                    return ActionCanUse(EnumUserAction.Fork_GetAlignmentValue);
                case EnumProfacePageIndex.Fork_CommandRecord:
                    return true;
                case EnumProfacePageIndex.Fork_PIO:
                    return true;
                case EnumProfacePageIndex.Fork_AxisData:
                    return true;
                case EnumProfacePageIndex.Fork_HomeSetting_UMTC:
                    return localData.AutoManual == EnumAutoState.Manual;


                case EnumProfacePageIndex.Charging_Select:
                    return true;
                case EnumProfacePageIndex.Charging_BatteryInfo:
                    return true;
                case EnumProfacePageIndex.Charging_Command:
                    return localData.AutoManual == EnumAutoState.Manual;
                case EnumProfacePageIndex.Charging_PIO:
                    return true;
                case EnumProfacePageIndex.Charging_Record:
                    return true;

                case EnumProfacePageIndex.IO:
                    return true;

                case EnumProfacePageIndex.Alarm:
                    return true;

                case EnumProfacePageIndex.Parameter:
                    return true;

                default:
                    return true;
            }
        }
    }
}
