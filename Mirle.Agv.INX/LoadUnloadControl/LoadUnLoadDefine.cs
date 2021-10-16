namespace Mirle.Agv
{
    public enum EnumUMTCTag
    {
        P軸補正_正極限,
        P軸補正_負極限,
        Theta補正_正極限,
        Theta補正_負極限,
        Z軸補正_正極限,
        Z軸補正_負極限,
        Y補正_正極限,
        Y補正_負極限,
    }

    public enum EnumUMTCLoadUnloadStatus
    {
        Step0_Idle = 0,
        Step1_檢查Loading = 1,
        Step2_CheckAlignmentValue = 2,
        Step3_ResetPIOAndCheckESAndHO = 3,
        Step4_確認Encoder補正範圍 = 4,
        Step5_ServoOn = 5,
        Step6_WaitServoOn = 6,
        Step7_Busy_Z軸移動 = 7,
        Step8_Wait_等待Z軸移動完成 = 8,
        Step9_PIO_PIOStart = 9,
        Step10_Busy_P軸Theta軸補正 = 10,
        Step11_Wait_等待P軸Theta軸補正完成 = 11,
        Step12_PIO_WaitReady = 12,
        Step13_Busy_RollerStart = 13,
        Step14_Busy_Roller_Sensor1 = 14,
        Step15_Busy_Roller_Sensor2 = 15,
        Step16_Busy_Roller_Sensor3 = 16,
        Step17_Wait_Roller_Stop = 17,
        Step18_PIO_PIOContinue = 18,
        Step19_ReadCSTID = 19,
        Step20_Busy_P軸Theta軸回Home = 20,
        Step21_Wait_等待P軸Theta軸回Home完成 = 21,
        Step22_PIO_WaitEnd = 22,
        Step23_Busy_Z軸回Home = 23,
        Step24_Wait_等待Z軸回Home完成 = 24,
        Step25_後CheckAlignmentValue = 25,

        Step26_LoadUnloadEnd = 26,
        ErrorStep1_Busy_AllStop = 101,
        ErrorStep2_Wait_TwoSecond = 102,
        ErrorStep3_Wait_AllStop = 103,
    }

    public enum EnumLoadUnloadHomeReturnCode
    {
        OK,
        Timeout,
        StopRequest,
    }

    public enum EnumLoadUnloadAxisCommandType
    {
        ServoOn,
        PositionCommand,
        VelocityCommand,
        StopCommand,
        PlausUnit,
        TargetPosition,
        Velocity,
        Acceleration,
        Deceleration,
        Home,
    }

    public enum EnumChargingStatus
    {
        Idle,
        SendCharingCommand,
        WaitOPOn,
        WaitA,
        Charging,
        SendStopCharging,
        WaitHPOn,
    }

    public enum EnumLoadUnloadAxisName
    {
        Z軸,
        Z軸_Slave,
        P軸,
        Theta軸,
        Roller,
        Y軸,
    }

    public enum EnumLoadUnload
    {
        Load,
        Unload,
        ReadCSTID,
    }

    public enum EnumLoadUnloadJogSpeed
    {
        High,
        Normal,
        Low,
    }

    public enum EnumAlignmentSensorType
    {
        LaserF_Right,
        LaserB_Right,
        BarcodeReader_Right,
        LaserF_Left,
        LaserB_Left,
        BarcodeReader_Left,
    }

    public enum EnumPIOType
    {
        None,
    }

    public enum EnumPIOStatus
    {
        None,
        Idle,
        T0,
        T1,
        T2,
        T3,
        T4,
        T5,
        T6,
        T7,
        T8,
        T9,
        T10,
        TA1,
        TA2,
        TA3,
        TP1,
        TP2,
        TP3,
        TP4,
        TP5,
        Complete,
        NG,

        LoadUnloadSingalError,

        Error,
        SendAllOff,
        WaitAllOff,

    }

    public enum EnumLoadUnloadErrorLevel
    {
        None = 0,
        PrePIOError = 1,
        AfterPIOError = 2,
        AfterPIOErrorAndActionError = 3,
        Error = 4,
    }

    public enum EnumLoadUnloadControlErrorCode
    {
        None = 0,
        拒絕取放命令_資料格式錯誤 = 300000,
        拒絕取放命令_LoadUnloadControlNotReady = 300001,
        拒絕取放命令_LoadUnloadControlErrorBitOn = 300002,
        拒絕取放命令_Exception = 300003,

        取放貨主流程Exception = 300004,

        取放貨初始化失敗_MIPCTag缺少 = 300100,
        取放貨補正元件_BarcoderReader連線失敗 = 300110,
        取放貨補正元件_BarcoderReader斷線 = 300111,
        取放貨補正元件_雷射測距連線失敗 = 300120,
        取放貨補正元件_雷射測距斷線 = 300121,

        //ATS 左右RFID
        CSTIDReader_連線失敗 = 300122,
        CSTIDReader_斷線 = 300123,
        CSTIDReaderLeft_連線失敗 = 300124,
        CSTIDReaderLeft_斷線 = 300125,
        CSTIDReaderRight_連線失敗 = 300126,
        CSTIDReaderRight_斷線 = 300127,

        
        Z軸驅動器異常 = 300200,
        Z軸_Slave驅動器異常 = 300201,
        P軸驅動器異常 = 300202,
        Theta軸驅動器異常 = 300203,
        Roller驅動器異常 = 300204,

        Z軸電流過大 = 300300,
        Z軸_Slave電流過大 = 300301,
        Z軸主從Encoder落差過大 = 300302,
        Z軸Slave上極限觸發 = 300303,
        Z軸Slave下極限觸發 = 300304,
        平衡Z軸模式中 = 300305,

        Getway通訊異常_Z軸 = 300400,
        Getway通訊異常_Z軸_Slave = 300401,
        Getway通訊異常_P軸 = 300402,
        Getway通訊異常_Theta軸 = 300403,
        Getway通訊異常_Roller = 300404,

        T0_Timeout = 301000,
        T1_Timeout = 301001,
        T2_Timeout = 301002,
        T3_Timeout = 301003,
        T4_Timeout = 301004,
        T5_Timeout = 301005,
        T6_Timeout = 301006,
        T7_Timeout = 301007,
        T8_Timeout = 301008,
        T9_Timeout = 301009,
        T10_Timeout = 301010,

        TA1_Timeout = 301011,
        TA2_Timeout = 301012,
        TA3_Timeout = 301013,
        TP1_Timeout = 301014,
        TP2_Timeout = 301015,
        TP3_Timeout = 301016,
        TP4_Timeout = 301017,
        TP5_Timeout = 301018,

        取放貨中EQPIOOff = 301051,
        取放命令與EQRequest不相符 = 301052,
        RollerStop後CV訊號異常 = 301054,
        啟用PIOTimeout測試 = 301055,

        取放貨中CV檢知異常 = 301100,
        取放貨中EMS = 301101,
        取貨_LoadingOn異常 = 301102,
        放貨_LoadingOff異常 = 301103,
        AlignmentNG = 301104,
        HomeSensor未On = 301105,
        Z軸上定位未On = 301106,
        取放貨異常_MIPCCommandReturnFail = 301107,
        動作結束但不在InpositionRange = 301108,
        AlignmentValueNG = 301109,
        取放中極限觸發 = 301110,
        取放中軸異常 = 301111,
        取放中安全迴路異常 = 301112,
        RollerStopTimeout = 301113,
        CVSensor異常 = 301114,
        Loading邏輯和Sensor不相符 = 301115,
        Fork不在Home點 = 301116,
        ServoOnTimeout = 301117,
        Port站ES或HO_AVBLNotOn = 301118,
        取放貨站點資訊異常 = 301119,
        取放貨異常_Z軸升降中CVSensor異常 = 301120,
        //Allen, ATS Robot相關異常
        取放貨手臂異常 = 301130,   //Allen
        Robot定點式Mark定位失敗 = 301131,
        Robot伺服式Mark定位失敗 = 301132,
        AGV與Mark相對位置超過範圍 = 301133,
        Robot補償值穩定度不足 = 301134,
        Robot姿態異常 = 301135,


        回Home失敗_CVSensor狀態異常 = 302000,
        回Home失敗_CST回CV_Timeout = 302001,
        回Home失敗_P軸不在Home = 302002,
        回Home失敗_Theta軸不再Home = 302003,
        回Home失敗_Z軸不在上定位 = 302004,
        回Home失敗_ServoOn_Timeout = 302005,
        回Home失敗_Exception = 302006,
        回Home失敗_MIPC指令失敗 = 302007,
        回Home失敗_人員觸發停止 = 302008,
        回Home失敗_極限Sensor未On = 302009,
        回Home失敗_MovingTimeout = 302010,
        回Home失敗_HomeSensor未on = 302011,
        回Home失敗_HomeSensor未off = 302012,
        回Home失敗_Y軸不在極限 = 302013,

        ChargingConfrimSensorNotOn = 310000,
        ChargerStationNotService = 310001,
        ChargerStationWaitOPTimeout = 310002,
        ChargerStationWaitChargingTimeout = 310003,
        ChargerStationAlarm = 310004,
        ChargerStationWarning = 310005,
        ChargerStationFullCharging = 310006,
        ChargingTimeout = 310007,
        ChargingWith_OPOff = 310008,
        ChargingWith_SensorEMO = 310009,
        ChargingOverA = 310010,
        Charging溫度過高 = 310011,
        ChargerStationWaitHPTimeout = 310012,
        ChargerError_AGV不在充電站或迷航中 = 310013,
        ChargerError_RFPIO切換失敗 = 310014,
        ChargerRFPIO_連線失敗 = 310015,
        //Charging
    }

    public enum EnumATSLoadUnloadStatus
    {
        Step0_Idle = 0,
        Step1_檢查Loading = 101,
        Step2_Robot_Move_Initial = 102,
        Step3_Wait_Robot_Initial = 103,
        Step4_PIO_PIOStart = 104,
        Step5_PIO_WaitReady = 105,
        Step6_Robot_Move_LDULD = 106,
        Step7_Wait_Robot_LDULD = 107,
        Step_PIO_PIOContinue = 108,
        Step_PIO_WaitEnd = 109,
        Step_ReadCSTID = 110,
        Step_Robot_Move_CARPos = 111,
        Step_Wait_Robot_CARPos = 112,
        Step_LoadUnload_Scuess = 113,
        Step_LoadUnload_Fail = 114,
        Step_Remove_Robot_Command = 115,
        Step_Removing_Robot_Command = 116,
        Step_Wait_Remove_Robot_Command = 117,

    }

    public enum EnumATSRobotStatus
    {
        Robot_unconnect = -1,   /* -1 no connection */
        Robot_rev,              /* 0  rev val:0 */
        Robot_init_finish,      /* 1  init finish, wait open project(init step 2) */
        Robot_project_open,     /* 2  openning project(init step 3~4) */
        Robot_project_init,     /* 3  project init(init step 5~8) */
        Robot_ready,            /* 4  command enable */
        Robot_execution,        /* 5  in action */
        Robot_project_err,      /* 6  project err */
        Robot_self_err,         /* 7  robot err */
        Robot_unknow,           /* 8  robot unknow status */
    }

    public enum EnumATSRobotLight
    {
        ProgNotRunning = 3,
        ProgRunnig =4,
        Alarm = 9,
    }

    public enum EnumATSRobotProgErrorCode
    {
        None = 0,
        VisionFixedFail = 30,
        VisionServoingFail = 31,
        TMVarNG = 32,
        TMVar2NG = 33,
        PositonError = 40,
        
        //Allen, 待補齊
    }
}
