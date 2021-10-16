using Newtonsoft.Json;

namespace Mirle.Agv.MiddlePackage.Umtc
{
    #region MainEnums

    public enum CommandState
    {
        None,
        LoadEnroute,
        UnloadEnroute
    }

    public enum EnumSectionType
    {
        None,
        Horizontal,
        Vertical,
        R2000
    }
    public enum EnumMoveToEndReference
    {
        Load,
        Unload,
        Avoid
    }

    public enum EnumCommandDirection
    {
        None,
        Forward,
        Backward
    }

    public enum EnumTransferStepType
    {
        Move,
        MoveToCharger,
        Load,
        Unload,
        Empty
    }

    public enum EnumAgvcTransCommandType
    {
        Move,
        Load,
        Unload,
        LoadUnload,
        Override,
        MoveToCharger,
        Scan,
        Else
    }

    public enum EnumAutoState
    {
        Auto,
        Manual,
        None
    }

    public enum EnumCommandInfoStep
    {
        Begin,
        End
    }

    public enum EnumLoginLevel
    {
        Op,
        Engineer,
        Admin,
        OneAboveAll
    }

    public enum EnumCmdNum
    {
        Cmd000_EmptyCommand = 0,
        Cmd11_CouplerInfoReport = 11,
        Cmd31_TransferRequest = 31,
        Cmd32_TransferCompleteResponse = 32,
        Cmd35_CarrierIdRenameRequest = 35,
        Cmd36_TransferEventResponse = 36,
        Cmd37_TransferCancelRequest = 37,
        Cmd38_GuideInfoResponse = 38,
        Cmd39_PauseRequest = 39,
        Cmd41_ModeChange = 41,
        Cmd43_StatusRequest = 43,
        Cmd44_StatusRequest = 44,
        Cmd45_PowerOnoffRequest = 45,
        Cmd51_AvoidRequest = 51,
        Cmd52_AvoidCompleteResponse = 52,
        Cmd71_RangeTeachRequest = 71,
        Cmd72_RangeTeachCompleteResponse = 72,
        Cmd74_AddressTeachResponse = 74,
        Cmd91_AlarmResetRequest = 91,
        Cmd94_AlarmResponse = 94,
        Cmd111_CouplerInfoResponse = 111,
        Cmd131_TransferResponse = 131,
        Cmd132_TransferCompleteReport = 132,
        Cmd133_ControlZoneCancelResponse = 133,
        Cmd134_TransferEventReport = 134,
        Cmd135_CarrierIdRenameResponse = 135,
        Cmd136_TransferEventReport = 136,
        Cmd137_TransferCancelResponse = 137,
        Cmd139_PauseResponse = 139,
        Cmd141_ModeChangeResponse = 141,
        Cmd143_StatusResponse = 143,
        Cmd144_StatusReport = 144,
        Cmd145_PowerOnoffResponse = 145,
        Cmd151_AvoidResponse = 151,
        Cmd152_AvoidCompleteReport = 152,
        Cmd171_RangeTeachResponse = 171,
        Cmd172_RangeTeachCompleteReport = 172,
        Cmd174_AddressTeachReport = 174,
        Cmd191_AlarmResetResponse = 191,
        Cmd194_AlarmReport = 194,
    }

    public enum EnumAlarmLevel
    {
        Warn,
        Alarm
    }

    public enum EnumCstIdReadResult
    {
        Normal,
        Mismatch,
        Fail
    }

    public enum EnumBeamDirection
    {
        Front,
        Back,
        Left,
        Right
    }

    public enum EnumAseMoveCommandIsEnd
    {
        None,
        End,
        Begin
    }

    public enum EnumAddressDirection
    {
        None = 0,
        Left = 1,
        Right = 2
    }

    public enum EnumSlotSelect
    {
        None,
        Left,
        Right,
        Both
    }


    public enum PsMessageType
    {
        P,
        S
    }

    public enum EnumRobotState
    {
        Idle,
        Busy,
        Error
    }

    public enum EnumMoveState
    {
        Idle,
        Busy,
        Pause,
        Block,
        Error,
        ReserveStop
    }

    public enum EnumCarrierSlotState
    {
        Empty,
        Loading,
        PositionError,
        ReadFail
    }

    public enum EnumMoveComplete
    {
        Success,
        Fail,
        Pause,
        Cancel
    }

    public enum EnumSlotNumber
    {
        L,
        R
    }

    public enum EnumAddressArrival
    {

        Fail,
        Arrival,
        EndArrival
    }

    public enum EnumIsExecute
    {
        Keep,
        Go
    }

    public enum EnumLDUD
    {
        LD,
        UD,
        None
    }

    public enum EnumChargingStage
    {
        Idle,
        ArrivalCharge,
        WaitChargingOn,
        LowPowerCharge,
        DisCharge,
        WaitChargingOff
    }

    public enum EnumTransferStep
    {
        Idle,
        MoveToAddress,
        MoveToLoad,
        MoveToUnload,
        MoveToAvoid,
        MoveToAvoidWaitArrival,
        AvoidMoveComplete,
        MoveToAddressWaitArrival,
        MoveToAddressWaitEnd,
        WaitMoveArrivalVitualPortReply,
        LoadArrival,
        WaitLoadArrivalReply,
        Load,
        LoadWaitEnd,
        WaitLoadCompleteReply,
        WaitCstIdReadReply,
        UnloadArrival,
        WaitUnloadArrivalReply,
        Unload,
        UnloadWaitEnd,
        WaitUnloadCompleteReply,
        TransferComplete,
        MoveFail,
        WaitOverrideToContinue,
        RobotFail,
        Abort
    }

    public enum EnumRobotEndType //liu++
    {
        Finished,
        InterlockError,
        RobotError,
        EmptyRetrival,
        DoubleStorage,
    }

    public enum EnumAgvcReplyCode
    {
        Accept,
        Reject,
        Unknow
    }

    public enum EnumMoveStopType
    {
        None,
        NormalStop,
        AvoidStop,
    }

    public enum EnumMiddlerAlarmCode
    {
        //AlreadyHaveCmd = 000001,
        //LowPowerCharging = 000002,
        //LoadingOff = 000003,
        CarrierIdReadFail = 000004,
        //CarrierIdIsError = 000005,
        //MoveFinishFail = 000006,
        //LoadingOn = 000007,
        //ForkCommandExist = 000008,
        //VehicleCannotUnload = 000009,
        //ForkCommandExist_2 = 000010,
        PositionLostWithCmd = 000011,
        //ChargeDirectionError = 000012,
        //StartChargeTimeout = 000013,
        //StopChargeTimeout = 000014,
        //VehicleCannotload = 000015,
        //AlreadyHaveCst = 000016,
        NoCstToUnload = 000017,
        MapCheckFail = 000018,
        //IsNotTransferCannotOverride = 000019,
        //IsNotMoveStepCannotOverride = 000020,
        //IsNotPauseCannotOverride = 000021,
        //OverrideUnloadAddressUnmatch = 000022,
        //OverrideLoadAddressUnmatch = 000023,

        //OverrideToUnloadIsEmpty = 000024,
        //OverrideToLoadIsEmpty = 000025,
        //CheckOverrideException = 000026,
        //ForkNotHome = 000027,
        CarrierIdMisMatch = 000028,
        //IsCarrierIdReadReplyTimeout = 000029,
        //VehicleHasAlarm = 000030,
        //ManualToAutoFail = 000031,
        //ManualToAutoFail_2 = 000032,
        //IsNotTransferCannotAvoid = 000033,
        //IsNotMoveStepCannotAvoid = 000034,
        //IsNotPauseCannotAvoid = 000035,
        CheckAvoidException = 000036,
        AgvcCallEms = 000037,
        //SendRecvTimeout = 000038,
        //AgvlError = 000040,
        //NearlyAddressInNoSection = 000042,
        //AgvlTryAutoFail = 000043,
        //SectionLostPreAuto = 000044,
        //AddressLostPreAuto = 000045,
        //MoveStateErrorPreAuto = 000046,
        //RobotStateErrorPreAuto = 000047,
        //RobotIsNotHomePreAuto = 000048,
        //InterlockError = 000049,
        //VisitStepPrecheckException = 000050,
        CstPositionError = 000051,
        //RecvRobotCommandEnd = 000052,
        //RobotErrorBeforeLDUD = 000053,
        InitialPositionError = 000054,
        IdleTimeout = 000055,
        //AgvcDisconnect = 000056,
        //LocalDisconnect = 000057,
    }

    #endregion

    public static class ExtensionMethods
    {
        public static string GetJsonInfo(this object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
    }
}
