using Mirle.Agv.MiddlePackage.Umtc.Model;
using Mirle.Agv.MiddlePackage.Umtc.Model.Configs;
using Mirle.Agv.MiddlePackage.Umtc.Model.TransferSteps;
using System.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using com.mirle.aka.sc.ProtocolFormat.agvMessage ;

namespace Mirle.Agv.MiddlePackage.Umtc.Controller
{
    public class MainFlowHandler
    {
        public double MiddlerVersion { get; set; } = 210524.0;

        public string LogConfigPath { get; set; } = "MainLog.ini";

        #region TransCmds
        public bool IsOverrideMove { get; set; }
        public bool IsAvoidMove { get; set; }
        public bool IsArrivalCharge { get; set; } = false;

        #endregion

        #region Controller

        private NLog.Logger _transferLogger = NLog.LogManager.GetLogger("Transfer");

        public AgvcConnector agvcConnector;

        public MapHandler mapHandler;

        //public UserAgent UserAgent { get; set; }
        internal Robot.IRobotHandler RobotHandler { get; set; }
        internal Battery.IBatteryHandler BatteryHandler { get; set; }
        internal Move.IMoveHandler MoveHandler { get; set; }
        internal RemoteMode.IRemoteModeHandler RemoteModeHandler { get; set; }
        internal Alarms.IAlarmHandler AlarmHandler { get; set; }

        #endregion

        #region Threads

        private Thread thdVisitTransferSteps;
        public bool IsVisitTransferStepPause { get; set; } = false;

        private Thread thdWatchChargeStage;
        public bool IsWatchChargeStagePause { get; set; } = false;

        #endregion

        #region Events

        public event EventHandler<InitialEventArgs> OnComponentIntialDoneEvent;

        #endregion

        #region Models

        public Vehicle Vehicle;

        private bool isIniOk;
        public int InitialSoc { get; set; } = 70;
        public bool IsFirstAhGet { get; set; }
        public string CanAutoMsg { get; set; } = "";
        public bool WaitingTransferCompleteEnd { get; set; } = false;
        public System.Text.StringBuilder SbDebugMsg { get; set; } = new System.Text.StringBuilder(short.MaxValue);
        public LastIdlePosition LastIdlePosition { get; set; } = new LastIdlePosition();
        public bool IsLowPowerStartChargeTimeout { get; set; } = false;
        public bool IsStopChargTimeoutInRobotStep { get; set; } = false;
        public DateTime LowPowerStartChargeTimeStamp { get; set; } = DateTime.Now;
        public int LowPowerRepeatedlyChargeCounter { get; set; } = 0;
        public bool IsStopCharging { get; set; } = false;
        public int isFirstReserveStop { get; set; } = 10; //liu
        #endregion

        public MainFlowHandler()
        {
            isIniOk = true;
        }

        #region InitialComponents

        public void InitialMainFlowHandler()
        {
            Vehicle = Vehicle.Instance;
            LoggersInitial();
            ConfigInitial();
            VehicleInitial();
            ControllersInitial();
            EventInitial();

            VehicleLocationInitialAndThreadsInitial();

            if (isIniOk)
            {
                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(true, "全部"));
                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, "Start Process Ok.");
            }
        }

        private void LoggersInitial()
        {
            try
            {
                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(true, "紀錄器"));
            }
            catch (Exception)
            {
                isIniOk = false;
                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(false, "紀錄器缺少 Log.ini"));
            }
        }

        private void ConfigInitial()
        {
            try
            {             
                
                string filename = Path.Combine(Vehicle.MiddlerConfigPath, "MainFlowConfig.json");
                Vehicle.MainFlowConfig = ReadFromJsonFilename<MainFlowConfig>(filename);
                if (Vehicle.MainFlowConfig.IsSimulation)
                {
                    Vehicle.LoginLevel = EnumLoginLevel.Admin;
                }
                int minThreadSleep = 100;
                Vehicle.MainFlowConfig.VisitTransferStepsSleepTimeMs = Math.Max(Vehicle.MainFlowConfig.VisitTransferStepsSleepTimeMs, minThreadSleep);
                Vehicle.MainFlowConfig.TrackPositionSleepTimeMs = Math.Max(Vehicle.MainFlowConfig.TrackPositionSleepTimeMs, minThreadSleep);
                Vehicle.MainFlowConfig.WatchLowPowerSleepTimeMs = Math.Max(Vehicle.MainFlowConfig.WatchLowPowerSleepTimeMs, minThreadSleep);
                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(true, $"讀寫 {filename}"));

                filename = Path.Combine(Vehicle.MiddlerConfigPath, "MapConfig.json");
                Vehicle.MapConfig = ReadFromJsonFilename<MapConfig>(filename);
                if (string.IsNullOrEmpty(Vehicle.MapConfig.FolderName))
                {
                    isIniOk = false;                   
                }
                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(true, $"讀寫 {filename}"));

                filename = Path.Combine(Vehicle.MiddlerConfigPath, "AgvcConnectorConfig.json");
                Vehicle.AgvcConnectorConfig = ReadFromJsonFilename<AgvcConnectorConfig>(filename);
                Vehicle.AgvcConnectorConfig.ScheduleIntervalMs = Math.Max(Vehicle.AgvcConnectorConfig.ScheduleIntervalMs, minThreadSleep);
                Vehicle.AgvcConnectorConfig.AskReserveIntervalMs = Math.Max(Vehicle.AgvcConnectorConfig.AskReserveIntervalMs, minThreadSleep);
                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(true, $"讀寫 {filename}"));

                filename = Path.Combine(Vehicle.MiddlerConfigPath, "AlarmConfig.json");
                Vehicle.AlarmConfig = ReadFromJsonFilename<AlarmConfig>(filename);
                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(true, $"讀寫 {filename}"));

                filename = Path.Combine(Vehicle.MiddlerConfigPath, "BatteryLog.json");
                Vehicle.BatteryLog = ReadFromJsonFilename<BatteryLog>(filename);
                InitialSoc = Vehicle.BatteryLog.InitialSoc;
                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(true, $"讀寫 {filename}"));

                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(true, "讀寫設定檔"));
            }
            catch (Exception ex)
            {
                var xx = ex.Message;
                isIniOk = false;
                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(false, "讀寫設定檔"));
            }
        }

        private T ReadFromJsonFilename<T>(string filename)
        {
            var allText = System.IO.File.ReadAllText(filename);
            return JsonConvert.DeserializeObject<T>(allText);
        }

        private void ControllersInitial()
        {
            try
            {
                mapHandler = new MapHandler();
                agvcConnector = new AgvcConnector(this);
                //UserAgent = new UserAgent();

                if (Vehicle.MainFlowConfig.IsSimulation)
                {
                    RobotHandler = new Robot.NullObjRobotHandler(Vehicle.RobotStatus, Vehicle.CarrierSlotLeft);
                    BatteryHandler = new Battery.NullObjBatteryHandler(Vehicle.BatteryStatus);
                    MoveHandler = new Move.NullObjMoveHandler(Vehicle.MapInfo);
                    RemoteModeHandler = new RemoteMode.NullObjRemoteModeHandler();
                    AlarmHandler = new Alarms.NullObjAlarmHandler();

                    Vehicle.MainFlowConfig.WatchLowPowerSleepTimeMs = 60 * 1000;
                }
                else
                {
                    RobotHandler = new Robot.UtmcRobotHandler();
                    BatteryHandler = new Battery.UmtcBatteryHandler();
                    MoveHandler = new Move.UmtcMoveHandler();
                    RemoteModeHandler = new RemoteMode.UmtcRemoteModeHandler();
                    AlarmHandler = new Alarms.UmtcAlarmHandler();
                }

                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(true, "控制層"));
            }
            catch (Exception ex)
            {
                isIniOk = false;
                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(false, "控制層"));
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        private void LocalPackage_OnLocalPackageComponentIntialDoneEvent(object sender, InitialEventArgs e)
        {
            OnComponentIntialDoneEvent?.Invoke(this, e);
        }

        private void VehicleInitial()
        {
            try
            {
                IsFirstAhGet = Vehicle.MainFlowConfig.IsSimulation;

                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(true, "台車"));
            }
            catch (Exception ex)
            {
                isIniOk = false;
                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(false, "台車"));
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        private void EventInitial()
        {
            try
            {
                //來自middleAgent的NewTransCmds訊息, Send to MainFlow(this)'mapHandler
                agvcConnector.OnInstallTransferCommandEvent += AgvcConnector_OnInstallTransferCommandEvent;
                agvcConnector.OnAvoideRequestEvent += AgvcConnector_OnAvoideRequestEvent;
                agvcConnector.OnRenameCassetteIdEvent += AgvcConnector_OnRenameCassetteIdEvent;

                agvcConnector.OnSendRecvTimeoutEvent += AgvcConnector_OnSendRecvTimeoutEvent;
                agvcConnector.OnCstRenameEvent += AgvcConnector_OnCstRenameEvent;

                RemoteModeHandler.OnModeChangeEvent += RemoteModeHandler_OnModeChangeEvent;

                MoveHandler.OnUpdateAddressArrivalArgsEvent += MoveHandler_OnUpdatePositionArgsEvent;
                MoveHandler.OnOpPauseOrResumeEvent += MoveHandler_OnOpPauseOrResumeEvent;
                MoveHandler.OnSectionPassEvent += MoveHandler_OnSectionPassEvent;

                RobotHandler.OnRobotEndEvent += RobotHandler_OnRobotEndEvent;
                RobotHandler.OnUpdateCarrierSlotStatusEvent += RobotHandler_OnUpdateCarrierSlotStatusEvent;
                RobotHandler.OnUpdateRobotStatusEvent += RobotHandler_OnUpdateRobotStatusEvent;
                RobotHandler.OnRobotLoadCompleteEvent += OnRobotLoadCompleteEvent;//0407liu LULComplete修改
                RobotHandler.OnRobotUnloadCompleteEvent += OnRobotUnloadCompleteEvent;//0407liu LULComplete修改

                BatteryHandler.OnUpdateBatteryStatusEvent += BatteryHandler_OnUpdateBatteryStatusEvent;

                AlarmHandler.OnSetAlarmToAgvcEvent += AlarmHandler_OnSetAlarmToAgvcEvent;
                AlarmHandler.OnResetAlarmToAgvcEvent += AlarmHandler_OnResetAlarmToAgvcEvent;

                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(true, "事件"));
            }
            catch (Exception ex)
            {
                isIniOk = false;
                OnComponentIntialDoneEvent?.Invoke(this, new InitialEventArgs(false, "事件"));

                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        private void VehicleLocationInitialAndThreadsInitial()
        {
            MoveHandler.InitialPosition();

            StartVisitTransferSteps();
            StartWatchChargeStage();
            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"讀取到的電量為{Vehicle.BatteryLog.InitialSoc}");
        }

        #endregion

        #region Thd Visit TransferSteps

        public void StartVisitTransferSteps()
        {
            thdVisitTransferSteps = new Thread(VisitTransferStepsSwitchCase);
            thdVisitTransferSteps.IsBackground = true;
            thdVisitTransferSteps.Start();

            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"MainFlow : StartVisitTransferSteps");
        }

        public void PauseVisitTransferSteps()
        {
            IsVisitTransferStepPause = true;
            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"MainFlow : PauseVisitTransferSteps");
        }

        public void ResumeVisitTransferSteps()
        {
            IsVisitTransferStepPause = false;
            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"MainFlow : ResumeVisitTransferSteps");
        }

        private void VisitTransferStepsSwitchCase()
        {
            while (true)
            {
                try
                {
                    if (Vehicle.TransferCommand.IsStopAndClear)
                    {
                        ClearTransferTransferCommand();
                        Thread.Sleep(Vehicle.MainFlowConfig.VisitTransferStepsSleepTimeMs);
                        continue;
                    }
                    else if (IsVisitTransferStepPause)
                    {
                        Thread.Sleep(Vehicle.MainFlowConfig.VisitTransferStepsSleepTimeMs);
                        continue;
                    }

                    switch (Vehicle.TransferCommand.TransferStep)
                    {
                        case EnumTransferStep.Idle:
                            Idle();
                            break;
                        case EnumTransferStep.MoveToLoad:
                            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[命令.移動.準備.至取貨站] MoveToLoad.");
                            MoveToAddress(Vehicle.TransferCommand.LoadAddressId, EnumMoveToEndReference.Load);
                            break;
                        case EnumTransferStep.MoveToAddress:
                        case EnumTransferStep.MoveToUnload:
                            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[命令.移動.準備.至終站] MoveToUnload or MoveToAddress.");
                            MoveToAddress(Vehicle.TransferCommand.UnloadAddressId, EnumMoveToEndReference.Unload);
                            break;
                        case EnumTransferStep.MoveToAvoid:
                            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[命令.移動.準備.至避車站] MoveToAvoid.");
                            MoveToAddress(Vehicle.MovingGuide.ToAddressId, EnumMoveToEndReference.Avoid);
                            break;
                        case EnumTransferStep.MoveToAvoidWaitArrival:
                            if (Vehicle.MoveStatus.IsMoveEnd)
                            {
                                MoveToAddressEnd();
                            }
                            else if (Vehicle.MoveStatus.LastAddress.Id == Vehicle.MovingGuide.ToAddressId)
                            {
                                Vehicle.MovingGuide.IsAvoidMove = true;
                                Vehicle.MovingGuide.MoveComplete = EnumMoveComplete.Success;
                                MoveToAddressEnd();
                            }
                            break;
                        case EnumTransferStep.AvoidMoveComplete:
                            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[避車.到站.回報.完成] AvoidMoveComplete.");
                            AvoidMoveComplete();
                            break;
                        case EnumTransferStep.MoveToAddressWaitEnd:
                            if (Vehicle.MoveStatus.IsMoveEnd)
                            {
                                MoveToAddressEnd();
                            }
                            break;
                        case EnumTransferStep.LoadArrival:
                            LoadArrival();
                            break;
                        case EnumTransferStep.WaitLoadArrivalReply:
                            if (Vehicle.TransferCommand.IsLoadArrivalReply)
                            {
                                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[取貨.到站.回報.成功] AgvcConnector_LoadArrivalReply.");
                                Vehicle.TransferCommand.TransferStep = EnumTransferStep.Load;
                            }
                            break;
                        case EnumTransferStep.Load:
                            Load();
                            break;
                        case EnumTransferStep.LoadWaitEnd:
                            if (Vehicle.TransferCommand.IsRobotEnd)
                            {
                                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[取貨.動作.結束] LoadComplete.");
                                LoadComplete();
                            }
                            break;
                        case EnumTransferStep.WaitLoadCompleteReply:
                            if (Vehicle.TransferCommand.IsLoadCompleteReply)
                            {
                                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, "[取貨.完成.回報.成功] AgvcConnector_LoadCompleteReply.");
                                Vehicle.TransferCommand.TransferStep = EnumTransferStep.WaitCstIdReadReply;
                                agvcConnector.SendRecv_Cmd136_CstIdReadReport();
                            }
                            break;
                        case EnumTransferStep.WaitCstIdReadReply:
                            if (Vehicle.TransferCommand.IsCstIdReadReply)
                            {
                                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, "[取貨.貨號回報.成功] AgvcConnector_CstIdReadReply.");
                                LoadEnd();
                            }
                            break;
                        case EnumTransferStep.UnloadArrival:
                            UnloadArrival();
                            break;
                        case EnumTransferStep.WaitUnloadArrivalReply:
                            if (Vehicle.TransferCommand.IsUnloadArrivalReply)
                            {
                                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[放貨.到站.回報.成功] AgvcConnector_OnAgvcAcceptUnloadArrivalEvent.");
                                Vehicle.TransferCommand.TransferStep = EnumTransferStep.Unload;
                            }
                            break;
                        case EnumTransferStep.Unload:
                            Unload();
                            break;
                        case EnumTransferStep.UnloadWaitEnd:
                            if (Vehicle.TransferCommand.IsRobotEnd)
                            {
                                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[放貨.動作.結束] UnloadComplete.");
                                UnloadComplete();
                            }
                            break;
                        case EnumTransferStep.WaitUnloadCompleteReply:
                            if (Vehicle.TransferCommand.IsUnloadCompleteReply)
                            {
                                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, "[放貨.完成.回報.成功] AgvcConnector_UnloadCompleteReply.");

                                UnloadEnd();
                            }
                            break;
                        case EnumTransferStep.TransferComplete:
                            TransferCommandComplete();
                            break;
                        case EnumTransferStep.WaitOverrideToContinue:
                            break;
                        case EnumTransferStep.MoveFail:
                        case EnumTransferStep.RobotFail:
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
                }

                Thread.Sleep(Vehicle.MainFlowConfig.VisitTransferStepsSleepTimeMs);
            }
        }

        public void GetAllStatusReport()
        {
            RobotHandler.GetRobotAndCarrierSlotStatus();
            MoveHandler.GetAddressArrivalArgs();
            BatteryHandler.GetBatteryAndChargeStatus();

        }

        #region Move Step

        private void MoveToAddress(string endAddressId, EnumMoveToEndReference endReference)
        {
            try
            {
                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[命令.移動.準備] MoveToAddress.[{endAddressId}].[{endReference}]");

                Vehicle.TransferCommand.TransferStep = EnumTransferStep.MoveToAddressWaitEnd;
                //Vehicle.MovingGuide = new MovingGuide();

                if (endAddressId == Vehicle.MoveStatus.LastAddress.Id)
                {
                    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[原地到站] Same address end.");

                    Vehicle.MoveStatus.IsMoveEnd = false;
                    AddressArrivalArgs data = new AddressArrivalArgs();

                    INX.Model.VehicleLocation now = INX.Model.LocalData.Instance.Location;

                    if (now != null)
                    {
                        data.EnumMoveState = EnumMoveState.Idle;
                        data.EnumAddressArrival = EnumAddressArrival.EndArrival;
                        data.MapAddressID = now.LastAddress;
                        data.MapSectionID = now.NowSection;
                        data.VehicleDistanceSinceHead = now.DistanceFormSectionHead;
                    }

                    MoveHandler.MoveComplete(data);
                }
                else
                {
                    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[移動前.斷充] Move Stop Charge");
                    StopCharge();

                    agvcConnector.ReportSectionPass();
                    if (!Vehicle.BatteryStatus.IsCharging)
                    {
                        LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[退出.站點] Move Begin.");
                        Vehicle.MoveStatus.IsMoveEnd = false;
                        LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"IsMoveEnd Need False And Cur IsMoveEnd = {Vehicle.MoveStatus.IsMoveEnd}");

                        agvcConnector.ClearAllReserve();
                        LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[詢問.路線] AskGuideAddressesAndSections.");
                        PrepareToMove(endAddressId);
                        //switch (endReference)
                        //{
                        //    case EnumMoveToEndReference.Load:
                        //        PrepareToMove(Vehicle.TransferCommand.ToLoadSectionIds, Vehicle.TransferCommand.ToLoadAddressIds);
                        //        break;
                        //    case EnumMoveToEndReference.Unload:
                        //        PrepareToMove(Vehicle.TransferCommand.ToUnloadSectionIds, Vehicle.TransferCommand.ToUnloadAddressIds);
                        //        break;
                        //    case EnumMoveToEndReference.Avoid:
                        //        Vehicle.TransferCommand.TransferStep = EnumTransferStep.MoveToAvoidWaitArrival;
                        //        break;
                        //    default:
                        //        break;
                        //}
                        //Vehicle.MovingGuide.ToAddressId = endAddressId;
                    }
                    else
                    {
                        //AlarmHandler.SetAlarmFromAgvm(58);
                        //Thread.Sleep(3000);
                        if (endReference == EnumMoveToEndReference.Avoid)
                        {
                            Vehicle.TransferCommand.TransferStep = EnumTransferStep.MoveToAvoid;
                        }
                        else
                        {
                            switch (Vehicle.TransferCommand.EnrouteState)
                            {
                                case CommandState.LoadEnroute:
                                    Vehicle.TransferCommand.TransferStep = EnumTransferStep.MoveToLoad;
                                    break;
                                case CommandState.None:
                                case CommandState.UnloadEnroute:
                                    Vehicle.TransferCommand.TransferStep = EnumTransferStep.MoveToUnload;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        private void PrepareToMove(string endAddressId)
        {
            agvcConnector.GuideSectionAndAddressRequest(endAddressId);
        }

        public void PrepareToMove()
        {
            try
            {
                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[設定.路線] Setup MovingGuide.");
                MovingGuide movingGuide = new MovingGuide(Vehicle.MovingGuide);
                movingGuide.MovingSections = new List<MapSection>();
                ////liu 改道一半
                //double distance = 0;
                //if (Vehicle.MoveStatus.LastSection.Id != movingGuide.GuideSectionIds[0].Trim())
                //{
                //    //當前路段沒給
                //    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[設定.路線.DebugTemp] 當前路段沒給.");
                //    movingGuide.GuideSectionIds.Insert(0, Vehicle.MoveStatus.LastSection.Id);
                //    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[設定.路線.DebugTemp] ." + Vehicle.MoveStatus.LastSection.Id);
                //    if (Vehicle.MapInfo.sectionMap[movingGuide.GuideSectionIds[0].Trim()].HeadAddress.Id == movingGuide.GuideAddressIds[0].Trim())
                //    {
                //        movingGuide.GuideAddressIds.Insert(0, Vehicle.MapInfo.sectionMap[Vehicle.MoveStatus.LastSection.Id].TailAddress.Id);
                //        LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[設定.路線.DebugTemp] Add Tail." + Vehicle.MapInfo.sectionMap[Vehicle.MoveStatus.LastSection.Id].TailAddress.Id);
                //    }
                //    else if (Vehicle.MapInfo.sectionMap[movingGuide.GuideSectionIds[0].Trim()].TailAddress.Id == movingGuide.GuideAddressIds[0].Trim())
                //    {
                //        movingGuide.GuideAddressIds.Insert(0, Vehicle.MapInfo.sectionMap[Vehicle.MoveStatus.LastSection.Id].HeadAddress.Id);
                //        LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[設定.路線.DebugTemp] Add Head." + Vehicle.MapInfo.sectionMap[Vehicle.MoveStatus.LastSection.Id].HeadAddress.Id);
                //    }
                //    else
                //    {
                //            throw new Exception($"PreparetoMove Add Address Fail!!");
                //    }
                //}
                //else
                //{
                //    //當前路段有給
                //    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[設定.路線.DebugTemp] 當前路段有給.");
                //}

                //拆兩種做法---一種S+A+S+A 一種 A+S+A
                if (movingGuide.GuideSectionIds.Count + 1 == movingGuide.GuideAddressIds.Count)
                {
                    for (int i = 0; i < Vehicle.MovingGuide.GuideSectionIds.Count; i++)
                    {
                        MapSection mapSection = new MapSection();
                        string sectionId = movingGuide.GuideSectionIds[i].Trim();
                        string addressId = movingGuide.GuideAddressIds[i + 1].Trim();
                        if (!Vehicle.MapInfo.sectionMap.ContainsKey(sectionId))
                        {
                            throw new Exception($"Map info has no this section ID.[{sectionId}]");
                        }
                        else if (!Vehicle.MapInfo.addressMap.ContainsKey(addressId))
                        {
                            throw new Exception($"Map info has no this address ID.[{addressId}]");
                        }

                        mapSection = Vehicle.MapInfo.sectionMap[sectionId];
                        mapSection.CmdDirection = addressId == mapSection.TailAddress.Id ? EnumCommandDirection.Forward : EnumCommandDirection.Backward;
                        movingGuide.MovingSections.Add(mapSection);
                    }
                }
                else
                {
                    throw new Exception($"PreparetoMove Add Address Fail!!");
                }
                isFirstReserveStop = 10;
                Vehicle.MovingGuide = movingGuide;
                MoveHandler.SetMovingGuide(movingGuide);
                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[設定.路線.成功] Setup MovingGuide OK.");
            }
            catch (Exception ex)
            {
                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[設定.路線.失敗] Setup MovingGuide Fail.{ex.Message}");
                Vehicle.MovingGuide.MovingSections = new List<MapSection>();
                AlarmHandler.SetAlarmFromAgvm(EnumMiddlerAlarmCode.MapCheckFail);
                StopClearAndReset();
            }
        }

        private void MoveToAddressEnd()
        {
            try
            {
                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[移動.結束] MoveToAddressEnd IsMoveEnd = {Vehicle.MoveStatus.IsMoveEnd}");

                agvcConnector.ClearAllReserve();

                #region Not EnumMoveComplete.Success

                if (Vehicle.MovingGuide.MoveComplete == EnumMoveComplete.Fail)
                {
                    Vehicle.MoveStatus.IsMoveEnd = false;
                    if (Vehicle.MovingGuide.IsAvoidMove)
                    {
                        agvcConnector.AvoidFail();
                        LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[避車.移動.失敗] : Avoid Fail. ");
                        Vehicle.MovingGuide.IsAvoidMove = false;
                    }
                    else if (Vehicle.MovingGuide.IsOverrideMove)
                    {
                        LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[變更路徑.移動失敗] :  Override Move Fail. ");
                        Vehicle.MovingGuide.IsOverrideMove = false;
                    }
                    else
                    {
                        LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[移動.失敗] : Move Fail. ");
                    }

                    //AlarmHandler.SetAlarmFromAgvm(6);
                    agvcConnector.StatusChangeReport();

                    StopClearAndReset();

                    return;
                }

                #endregion

                #region EnumMoveComplete.Success

                if (Vehicle.MovingGuide.MoveComplete == EnumMoveComplete.Success)
                {
                    if (Vehicle.MovingGuide.IsAvoidMove)
                    {
                        LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[避車.到站] AvoidMoveComplete.");

                        Vehicle.MovingGuide.IsAvoidMove = false;
                        Vehicle.TransferCommand.TransferStep = EnumTransferStep.AvoidMoveComplete;
                        Vehicle.MovingGuide.IsAvoidComplete = true;
                        agvcConnector.AvoidComplete();
                    }
                    else
                    {
                        LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[移動.二次定位.到站] : Move End Ok.");
                        if (!Vehicle.BatteryStatus.IsCharging) ArrivalStartCharge(Vehicle.MoveStatus.LastAddress);
                        Vehicle.MovingGuide = new MovingGuide();
                        agvcConnector.StatusChangeReport();

                        switch (Vehicle.TransferCommand.EnrouteState)
                        {
                            case CommandState.None:
                                agvcConnector.MoveArrival();
                                Vehicle.TransferCommand.TransferStep = EnumTransferStep.TransferComplete;
                                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"Move Arrival. [AddressId = {Vehicle.MoveStatus.LastAddress.Id}]");
                                break;
                            case CommandState.LoadEnroute:
                                Vehicle.TransferCommand.TransferStep = EnumTransferStep.LoadArrival;
                                break;
                            case CommandState.UnloadEnroute:
                                Vehicle.TransferCommand.TransferStep = EnumTransferStep.UnloadArrival;
                                break;
                            default:
                                break;
                        }
                    }

                }

                #endregion
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        private void AvoidMoveComplete()
        {
            try
            {//LIU
                switch (Vehicle.TransferCommand.EnrouteState)
                {
                    case CommandState.None:
                        if (Vehicle.MoveStatus.LastAddress.Id == Vehicle.TransferCommand.UnloadAddressId)
                        {
                            agvcConnector.MoveArrival();
                            Vehicle.TransferCommand.TransferStep = EnumTransferStep.MoveToAddressWaitEnd;
                            Vehicle.MovingGuide.ToAddressId = Vehicle.TransferCommand.UnloadAddressId;
                        }
                        else
                        {
                            //Vehicle.TransferCommand.TransferStep = EnumTransferStep.WaitOverrideToContinue;

                            agvcConnector.MoveArrival();
                            Vehicle.TransferCommand.TransferStep = EnumTransferStep.MoveToAddress;
                            Vehicle.MovingGuide.ToAddressId = Vehicle.TransferCommand.UnloadAddressId;
                        }
                        break;
                    case CommandState.LoadEnroute:
                        if (Vehicle.MoveStatus.LastAddress.Id == Vehicle.TransferCommand.LoadAddressId)
                        {
                            Vehicle.TransferCommand.TransferStep = EnumTransferStep.MoveToAddressWaitEnd;
                            Vehicle.MovingGuide.ToAddressId = Vehicle.TransferCommand.LoadAddressId;
                        }
                        else
                        {
                            //Vehicle.TransferCommand.TransferStep = EnumTransferStep.WaitOverrideToContinue;
                            Vehicle.TransferCommand.TransferStep = EnumTransferStep.MoveToAddress;
                            Vehicle.MovingGuide.ToAddressId = Vehicle.TransferCommand.LoadAddressId;
                        }
                        break;
                    case CommandState.UnloadEnroute:
                        if (Vehicle.MoveStatus.LastAddress.Id == Vehicle.TransferCommand.UnloadAddressId)
                        {
                            Vehicle.TransferCommand.TransferStep = EnumTransferStep.MoveToAddressWaitEnd;
                            Vehicle.MovingGuide.ToAddressId = Vehicle.TransferCommand.UnloadAddressId;
                        }
                        else
                        {
                            //Vehicle.TransferCommand.TransferStep = EnumTransferStep.WaitOverrideToContinue;
                            Vehicle.TransferCommand.TransferStep = EnumTransferStep.MoveToAddress;
                            Vehicle.MovingGuide.ToAddressId = Vehicle.TransferCommand.UnloadAddressId;
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        public bool CanVehMove()
        {
            try
            {
                if (Vehicle.BatteryStatus.IsCharging) //dabid
                {
                    StopCharge();
                }
            }
            catch
            {

            }
            return Vehicle.RobotStatus.IsHome && !Vehicle.BatteryStatus.IsCharging;

        }

        //public void SetPositionArgs(AddressArrivalArgs positionArgs)
        //{
        //    MoveHandler_OnUpdatePositionArgsEvent(this, positionArgs);
        //}

        public void AgvcConnector_GetReserveOkUpdateMoveControlNextPartMovePosition(MapSection mapSection, EnumIsExecute keepOrGo)
        {
            try
            {
                int sectionIndex = Vehicle.MovingGuide.GuideSectionIds.FindIndex(x => x == mapSection.Id);
                MapAddress address = Vehicle.MapInfo.addressMap[Vehicle.MovingGuide.GuideAddressIds[sectionIndex + 1]];

                MoveHandler.ReserveOkPartMove(mapSection);

                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"Send to MoveControl get reserve {mapSection.Id} ok , next end point [{address.Id}]({Convert.ToInt32(address.Position.X)},{Convert.ToInt32(address.Position.Y)}).");
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        private void MoveHandler_OnUpdatePositionArgsEvent(object sender, AddressArrivalArgs positionArgs)
        {
            try
            {
                if (positionArgs == null) return;

                MoveStatus moveStatus = GetMoveStatusFrom(positionArgs);
                //liu

                if (isFirstReserveStop > 0)
                {
                    if (moveStatus.EnumMoveState == EnumMoveState.ReserveStop)
                    {
                        moveStatus.EnumMoveState = EnumMoveState.Busy;
                        isFirstReserveStop--;
                    }
                }
                
                moveStatus.IsMoveEnd = Vehicle.MoveStatus.IsMoveEnd;
                Vehicle.MoveStatus = moveStatus;

                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[車輛.經過.站點] UpdatePositionArgs. [{positionArgs.EnumAddressArrival}][({(int)positionArgs.MapPosition.X},{(int)positionArgs.MapPosition.Y})][Address={positionArgs.MapAddressID}][Section={positionArgs.MapSectionID}][MoveStatus]={Vehicle.MoveStatus.EnumMoveState}" + isFirstReserveStop);
                if (moveStatus.EnumMoveState == EnumMoveState.ReserveStop)
                {
                    Vehicle.MovingGuide.ReserveStop = VhStopSingle.On;
                }
                else if (moveStatus.EnumMoveState == EnumMoveState.Busy ||
                    moveStatus.EnumMoveState == EnumMoveState.Idle)
                {
                    Vehicle.MovingGuide.ReserveStop = VhStopSingle.Off;
                }

                agvcConnector.ReportSectionPass();

                switch (positionArgs.EnumAddressArrival)
                {
                    case EnumAddressArrival.Fail:
                        Vehicle.MovingGuide.MoveComplete = EnumMoveComplete.Fail;
                        Vehicle.MoveStatus.IsMoveEnd = true;
                        break;
                    case EnumAddressArrival.Arrival:
                        break;
                    case EnumAddressArrival.EndArrival:
                        Vehicle.MovingGuide.MoveComplete = EnumMoveComplete.Success;
                        Vehicle.MoveStatus.IsMoveEnd = true;
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
                AlarmHandler.SetAlarmFromAgvm(EnumMiddlerAlarmCode.PositionLostWithCmd);
            }
        }

        private MoveStatus GetMoveStatusFrom(AddressArrivalArgs positionArgs)
        {
            MoveStatus moveStatus = new MoveStatus();
            moveStatus.EnumMoveState = positionArgs.EnumMoveState;
            moveStatus.LastMapPosition = positionArgs.MapPosition;
            if (Vehicle.MapInfo.addressMap.ContainsKey(positionArgs.MapAddressID))
            {
                moveStatus.LastAddress = Vehicle.MapInfo.addressMap[positionArgs.MapAddressID];
            }
            else
            {
                moveStatus.LastAddress = new MapAddress() { Id = positionArgs.MapAddressID };
            }
            if (Vehicle.MapInfo.sectionMap.ContainsKey(positionArgs.MapSectionID))
            {
                moveStatus.LastSection = Vehicle.MapInfo.sectionMap[positionArgs.MapSectionID];
            }
            else
            {
                moveStatus.LastSection = new MapSection() { Id = positionArgs.MapSectionID };
            }
            moveStatus.LastSection.VehicleDistanceSinceHead = positionArgs.VehicleDistanceSinceHead;
            moveStatus.HeadAngle = positionArgs.HeadAngle;
            moveStatus.MovingDirection = positionArgs.MovingDirection;
            moveStatus.Speed = positionArgs.Speed;
            moveStatus.InAddress = positionArgs.InAddress;
            return moveStatus;
        }

        private void UpdateMovePassSections(string id)
        {
            int getReserveOkSectionIndex = 0;
            try
            {
                var getReserveOkSections = agvcConnector.GetReserveOkSections();
                getReserveOkSectionIndex = getReserveOkSections.FindIndex(x => x.Id == id);
                if (getReserveOkSectionIndex < 0) return;
                for (int i = 0; i < getReserveOkSectionIndex; i++)
                {
                    //Remove passed section in ReserveOkSection
                    agvcConnector.DequeueGotReserveOkSections();
                }
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"FAIL [SecId={id}][Index={getReserveOkSectionIndex}]");
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }

        }

        //private void MoveHandler_OnUpdateMoveStatusEvent(object sender, MoveStatus moveStatus)
        //{
        //    Vehicle.MoveStatus = moveStatus;
        //    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[移動.狀態.改變] UpdateMoveStatus:[{Vehicle.MoveStatus.EnumMoveState}][Address={Vehicle.MoveStatus.LastAddress.Id}][Section={Vehicle.MoveStatus.LastSection.Id}]");
        //}

        private object _sectionPassLocker = new object();

        private void MoveHandler_OnSectionPassEvent(object sender, string e)
        {
            lock (_sectionPassLocker)
            {
                var reserveOkSections = agvcConnector.queReserveOkSections.ToList();
                int findIndex = -1;
                for (int i = 0; i < reserveOkSections.Count; i++)
                {
                    var targetSection = reserveOkSections[i];
                    if (targetSection.CmdDirection == EnumCommandDirection.Backward)
                    {
                        if (targetSection.HeadAddress.Id == e.Trim())
                        {
                            findIndex = i;
                            break;
                        }
                    }
                    else
                    {
                        if (targetSection.TailAddress.Id == e.Trim())
                        {
                            findIndex = i;
                            break;
                        }
                    }
                }
                for (int i = 0; i < findIndex; i++)
                {
                    agvcConnector.queReserveOkSections.TryDequeue(out MapSection section);
                }
            }
        }

        private void MoveHandler_OnOpPauseOrResumeEvent(object sender, bool e)
        {
            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[遙控器.狀態.改變] MoveHandler_OnOpPauseOrResumeEvent [IsPause={e}].");

            if (e)
            {
                Vehicle.OpPauseStatus = VhStopSingle.On;
                agvcConnector.StatusChangeReport();
            }
            else
            {
                Vehicle.OpPauseStatus = VhStopSingle.Off;
                Vehicle.ResetPauseFlags();
                ResumeMiddler();
            }
        }

        private void CheckPositionUnchangeTimeout(AddressArrivalArgs positionArgs)
        {
            if (!Vehicle.MoveStatus.IsMoveEnd)
            {
                if (LastIdlePosition.Position.MyDistance(positionArgs.MapPosition) <= Vehicle.MainFlowConfig.IdleReportRangeMm)
                {
                    if ((DateTime.Now - LastIdlePosition.TimeStamp).TotalMilliseconds >= Vehicle.MainFlowConfig.IdleReportIntervalMs)
                    {
                        UpdateLastIdlePositionAndTimeStamp(positionArgs);
                        AlarmHandler.SetAlarmFromAgvm(EnumMiddlerAlarmCode.IdleTimeout);
                    }
                }
                else
                {
                    UpdateLastIdlePositionAndTimeStamp(positionArgs);
                }
            }
            else
            {
                LastIdlePosition.TimeStamp = DateTime.Now;
            }
        }

        private void UpdateLastIdlePositionAndTimeStamp(AddressArrivalArgs positionArgs)
        {
            LastIdlePosition lastIdlePosition = new LastIdlePosition();
            lastIdlePosition.Position = positionArgs.MapPosition;
            lastIdlePosition.TimeStamp = DateTime.Now;
            LastIdlePosition = lastIdlePosition;
        }

        #endregion

        #region Robot Step

        private void LoadArrival()
        {
            try
            {
                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[取貨.到站.回報] Load Arrival. [AddressId = {Vehicle.MoveStatus.LastAddress.Id}]");

                Vehicle.TransferCommand.TransferStep = EnumTransferStep.WaitLoadArrivalReply;
                agvcConnector.ReportLoadArrival();
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }
        private void Load()
        {
            try
            {
                if (Vehicle.TransferCommand.IsStopAndClear) return;

                Vehicle.TransferCommand.IsRobotEnd = false;
                Vehicle.TransferCommand.TransferStep = EnumTransferStep.LoadWaitEnd;

                if (Vehicle.CarrierSlotLeft.EnumCarrierSlotState == EnumCarrierSlotState.Empty && Vehicle.MainFlowConfig.SlotDisable != EnumSlotSelect.Left)
                {
                    Vehicle.TransferCommand.SlotNumber = EnumSlotNumber.L;
                    Vehicle.LeftReadResult = BCRReadResult.BcrReadFail;
                }
                else if (Vehicle.CarrierSlotRight.EnumCarrierSlotState == EnumCarrierSlotState.Empty && Vehicle.MainFlowConfig.SlotDisable != EnumSlotSelect.Right)
                {
                    Vehicle.TransferCommand.SlotNumber = EnumSlotNumber.R;
                    Vehicle.RightReadResult = BCRReadResult.BcrReadFail;
                }
                else
                {
                    //VehicleSlotFullFindFitUnloadCommand();
                    //return;
                    Vehicle.TransferCommand.IsStopAndClear = true;
                    throw new Exception("No slot to Load.");
                }

                LoadCmdInfo loadCmd = new LoadCmdInfo(Vehicle.TransferCommand);
                if (Vehicle.MoveStatus.LastAddress.Id != Vehicle.TransferCommand.LoadAddressId)
                {
                    Vehicle.TransferCommand.TransferStep = EnumTransferStep.MoveToLoad;
                    return;
                }

                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[執行.取貨] Loading, [Load Adr={loadCmd.PortAddressId}]");
                agvcConnector.Loading();

                RobotHandler.DoRobotCommandFor(loadCmd);
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }
        private void LoadComplete()
        {
            try
            {
                Vehicle.TransferCommand.TransferStep = EnumTransferStep.WaitLoadCompleteReply;
                ConfirmBcrReadResultInLoad(Vehicle.TransferCommand.SlotNumber);
                Vehicle.TransferCommand.EnrouteState = CommandState.UnloadEnroute;
                //agvcConnector.LoadComplete(); //0407liu LULComplete修改
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        private void OnRobotLoadCompleteEvent(object sender, object e)//0407liu LULComplete修改
        {
            agvcConnector.LoadComplete();
        }

        private void OnRobotUnloadCompleteEvent(object sender, object e)//0407liu LULComplete修改
        {
            agvcConnector.UnloadComplete();
        }

        private void ConfirmBcrReadResultInLoad(EnumSlotNumber slotNumber)
        {
            try
            {
                RobotHandler.GetRobotAndCarrierSlotStatus();
                CarrierSlotStatus slotStatus = Vehicle.GetCarrierSlotStatusFrom(slotNumber);

                if (Vehicle.MainFlowConfig.BcrByPass)
                {
                    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[取貨.讀取.關閉] BcrByPass.");

                    switch (slotStatus.EnumCarrierSlotState)
                    {
                        case EnumCarrierSlotState.Empty:
                            {
                                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[取貨.讀取.失敗] BcrByPass, slot is empty.");

                                slotStatus.CarrierId = "";
                                slotStatus.EnumCarrierSlotState = EnumCarrierSlotState.Empty;
                                if (slotNumber == EnumSlotNumber.L)
                                {
                                    Vehicle.CarrierSlotLeft = slotStatus;
                                    Vehicle.LeftReadResult = BCRReadResult.BcrReadFail;
                                }
                                else
                                {
                                    Vehicle.CarrierSlotRight = slotStatus;
                                    Vehicle.RightReadResult = BCRReadResult.BcrReadFail;

                                }
                            }
                            break;
                        case EnumCarrierSlotState.ReadFail:
                        case EnumCarrierSlotState.Loading:
                            {
                                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[取貨.讀取.成功] BcrByPass, loading is true.");
                                slotStatus.CarrierId = Vehicle.TransferCommand.CassetteId;
                                slotStatus.EnumCarrierSlotState = EnumCarrierSlotState.Loading;

                                if (slotNumber == EnumSlotNumber.L)
                                {
                                    Vehicle.CarrierSlotLeft = slotStatus;
                                    Vehicle.LeftReadResult = BCRReadResult.BcrNormal;
                                }
                                else
                                {
                                    Vehicle.CarrierSlotRight = slotStatus;
                                    Vehicle.RightReadResult = BCRReadResult.BcrNormal;
                                }
                            }
                            break;
                        case EnumCarrierSlotState.PositionError:
                            {
                                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[取貨.讀取.凸片] CST Position Error.");

                                slotStatus.CarrierId = "PositionError";
                                slotStatus.EnumCarrierSlotState = EnumCarrierSlotState.PositionError;

                                if (slotNumber == EnumSlotNumber.L)
                                {
                                    Vehicle.CarrierSlotLeft = slotStatus;
                                    Vehicle.LeftReadResult = BCRReadResult.BcrReadFail;
                                }
                                else
                                {
                                    Vehicle.CarrierSlotRight = slotStatus;
                                    Vehicle.RightReadResult = BCRReadResult.BcrReadFail;
                                }

                                AlarmHandler.SetAlarmFromAgvm(EnumMiddlerAlarmCode.CstPositionError);
                                return;
                            }
                        default:
                            break;
                    }
                }
                else
                {
                    switch (slotStatus.EnumCarrierSlotState)
                    {
                        case EnumCarrierSlotState.Empty:
                            {
                                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[取貨.讀取.失敗] CST ID is empty.");

                                slotStatus.CarrierId = "";
                                slotStatus.EnumCarrierSlotState = EnumCarrierSlotState.Empty;
                                if (slotNumber == EnumSlotNumber.L)
                                {
                                    Vehicle.CarrierSlotLeft = slotStatus;
                                    Vehicle.LeftReadResult = BCRReadResult.BcrReadFail;
                                }
                                else
                                {
                                    Vehicle.CarrierSlotRight = slotStatus;
                                    Vehicle.RightReadResult = BCRReadResult.BcrReadFail;
                                }

                                AlarmHandler.SetAlarmFromAgvm(EnumMiddlerAlarmCode.CstPositionError);
                            }
                            break;
                        case EnumCarrierSlotState.Loading:
                            if (Vehicle.TransferCommand.CassetteId == slotStatus.CarrierId.Trim())
                            {
                                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[取貨.讀取.成功] CST ID = [{slotStatus.CarrierId.Trim()}] read ok.");

                                if (slotNumber == EnumSlotNumber.L)
                                {
                                    Vehicle.LeftReadResult = BCRReadResult.BcrNormal;
                                }
                                else
                                {
                                    Vehicle.RightReadResult = BCRReadResult.BcrNormal;
                                }
                            }
                            else
                            {
                                switch (Vehicle.TransferCommand.AgvcTransCommandType) //liu 0416 scan
                                {
                                    case EnumAgvcTransCommandType.Scan:
                                        LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[取貨.讀取.失敗] CST ID = [{slotStatus.CarrierId.Trim()}] read ok.");

                                        if (slotNumber == EnumSlotNumber.L)
                                        {
                                            Vehicle.LeftReadResult = BCRReadResult.BcrNormal;
                                        }
                                        else
                                        {
                                            Vehicle.RightReadResult = BCRReadResult.BcrNormal;
                                        }
                                        break;
                                    default:
                                        LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[取貨.讀取.失敗] Read CST ID = [{slotStatus.CarrierId}], unmatched command CST ID = [{Vehicle.TransferCommand.CassetteId }]");

                                        if (slotNumber == EnumSlotNumber.L)
                                        {
                                            Vehicle.LeftReadResult = BCRReadResult.BcrMisMatch;
                                        }
                                        else
                                        {
                                            Vehicle.RightReadResult = BCRReadResult.BcrMisMatch;
                                        }

                                        //AlarmHandler.SetAlarmFromAgvm(EnumMiddlerAlarmCode.CarrierIdMisMatch);
                                        break;
                                }
                            }
                            break;
                        case EnumCarrierSlotState.ReadFail:
                            {
                                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, "[取貨.讀取.失敗] ReadFail.");

                                slotStatus.CarrierId = "ReadFail";
                                slotStatus.EnumCarrierSlotState = EnumCarrierSlotState.ReadFail;

                                if (slotNumber == EnumSlotNumber.L)
                                {
                                    Vehicle.CarrierSlotLeft = slotStatus;
                                    Vehicle.LeftReadResult = BCRReadResult.BcrReadFail;
                                }
                                else
                                {
                                    Vehicle.CarrierSlotRight = slotStatus;
                                    Vehicle.RightReadResult = BCRReadResult.BcrReadFail;
                                }
                                //AlarmHandler.SetAlarmFromAgvm(EnumMiddlerAlarmCode.CarrierIdReadFail);
                            }
                            break;
                        case EnumCarrierSlotState.PositionError:
                            {
                                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[取貨.讀取.凸片] CST Position Error.");

                                slotStatus.CarrierId = "PositionError";
                                slotStatus.EnumCarrierSlotState = EnumCarrierSlotState.PositionError;

                                if (slotNumber == EnumSlotNumber.L)
                                {
                                    Vehicle.CarrierSlotLeft = slotStatus;
                                    Vehicle.LeftReadResult = BCRReadResult.BcrReadFail;
                                }
                                else
                                {
                                    Vehicle.CarrierSlotRight = slotStatus;
                                    Vehicle.RightReadResult = BCRReadResult.BcrReadFail;
                                }

                                AlarmHandler.SetAlarmFromAgvm(EnumMiddlerAlarmCode.CstPositionError);
                                return;
                            }
                        default:
                            break;
                    }
                }

            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }
        private void LoadEnd()
        {
            try
            {
                switch (Vehicle.TransferCommand.AgvcTransCommandType) //liu 0416 scan
                {
                    case EnumAgvcTransCommandType.Load:
                    Vehicle.TransferCommand.TransferStep = EnumTransferStep.TransferComplete;
                        break;
                    case EnumAgvcTransCommandType.Scan:
                        Vehicle.TransferCommand.TransferStep = EnumTransferStep.UnloadArrival;
                        Vehicle.TransferCommand.EnrouteState = CommandState.UnloadEnroute;
                        break;
                    default:
                    Vehicle.TransferCommand.TransferStep = EnumTransferStep.MoveToUnload;
                    Vehicle.TransferCommand.EnrouteState = CommandState.UnloadEnroute;

                    if (Vehicle.MainFlowConfig.DualCommandOptimize) // liu0405 加回來
                    {
                        LoadEndOptimize();
                    }
                        break;
                }
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }
        public void LoadEndOptimize()
        {
            try
            {
                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[取貨完成.命令選擇] Load End Optimize");

                var transferCommands = Vehicle.MapTransferCommands.Values.ToArray();

                foreach (var transferCommand in transferCommands)
                {
                    if (transferCommand.IsStopAndClear)
                    {
                        Vehicle.TransferCommand = transferCommand;

                        LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[取貨完成.命令切換.中止] Load End Stop And Clear.[{Vehicle.TransferCommand.CommandId}]");

                        return;
                    }
                }

                if (Vehicle.MapTransferCommands.Count > 1)
                {
                    bool isEqLoad = string.IsNullOrEmpty(Vehicle.MapInfo.addressMap[Vehicle.TransferCommand.LoadAddressId].AgvStationId);

                    var minDis = DistanceFromLastPosition(Vehicle.TransferCommand.UnloadAddressId);

                    bool foundNextCommand = false;

                    foreach (var transferCommand in transferCommands)
                    {
                        string targetAddressId = "";
                        if (transferCommand.EnrouteState == CommandState.LoadEnroute)
                        {
                            transferCommand.TransferStep = EnumTransferStep.MoveToLoad;
                            targetAddressId = transferCommand.LoadAddressId;
                        }
                        else
                        {
                            transferCommand.TransferStep = EnumTransferStep.MoveToUnload;
                            targetAddressId = transferCommand.UnloadAddressId;
                        }

                        bool isTransferCommandToEq = string.IsNullOrEmpty(Vehicle.MapInfo.addressMap[targetAddressId].AgvStationId);

                        if (isTransferCommandToEq == isEqLoad)
                        {
                            if (targetAddressId == Vehicle.MoveStatus.LastAddress.Id)
                            {
                                Vehicle.TransferCommand = transferCommand;

                                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[取貨完成.命令切換] Load End Select Another Transfer Command.[{Vehicle.TransferCommand.CommandId}]");

                                foundNextCommand = true;

                                break;
                            }

                            var disTransferCommand = DistanceFromLastPosition(targetAddressId);

                            if (disTransferCommand < minDis)
                            {
                                minDis = disTransferCommand;
                                Vehicle.TransferCommand = transferCommand;

                                foundNextCommand = true;

                                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[取貨完成.命令切換] Load End Select Another Transfer Command.[{Vehicle.TransferCommand.CommandId}]");
                            }
                        }
                    }
                    if (!foundNextCommand)
                    {
                        foreach (var transferCommand in transferCommands)
                        {
                            string targetAddressId = "";
                            if (transferCommand.EnrouteState == CommandState.LoadEnroute)
                            {
                                transferCommand.TransferStep = EnumTransferStep.MoveToLoad;
                                targetAddressId = transferCommand.LoadAddressId;
                            }
                            else
                            {
                                transferCommand.TransferStep = EnumTransferStep.MoveToUnload;
                                targetAddressId = transferCommand.UnloadAddressId;
                            }

                            bool isTransferCommandToEq = string.IsNullOrEmpty(Vehicle.MapInfo.addressMap[targetAddressId].AgvStationId);

                            if (targetAddressId == Vehicle.MoveStatus.LastAddress.Id)
                            {
                                Vehicle.TransferCommand = transferCommand;

                                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[取貨完成.命令切換] Load End Select Another Transfer Command.[{Vehicle.TransferCommand.CommandId}]");

                                break;
                            }

                            var disTransferCommand = DistanceFromLastPosition(targetAddressId);

                            if (disTransferCommand < minDis)
                            {
                                minDis = disTransferCommand;
                                Vehicle.TransferCommand = transferCommand;

                                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[取貨完成.命令切換] Load End Select Another Transfer Command.[{Vehicle.TransferCommand.CommandId}]");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }
        private int DistanceFromLastPosition(string addressId)
        {
            var lastPosition = Vehicle.MoveStatus.LastMapPosition;
            var addressPosition = Vehicle.MapInfo.addressMap[addressId].Position;
            return (int)mapHandler.GetDistance(lastPosition, addressPosition);
        }
        private void UnloadArrival()
        {
            try
            {
                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"Unload Arrival. [AddressId = {Vehicle.MoveStatus.LastAddress.Id}]");

                Vehicle.TransferCommand.TransferStep = EnumTransferStep.WaitUnloadArrivalReply;
                agvcConnector.ReportUnloadArrival();
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }
        private void Unload()
        {
            try
            {
                if (Vehicle.TransferCommand.IsStopAndClear) return;

                Vehicle.TransferCommand.IsRobotEnd = false;
                Vehicle.TransferCommand.TransferStep = EnumTransferStep.UnloadWaitEnd;

                switch (Vehicle.TransferCommand.SlotNumber)
                {
                    case EnumSlotNumber.L:
                        if (Vehicle.CarrierSlotLeft.EnumCarrierSlotState == EnumCarrierSlotState.Empty)
                        {
                            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[放貨前.檢查.失敗] Pre Unload Check Fail. Slot is Empty.");

                            AlarmHandler.SetAlarmFromAgvm(EnumMiddlerAlarmCode.NoCstToUnload);
                            return;
                        }
                        break;
                    case EnumSlotNumber.R:
                        if (Vehicle.CarrierSlotRight.EnumCarrierSlotState == EnumCarrierSlotState.Empty)
                        {
                            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[放貨前.檢查.失敗] Pre Unload Check Fail. Slot is Empty.");

                            AlarmHandler.SetAlarmFromAgvm(EnumMiddlerAlarmCode.NoCstToUnload);
                            return;
                        }
                        break;
                }

                UnloadCmdInfo unloadCmd = new UnloadCmdInfo(Vehicle.TransferCommand);
                if (Vehicle.MoveStatus.LastAddress.Id != Vehicle.TransferCommand.UnloadAddressId)
                {
                    Vehicle.TransferCommand.TransferStep = EnumTransferStep.MoveToUnload;
                    return;
                }

                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[執行.放貨] : Unloading,[Unload Adr={unloadCmd.PortAddressId}]");
                agvcConnector.Unloading();

                RobotHandler.DoRobotCommandFor(unloadCmd);
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }
        private void UnloadComplete()
        {
            try
            {
                RobotHandler.GetRobotAndCarrierSlotStatus();
                CarrierSlotStatus carrierSlotStatus = Vehicle.GetCarrierSlotStatusFrom(Vehicle.TransferCommand.SlotNumber);

                switch (carrierSlotStatus.EnumCarrierSlotState)
                {
                    case EnumCarrierSlotState.Empty:
                        {
                            Vehicle.TransferCommand.EnrouteState = CommandState.None;
                            Vehicle.TransferCommand.TransferStep = EnumTransferStep.WaitUnloadCompleteReply;
                            carrierSlotStatus.CarrierId = "";
                            //agvcConnector.UnloadComplete(); //0407liu LULComplete修改
                        }
                        break;
                    case EnumCarrierSlotState.Loading:
                    case EnumCarrierSlotState.ReadFail:
                        {
                            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[放貨.失敗] :[{Vehicle.TransferCommand.SlotNumber}][{carrierSlotStatus.EnumCarrierSlotState}].");
                            Vehicle.TransferCommand.IsStopAndClear = true;
                        }
                        break;
                    case EnumCarrierSlotState.PositionError:
                        {
                            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[放貨.失敗.凸片] : PositionError.");
                            AlarmHandler.SetAlarmFromAgvm(EnumMiddlerAlarmCode.CstPositionError);
                            Vehicle.TransferCommand.EnrouteState = CommandState.None;
                            Vehicle.TransferCommand.TransferStep = EnumTransferStep.RobotFail;
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }
        private void UnloadEnd()
        {
            try
            {
                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[命令完成 {Vehicle.TransferCommand.AgvcTransCommandType}] TransferComplete.");

                Vehicle.TransferCommand.TransferStep = EnumTransferStep.TransferComplete;
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }
        private void AgvcConnector_OnSendRecvTimeoutEvent(object sender, EventArgs e)
        {
            // SetAlarmFromAgvm(38);
        }
        private void AgvcConnector_OnCstRenameEvent(object sender, EnumSlotNumber slotNumber)
        {
            try
            {
                CarrierSlotStatus slotStatus = Vehicle.GetCarrierSlotStatusFrom(slotNumber);
                RobotHandler.CarrierRenameTo(slotStatus);
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }
        private void RobotHandler_OnUpdateRobotStatusEvent(object sender, RobotStatus robotStatus)
        {
            try
            {
                Vehicle.RobotStatus = robotStatus;

                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[手臂.狀態.改變] UpdateRobotStatus:[{Vehicle.RobotStatus.EnumRobotState}][RobotHome={Vehicle.RobotStatus.IsHome}]");
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }
        private void RobotHandler_OnUpdateCarrierSlotStatusEvent(object sender, CarrierSlotStatus slotStatus)
        {
            try
            {
                if (slotStatus.ManualDeleteCST)
                {
                    slotStatus.CarrierId = "";
                    slotStatus.EnumCarrierSlotState = EnumCarrierSlotState.Empty;
                    switch (slotStatus.SlotNumber)
                    {
                        case EnumSlotNumber.L:
                            Vehicle.CarrierSlotLeft = slotStatus;
                            break;
                        case EnumSlotNumber.R:
                            Vehicle.CarrierSlotRight = slotStatus;
                            break;
                    }

                    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[手動.清空.儲位.狀態] OnUpdateCarrierSlotStatus: ManualDeleteCST[{slotStatus.SlotNumber}][{slotStatus.EnumCarrierSlotState}][ID={slotStatus.CarrierId}]");

                    agvcConnector.Send_Cmd136_CstRemove(slotStatus.SlotNumber);
                }
                else
                {
                    switch (slotStatus.SlotNumber)
                    {
                        case EnumSlotNumber.L:
                            Vehicle.CarrierSlotLeft = slotStatus;
                            break;
                        case EnumSlotNumber.R:
                            Vehicle.CarrierSlotRight = slotStatus;
                            break;
                    }

                    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[儲位.狀態.改變] OnUpdateCarrierSlotStatus:[{slotStatus.SlotNumber}][{slotStatus.EnumCarrierSlotState}][ID={slotStatus.CarrierId}]");
                }
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }

            agvcConnector.StatusChangeReport();
        }
        private void RobotHandler_OnRobotEndEvent(object sender, EnumRobotEndType robotEndType)
        {
            try
            {
                if (IsStopChargTimeoutInRobotStep)
                {
                    IsStopChargTimeoutInRobotStep = false;
                    //AlarmHandler.SetAlarmFromAgvm(14);
                }

                RobotHandler.GetRobotAndCarrierSlotStatus();

                switch (robotEndType)
                {
                    case EnumRobotEndType.Finished:
                        {
                            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, "[手臂.命令.完成] AseRobotContorl_OnRobotCommandFinishEvent");
                            Vehicle.TransferCommand.IsRobotEnd = true;
                        }
                        break;
                    case EnumRobotEndType.InterlockError:
                        {
                            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, "[手臂.交握.失敗] AseRobotControl_OnRobotInterlockErrorEvent");
                            AlarmHandler.ResetAllAlarmsFromAgvm();

                            Vehicle.TransferCommand.CompleteStatus = CompleteStatus.InterlockError;
                            Vehicle.TransferCommand.IsStopAndClear = true;
                        }
                        break;
                    case EnumRobotEndType.RobotError:
                        {
                            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, "[手臂.命令.失敗] AseRobotControl_OnRobotCommandErrorEvent");
                            Vehicle.TransferCommand.TransferStep = EnumTransferStep.RobotFail;
                        }
                        break;
                    case EnumRobotEndType.EmptyRetrival: //liu++
                        {
                            agvcConnector.EmptyRetrival();
                            switch (Vehicle.TransferCommand.AgvcTransCommandType) //liu 0416 scan
                            {
                                case EnumAgvcTransCommandType.Scan:
                                    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, "[手臂.命令.完成] EmptyRetrival,AseRobotContorl_OnRobotCommandFinishEvent");
                                    break;
                                default:
                            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, "[手臂.命令.失敗] EmptyRetrival");
                                    break;
                            }
                            AlarmHandler.ResetAllAlarmsFromAgvm();

                            Vehicle.TransferCommand.CompleteStatus = CompleteStatus.EmptyRetrieval;
                            Vehicle.TransferCommand.IsStopAndClear = true;
                        }
                        break;
                    case EnumRobotEndType.DoubleStorage: //liu++
                        {
                            agvcConnector.DoubleStorage();
                            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, "[手臂.交握.失敗] DoubleStorage");
                            AlarmHandler.ResetAllAlarmsFromAgvm();

                            Vehicle.TransferCommand.CompleteStatus = CompleteStatus.DoubleStorage;
                            Vehicle.TransferCommand.IsStopAndClear = true;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        #endregion

        #region Handle Transfer Command

        private void ClearTransferTransferCommand()
        {
            Vehicle.TransferCommand.IsStopAndClear = false;

            if (Vehicle.TransferCommand.TransferStep == EnumTransferStep.Idle) return;

            Vehicle.TransferCommand.TransferStep = EnumTransferStep.TransferComplete;
            Vehicle.MovingGuide = new MovingGuide();

            if (!Vehicle.TransferCommand.IsAbortByAgvc())
            {
                Vehicle.TransferCommand.CompleteStatus = CompleteStatus.VehicleAbort;
            }

            TransferCommandComplete();
        }

        private void Idle()
        {
            if (!Vehicle.MapTransferCommands.IsEmpty)
            {
                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[發呆.結束.選擇.命令] Idle Pick Command To Do.");

                Vehicle.TransferCommand = Vehicle.MapTransferCommands.Values.ToArray()[0];
            }
            else
            {

            }
        }

        private void TransferCommandComplete()
        {
            try
            {
                WaitingTransferCompleteEnd = true;

                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[命令.結束] TransferComplete. [CommandId = {Vehicle.TransferCommand.CommandId}][CompleteStatus = {Vehicle.TransferCommand.CompleteStatus}]");

                if (AlarmHandler.HasHappeningAlarm())
                {
                    AlarmHandler.ResetAllAlarmsFromAgvm();
                }

                Vehicle.MapTransferCommands.TryRemove(Vehicle.TransferCommand.CommandId, out AgvcTransferCommand transferCommand);
                agvcConnector.TransferComplete(transferCommand);

                Vehicle.TransferCommand = new AgvcTransferCommand();
                TransferCompleteOptimize();  // liu0405 加回來

                if (Vehicle.MapTransferCommands.IsEmpty)
                {
                    Vehicle.ResetPauseFlags();

                    agvcConnector.NoCommand();
                }
                else
                {
                    agvcConnector.StatusChangeReport();
                }

                WaitingTransferCompleteEnd = false;
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        private void TransferCompleteOptimize()
        {
            try
            {
                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[命令完成.命令選擇] TransferCompleteOptimize");

                bool isEqEnd = string.IsNullOrEmpty(Vehicle.MoveStatus.LastAddress.AgvStationId);

                if (!Vehicle.MapTransferCommands.IsEmpty)
                {
                    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[命令選擇.命令選取] Transfer Complete Select Another Transfer Command.");

                    if (Vehicle.MapTransferCommands.Count == 1)
                    {
                        Vehicle.TransferCommand = Vehicle.MapTransferCommands.Values.ToArray()[0];
                    }

                    if (Vehicle.MapTransferCommands.Count > 1)
                    {
                        var minDis = 999999;

                        var transferCommands = Vehicle.MapTransferCommands.Values.ToArray();
                        foreach (var transferCommand in transferCommands)
                        {
                            if (transferCommand.IsStopAndClear)
                            {
                                Vehicle.TransferCommand = transferCommand;

                                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[命令完成.命令選擇.命令切換] Transfer Complete Select Another Transfer Command.[{Vehicle.TransferCommand.CommandId}]");

                                return;
                            }
                        }

                        bool foundNextCommand = false;
                        foreach (var transferCommand in transferCommands)
                        {
                            string targetAddressId = "";

                            if (transferCommand.EnrouteState == CommandState.LoadEnroute)
                            {
                                transferCommand.TransferStep = EnumTransferStep.MoveToLoad;
                                targetAddressId = transferCommand.LoadAddressId;
                            }
                            else
                            {
                                transferCommand.TransferStep = EnumTransferStep.MoveToUnload;
                                targetAddressId = transferCommand.UnloadAddressId;
                            }
                            bool isTransferCommandToEq = string.IsNullOrEmpty(Vehicle.MapInfo.addressMap[targetAddressId].AgvStationId); ;

                            if (isTransferCommandToEq == isEqEnd)
                            {
                                if (targetAddressId == Vehicle.MoveStatus.LastAddress.Id)
                                {
                                    Vehicle.TransferCommand = transferCommand;
                                    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[命令完成.命令選擇.命令切換] Transfer Complete Select Another Transfer Command.[{Vehicle.TransferCommand.CommandId}]");
                                    foundNextCommand = true;
                                    break;
                                }

                                var disTransferCommand = DistanceFromLastPosition(targetAddressId);

                                if (disTransferCommand < minDis)
                                {
                                    minDis = disTransferCommand;
                                    Vehicle.TransferCommand = transferCommand;
                                    foundNextCommand = true;
                                    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[命令完成.命令選擇.命令切換] Transfer Complete Select Another Transfer Command.[{Vehicle.TransferCommand.CommandId}]");
                                }

                            }
                        }

                        if (!foundNextCommand)
                        {
                            foreach (var transferCommand in transferCommands)
                            {
                                string targetAddressId = "";

                                if (transferCommand.EnrouteState == CommandState.LoadEnroute)
                                {
                                    transferCommand.TransferStep = EnumTransferStep.MoveToLoad;
                                    targetAddressId = transferCommand.LoadAddressId;
                                }
                                else
                                {
                                    transferCommand.TransferStep = EnumTransferStep.MoveToUnload;
                                    targetAddressId = transferCommand.UnloadAddressId;
                                }
                                bool isTransferCommandToEq = string.IsNullOrEmpty(Vehicle.MapInfo.addressMap[targetAddressId].AgvStationId);

                                if (targetAddressId == Vehicle.MoveStatus.LastAddress.Id)
                                {
                                    Vehicle.TransferCommand = transferCommand;
                                    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[命令完成.命令選擇.命令切換] Transfer Complete Select Another Transfer Command.[{Vehicle.TransferCommand.CommandId}]");
                                    foundNextCommand = true;
                                    break;
                                }

                                var disTransferCommand = DistanceFromLastPosition(targetAddressId);

                                if (disTransferCommand < minDis)
                                {
                                    minDis = disTransferCommand;
                                    Vehicle.TransferCommand = transferCommand;
                                    foundNextCommand = true;
                                    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[命令完成.命令選擇.命令切換] Transfer Complete Select Another Transfer Command.[{Vehicle.TransferCommand.CommandId}]");
                                }
                            }
                        }
                    }
                }
                else
                {
                    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[命令完成.命令選擇.無命令] Transfer Complete into Idle.");
                    Vehicle.TransferCommand = new AgvcTransferCommand();
                }
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        public void StopClearAndReset()
        {
            PauseTransfer();

            try
            {
                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[停止.重置] Stop.Clear.Reset.");

                Thread.Sleep(500);
                agvcConnector.ClearAllReserve();

                if (Vehicle.CarrierSlotLeft.EnumCarrierSlotState == EnumCarrierSlotState.Loading || Vehicle.CarrierSlotRight.EnumCarrierSlotState == EnumCarrierSlotState.Loading)
                {
                    RobotHandler.GetRobotAndCarrierSlotStatus();
                }

                foreach (var transCmd in Vehicle.MapTransferCommands.Values.ToList())
                {
                    transCmd.IsStopAndClear = true;
                }

                Vehicle.TransferCommand.IsStopAndClear = true;

                StopVehicle();

                agvcConnector.StatusChangeReport();
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }

            ResumeTransfer();
        }

        private void PauseTransfer()
        {
            agvcConnector.PauseAskReserve();
            PauseVisitTransferSteps();
        }

        private void ResumeTransfer()
        {
            ResumeVisitTransferSteps();
            agvcConnector.ResumeAskReserve();
        }

        private static object installTransferCommandLocker = new object();

        public void AgvcConnector_OnInstallTransferCommandEvent(object sender, AgvcTransferCommand transferCommand)
        {
            lock (installTransferCommandLocker)
            {
                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[檢查搬送命令] Check Transfer Command [{transferCommand.CommandId}]");

                #region 檢查搬送Command
                try
                {
                    if (transferCommand.AgvcTransCommandType == EnumAgvcTransCommandType.Override)
                    {
                        AgvcConnector_OnOverrideCommandEvent(sender, transferCommand);
                        return;
                    }

                    switch (transferCommand.AgvcTransCommandType)
                    {
                        case EnumAgvcTransCommandType.Move:
                        case EnumAgvcTransCommandType.MoveToCharger:
                            CheckVehicleTransferCommandMapEmpty();
                            CheckMoveEndAddress(transferCommand.UnloadAddressId);
                            break;
                        case EnumAgvcTransCommandType.Load:
                            CheckTransferCommandMap(transferCommand);
                            CheckEnoughEmptySlotToLoad();
                            break;
                        case EnumAgvcTransCommandType.Unload:
                            transferCommand.SlotNumber = CheckUnloadCstId(transferCommand.CassetteId);
                            CheckExceutingCommand(transferCommand.CassetteId);
                            CheckVehicleTransferCommandMapEmpty();
                            break;
                        case EnumAgvcTransCommandType.LoadUnload:
                        case EnumAgvcTransCommandType.Scan: //liu 0416 scan
                            CheckTransferCommandMap(transferCommand);
                            CheckEnoughEmptySlotToLoad();
                            break;
                        case EnumAgvcTransCommandType.Else:
                            break;
                        default:
                            break;
                    }

                    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[檢查搬送命令.成功] Check Transfer Command Ok. [{transferCommand.CommandId}]");

                }
                catch (Exception ex)
                {
                    LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
                    agvcConnector.ReplyTransferCommand(transferCommand.CommandId, transferCommand.GetCommandActionType(), transferCommand.SeqNum, (int)EnumAgvcReplyCode.Reject, ex.Message);
                    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[檢查搬送命令.失敗] Check Transfer Command Fail. [{transferCommand.CommandId}] {ex.Message}");
                    return;
                }
                #endregion

                #region 搬送流程更新
                try
                {
                    var isMapTransferCommandsEmpty = Vehicle.MapTransferCommands.IsEmpty;
                    Vehicle.MapTransferCommands.TryAdd(transferCommand.CommandId, transferCommand);
                    agvcConnector.ReplyTransferCommand(transferCommand.CommandId, transferCommand.GetCommandActionType(), transferCommand.SeqNum, (int)EnumAgvcReplyCode.Accept, "");
                    if (isMapTransferCommandsEmpty) agvcConnector.Commanding();
                    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[初始化搬送命令.成功] Initial Transfer Command Ok. [{transferCommand.CommandId}]");
                }
                catch (Exception ex)
                {
                    agvcConnector.ReplyTransferCommand(transferCommand.CommandId, transferCommand.GetCommandActionType(), transferCommand.SeqNum, (int)EnumAgvcReplyCode.Reject, "");
                    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[初始化搬送命令 失敗] Initial Transfer Command Fail. [{transferCommand.CommandId}]");
                    LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
                }
                #endregion
            }
        }

        private void CheckVehicleTransferCommandMapEmpty()
        {
            if (WaitingTransferCompleteEnd)
            {
                throw new Exception("Vehicle is waiting last transfer commmand end.");
            }

            if (!Vehicle.MapTransferCommands.IsEmpty)
            {
                throw new Exception("Vehicle transfer command map is not empty.");
            }
        }

        private void CheckCstIdDuplicate(string cassetteId)
        {
            var agvcTransCmdBuffer = Vehicle.MapTransferCommands.Values.ToList();
            for (int i = 0; i < agvcTransCmdBuffer.Count; i++)
            {
                if (agvcTransCmdBuffer[i].CassetteId == cassetteId)
                {
                    throw new Exception("Transfer command casette ID duplicate.");
                }
            }
        }

        private void CheckEnoughEmptySlotToLoad()
        {
            if (Vehicle.CarrierSlotLeft.EnumCarrierSlotState == EnumCarrierSlotState.Empty ||
                Vehicle.CarrierSlotRight.EnumCarrierSlotState == EnumCarrierSlotState.Empty)
            {
            }
            else
            {
                throw new Exception("Slot is full.");
            }
        }

        private void CheckExceutingCommand(string CassetteId)
        {
            if (Vehicle.TransferCommand == null)
            {
            }
            else
            {
                if (Vehicle.TransferCommand.CassetteId == CassetteId)
                {
                    throw new Exception("Duplicate CassetteId.");
                }
            }
        }

        private void CheckTransferCommandMap(AgvcTransferCommand transferCommand)
        {
            if (Vehicle.MapTransferCommands.Any(x => IsMoveTransferCommand(x.Value.AgvcTransCommandType)))
            {
                throw new Exception("Vehicle has move command, can not do loadunload.");
            }

            if (Vehicle.MainFlowConfig.SlotDisable == EnumSlotSelect.Both)
            {
                throw new Exception($"Vehicle has no empty slot to transfer cst. Left = Disable, Right = Disable.");
            }


            if (Vehicle.MapTransferCommands.Values.Count >= 2)
            {
                throw new Exception("Vehicle has two command, can not do loadunload.");
            }

            //int existEnroute = 0;
            //foreach (var item in Vehicle.MapTransferCommands.Values.ToArray())
            //{
            //    if (item.EnrouteState == transferCommand.EnrouteState)
            //    {
            //        existEnroute++;
            //    }
            //}
            //if (existEnroute > 1)
            //{
            //    throw new Exception($"Vehicle has no enough slot to transfer. ExistEnroute[{existEnroute}]");
            //}
        }

        private bool IsMoveTransferCommand(EnumAgvcTransCommandType agvcTransCommandType)
        {
            return agvcTransCommandType == EnumAgvcTransCommandType.Move || agvcTransCommandType == EnumAgvcTransCommandType.MoveToCharger;
        }

        private EnumSlotNumber CheckUnloadCstId(string cassetteId)
        {
            if (Vehicle.MapTransferCommands.Any(x => IsMoveTransferCommand(x.Value.AgvcTransCommandType)))
            {
                throw new Exception("Vehicle has move command, can not do loadunload.");
            }
            if (string.IsNullOrEmpty(cassetteId))
            {
                throw new Exception($"Unload CST ID is Empty.");
            }
            if (Vehicle.CarrierSlotLeft.CarrierId.Trim() == cassetteId)
            {
                return EnumSlotNumber.L;
            }
            else if (Vehicle.CarrierSlotRight.CarrierId.Trim() == cassetteId)
            {
                return EnumSlotNumber.R;
            }
            else
            {
                throw new Exception($"No [{cassetteId}] to unload.");
            }
        }

        private void CheckOverrideAddress(AgvcTransferCommand transferCommand)
        {
            return;
        }

        private void CheckRobotPortAddress(string portAddressId, string portId)
        {
            CheckMoveEndAddress(portAddressId);
            MapAddress portAddress = Vehicle.MapInfo.addressMap[portAddressId];
            if (!portAddress.IsTransferPort())
            {
                throw new Exception($"{portAddressId} can not unload.");
            }

            if (Vehicle.MapInfo.portMap.ContainsKey(portId))
            {
                var port = Vehicle.MapInfo.portMap[portId];
                if (port.ReferenceAddressId != portAddressId)
                {
                    throw new Exception($"{portAddressId} unmatch {portId}.");
                }
            }
        }

        private void CheckMoveEndAddress(string unloadAddressId)
        {
            if (!Vehicle.MapInfo.addressMap.ContainsKey(unloadAddressId))
            {
                throw new Exception($"{unloadAddressId} is not in the map.");
            }
        }

        private void AgvcConnector_OnOverrideCommandEvent(object sender, AgvcTransferCommand transferCommand)
        {
            try
            {
                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[檢查替代命令] Check Override Transfer Command [{transferCommand.CommandId}]");

                #region 替代路徑檢查
                try
                {
                    if (Vehicle.TransferCommand.IsCheckingAvoid)
                    {
                        throw new Exception($"Vehicle is checking avoid request, cant not override.");
                    }
                    else
                    {
                        Vehicle.TransferCommand.IsCheckingOverride = true;
                    }

                    if (Vehicle.TransferCommand.TransferStep == EnumTransferStep.Idle)
                    {
                        throw new Exception($"Vehicle is idle, cant not override.");
                    }

                    if (Vehicle.TransferCommand.TransferStep != EnumTransferStep.MoveToAddressWaitEnd && Vehicle.TransferCommand.TransferStep != EnumTransferStep.WaitOverrideToContinue)
                    {
                        throw new Exception("Vehicle is not in moving step or waiting for override, cant not override.");
                    }

                    if (!(Vehicle.MovingGuide.ReserveStop == VhStopSingle.On || Vehicle.MovingGuide.IsAvoidComplete))
                    {
                        throw new Exception($"Vehicle is not in reserve-stop, cant not override.");
                    }

                    if (Vehicle.TransferCommand.EnrouteState == CommandState.LoadEnroute)
                    {
                        CheckPathToEnd(transferCommand.ToLoadSectionIds, transferCommand.ToLoadAddressIds, transferCommand.LoadAddressId);

                        if (Vehicle.TransferCommand.LoadAddressId != transferCommand.LoadAddressId)
                        {
                            throw new Exception($"Load address id check fail, cant not override.");
                        }

                        if (Vehicle.TransferCommand.AgvcTransCommandType == EnumAgvcTransCommandType.LoadUnload)
                        {
                            CheckPathToEnd(transferCommand.ToUnloadSectionIds, transferCommand.ToUnloadAddressIds, transferCommand.UnloadAddressId);

                            if (Vehicle.TransferCommand.UnloadAddressId != transferCommand.UnloadAddressId)
                            {
                                throw new Exception($"Unload address id check fail, cant not override.");
                            }
                        }
                    }
                    else
                    {
                        CheckLoadInfoIsEmpty(transferCommand);

                        CheckPathToEnd(transferCommand.ToUnloadSectionIds, transferCommand.ToUnloadAddressIds, transferCommand.UnloadAddressId);

                        if (Vehicle.TransferCommand.UnloadAddressId != transferCommand.UnloadAddressId)
                        {
                            throw new Exception($"Unload address id check fail, cant not override.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    agvcConnector.ReplyTransferCommand(transferCommand.CommandId, transferCommand.GetCommandActionType(), transferCommand.SeqNum, (int)EnumAgvcReplyCode.Reject, ex.Message);
                    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
                    Vehicle.TransferCommand.IsCheckingOverride = false;
                    return;
                }
                #endregion

                #region 替代路徑生成
                try
                {
                    PauseVisitTransferSteps();
                    agvcConnector.ClearAllReserve();
                    IsNotAvoid();
                    OverrideTransferCommand(transferCommand);
                    agvcConnector.ReplyTransferCommand(transferCommand.CommandId, transferCommand.GetCommandActionType(), transferCommand.SeqNum, (int)EnumAgvcReplyCode.Accept, "");
                    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"Accept override transfer command, id = [{transferCommand.CommandId}].");
                    Vehicle.TransferCommand.IsCheckingOverride = false;
                    ResumeVisitTransferSteps();
                }
                catch (Exception ex)
                {
                    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"Override initial fail. reason = {ex.Message}");
                    StopClearAndReset();
                    agvcConnector.ReplyTransferCommand(transferCommand.CommandId, transferCommand.GetCommandActionType(), transferCommand.SeqNum, (int)EnumAgvcReplyCode.Reject, ex.Message);
                }

                #endregion

            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        private void IsNotAvoid()
        {
            Vehicle.MovingGuide.IsAvoidMove = false;
            Vehicle.MovingGuide.IsAvoidComplete = false;
        }

        private void OverrideTransferCommand(AgvcTransferCommand transferCommand)
        {
            switch (Vehicle.TransferCommand.EnrouteState)
            {
                case CommandState.LoadEnroute:
                    OverrideTransferCommandToLoad(transferCommand);
                    if (Vehicle.TransferCommand.AgvcTransCommandType == EnumAgvcTransCommandType.LoadUnload)
                    {
                        OverrideTransferCommandToUnload(transferCommand);
                    }
                    Vehicle.TransferCommand.TransferStep = EnumTransferStep.MoveToLoad;
                    break;
                case CommandState.UnloadEnroute:
                    OverrideTransferCommandToUnload(transferCommand);
                    Vehicle.TransferCommand.TransferStep = EnumTransferStep.MoveToUnload;
                    break;
                case CommandState.None:
                    OverrideTransferCommandToUnload(transferCommand);
                    Vehicle.TransferCommand.TransferStep = EnumTransferStep.MoveToAddress;
                    break;
            }
        }

        private void OverrideTransferCommandToUnload(AgvcTransferCommand transferCommand)
        {
            Vehicle.TransferCommand.ToUnloadSectionIds = transferCommand.ToUnloadSectionIds;
            Vehicle.TransferCommand.ToUnloadAddressIds = transferCommand.ToUnloadAddressIds;
        }

        private void OverrideTransferCommandToLoad(AgvcTransferCommand transferCommand)
        {
            Vehicle.TransferCommand.ToLoadSectionIds = transferCommand.ToLoadSectionIds;
            Vehicle.TransferCommand.ToLoadAddressIds = transferCommand.ToLoadAddressIds;
        }

        private void CheckLoadInfoIsEmpty(AgvcTransferCommand transferCommand)
        {
            if (transferCommand.ToLoadSectionIds.Any())
            {
                throw new Exception($"Load section list is not empty.");
            }

            if (transferCommand.ToLoadAddressIds.Any())
            {
                throw new Exception($"Load address list is not empty.");
            }

            if (!string.IsNullOrEmpty(transferCommand.LoadAddressId))
            {
                throw new Exception($"Load port address is not empty.");
            }
        }

        private void CheckPathToEnd(List<string> sectionIds, List<string> addressIds, string endAddressId)
        {
            if (sectionIds.Count == 0)
            {
                throw new Exception($"Section list is empty.");
            }

            if (addressIds.Count == 0)
            {
                throw new Exception($"Address list is empty.");
            }

            for (int i = 0; i < sectionIds.Count; i++)
            {
                if (!Vehicle.MapInfo.sectionMap.ContainsKey(sectionIds[i]))
                {
                    throw new Exception($"{sectionIds[i]} is not in the map.");
                }

                if (!Vehicle.MapInfo.addressMap.ContainsKey(addressIds[i]))
                {
                    throw new Exception($"{addressIds[i]} is not in the map.");
                }

                MapSection mapSection = Vehicle.MapInfo.sectionMap[sectionIds[i]];
                if (!mapSection.InSection(addressIds[i]))
                {
                    throw new Exception($"{addressIds[i]} is not in {sectionIds[i]}.");
                }
            }

            if (!Vehicle.MapInfo.addressMap.ContainsKey(endAddressId))
            {
                throw new Exception($"{endAddressId} is not in the map.");
            }

            MapSection endSection = Vehicle.MapInfo.sectionMap[sectionIds[sectionIds.Count - 1]];
            if (!endSection.InSection(endAddressId))
            {
                throw new Exception($"{endAddressId} is not in {endSection.Id}.");
            }
        }

        private void AgvcConnector_OnAvoideRequestEvent(object sender, MovingGuide aseMovingGuide)
        {
            #region 避車檢查
            try
            {
                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"MainFlow :  Get Avoid Command, End Adr=[{aseMovingGuide.ToAddressId}],  start check .");

                if (Vehicle.TransferCommand.IsCheckingOverride)
                {
                    throw new Exception($"Vehicle is checking avoid request, cant not override.");
                }
                else
                {
                    Vehicle.TransferCommand.IsCheckingAvoid = true;
                }

                if (Vehicle.MapTransferCommands.IsEmpty)
                {
                    throw new Exception("Vehicle has no Command, can not Avoid");
                }

                if (!IsMoveStep() || Vehicle.TransferCommand.TransferStep == EnumTransferStep.WaitOverrideToContinue)
                {
                    throw new Exception("Vehicle is not moving, can not Avoid");
                }

                if (!IsMoveStopByNoReserve() && !Vehicle.MovingGuide.IsAvoidComplete)
                {
                    throw new Exception($"Vehicle is not stop by no reserve, can not Avoid");
                }

                CheckPathToEnd(aseMovingGuide.GuideSectionIds, aseMovingGuide.GuideAddressIds, aseMovingGuide.ToAddressId);
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
                RejectAvoidCommandAndResume(EnumMiddlerAlarmCode.CheckAvoidException, ex.Message, aseMovingGuide);
                Vehicle.TransferCommand.IsCheckingAvoid = false;
            }
            #endregion
            //liu
            #region Cancel當前命令
            try
            {
                MoveHandler.StopMove(EnumMoveStopType.AvoidStop);
                MoveHandler.AskReadyForMoveCommandRequest();
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
                RejectAvoidCommandAndResume(EnumMiddlerAlarmCode.CheckAvoidException, ex.Message, aseMovingGuide);
                Vehicle.TransferCommand.IsCheckingAvoid = false;
            }
            #endregion

            #region 避車Command生成
            try
            {
                //Vehicle.MovingGuide.IsAvoidMove = true;
                agvcConnector.ClearAllReserve();
                Vehicle.MovingGuide = aseMovingGuide;
                PrepareToMove();
                agvcConnector.SetupNeedReserveSections();
                agvcConnector.StatusChangeReport();
                agvcConnector.ReplyAvoidCommand(aseMovingGuide.SeqNum, 0, "");
                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"MainFlow : Get 避車Command checked , 終點[{aseMovingGuide.ToAddressId}].");
                Vehicle.TransferCommand.IsCheckingAvoid = false;
            }
            catch (Exception ex)
            {
                StopClearAndReset();
                RejectAvoidCommandAndResume(EnumMiddlerAlarmCode.CheckAvoidException, "避車Exception", aseMovingGuide);
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }

            #endregion
        }

        public bool IsMoveStep()
        {
            return Vehicle.TransferCommand.TransferStep == EnumTransferStep.MoveToAddressWaitEnd;
        }

        private bool IsMoveStopByNoReserve()
        {
            return Vehicle.MovingGuide.ReserveStop == VhStopSingle.On;
        }

        private void RejectAvoidCommandAndResume(EnumMiddlerAlarmCode alarmCode, string reason, MovingGuide aseMovingGuide)
        {
            try
            {
                AlarmHandler.SetAlarmFromAgvm(alarmCode);
                agvcConnector.ReplyAvoidCommand(aseMovingGuide.SeqNum, 1, reason);
                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, string.Concat($"MainFlow : Reject Avoid Command, ", reason));
                agvcConnector.ResumeAskReserve();
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        #endregion

        #endregion

        #region Thd Watch Charge Stage

        private void WatchChargeStage()
        {
            while (true)
            {
                try
                {
                    BatteryHandler.GetBatteryAndChargeStatus();

                    if (Vehicle.AutoState == EnumAutoState.Auto && IsVehicleIdle())
                    {
                        if (IsLowPower())
                        {
                            LowPowerStartCharge(Vehicle.MoveStatus.LastAddress);
                        }
                    }

                    if (IsMuchLowPower() && !Vehicle.BatteryStatus.IsCharging) //200701 dabid+
                    {
                        throw new Exception($"[AutoState={Vehicle.AutoState}][IsVehicleIdle()={IsVehicleIdle()}][Percentage={Vehicle.BatteryStatus.Percentage}][HighThreshold={Vehicle.MainFlowConfig.HighPowerPercentage}]");
                    }
                }
                catch (Exception ex)
                {
                    LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
                    //AlarmHandler.SetAlarmFromAgvm(2);
                }

                SpinWait.SpinUntil(() => false, Vehicle.MainFlowConfig.WatchLowPowerSleepTimeMs);
            }
        }

        private bool IsMuchLowPower()
        {
            return Vehicle.BatteryStatus.Percentage + 10 <= Vehicle.MainFlowConfig.HighPowerPercentage;
        }

        public bool IsVehicleIdle()
        {
            if (Vehicle.TransferCommand.TransferStep == EnumTransferStep.Idle)
            {
                return true;
            }
            if (!Vehicle.MapTransferCommands.Any())
            {
                return true;
            }
            return false;
        }

        public void StartWatchChargeStage()
        {
            thdWatchChargeStage = new Thread(WatchChargeStage);
            thdWatchChargeStage.IsBackground = true;
            thdWatchChargeStage.Start();
            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"StartWatchChargeStage");
        }

        public bool IsLowPower()
        {
            return Vehicle.BatteryStatus.Percentage <= Vehicle.MainFlowConfig.HighPowerPercentage;
        }

        private bool IsHighPower()
        {
            return Vehicle.BatteryStatus.Percentage > Vehicle.MainFlowConfig.HighPowerPercentage;
        }

        public void MainFormStartCharge()
        {
            Task.Run(() =>
            {
                StartCharge(Vehicle.MoveStatus.LastAddress);
            });
        }

        private object _StartOrStopChargeLocker = new object();

        private void StartCharge(MapAddress endAddress)
        {
            try
            {
                lock (_StartOrStopChargeLocker)
                {
                    BatteryHandler.GetBatteryAndChargeStatus();

                    if (endAddress.IsCharger())
                    {
                        if (IsHighPower())
                        {
                            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[電量過高.無法充電] High power not start charge.[Precentage = {Vehicle.BatteryStatus.Percentage}] > [Threshold = {Vehicle.MainFlowConfig.HighPowerPercentage}][Vehicle arrival {endAddress.Id},Charge Direction = {endAddress.ChargingDirection}].");
                            return;
                        }

                        agvcConnector.ChargHandshaking();

                        Vehicle.BatteryStatus.IsCharging = true;

                        LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $@"[充電.開始.執行] Start Charge, Vehicle arrival {endAddress.Id},Charge Direction = {endAddress.ChargingDirection},Precentage = {Vehicle.BatteryStatus.Percentage}.");

                        BatteryHandler.StartCharge(endAddress.ChargingDirection);

                        SpinWait.SpinUntil(() => Vehicle.CheckStartChargeReplyEnd, Vehicle.MainFlowConfig.StartChargeWaitingTimeoutMs);

                        if (Vehicle.BatteryStatus.IsCharging)
                        {
                            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, "[充電.成功] Start Charge success.");
                            agvcConnector.Charging();
                        }
                        else
                        {
                            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, "[充電.失敗] Start Charge fail.");
                            //AlarmHandler.SetAlarmFromAgvm(000013);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }

            BatteryHandler.GetBatteryAndChargeStatus();
        }

        private void LowPowerStartCharge(MapAddress lastAddress)
        {
            try
            {
                lock (_StartOrStopChargeLocker)
                {
                    if (Vehicle.BatteryStatus.IsCharging)
                    {
                        throw new Exception($"Vehicle is Charging.");
                    }

                    if (!lastAddress.IsCharger())
                    {
                        throw new Exception($"At {lastAddress.Id} is not coupler.");
                    }

                    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[低電量閒置.自動充電] Low power charging.");

                    Vehicle.BatteryStatus.IsCharging = true;

                    agvcConnector.ChargHandshaking();

                    BatteryHandler.StartCharge(lastAddress.ChargingDirection);

                    SpinWait.SpinUntil(() => Vehicle.CheckStartChargeReplyEnd, Vehicle.MainFlowConfig.StartChargeWaitingTimeoutMs);

                    if (Vehicle.BatteryStatus.IsCharging)
                    {
                        LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, "[低電量閒置.自動充電.成功] Low power charge success.");
                        agvcConnector.Charging();
                    }
                    else
                    {
                        LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[低電量閒置.自動充電.失敗] Low power charge fail.");
                        //AlarmHandler.SetAlarmFromAgvm(000013);
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[低電量閒置.自動充電.失敗] Low Power Charge Fail, {ex.Message}");
            }

            BatteryHandler.GetBatteryAndChargeStatus();
        }

        public void StopCharge()
        {
            try
            {
                lock (_StartOrStopChargeLocker)
                {
                    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $@"[斷充.開始] Try STOP charge.[IsCharging = {Vehicle.BatteryStatus.IsCharging}]");

                    BatteryHandler.GetBatteryAndChargeStatus();

                    if (Vehicle.MoveStatus.LastAddress.IsCharger() || Vehicle.BatteryStatus.IsCharging)
                    {
                        agvcConnector.ChargHandshaking();

                        BatteryHandler.StopCharge();

                        SpinWait.SpinUntil(() => Vehicle.CheckStopChargeReplyEnd, Vehicle.MainFlowConfig.StopChargeWaitingTimeoutMs + 5 * 1000);

                        if (!Vehicle.BatteryStatus.IsCharging)
                        {
                            agvcConnector.ChargeOff();
                            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[斷充.成功] Stop Charge success.");
                        }
                        else
                        {
                            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[斷充.逾時] Stop Charge Timeout.");
                            //AlarmHandler.SetAlarmFromAgvm(000014);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }

        }

        private bool IsRobotStep()
        {
            switch (Vehicle.TransferCommand.TransferStep)
            {
                case EnumTransferStep.WaitLoadArrivalReply:
                case EnumTransferStep.Load:
                case EnumTransferStep.WaitLoadCompleteReply:
                case EnumTransferStep.WaitCstIdReadReply:
                case EnumTransferStep.UnloadArrival:
                case EnumTransferStep.WaitUnloadArrivalReply:
                case EnumTransferStep.Unload:
                case EnumTransferStep.WaitUnloadCompleteReply:
                case EnumTransferStep.LoadWaitEnd:
                case EnumTransferStep.UnloadWaitEnd:
                case EnumTransferStep.RobotFail:
                    return true;
                default:
                    return false; ;
            }
        }

        private void ArrivalStartCharge(MapAddress endAddress)
        {
            try
            {
                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[到站.充電] : ArrivalStartCharge.");
                Task.Run(() =>
                {
                    StartCharge(endAddress);
                });
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        private bool IsMoveEndRobotStep()
        {
            try
            {
                switch (Vehicle.TransferCommand.AgvcTransCommandType)
                {
                    case EnumAgvcTransCommandType.Load:
                    case EnumAgvcTransCommandType.Unload:
                    case EnumAgvcTransCommandType.LoadUnload:
                        return true;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }

            return false;
        }

        private void BatteryHandler_OnUpdateBatteryStatusEvent(object sender, BatteryStatus batteryStatus)
        {
            try
            {
                Vehicle.BatteryStatus = batteryStatus;
                Vehicle.BatteryLog.InitialSoc = batteryStatus.Percentage;
                agvcConnector.BatteryHandler_OnBatteryPercentageChangeEvent(sender, batteryStatus.Percentage);

                if (batteryStatus.IsCharging)
                {
                    Vehicle.CheckStartChargeReplyEnd = true;
                }
                else
                {
                    Vehicle.CheckStopChargeReplyEnd = true;
                }

                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[電池.狀態.改變] UpdateBatteryStatus:[{batteryStatus.IsCharging}][Percentage={batteryStatus.Percentage}]");
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        #endregion

        public void StopVehicle()
        {
            MoveHandler.StopMove(EnumMoveStopType.NormalStop);
            RobotHandler.ClearRobotCommand();
            BatteryHandler.StopCharge();

            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"MainFlow : Stop Vehicle, [MoveState={Vehicle.MoveStatus.EnumMoveState}][IsCharging={Vehicle.BatteryStatus.IsCharging}]");
        }

        public void SetupVehicleSoc(int percentage)
        {
            BatteryHandler.SetPercentageTo(percentage);
        }

        private void AgvcConnector_OnRenameCassetteIdEvent(object sender, CarrierSlotStatus e)
        {
            try
            {
                foreach (var transCmd in Vehicle.MapTransferCommands.Values.ToList())
                {
                    if (transCmd.SlotNumber == e.SlotNumber)
                    {
                        transCmd.CassetteId = e.CarrierId;
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        public void AgvcConnector_OnCmdPauseEvent(ushort iSeqNum, PauseType pauseType)
        {
            try
            {
                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[執行.暫停] [{PauseEvent.Pause}][{pauseType}]");

                Vehicle.PauseFlags[pauseType] = true;
                PauseTransfer();
                MoveHandler.PauseMove();
                agvcConnector.PauseReply(iSeqNum, (int)EnumAgvcReplyCode.Accept, PauseEvent.Pause);
                agvcConnector.StatusChangeReport();
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        public void AgvcConnector_OnCmdResumeEvent(ushort iSeqNum, PauseType pauseType)
        {
            try
            {
                if (pauseType == PauseType.All)
                {
                    Vehicle.ResetPauseFlags();
                    ResumeMiddler(iSeqNum, pauseType);
                }
                else
                {
                    Vehicle.PauseFlags[pauseType] = false;
                    agvcConnector.PauseReply(iSeqNum, (int)EnumAgvcReplyCode.Accept, PauseEvent.Continue);

                    if (!Vehicle.IsPause())
                    {
                        ResumeMiddler(iSeqNum, pauseType);
                    }
                    else
                    {
                        LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[尚有.其他.暫停旗標] [{PauseEvent.Continue}][{pauseType}]");
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        private void ResumeMiddler(ushort iSeqNum, PauseType pauseType)
        {
            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[執行.續行] [{PauseEvent.Continue}][{pauseType}]");

            agvcConnector.PauseReply(iSeqNum, (int)EnumAgvcReplyCode.Accept, PauseEvent.Continue);
            MoveHandler.ResumeMove();
            ResumeTransfer();
            agvcConnector.StatusChangeReport();
        }

        private void ResumeMiddler()
        {
            LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[執行.續行] By Op Resume.");

            MoveHandler.ResumeMove();
            ResumeTransfer();
            agvcConnector.StatusChangeReport();
        }

        public void AgvcConnector_OnCmdCancelAbortEvent(ushort iSeqNum, ID_37_TRANS_CANCEL_REQUEST receive)
        {
            PauseTransfer();

            try
            {
                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"MainFlow : Get [{receive.CancelAction}] Command.");

                string abortCmdId = receive.CmdID.Trim();
                bool IsAbortCurCommand = Vehicle.TransferCommand.CommandId == abortCmdId;
                var targetAbortCmd = Vehicle.MapTransferCommands[abortCmdId];

                if (IsAbortCurCommand)
                {
                    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[放棄.當前.命令] TransferComplete [{targetAbortCmd.CompleteStatus}].");

                    agvcConnector.ClearAllReserve();
                    MoveHandler.StopMove(EnumMoveStopType.NormalStop);
                    Vehicle.MovingGuide = new MovingGuide();
                    Vehicle.TransferCommand.CompleteStatus = GetCompleteStatusFromCancelRequest(receive.CancelAction);
                    Vehicle.TransferCommand.TransferStep = EnumTransferStep.TransferComplete;
                }
                else
                {
                    LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"[放棄.背景.命令] TransferComplete [{targetAbortCmd.CompleteStatus}].");

                    WaitingTransferCompleteEnd = true;

                    targetAbortCmd.TransferStep = EnumTransferStep.Abort;
                    targetAbortCmd.CompleteStatus = GetCompleteStatusFromCancelRequest(receive.CancelAction);

                    Vehicle.MapTransferCommands.TryRemove(Vehicle.TransferCommand.CommandId, out AgvcTransferCommand transferCommand);
                    agvcConnector.TransferComplete(transferCommand);

                    if (Vehicle.MapTransferCommands.IsEmpty)
                    {
                        Vehicle.ResetPauseFlags();

                        agvcConnector.NoCommand();

                        Vehicle.TransferCommand = new AgvcTransferCommand();
                    }
                    else
                    {
                        agvcConnector.StatusChangeReport();
                    }

                    WaitingTransferCompleteEnd = false;
                }
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }

            ResumeTransfer();
        }

        private CompleteStatus GetCompleteStatusFromCancelRequest(CancelActionType cancelAction)
        {
            switch (cancelAction)
            {
                case CancelActionType.CmdCancel:
                    return CompleteStatus.Cancel;
                case CancelActionType.CmdCancelIdMismatch:
                    return CompleteStatus.IdmisMatch;
                case CancelActionType.CmdCancelIdReadFailed:
                    return CompleteStatus.IdreadFailed;
                case CancelActionType.CmdNone:
                case CancelActionType.CmdAbort:
                case CancelActionType.CmdEms:
                default:
                    return CompleteStatus.Abort;
            }
        }

        public void AgvcConnectionChanged()
        {
            try
            {
                RemoteModeHandler.AgvcConnectionChanged(Vehicle.IsAgvcConnect);
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        #region LocalPackage        

        public object _ModeChangeLocker = new object();

        public void RemoteModeHandler_OnModeChangeEvent(object sender, EnumAutoState autoState)
        {
            try
            {
                lock (_ModeChangeLocker)
                {
                    StopClearAndReset();

                    if (Vehicle.AutoState != autoState)
                    {
                        if (autoState == EnumAutoState.Auto)
                        {
                            GetAllStatusReport();
                            AlarmHandler.ResetAllAlarmsFromAgvm();
                            //UpdateSlotStatus();
                            Vehicle.MoveStatus.IsMoveEnd = false;
                        }

                        agvcConnector.CSTStatus_Initial();

                        Vehicle.AutoState = autoState;
                        agvcConnector.StatusChangeReport();

                        LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"Switch to {autoState}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        private void CheckCanAuto()
        {

            if (Vehicle.MoveStatus.LastSection == null || string.IsNullOrEmpty(Vehicle.MoveStatus.LastSection.Id))
            {
                CanAutoMsg = "Section Lost";
                throw new Exception("CheckCanAuto fail. Section Lost.");
            }
            else if (Vehicle.MoveStatus.LastAddress == null || string.IsNullOrEmpty(Vehicle.MoveStatus.LastAddress.Id))
            {
                CanAutoMsg = "Address Lost";
                throw new Exception("CheckCanAuto fail. Address Lost.");
            }
            else if (Vehicle.MoveStatus.EnumMoveState != EnumMoveState.Idle && Vehicle.MoveStatus.EnumMoveState != EnumMoveState.Block)
            {
                CanAutoMsg = $"Move State = {Vehicle.MoveStatus.EnumMoveState}";
                throw new Exception($"CheckCanAuto fail. {CanAutoMsg}");
            }
            else if (Vehicle.MoveStatus.LastAddress.MyDistance(Vehicle.MoveStatus.LastMapPosition) >= Vehicle.MainFlowConfig.InitialPositionRangeMm)
            {
                AlarmHandler.SetAlarmFromAgvm(EnumMiddlerAlarmCode.InitialPositionError);
                CanAutoMsg = $"Initial Positon Too Far.";
                throw new Exception($"CheckCanAuto fail. {CanAutoMsg}");
            }

            var aseRobotStatus = Vehicle.RobotStatus;
            if (aseRobotStatus.EnumRobotState != EnumRobotState.Idle)
            {
                CanAutoMsg = $"Robot State = {aseRobotStatus.EnumRobotState}";
                throw new Exception($"CheckCanAuto fail. {CanAutoMsg}");
            }
            else if (!aseRobotStatus.IsHome)
            {
                CanAutoMsg = $"Robot IsHome = {aseRobotStatus.IsHome}";
                throw new Exception($"CheckCanAuto fail. {CanAutoMsg}");
            }

            CanAutoMsg = "OK";
        }

        private void UpdateSlotStatus()
        {
            try
            {
                CarrierSlotStatus leftSlotStatus = new CarrierSlotStatus(Vehicle.CarrierSlotLeft);

                switch (leftSlotStatus.EnumCarrierSlotState)
                {
                    case EnumCarrierSlotState.Empty:
                        {
                            leftSlotStatus.CarrierId = "";
                            leftSlotStatus.EnumCarrierSlotState = EnumCarrierSlotState.Empty;
                            Vehicle.CarrierSlotLeft = leftSlotStatus;
                            Vehicle.LeftReadResult = BCRReadResult.BcrNormal;
                        }
                        break;
                    case EnumCarrierSlotState.Loading:
                        {
                            if (string.IsNullOrEmpty(leftSlotStatus.CarrierId.Trim()))
                            {
                                leftSlotStatus.CarrierId = "ReadFail";
                                leftSlotStatus.EnumCarrierSlotState = EnumCarrierSlotState.ReadFail;
                                Vehicle.CarrierSlotLeft = leftSlotStatus;
                                Vehicle.LeftReadResult = BCRReadResult.BcrReadFail;
                            }
                            else
                            {
                                Vehicle.LeftReadResult = BCRReadResult.BcrNormal;
                            }
                        }
                        break;
                    case EnumCarrierSlotState.PositionError:
                        {
                            AlarmHandler.SetAlarmFromAgvm(EnumMiddlerAlarmCode.CstPositionError);
                        }
                        return;
                    case EnumCarrierSlotState.ReadFail:
                        {
                            leftSlotStatus.CarrierId = "ReadFail";
                            leftSlotStatus.EnumCarrierSlotState = EnumCarrierSlotState.ReadFail;
                            Vehicle.CarrierSlotLeft = leftSlotStatus;
                            Vehicle.LeftReadResult = BCRReadResult.BcrReadFail;
                        }
                        break;
                    default:
                        break;
                }


                agvcConnector.CSTStatusReport(); //200625 dabid#
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        #endregion

        #region Set / Reset Alarm

        private void AlarmHandler_OnResetAlarmToAgvcEvent(object sender, EventArgs e)
        {
            agvcConnector.ResetAllAlarmsToAgvc();
        }

        private void AlarmHandler_OnSetAlarmToAgvcEvent(object sender, Model.AlarmArgs e)
        {
            agvcConnector.SetlAlarmToAgvc(e.ErrorCode, e.IsAlarm);
        }

        #endregion

        #region Log

        //private void IMessageHandler_OnLogErrorEvent(object sender, Tools.MessageHandlerArgs e)
        //{
        //    LogException(e.ClassMethodName, e.Message);
        //}

        //private void IMessageHandler_OnLogDebugEvent(object sender, Tools.MessageHandlerArgs e)
        //{
        //    LogDebug(e.ClassMethodName, e.Message);
        //}

        public void AppendDebugLog(string msg)
        {
            try
            {
                int th = Vehicle.MainFlowConfig.StringBuilderMax;
                int thHalf = th / 2;

                lock (SbDebugMsg)
                {
                    if (SbDebugMsg.Length + msg.Length > th)
                    {
                        SbDebugMsg.Remove(0, thHalf);
                    }
                    SbDebugMsg.AppendLine($"{DateTime.Now:HH:mm:ss} {msg}");
                }
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        public string GetDebugLog()
        {
            string result = "";

            try
            {
                lock (SbDebugMsg)
                {
                    result = SbDebugMsg.ToString();
                }
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }

            return result;
        }


        private void LogException(string classMethodName, string exMsg)
        {
            try
            {
                _transferLogger.Error($"[{Vehicle.SoftwareVersion}][{Vehicle.AgvcConnectorConfig.ClientName}][{classMethodName}][{exMsg}]");

            }
            catch (Exception) { }
        }

        public void LogDebug(string classMethodName, string msg)
        {
            try
            {
                _transferLogger.Debug($"[{Vehicle.SoftwareVersion}][{Vehicle.AgvcConnectorConfig.ClientName}][{classMethodName}][{msg}]");

                AppendDebugLog(msg);
            }
            catch (Exception) { }
        }

        #endregion
    }

    public class LastIdlePosition
    {
        public DateTime TimeStamp { get; set; } = DateTime.Now;
        public MapPosition Position { get; set; } = new MapPosition();
    }
}