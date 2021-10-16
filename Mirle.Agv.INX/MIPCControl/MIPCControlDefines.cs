namespace Mirle.Agv
{
    public enum EnumMIPCServoOnOffValue
    {
        ServoOn = 1,
        ServoOff = 2,
    }

    // 安全保護區域.
    public enum EnumMovingDirection
    {
        Initial,
        None,
        Front,
        Back,
        Left,
        Right,
        FrontLeft,
        FrontRight,
        BackLeft,
        BackRight,
        SpinTurn,
        LoadUnload,
    }

    // 叫聲.
    public enum EnumBuzzerType
    {
        Initial,
        None,
        Moving,
        MoveShift,
        Turning,
        SpinTurn,

        LoadUnload,

        Charging,

        Warn,
        Alarm,
    }


    // 方向燈.
    public enum EnumDirectionLight
    {
        Initial,

        // Move
        None,
        Front,
        Back,
        Left,
        Right,
        SpinTurn,
        FrontLeft,
        FrontRight,
        BackLeft,
        BackRight,
        RTurnLeft,
        RTurnRight,

        // Fork
        LoadUnload,

        // Other
        Charging,
    }

    public enum EnumSafetySensorType
    {
        Initial,
        None,
        BeamSensor,
        AreaSensor,
        Bumper,
        EMO,
    }

    public enum EnumDeviceType
    {
        None,
        Tim781,
        Bumper,
        EMO,
        Sensor,
    }

    public enum EnumMIPCConnectType
    {
        None,
        TCP_Server,
        TCP_Client,
    }

    public enum EnumDataType
    {
        UInt32,
        Int32,
        Double_1,
        Float,
        Boolean
    }

    public enum EnumIOType
    {
        Write,
        Read
    }

    public enum EnumSendAndRecieve
    {
        None,
        OK,
        Error
    }

    public enum EnumMecanumIPCdefaultTag
    {
        #region 設定MIPC時間Tag.
        SetMIPCTime_Year,
        SetMIPCTime_Month,
        SetMIPCTime_Day,
        SetMIPCTime_Hour,
        SetMIPCTime_Minute,
        SetMIPCTime_Second,
        SetMIPCTime_SetEnd,
        #endregion

        #region 晟淇的走行控制命令.
        Command_MapX,
        Command_MapY,
        Command_MapTheta,
        Command_線速度,
        Command_線加速度,
        Command_線減速度,
        Command_線急跳度,
        Command_角速度,
        Command_角加速度,
        Command_角減速度,
        Command_角急跳度,
        Command_Start,

        Command_Stop,
        #endregion

        #region 晟淇的SetPosition.
        SetPosition_MapX,
        SetPosition_MapY,
        SetPosition_MapTheta,
        SetPosition_TimeStmap,
        SetPosition_Start,
        #endregion

        #region 被上頭說不要先做的轉彎命令.
        Turn_MapX,
        Turn_MapY,
        Turn_MapTheta,
        Turn_R,
        Turn_Theta,
        Turn_Velocity,
        Turn_MovingAngle,
        Turn_DeltaTheta,
        Turn_Start,
        #endregion

        #region 晟淇的麥克阿姆輪回售資料?
        Feedback_X,
        Feedback_Y,
        Feedback_Theta,
        Feedback_線速度,
        Feedback_線速度方向,
        Feedback_線加速度,
        Feedback_線減速度,
        Feedback_線急跳度,
        Feedback_角速度,
        Feedback_角加速度,
        Feedback_角減速度,
        Feedback_角急跳度,
        Feedback_MoveStatus,
        Feedback_TimeStamp,

        Feedback_X_VelocityCommand,
        Feedback_X_VelocityFeedback,

        Feedback_Y_VelocityCommand,
        Feedback_Y_VelocityFeedback,

        Feedback_Theta_VelocityCommand,
        Feedback_Theta_VelocityFeedback,

        XVelocityError,
        YVelocityError,
        ThetaVelocityError,

        Slam_XVelocityError,
        Slam_YVelocityError,
        Slam_ThetaVelocityError,
        #endregion

        ResetAlarm,

        MIPCVersion,
        MotionVersion,

        EMS,

        Battery_SOC,
        Battery_V,
        Battery_A,
        Battery_溫度1,
        Battery_溫度2,

        HeartBeat_ByPass,
        Heartbeat_IPC,
        Heartbeat_System,
        Heartbeat_Motion,
        ServoOnOff,

        Slam數值補正走行關閉,

        MIPCAlarmCode_1,
        MIPCAlarmCode_2,
        MIPCAlarmCode_3,
        MIPCAlarmCode_4,
        MIPCAlarmCode_5,
        MIPCAlarmCode_6,
        MIPCAlarmCode_7,
        MIPCAlarmCode_8,
        MIPCAlarmCode_9,
        MIPCAlarmCode_10,
        MIPCAlarmCode_11,
        MIPCAlarmCode_12,
        MIPCAlarmCode_13,
        MIPCAlarmCode_14,
        MIPCAlarmCode_15,
        MIPCAlarmCode_16,
        MIPCAlarmCode_17,
        MIPCAlarmCode_18,
        MIPCAlarmCode_19,
        MIPCAlarmCode_20,

        RS485_Error,

        MIPC_Test1,
        MIPC_Test2,
        MIPC_Test3,
        MIPC_Test4,
        MIPC_Test5,
        MIPC_Test6,
        MIPC_Test7,
        MIPC_Test8,
        MIPC_Test9,
        MIPC_Test10,

        MIPC_Test11,
        MIPC_Test12,
        MIPC_Test13,
        MIPC_Test14,
        MIPC_Test15,
        MIPC_Test16,
        MIPC_Test17,
        MIPC_Test18,
        MIPC_Test19,
        MIPC_Test20,


        MIPC_Test21,
        MIPC_Test22,
        MIPC_Test23,
        MIPC_Test24,
        MIPC_Test25,
        MIPC_Test26,
        MIPC_Test27,
        MIPC_Test28,
        MIPC_Test29,
        MIPC_Test30,

        #region 遙控器的命令輸入?
        JoystickOnOff,
        Joystick_LineVelocity,
        Joystick_LineAcc,
        Joystick_LineDec,
        Joystick_ThetaVelocity,
        Joystick_ThetaAcc,
        Joystick_ThetaDec,
        #endregion

        ShutDown,
        ShutDown_Nag,
        MIPCReady,
        MIPCNotReady,
        SafetyRelay,

        ShutDown_SOC,
        ShutDown_V,

        Light_Red,
        Light_Yellow,
        Light_Green,

        RGBLight_Red,
        RGBLight_Green,
        RGBLight_Blue,

        Reset_Front,
        Reset_Back,
        Start_Front,
        Start_Back,
        BrakeRelease_Front,
        BrakeRelease_Back,

        AGV_Type,

        IPC_Alive,
        Motion_CantMove,

        Battery_Alarm,
        Meter_A,
        Meter_V,
        Meter_W,
        Meter_WH,

        Z軸Encoder歪掉,

        Z軸EncoderOffset,
        Z軸_SlaveEncoderOffset,
        P軸EncoderOffset,
        Theta軸EncoderOffset,
        Y軸EncoderOffset, //liu
        Encoder已回Home,

        SetPosition_OriginX,
        SetPosition_OriginY,
        SetPosition_OriginTheta,
        SetPosition_Origin_OK,

        #region 電芯?
        Cell_1,
        Cell_2,
        Cell_3,
        Cell_4,
        Cell_5,
        Cell_6,
        Cell_7,
        Cell_8,
        Cell_9,
        Cell_10,
        Cell_11,
        Cell_12,
        Cell_13,
        Cell_14,
        Cell_15,
        #endregion

        #region 走行輪子資料.
        XFL_Encoder,
        XFL_RPM,
        XFL_DA,
        XFL_QA,
        XFL_V,
        XFL_ServoStatus,
        XFL_EC,
        XFL_MF,
        XFL_SR,
        XFL_GetwayError,
        XFL_溫度,
        XFL_VelocityFeedback,
        XFL_VelocityCommand,
        XFL_PWM,
        XFL_Driver_Encoder,
        XFL_Driver_PWM,

        XFR_Encoder,
        XFR_RPM,
        XFR_DA,
        XFR_QA,
        XFR_V,
        XFR_ServoStatus,
        XFR_EC,
        XFR_MF,
        XFR_SR,
        XFR_GetwayError,
        XFR_溫度,
        XFR_VelocityFeedback,
        XFR_VelocityCommand,
        XFR_PWM,
        XFR_Driver_Encoder,
        XFR_Driver_PWM,

        XRL_Encoder,
        XRL_RPM,
        XRL_DA,
        XRL_QA,
        XRL_V,
        XRL_ServoStatus,
        XRL_EC,
        XRL_MF,
        XRL_SR,
        XRL_GetwayError,
        XRL_溫度,
        XRL_VelocityFeedback,
        XRL_VelocityCommand,
        XRL_PWM,
        XRL_Driver_Encoder,
        XRL_Driver_PWM,

        XRR_Encoder,
        XRR_RPM,
        XRR_DA,
        XRR_QA,
        XRR_V,
        XRR_ServoStatus,
        XRR_EC,
        XRR_MF,
        XRR_SR,
        XRR_GetwayError,
        XRR_溫度,
        XRR_VelocityFeedback,
        XRR_VelocityCommand,
        XRR_PWM,
        XRR_Driver_Encoder,
        XRR_Driver_PWM,
        #endregion

        #region CV資料.
        Z軸_Encoder,
        Z軸_RPM,
        Z軸_DA,
        Z軸_QA,
        Z軸_V,
        Z軸_ServoStatus,
        Z軸_EC,
        Z軸_MF,
        Z軸_Stop,
        Z軸_SR,
        Z軸_IP,
        Z軸_GetwayError,
        Z軸_HM1,
        Z軸_HM7,
        Z軸_溫度,
        Z軸_PositionCommand,

        Z軸_Slave_Encoder,
        Z軸_Slave_RPM,
        Z軸_Slave_DA,
        Z軸_Slave_QA,
        Z軸_Slave_V,
        Z軸_Slave_ServoStatus,
        Z軸_Slave_EC,
        Z軸_Slave_MF,
        Z軸_Slave_Stop,
        Z軸_Slave_SR,
        Z軸_Slave_IP,
        Z軸_Slave_GetwayError,
        Z軸_Slave_HM1,
        Z軸_Slave_HM7,
        Z軸_Slave_溫度,

        P軸_Encoder,
        P軸_RPM,
        P軸_DA,
        P軸_QA,
        P軸_V,
        P軸_ServoStatus,
        P軸_EC,
        P軸_MF,
        P軸_Stop,
        P軸_SR,
        P軸_IP,
        P軸_GetwayError,
        P軸_HM1,
        P軸_HM7,
        P軸_溫度,
        P軸_PositionCommand,

        Theta軸_Encoder,
        Theta軸_RPM,
        Theta軸_DA,
        Theta軸_QA,
        Theta軸_V,
        Theta軸_ServoStatus,
        Theta軸_EC,
        Theta軸_MF,
        Theta軸_Stop,
        Theta軸_SR,
        Theta軸_IP,
        Theta軸_GetwayError,
        Theta軸_HM1,
        Theta軸_HM7,
        Theta軸_溫度,
        Theta軸_PositionCommand,

        Roller_Encoder,
        Roller_RPM,
        Roller_DA,
        Roller_QA,
        Roller_V,
        Roller_ServoStatus,
        Roller_EC,
        Roller_MF,
        Roller_Stop,
        Roller_SR,
        Roller_IP,
        Roller_GetwayError,
        Roller_HM1,
        Roller_HM7,
        Roller_溫度,

        Y軸_Encoder,
        Y軸_RPM,
        Y軸_DA,
        Y軸_QA,
        Y軸_V,
        Y軸_ServoStatus,
        Y軸_EC,
        Y軸_MF,
        Y軸_Stop,
        Y軸_SR,
        Y軸_IP,
        Y軸_GetwayError,
        Y軸_HM1,
        Y軸_HM7,
        Y軸_溫度,
        Y軸_PositionCommand,
        #endregion

        Auto_Signal,
        Manual_Signal,
        BrakeRelease,
    }

    public enum DefaultAxisTag
    {
        Encoder,
        RPM,
        DA,
        QA,
        V,
        VelocityFeedback,
        VelocityCommand,
        PWM,
        ServoStatus,
        EC,
        MF,
        Stop,
        SR,
        IP,
        GetwayError,
        溫度,
        Driver_Encoder,
        Driver_RPM,
    }

    public enum EnumMIPCControlErrorCode
    {
        MIPC初始化失敗 = 200000,
        MIPC連線失敗 = 200001,
        MIPC斷線 = 200002,
        MIPC通訊異常 = 200003,
        MIPC回傳資料異常 = 200004,
        ByPass聲音燈號IO自動傳送 = 200005,

        MIPC_DeviceHeartBeatLoss = 200100,
        MIPC_IPCHeartBeatLoss = 200101,
        MIPC_Motion_EMS = 200102,
        MIPC_SLAM誤差過大 = 200103,
        MIPC_SLAM過久沒更新 = 200104,
        MIPC_走行驅動器異常 = 200105,
        MIPC_電池低電壓斷電 = 200106,
        MIPC_四輪ServoOnOff不同步 = 200107,
        MIPC_驅動器過電流 = 200108,
        MIPC_速度追隨誤差過大 = 200109,

        MIPC_Alarm11 = 200110,
        MIPC_超過命令速度上限 = 200111,
        MIPC_斷動力電 = 200112,
        MIPC_IPCEMS = 200113,
        MIPC_SetPosition角度誤差過大 = 200114,
        MIPC_Alarm16 = 200115,
        MIPC_Alarm17 = 200116,
        MIPC_Alarm18 = 200117,
        MIPC_Alarm19 = 200118,
        MIPC_DeviceDisconnectCounter = 200119,

        LowBattery_SOC = 200200,
        LowBattery_V = 200201,
        ShutDown_LowBattery_SOC = 200202,
        ShutDown_LowBattery_V = 200203,
        SafetyRelayNotOK = 200204,
        ShutDown_BatteryTemp = 200205,
        BatteryWarningTemp = 200206,

        電池通訊異常 = 200207,
        電表通訊異常 = 200208,
        溫度通訊異常 = 200209,
        電池BMS異常 = 200210,

        Getway通訊異常_XFL = 200300,
        Getway通訊異常_XFR = 200301,
        Getway通訊異常_XRL = 200302,
        Getway通訊異常_XRR = 200303,

        SensorSafety_AlarmByPass = 201000,
        SensorSafety_SafetyByPass = 201001,
        SensorSafety_停止訊號Timeout = 201002,

        EMO觸發 = 201100,

        Bumper觸發 = 201200,

        Sensor觸發 = 201300,

        AreaSensorAlarm = 201400,
        AreaSensor觸發 = 201401,

        解剎車中 = 202001,
    }

    public enum EnumMIPCSocketName
    {
        Normal,
        Polling,
        MotionCommand
    }

    public enum EnumVehicleSafetyAction
    {
        Normal = 0,
        LowSpeed_High = 1,
        LowSpeed_Low = 2,
        SlowStop = 3,
        EMS = 4,
    }

    public enum EnumSafetyLevel
    {
        Alarm = 8,
        Warn = 7,
        EMO = 6,
        IPCEMO = 5,
        EMS = 4,
        SlowStop = 3,
        LowSpeed_Low = 2,
        LowSpeed_High = 1,
        Normal = 0,
    }


    public enum EnumVehicleStopLevel
    {
        None,
        Normal,
        EMS,
        EMO
    }
}
