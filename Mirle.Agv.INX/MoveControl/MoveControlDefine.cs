namespace Mirle.Agv
{
    public enum EnumEncoderAndVelocityMismach
    {
        VelocityPos,
        VelocityNag,
        None,
    }

    public enum EnumMoveCommandStartStatus
    {
        WaitStart,
        Start,
        Reporting,
        End
    }

    public enum EnumLineReviseType
    {
        None,
        Theta,
        SectionDeviation
    }

    public enum EnumCommandType
    {
        //STurn,
        //RTurn,
        SpinTurn,
        Vchange,
        ChangeSection,
        //ReviseOpen,
        //ReviseClose,
        Move,
        SlowStop,
        Stop,
        End
    }

    public enum EnumErrorStopReLocateStep
    {
        None,
        ErrorStop,
        FindSectionThreadEnd,
    }

    #region Motion相關.

    public enum EnumAxisStatus
    {
        Normal = 0,
        Error = 1
    }

    public enum EnumAxisMoveStatus
    {
        None = 4,         // 空狀態.
        PreMove = 2,      // 下命令了,但是撈取資料還不是運動中.
        Move = 1,         // 運動中.
        PreStop = 3,      // 下停止命令了,但是撈取資料還不是停止中.
        Stop = 0,         // 停止中.
        PreStop_Force = 5 // 下停止命令,但是下的瞬間還在PreMove, 強制等到時間到才取消Pre訊號.
    }

    public enum EnumDefaultAxisName
    {
        XFL,
        XFR,
        XRL,
        XRR,
    }

    public enum EnumDefaultAxisNameChinese
    {
        左前輪 = EnumDefaultAxisName.XFL,
        右前輪 = EnumDefaultAxisName.XFR,
        左後輪 = EnumDefaultAxisName.XRL,
        右後輪 = EnumDefaultAxisName.XRR
    }
    #endregion

    #region LocateControl相關.
    public enum EnumMIPCSetPostionStep
    {
        Step0_WaitLocateReady,
        Step1_WaitMIPCDataOK,
        Ready
    }
    #endregion

    #region 資料格式相關.
    public enum EnumByteChangeType
    {
        LittleEndian,
        BigEndian
    }
    #endregion

    #region 模擬相關.
    public enum EnumSimulateVelocityType
    {
        AccJerkUp,      // 加速段 & Acc增加.
        Accing,         // 加速段 Acc不變.
        AccJerkDown,    // 加速段 & Acc減少.
        Isokinetic,     // 等速段.
        DecJerkUp,      // 減速段 & Acc增加.
        Decing,         // 減速段 Acc不變.
        DecJerkDown     // 減速段 & Acc減少.
    }
    #endregion

    public enum EnumMoveStatus
    {
        Moving = 100,
        Stop = 101,
        STurn = 102,
        RTurn = 103,
        SpinTurn = 104,
        Error = 201
    }

    public enum EnumMoveComplete
    {
        End,
        Error,
        Cancel
    }

    public enum EnumLoadUnloadComplete
    {
        None,
        End,
        Error,
        Interlock,
        EmptyRetrival,
        DoubleStorage,
    }

    public enum EnumMoveControlSafetyType
    {
        OntimeReviseTheta,
        OntimeReviseSectionDeviationLine,
        VChangeSafetyDistance,
    }

    public enum EnumSensorSafetyType
    {
        ChargingCheck,
        ForkHomeCheck,
    }

    public enum EnumEndGetPlcDataStatus
    {
        Wait,
        NoData,
        NeedRevise,
        NeedRevise_OnlyX,
        Warning,
        OK
    }

    public enum EnumMoveStartType
    {
        FirstMove,
        ChangeDirFlagMove,
        ReserveStopMove,
        SensorStopMove
    }

    public enum EnumSlowStopType
    {
        ChangeMovingAngle,
        End
    }

    public enum EnumVChangeType
    {
        MoveStart,
        Normal,
        SensorSlow,
        EQ,
        SlowStop,
        TurnOut
    }

    public enum EnumDefaultAction
    {
        ST,
        BST,
        End,
        SlowStop,
        SpinTurn,
        StopTurn
    }

    public enum EnumActionType
    {
        None,
        FrontOrTurn,
        BackOrBackTurn,
        End
    }

    public enum EnumTurnType
    {
        None,
        STurn,
        RTurn,
        SpinTurn,
        StopTurn
    }

    public enum EnumBITOStatus
    {
        正常,
        重定位,
        未準備好,
        初始化中,
        未取得Lidar資料,
    }

    public enum EnumLocateDriverType
    {
        None,
        AlignmentValue,
        BarcodeMapSystem,
        SLAM_Sick,
        SLAM_BITO,
    }

    public enum EnumBarcodeReaderType
    {
        None,
        Keyence,
        Datalogic
    }

    public enum EnumLocateType
    {
        Normal,
        SLAM
    }

    public enum EnumAGVPositionType
    {
        None = 0,
        Normal = 3,
        OnlyRevise = 2,
        OnlyRead = 1
    }

    public enum EnumTimeoutValueType
    {
        EnableTimeoutValue,
        DisableTimeoutValue,
        SlowStopTimeoutValue,
        EndTimeoutValue,
        SpinTurnFlowTimeoutValue,
        CloseProgameTimeoutValue,
        SafetySensorStopTimeout,
    }

    public enum EnumIntervalTimeType
    {
        MoveControlThreadInterval,
        CSVLogInterval,
        ManualFindSectionInterval,
        SetPositionInterval
    }

    public enum EnumDelayTimeType
    {
        CommandStartDelayTime,
        OntimeReviseAlarmDelayTime,
        SafetySensorStartDelayTime,
        Local_PauseStartDelayTime,
    }

    public enum EnumAxisServoOnOff
    {
        ServoOn = 1,
        ServoOff = 0
    }

    public enum EnumPositionUpdateSafteyType
    {
        None,
        Line,
        Turning,
        TurnOut
    }

    public enum EnumSlamAutoSetPosition
    {
        WaitMIPCSlamData,
        WaitSlamDataOK,
        SetPosition,
        WaitResult,
        End
    }

    public enum EnumEMSResetFlow
    {
        None = 0,
        EMS_Stopping = 1,
        EMS_WaitReset = 2,
        EMS_WaitStart = 3,
        EMS_Delaying = 4,
    }

    public enum EnumMoveCommandControlErrorCode
    {
        None = 0,

        //●100XXX : MoveControl層
        MoveControl主Thread跳Exception = 100000,
        超過觸發區間 = 100001,

        //    ●1001XX : CommandType : Move
        Move_EnableTimeout = 100100,

        //    ●1002XX : CommandType : Reserve

        //    ●1003XX : CommandType : SlowStop
        SlowStop_Timeout = 100300,

        //    ●1004XX : CommandType : TR
        STurn_入彎超速 = 100400,
        STurn_入彎過慢 = 100401,
        STurn_未開啟TR中停止 = 100402,
        STurn_未開啟STurn中重新啟動 = 100403,
        STurn_流程Timeout = 100404,

        //    ●1005XX : CommandType : RTurn
        RTurn_入彎超速 = 100500,
        RTurn_入彎過慢 = 100501,
        RTurn_未開啟RTurn中停止 = 100502,
        RTurn_未開啟RTurn中重新啟動 = 100503,
        RTurn_流程Timeout = 100504,

        //    ●1006XX : CommandType : SpinTurn
        SpinTurn_Timeout = 100600,

        //    ●1008XX : CommandType : End
        End_SecondCorrectionTimeout = 100800,
        End_ServoOffTimeout = 100801,

        //●1010XX : MoveMethod層
        MoveMethod層_DriverReturnFalse = 101000,

        //●102XXX : LocateControl層
        LocateControl初始化失敗 = 102000,

        //    ●1021XX : LocateDriver_BaroceMapSystem
        LocateDriver_BarcodeMapSystem初始化失敗 = 102100,
        LocateDriver_BarcodeMapSystem連線失敗 = 102101,
        LocateDriver_BarcodeMapSystem回傳資料格式錯誤 = 102102,
        LocateDriver_BarcodeMapSystemTriggerException = 102103,
        LocateDriver_BarcodeMapSystemError = 102104,
        
        //    ●1023XX : LocateDriver_SLAM
        LocateDriver_SLAM_初始化失敗 = 102300,
        LocateDriver_SLAM_連線失敗 = 102301,
        LocateDriver_SLAM_資料格式錯誤 = 102302,
        LocateDriver_SLAM_精度迷航 = 102303,
        LocateDriver_SLAM_未取得定位資料 = 102304,
        LocateDriver_SLAM_定位資料位移過大 = 102305,
        LocateDriver_SLAM_信心度低下 = 102306,
        
        //●103XXX : CreateCommandList層

        //    ●1030XX TransferMove
        命令分解失敗 = 103000,

        //    ●1032XX TransferMove_RetryMove

        //●104XXX : 安全保護層

        //    ●1041XX : 接受命令前的保護
        拒絕移動命令_資料格式錯誤 = 104100,
        拒絕移動命令_MoveControlNotReady = 104101,
        拒絕移動命令_MoveControlErrorBitOn = 104102,
        拒絕移動命令_充電中 = 104103,
        拒絕移動命令_Fork不在Home點 = 104104,
        拒絕移動命令_迷航中 = 104106,
        拒絕移動命令_移動命令中 = 104107,
        拒絕移動命令_不在Section上 = 104108,
        
        //    ●1042XX : 移動中的保護
        安全保護停止_Fork不在Home點 = 104200,
        安全保護停止_充電中 = 104201,
        安全保護停止_角度偏差過大 = 104202,
        安全保護停止_軌道偏差過大 = 104203,
        安全保護停止_出彎過久沒取得定位資料 = 104204,
        安全保護停止_直線過久沒取得定位資料 = 104205,
        安全保護停止_速度變化異常 = 104207,
        安全保護停止_定位Control異常 = 104209,
        安全保護停止_人為控制 = 104210,
        安全保護停止_Bumper觸發 = 104211,
        安全保護停止_EMO停止 = 104212,
        安全保護停止_SafetySensorAlarm = 104214,
        安全保護停止_MotionAlarm = 104215,
        安全保護停止_走行中安全迴路異常 = 104216,
        安全保護停止_走行中定位資料過久未更新 = 104217,

        MoveControl_ErrorBitOn = 105001,
    }

    public enum AlarmLevel
    {
        Alarm,
        Warn
    }
}
