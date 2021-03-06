using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv
{
    public enum EnumLanguage
    {
        None,
        繁體中文,
        English,
        簡體中文,
    }

    public enum EnumAGVCActionType
    {
        Move,
        Load,
        Unload,
        LoadUnload,
    }

    public enum EnumDelayType
    {
        OnDelay,
        OffDelay,
    }

    public enum EnumControlStatus
    {
        NotInitial = 403,
        Initial = 302,
        Ready = 100,
        NotReady = 300,
        Error = 301,
        ResetAlarm = 200,
        Closing = 400,
        WaitThreadStop = 401,
        Closed = 402
    }

    public enum EnumAGVType
    {
        Demo = 0,
        AGC = 1,
        UMTC = 2,
        PTI = 3,
        ATS = 4,
    }

    public enum EnumSectionAction
    {
        None,
        Idle,
        NotGetReserve,
        GetReserve
    }

    public enum EnumCommandStatus
    {
        Idle = 100,
        Initial = 300,
        Busy = 200,
        Reporting = 300
    }

    public enum EnumStageDirection
    {
        None,
        Left,
        Right
    }

    public enum EnumCstInAGVLocate
    {
        None,
        Left,
        Right
    }

    public enum EnumAutoState
    {
        Auto,
        Manual,
        PreAuto
    }

    public enum EnumLoginLevel
    {
        User = 0,
        Engineer = 1,
        Admin = 2,
        MirleAdmin = 3
    }

    public enum EnumAlarmLevel
    {
        Warn,
        Alarm
    }


    public enum EnumBeamDirection
    {
        Front,
        Back,
        Left,
        Right
    }

    public enum EnumProfacePageIndex
    {
        Main = 0,
        Move_Select = 1,
        Move_Jog = 7,
        Move_Map = 8,
        Move_DataInfo = 9,
        Move_AxisData = 10,
        Move_LocateDriver = 11,
        Move_CommandRecord = 12,
        Move_SetSlamPosition = 13,

        Fork_Select = 2,
        Fork_Jog = 14,
        Fork_Home = 15,
        Fork_Command = 16,
        Fork_Alignment = 17,
        Fork_CommandRecord = 18,
        Fork_PIO = 19,
        Fork_AxisData = 20,
        Fork_HomeSetting_UMTC = 21,

        Charging_Select = 3,
        Charging_BatteryInfo = 22,
        Charging_Command = 23,
        Charging_PIO = 24,
        Charging_Record = 25,

        IO = 4,

        Alarm = 5,

        Parameter = 6,
    }

    public enum EnumUserAction
    {
        Main_Hide,
        Main_IPCPowerOff,

        Move_Jog,
        Move_Jog_SettingAccDec,
        Move_SpecialFlow_ReviseByTarget,
        Move_SpecialFlow_ToSectionCenter,
        Move_SpecialFlow_ReviseByTargetOrLocateData,
        Move_SpecialFlow_ActionBeforeAuto_MoveToAddressIfClose,
        Move_LocalCommand,
        Move_SetSlamAddressPosition, // 踩點.
        Move_LocateDriver_TriggerChange,
        Move_SetPosition,
        Move_ForceSetSlamDataOK,
        Move_SpecialFlow_CanAutoRecovery, // Allen, 自動恢復測試

        Fork_Jog,
        Fork_Home,
        Fork_LocalCommand,
        Fork_GetAlignmentValue,
        Fork_PIOTest,
        Fork_HomeSetting,

        Charging_LocalCommand,
        Charging_ChargingTest,
        Charging_PIOTest,

        IO_IOTest,
        IO_ChangeAreaSensorDirection,

        Parameter_SafetySensor_ByPassSafety,
        Parameter_SafetySensor_ByPassAlarm,

        Parameter_BatteryConfig,
        Parameter_PIOTimeoutConfig,
        Parameter_MoveConfig,
        Parameter_MainConfig,
    }

    public enum EnumProfaceStringTag
    {
        #region 共用上.
        LoginLevel,
        User,
        Engineer,
        Admin,
        MirleAdmin,
        Login,
        Logout,
        Login_Success,
        Login_Failed,

        SOC,
        電壓,
        Warn,
        Alarm,
        Manual,
        Auto,
        AlarmCode,
        CstReadFail,
        #endregion

        #region 共用下.
        主畫面,
        走行相關,
        取放相關,
        充電資訊,
        IO監控,
        異常資訊,
        參數設定,
        #endregion

        #region Main
        程式版本資訊,
        Local版本,
        MIPC通訊板本,
        Motion版本,
        Middler版本,
        Middler,
        InitialFail,
        AGVC,
        ID,
        Step,
        無命令,
        命令開始時間,
        有貨,
        無貨,
        充電中,

        Online,
        Offline,
        Local_IP,
        MiddlerCommand,
        MoveCommand,
        LoadUnloadCommand,
        AreaSensorDirection,
        Loading,
        Charging,
        Cassette_ID,
        Hide,
        IPC關機,
        #endregion

        #region Move.
        JogPitch,
        圖資顯示,
        Slam位置設定,
        狀態監控,
        各軸資訊,
        定位裝置,
        走行命令紀錄,
        SLAM定位走行FromTo,
        無法使用搖桿,
        Auto中,
        移動命令中,
        取放命令中,
        特殊走行流程中,
        Fork_Not_Home,

        搖桿操作,
        開啟中,
        關閉中,
        開啟,
        關閉,
        AllServoOn,
        AllServoOff,
        ServoOn,
        ServoOff,
        線速度,
        線加速度,
        線減速度,
        角速度,
        角加速度,
        角減速度,
        Real,
        設定,
        清除,
        軌道偏差,
        角度偏差,
        不能Auto,
        可以Auto,
        狀態,
        停止,
        移動中,
        自動站點補正,
        自動Target補正,
        移動至路中央,
        Stop,
        Move,
        Load,
        Unload,
        LoadUnload,
        Start,
        指定Address,
        車頭角度,
        搜尋站點,
        紀錄位置,
        搜尋範圍,
        搜尋角度,
        軸代號,
        軸名稱,
        軸狀態,
        Encoder,
        RPM,
        ServoOnOff,
        EC,
        MF,
        V,
        Q軸電流,
        CommandID,
        StartTime,
        EndTime,
        Result,
        AddressID,
        一般點位,
        充電站,
        取放站,
        SlamInfo,
        認可目前位置,
        定位OK,
        補正中,
        重定位,
        定位NG,

        補正異常_距離過遠,
        重定位失敗_差距過大,
        重定位成功,

        SetPosition,
        回充電站,
        LocalCycleRun,
        Address,
        Section,
        Distance,
        MIPC,
        Locate,
        CmdStatus,
        MoveStatus,
        CmdEncoder,
        Velocity,

        Normal,
        LowSpeed_High,
        LowSpeed_Low,
        SlowStop,
        EMS,
        On,
        Off,
        轉換後座標,
        原始座標,
        最後輸出,
        信心度,
        Barcode角度,
        DriverName,
        Status,
        DataType,
        Map_X,
        Map_Y,
        Map_Theta,
        Trigger,

        NotInitial,
        Initial,
        Ready,
        NotReady,
        Error,
        ResetAlarm,
        Closing,
        WaitThreadStop,
        Closed,

        #endregion

        #region 不能Auto原因.
        命令移動中,
        搖桿控制中,
        Fork不在原點,
        程式關閉中,
        定位裝置Not_Ready,
        Motion_Not_Ready,

        MoveControl_ErrorBit_On,
        MoveControl_迷航中_Real,
        MoveControl_迷航中_AddressOrSection,
        MoveControl_偏離路線,
        MoveControl_在RTurn路線上,

        #endregion

        #region Fork-Select.
        手臂吋動,
        原點復歸,
        手臂半自動,
        補正測試,
        命令紀錄,
        PIO監控,
        Home設定_站點設定,
        #endregion

        #region Fork-Jog.
        Z軸,
        P軸,
        Theta軸,
        Roller,
        Z軸_Slave,
        Y軸,

        Z軸Jog正,
        Z軸Jog負,

        Z軸_SlaveJog正,
        Z軸_SlaveJog負,

        P軸Jog正,
        P軸Jog負,

        Y軸Jog正,
        Y軸Jog負,

        Theta軸Jog正,
        Theta軸Jog負,

        RollerJog正,
        RollerJog負,
        ForkJogStop,

        Z軸原點,
        Z軸正極限,
        Z軸負極限,
        Z軸正定位,
        Z軸負定位,

        Z軸_Slave_原點,
        Z軸_Slave_正極限,
        Z軸_Slave_負極限,

        P軸原點,
        P軸正極限,
        P軸負極限,
        P軸正定位,
        P軸負定位,

        Y軸原點,
        Y軸正極限,
        Y軸負極限,

        Theta軸原點,
        Theta軸正極限,
        Theta軸負極限,
        Theta軸正定位,
        Theta軸負定位,

        Roller_CV入料,
        Roller_CV減速,
        Roller_CV停止,
        Roller_CV荷有,

        S軸正定位,
        S軸負定位,
        二重格檢知_L,
        二重格檢知_R,
        CST座_在席檢知,
        FORK_在席檢知,

        Fork_Jog_快,
        Fork_Jog_中,
        Fork_Jog_慢,
        Fork_Jog_強制Bypass,
        #endregion

        #region Fork-Home.
        回Home條件,
        回Home條件_內容,
        回Home流程中,
        Fork_Home,
        Home,
        Home_Initial,

        #endregion

        #region Fork-Command.
        命令,
        方向,
        Left,
        Right,
        StageNumber,
        速度Percentage,
        PIO,
        使用,
        不使用,
        分解模式,
        啟用補正,
        啟用,
        不啟用,
        Start_By_NowAddress,
        Pause,
        Continue,
        下一步,
        上一步,
        Command,
        ForkHome,
        二重格_L,
        二重格_R,
        儲位,
        Alignment_P,
        Alignment_Y,
        Alignment_Theta,
        Alignment_Z,
        #endregion

        #region Fork-Alignment.
        左側,
        右側,
        使用圖資偵測,
        AlignmentValue,
        Barcode資料,
        Laser_Front,
        Laser_Back,
        Barcode_ID,
        Barcode_X,
        Barcode_Y,
        #endregion

        #region Fork-PIO.
        NotSendTR_REQ,
        NotSendBUSY,
        NotForkBusyAction,
        NotSendCOMPT,
        NotSendAllOff,
        #endregion
        
        #region Charging-Command.
        電池資訊,
        手自動充電,
        充電PIO監控,
        充電紀錄,
        Charging充電中,
        左充電,
        右充電,
        電磁接觸器,
        對位Sensor_L,
        對位Sensor_R,
        電池電壓,
        電池安培,
        電池溫度,
        電表電壓,
        電表安培,
        #endregion
        
        #region IO.
        安全元件訊號,
        IO_測試,
        DeviceName,
        EMO,
        IPCEMO,
        Input,
        Output,
        取消自動輸出Output,

        #endregion

        #region Alarm.
        現有異常,
        異常紀錄,
        無動力電中_請按Reset_重新送電,
        清除異常,
        Buzz_Off,
        #endregion

        #region MoveControlConfig.
        TurnOut,
        LineInterval,
        OntimeReviseTheta,
        OntimeReviseSectionDeviationLine,
        VChangeSafetyDistance,

        ChargingCheck,
        ForkHomeCheck,
        STurnStop,
        STurnStart,
        RTurnStop,
        RTurnStart,
        #endregion

        #region Parameter.
        秒,
        安全偵測設定,
        電池保護設定,
        PIO_timeout設定,
        移動控制參數,
        其他設定,

        Name,
        ByPassAlarm,
        ByPassSafety,
        Reset,
        滿充SOC,
        低水位SOC,
        斷電SOC,
        滿充電壓,
        低水位電壓,
        斷電電壓,
        充電最大安培,
        充電最高溫度,
        警報延遲時間,
        斷電延遲時間,
        溫度警告閥值,
        溫度斷電閥值,
        省電模式,
        Z軸上位Home,
        檢查上定位,
        Idle不紀錄CSV,
        #endregion

        #region lpms
        Gyroscope,
        Acceleromete,
        Magnetometer,
        Orientation,
        EulerAngle,
        LinearAccelerationa,
        #endregion
    }

    public enum EnumMainFlowErrorCode
    {
        None = 0,
        無法Auto_MoveControlNotReady = 400100,
        無法Auto_Fork不在Home點上 = 400101,
        無法Auto_MiddlerInitialFail = 400102,
        無法Auto_尚有Alarm = 400103,
        無法Auto_ResetAlarm中 = 400104,
        無法Auto_讀取CSTID異常 = 400105,
        無法Auto_定位資料異常 = 400106,
        無法Auto_移動中 = 400207,
        無法Auto_搖桿控制中 = 400208,
        無法Auto_AGV不在Section上 = 400209,
    }
}