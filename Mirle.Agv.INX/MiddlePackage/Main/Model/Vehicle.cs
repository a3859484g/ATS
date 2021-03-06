using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mirle.Agv.MiddlePackage.Umtc.Model.TransferSteps;
using com.mirle.aka.sc.ProtocolFormat.agvMessage ;
using Mirle.Agv.MiddlePackage.Umtc.Model.Configs;
using Mirle.Agv.MiddlePackage.Umtc.Controller;
using System.Reflection;
using System.Collections.Concurrent;
using NUnit.Framework.Constraints;

namespace Mirle.Agv.MiddlePackage.Umtc.Model
{

    public class Vehicle
    {
        private static readonly Vehicle theVehicle = new Vehicle();
        public static Vehicle Instance { get { return theVehicle; } }
        public ConcurrentDictionary<string, AgvcTransferCommand> MapTransferCommands { get; set; } = new ConcurrentDictionary<string, AgvcTransferCommand>();
        public AgvcTransferCommand TransferCommand { get; set; } = new AgvcTransferCommand();
        public EnumAutoState AutoState { get; set; } = EnumAutoState.Manual;
        public string SoftwareVersion { get; set; } = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public bool IsAgvcConnect { get; set; } = false;
        public EnumLoginLevel LoginLevel { get; set; } = EnumLoginLevel.Op;
        public EnumChargingStage ChargingStage { get; set; } = EnumChargingStage.Idle;
        public MapInfo MapInfo { get; private set; } = new MapInfo();
        public string AskReserveQueueException { get; set; } = "NONE";

        #region AsePackage
        public bool IsLocalConnect { get; set; } = false;
        public MoveStatus MoveStatus { get; set; } = new MoveStatus();
        public RobotStatus RobotStatus { get; set; } = new RobotStatus();
        public CarrierSlotStatus CarrierSlotLeft { get; set; } = new CarrierSlotStatus();
        public CarrierSlotStatus CarrierSlotRight { get; set; } = new CarrierSlotStatus(EnumSlotNumber.R);
        public BatteryStatus BatteryStatus { get; set; } = new BatteryStatus();
        public MovingGuide MovingGuide { get;set; } = new MovingGuide();
        public string PspSpecVersion { get; set; } = "1.0";
        public bool CheckStartChargeReplyEnd { get; set; } = true;
        public bool CheckStopChargeReplyEnd { get; set; } = true;

        #endregion

        #region Comm Property
        //public VHActionStatus ActionStatus { get; set; } = VHActionStatus.NoCommand;
        public VhStopSingle BlockingStatus { get; set; } = VhStopSingle.Off;
        public VhChargeStatus ChargeStatus { get; set; } = VhChargeStatus.ChargeStatusNone;
        public DriveDirction DrivingDirection { get; set; } = DriveDirction.DriveDirNone;
        public VhStopSingle ObstacleStatus { get; set; } = VhStopSingle.Off;
        public int ObstDistance { get; set; }
        public string ObstVehicleID { get; set; } = "";
        public VhPowerStatus PowerStatus { get; set; } = VhPowerStatus.PowerOn;
        public string StoppedBlockID { get; set; } = "";
        public VhStopSingle ErrorStatus { get; set; } = VhStopSingle.Off;
        public uint CmdPowerConsume { get; set; }
        public int CmdDistance { get; set; }
        public string TeachingFromAddress { get; internal set; } = "";
        public string TeachingToAddress { get; internal set; } = "";
        public BCRReadResult LeftReadResult { get; set; } = BCRReadResult.BcrReadFail;
        public BCRReadResult RightReadResult { get; set; } = BCRReadResult.BcrReadFail;
        public VhStopSingle OpPauseStatus { get; set; } = VhStopSingle.Off;
        public ConcurrentDictionary<PauseType, bool> PauseFlags = new ConcurrentDictionary<PauseType, bool>(Enum.GetValues(typeof(PauseType)).Cast<PauseType>().ToDictionary(x => x, x => false));
        public uint WifiSignalStrength { get; set; } = 0;
        //public List<PortInfo> PortInfos { get; set; } = new List<PortInfo>();

        #endregion

        #region Configs
        //Main Configs
        public string MiddlerConfigPath { get; set; } = @"D:\MecanumConfigs\MiddlerConfig"; 
        public MainFlowConfig MainFlowConfig { get; set; } = new MainFlowConfig();
        public AgvcConnectorConfig AgvcConnectorConfig { get; set; } = new AgvcConnectorConfig();
        public MapConfig MapConfig { get; set; } = new MapConfig();
        public AlarmConfig AlarmConfig { get; set; } = new AlarmConfig();
        public BatteryLog BatteryLog { get; set; } = new BatteryLog();
 
        #endregion

        private Vehicle() { }

        public CarrierSlotStatus GetCarrierSlotStatusFrom(EnumSlotNumber slotNumber)
        {
            switch (slotNumber)
            {
                case EnumSlotNumber.R:
                    return this.CarrierSlotRight;
                case EnumSlotNumber.L:
                default:
                    return this.CarrierSlotLeft;
            }
        }

        public bool IsPause()
        {
            return PauseFlags.Values.Any(x => x);
        }

        public void ResetPauseFlags()
        {
            PauseFlags = new ConcurrentDictionary<PauseType, bool>(Enum.GetValues(typeof(PauseType)).Cast<PauseType>().ToDictionary(x => x, x => false));
        }

        public VHActionStatus GetActionStatus()
        {
            if (MapTransferCommands.Any())
            {
                return VHActionStatus.Commanding;
            }
            else if (TransferCommand.TransferStep == EnumTransferStep.TransferComplete)
            {
                return VHActionStatus.Commanding;
            }
            else
            {
                return VHActionStatus.NoCommand;
            }
        }

        public VhLoadCSTStatus GetHasCst(EnumSlotNumber slotNumber)
        {
            var slot = GetCarrierSlotStatusFrom(slotNumber);
            switch (slot.EnumCarrierSlotState)
            {
                case EnumCarrierSlotState.Loading:
                case EnumCarrierSlotState.PositionError:
                case EnumCarrierSlotState.ReadFail:
                    return VhLoadCSTStatus.Exist;
                case EnumCarrierSlotState.Empty:
                default:
                    return VhLoadCSTStatus.NotExist;
            }
        }

        public VhStopSingle GetPauseStatus(PauseType pauseType)
        {
            return PauseFlags[pauseType] ? VhStopSingle.On : VhStopSingle.Off;
        }

        public string GetCommandId(int index)
        {
            if (MapTransferCommands.Count < index)
            {
                return "";
            }
            else
            {
                return MapTransferCommands.Values.ToArray()[index - 1].CommandId;
            }
        }

        public com.mirle.aka.sc.ProtocolFormat.agvMessage .CommandState GetCommandState(int index)
        {
            if (MapTransferCommands.Count < index)
            {
                return com.mirle.aka.sc.ProtocolFormat.agvMessage .CommandState.None;
            }
            else
            {
                return GetEnrouteParse(MapTransferCommands.Values.ToArray()[index - 1].EnrouteState);
            }
        }

        private com.mirle.aka.sc.ProtocolFormat.agvMessage .CommandState GetEnrouteParse(CommandState enrouteState)
        {
            switch (enrouteState)
            {
                case CommandState.LoadEnroute:
                    return com.mirle.aka.sc.ProtocolFormat.agvMessage .CommandState.LoadEnroute;
                case CommandState.UnloadEnroute:
                    return com.mirle.aka.sc.ProtocolFormat.agvMessage .CommandState.UnloadEnroute;
                case CommandState.None:
                default:
                    return com.mirle.aka.sc.ProtocolFormat.agvMessage .CommandState.None;
            }
        }
    }
}
