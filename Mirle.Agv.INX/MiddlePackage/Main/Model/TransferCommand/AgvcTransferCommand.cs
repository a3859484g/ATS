using System.Collections.Generic;
using com.mirle.aka.sc.ProtocolFormat.agvMessage ;

namespace Mirle.Agv.MiddlePackage.Umtc.Model
{

    public class AgvcTransferCommand
    {
        public EnumTransferStep TransferStep { get; set; } = EnumTransferStep.Idle;
        public string CommandId { get; set; } = "";
        public EnumAgvcTransCommandType AgvcTransCommandType { get; set; } = EnumAgvcTransCommandType.Else;
        public string LoadAddressId { get; set; } = "";
        public string UnloadAddressId { get; set; } = "";
        public string CassetteId { get; set; } = "";
        public ushort SeqNum { get; set; }
        public CompleteStatus CompleteStatus { get; set; }
        public EnumSlotNumber SlotNumber { get; set; } = EnumSlotNumber.L;
        public CommandState EnrouteState { get; set; } = CommandState.None;
        public string LotId { get; set; } = "";
        public string LoadPortId { get; set; } = "";
        public string UnloadPortId { get; set; } = "";
        public bool IsRobotEnd { get; set; } = false;
        public bool IsStopAndClear { get; set; } = false;
        public bool IsLoadArrivalReply { get; set; } = false;
        public bool IsLoadCompleteReply { get; set; } = false;
        public bool IsCstIdReadReply { get; set; } = false;
        public bool IsUnloadArrivalReply { get; set; } = false;
        public bool IsUnloadCompleteReply { get; set; } = false;
        public bool IsVitualPortUnloadArrivalReply { get; set; } = false;
        public List<string> ToLoadAddressIds { get; set; } = new List<string>();
        public List<string> ToLoadSectionIds { get; set; } = new List<string>();
        public List<string> ToUnloadAddressIds { get; set; } = new List<string>();
        public List<string> ToUnloadSectionIds { get; set; } = new List<string>();
        public bool IsCheckingAvoid { get; set; }
        public bool IsCheckingOverride { get; set; }

        public AgvcTransferCommand()
        {
        }

        public AgvcTransferCommand(ID_31_TRANS_REQUEST transRequest, ushort aSeqNum)
        {
            CommandId = transRequest.CmdID.Trim();
            CassetteId = string.IsNullOrEmpty(transRequest.CSTID) ? "" : transRequest.CSTID.Trim();
            SeqNum = aSeqNum;

            InitialCommandType(transRequest.CommandAction);

            LoadAddressId = string.IsNullOrEmpty(transRequest.LoadAdr) ? "" : transRequest.LoadAdr.Trim();
            UnloadAddressId = string.IsNullOrEmpty(transRequest.DestinationAdr) ? "" : transRequest.DestinationAdr.Trim();

            //ToLoadAddressIds = transRequest.GuideAddressesStartToLoad == null ? new List<string>() : transRequest.GuideAddressesStartToLoad.ToList();
            //ToLoadSectionIds = transRequest.GuideSectionsStartToLoad == null ? new List<string>() : transRequest.GuideSectionsStartToLoad.ToList();

            //ToUnloadAddressIds = transRequest.GuideAddressesToDestination == null ? new List<string>() : transRequest.GuideAddressesToDestination.ToList();
            //ToUnloadSectionIds = transRequest.GuideSectionsToDestination == null ? new List<string>() : transRequest.GuideSectionsToDestination.ToList();
        }

        private void InitialCommandType(CommandActionType activeType)
        {
            switch (activeType)
            {
                case CommandActionType.Move:
                    AgvcTransCommandType = EnumAgvcTransCommandType.Move;
                    break;
                case CommandActionType.Load:
                    AgvcTransCommandType = EnumAgvcTransCommandType.Load;
                    break;
                case CommandActionType.Unload:
                    AgvcTransCommandType = EnumAgvcTransCommandType.Unload;
                    break;
                case CommandActionType.Loadunload:
                    AgvcTransCommandType = EnumAgvcTransCommandType.LoadUnload;
                    break;
                case CommandActionType.Movetocharger:
                    AgvcTransCommandType = EnumAgvcTransCommandType.MoveToCharger;
                    break;
                case CommandActionType.Home:
                    break;
                case CommandActionType.Override:
                    AgvcTransCommandType = EnumAgvcTransCommandType.Override;
                    //CompleteStatus = CompleteStatus.Loadunload;
                    break;
                case CommandActionType.Scan:  //liu 0416 scan
                    AgvcTransCommandType = EnumAgvcTransCommandType.Scan;
                    break;
                default:
                    break;
            }

            InitialCommandStepCompleteAndEnroute();
        }

        public void InitialCommandStepCompleteAndEnroute()
        {
            switch (AgvcTransCommandType)
            {
                case EnumAgvcTransCommandType.Move:
                    CompleteStatus = CompleteStatus.Move;
                    TransferStep = EnumTransferStep.MoveToAddress;
                    EnrouteState = CommandState.None;
                    break;
                case EnumAgvcTransCommandType.Load:
                    CompleteStatus = CompleteStatus.Load;
                    TransferStep = EnumTransferStep.MoveToLoad;
                    EnrouteState = CommandState.LoadEnroute;
                    break;
                case EnumAgvcTransCommandType.Unload:
                    CompleteStatus = CompleteStatus.Unload;
                    TransferStep = EnumTransferStep.MoveToUnload;
                    EnrouteState = CommandState.UnloadEnroute;
                    break;
                case EnumAgvcTransCommandType.LoadUnload:
                    CompleteStatus = CompleteStatus.Loadunload;
                    TransferStep = EnumTransferStep.MoveToLoad;
                    EnrouteState = CommandState.LoadEnroute;
                    break;
                case EnumAgvcTransCommandType.Override:
                    break;
                case EnumAgvcTransCommandType.MoveToCharger:
                    CompleteStatus = CompleteStatus.MoveToCharger;
                    TransferStep = EnumTransferStep.MoveToAddress;
                    EnrouteState = CommandState.None;
                    break;
                case EnumAgvcTransCommandType.Else:
                    break;
                case EnumAgvcTransCommandType.Scan: //liu 0416 scan
                    CompleteStatus = CompleteStatus.Scan;
                    TransferStep = EnumTransferStep.MoveToLoad;
                    EnrouteState = CommandState.LoadEnroute;
                    break;
                default:
                    break;
            }
        }

        public CommandActionType GetCommandActionType()
        {
            switch (AgvcTransCommandType)
            {
                case EnumAgvcTransCommandType.Move:
                    return CommandActionType.Move;
                case EnumAgvcTransCommandType.Load:
                    return CommandActionType.Load;
                case EnumAgvcTransCommandType.Unload:
                    return CommandActionType.Unload;
                case EnumAgvcTransCommandType.LoadUnload:
                    return CommandActionType.Loadunload;
                case EnumAgvcTransCommandType.Override:
                    return CommandActionType.Override;
                case EnumAgvcTransCommandType.MoveToCharger:
                    return CommandActionType.Movetocharger;
                case EnumAgvcTransCommandType.Scan: //liu 0416 scan
                    return CommandActionType.Scan;
                    break;
                case EnumAgvcTransCommandType.Else:
                default:
                    return CommandActionType.Home;
            }
        }

        //protected void LogException(string source, string exMsg)
        //{
        //    MirleLogger.Instance.Log(new LogFormat("MainError", "5", source, "Device", "CarrierID", exMsg));
        //}

        public bool IsAbortByAgvc()
        {
            switch (CompleteStatus)
            {
                case CompleteStatus.InterlockError:
                case CompleteStatus.IdreadFailed:
                case CompleteStatus.IdmisMatch:
                case CompleteStatus.VehicleAbort:
                case CompleteStatus.Abort:
                case CompleteStatus.Cancel:
                case CompleteStatus.EmptyRetrieval: //liu
                case CompleteStatus.DoubleStorage:
                    return true;
                case CompleteStatus.Move:
                case CompleteStatus.Load:
                case CompleteStatus.Unload:
                case CompleteStatus.Loadunload:
                case CompleteStatus.MoveToCharger:
                case CompleteStatus.Scan://liu 0416 scan
                default:
                    return false;
            }
        }
    }
}
