using Mirle.Agv.INX.Configs;
using Mirle.Agv.INX.Control;
using Mirle.Agv.INX.Controller.Tools;
using Mirle.Agv.INX.Model;
using Mirle.Agv.INX.Model.Configs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Mirle.Agv.INX.Controller
{
    public class MIPCControlHandler
    {
        private Stopwatch shutDownTimer = new Stopwatch();

        private ComputeFunction computeFunction = ComputeFunction.Instance;
        private LoggerAgent loggerAgent = LoggerAgent.Instance;
        private LocalData localData = LocalData.Instance;
        private string device = MethodInfo.GetCurrentMethod().ReflectedType.Name;
        private string normalLogName = "MIPC";
        private string motionLogName = "Motion";
        private string socketLogName = "MIPCSocket";
        private Logger logger = LoggerAgent.Instance.GetLooger("BatteryCSV");
        private AlarmHandler alarmHandler;
        private MIPCConfig config;

        private Dictionary<string, MIPCData> allDataByMIPCTagName = new Dictionary<string, MIPCData>();
        private Dictionary<string, MIPCData> allDataByIPCTagName = new Dictionary<string, MIPCData>();
        public Dictionary<string, List<MIPCData>> AllDataByClassification { get; set; } = new Dictionary<string, List<MIPCData>>();
        private Dictionary<int, List<MIPCData>> pollingGroup = new Dictionary<int, List<MIPCData>>();

        private Dictionary<string, Thread> allSocketThread = new Dictionary<string, Thread>();
        private Dictionary<string, Thread> allProcessDataThread = new Dictionary<string, Thread>();
        private Dictionary<string, Socket> allSocket = new Dictionary<string, Socket>();

        private Thread pollingThread;

        private Dictionary<string, object> allReadObject = new Dictionary<string, object>();
        private Dictionary<string, object> allWriteObject = new Dictionary<string, object>();
        private Dictionary<string, Queue<SendAndReceive>> allReadQueue = new Dictionary<string, Queue<SendAndReceive>>();
        private Dictionary<string, Queue<SendAndReceive>> allWriteQueue = new Dictionary<string, Queue<SendAndReceive>>();

        private Dictionary<string, Queue<SendAndReceive>> processQueue = new Dictionary<string, Queue<SendAndReceive>>();
        private Dictionary<string, bool> needClearBuffer = new Dictionary<string, bool>();


        private Dictionary<int, MIPCPollingData> allPollingData = new Dictionary<int, MIPCPollingData>();
        private List<MIPCPollingData> pollingDataList = new List<MIPCPollingData>();
        private List<int> pollingIntervalList = new List<int>();

        private object newSendLockObject = new object();
        private UInt16 count = 0;

        private UInt32 dataStartAddress = 0;
        private Byte[] allData { get; set; }
        public UInt32 ipcHeartbeatNumber { get; set; } = 0;

        public uint MoveControlHeartBeat { get; set; } = 0;
        private uint lastMoveControlHeartBeat = 0;

        private Thread batteryCSVThread = null;
        private SendAndReceive sendHeartbeat = null;

        private Thread pingThread = null;

        private EnumControlStatus status = EnumControlStatus.NotInitial;
        private bool resetAlarm = false;
        private Thread resetAlarmThread = null;

        private TimeStampData timeStampData = null;

        private double overflowValue = 10000;
        private string localPath = @"D:\MecanumConfigs\MIPCControl";

        private List<string> buzzerMIPCTageList = new List<string>();
        private Dictionary<EnumBuzzerType, List<float>> buzzerMIPCTageChangeList = new Dictionary<EnumBuzzerType, List<float>>();
        private EnumBuzzerType lastBuzzerType = EnumBuzzerType.Initial;

        private List<string> DirectionLightMIPCTageList = new List<string>();
        private Dictionary<EnumDirectionLight, List<float>> DirectionLightMIPCTageChangeList = new Dictionary<EnumDirectionLight, List<float>>();
        private EnumDirectionLight lastDirectionLightType = EnumDirectionLight.Initial;

        public SafetySensorControlHandler SafetySensorControl { get; set; } = null;
        private MainFlowHandler main = null;

        private bool logMode = true;

        public event EventHandler CallForkHome;
        public event EventHandler<EnumAutoState> AutoManualEvent;
        public event EventHandler<bool> ChargingStatusChange;

        private DataDelayAndChange Getway通訊正常_XFL = new DataDelayAndChange(5000, EnumDelayType.OnDelay);
        private DataDelayAndChange Getway通訊正常_XFR = new DataDelayAndChange(5000, EnumDelayType.OnDelay);
        private DataDelayAndChange Getway通訊正常_XRL = new DataDelayAndChange(5000, EnumDelayType.OnDelay);
        private DataDelayAndChange Getway通訊正常_XRR = new DataDelayAndChange(5000, EnumDelayType.OnDelay);

        private ShareMemoryWriter SMWriter = new ShareMemoryWriter();   //Allen, share memory
        private ShareMemoryReader SMReader = new ShareMemoryReader();   //Allen, share memory

        public EnumControlStatus Status
        {
            get
            {
                if (resetAlarm)
                    return EnumControlStatus.ResetAlarm;
                else
                    return status;
            }

            set
            {
                status = value;
            }
        }

        private List<EnumDefaultAxisName> axisList = new List<EnumDefaultAxisName>() { EnumDefaultAxisName.XFL, EnumDefaultAxisName.XFR, EnumDefaultAxisName.XRL, EnumDefaultAxisName.XRR };

        private UInt32[] moveCommandAddressArray = new UInt32[0];
        private List<string> moveCommandTagNameList = new List<string>()
        {
            EnumMecanumIPCdefaultTag.Command_MapX.ToString(),
            EnumMecanumIPCdefaultTag.Command_MapY.ToString(),
            EnumMecanumIPCdefaultTag.Command_MapTheta.ToString(),
            EnumMecanumIPCdefaultTag.Command_線速度.ToString(),
            EnumMecanumIPCdefaultTag.Command_線加速度.ToString(),
            EnumMecanumIPCdefaultTag.Command_線減速度.ToString(),
            EnumMecanumIPCdefaultTag.Command_線急跳度.ToString(),
            EnumMecanumIPCdefaultTag.Command_角速度.ToString(),
            EnumMecanumIPCdefaultTag.Command_角加速度.ToString(),
            EnumMecanumIPCdefaultTag.Command_角減速度.ToString(),
            EnumMecanumIPCdefaultTag.Command_角急跳度.ToString(),
            EnumMecanumIPCdefaultTag.Command_Start.ToString()
        };

        private UInt32[] jogjoystickDataArray = new UInt32[0];
        private List<string> jogjoystickDataList = new List<string>()
        {
            EnumMecanumIPCdefaultTag.Joystick_LineVelocity.ToString(),
            EnumMecanumIPCdefaultTag.Joystick_LineAcc.ToString(),
            EnumMecanumIPCdefaultTag.Joystick_LineDec.ToString(),
            EnumMecanumIPCdefaultTag.Joystick_ThetaVelocity.ToString(),
            EnumMecanumIPCdefaultTag.Joystick_ThetaAcc.ToString(),
            EnumMecanumIPCdefaultTag.Joystick_ThetaDec.ToString()
        };

        private UInt32[] changeVelociyAddressArray = new UInt32[0];
        private List<string> changeVelociyTagNameList = new List<string>()
        {
            EnumMecanumIPCdefaultTag.Command_線速度.ToString()
            ,
            EnumMecanumIPCdefaultTag.Command_Start.ToString()
        };

        private UInt32[] changeEndAddressArray = new UInt32[0];
        private List<string> changeEndTagNameList = new List<string>()
        {
            EnumMecanumIPCdefaultTag.Command_MapX.ToString(),
            EnumMecanumIPCdefaultTag.Command_MapY.ToString(),
            EnumMecanumIPCdefaultTag.Command_MapTheta.ToString()
            //,
            //EnumMecanumIPCdefaultTag.Command_Start.ToString()
        };

        private UInt32[] setPositionAddressArray = new UInt32[0];
        private List<string> setPositionTagNameList = new List<string>()
        {
            EnumMecanumIPCdefaultTag.SetPosition_MapX.ToString(),
            EnumMecanumIPCdefaultTag.SetPosition_MapY.ToString(),
            EnumMecanumIPCdefaultTag.SetPosition_MapTheta.ToString(),
            EnumMecanumIPCdefaultTag.SetPosition_TimeStmap.ToString(),
            EnumMecanumIPCdefaultTag.SetPosition_Start.ToString(),

            EnumMecanumIPCdefaultTag.SetPosition_OriginX.ToString(),
            EnumMecanumIPCdefaultTag.SetPosition_OriginY.ToString(),
            EnumMecanumIPCdefaultTag.SetPosition_OriginTheta.ToString(),
            EnumMecanumIPCdefaultTag.SetPosition_Origin_OK.ToString(),

            EnumMecanumIPCdefaultTag.Heartbeat_IPC.ToString()
        };

        private UInt32[] setPositionAddressArray_OnlyOriginData = new UInt32[0];
        private List<string> setPositionTagNameList_OnlyOriginData = new List<string>()
        {
            EnumMecanumIPCdefaultTag.SetPosition_OriginX.ToString(),
            EnumMecanumIPCdefaultTag.SetPosition_OriginY.ToString(),
            EnumMecanumIPCdefaultTag.SetPosition_OriginTheta.ToString(),
            EnumMecanumIPCdefaultTag.SetPosition_Origin_OK.ToString(),

            EnumMecanumIPCdefaultTag.Heartbeat_IPC.ToString()
        };

        private UInt32[] setPositionAddressArray_OnlyOriginOK = new UInt32[0];
        private List<string> setPositionTagNameList_OnlyOriginOK = new List<string>()
        {
            EnumMecanumIPCdefaultTag.SetPosition_Origin_OK.ToString(),

            EnumMecanumIPCdefaultTag.Heartbeat_IPC.ToString()
        };

        //private List<string> turnCommandTagNameList = new List<string>()
        //{
        //    EnumMecanumIPCdefaultTag.Turn_MapX.ToString(),
        //    EnumMecanumIPCdefaultTag.Turn_MapY.ToString(),
        //    EnumMecanumIPCdefaultTag.Turn_MapTheta.ToString(),
        //    EnumMecanumIPCdefaultTag.Turn_R.ToString(),
        //    EnumMecanumIPCdefaultTag.Turn_Theta.ToString(),
        //    EnumMecanumIPCdefaultTag.Turn_Velocity.ToString(),
        //    EnumMecanumIPCdefaultTag.Turn_MovingAngle.ToString(),
        //    EnumMecanumIPCdefaultTag.Turn_DeltaTheta.ToString(),
        //    EnumMecanumIPCdefaultTag.Turn_Start.ToString()
        //};

        private UInt32[] stopCommandAddressArray = new UInt32[0];

        private List<string> stopCommandTagNameList = new List<string>()
        {
            EnumMecanumIPCdefaultTag.Command_線減速度.ToString(),
            EnumMecanumIPCdefaultTag.Command_線急跳度.ToString(),
            EnumMecanumIPCdefaultTag.Command_角減速度.ToString(),
            EnumMecanumIPCdefaultTag.Command_角急跳度.ToString(),
            EnumMecanumIPCdefaultTag.Command_Stop.ToString()
        };

        private byte[] shareMemoryByteArray = new byte[1000];

        private List<string> shareMemoryTagNameList = new List<string>()
        {
            //戰情室上拋資料的MIPC TagName
            "IPCL 版本號",
            "Divice 版本號",
            "motion 版本號",
            "ServoOn",
            "AlarmReset",
            "EmgStop",
            "ESTOP request 1",
            "ESTOP request 2",
            "Battery_Alarm",
            "電表A",
            "電表V",
            "電表W",
            "電表WH",
            "電池模組電壓",
            "電池模組電流 正值充電，負值放電",
            "充電狀態 0x0000 閒置 0x0001 ? 2A充電 0x0008  ? -2A放電",
            "電芯溫感器 1",
            "電芯溫感器 2",
            "電池蓄電層級 （SOC％）",
            "電池警報(Mapping至610036)",
            "外部接點",
            "馬達溫度#1",
            "馬達溫度#2",
            "馬達溫度#3",
            "馬達溫度#4",
            "SetPosition_MapY",
            "SetPosition_MapX",                                
            "SetPosition_MapTheta",
            "Start前",
            "Reset前",
            "BrakeRelease前",
            "Auto訊號",
            "Manual訊號",
            "Start後",
            "Reset後",
            "BrakeRelease後",
            "Robot前翹高",
            "Robot夾爪在席",
            "Robot後翹高",
            "FoupL1在席",
            "FoupL2在席",
            "FoupR1在席",
            "FoupR2在席",
            "SPARE",
            "PIO-in00L",
            "PIO-in01L",
            "PIO-in02L",
            "PIO-in03L",
            "PIO-in04L",
            "PIO-in05L",
            "PIO-in06L",
            "PIO-in07L",
            "PIO-in08L",
            "CPIO-in00",
            "CPIO-in01",
            "ConfirmSensor",
            "SPARE",
            "EMO_F PL",
            "EMO_B PL",
            "Safety Relay OK",
            "PIO-in00R",
            "PIO-in01R",
            "PIO-in02R",
            "PIO-in03R",
            "PIO-in04R",
            "PIO-in05R",
            "PIO-in06R",
            "PIO-in07R",
            "PIO-in08R",
            "CPIO-in02",
            "CPIO-in03",
            "CPIO-in04",
            "CPIO-in05",
            "CPIO-in06",
            "CPIO-in07",
            "CPIO-in08",
            "Hokuyo-HL-Output01",
            "Hokuyo-HL-Output02",
            "Hokuyo-HL-Output03",
            "Hokuyo-HL-Output04",
            "Hokuyo-HR-Output01",
            "Hokuyo-HR-Output02",
            "Hokuyo-HR-Output03",
            "Hokuyo-HR-Output04",
            "Hokuyo-VL-Output01",
            "Hokuyo-VL-Output02",
            "Hokuyo-VL-Output03",
            "Hokuyo-VL-Output04",
            "Hokuyo-VR-Output01",
            "Hokuyo-VR-Output02",
            "Hokuyo-VR-Output03",
            "Hokuyo-VR-Output04",
            "NanoScan3-Output01",
            "NanoScan3-Output02",
            "NanoScan3-Output03",
            "NanoScan3-Output04",
            "前Bump作動",
            "後Bump作動",
            "左Bump作動",
            "右Bump作動",
            "SPARE",     //373
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "Start前(燈)",
            "Reset前(燈)",
            "BrakeRelease前(燈)",
            "Start後(燈)",
            "Reset後(燈)",
            "BrakeRelease後(燈)",
            "PIO-out01L",
            "PIO-out02L",
            "PIO-out03L",
            "PIO-out04L",
            "PIO-out05L",
            "PIO-out06L",
            "PIO-out07L",
            "PIO-out08L",
            "PIO-out01R",
            "PIO-out02R",
            "PIO-out03R",
            "PIO-out04R",
            "PIO-out05R",
            "PIO-out06R",
            "PIO-out07R",
            "PIO-out08R",
            "Buzzer01",
            "Buzzer02",
            "Buzzer03",
            "Buzzer04",
            "MIPC Not Ready",
            "電池電壓不足 斷電",
            "三色燈F&B&L&R_R",
            "三色燈F&B&L&R_G",
            "三色燈F&B&L&R_B",
            "CPIO-out01",
            "CPIO-out02",
            "CPIO-out03",
            "CPIO-out04",
            "CPIO-out05",
            "CPIO-out06",
            "CPIO-out07",
            "CPIO-out08",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "ChargingSaftey",
            "Hokuyo-HL-Input01",
            "Hokuyo-HL-Input02",
            "Hokuyo-HL-Input03",
            "Hokuyo-HL-Input04",
            "Hokuyo-HL-Input05",
            "Hokuyo-HR-Input01",
            "Hokuyo-HR-Input02",
            "Hokuyo-HR-Input03",
            "Hokuyo-HR-Input04",
            "Hokuyo-HR-Input05",
            "Hokuyo-VL-Input01",
            "Hokuyo-VL-Input02",
            "Hokuyo-VL-Input03",
            "Hokuyo-VL-Input04",
            "Hokuyo-VL-Input05",
            "Hokuyo-VR-Input01",
            "Hokuyo-VR-Input02",
            "Hokuyo-VR-Input03",
            "Hokuyo-VR-Input04",
            "Hokuyo-VR-Input05",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "NanoScan3-Input01",
            "NanoScan3-Input02",
            "NanoScan3-Input03",
            "NanoScan3-Input04",
            "NanoScan3-Input05",
            "NanoScan3-Input06",
            "NanoScan3-Input07",
            "NanoScan3-Input08",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "SPARE",
            "RB_Joint1",    // TMRobot
            "RB_Joint2",
            "RB_Joint3",
            "RB_Joint4",
            "RB_Joint5",
            "RB_Joint6",
        };

        private Dictionary<string, bool> allIPCTageName = new Dictionary<string, bool>();
        private Dictionary<EnumMecanumIPCdefaultTag, EnumMIPCControlErrorCode> ipcDefaultTagToAlarmCode = new Dictionary<EnumMecanumIPCdefaultTag, EnumMIPCControlErrorCode>();

        private void InitialMIPCAlarmCode()
        {
            #region Alarm 1~10.
            ipcDefaultTagToAlarmCode.Add(EnumMecanumIPCdefaultTag.MIPCAlarmCode_1, EnumMIPCControlErrorCode.MIPC_DeviceHeartBeatLoss);
            ipcDefaultTagToAlarmCode.Add(EnumMecanumIPCdefaultTag.MIPCAlarmCode_2, EnumMIPCControlErrorCode.MIPC_IPCHeartBeatLoss);
            ipcDefaultTagToAlarmCode.Add(EnumMecanumIPCdefaultTag.MIPCAlarmCode_3, EnumMIPCControlErrorCode.MIPC_Motion_EMS);
            ipcDefaultTagToAlarmCode.Add(EnumMecanumIPCdefaultTag.MIPCAlarmCode_4, EnumMIPCControlErrorCode.MIPC_SLAM誤差過大);
            ipcDefaultTagToAlarmCode.Add(EnumMecanumIPCdefaultTag.MIPCAlarmCode_5, EnumMIPCControlErrorCode.MIPC_SLAM過久沒更新);
            ipcDefaultTagToAlarmCode.Add(EnumMecanumIPCdefaultTag.MIPCAlarmCode_6, EnumMIPCControlErrorCode.MIPC_走行驅動器異常);
            ipcDefaultTagToAlarmCode.Add(EnumMecanumIPCdefaultTag.MIPCAlarmCode_7, EnumMIPCControlErrorCode.MIPC_電池低電壓斷電);
            ipcDefaultTagToAlarmCode.Add(EnumMecanumIPCdefaultTag.MIPCAlarmCode_8, EnumMIPCControlErrorCode.MIPC_四輪ServoOnOff不同步);
            ipcDefaultTagToAlarmCode.Add(EnumMecanumIPCdefaultTag.MIPCAlarmCode_9, EnumMIPCControlErrorCode.MIPC_驅動器過電流);
            ipcDefaultTagToAlarmCode.Add(EnumMecanumIPCdefaultTag.MIPCAlarmCode_10, EnumMIPCControlErrorCode.MIPC_速度追隨誤差過大);
            #endregion
             
            ipcDefaultTagToAlarmCode.Add(EnumMecanumIPCdefaultTag.MIPCAlarmCode_11, EnumMIPCControlErrorCode.MIPC_Alarm11);
            ipcDefaultTagToAlarmCode.Add(EnumMecanumIPCdefaultTag.MIPCAlarmCode_12, EnumMIPCControlErrorCode.MIPC_超過命令速度上限);
            ipcDefaultTagToAlarmCode.Add(EnumMecanumIPCdefaultTag.MIPCAlarmCode_13, EnumMIPCControlErrorCode.MIPC_斷動力電);
            ipcDefaultTagToAlarmCode.Add(EnumMecanumIPCdefaultTag.MIPCAlarmCode_14, EnumMIPCControlErrorCode.MIPC_IPCEMS);
            ipcDefaultTagToAlarmCode.Add(EnumMecanumIPCdefaultTag.MIPCAlarmCode_15, EnumMIPCControlErrorCode.MIPC_SetPosition角度誤差過大);
            ipcDefaultTagToAlarmCode.Add(EnumMecanumIPCdefaultTag.MIPCAlarmCode_16, EnumMIPCControlErrorCode.MIPC_Alarm16);
            ipcDefaultTagToAlarmCode.Add(EnumMecanumIPCdefaultTag.MIPCAlarmCode_17, EnumMIPCControlErrorCode.MIPC_Alarm17);
            ipcDefaultTagToAlarmCode.Add(EnumMecanumIPCdefaultTag.MIPCAlarmCode_18, EnumMIPCControlErrorCode.MIPC_Alarm18);
            ipcDefaultTagToAlarmCode.Add(EnumMecanumIPCdefaultTag.MIPCAlarmCode_19, EnumMIPCControlErrorCode.MIPC_Alarm19);
            ipcDefaultTagToAlarmCode.Add(EnumMecanumIPCdefaultTag.MIPCAlarmCode_20, EnumMIPCControlErrorCode.MIPC_DeviceDisconnectCounter);
        }

        private Thread connectedLoopThread = null;

        private Exception lastPingException = null;

        private void ConnectedLoopThread()
        {
            while (Status != EnumControlStatus.Closing && Status != EnumControlStatus.Closed)
            {
                if (resetAlarmThread == null || !resetAlarmThread.IsAlive)
                {
                    switch (Status)
                    {
                        case EnumControlStatus.Initial:
                            if (mipcPing)
                            {
                                resetAlarm = true;
                                resetAlarmThread = new Thread(ResetConnectThread);
                                resetAlarmThread.Start();
                            }
                            break;
                        case EnumControlStatus.Error:
                            if (mipcPing)
                            {
                                resetAlarm = true;
                                resetAlarmThread = new Thread(ResetConnectThread_斷線);
                                resetAlarmThread.Start();
                            }
                            break;
                    }
                }

                Thread.Sleep(1000);
            }
        }

        public MIPCControlHandler(MainFlowHandler main, AlarmHandler alarmHandler)
        {
            InitialMIPCAlarmCode();
            this.main = main;
            this.alarmHandler = alarmHandler;
            ReadXMLAndConfig();

            ReadBuzzerTypeCSV();
            ReadDirectionLightCSV();
            InitailCharging();
            SafetySensorControl = new SafetySensorControlHandler(this, alarmHandler, config.SafetySensorConfigPath);

            if (localData.SimulateMode)
                Status = EnumControlStatus.Ready;
            else
            {
                for (int i = 0; i < config.PortList.Count; i++)
                    socketListConnect.Add(false);

                if (Status == EnumControlStatus.Initial)
                {
                    pingThread = new Thread(PingTestThread);
                    pingThread.Start();

                    if (!InitialSocketAndThread())
                        SetAlarmCode(EnumMIPCControlErrorCode.MIPC連線失敗);

                    SafetySensorControl.InitialSafetySensor();
                }
                else
                    SetAlarmCode(EnumMIPCControlErrorCode.MIPC初始化失敗);

                connectedLoopThread = new Thread(ConnectedLoopThread);
                connectedLoopThread.Start();
            }

            SMWriter.Fun_Ini_ShareMemoryWriter("SM");    

        }

        public void CloseMipcControlHandler()
        {
            Status = EnumControlStatus.Closing;

            Stopwatch timer = new Stopwatch();
            timer.Restart();

            while (timer.ElapsedMilliseconds < 2000 && connectedLoopThread != null && connectedLoopThread.IsAlive)
                Thread.Sleep(50);

            while (timer.ElapsedMilliseconds < 2000 && Status != EnumControlStatus.Closed)
                Thread.Sleep(50);

            foreach (Socket socket in allSocket.Values)
            {
                try
                {
                    socket.Dispose();
                }
                catch { }
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

        public void WriteMotionLog(int logLevel, string carrierId, string message, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            LogFormat logFormat = new LogFormat(motionLogName, logLevel.ToString(), memberName, device, carrierId, message);

            loggerAgent.Log(logFormat.Category, logFormat);

            if (logLevel <= localData.ErrorLevel)
            {
                logFormat = new LogFormat(localData.ErrorLogName, logLevel.ToString(), memberName, device, carrierId, message);
                loggerAgent.Log(logFormat.Category, logFormat);
            }
        }

        public void WriteSocketLog(int logLevel, string carrierId, string message, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            LogFormat logFormat = new LogFormat(socketLogName, logLevel.ToString(), memberName, device, carrierId, message);

            loggerAgent.Log(logFormat.Category, logFormat);
        }

        private void AutoReConnectThread()
        {
            WriteLog(7, "", "AutoReconnect Start");

            while (Status != EnumControlStatus.Ready)
            {
                Thread.Sleep(3000);

                ResetAlarm();
            }

            WriteLog(7, "", "AutoReconnect End");
        }

        private void SetAlarmCode(EnumMIPCControlErrorCode alarmCode)
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

        private void ResetAlarmCode(EnumMIPCControlErrorCode alarmCode)
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

        #region ReadXML/Config.
        private void ReadBuzzerTypeCSV()
        {
            string path = Path.Combine(localPath, localData.MainFlowConfig.AGVType.ToString(), "BuzzerType.csv");

            if (!File.Exists(path))
            {
                WriteLog(3, "", String.Concat("BuzzerType檔案不存在, path : ", path));
                return;
            }

            string[] allRows = File.ReadAllLines(path);

            if (allRows == null || allRows.Length < 1)
            {
                WriteLog(5, "", String.Concat("BuzzerType.csv line == 0"));
                return;
            }

            string[] titleRow = allRows[0].Split(',');
            allRows = allRows.Skip(1).ToArray();

            for (int i = 1; i < titleRow.Length; i++)
            {
                if (allDataByMIPCTagName.ContainsKey(titleRow[i]))
                    buzzerMIPCTageList.Add(titleRow[i]);
                else
                {
                    buzzerMIPCTageList = new List<string>();
                    WriteLog(7, "", String.Concat("BuzzerType.csv 內Title有不屬於EnumMecanumIPCdefaultTag的Tag"));
                    return;
                }
            }

            string[] getThisRow;

            EnumBuzzerType buzzerType;
            List<float> tempList;
            float tryParseFloat;

            for (int i = 0; i < allRows.Length; i++)
            {
                getThisRow = allRows[i].Split(',');

                if (getThisRow.Length < 1)
                    WriteLog(3, "", String.Concat("BuzzerType.csv Line : ", (i + 1).ToString(), " 無內容"));
                else if (!Enum.TryParse(getThisRow[0], out buzzerType))
                    WriteLog(3, "", String.Concat("BuzzerType.csv Line : ", (i + 1).ToString(), " buzzerType 轉換失敗"));
                else if (getThisRow.Length - 1 != buzzerMIPCTageList.Count)
                    WriteLog(3, "", String.Concat("BuzzerType.csv Line : ", (i + 1).ToString(), " Count != Title Count"));
                else
                {
                    tempList = new List<float>();

                    for (int j = 1; j < getThisRow.Length; j++)
                    {
                        if (float.TryParse(getThisRow[j], out tryParseFloat))
                            tempList.Add(tryParseFloat);
                        else
                        {
                            WriteLog(3, "", String.Concat("BuzzerType.csv Line : ", (i + 1).ToString(), " float 轉換失敗"));
                            break;
                        }
                    }

                    if (tempList.Count == buzzerMIPCTageList.Count)
                        buzzerMIPCTageChangeList.Add(buzzerType, tempList);
                }
            }
        }

        private void ReadDirectionLightCSV()
        {
            string path = Path.Combine(localPath, localData.MainFlowConfig.AGVType.ToString(), "DirectionLight.csv");

            if (!File.Exists(path))
            {
                WriteLog(3, "", String.Concat("DirectionLight檔案不存在, path : ", path));
                return;
            }

            string[] allRows = File.ReadAllLines(path);

            if (allRows == null || allRows.Length < 1)
            {
                WriteLog(5, "", String.Concat("DirectionLight.csv line == 0"));
                return;
            }

            string[] titleRow = allRows[0].Split(',');
            allRows = allRows.Skip(1).ToArray();

            for (int i = 1; i < titleRow.Length; i++)
            {
                if (allDataByMIPCTagName.ContainsKey(titleRow[i]))
                    DirectionLightMIPCTageList.Add(titleRow[i]);
                else
                {
                    DirectionLightMIPCTageList = new List<string>();
                    WriteLog(7, "", String.Concat("DirectionLight.csv 內Title有不屬於EnumMecanumIPCdefaultTag的Tag"));
                    return;
                }
            }

            string[] getThisRow;

            EnumDirectionLight directionList;
            List<float> tempList;
            float tryParseFloat;

            for (int i = 0; i < allRows.Length; i++)
            {
                getThisRow = allRows[i].Split(',');

                if (getThisRow.Length < 1)
                    WriteLog(3, "", String.Concat("BuzzerType.csv Line : ", (i + 1).ToString(), " 無內容"));
                else if (!Enum.TryParse(getThisRow[0], out directionList))
                    WriteLog(3, "", String.Concat("BuzzerType.csv Line : ", (i + 1).ToString(), " buzzerType 轉換失敗"));
                else if (getThisRow.Length - 1 != DirectionLightMIPCTageList.Count)
                    WriteLog(3, "", String.Concat("BuzzerType.csv Line : ", (i + 1).ToString(), " Count != Title Count"));
                else
                {
                    tempList = new List<float>();

                    for (int j = 1; j < getThisRow.Length; j++)
                    {
                        if (float.TryParse(getThisRow[j], out tryParseFloat))
                            tempList.Add(tryParseFloat);
                        else
                        {
                            WriteLog(3, "", String.Concat("BuzzerType.csv Line : ", (i + 1).ToString(), " float 轉換失敗"));
                            break;
                        }
                    }

                    if (tempList.Count == DirectionLightMIPCTageList.Count)
                        DirectionLightMIPCTageChangeList.Add(directionList, tempList);
                }
            }
        }

        private bool ReadPortXML(XmlElement element)
        {
            try
            {
                MIPCPortData temp = new MIPCPortData();

                foreach (XmlNode item in element.ChildNodes)
                {
                    switch (item.Name)
                    {
                        case "PortNumber":
                            temp.PortNumber = Int32.Parse(item.InnerText);
                            break;
                        case "ConnectType":
                            EnumMIPCConnectType type;

                            if (Enum.TryParse(item.InnerText, out type))
                                temp.ConnectType = type;
                            else
                            {
                                WriteLog(3, "", String.Concat("ConnectType Error : ", item.InnerText));
                                return false;
                            }

                            break;
                        case "SocketName":
                            temp.SocketName = item.InnerText;
                            break;
                        default:
                            break;
                    }
                }

                if (temp.PortNumber == -1 || temp.ConnectType == EnumMIPCConnectType.None || temp.SocketName == "")
                {
                    WriteLog(3, "", "Port- PortNumber/ConnectType/TagName 皆需設定");
                    return false;
                }

                for (int i = 0; i < config.PortList.Count; i++)
                {
                    if (config.PortList[i].PortNumber == temp.PortNumber)
                    {
                        WriteLog(3, "", "Port- PortNumber重複");
                        return false;
                    }
                    else if (config.PortList[i].SocketName == temp.SocketName)
                    {
                        WriteLog(3, "", "Port- TagName重複");
                        return false;
                    }
                }

                config.PortList.Add(temp);
                config.AllPort.Add(temp.SocketName, temp);

                return true;
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }

        private bool ReadXML()
        {
            try
            {

                string path = Path.Combine(localPath, "MIPCConfig.xml");
                config = new MIPCConfig();

                if (path == null || path == "")
                {
                    WriteLog(1, "", "MIPCConfig 路徑錯誤為null或空值,請檢查程式內部的string.");
                    return false;
                }
                else if (!File.Exists(path))
                {
                    WriteLog(1, "", "找不到MIPCConfig.xml.");
                    return false;
                }

                XmlDocument doc = new XmlDocument();
                doc.Load(path);
                XmlElement rootNode = doc.DocumentElement;

                string locatePath = new DirectoryInfo(path).Parent.FullName;

                foreach (XmlNode item in rootNode.ChildNodes)
                {
                    switch (item.Name)
                    {
                        case "IP":
                            config.IP = item.InnerText;
                            break;
                        case "PollingInterval":
                            config.PollingInterval = Int32.Parse(item.InnerText);
                            break;
                        case "SocketTimeoutValue":
                            config.SocketTimeoutValue = Int32.Parse(item.InnerText);
                            break;
                        case "CommandTimeoutValue":
                            config.CommandTimeoutValue = Int32.Parse(item.InnerText);
                            break;
                        case "SafetySensorUpdateInterval":
                            config.SafetySensorUpdateInterval = Int32.Parse(item.InnerText);
                            break;
                        case "PollingGroupInterval":
                            ReadPollingGroupIntervalXML((XmlElement)item);
                            break;
                        case "HeartbeatInterval":
                            config.HeartbeatInterval = Int32.Parse(item.InnerText);
                            break;
                        case "MIPCDataConfigPath":
                            config.MIPCDataConfigPath = Path.Combine(localPath, localData.MainFlowConfig.AGVType.ToString(), item.InnerText);
                            break;
                        case "SafetySensorConfigPath":
                            config.SafetySensorConfigPath = Path.Combine(localPath, localData.MainFlowConfig.AGVType.ToString(), item.InnerText);
                            break;
                        case "Port":
                            if (!ReadPortXML((XmlElement)item))
                                return false;
                            break;
                        case "LogMode":
                            config.LogMode = Boolean.Parse(item.InnerText);
                            break;
                        default:
                            break;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }

        private void ReadPollingGroupIntervalXML(XmlElement element)
        {
            int pollingGroup;
            int interval;

            foreach (XmlNode item in element.ChildNodes)
            {
                if (Int32.TryParse(Regex.Replace(item.Name, "[^0-9]", ""), out pollingGroup) && Int32.TryParse(item.InnerText, out interval))
                {
                    if (config.PollingGroupInterval.ContainsKey(pollingGroup))
                        WriteLog(3, "", String.Concat("Group : ", item.Name, " 重複"));
                    else if (interval < 0)
                        WriteLog(3, "", String.Concat("Group : ", item.Name, " Interval < 0 : ", item.InnerText));
                    else
                        config.PollingGroupInterval.Add(pollingGroup, interval);
                }
                else
                    WriteLog(3, "", String.Concat("Group : ", item.Name, " Interval : ", item.InnerText, " 非數字"));
            }
        }

        private bool ReadConfig(string path)
        {
            if (path == null || path == "")
            {
                WriteLog(3, "", "MIPCDataConfig 路徑錯誤為null或空值.");
                return false;
            }
            else if (!File.Exists(path))
            {
                WriteLog(3, "", "找不到 MIPCDataConfig.csv.");
                return false;
            }

            try
            {
                string[] allRows = File.ReadAllLines(path);
                string[] titleRow = allRows[0].Split(',');
                allRows = allRows.Skip(1).ToArray();

                int nRows = allRows.Length;
                int nColumns = titleRow.Length;

                Dictionary<string, int> dicHeaderIndexes = new Dictionary<string, int>();

                for (int i = 0; i < nColumns; i++)
                {
                    var keyword = titleRow[i].Trim();

                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        if (dicHeaderIndexes.ContainsKey(keyword))
                            WriteLog(3, "", String.Concat("Title repeat : ", keyword));
                        else
                            dicHeaderIndexes.Add(keyword, i);
                    }
                }

                if (dicHeaderIndexes.ContainsKey("DataName") && dicHeaderIndexes.ContainsKey("Address") &&
                    dicHeaderIndexes.ContainsKey("ByteNumber") && dicHeaderIndexes.ContainsKey("BitNumber") &&
                    dicHeaderIndexes.ContainsKey("DataType") && dicHeaderIndexes.ContainsKey("IoStatus") &&
                    dicHeaderIndexes.ContainsKey("IPCName") && dicHeaderIndexes.ContainsKey("Classification"))
                {
                }
                else
                {
                    WriteLog(3, "", String.Concat("Title must have : DataName,Address,ByteNumber,BitNumber,DataType,IoStatus,IPCName,Classification"));
                    return false;
                }


                MIPCData temp;
                EnumDataType dataType;
                EnumIOType ioStatus;

                for (int i = 0; i < nRows; i++)
                {
                    string[] getThisRow = allRows[i].Split(',');

                    if (getThisRow.Length != dicHeaderIndexes.Count)
                        WriteLog(3, "", String.Concat("line : ", (i + 2).ToString(), " 和Title數量不吻合"));
                    else
                    {
                        temp = new MIPCData();

                        temp.DataName = getThisRow[dicHeaderIndexes["DataName"]];
                        temp.Address = UInt32.Parse(getThisRow[dicHeaderIndexes["Address"]]);
                        temp.ByteNumber = UInt32.Parse(getThisRow[dicHeaderIndexes["ByteNumber"]]);
                        temp.BitNumber = UInt32.Parse(getThisRow[dicHeaderIndexes["BitNumber"]]);
                        temp.IPCName = getThisRow[dicHeaderIndexes["IPCName"]];
                        temp.Classification = getThisRow[dicHeaderIndexes["Classification"]];

                        int group;
                        if (Int32.TryParse(getThisRow[dicHeaderIndexes["PollingGroup"]], out group))
                            temp.PollingGroup = group;
                        else
                            temp.PollingGroup = -1;

                        if (dicHeaderIndexes.ContainsKey("Description"))
                            temp.Description = getThisRow[dicHeaderIndexes["Description"]];

                        if (allDataByMIPCTagName.ContainsKey(temp.DataName))
                            WriteLog(3, "", String.Concat("line : ", (i + 2).ToString(), ", DataName 重複 : ", temp.DataName));
                        else if (allDataByIPCTagName.ContainsKey(temp.IPCName))
                            WriteLog(3, "", String.Concat("line : ", (i + 2).ToString(), ", IPCName 重複 : ", temp.IPCName));
                        else if (temp.Address < 0 || temp.ByteNumber < 0 || temp.BitNumber < 0)
                            WriteLog(3, "", String.Concat("line : ", (i + 2).ToString(), ", Int屬性錯誤 < 0"));
                        else if (!Enum.TryParse(getThisRow[dicHeaderIndexes["DataType"]], out dataType))
                            WriteLog(3, "", String.Concat("line : ", (i + 2).ToString(), ", DataType 錯誤 : ", getThisRow[dicHeaderIndexes["DataType"]]));
                        else if (!Enum.TryParse(getThisRow[dicHeaderIndexes["IoStatus"]], out ioStatus))
                            WriteLog(3, "", String.Concat("line : ", (i + 2).ToString(), ", ioType 錯誤 : ", getThisRow[dicHeaderIndexes["IoStatus"]]));
                        else
                        {
                            temp.DataType = dataType;

                            switch (dataType)
                            {
                                case EnumDataType.Int32:
                                case EnumDataType.UInt32:
                                case EnumDataType.Double_1:
                                case EnumDataType.Float:
                                    temp.Length = 32;
                                    break;
                                case EnumDataType.Boolean:
                                    temp.Length = 1;
                                    break;
                                default:
                                    temp.Length = 32;
                                    break;
                            }

                            temp.IoStatus = ioStatus;

                            allDataByMIPCTagName.Add(temp.DataName, temp);

                            if (temp.IPCName != "")
                                allDataByIPCTagName.Add(temp.IPCName, temp);

                            if (!AllDataByClassification.ContainsKey(temp.Classification))
                                AllDataByClassification.Add(temp.Classification, new List<MIPCData>());

                            AllDataByClassification[temp.Classification].Add(temp);

                            if (!pollingGroup.ContainsKey(temp.PollingGroup))
                            {
                                pollingGroup.Add(temp.PollingGroup, new List<MIPCData>());
                                allPollingData.Add(temp.PollingGroup, new MIPCPollingData());
                            }

                            allPollingData[temp.PollingGroup].UpdateData(temp);

                            pollingGroup[temp.PollingGroup].Add(temp);
                        }
                    }
                }

                localData.MIPCData.AllDataByIPCTagName = allDataByIPCTagName;
                localData.MIPCData.AllDataByMIPCTagName = allDataByMIPCTagName;

                return true;
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }

        private bool SetPollingData()
        {
            try
            {
                pollingDataList = new List<MIPCPollingData>();
                pollingIntervalList = new List<int>();

                bool first = true;

                foreach (MIPCPollingData pollingData in allPollingData.Values)
                {
                    pollingData.InitialPollingData();

                    if (pollingData.StartAddress > pollingData.EndAddress)
                        WriteLog(1, "", String.Concat("Fatal :: Group = ", pollingData.GroupNumber.ToString("0"), ", startAddress > endAddress"));

                    if (pollingData.GroupNumber > 0)
                    {
                        pollingDataList.Add(pollingData);

                        if (config.PollingGroupInterval.ContainsKey(pollingData.GroupNumber))
                            pollingIntervalList.Add(config.PollingGroupInterval[pollingData.GroupNumber]);
                        else
                            pollingIntervalList.Add(config.PollingInterval);
                    }
                }

                //UInt32 minStartAddress = 0;
                UInt32 maxEndAddress = 0;
                first = true;

                foreach (MIPCPollingData pollingData in allPollingData.Values)
                {
                    if (first)
                    {
                        first = false;
                        dataStartAddress = pollingData.StartAddress;
                        maxEndAddress = pollingData.EndAddress;
                    }
                    else
                    {
                        if (pollingData.StartAddress < dataStartAddress)
                            dataStartAddress = pollingData.StartAddress;

                        if (pollingData.EndAddress > maxEndAddress)
                            maxEndAddress = pollingData.EndAddress;
                    }
                }

                allData = new byte[(maxEndAddress - dataStartAddress + 1) * 4];

                for (int i = 0; i < pollingDataList.Count; i++)
                {
                    for (int j = i + 1; j < pollingDataList.Count; j++)
                    {
                        if (pollingDataList[i].StartAddress > pollingDataList[j].EndAddress ||
                            pollingDataList[i].EndAddress < pollingDataList[j].StartAddress)
                        {
                        }
                        else
                        {
                            WriteLog(1, "", String.Concat("Address start end 干涉 : ", pollingDataList[i].GroupNumber.ToString("0"), " and ", pollingDataList[j].GroupNumber.ToString("0")));
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }

        private void SetCommandList()
        {
            foreach (EnumMecanumIPCdefaultTag item in (EnumMecanumIPCdefaultTag[])Enum.GetValues(typeof(EnumMecanumIPCdefaultTag)))
            {
                allIPCTageName.Add(item.ToString(), true);
            }

            #region Command-Move.
            bool allFind = true;

            for (int i = 0; i < moveCommandTagNameList.Count && allFind; i++)
            {
                if (!allDataByIPCTagName.ContainsKey(moveCommandTagNameList[i]))
                    allFind = false;
            }

            if (allFind)
            {
                moveCommandAddressArray = new UInt32[moveCommandTagNameList.Count];

                for (int i = 0; i < moveCommandTagNameList.Count; i++)
                    moveCommandAddressArray[i] = allDataByIPCTagName[moveCommandTagNameList[i]].Address;
            }
            #endregion

            #region Command-SetPosition.
            allFind = true;

            for (int i = 0; i < setPositionTagNameList.Count && allFind; i++)
            {
                if (!allDataByIPCTagName.ContainsKey(setPositionTagNameList[i]))
                    allFind = false;
            }

            if (allFind)
            {
                setPositionAddressArray = new UInt32[setPositionTagNameList.Count];

                for (int i = 0; i < setPositionTagNameList.Count; i++)
                    setPositionAddressArray[i] = allDataByIPCTagName[setPositionTagNameList[i]].Address;

                setPositionAddressArray_OnlyOriginData = new UInt32[setPositionTagNameList_OnlyOriginData.Count];

                for (int i = 0; i < setPositionTagNameList_OnlyOriginData.Count; i++)
                    setPositionAddressArray_OnlyOriginData[i] = allDataByIPCTagName[setPositionTagNameList_OnlyOriginData[i]].Address;

                setPositionAddressArray_OnlyOriginOK = new UInt32[setPositionTagNameList_OnlyOriginOK.Count];
                
                for (int i = 0; i < setPositionTagNameList_OnlyOriginOK.Count; i++)
                    setPositionAddressArray_OnlyOriginOK[i] = allDataByIPCTagName[setPositionTagNameList_OnlyOriginOK[i]].Address;
            }
            #endregion

            #region changeEnd.
            //allFind = true;

            //for (int i = 0; i < changeEndTagNameList.Count && allFind; i++)
            //{
            //    if (!allDataByIPCTagName.ContainsKey(changeEndTagNameList[i]))
            //        allFind = false;
            //}

            //if (allFind)
            //{
            //    changeEndAddressArray = new UInt32[changeEndTagNameList.Count];

            //    for (int i = 0; i < changeEndTagNameList.Count; i++)
            //        changeEndAddressArray[i] = allDataByIPCTagName[changeEndTagNameList[i]].Address;
            //}
            #endregion

            #region changeVelocity.
            allFind = true;

            for (int i = 0; i < changeVelociyTagNameList.Count && allFind; i++)
            {
                if (!allDataByIPCTagName.ContainsKey(changeVelociyTagNameList[i]))
                    allFind = false;
            }

            if (allFind)
            {
                changeVelociyAddressArray = new UInt32[changeVelociyTagNameList.Count];

                for (int i = 0; i < changeVelociyTagNameList.Count; i++)
                    changeVelociyAddressArray[i] = allDataByIPCTagName[changeVelociyTagNameList[i]].Address;
            }
            #endregion

            #region Command-Turn.
            //allFind = true;

            //for (int i = 0; i < turnCommandTagNameList.Count && allFind; i++)
            //{
            //    if (!allDataByIPCTagName.ContainsKey(turnCommandTagNameList[i]))
            //        allFind = false;
            //}

            //if (allFind)
            //{
            //    turnCommandAddressArray = new UInt32[turnCommandTagNameList.Count];

            //    for (int i = 0; i < turnCommandTagNameList.Count; i++)
            //        turnCommandAddressArray[i] = allDataByIPCTagName[turnCommandTagNameList[i]].Address;
            //}
            #endregion

            #region Command-Stop.
            allFind = true;

            for (int i = 0; i < stopCommandTagNameList.Count && allFind; i++)
            {
                if (!allDataByIPCTagName.ContainsKey(stopCommandTagNameList[i]))
                    allFind = false;
            }

            if (allFind)
            {
                stopCommandAddressArray = new UInt32[stopCommandTagNameList.Count];

                for (int i = 0; i < stopCommandTagNameList.Count; i++)
                    stopCommandAddressArray[i] = allDataByIPCTagName[stopCommandTagNameList[i]].Address;
            }
            #endregion

            #region Command-Move.
            allFind = true;

            for (int i = 0; i < jogjoystickDataList.Count && allFind; i++)
            {
                if (!allDataByIPCTagName.ContainsKey(jogjoystickDataList[i]))
                    allFind = false;
            }

            if (allFind)
            {
                jogjoystickDataArray = new UInt32[jogjoystickDataList.Count];

                for (int i = 0; i < jogjoystickDataList.Count; i++)
                    jogjoystickDataArray[i] = allDataByIPCTagName[jogjoystickDataList[i]].Address;
            }
            #endregion
        }

        private void ReadXMLAndConfig()
        {
            if (ReadXML())
            {
                localData.MIPCData.Config = config;

                if (ReadConfig(config.MIPCDataConfigPath))
                {
                    SetCommandList();

                    if (SetPollingData())
                        Status = EnumControlStatus.Initial;
                }
            }
        }
        #endregion



        private void ShareMemoryByteArrayUpdate()
        {
            string tempTag ;
            float tempData ;
            float zeroData = 0.0f;

            for (int i = 0; i < shareMemoryTagNameList.Count; i++)
            {
                tempTag = shareMemoryTagNameList[i];


                tempData = localData.MIPCData.GetDataByMIPCTagName(tempTag);
                byte[] dataArray = BitConverter.GetBytes((float)tempData);
                byte[] zeroArray = BitConverter.GetBytes((float)zeroData);

                if (localData.MIPCData.AllDataByMIPCTagName.ContainsKey(tempTag))
                {
                    dataArray.CopyTo(shareMemoryByteArray, i * 4);
                }
                else
                {
                    zeroArray.CopyTo(shareMemoryByteArray, i * 4);
                }
            }

        }
		
        #region Ping.
        private bool mipcPing = true;

        private void PingTestThread()
        {
            try
            {
                bool lastPingOK = true;

                while (Status != EnumControlStatus.Closing)
                {
                    mipcPing = PingTest();

                    if (lastPingOK != mipcPing)
                    {
                        WriteLog(3, "", String.Concat("MIPC Ping 狀態改變成 ", (mipcPing ? "OK" : "NG")));
                        lastPingOK = mipcPing;
                    }

                    Thread.Sleep(localData.PingTestIntervalTime);
                }
            }
            catch (Exception ex)
            {
                WriteLog(1, "", String.Concat("PingThread Exception : ", ex.ToString()));
            }
        }

        private bool PingTest()
        {
            try
            {
                Ping ping = new Ping();
                PingReply result = ping.Send("192.168.29.2", 10000);

                if (result.RoundtripTime > 10)
                    WriteLog(3, "", String.Concat("MIPC Ping Time = ", result.RoundtripTime.ToString("0")));
                //if (result.Status != IPStatus.Success)
                //    WriteLog(3, "", String.Concat("MIPC : Status = ", result.Status.ToString(), ", Time = ", result.RoundtripTime.ToString("0")));
                return (result.Status == IPStatus.Success);
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Ping Excepion : ", ex.ToString()));
                return false;
            }
        }
        #endregion

        #region Initial-Socket,Thread.
        private List<bool> socketListConnect = new List<bool>();

        private bool InitialSocketAndThread()
        {
            try
            {
                Getway通訊正常_XFL.Data = true;
                Getway通訊正常_XFR.Data = true;
                Getway通訊正常_XRL.Data = true;
                Getway通訊正常_XRR.Data = true;

                bool anyError = false;

                for (int i = 0; i < config.PortList.Count; i++)
                {
                    Socket socket;
                    Thread thread;
                    Thread threadProcessData;

                    if (!socketListConnect[i])
                    {
                        try
                        {
                            if (allSocket.ContainsKey(config.PortList[i].SocketName))
                            {
                                allSocket[config.PortList[i].SocketName].Dispose();
                                allSocket.Remove(config.PortList[i].SocketName);
                                Thread.Sleep(1000);
                            }

                            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 5000);
                            socket.Connect(IPAddress.Parse(config.IP), config.PortList[i].PortNumber);

                            if (allSocket.ContainsKey(config.PortList[i].SocketName))
                                allSocket[config.PortList[i].SocketName] = socket;
                            else
                                allSocket.Add(config.PortList[i].SocketName, socket);


                            if (!allReadObject.ContainsKey(config.PortList[i].SocketName))
                                allReadObject.Add(config.PortList[i].SocketName, new object());

                            if (!allWriteObject.ContainsKey(config.PortList[i].SocketName))
                                allWriteObject.Add(config.PortList[i].SocketName, new object());

                            if (allReadQueue.ContainsKey(config.PortList[i].SocketName))
                                allReadQueue[config.PortList[i].SocketName] = new Queue<SendAndReceive>();
                            else
                                allReadQueue.Add(config.PortList[i].SocketName, new Queue<SendAndReceive>());

                            if (allWriteQueue.ContainsKey(config.PortList[i].SocketName))
                                allWriteQueue[config.PortList[i].SocketName] = new Queue<SendAndReceive>();
                            else
                                allWriteQueue.Add(config.PortList[i].SocketName, new Queue<SendAndReceive>());

                            if (processQueue.ContainsKey(config.PortList[i].SocketName))
                                processQueue[config.PortList[i].SocketName] = new Queue<SendAndReceive>();
                            else
                                processQueue.Add(config.PortList[i].SocketName, new Queue<SendAndReceive>());

                            if (needClearBuffer.ContainsKey(config.PortList[i].SocketName))
                                needClearBuffer[config.PortList[i].SocketName] = false;
                            else
                                needClearBuffer.Add(config.PortList[i].SocketName, false);

                            thread = new Thread(new ParameterizedThreadStart(SocketThread));
                            thread.Start((object)i);

                            if (allSocketThread.ContainsKey(config.PortList[i].SocketName))
                                allSocketThread[config.PortList[i].SocketName] = thread;
                            else
                                allSocketThread.Add(config.PortList[i].SocketName, thread);

                            threadProcessData = new Thread(new ParameterizedThreadStart(ProcessQueueThread));
                            threadProcessData.Start((object)i);

                            if (allProcessDataThread.ContainsKey(config.PortList[i].SocketName))
                                allProcessDataThread[config.PortList[i].SocketName] = threadProcessData;
                            else
                                allProcessDataThread.Add(config.PortList[i].SocketName, threadProcessData);

                            socketListConnect[i] = true;
                        }
                        catch (Exception ex)
                        {
                            anyError = true;
                            WriteLog(3, "", String.Concat("socket, Name =  ", config.PortList[i].SocketName, ", port = ", config.PortList[i].PortNumber.ToString("0"), " Connect Exception : ", ex.ToString()));
                        }
                    }
                }

                if (!anyError)
                {
                    double mipcShutDownSOC = localData.BatteryConfig.ShutDownBattery_SOC - 3;
                    double mipcShutDownV = localData.BatteryConfig.ShutDownBattery_Voltage - 0.5;

                    if (mipcShutDownSOC < 0)
                        mipcShutDownSOC = 0;

                    if (mipcShutDownV < 0)
                        mipcShutDownV = 0;

                    SendMIPCDataByIPCTagName(new List<EnumMecanumIPCdefaultTag>() { EnumMecanumIPCdefaultTag.AGV_Type, EnumMecanumIPCdefaultTag.ShutDown_SOC, EnumMecanumIPCdefaultTag.ShutDown_V },
                                             new List<float>() { (float)localData.MainFlowConfig.AGVType, (float)mipcShutDownSOC, (float)mipcShutDownV });

                    switch (localData.MainFlowConfig.AGVType)
                    {
                        case EnumAGVType.UMTC:
                            WriteJogjoystickData(100, 300, 500, 1, 10, 30);
                            break;
                        case EnumAGVType.ATS:
                            DateTime setMIPCTime = DateTime.Now;

                            SendMIPCDataByIPCTagName(new List<EnumMecanumIPCdefaultTag>() {
                                    EnumMecanumIPCdefaultTag.SetMIPCTime_Year,
                                    EnumMecanumIPCdefaultTag.SetMIPCTime_Month,
                                    EnumMecanumIPCdefaultTag.SetMIPCTime_Day,
                                    EnumMecanumIPCdefaultTag.SetMIPCTime_Hour,
                                    EnumMecanumIPCdefaultTag.SetMIPCTime_Minute,
                                    EnumMecanumIPCdefaultTag.SetMIPCTime_Second,
                                    EnumMecanumIPCdefaultTag.SetMIPCTime_SetEnd
                            },
                            new List<float>()
                            {
                                    (float)(setMIPCTime.Year),
                                    (float)(setMIPCTime.Month),
                                    (float)(setMIPCTime.Day),
                                    (float)(setMIPCTime.Hour),
                                    (float)(setMIPCTime.Minute),
                                    (float)(setMIPCTime.Second),
                                    (float)1
                            });
                            break;
                    }

                    communicationErrorCount = 0;
                    pollingThread = new Thread(PollingThread);
                    pollingThread.Start();

                    batteryCSVThread = new Thread(BatteryCSVThread);
                    batteryCSVThread.Start();


                    Status = EnumControlStatus.Ready;

                    ResetMIPCAlarm();
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }
        #endregion

        #region Enqueue/Dequeue.
        private SendAndReceive GetDataFromWriteQueue(string socketName)
        {
            try
            {
                SendAndReceive returnValue = null;

                lock (allWriteObject[socketName])
                {
                    if (allWriteQueue[socketName].Count > 0)
                        returnValue = allWriteQueue[socketName].Dequeue();
                }

                return returnValue;
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return null;
            }
        }

        private SendAndReceive GetDataFromReadQueue(string socketName)
        {
            try
            {
                SendAndReceive returnValue = null;

                lock (allReadObject[socketName])
                {
                    if (allReadQueue[socketName].Count > 0)
                        returnValue = allReadQueue[socketName].Dequeue();
                }

                return returnValue;
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return null;
            }
        }

        private void AddReaeQueue(string socketName, SendAndReceive data, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            try
            {
                lock (allReadObject[socketName])
                {
                    allReadQueue[socketName].Enqueue(data);
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        public void AddWriteQueue(string socketName, SendAndReceive data, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            try
            {
                lock (allWriteObject[socketName])
                {
                    allWriteQueue[socketName].Enqueue(data);
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }
        #endregion

        private void ResetConnectThread()
        {
            WriteLog(7, "", "ResetConnectThread Start");

            if (InitialSocketAndThread())
                ResetAlarmCode(EnumMIPCControlErrorCode.MIPC連線失敗);

            resetAlarm = false;
        }

        private void ResetConnectThread_斷線()
        {
            WriteLog(7, "", "ResetConnectThread Start");

            if (InitialSocketAndThread())
                ResetAlarmCode(EnumMIPCControlErrorCode.MIPC斷線);

            resetAlarm = false;
        }

        public void ResetAlarm()
        {
            SafetySensorControl.ResetAlarm();

            if (localData.MIPCData.CanRightCharging)
                localData.MIPCData.RightChargingPIO.AlarmCodeClear();

            if (localData.MIPCData.CanLeftCharging)
                localData.MIPCData.LeftChargingPIO.AlarmCodeClear();

            localData.MIPCData.MotionAlarm = false;

            if (Status == EnumControlStatus.Ready)
                ResetMIPCAlarm();
        }

        private bool SendAndReceiveBySocket(Socket socket, SendAndReceive data)
        {
            try
            {
                if (config.LogMode)
                    WriteSocketLog(7, "Send", String.Concat("Seq[", data.Send.SeqNumber.ToString("0"), "]StaNum[", data.Send.StationNo.ToString("0"),
                                                            "]FunCode[", data.Send.FunctionCode.ToString("0"), "]Len[", data.Send.DataLength.ToString("0"),
                                                            "]Address[", data.Send.StartAddress.ToString("0"), "]\r\n", BitConverter.ToString(data.Send.ByteData)));

                byte[] receiveData = new byte[data.Send.ReceiveLength];

                Stopwatch sendReceiveTimer = new Stopwatch();
                sendReceiveTimer.Restart();

                socket.Send(data.Send.ByteData, 0, data.Send.ByteData.GetLength(0), SocketFlags.None);
                data.Time = DateTime.Now;

                socket.ReceiveTimeout = config.SocketTimeoutValue;
                socket.Receive(receiveData, 0, data.Send.ReceiveLength, SocketFlags.None);

                sendReceiveTimer.Stop();
                data.ScanTime = sendReceiveTimer.ElapsedMilliseconds / 2;

                if (config.LogMode)
                    WriteSocketLog(7, "Receive", BitConverter.ToString(receiveData));

                data.Receive = new ModbusData();
                data.Receive.ByteData = receiveData;

                return true;
            }
            catch (Exception ex)
            {
                data.Result = EnumSendAndRecieve.Error;
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }

        private void ClearBuffer(Socket socket)
        {
            WriteLog(7, "", "Clear Buffer Clear");

            int clearDataSize = 0;

            try
            {
                byte[] receiveData = new byte[1024];

                socket.ReceiveTimeout = 10;
                int receiveDataLength = 1024;

                while (receiveDataLength == 1024)
                {
                    receiveDataLength = socket.Receive(receiveData, 0, 1024, SocketFlags.None);
                    clearDataSize += receiveDataLength;
                }
            }
            catch { }

            WriteLog(7, "", String.Concat("Clear Buffer End, Clear Buffer Size : ", clearDataSize.ToString()));
        }

        private bool ProcessReceiveData(SendAndReceive data)
        {
            try
            {
                data.Receive.GetDataByByteData();

                if (data.Send.FunctionCode == 0x87)
                    return false;

                if (data.Send.SeqNumber != data.Receive.SeqNumber ||
                    data.Send.StationNo != data.Receive.StationNo ||
                    data.Send.FunctionCode != data.Receive.FunctionCode)
                {
                    WriteLog(3, "", "SeqNumber/StationNo/FunctionCode send and recieve不符合");
                    data.Result = EnumSendAndRecieve.Error;
                    return false;
                }
                else if (data.Receive.StartAddress != 0)
                {
                    WriteLog(3, "", "Recieve return code (Address) != 0");
                    data.Result = EnumSendAndRecieve.Error;
                    return false;
                }

                switch (data.Send.FunctionCode)
                {
                    case 0x01:
                        // 連續.
                        for (int i = 0; i < data.Receive.DataLength; i++)
                        {
                            for (int j = 0; j < 4; j++)
                                allData[(data.Send.StartAddress - dataStartAddress + i) * 4 + 3 - j] = data.Receive.ByteData[12 + i * 4 + j];
                        }

                        data.Result = EnumSendAndRecieve.OK;
                        break;

                    case 0x03:
                        for (int i = 0; i < data.Receive.DataLength; i++)
                        {
                            for (int j = 0; j < 4; j++)
                                allData[(BitConverter.ToUInt32(data.Send.DataBuffer, i * 4) - dataStartAddress) * 4 + 3 - j] = data.Receive.ByteData[12 + i * 4 + j];
                        }

                        data.Result = EnumSendAndRecieve.OK;
                        break;

                    case 0x81:
                    case 0x83:
                        data.Result = EnumSendAndRecieve.OK;

                        break;
                    default:
                        WriteLog(3, "", String.Concat("RecieveData Function Code : ", data.Receive.FunctionCode.ToString("0"), ", return false"));
                        data.Result = EnumSendAndRecieve.Error;
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }

        private Dictionary<EnumMIPCControlErrorCode, Stopwatch> errorTimer = new Dictionary<EnumMIPCControlErrorCode, Stopwatch>();
        private Dictionary<EnumMIPCControlErrorCode, bool> errorDelayiing = new Dictionary<EnumMIPCControlErrorCode, bool>();

        private void SetErrorCodeAndDelayTime(EnumMIPCControlErrorCode errorCode, bool result)
        {
            if (!errorTimer.ContainsKey(errorCode))
            {
                errorTimer.Add(errorCode, new Stopwatch());
                errorDelayiing.Add(errorCode, false);
            }

            if (result)
            {
                if (errorDelayiing[errorCode])
                {
                    if (errorTimer[errorCode].ElapsedMilliseconds > localData.BatteryConfig.AlarmDelayTime)
                        SetAlarmCode(errorCode);
                }
                else
                {
                    errorTimer[errorCode].Restart();
                    errorDelayiing[errorCode] = true;
                }
            }
            else
            {
                ResetAlarmCode(errorCode);
                errorTimer[errorCode].Stop();
                errorDelayiing[errorCode] = false;
            }
        }

        private void GetDataValueByGroup(int index, DateTime getDataTime, double scanTime)
        {
            string tempTag = "";

            try
            {
                MIPCData temp;
                EnumMecanumIPCdefaultTag enumTagName;

                for (int i = 0; i < pollingGroup[index].Count; i++)
                {
                    temp = pollingGroup[index][i];
                    temp.LastObject = temp.Object;

                    #region 依照不同資料存取型別做處理, 但是目前都只有用Float.
                    switch (temp.DataType)
                    {
                        case EnumDataType.Boolean:
                            /// addres -> one bool.
                            byte[] boolDataAarray = new byte[4] { allData[(temp.Address - dataStartAddress) * 4 + 3],
                                                                  allData[(temp.Address - dataStartAddress) * 4 + 2],
                                                                  allData[(temp.Address - dataStartAddress) * 4 + 1],
                                                                  allData[(temp.Address - dataStartAddress) * 4 + 0]};

                            UInt32 boolValue = BitConverter.ToUInt32(boolDataAarray, 0);

                            if (boolValue == 0)
                            {
                                temp.Object = (object)((uint)0);
                                temp.Value = "0";
                            }
                            else
                            {
                                temp.Object = (object)((uint)1);
                                temp.Value = "1";
                            }

                            break;
                        case EnumDataType.UInt32:
                            byte[] uint32DataAarray = new byte[4] { allData[(temp.Address - dataStartAddress) * 4 + 3],
                                                                    allData[(temp.Address - dataStartAddress) * 4 + 2],
                                                                    allData[(temp.Address - dataStartAddress) * 4 + 1],
                                                                    allData[(temp.Address - dataStartAddress) * 4 + 0]};

                            UInt32 uint32Value = BitConverter.ToUInt32(uint32DataAarray, 0);

                            temp.Object = (object)uint32Value;
                            temp.Value = uint32Value.ToString("0");
                            break;
                        case EnumDataType.Int32:
                            byte[] int32DataAarray = new byte[4] { allData[(temp.Address - dataStartAddress) * 4 + 3],
                                                                   allData[(temp.Address - dataStartAddress) * 4 + 2],
                                                                   allData[(temp.Address - dataStartAddress) * 4 + 1],
                                                                   allData[(temp.Address - dataStartAddress) * 4 + 0]};

                            Int32 int32Value = BitConverter.ToInt32(int32DataAarray, 0);

                            temp.Object = (object)int32Value;
                            temp.Value = int32Value.ToString("0");
                            break;
                        case EnumDataType.Double_1:
                            byte[] double_1DataAarray = new byte[4] { allData[(temp.Address - dataStartAddress) * 4 + 3],
                                                                      allData[(temp.Address - dataStartAddress) * 4 + 2],
                                                                      allData[(temp.Address - dataStartAddress) * 4 + 1],
                                                                      allData[(temp.Address - dataStartAddress) * 4 + 0]};

                            double double_1Value = BitConverter.ToInt32(double_1DataAarray, 0) / 10;

                            temp.Object = (object)double_1Value;
                            temp.Value = double_1Value.ToString("0.0");
                            break;

                        case EnumDataType.Float:
                            byte[] double_DataAarray = new byte[4] { allData[(temp.Address - dataStartAddress) * 4 + 3],
                                                                     allData[(temp.Address - dataStartAddress) * 4 + 2],
                                                                     allData[(temp.Address - dataStartAddress) * 4 + 1],
                                                                     allData[(temp.Address - dataStartAddress) * 4 + 0]};

                            float double_Value = BitConverter.ToSingle(double_DataAarray, 0);
                            temp.LastObject = temp.Object;
                            temp.Object = (object)double_Value;
                            temp.Value = double_Value.ToString("0.0");

                            break;
                        default:
                            byte[] defaultArray = new byte[4] { allData[(temp.Address - dataStartAddress) * 4 + 3],
                                                                allData[(temp.Address - dataStartAddress) * 4 + 2],
                                                                allData[(temp.Address - dataStartAddress) * 4 + 1],
                                                                allData[(temp.Address - dataStartAddress) * 4 + 0]};

                            float defaultFloat = BitConverter.ToSingle(defaultArray, 0);

                            temp.Object = (object)defaultFloat;
                            temp.Value = defaultFloat.ToString("0.0");
                            break;
                    }
                    #endregion

                    try
                    {
                        // 這邊依照 D:\MecanumConfigs\MIPCControl\案子名稱\MIPCDataConfig.csv 內的 IPCName 屬性 做資料處理/Assign.
                        if (allIPCTageName.ContainsKey(temp.IPCName) && Enum.TryParse(temp.IPCName, out enumTagName))
                        {
                            switch (enumTagName)
                            {
                                #region 麥克阿姆輪共用Feedback.
                                case EnumMecanumIPCdefaultTag.Feedback_Theta:
                                    LocateAGVPosition newAGVPosition = new LocateAGVPosition();
                                    newAGVPosition.GetDataTime = getDataTime;
                                    newAGVPosition.ScanTime = scanTime;

                                    newAGVPosition.AGVPosition.Angle = computeFunction.GetCurrectAngle((float)(temp.Object));

                                    if (allIPCTageName.ContainsKey(EnumMecanumIPCdefaultTag.Feedback_X.ToString()))
                                        newAGVPosition.AGVPosition.Position.Y = (float)(allDataByIPCTagName[EnumMecanumIPCdefaultTag.Feedback_X.ToString()].Object);

                                    if (allIPCTageName.ContainsKey(EnumMecanumIPCdefaultTag.Feedback_Y.ToString()))
                                        newAGVPosition.AGVPosition.Position.X = (float)(allDataByIPCTagName[EnumMecanumIPCdefaultTag.Feedback_Y.ToString()].Object);

                                    localData.MoveControlData.MotionControlData.EncoderAGVPosition = newAGVPosition;

                                    break;
                                case EnumMecanumIPCdefaultTag.Feedback_MoveStatus:
                                    EnumAxisMoveStatus tempMoveStatus = (((float)(temp.Object) != 0) ? EnumAxisMoveStatus.Move : EnumAxisMoveStatus.Stop);

                                    if (tempMoveStatus != localData.MoveControlData.MotionControlData.moveStatus)
                                    {
                                        WriteMotionLog(7, "", String.Concat("MoveStatus Change : ", tempMoveStatus.ToString()));

                                        if (tempMoveStatus == EnumAxisMoveStatus.Stop && Math.Abs(localData.MoveControlData.MotionControlData.LineVelocity) > 5)
                                            WriteMotionLog(5, "", String.Concat("目前線速度為 : ", localData.MoveControlData.MotionControlData.LineVelocity.ToString("0"), " 但訊號變為Stop"));

                                        if (tempMoveStatus == EnumAxisMoveStatus.Stop && Math.Abs(localData.MoveControlData.MotionControlData.ThetaVelocity) > 1)
                                            WriteMotionLog(5, "", String.Concat("目前角速度為 : ", localData.MoveControlData.MotionControlData.ThetaVelocity.ToString("0"), " 但訊號變為Stop"));
                                    }

                                    tempMoveStatus = (((float)(temp.Object) != 0 ||
                                                      Math.Abs(localData.MoveControlData.MotionControlData.LineVelocity) > 5 ||
                                                      Math.Abs(localData.MoveControlData.MotionControlData.ThetaVelocity) > 1) ? EnumAxisMoveStatus.Move : EnumAxisMoveStatus.Stop);

                                    localData.MoveControlData.MotionControlData.MoveStatus = tempMoveStatus;
                                    break;

                                case EnumMecanumIPCdefaultTag.Feedback_線速度:
                                    localData.MoveControlData.MotionControlData.LineVelocity = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Feedback_線速度方向:
                                    localData.MoveControlData.MotionControlData.LineVelocityAngle = computeFunction.GetCurrectAngle((float)(temp.Object));
                                    break;
                                case EnumMecanumIPCdefaultTag.Feedback_線加速度:
                                    localData.MoveControlData.MotionControlData.LineAcc = computeFunction.GetCurrectAngle((float)(temp.Object));
                                    break;
                                case EnumMecanumIPCdefaultTag.Feedback_線減速度:
                                    localData.MoveControlData.MotionControlData.LineDec = computeFunction.GetCurrectAngle((float)(temp.Object));
                                    break;
                                case EnumMecanumIPCdefaultTag.Feedback_線急跳度:
                                    localData.MoveControlData.MotionControlData.LineJerk = computeFunction.GetCurrectAngle((float)(temp.Object));
                                    break;
                                case EnumMecanumIPCdefaultTag.Feedback_角速度:
                                    localData.MoveControlData.MotionControlData.ThetaVelocity = computeFunction.GetCurrectAngle((float)(temp.Object));
                                    break;
                                case EnumMecanumIPCdefaultTag.Feedback_角加速度:
                                    localData.MoveControlData.MotionControlData.ThetaAcc = computeFunction.GetCurrectAngle((float)(temp.Object));
                                    break;
                                case EnumMecanumIPCdefaultTag.Feedback_角減速度:
                                    localData.MoveControlData.MotionControlData.ThetaDec = computeFunction.GetCurrectAngle((float)(temp.Object));
                                    break;
                                case EnumMecanumIPCdefaultTag.Feedback_角急跳度:
                                    localData.MoveControlData.MotionControlData.ThetaJerk = computeFunction.GetCurrectAngle((float)(temp.Object));
                                    break;
                                case EnumMecanumIPCdefaultTag.Feedback_TimeStamp:
                                    TimeStampData nowTimeStamp = timeStampData;

                                    if (nowTimeStamp == null || nowTimeStamp.Time.Day != getDataTime.Day)
                                    {
                                        TimeStampData newTimeStampData = new TimeStampData();
                                        newTimeStampData.Time = getDataTime.AddMilliseconds(-scanTime - (double)((float)(temp.Object)));
                                        newTimeStampData.GetTime = newTimeStampData.Time;
                                        newTimeStampData.SendTime = newTimeStampData.Time;

                                        if (nowTimeStamp == null)
                                            WriteMotionLog(7, "", String.Concat("newTimeStamp : ", newTimeStampData.Time.ToString("HH:mm:ss.fff")));
                                        else
                                            WriteMotionLog(7, "", String.Concat("newTimeStamp : ", newTimeStampData.Time.ToString("HH:mm:ss.fff"),
                                                                          ", delta time : ", (nowTimeStamp.GetTime - newTimeStampData.GetTime).TotalMilliseconds), " ms");

                                        timeStampData = newTimeStampData;
                                    }

                                    break;
                                case EnumMecanumIPCdefaultTag.Feedback_X_VelocityCommand:
                                    localData.MoveControlData.MotionControlData.X_VelocityCommand = (float)temp.Object;
                                    break;
                                case EnumMecanumIPCdefaultTag.MIPCVersion:
                                    if (temp.LastObject == null)
                                        WriteLog(7, "", String.Concat("MIPC Version : ", ((float)temp.Object).ToString("0.0"), " / ",
                                                                      localData.MIPCData_MIPCVersion.ToString("0.0"), " (config)"));

                                    localData.MIPCVersion = (float)temp.Object;
                                    break;
                                case EnumMecanumIPCdefaultTag.MotionVersion:
                                    if (temp.LastObject == null)
                                        WriteLog(7, "", String.Concat("Motion Version : ", ((float)temp.Object).ToString("0.0"), " / ",
                                                                      localData.MIPCData_MotionVersion.ToString("0.0"), " (config)"));

                                    localData.MotionVersion = (float)temp.Object;
                                    break;
                                case EnumMecanumIPCdefaultTag.Feedback_X_VelocityFeedback:
                                    localData.MoveControlData.MotionControlData.X_VelocityFeedback = (float)temp.Object;
                                    break;
                                case EnumMecanumIPCdefaultTag.Feedback_Y_VelocityCommand:
                                    localData.MoveControlData.MotionControlData.Y_VelocityCommand = (float)temp.Object;
                                    break;
                                case EnumMecanumIPCdefaultTag.Feedback_Y_VelocityFeedback:
                                    localData.MoveControlData.MotionControlData.Y_VelocityFeedback = (float)temp.Object;
                                    break;
                                case EnumMecanumIPCdefaultTag.Feedback_Theta_VelocityCommand:
                                    localData.MoveControlData.MotionControlData.Theta_VelocityCommand = (float)temp.Object;
                                    break;
                                case EnumMecanumIPCdefaultTag.Feedback_Theta_VelocityFeedback:
                                    localData.MoveControlData.MotionControlData.Theta_VelocityFeedback = (float)temp.Object;
                                    break;
                                #endregion
                                #region Motion控制, 各種速度誤差.
                                case EnumMecanumIPCdefaultTag.XVelocityError:
                                    localData.MoveControlData.MotionControlData.XVelocityError = (float)temp.Object;
                                    break;
                                case EnumMecanumIPCdefaultTag.YVelocityError:
                                    localData.MoveControlData.MotionControlData.YVelocityError = (float)temp.Object;
                                    break;
                                case EnumMecanumIPCdefaultTag.ThetaVelocityError:
                                    localData.MoveControlData.MotionControlData.ThetaVelocityError = (float)temp.Object;
                                    break;
                                case EnumMecanumIPCdefaultTag.Slam_XVelocityError:
                                    localData.MoveControlData.MotionControlData.Slam_XVelocityError = (float)temp.Object;
                                    break;
                                case EnumMecanumIPCdefaultTag.Slam_YVelocityError:
                                    localData.MoveControlData.MotionControlData.Slam_YVelocityError = (float)temp.Object;
                                    break;
                                case EnumMecanumIPCdefaultTag.Slam_ThetaVelocityError:
                                    localData.MoveControlData.MotionControlData.Slam_ThetaVelocityError = (float)temp.Object;
                                    break;
                                #endregion

                                #region Motion 單軸資訊-Elmo, 不確定西班牙?那家會不會改回饋資料.
                                case EnumMecanumIPCdefaultTag.XRR_QA:
                                    uint servoOnOffStatus = 0;
                                    AxisFeedbackData tempAxisFeedbackData;
                                    EnumDefaultAxisName axisName;

                                    #region axisData lsit.
                                    for (int axisIndex = 0; axisIndex < axisList.Count; axisIndex++)
                                    {
                                        tempAxisFeedbackData = new AxisFeedbackData();
                                        axisName = axisList[axisIndex];
                                        tempAxisFeedbackData.GetDataTime = getDataTime;

                                        tempTag = String.Concat(axisName.ToString(), "_", DefaultAxisTag.ServoStatus.ToString());

                                        if (allDataByIPCTagName.ContainsKey(tempTag))
                                            tempAxisFeedbackData.AxisServoOnOff = (localData.MIPCData.GetDataByIPCTagName(tempTag) == 1) ? EnumAxisServoOnOff.ServoOn : EnumAxisServoOnOff.ServoOff;

                                        tempTag = String.Concat(axisName.ToString(), "_", DefaultAxisTag.Encoder.ToString());

                                        if (allDataByIPCTagName.ContainsKey(tempTag))
                                            tempAxisFeedbackData.Position = localData.MIPCData.GetDataByIPCTagName(tempTag);

                                        tempTag = String.Concat(axisName.ToString(), "_", DefaultAxisTag.RPM.ToString());

                                        if (allDataByIPCTagName.ContainsKey(tempTag))
                                            tempAxisFeedbackData.Velocity = localData.MIPCData.GetDataByIPCTagName(tempTag);

                                        tempTag = String.Concat(axisName.ToString(), "_", DefaultAxisTag.DA.ToString());

                                        if (allDataByIPCTagName.ContainsKey(tempTag))
                                            tempAxisFeedbackData.DA = localData.MIPCData.GetDataByIPCTagName(tempTag);

                                        tempTag = String.Concat(axisName.ToString(), "_", DefaultAxisTag.QA.ToString());

                                        if (allDataByIPCTagName.ContainsKey(tempTag))
                                            tempAxisFeedbackData.QA = localData.MIPCData.GetDataByIPCTagName(tempTag);

                                        tempTag = String.Concat(axisName.ToString(), "_", DefaultAxisTag.V.ToString());

                                        if (allDataByIPCTagName.ContainsKey(tempTag))
                                            tempAxisFeedbackData.V = localData.MIPCData.GetDataByIPCTagName(tempTag);

                                        tempTag = String.Concat(axisName.ToString(), "_", DefaultAxisTag.EC.ToString());

                                        if (allDataByIPCTagName.ContainsKey(tempTag))
                                            tempAxisFeedbackData.EC = localData.MIPCData.GetDataByIPCTagName(tempTag);

                                        tempTag = String.Concat(axisName.ToString(), "_", DefaultAxisTag.MF.ToString());

                                        if (allDataByIPCTagName.ContainsKey(tempTag))
                                            tempAxisFeedbackData.MF = localData.MIPCData.GetDataByIPCTagName(tempTag);

                                        tempTag = String.Concat(axisName.ToString(), "_", DefaultAxisTag.溫度.ToString());

                                        if (allDataByIPCTagName.ContainsKey(tempTag))
                                            tempAxisFeedbackData.Temperature = localData.MIPCData.GetDataByIPCTagName(tempTag);

                                        tempTag = String.Concat(axisName.ToString(), "_", DefaultAxisTag.GetwayError.ToString());

                                        if (allDataByIPCTagName.ContainsKey(tempTag))
                                            tempAxisFeedbackData.GetwayError = localData.MIPCData.GetDataByIPCTagName(tempTag);

                                        switch (axisName)
                                        {
                                            case EnumDefaultAxisName.XFL:
                                                Getway通訊正常_XFL.Data = tempAxisFeedbackData.GetwayError != 1;

                                                if (Getway通訊正常_XFL.Data)
                                                    ResetAlarmCode(EnumMIPCControlErrorCode.Getway通訊異常_XFL);
                                                else
                                                    SetAlarmCode(EnumMIPCControlErrorCode.Getway通訊異常_XFL);
                                                break;
                                            case EnumDefaultAxisName.XFR:
                                                Getway通訊正常_XFR.Data = tempAxisFeedbackData.GetwayError != 1;

                                                if (Getway通訊正常_XFR.Data)
                                                    ResetAlarmCode(EnumMIPCControlErrorCode.Getway通訊異常_XFR);
                                                else
                                                    SetAlarmCode(EnumMIPCControlErrorCode.Getway通訊異常_XFR);
                                                break;
                                            case EnumDefaultAxisName.XRL:
                                                Getway通訊正常_XRL.Data = tempAxisFeedbackData.GetwayError != 1;

                                                if (Getway通訊正常_XRL.Data)
                                                    ResetAlarmCode(EnumMIPCControlErrorCode.Getway通訊異常_XRL);
                                                else
                                                    SetAlarmCode(EnumMIPCControlErrorCode.Getway通訊異常_XRL);
                                                break;
                                            case EnumDefaultAxisName.XRR:
                                                Getway通訊正常_XRR.Data = tempAxisFeedbackData.GetwayError != 1;

                                                if (Getway通訊正常_XRR.Data)
                                                    ResetAlarmCode(EnumMIPCControlErrorCode.Getway通訊異常_XRR);
                                                else
                                                    SetAlarmCode(EnumMIPCControlErrorCode.Getway通訊異常_XRR);
                                                break;
                                        }

                                        tempTag = String.Concat(axisName.ToString(), "_", DefaultAxisTag.SR.ToString());

                                        if (allDataByIPCTagName.ContainsKey(tempTag))
                                            tempAxisFeedbackData.SR = localData.MIPCData.GetDataByIPCTagName(tempTag);

                                        tempTag = String.Concat(axisName.ToString(), "_", DefaultAxisTag.IP.ToString());

                                        if (allDataByIPCTagName.ContainsKey(tempTag))
                                            tempAxisFeedbackData.IP = localData.MIPCData.GetDataByIPCTagName(tempTag);

                                        tempTag = String.Concat(axisName.ToString(), "_", DefaultAxisTag.VelocityCommand.ToString());

                                        if (allDataByIPCTagName.ContainsKey(tempTag))
                                            tempAxisFeedbackData.VelocityCommand = localData.MIPCData.GetDataByIPCTagName(tempTag);

                                        tempTag = String.Concat(axisName.ToString(), "_", DefaultAxisTag.VelocityFeedback.ToString());

                                        if (allDataByIPCTagName.ContainsKey(tempTag))
                                            tempAxisFeedbackData.VelocityFeedback = localData.MIPCData.GetDataByIPCTagName(tempTag);

                                        tempTag = String.Concat(axisName.ToString(), "_", DefaultAxisTag.PWM.ToString());

                                        if (allDataByIPCTagName.ContainsKey(tempTag))
                                            tempAxisFeedbackData.PWM = localData.MIPCData.GetDataByIPCTagName(tempTag);

                                        tempTag = String.Concat(axisName.ToString(), "_", DefaultAxisTag.Driver_Encoder.ToString());

                                        if (allDataByIPCTagName.ContainsKey(tempTag))
                                            tempAxisFeedbackData.Driver_Encoder = localData.MIPCData.GetDataByIPCTagName(tempTag);

                                        tempTag = String.Concat(axisName.ToString(), "_", DefaultAxisTag.Driver_RPM.ToString());

                                        if (allDataByIPCTagName.ContainsKey(tempTag))
                                            tempAxisFeedbackData.Driver_RPM = localData.MIPCData.GetDataByIPCTagName(tempTag);

                                        servoOnOffStatus = ((servoOnOffStatus << 1) + (uint)(tempAxisFeedbackData.AxisServoOnOff == EnumAxisServoOnOff.ServoOn ? 1 : 0));

                                        if (localData.MoveControlData.MotionControlData.AllAxisFeedbackData.ContainsKey(axisName))
                                        {
                                            if (localData.MoveControlData.MotionControlData.AllAxisFeedbackData[axisName] != null)
                                            {
                                                if (localData.MoveControlData.MotionControlData.AllAxisFeedbackData[axisName].EC != tempAxisFeedbackData.EC)
                                                    WriteMotionLog(5, "", String.Concat(axisName.ToString(), " EC(Change) : ", tempAxisFeedbackData.EC.ToString("0")));

                                                if (localData.MoveControlData.MotionControlData.AllAxisFeedbackData[axisName].MF != tempAxisFeedbackData.MF)
                                                    WriteMotionLog(5, "", String.Concat(axisName.ToString(), " MF(Change) : ", tempAxisFeedbackData.MF.ToString("0")));
                                            }

                                            localData.MoveControlData.MotionControlData.AllAxisFeedbackData[axisName] = tempAxisFeedbackData;
                                        }
                                        else
                                        {
                                            WriteMotionLog(7, "", String.Concat(axisName.ToString(), " EC(Initial) : ", tempAxisFeedbackData.EC.ToString("0")));
                                            WriteMotionLog(7, "", String.Concat(axisName.ToString(), " MF(Initial) : ", tempAxisFeedbackData.MF.ToString("0")));

                                            localData.MoveControlData.MotionControlData.AllAxisFeedbackData.Add(axisName, tempAxisFeedbackData);

                                        }
                                    }
                                    #endregion

                                    localData.MoveControlData.MotionControlData.AllServoStatus = servoOnOffStatus;
                                    break;
                                #endregion

                                #region Slam-自動重定位資訊.
                                case EnumMecanumIPCdefaultTag.SetPosition_Origin_OK:
                                    if (localData.MoveControlData.LocateControlData.AutoSetPositionStatus == EnumSlamAutoSetPosition.WaitMIPCSlamData)
                                    {
                                        if ((float)temp.Object != 0)
                                        {
                                            MapAGVPosition slamOrigin = new MapAGVPosition();

                                            slamOrigin.Position.X = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.SetPosition_OriginX.ToString());
                                            slamOrigin.Position.Y = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.SetPosition_OriginY.ToString());
                                            slamOrigin.Angle = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.SetPosition_OriginTheta.ToString());

                                            localData.MoveControlData.LocateControlData.AutoSetSlamPositionData = slamOrigin;
                                            localData.MoveControlData.LocateControlData.AutoSetPositionStatus = EnumSlamAutoSetPosition.WaitSlamDataOK;

                                            WriteLog(7, "", String.Concat("AutoSetPosition Change to WaitSlamDataOK\r\n",
                                                                          "( ", slamOrigin.Position.X.ToString("0"),
                                                                          ", ", slamOrigin.Position.Y.ToString("0"),
                                                                          " ) ", slamOrigin.Angle.ToString("0.0")));
                                        }
                                        else
                                        {
                                            localData.MoveControlData.LocateControlData.AutoSetPositionStatus = EnumSlamAutoSetPosition.End;
                                            WriteLog(7, "", "AutoSetPosition Change to End (SetPosition_Origin_OK = false)");
                                        }
                                    }
                                    break;
                                #endregion
                                #region 搖桿資訊.
                                case EnumMecanumIPCdefaultTag.JoystickOnOff:
                                    localData.MoveControlData.MotionControlData.JoystickMode = ((float)(temp.Object) == 1);
                                    break;
                                case EnumMecanumIPCdefaultTag.Joystick_LineVelocity:
                                    localData.MoveControlData.MotionControlData.JoystickLineAxisData.Velocity = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Joystick_LineAcc:
                                    localData.MoveControlData.MotionControlData.JoystickLineAxisData.Acceleration = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Joystick_LineDec:
                                    localData.MoveControlData.MotionControlData.JoystickLineAxisData.Deceleration = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Joystick_ThetaVelocity:
                                    localData.MoveControlData.MotionControlData.JoystickThetaAxisData.Velocity = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Joystick_ThetaAcc:
                                    localData.MoveControlData.MotionControlData.JoystickThetaAxisData.Acceleration = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Joystick_ThetaDec:
                                    localData.MoveControlData.MotionControlData.JoystickThetaAxisData.Deceleration = (float)(temp.Object);
                                    break;
                                #endregion
                                #region MIPC-Motion測試用log.
                                case EnumMecanumIPCdefaultTag.MIPC_Test30:
                                    for (int j = 1; j <= 30; j++)
                                        localData.MIPCData.MIPCTestArray[j - 1] = localData.MIPCData.GetDataByIPCTagName(String.Concat("MIPC_Test", j.ToString()));
                                    break;
                                #endregion
                                #region 應該是電池皆有的資訊.
                                case EnumMecanumIPCdefaultTag.Battery_SOC:
                                    localData.BatteryInfo.Battery_SOC = (float)(temp.Object);
                                    localData.BatteryInfo.LowSOC_Warn = (localData.BatteryInfo.Battery_SOC < localData.BatteryConfig.LowBattery_SOC);
                                    localData.BatteryInfo.ShutDownSOC_Alarm = (localData.BatteryInfo.Battery_SOC < localData.BatteryConfig.ShutDownBattery_SOC);
                                    break;
                                case EnumMecanumIPCdefaultTag.Battery_V:
                                    localData.BatteryInfo.Battery_V = (float)(temp.Object);
                                    localData.BatteryInfo.LowV_Warn = (localData.BatteryInfo.Battery_V < localData.BatteryConfig.LowBattery_Voltage);
                                    localData.BatteryInfo.ShutDownV_Alarm = (localData.BatteryInfo.Battery_V < localData.BatteryConfig.ShutDownBattery_Voltage);
                                    break;
                                case EnumMecanumIPCdefaultTag.Battery_A:
                                    localData.BatteryInfo.Battery_A = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Battery_溫度1:
                                    localData.BatteryInfo.Battery_溫度1 = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Battery_溫度2:
                                    localData.BatteryInfo.Battery_溫度2 = (float)(temp.Object);
                                    break;
                                #endregion
                                #region 智慧電表.
                                case EnumMecanumIPCdefaultTag.Meter_V:
                                    localData.BatteryInfo.Meter_V = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Meter_A:
                                    localData.BatteryInfo.Meter_A = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Meter_W:
                                    localData.BatteryInfo.Meter_W = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Meter_WH:
                                    localData.BatteryInfo.Meter_WH = (float)(temp.Object);
                                    break;
                                #endregion
                                #region 台塑電池電芯電壓.
                                case EnumMecanumIPCdefaultTag.Cell_1:
                                    localData.BatteryInfo.CellArray[0] = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Cell_2:
                                    localData.BatteryInfo.CellArray[1] = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Cell_3:
                                    localData.BatteryInfo.CellArray[2] = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Cell_4:
                                    localData.BatteryInfo.CellArray[3] = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Cell_5:
                                    localData.BatteryInfo.CellArray[4] = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Cell_6:
                                    localData.BatteryInfo.CellArray[5] = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Cell_7:
                                    localData.BatteryInfo.CellArray[6] = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Cell_8:
                                    localData.BatteryInfo.CellArray[7] = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Cell_9:
                                    localData.BatteryInfo.CellArray[8] = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Cell_10:
                                    localData.BatteryInfo.CellArray[9] = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Cell_11:
                                    localData.BatteryInfo.CellArray[10] = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Cell_12:
                                    localData.BatteryInfo.CellArray[11] = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Cell_13:
                                    localData.BatteryInfo.CellArray[12] = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Cell_14:
                                    localData.BatteryInfo.CellArray[13] = (float)(temp.Object);
                                    break;
                                case EnumMecanumIPCdefaultTag.Cell_15:
                                    localData.BatteryInfo.CellArray[14] = (float)(temp.Object);
                                    break;
                                #endregion
                                #region MIPC AlarmCode.
                                case EnumMecanumIPCdefaultTag.MIPCAlarmCode_1:
                                case EnumMecanumIPCdefaultTag.MIPCAlarmCode_2:
                                case EnumMecanumIPCdefaultTag.MIPCAlarmCode_3:
                                case EnumMecanumIPCdefaultTag.MIPCAlarmCode_4:
                                case EnumMecanumIPCdefaultTag.MIPCAlarmCode_5:
                                case EnumMecanumIPCdefaultTag.MIPCAlarmCode_6:
                                case EnumMecanumIPCdefaultTag.MIPCAlarmCode_7:
                                case EnumMecanumIPCdefaultTag.MIPCAlarmCode_8:
                                case EnumMecanumIPCdefaultTag.MIPCAlarmCode_9:
                                case EnumMecanumIPCdefaultTag.MIPCAlarmCode_10:
                                case EnumMecanumIPCdefaultTag.MIPCAlarmCode_11:
                                case EnumMecanumIPCdefaultTag.MIPCAlarmCode_12:
                                case EnumMecanumIPCdefaultTag.MIPCAlarmCode_13:
                                case EnumMecanumIPCdefaultTag.MIPCAlarmCode_14:
                                case EnumMecanumIPCdefaultTag.MIPCAlarmCode_15:
                                case EnumMecanumIPCdefaultTag.MIPCAlarmCode_16:
                                case EnumMecanumIPCdefaultTag.MIPCAlarmCode_17:
                                case EnumMecanumIPCdefaultTag.MIPCAlarmCode_18:
                                case EnumMecanumIPCdefaultTag.MIPCAlarmCode_19:
                                case EnumMecanumIPCdefaultTag.MIPCAlarmCode_20:
                                    if ((float)temp.Object != 0)
                                        SetAlarmCode(ipcDefaultTagToAlarmCode[enumTagName]);
                                    else if ((float)temp.Object == 0)
                                        ResetAlarmCode(ipcDefaultTagToAlarmCode[enumTagName]);

                                    if (enumTagName == EnumMecanumIPCdefaultTag.MIPCAlarmCode_6)
                                        localData.MoveControlData.NeedResetMotionAlarm = (float)(temp.Object) != 0;

                                    if (enumTagName == EnumMecanumIPCdefaultTag.MIPCAlarmCode_3)
                                        localData.MIPCData.MotionAlarm = (float)(temp.Object) != 0;

                                    if (temp.LastObject == null && ((float)temp.Object) != 0)
                                        WriteMotionLog(5, "", String.Concat("MIPCAlarm : ", enumTagName.ToString(), " value Change to ", ((float)temp.Object).ToString("0")));
                                    else if (temp.LastObject != null && ((float)temp.Object) != ((float)temp.LastObject) && ((float)temp.Object) != 0)
                                        WriteMotionLog(5, "", String.Concat("MIPCAlarm : ", enumTagName.ToString(), " value Change to ", ((float)temp.Object).ToString("0")));
                                    break;
                                #endregion
                                #region 有動力電訊號.
                                case EnumMecanumIPCdefaultTag.SafetyRelay:
                                    if ((float)temp.Object != 0)
                                        ResetAlarmCode(EnumMIPCControlErrorCode.SafetyRelayNotOK);
                                    else if ((float)temp.Object == 0)
                                        SetAlarmCode(EnumMIPCControlErrorCode.SafetyRelayNotOK);

                                    localData.MIPCData.U動力電 = ((float)(temp.Object) != 0);
                                    break;
                                #endregion
                                #region Reset-Button.
                                case EnumMecanumIPCdefaultTag.Reset_Front:
                                case EnumMecanumIPCdefaultTag.Reset_Back:
                                    if ((float)temp.Object != 0 && (temp.LastObject == null || (float)temp.LastObject == 0))
                                        SendMainFlowResetAlarm();
                                    break;
                                #endregion
                                #region Start-Button.
                                case EnumMecanumIPCdefaultTag.Start_Front:
                                    localData.MIPCData.StartButton_Front = ((float)temp.Object != 0);
                                    break;
                                case EnumMecanumIPCdefaultTag.Start_Back:
                                    localData.MIPCData.StartButton_Back = ((float)temp.Object != 0);
                                    break;
                                #endregion
                                #region 解剎車 - 有依照不同案子定義.
                                case EnumMecanumIPCdefaultTag.BrakeRelease_Front:
                                    if (allIPCTageName.ContainsKey(EnumMecanumIPCdefaultTag.Auto_Signal.ToString()))
                                        localData.MIPCData.BreakRelease_Front = ((float)temp.Object != 0 && localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Auto_Signal.ToString()) == 0);
                                    else if (allIPCTageName.ContainsKey(EnumMecanumIPCdefaultTag.Manual_Signal.ToString()))
                                        localData.MIPCData.BreakRelease_Front = ((float)temp.Object != 0 && localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Manual_Signal.ToString()) != 0);
                                    else
                                        localData.MIPCData.BreakRelease_Front = (float)temp.Object != 0;

                                    if (localData.MIPCData.BreakRelease)
                                        SetAlarmCode(EnumMIPCControlErrorCode.解剎車中);
                                    else
                                        ResetAlarmCode(EnumMIPCControlErrorCode.解剎車中);

                                    switch (localData.MainFlowConfig.AGVType)
                                    {
                                        case EnumAGVType.UMTC:
                                            localData.MIPCData.ButtonPause_Front = ((float)temp.Object != 0);
                                            break;
                                        case EnumAGVType.AGC:
                                            break;
                                        case EnumAGVType.PTI:
                                            break;
                                        case EnumAGVType.ATS:
                                            break;
                                        default:
                                            break;
                                    }

                                    localData.MoveControlData.SensorStatus.ButtonPause = localData.MIPCData.ButtonPause;
                                    break;
                                case EnumMecanumIPCdefaultTag.BrakeRelease_Back:
                                    if (allIPCTageName.ContainsKey(EnumMecanumIPCdefaultTag.Auto_Signal.ToString()))
                                        localData.MIPCData.BreakRelease_Back = ((float)temp.Object != 0 && localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Auto_Signal.ToString()) == 0);
                                    else if (allIPCTageName.ContainsKey(EnumMecanumIPCdefaultTag.Manual_Signal.ToString()))
                                        localData.MIPCData.BreakRelease_Back = ((float)temp.Object != 0 && localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Manual_Signal.ToString()) != 0);
                                    else
                                        localData.MIPCData.BreakRelease_Back = (float)temp.Object != 0;

                                    if (localData.MIPCData.BreakRelease)
                                        SetAlarmCode(EnumMIPCControlErrorCode.解剎車中);
                                    else
                                        ResetAlarmCode(EnumMIPCControlErrorCode.解剎車中);

                                    switch (localData.MainFlowConfig.AGVType)
                                    {
                                        case EnumAGVType.UMTC:
                                            localData.MIPCData.ButtonPause_Back = ((float)temp.Object != 0);
                                            break;
                                        case EnumAGVType.AGC:
                                            break;
                                        case EnumAGVType.PTI:
                                            break;
                                        case EnumAGVType.ATS:
                                            break;
                                        default:
                                            break;
                                    }

                                    localData.MoveControlData.SensorStatus.ButtonPause = localData.MIPCData.ButtonPause;
                                    break;
                                #endregion
                                #region RS485_Error - 有依照不同案子不同定義.
                                case EnumMecanumIPCdefaultTag.RS485_Error:
                                    switch (localData.MainFlowConfig.AGVType)
                                    {
                                        case EnumAGVType.UMTC:
                                            if ((((uint)((float)temp.Object)) & 0b1) != 0)
                                                localData.BatteryInfo.Battery_DisConnect = true;
                                            else
                                                localData.BatteryInfo.Battery_DisConnect = false;

                                            SetErrorCodeAndDelayTime(EnumMIPCControlErrorCode.溫度通訊異常,
                                                (((uint)((float)temp.Object)) & 0b10) != 0);

                                            SetErrorCodeAndDelayTime(EnumMIPCControlErrorCode.電表通訊異常,
                                                (((uint)((float)temp.Object)) & 0b100) != 0);

                                            break;
                                        case EnumAGVType.AGC:
                                            break;
                                        case EnumAGVType.PTI:
                                            break;
                                        case EnumAGVType.ATS:
                                            break;
                                        default:
                                            break;
                                    }
                                    break;
                                #endregion
                                #region Battery_Alarm.
                                case EnumMecanumIPCdefaultTag.Battery_Alarm:
                                    if ((float)temp.Object != 0)
                                        SetAlarmCode(EnumMIPCControlErrorCode.電池BMS異常);
                                    else if ((float)temp.Object == 0)
                                        ResetAlarmCode(EnumMIPCControlErrorCode.電池BMS異常);

                                    if (localData.BatteryInfo.BatteryAlarm != (float)temp.Object && (float)temp.Object != 0)
                                        WriteLog(5, "", String.Concat("BatteryAlarm Change : ", ((float)temp.Object).ToString()));

                                    localData.BatteryInfo.BatteryAlarm = (float)temp.Object;
                                    break;
                                #endregion
                                #region Auto/Manual 硬體切換.
                                case EnumMecanumIPCdefaultTag.Auto_Signal:
                                    if (((float)temp.Object != 0) &&
                                        (temp.LastObject != null && (float)temp.LastObject == 0))
                                        ChangeAutoManual(EnumAutoState.Auto);
                                    else if (((float)temp.Object == 0) &&
                                             (temp.LastObject == null || (float)temp.LastObject != 0))
                                        ChangeAutoManual(EnumAutoState.Manual);
                                    break;
                                case EnumMecanumIPCdefaultTag.Manual_Signal:
                                    if (((float)temp.Object != 0) &&
                                        (temp.LastObject == null || (float)temp.LastObject == 0))
                                        ChangeAutoManual(EnumAutoState.Manual);
                                    else if (((float)temp.Object == 0) &&
                                             (temp.LastObject != null && (float)temp.LastObject != 0))
                                        ChangeAutoManual(EnumAutoState.Auto);
                                    break;
                                #endregion
                                #region UMTC-取放專用.
                                case EnumMecanumIPCdefaultTag.Z軸_HM7:
                                    #region Z軸.
                                    tempAxisFeedbackData = new AxisFeedbackData();
                                    tempAxisFeedbackData.GetDataTime = getDataTime;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_Encoder.ToString()))
                                        tempAxisFeedbackData.Position = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Z軸_Encoder.ToString());

                                    tempAxisFeedbackData.OriginPosition = tempAxisFeedbackData.Position;

                                    if (localData.LoadUnloadData.CVEncoderOffsetValue.ContainsKey(EnumLoadUnloadAxisName.Z軸.ToString()))
                                        tempAxisFeedbackData.Position -= localData.LoadUnloadData.CVEncoderOffsetValue[EnumLoadUnloadAxisName.Z軸.ToString()];

                                    if (localData.LoadUnloadData.CVHomeOffsetValue.ContainsKey(EnumLoadUnloadAxisName.Z軸.ToString()))
                                        tempAxisFeedbackData.Position -= localData.LoadUnloadData.CVHomeOffsetValue[EnumLoadUnloadAxisName.Z軸.ToString()];

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_RPM.ToString()))
                                        tempAxisFeedbackData.Velocity = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Z軸_RPM.ToString());

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_ServoStatus.ToString()))
                                        tempAxisFeedbackData.AxisServoOnOff = (localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Z軸_ServoStatus.ToString()) == 1) ? EnumAxisServoOnOff.ServoOn : EnumAxisServoOnOff.ServoOff;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_Stop.ToString()))
                                        tempAxisFeedbackData.AxisMoveStaus = (localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Z軸_Stop.ToString()) == 2) ? EnumAxisMoveStatus.Move : EnumAxisMoveStatus.Stop;

                                    if (tempAxisFeedbackData.AxisServoOnOff == EnumAxisServoOnOff.ServoOff)
                                        tempAxisFeedbackData.AxisMoveStaus = EnumAxisMoveStatus.Stop;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_EC.ToString()))
                                        tempAxisFeedbackData.EC = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Z軸_EC.ToString());

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_MF.ToString()))
                                        tempAxisFeedbackData.MF = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Z軸_MF.ToString());

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_SR.ToString()))
                                        tempAxisFeedbackData.SR = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Z軸_SR.ToString());

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_HM1.ToString()))
                                        tempAxisFeedbackData.HM1 = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Z軸_HM1.ToString());

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_HM7.ToString()))
                                        tempAxisFeedbackData.HM7 = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Z軸_HM7.ToString());

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_DA.ToString()))
                                        tempAxisFeedbackData.DA = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Z軸_DA.ToString());

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_QA.ToString()))
                                        tempAxisFeedbackData.QA = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Z軸_QA.ToString());

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_V.ToString()))
                                        tempAxisFeedbackData.V = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Z軸_V.ToString());

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_溫度.ToString()))
                                        tempAxisFeedbackData.Temperature = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Z軸_溫度.ToString());

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_PositionCommand.ToString()))
                                        tempAxisFeedbackData.PositionCommand = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Z軸_PositionCommand.ToString());

                                    if (localData.LoadUnloadData.CVFeedbackData.ContainsKey(EnumLoadUnloadAxisName.Z軸.ToString()))
                                        localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()] = tempAxisFeedbackData;
                                    #endregion
                                    break;
                                case EnumMecanumIPCdefaultTag.Z軸_Slave_HM7:
                                    #region Z軸_Slave.
                                    tempAxisFeedbackData = new AxisFeedbackData();

                                    tempAxisFeedbackData.GetDataTime = getDataTime;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_Slave_Encoder.ToString()))
                                        tempAxisFeedbackData.Position = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Z軸_Slave_Encoder.ToString()].Object;

                                    tempAxisFeedbackData.OriginPosition = tempAxisFeedbackData.Position;

                                    if (localData.LoadUnloadData.CVEncoderOffsetValue.ContainsKey(EnumLoadUnloadAxisName.Z軸_Slave.ToString()))
                                        tempAxisFeedbackData.Position -= localData.LoadUnloadData.CVEncoderOffsetValue[EnumLoadUnloadAxisName.Z軸_Slave.ToString()];

                                    if (localData.LoadUnloadData.CVHomeOffsetValue.ContainsKey(EnumLoadUnloadAxisName.Z軸_Slave.ToString()))
                                        tempAxisFeedbackData.Position -= localData.LoadUnloadData.CVHomeOffsetValue[EnumLoadUnloadAxisName.Z軸_Slave.ToString()];

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_Slave_RPM.ToString()))
                                        tempAxisFeedbackData.Velocity = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Z軸_Slave_RPM.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_Slave_ServoStatus.ToString()))
                                        tempAxisFeedbackData.AxisServoOnOff = ((float)(allDataByIPCTagName[EnumMecanumIPCdefaultTag.Z軸_Slave_ServoStatus.ToString()].Object) == 1) ? EnumAxisServoOnOff.ServoOn : EnumAxisServoOnOff.ServoOff;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_Slave_Stop.ToString()))
                                        tempAxisFeedbackData.AxisMoveStaus = ((float)(allDataByIPCTagName[EnumMecanumIPCdefaultTag.Z軸_Slave_Stop.ToString()].Object) == 2) ? EnumAxisMoveStatus.Move : EnumAxisMoveStatus.Stop;

                                    if (tempAxisFeedbackData.AxisServoOnOff == EnumAxisServoOnOff.ServoOff)
                                        tempAxisFeedbackData.AxisMoveStaus = EnumAxisMoveStatus.Stop;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_Slave_EC.ToString()))
                                        tempAxisFeedbackData.EC = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Z軸_Slave_EC.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_Slave_MF.ToString()))
                                        tempAxisFeedbackData.MF = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Z軸_Slave_MF.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_Slave_SR.ToString()))
                                        tempAxisFeedbackData.SR = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Z軸_Slave_SR.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_Slave_HM1.ToString()))
                                        tempAxisFeedbackData.HM1 = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Z軸_Slave_HM1.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_Slave_HM7.ToString()))
                                        tempAxisFeedbackData.HM7 = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Z軸_Slave_HM7.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_Slave_DA.ToString()))
                                        tempAxisFeedbackData.DA = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Z軸_Slave_DA.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_Slave_QA.ToString()))
                                        tempAxisFeedbackData.QA = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Z軸_Slave_QA.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_Slave_V.ToString()))
                                        tempAxisFeedbackData.V = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Z軸_Slave_V.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_Slave_溫度.ToString()))
                                        tempAxisFeedbackData.Temperature = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Z軸_Slave_溫度.ToString());

                                    if (localData.LoadUnloadData.CVFeedbackData.ContainsKey(EnumLoadUnloadAxisName.Z軸_Slave.ToString()))
                                        localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()] = tempAxisFeedbackData;
                                    #endregion
                                    break;
                                case EnumMecanumIPCdefaultTag.P軸_HM7:
                                    #region P軸.
                                    tempAxisFeedbackData = new AxisFeedbackData();

                                    tempAxisFeedbackData.GetDataTime = getDataTime;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.P軸_Encoder.ToString()))
                                        tempAxisFeedbackData.Position = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.P軸_Encoder.ToString()].Object;

                                    tempAxisFeedbackData.OriginPosition = tempAxisFeedbackData.Position;

                                    if (localData.LoadUnloadData.CVEncoderOffsetValue.ContainsKey(EnumLoadUnloadAxisName.P軸.ToString()))
                                        tempAxisFeedbackData.Position -= localData.LoadUnloadData.CVEncoderOffsetValue[EnumLoadUnloadAxisName.P軸.ToString()];

                                    if (localData.LoadUnloadData.CVHomeOffsetValue.ContainsKey(EnumLoadUnloadAxisName.P軸.ToString()))
                                        tempAxisFeedbackData.Position -= localData.LoadUnloadData.CVHomeOffsetValue[EnumLoadUnloadAxisName.P軸.ToString()];

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.P軸_RPM.ToString()))
                                        tempAxisFeedbackData.Velocity = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.P軸_RPM.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.P軸_ServoStatus.ToString()))
                                        tempAxisFeedbackData.AxisServoOnOff = ((float)(allDataByIPCTagName[EnumMecanumIPCdefaultTag.P軸_ServoStatus.ToString()].Object) == 1) ? EnumAxisServoOnOff.ServoOn : EnumAxisServoOnOff.ServoOff;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.P軸_Stop.ToString()))
                                        tempAxisFeedbackData.AxisMoveStaus = ((float)(allDataByIPCTagName[EnumMecanumIPCdefaultTag.P軸_Stop.ToString()].Object) == 2) ? EnumAxisMoveStatus.Move : EnumAxisMoveStatus.Stop;

                                    if (tempAxisFeedbackData.AxisServoOnOff == EnumAxisServoOnOff.ServoOff)
                                        tempAxisFeedbackData.AxisMoveStaus = EnumAxisMoveStatus.Stop;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.P軸_EC.ToString()))
                                        tempAxisFeedbackData.EC = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.P軸_EC.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.P軸_MF.ToString()))
                                        tempAxisFeedbackData.MF = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.P軸_MF.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.P軸_SR.ToString()))
                                        tempAxisFeedbackData.SR = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.P軸_SR.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.P軸_HM1.ToString()))
                                        tempAxisFeedbackData.HM1 = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.P軸_HM1.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.P軸_HM7.ToString()))
                                        tempAxisFeedbackData.HM7 = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.P軸_HM7.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.P軸_DA.ToString()))
                                        tempAxisFeedbackData.DA = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.P軸_DA.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.P軸_QA.ToString()))
                                        tempAxisFeedbackData.QA = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.P軸_QA.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.P軸_V.ToString()))
                                        tempAxisFeedbackData.V = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.P軸_V.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.P軸_溫度.ToString()))
                                        tempAxisFeedbackData.Temperature = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.P軸_溫度.ToString());

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.P軸_PositionCommand.ToString()))
                                        tempAxisFeedbackData.PositionCommand = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.P軸_PositionCommand.ToString());

                                    if (localData.LoadUnloadData.CVFeedbackData.ContainsKey(EnumLoadUnloadAxisName.P軸.ToString()))
                                        localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()] = tempAxisFeedbackData;
                                    #endregion
                                    break;
                                case EnumMecanumIPCdefaultTag.Theta軸_HM7:
                                    #region Theta.
                                    tempAxisFeedbackData = new AxisFeedbackData();

                                    tempAxisFeedbackData.GetDataTime = getDataTime;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Theta軸_Encoder.ToString()))
                                        tempAxisFeedbackData.Position = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Theta軸_Encoder.ToString()].Object;

                                    tempAxisFeedbackData.OriginPosition = tempAxisFeedbackData.Position;

                                    if (localData.LoadUnloadData.CVEncoderOffsetValue.ContainsKey(EnumLoadUnloadAxisName.Theta軸.ToString()))
                                        tempAxisFeedbackData.Position -= localData.LoadUnloadData.CVEncoderOffsetValue[EnumLoadUnloadAxisName.Theta軸.ToString()];

                                    if (localData.LoadUnloadData.CVHomeOffsetValue.ContainsKey(EnumLoadUnloadAxisName.Theta軸.ToString()))
                                        tempAxisFeedbackData.Position -= localData.LoadUnloadData.CVHomeOffsetValue[EnumLoadUnloadAxisName.Theta軸.ToString()];

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Theta軸_RPM.ToString()))
                                        tempAxisFeedbackData.Velocity = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Theta軸_RPM.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Theta軸_ServoStatus.ToString()))
                                        tempAxisFeedbackData.AxisServoOnOff = ((float)(allDataByIPCTagName[EnumMecanumIPCdefaultTag.Theta軸_ServoStatus.ToString()].Object) == 1) ? EnumAxisServoOnOff.ServoOn : EnumAxisServoOnOff.ServoOff;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Theta軸_Stop.ToString()))
                                        tempAxisFeedbackData.AxisMoveStaus = ((float)(allDataByIPCTagName[EnumMecanumIPCdefaultTag.Theta軸_Stop.ToString()].Object) == 2) ? EnumAxisMoveStatus.Move : EnumAxisMoveStatus.Stop;

                                    if (tempAxisFeedbackData.AxisServoOnOff == EnumAxisServoOnOff.ServoOff)
                                        tempAxisFeedbackData.AxisMoveStaus = EnumAxisMoveStatus.Stop;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Theta軸_EC.ToString()))
                                        tempAxisFeedbackData.EC = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Theta軸_EC.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Theta軸_MF.ToString()))
                                        tempAxisFeedbackData.MF = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Theta軸_MF.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Theta軸_SR.ToString()))
                                        tempAxisFeedbackData.SR = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Theta軸_SR.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Theta軸_HM1.ToString()))
                                        tempAxisFeedbackData.HM1 = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Theta軸_HM1.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Theta軸_HM7.ToString()))
                                        tempAxisFeedbackData.HM7 = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Theta軸_HM7.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Theta軸_DA.ToString()))
                                        tempAxisFeedbackData.DA = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Theta軸_DA.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Theta軸_QA.ToString()))
                                        tempAxisFeedbackData.QA = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Theta軸_QA.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Theta軸_V.ToString()))
                                        tempAxisFeedbackData.V = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Theta軸_V.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Theta軸_溫度.ToString()))
                                        tempAxisFeedbackData.Temperature = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Theta軸_溫度.ToString());

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Theta軸_PositionCommand.ToString()))
                                        tempAxisFeedbackData.PositionCommand = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Theta軸_PositionCommand.ToString());

                                    if (localData.LoadUnloadData.CVFeedbackData.ContainsKey(EnumLoadUnloadAxisName.Theta軸.ToString()))
                                        localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()] = tempAxisFeedbackData;
                                    #endregion
                                    break;
                                case EnumMecanumIPCdefaultTag.Roller_HM7:
                                    #region Roller.
                                    tempAxisFeedbackData = new AxisFeedbackData();

                                    tempAxisFeedbackData.GetDataTime = getDataTime;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Roller_Encoder.ToString()))
                                        tempAxisFeedbackData.Position = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Roller_Encoder.ToString()].Object;

                                    tempAxisFeedbackData.OriginPosition = tempAxisFeedbackData.Position;

                                    if (localData.LoadUnloadData.CVEncoderOffsetValue.ContainsKey(EnumLoadUnloadAxisName.Roller.ToString()))
                                        tempAxisFeedbackData.Position -= localData.LoadUnloadData.CVEncoderOffsetValue[EnumLoadUnloadAxisName.Roller.ToString()];

                                    if (localData.LoadUnloadData.CVHomeOffsetValue.ContainsKey(EnumLoadUnloadAxisName.Roller.ToString()))
                                        tempAxisFeedbackData.Position -= localData.LoadUnloadData.CVHomeOffsetValue[EnumLoadUnloadAxisName.Roller.ToString()];

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Roller_RPM.ToString()))
                                        tempAxisFeedbackData.Velocity = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Roller_RPM.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Roller_ServoStatus.ToString()))
                                        tempAxisFeedbackData.AxisServoOnOff = ((float)(allDataByIPCTagName[EnumMecanumIPCdefaultTag.Roller_ServoStatus.ToString()].Object) == 1) ? EnumAxisServoOnOff.ServoOn : EnumAxisServoOnOff.ServoOff;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Roller_Stop.ToString()))
                                        tempAxisFeedbackData.AxisMoveStaus = ((float)(allDataByIPCTagName[EnumMecanumIPCdefaultTag.Roller_Stop.ToString()].Object) == 2) ? EnumAxisMoveStatus.Move : EnumAxisMoveStatus.Stop;

                                    if (tempAxisFeedbackData.AxisServoOnOff == EnumAxisServoOnOff.ServoOff)
                                        tempAxisFeedbackData.AxisMoveStaus = EnumAxisMoveStatus.Stop;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Roller_EC.ToString()))
                                        tempAxisFeedbackData.EC = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Roller_EC.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Roller_MF.ToString()))
                                        tempAxisFeedbackData.MF = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Roller_MF.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Roller_SR.ToString()))
                                        tempAxisFeedbackData.SR = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Roller_SR.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Roller_HM1.ToString()))
                                        tempAxisFeedbackData.HM1 = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Roller_HM1.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Roller_HM7.ToString()))
                                        tempAxisFeedbackData.HM7 = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Roller_HM7.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Roller_DA.ToString()))
                                        tempAxisFeedbackData.DA = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Roller_DA.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Roller_QA.ToString()))
                                        tempAxisFeedbackData.QA = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Roller_QA.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Roller_V.ToString()))
                                        tempAxisFeedbackData.V = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Roller_V.ToString()].Object;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Roller_溫度.ToString()))
                                        tempAxisFeedbackData.Temperature = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Roller_溫度.ToString());

                                    if (localData.LoadUnloadData.CVFeedbackData.ContainsKey(EnumLoadUnloadAxisName.Roller.ToString()))
                                        localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Roller.ToString()] = tempAxisFeedbackData;
                                    #endregion
                                    break;
                                case EnumMecanumIPCdefaultTag.Z軸Encoder歪掉:
                                    #region Z軸Encoder歪掉.
                                    if ((float)temp.Object != 0)
                                    {
                                        localData.LoadUnloadData.Z軸主從誤差過大 = true;
                                        localData.LoadUnloadData.Z軸主從誤差 = (float)temp.Object;
                                    }
                                    else
                                    {
                                        localData.LoadUnloadData.Z軸主從誤差過大 = false;
                                        localData.LoadUnloadData.Z軸主從誤差 = 0;
                                    }
                                    #endregion
                                    break;
                                case EnumMecanumIPCdefaultTag.Encoder已回Home:
                                    #region Encoder已回Home.
                                    if (!localData.LoadUnloadData.EncoderOffset讀取)
                                    {
                                        if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸EncoderOffset.ToString()) &&
                                            allDataByIPCTagName[EnumMecanumIPCdefaultTag.Z軸EncoderOffset.ToString()].Object != null &&
                                            allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Z軸_SlaveEncoderOffset.ToString()) &&
                                            allDataByIPCTagName[EnumMecanumIPCdefaultTag.Z軸_SlaveEncoderOffset.ToString()].Object != null &&
                                            allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.P軸EncoderOffset.ToString()) &&
                                            allDataByIPCTagName[EnumMecanumIPCdefaultTag.P軸EncoderOffset.ToString()].Object != null &&
                                            allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Theta軸EncoderOffset.ToString()) &&
                                            allDataByIPCTagName[EnumMecanumIPCdefaultTag.Theta軸EncoderOffset.ToString()].Object != null)
                                        {
                                            if ((float)temp.Object != 0)
                                            {
                                                localData.LoadUnloadData.Z軸EncoderOffset = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Z軸EncoderOffset.ToString()].Object;
                                                localData.LoadUnloadData.Z軸_SlaveEncoderOffset = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Z軸_SlaveEncoderOffset.ToString()].Object;
                                                localData.LoadUnloadData.P軸EncoderOffset = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.P軸EncoderOffset.ToString()].Object;
                                                localData.LoadUnloadData.Theta軸EncoderOffset = (float)allDataByIPCTagName[EnumMecanumIPCdefaultTag.Theta軸EncoderOffset.ToString()].Object;

                                                localData.LoadUnloadData.Encoder已回Home = true;
                                            }
                                        }

                                        localData.LoadUnloadData.EncoderOffset讀取 = true;
                                    }
                                    #endregion
                                    break;

                                //liu++
                                #region Y軸.
                                case EnumMecanumIPCdefaultTag.Y軸_HM7:
                                    tempAxisFeedbackData = new AxisFeedbackData();
                                    tempAxisFeedbackData.GetDataTime = getDataTime;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Y軸_Encoder.ToString()))
                                        tempAxisFeedbackData.Position = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Y軸_Encoder.ToString());

                                    tempAxisFeedbackData.OriginPosition = tempAxisFeedbackData.Position;

                                    if (localData.LoadUnloadData.CVEncoderOffsetValue.ContainsKey(EnumLoadUnloadAxisName.Y軸.ToString()))
                                        tempAxisFeedbackData.Position -= localData.LoadUnloadData.CVEncoderOffsetValue[EnumLoadUnloadAxisName.Y軸.ToString()];

                                    if (localData.LoadUnloadData.CVHomeOffsetValue.ContainsKey(EnumLoadUnloadAxisName.Y軸.ToString()))
                                        tempAxisFeedbackData.Position -= localData.LoadUnloadData.CVHomeOffsetValue[EnumLoadUnloadAxisName.Y軸.ToString()];

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Y軸_RPM.ToString()))
                                        tempAxisFeedbackData.Velocity = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Y軸_RPM.ToString());

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Y軸_ServoStatus.ToString()))
                                        tempAxisFeedbackData.AxisServoOnOff = (localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Y軸_ServoStatus.ToString()) == 1) ? EnumAxisServoOnOff.ServoOn : EnumAxisServoOnOff.ServoOff;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Y軸_Stop.ToString()))
                                        tempAxisFeedbackData.AxisMoveStaus = (localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Y軸_Stop.ToString()) == 2) ? EnumAxisMoveStatus.Move : EnumAxisMoveStatus.Stop;

                                    if (tempAxisFeedbackData.AxisServoOnOff == EnumAxisServoOnOff.ServoOff)
                                        tempAxisFeedbackData.AxisMoveStaus = EnumAxisMoveStatus.Stop;

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Y軸_EC.ToString()))
                                        tempAxisFeedbackData.EC = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Y軸_EC.ToString());

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Y軸_MF.ToString()))
                                        tempAxisFeedbackData.MF = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Y軸_MF.ToString());

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Y軸_SR.ToString()))
                                        tempAxisFeedbackData.SR = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Y軸_SR.ToString());

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Y軸_HM1.ToString()))
                                        tempAxisFeedbackData.HM1 = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Y軸_HM1.ToString());

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Y軸_HM7.ToString()))
                                        tempAxisFeedbackData.HM7 = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Y軸_HM7.ToString());

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Y軸_DA.ToString()))
                                        tempAxisFeedbackData.DA = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Y軸_DA.ToString());

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Y軸_QA.ToString()))
                                        tempAxisFeedbackData.QA = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Y軸_QA.ToString());

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Y軸_V.ToString()))
                                        tempAxisFeedbackData.V = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Y軸_V.ToString());

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Y軸_溫度.ToString()))
                                        tempAxisFeedbackData.Temperature = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Y軸_溫度.ToString());

                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Y軸_PositionCommand.ToString()))
                                        tempAxisFeedbackData.PositionCommand = localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.Y軸_PositionCommand.ToString());

                                    if (localData.LoadUnloadData.CVFeedbackData.ContainsKey(EnumLoadUnloadAxisName.Y軸.ToString()))
                                        localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Y軸.ToString()] = tempAxisFeedbackData;
                                    break;
                                #endregion
                                #endregion
                                default:
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteLog(7, "", String.Concat("Case : ", temp.IPCName, ", Exception : ", ex.ToString()));
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        private void ChangeAutoManual(EnumAutoState autoManual)
        {
            if (localData.MIPCData.StartProcessReceiveData)
                AutoManualEvent?.Invoke(this, autoManual);
        }

        private int receiveDataErrorCount = 0;
        private int receiveDataErrorMaxCount = 10;

        private void ProcessQueueThread(object index)
        {
            try
            {
                string socketName = config.PortList[(int)index].SocketName;

                Socket socket = allSocket[socketName];

                Stopwatch processTimer = new Stopwatch();
                SendAndReceive data;
                double assignTime = 0;

                while (Status != EnumControlStatus.Closing && Status != EnumControlStatus.Error)
                {
                    lock (processQueue[socketName])
                    {
                        if (processQueue[socketName].Count > 0)
                            data = processQueue[socketName].Dequeue();
                        else
                            data = null;
                    }

                    if (data != null)
                    {
                        if (data.IsMotionCommand)
                            data.Result = EnumSendAndRecieve.OK;
                        else
                        {
                            processTimer.Restart();

                            if (ProcessReceiveData(data))
                            {
                                assignTime = processTimer.ElapsedMilliseconds;

                                if (localData.MIPCData.StartProcessReceiveData)
                                {
                                    if (data.PollingGroup > 0)
                                        GetDataValueByGroup(data.PollingGroup, data.Time, data.ScanTime);
                                }

                                ResetAlarmCode(EnumMIPCControlErrorCode.MIPC回傳資料異常);
                            }
                            else
                            {
                                assignTime = processTimer.ElapsedMilliseconds;

                                SetAlarmCode(EnumMIPCControlErrorCode.MIPC回傳資料異常);

                                needClearBuffer[socketName] = true;

                                if (localData.CoverMIPCBug)
                                {
                                    receiveDataErrorCount++;

                                    if (receiveDataErrorCount >= receiveDataErrorMaxCount)
                                    {
                                        for (int i = 0; i < socketListConnect.Count; i++)
                                            socketListConnect[i] = false;

                                        WriteLog(5, "", String.Concat("通訊資料異常連續次數超過 : ", changeErrorCount.ToString(), "次, 切成Error狀態"));
                                        SetAlarmCode(EnumMIPCControlErrorCode.MIPC斷線);
                                        Status = EnumControlStatus.Error;
                                    }
                                }
                            }

                            processTimer.Stop();

                            localData.TestString = String.Concat(processTimer.ElapsedMilliseconds.ToString("0"), " ms");

                            if (processTimer.ElapsedMilliseconds > 20)
                                WriteLog(5, "", String.Concat("Polling Group = ", data.PollingGroup.ToString(), ", 處理時間過長 = ", processTimer.ElapsedMilliseconds.ToString("0.0"), ", assign Time = ", assignTime.ToString("0"), " ms"));
                        }
                    }

                    //SpinWait.SpinUntil(() => false, 1);
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("ProcessQueueThread Exception : ", ex.ToString()));
            }
        }

        private void SocketThread(object index)
        {
            try
            {
                string socketName = config.PortList[(int)index].SocketName;

                Socket socket = allSocket[socketName];
                SendAndReceive sendAndRecieve;

                while (Status != EnumControlStatus.Closing && Status != EnumControlStatus.Error)
                {
                    if (needClearBuffer[socketName])
                    {
                        ClearBuffer(socket);
                        needClearBuffer[socketName] = false;
                    }

                    sendAndRecieve = GetDataFromWriteQueue(socketName);

                    if (sendAndRecieve == null)
                        sendAndRecieve = GetDataFromReadQueue(socketName);

                    if (sendAndRecieve != null)
                    {
                        if (SendAndReceiveBySocket(socket, sendAndRecieve))
                        {
                            lock (processQueue[socketName])
                            {
                                processQueue[socketName].Enqueue(sendAndRecieve);
                            }

                            ResetAlarmCode(EnumMIPCControlErrorCode.MIPC通訊異常);
                        }
                        else
                        {
                            communicationErrorCount++;
                            SetAlarmCode(EnumMIPCControlErrorCode.MIPC通訊異常);

                            if (communicationErrorCount > changeErrorCount)
                            {
                                for (int i = 0; i < socketListConnect.Count; i++)
                                    socketListConnect[i] = false;

                                WriteLog(5, "", String.Concat("通訊失敗連續次數超過 : ", changeErrorCount.ToString(), "次, 切成Error狀態"));
                                SetAlarmCode(EnumMIPCControlErrorCode.MIPC斷線);
                                Status = EnumControlStatus.Error;
                            }
                        }
                    }

                    Thread.Sleep(1);
                    //SpinWait.SpinUntil(() => false, 1);
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Socket Thread Exception : ", ex.ToString()));
            }
        }

        private int communicationErrorCount = 0;
        private int changeErrorCount = 10;

        private bool CheckAllSockeThreadEnd()
        {
            foreach (Thread thread in allSocketThread.Values)
            {
                if (thread != null && thread.IsAlive)
                    return false;
            }

            foreach (Thread thread in allProcessDataThread.Values)
            {
                if (thread != null && thread.IsAlive)
                    return false;
            }

            return true;
        }

        private Stopwatch heartbeatTimer = new Stopwatch();

        public void WriteHeartBeat()
        {
            try
            {
                if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Heartbeat_IPC.ToString()))
                {
                    if (sendHeartbeat == null || sendHeartbeat.Result != EnumSendAndRecieve.None)
                    {
                        if (heartbeatTimer.ElapsedMilliseconds > config.HeartbeatInterval * 2)
                            WriteLog(3, "", "PoolingThreadVearyLag");

                        heartbeatTimer.Restart();

                        if (sendHeartbeat != null && sendHeartbeat.Result == EnumSendAndRecieve.Error)
                            WriteLog(7, "", "HeartBeat Receive Error");

                        lastMoveControlHeartBeat = MoveControlHeartBeat;
                        ipcHeartbeatNumber++;

                        sendHeartbeat = new SendAndReceive();
                        List<Byte[]> writeData = new List<byte[]>();
                        writeData.Add(BitConverter.GetBytes((float)ipcHeartbeatNumber));
                        sendHeartbeat.Send = Write_連續(allDataByIPCTagName[EnumMecanumIPCdefaultTag.Heartbeat_IPC.ToString()].Address, 1, writeData);
                        sendHeartbeat.IsHearBeat = true;

                        AddWriteQueue(EnumMIPCSocketName.Normal.ToString(), sendHeartbeat);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        private Thread startButtonIDontCarThread = null;

        private void StartButtonIDontCarThread()
        {
            if (localData.MIPCData.StartProcessReceiveData)
            {
                if (localData.MoveControlData.MoveControlConfig.SensorByPass[EnumSensorSafetyType.ForkHomeCheck] && !localData.LoadUnloadData.ForkHome)
                {
                    if (!localData.LoadUnloadData.Homing)
                        CallForkHome?.Invoke(this, null);
                }
                else
                    JogjoystickOnOff(!localData.MoveControlData.MotionControlData.JoystickMode);
            }
        }

        private void PollingThread()
        {
            try
            {
                #region 宣告/初始化.
                Stopwatch pollingGroupTimer = new Stopwatch();
                pollingGroupTimer.Restart();

                int minIndex;
                double minValue = -1;
                double tempMin;

                bool lastStartButtonValue = false;
                bool lastCharging = false;
                bool nowCharging = false;
                EnumBuzzerType buzzerType;
                EnumDirectionLight directionLight;
                List<Stopwatch> pollingIntervalTimerList = new List<Stopwatch>();

                for (int i = 0; i < pollingDataList.Count; i++)
                {
                    pollingIntervalTimerList.Add(new Stopwatch());
                    pollingIntervalTimerList[i].Restart();
                }

                Stopwatch safetySensorTimer = new Stopwatch();

                heartbeatTimer.Restart();
                safetySensorTimer.Restart();

                List<EnumMecanumIPCdefaultTag> light_TagList = new List<EnumMecanumIPCdefaultTag>();

                List<string> writeBuzzerAndLightTags = new List<string>();
                List<float> writeBuzzerAndLightValues = new List<float>();

                switch (localData.MainFlowConfig.AGVType)
                {
                    case EnumAGVType.AGC:
                        light_TagList = new List<EnumMecanumIPCdefaultTag>() {
                                            EnumMecanumIPCdefaultTag.Light_Green,
                                            EnumMecanumIPCdefaultTag.Light_Yellow,
                                            EnumMecanumIPCdefaultTag.Light_Red
                                        };
                        break;
                    case EnumAGVType.UMTC:
                    case EnumAGVType.PTI:
                    case EnumAGVType.ATS:
                        light_TagList = new List<EnumMecanumIPCdefaultTag>() {
                                            EnumMecanumIPCdefaultTag.RGBLight_Red,
                                            EnumMecanumIPCdefaultTag.RGBLight_Green,
                                            EnumMecanumIPCdefaultTag.RGBLight_Blue
                                        };
                        break;
                    default:
                        break;
                }

                float value1;
                float value2;
                float value3;

                float light1 = -1;
                float light2 = -1;
                float light3 = -1;

                List<SendAndReceive> pollingList = new List<SendAndReceive>();

                for (int i = 0; i < pollingDataList.Count; i++)
                {
                    pollingList.Add(new SendAndReceive());
                    pollingList[pollingList.Count - 1].Result = EnumSendAndRecieve.OK;
                }

                ChargingStatusChange?.Invoke(this, lastCharging);
                #endregion

                while ((Status != EnumControlStatus.Closing || !CheckAllSockeThreadEnd()) && Status != EnumControlStatus.Error)
                {
                    #region MIPC Group撈取資料SendQueue.
                    if (pollingGroupTimer.ElapsedMilliseconds >= 5)
                    {
                        pollingGroupTimer.Restart();
                        minIndex = -1;

                        for (int i = 0; i < pollingDataList.Count; i++)
                        {
                            if (pollingIntervalTimerList[i].ElapsedMilliseconds >= pollingIntervalList[i] &&
                                pollingList[i].Result != EnumSendAndRecieve.None)
                            {
                                tempMin = (pollingIntervalTimerList[i].ElapsedMilliseconds - (double)pollingIntervalList[i]) / (double)pollingIntervalList[i];

                                if (minIndex == -1 || tempMin > minValue)
                                {
                                    minIndex = i;
                                    minValue = tempMin;
                                }
                            }
                        }

                        if (minIndex != -1)
                        {
                            if (pollingIntervalTimerList[minIndex].ElapsedMilliseconds >= pollingIntervalList[minIndex] * 2)
                                WriteLog(3, "", String.Concat("polling lag, groupe = ", pollingDataList[minIndex].GroupNumber.ToString("0")));

                            pollingIntervalTimerList[minIndex].Restart();

                            pollingList[minIndex] = new SendAndReceive();
                            pollingList[minIndex].Send = Read_連續(pollingDataList[minIndex].StartAddress, pollingDataList[minIndex].Length);
                            pollingList[minIndex].PollingGroup = pollingDataList[minIndex].GroupNumber;

                            if (pollingList[minIndex].Send == null)
                                pollingList[minIndex].Result = EnumSendAndRecieve.OK;
                            else
                                AddReaeQueue(EnumMIPCSocketName.Normal.ToString(), pollingList[minIndex]);
                        }
                    }
                    #endregion

                    #region HeartBeat.
                    if (heartbeatTimer.ElapsedMilliseconds >= config.HeartbeatInterval)
                        WriteHeartBeat();
                    #endregion

                    #region 更新AreaSensor區域/方向燈/聲音/三色燈or七彩霓虹燈.
                    if (safetySensorTimer.ElapsedMilliseconds >= config.SafetySensorUpdateInterval)
                    {
                        safetySensorTimer.Restart();

                        if (localData.MIPCData.BypassIO)
                        {
                            SetAlarmCode(EnumMIPCControlErrorCode.ByPass聲音燈號IO自動傳送);
                            light1 = -1;
                            light2 = -1;
                            light3 = -1;
                        }
                        else
                        {
                            ResetAlarmCode(EnumMIPCControlErrorCode.ByPass聲音燈號IO自動傳送);
                            SafetySensorControl.UpdateAllSafetySensor();

                            buzzerType = localData.MIPCData.BuzzerType;

                            if (lastBuzzerType != buzzerType)
                            {
                                if (buzzerMIPCTageChangeList.ContainsKey(buzzerType))
                                {
                                    WriteLog(7, "", String.Concat("切換 BuzzerType : ", buzzerType.ToString()));
                                    writeBuzzerAndLightTags = writeBuzzerAndLightTags.Concat(buzzerMIPCTageList).ToList<string>();
                                    writeBuzzerAndLightValues = writeBuzzerAndLightValues.Concat(buzzerMIPCTageChangeList[buzzerType]).ToList<float>();
                                }
                                else
                                    WriteLog(3, "", String.Concat("並無 BuzzerType : ", buzzerType.ToString(), " 的設定"));

                                lastBuzzerType = buzzerType;
                            }

                            directionLight = localData.MIPCData.DirectionLight;

                            if (lastDirectionLightType != directionLight)
                            {
                                if (DirectionLightMIPCTageChangeList.ContainsKey(directionLight))
                                {
                                    WriteLog(7, "", String.Concat("切換 DirectionLightType : ", directionLight.ToString()));
                                    writeBuzzerAndLightTags = writeBuzzerAndLightTags.Concat(DirectionLightMIPCTageList).ToList<string>();
                                    writeBuzzerAndLightValues = writeBuzzerAndLightValues.Concat(DirectionLightMIPCTageChangeList[directionLight]).ToList<float>();
                                }
                                else
                                    WriteLog(3, "", String.Concat("並無 DirectionLightType : ", directionLight.ToString(), " 的設定"));

                                lastDirectionLightType = directionLight;
                            }

                            switch (localData.MainFlowConfig.AGVType)
                            {
                                case EnumAGVType.AGC:
                                    #region AGC.
                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Light_Green.ToString()) &&
                                        allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Light_Yellow.ToString()) &&
                                        allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Light_Red.ToString()))
                                    {
                                        value1 = 1;
                                        value2 = localData.MIPCData.HasWarn ? 1 : 0;
                                        value3 = localData.MIPCData.HasAlarm ? 1 : 0;

                                        if (light1 == value1 && light2 == value2 && light3 == value3)
                                        {
                                        }
                                        else
                                        {
                                            light1 = value1;
                                            light2 = value2;
                                            light3 = value3;
                                            writeBuzzerAndLightTags.Add(allDataByIPCTagName[EnumMecanumIPCdefaultTag.Light_Green.ToString()].DataName);
                                            writeBuzzerAndLightValues.Add(value1);
                                            writeBuzzerAndLightTags.Add(allDataByIPCTagName[EnumMecanumIPCdefaultTag.Light_Yellow.ToString()].DataName);
                                            writeBuzzerAndLightValues.Add(value2);
                                            writeBuzzerAndLightTags.Add(allDataByIPCTagName[EnumMecanumIPCdefaultTag.Light_Red.ToString()].DataName);
                                            writeBuzzerAndLightValues.Add(value3);
                                        }
                                    }
                                    #endregion
                                    break;
                                case EnumAGVType.UMTC:
                                case EnumAGVType.ATS:
                                case EnumAGVType.PTI:
                                    #region UMTC.
                                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.RGBLight_Red.ToString()) &&
                                        allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.RGBLight_Green.ToString()) &&
                                        allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.RGBLight_Blue.ToString()))
                                    {
                                        //if (ifoffline && localData.AutoManual == EnumAutoState.Auto)
                                        //{
                                        //    value1 = 0;
                                        //    value2 = 0;
                                        //    value3 = 2;
                                        //}
                                        //else 
                                        if (localData.LoadUnloadData.Homing)
                                        {
                                            value1 = 2;
                                            value2 = 2;
                                            value3 = 2;
                                        }
                                        else if (localData.MIPCData.HasAlarm)
                                        {
                                            value1 = 1;
                                            value2 = 0;
                                            value3 = 0;
                                        }
                                        else if (localData.MIPCData.HasWarn)
                                        {
                                            value1 = 2;
                                            value2 = 0;
                                            value3 = 0;
                                        }
                                        else if (localData.MIPCData.Charging)
                                        {
                                            value1 = 2;
                                            value2 = 2;
                                            value3 = 0;
                                        }
                                        else if (localData.MoveControlData.MoveCommand != null)
                                        {
                                            MoveCommandData moveCommand = localData.MoveControlData.MoveCommand;

                                            if (moveCommand != null && moveCommand.SensorStatus >= EnumVehicleSafetyAction.SlowStop &&
                                                localData.MIPCData.SafetySensorStatus >= EnumSafetyLevel.SlowStop)
                                            {
                                                value1 = 2;
                                                value2 = 0;
                                                value3 = 2;
                                            }
                                            else
                                            {
                                                value1 = 0;
                                                value2 = 1;
                                                value3 = 0;
                                            }
                                        }
                                        else if (localData.LoadUnloadData.LoadUnloadCommand != null)
                                        {
                                            value1 = 0;
                                            value2 = 1;
                                            value3 = 0;
                                        }
                                        else
                                        {
                                            value1 = 1;
                                            value2 = 1;
                                            value3 = 0;
                                        }

                                        if (light1 == value1 && light2 == value2 && light3 == value3)
                                        {
                                        }
                                        else
                                        {
                                            writeBuzzerAndLightTags.Add(allDataByIPCTagName[EnumMecanumIPCdefaultTag.RGBLight_Red.ToString()].DataName);
                                            writeBuzzerAndLightValues.Add(value1);
                                            writeBuzzerAndLightTags.Add(allDataByIPCTagName[EnumMecanumIPCdefaultTag.RGBLight_Green.ToString()].DataName);
                                            writeBuzzerAndLightValues.Add(value2);
                                            writeBuzzerAndLightTags.Add(allDataByIPCTagName[EnumMecanumIPCdefaultTag.RGBLight_Blue.ToString()].DataName);
                                            writeBuzzerAndLightValues.Add(value3);
                                            light1 = value1;
                                            light2 = value2;
                                            light3 = value3;
                                        }
                                    }
                                    #endregion
                                    break;
                                default:
                                    break;
                            }

                            if (writeBuzzerAndLightTags.Count != 0)
                            {
                                SendMIPCDataByMIPCTagName(writeBuzzerAndLightTags, writeBuzzerAndLightValues, false);
                                writeBuzzerAndLightTags = new List<string>();
                                writeBuzzerAndLightValues = new List<float>();
                            }
                        }
                    }
                    #endregion
                    #region 充/斷電變化通知Middler.
                    nowCharging = localData.MIPCData.Charging;

                    if (lastCharging != nowCharging)
                    {
                        lastCharging = nowCharging;
                        ChargingStatusChange?.Invoke(this, lastCharging);
                    }
                    #endregion
                    #region StartButton.
                    if (lastStartButtonValue != localData.MIPCData.Start)
                    {
                        lastStartButtonValue = localData.MIPCData.Start;

                        if (localData.AutoManual == EnumAutoState.Manual &&
                            localData.MoveControlData.MoveCommand == null &&
                            localData.LoadUnloadData.LoadUnloadCommand == null &&
                            lastStartButtonValue)
                        {
                            if (startButtonIDontCarThread == null || !startButtonIDontCarThread.IsAlive)
                            {
                                startButtonIDontCarThread = new Thread(StartButtonIDontCarThread);
                                startButtonIDontCarThread.Start();
                            }
                        }
                    }
                    #endregion
                    #region Warning.
                    if (localData.BatteryInfo.LowV_Warn)
                        SetAlarmCode(EnumMIPCControlErrorCode.LowBattery_V);
                    else
                        ResetAlarmCode(EnumMIPCControlErrorCode.LowBattery_V);

                    if (localData.BatteryInfo.LowSOC_Warn)
                        SetAlarmCode(EnumMIPCControlErrorCode.LowBattery_SOC);
                    else
                        ResetAlarmCode(EnumMIPCControlErrorCode.LowBattery_SOC);

                    if ((localData.BatteryInfo.Battery_溫度1 > localData.BatteryConfig.Battery_WarningTemperature ||
                         localData.BatteryInfo.Battery_溫度2 > localData.BatteryConfig.Battery_WarningTemperature))
                        SetAlarmCode(EnumMIPCControlErrorCode.BatteryWarningTemp);
                    else
                        ResetAlarmCode(EnumMIPCControlErrorCode.BatteryWarningTemp);
                    #endregion
                    #region Alarm.
                    if (localData.BatteryInfo.ShutDownV_Alarm)
                        SetAlarmCode(EnumMIPCControlErrorCode.ShutDown_LowBattery_V);
                    else
                        ResetAlarmCode(EnumMIPCControlErrorCode.ShutDown_LowBattery_V);

                    if (localData.BatteryInfo.ShutDownSOC_Alarm)
                        SetAlarmCode(EnumMIPCControlErrorCode.ShutDown_LowBattery_SOC);
                    else
                        ResetAlarmCode(EnumMIPCControlErrorCode.ShutDown_LowBattery_SOC);

                    if (localData.BatteryInfo.Battery_DisConnect)
                        SetAlarmCode(EnumMIPCControlErrorCode.電池通訊異常);
                    else
                        ResetAlarmCode(EnumMIPCControlErrorCode.電池通訊異常);

                    if (localData.BatteryInfo.Battery_溫度1 > localData.BatteryConfig.Battery_ShowDownTemperature ||
                        localData.BatteryInfo.Battery_溫度2 > localData.BatteryConfig.Battery_ShowDownTemperature)
                        SetAlarmCode(EnumMIPCControlErrorCode.ShutDown_BatteryTemp);
                    else
                        ResetAlarmCode(EnumMIPCControlErrorCode.ShutDown_BatteryTemp);
                    #endregion
                    #region ShutDown.
                    localData.BatteryInfo.ShutDownAction = localData.BatteryInfo.ShutDownV_Alarm ||
                                                           localData.BatteryInfo.ShutDownSOC_Alarm ||
                                                           localData.BatteryInfo.Battery_DisConnect ||
                                                           localData.BatteryInfo.Battery_溫度1 > localData.BatteryConfig.Battery_ShowDownTemperature ||
                                                           localData.BatteryInfo.Battery_溫度2 > localData.BatteryConfig.Battery_ShowDownTemperature;

                    if (localData.BatteryInfo.ShutDownAction)
                    {
                        if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.ShutDown.ToString()))
                        {
                            if (localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.ShutDown.ToString()) == 0)
                                SendMIPCDataByIPCTagName(new List<EnumMecanumIPCdefaultTag>() { EnumMecanumIPCdefaultTag.ShutDown }, new List<float> { 1 });
                        }
                        else if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.ShutDown_Nag.ToString()))
                        {
                            if (localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.ShutDown_Nag.ToString()) == 1)
                                SendMIPCDataByIPCTagName(new List<EnumMecanumIPCdefaultTag>() { EnumMecanumIPCdefaultTag.ShutDown_Nag }, new List<float> { 0 });
                        }
                    }
                    else
                    {
                        if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.ShutDown.ToString()))
                        {
                            if (localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.ShutDown.ToString()) == 1)
                                SendMIPCDataByIPCTagName(new List<EnumMecanumIPCdefaultTag>() { EnumMecanumIPCdefaultTag.ShutDown }, new List<float> { 0 });
                        }
                        else if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.ShutDown_Nag.ToString()))
                        {
                            if (localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.ShutDown_Nag.ToString()) == 0)
                                SendMIPCDataByIPCTagName(new List<EnumMecanumIPCdefaultTag>() { EnumMecanumIPCdefaultTag.ShutDown_Nag }, new List<float> { 0 });
                        }
                    }
                    #endregion
                    //WriteLog(7, "", String.Concat("ShareMemory開始  "));
                    ShareMemoryByteArrayUpdate();   //Allen, test sharememory               
                    SMWriter.Fun_ShareMemoryWriter("SM", shareMemoryByteArray);
                    
                    //WriteLog(7, "", String.Concat("ShareMemoryByteArray  "+ shareMemoryByteArray[0].ToString() + shareMemoryByteArray[1].ToString() + shareMemoryByteArray[2].ToString() + shareMemoryByteArray[3].ToString()));
                    //SpinWait.SpinUntil(() => false, 1);
                    Thread.Sleep(1);
                }

                if (Status == EnumControlStatus.Closing)
                    Status = EnumControlStatus.Closed;
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        private void SendMainFlowResetAlarmThread()
        {
            try
            {
                main.ResetAlarm();
            }
            catch { }
        }

        private Thread sendMainFlowResetAlarmThread = null;

        private void SendMainFlowResetAlarm()
        {
            if (sendMainFlowResetAlarmThread == null || !sendMainFlowResetAlarmThread.IsAlive)
            {
                sendMainFlowResetAlarmThread = new Thread(SendMainFlowResetAlarmThread);
                sendMainFlowResetAlarmThread.Start();
            }
        }

        #region MotionCommand.
        public bool WriteJogjoystickData(double lineVelocity, double lineAcc, double lineDec, double thetaVelocity, double thetaAcc, double thetaDec)
        {
            try
            {
                if (localData.SimulateMode)
                {
                    return true;
                }

                if (jogjoystickDataArray.Length == 0)
                {
                    WriteMotionLog(3, "", String.Concat("Motion Command :: move 定義的tag在MIPC Config內有缺,因此無法執行移動命令"));
                    return false;
                }

                List<Byte[]> byteValueArray = new List<byte[]>();

                if (lineVelocity != -1)
                    byteValueArray.Add(BitConverter.GetBytes((float)(lineVelocity)));
                else
                    byteValueArray.Add(BitConverter.GetBytes((float)(localData.MoveControlData.MotionControlData.JoystickLineAxisData.Velocity)));

                if (lineAcc != -1)
                    byteValueArray.Add(BitConverter.GetBytes((float)(lineAcc)));
                else
                    byteValueArray.Add(BitConverter.GetBytes((float)(localData.MoveControlData.MotionControlData.JoystickLineAxisData.Acceleration)));

                if (lineDec != -1)
                    byteValueArray.Add(BitConverter.GetBytes((float)(lineDec)));
                else
                    byteValueArray.Add(BitConverter.GetBytes((float)(localData.MoveControlData.MotionControlData.JoystickLineAxisData.Deceleration)));


                if (thetaVelocity != -1)
                    byteValueArray.Add(BitConverter.GetBytes((float)(thetaVelocity)));
                else
                    byteValueArray.Add(BitConverter.GetBytes((float)(localData.MoveControlData.MotionControlData.JoystickThetaAxisData.Velocity)));

                if (thetaAcc != -1)
                    byteValueArray.Add(BitConverter.GetBytes((float)(thetaAcc)));
                else
                    byteValueArray.Add(BitConverter.GetBytes((float)(localData.MoveControlData.MotionControlData.JoystickThetaAxisData.Acceleration)));

                if (thetaDec != -1)
                    byteValueArray.Add(BitConverter.GetBytes((float)(thetaDec)));
                else
                    byteValueArray.Add(BitConverter.GetBytes((float)(localData.MoveControlData.MotionControlData.JoystickThetaAxisData.Deceleration)));

                SendAndReceive sendAndReceive = new SendAndReceive();

                sendAndReceive.Send = Write_非連續(jogjoystickDataArray, (UInt16)jogjoystickDataArray.Length, byteValueArray);
                AddWriteQueue(EnumMIPCSocketName.Normal.ToString(), sendAndReceive);

                Stopwatch timer = new Stopwatch();
                timer.Restart();

                while (sendAndReceive.Result == EnumSendAndRecieve.None)
                {
                    if (timer.ElapsedMilliseconds > config.CommandTimeoutValue)
                    {
                        WriteMotionLog(3, "", String.Concat("timeout"));
                        return false;
                    }

                    Thread.Sleep(1);
                }

                if (sendAndReceive.Result == EnumSendAndRecieve.OK)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                WriteMotionLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }

        public bool AGV_Move(MapAGVPosition end, double lineVelocity, double lineAcc, double lineDec, double lineJerk, double thetaVelocity, double thetaAcc, double thetaDec, double thetaJerk)
        {
            try
            {
                WriteMotionLog(7, "", String.Concat("Move :: end ", computeFunction.GetMapAGVPositionStringWithAngle(end, "0"), ",lineVel : ", lineVelocity.ToString("0"),
                                              ",lineAcc : ", lineAcc.ToString("0"), ",lineDec : ", lineDec.ToString("0"), ",lineJerk : ", lineJerk.ToString("0"),
                                              ", thetaVel : ", thetaVelocity.ToString("0"), ", thetaAcc : ", thetaAcc.ToString("0"), ", thetaDec : ", thetaDec.ToString("0"),
                                              ", thetaJerk : ", thetaJerk.ToString("0")));

                if (localData.SimulateMode)
                {
                    localData.MoveControlData.MotionControlData.PreMoveStatus = EnumAxisMoveStatus.PreMove;
                    return true;
                }

                float x = (float)(end.Position.Y);
                float y = (float)(end.Position.X);
                float angle = (float)(end.Angle);

                if (moveCommandAddressArray.Length == 0)
                {
                    WriteMotionLog(3, "", String.Concat("Motion Command :: move 定義的tag在MIPC Config內有缺,因此無法執行移動命令"));
                    return false;
                }

                List<Byte[]> byteValueArray = new List<byte[]>();

                byteValueArray.Add(BitConverter.GetBytes(x));
                byteValueArray.Add(BitConverter.GetBytes(y));
                byteValueArray.Add(BitConverter.GetBytes(angle));

                byteValueArray.Add(BitConverter.GetBytes((float)(lineVelocity)));
                byteValueArray.Add(BitConverter.GetBytes((float)(lineAcc)));
                byteValueArray.Add(BitConverter.GetBytes((float)(lineDec)));
                byteValueArray.Add(BitConverter.GetBytes((float)(lineJerk)));

                byteValueArray.Add(BitConverter.GetBytes((float)(thetaVelocity)));
                byteValueArray.Add(BitConverter.GetBytes((float)(thetaAcc)));
                byteValueArray.Add(BitConverter.GetBytes((float)(thetaDec)));
                byteValueArray.Add(BitConverter.GetBytes((float)(thetaJerk)));
                byteValueArray.Add(BitConverter.GetBytes((float)(1)));

                SendAndReceive sendAndReceive = new SendAndReceive();

                sendAndReceive.Send = Write_非連續(moveCommandAddressArray, (UInt16)moveCommandAddressArray.Length, byteValueArray);
                AddWriteQueue(EnumMIPCSocketName.Normal.ToString(), sendAndReceive);

                Stopwatch timer = new Stopwatch();
                timer.Restart();

                while (sendAndReceive.Result == EnumSendAndRecieve.None)
                {
                    if (timer.ElapsedMilliseconds > config.CommandTimeoutValue)
                    {
                        WriteMotionLog(3, "", String.Concat("timeout"));
                        return false;
                    }

                    Thread.Sleep(1);
                }

                if (sendAndReceive.Result == EnumSendAndRecieve.OK)
                {
                    localData.MoveControlData.MotionControlData.PreMoveStatus = EnumAxisMoveStatus.PreMove;
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                WriteMotionLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }

        public bool AGV_ChangeVelocity(double newVelociy)
        {
            try
            {
                WriteMotionLog(7, "", String.Concat("VChange :: velocity : ", newVelociy.ToString("0")));

                if (localData.SimulateMode)
                    return true;

                if (changeVelociyAddressArray.Length == 0)
                {
                    WriteMotionLog(3, "", String.Concat("vChange Command :: vChange 定義的tag在MIPC Config內有缺,因此無法執行移動命令"));
                    return false;
                }

                List<Byte[]> byteValueArray = new List<byte[]>();

                byteValueArray.Add(BitConverter.GetBytes((float)(newVelociy)));
                byteValueArray.Add(BitConverter.GetBytes((float)(1)));

                SendAndReceive sendAndReceive = new SendAndReceive();

                sendAndReceive.Send = Write_非連續(changeVelociyAddressArray, (UInt16)changeVelociyAddressArray.Length, byteValueArray);
                AddWriteQueue(EnumMIPCSocketName.Normal.ToString(), sendAndReceive);

                Stopwatch timer = new Stopwatch();
                timer.Restart();

                while (sendAndReceive.Result == EnumSendAndRecieve.None)
                {
                    if (timer.ElapsedMilliseconds > config.CommandTimeoutValue)
                    {
                        WriteMotionLog(3, "", String.Concat("timeout"));
                        return false;
                    }

                    Thread.Sleep(1);
                }

                return sendAndReceive.Result == EnumSendAndRecieve.OK;
            }
            catch (Exception ex)
            {
                WriteMotionLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }

        public bool AGV_CheckTimeEnd()
        {
            try
            {
                return false;
            }
            catch (Exception ex)
            {
                WriteMotionLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }

        public bool AGV_Stop(double lineDec, double lineJerk, double thetaDec, double thetaJerk)
        {
            try
            {
                WriteMotionLog(7, "", String.Concat("Stop :: lineDec : ", lineDec.ToString("0"), ",lineJerk : ", lineJerk.ToString("0"),
                                               ", thetaDec : ", thetaDec.ToString("0"), ", thetaJerk : ", thetaJerk.ToString("0")));

                if (localData.SimulateMode)
                {
                    localData.MoveControlData.MotionControlData.PreMoveStatus = EnumAxisMoveStatus.PreStop;
                    return true;
                }

                if (stopCommandAddressArray.Length == 0)
                {
                    WriteMotionLog(3, "", String.Concat("Motion Command :: stop 定義的tag在MIPC Config內有缺,因此無法執行移動命令"));
                    return false;
                }

                List<Byte[]> byteValueArray = new List<byte[]>();


                byteValueArray.Add(BitConverter.GetBytes((float)(lineDec)));
                byteValueArray.Add(BitConverter.GetBytes((float)(lineJerk)));

                byteValueArray.Add(BitConverter.GetBytes((float)(thetaDec)));
                byteValueArray.Add(BitConverter.GetBytes((float)(thetaJerk)));
                byteValueArray.Add(BitConverter.GetBytes((float)(1)));

                SendAndReceive sendAndReceive = new SendAndReceive();

                sendAndReceive.Send = Write_非連續(stopCommandAddressArray, (UInt16)stopCommandAddressArray.Length, byteValueArray);
                AddWriteQueue(EnumMIPCSocketName.Normal.ToString(), sendAndReceive);

                Stopwatch timer = new Stopwatch();
                timer.Restart();

                while (sendAndReceive.Result == EnumSendAndRecieve.None)
                {
                    if (timer.ElapsedMilliseconds > config.CommandTimeoutValue)
                    {
                        WriteMotionLog(3, "", String.Concat("timeout"));
                        return false;
                    }

                    Thread.Sleep(1);
                }

                bool result = sendAndReceive.Result == EnumSendAndRecieve.OK;

                if (result)
                {
                    localData.MoveControlData.MotionControlData.PreMoveStatus = EnumAxisMoveStatus.PreStop;
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                WriteMotionLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }

        public bool AGV_EMS()
        {
            try
            {
                if (localData.SimulateMode)
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                WriteMotionLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }

        public void AGV_Reset()
        {
            try
            {
            }
            catch (Exception ex)
            {
                WriteMotionLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        private bool ServoOnOff(bool onOff)
        {
            try
            {
                WriteMotionLog(7, "", String.Concat("ServoOnOff : ", (onOff ? "on" : "off")));

                if (localData.SimulateMode)
                    return true;

                if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.ServoOnOff.ToString()))
                {
                    SendAndReceive sendAndReceive = new SendAndReceive();

                    List<Byte[]> data = new List<byte[]>();

                    switch (allDataByIPCTagName[EnumMecanumIPCdefaultTag.ServoOnOff.ToString()].DataType)
                    {
                        case EnumDataType.Boolean:
                            if (onOff)
                                data.Add(new Byte[4] { (int)EnumMIPCServoOnOffValue.ServoOn, 0, 0, 0 });
                            else
                                data.Add(new Byte[4] { (int)EnumMIPCServoOnOffValue.ServoOff, 0, 0, 0 });

                            break;

                        case EnumDataType.Float:
                            data.Add(BitConverter.GetBytes((float)(onOff ? (int)EnumMIPCServoOnOffValue.ServoOn : (int)EnumMIPCServoOnOffValue.ServoOff)));
                            break;

                        default:
                            WriteMotionLog(3, "", String.Concat(EnumMecanumIPCdefaultTag.ServoOnOff.ToString(), " type not boolean or float"));
                            return false;
                    }

                    switch (localData.MainFlowConfig.AGVType)
                    {
                        case EnumAGVType.PTI:
                            sendAndReceive.Send = Write_連續(allDataByIPCTagName[EnumMecanumIPCdefaultTag.BrakeRelease.ToString()].Address, 1, data);
                            AddWriteQueue(EnumMIPCSocketName.Normal.ToString(), sendAndReceive);

                            Stopwatch timer1 = new Stopwatch();
                            timer1.Restart();

                            while (sendAndReceive.Result == EnumSendAndRecieve.None)
                            {
                                if (timer1.ElapsedMilliseconds > config.CommandTimeoutValue)
                                {
                                    WriteMotionLog(3, "", String.Concat("timeout"));
                                    return false;
                                }

                                Thread.Sleep(1);
                            }
                            break;
                        default:
                            break;
                    }

                    sendAndReceive.Send = Write_連續(allDataByIPCTagName[EnumMecanumIPCdefaultTag.ServoOnOff.ToString()].Address, 1, data);
                    AddWriteQueue(EnumMIPCSocketName.Normal.ToString(), sendAndReceive);

                    Stopwatch timer = new Stopwatch();
                    timer.Restart();

                    while (sendAndReceive.Result == EnumSendAndRecieve.None)
                    {
                        if (timer.ElapsedMilliseconds > config.CommandTimeoutValue)
                        {
                            WriteMotionLog(3, "", String.Concat("timeout"));
                            return false;
                        }

                        Thread.Sleep(1);
                    }

                    return sendAndReceive.Result == EnumSendAndRecieve.OK;
                }
                else
                {
                    WriteMotionLog(3, "", String.Concat(EnumMecanumIPCdefaultTag.ServoOnOff.ToString(), " not define in mipcConfig"));
                    return false;
                }
            }
            catch (Exception ex)
            {
                WriteMotionLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }

        public bool JogjoystickOnOff(bool onOff)
        {
            try
            {
                WriteMotionLog(7, "", String.Concat("JogjoystickOnOff : ", (onOff ? "on" : "off")));

                if (onOff)
                {
                    if (localData.AutoManual != EnumAutoState.Manual)
                    {
                        WriteMotionLog(7, "", "由於非Manual中, 因此無法使用搖桿模式");
                        return false;
                    }
                    else if (localData.MoveControlData.MoveCommand != null ||
                             localData.LoadUnloadData.LoadUnloadCommand != null ||
                             localData.MoveControlData.SpecialFlow)
                    {
                        WriteMotionLog(7, "", "由於走行/取放命令中, 因此無法使用搖桿模式");
                        return false;
                    }
                    else if (localData.MoveControlData.MoveControlConfig.SensorByPass[EnumSensorSafetyType.ChargingCheck] && localData.MIPCData.Charging)
                    {
                        WriteMotionLog(7, "", "由於充電中, 因此無法使用搖桿模式");
                        return false;
                    }
                    else if (localData.MoveControlData.MoveControlConfig.SensorByPass[EnumSensorSafetyType.ForkHomeCheck] && !localData.LoadUnloadData.Ready)
                    {
                        WriteMotionLog(7, "", "由於ForkNotReady, 因此無法使用搖桿模式");
                        return false;
                    }
                }

                if (localData.SimulateMode)
                    return true;

                if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.JoystickOnOff.ToString()))
                {
                    SendAndReceive sendAndReceive = new SendAndReceive();

                    List<Byte[]> data = new List<byte[]>();

                    switch (allDataByIPCTagName[EnumMecanumIPCdefaultTag.JoystickOnOff.ToString()].DataType)
                    {
                        case EnumDataType.Boolean:
                            if (onOff)
                                data.Add(new Byte[4] { 1, 0, 0, 0 });
                            else
                                data.Add(new Byte[4] { 0, 0, 0, 0 });

                            break;

                        case EnumDataType.Float:
                            data.Add(BitConverter.GetBytes((float)(onOff ? 1 : 0)));
                            break;

                        default:
                            WriteMotionLog(3, "", String.Concat(EnumMecanumIPCdefaultTag.JoystickOnOff.ToString(), " type not boolean or float"));
                            return false;
                    }

                    uint[] sendArrary = new uint[2];
                    sendArrary[0] = allDataByIPCTagName[EnumMecanumIPCdefaultTag.JoystickOnOff.ToString()].Address;
                    sendArrary[1] = allDataByIPCTagName[EnumMecanumIPCdefaultTag.ServoOnOff.ToString()].Address;

                    data.Add(BitConverter.GetBytes((float)(onOff ? EnumMIPCServoOnOffValue.ServoOn : EnumMIPCServoOnOffValue.ServoOff)));

                    sendAndReceive.Send = Write_非連續(sendArrary, 2, data);
                    AddWriteQueue(EnumMIPCSocketName.Normal.ToString(), sendAndReceive);

                    Stopwatch timer = new Stopwatch();
                    timer.Restart();

                    while (sendAndReceive.Result == EnumSendAndRecieve.None)
                    {
                        if (timer.ElapsedMilliseconds > config.CommandTimeoutValue)
                        {
                            WriteMotionLog(3, "", String.Concat("timeout"));
                            return false;
                        }

                        Thread.Sleep(1);
                    }

                    return sendAndReceive.Result == EnumSendAndRecieve.OK;
                }
                else
                {
                    WriteMotionLog(3, "", String.Concat(EnumMecanumIPCdefaultTag.JoystickOnOff.ToString(), " not define in mipcConfig"));
                    return false;
                }
            }
            catch (Exception ex)
            {
                WriteMotionLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }

        public void ResetMIPCAlarm()
        {
            try
            {
                if (localData.SimulateMode)
                    return;

                if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.ResetAlarm.ToString()))
                {
                    SendAndReceive sendAndReceive = new SendAndReceive();

                    List<Byte[]> data = new List<byte[]>();

                    switch (allDataByIPCTagName[EnumMecanumIPCdefaultTag.ResetAlarm.ToString()].DataType)
                    {
                        case EnumDataType.Float:
                            data.Add(BitConverter.GetBytes((float)(1)));
                            break;
                        default:
                            return;
                    }

                    sendAndReceive.Send = Write_連續(allDataByIPCTagName[EnumMecanumIPCdefaultTag.ResetAlarm.ToString()].Address, 1, data);
                    AddWriteQueue(EnumMIPCSocketName.Normal.ToString(), sendAndReceive);
                }
                else
                {
                    WriteMotionLog(3, "", String.Concat(EnumMecanumIPCdefaultTag.ResetAlarm.ToString(), " not define in mipcConfig"));
                }
            }
            catch (Exception ex)
            {
                WriteMotionLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        public bool AGV_ServoOn()
        {
            return ServoOnOff(true);
        }

        public bool AGV_ServoOff()
        {
            return ServoOnOff(false);
        }

        private float GetSendTimeStamp(DateTime getDataTime, double scnaTime)
        {
            TimeStampData nowTimeStamp = timeStampData;

            if (nowTimeStamp == null)
            {
                WriteMotionLog(3, "", "TimeStampError, timeStampData == null");
                return 0;
            }

            double timeStampValue = (getDataTime - nowTimeStamp.SendTime).TotalMilliseconds - scnaTime;

            while (timeStampValue > overflowValue)
            {
                nowTimeStamp.SendTime = nowTimeStamp.SendTime.AddMilliseconds(overflowValue);
                timeStampValue -= overflowValue;
            }

            if (timeStampValue < 0)
            {
                WriteMotionLog(3, "", "TimeStampError < 0!");
                return 0;
            }

            return (float)timeStampValue;
        }

        public bool SetPosition(LocateAGVPosition locateAGVPosition, MapAGVPosition originSlamPosition, bool waitResult)
        {
            try
            {
                if (timeStampData == null)
                {
                    WriteMotionLog(7, "", String.Concat("由於timeStamp未設定,因此不能SetPosition"));
                    return false;
                }

                SendAndReceive sendAndReceive = new SendAndReceive();
                List<Byte[]> byteValueArray = new List<byte[]>();

                if (locateAGVPosition == null && originSlamPosition == null)
                {
                    if (setPositionAddressArray_OnlyOriginOK.Length == 0)
                    {
                        WriteMotionLog(3, "", String.Concat("SetPosition Command :: setPosition[OriginOK] 定義的tag在MIPC Config內有缺,因此無法執行移動命令"));
                        return false;
                    }
                    
                    if (localData.MoveControlData.LocateControlData.SlamLocateOK)
                        byteValueArray.Add(BitConverter.GetBytes(((float)1)));
                    else
                        byteValueArray.Add(BitConverter.GetBytes(0));

                    ipcHeartbeatNumber++;
                    byteValueArray.Add(BitConverter.GetBytes((float)(ipcHeartbeatNumber)));

                    heartbeatTimer.Restart();
                    sendAndReceive.Send = Write_非連續(setPositionAddressArray_OnlyOriginOK, (UInt16)setPositionAddressArray_OnlyOriginOK.Length, byteValueArray);
                }
                else if (locateAGVPosition == null)
                {
                    if (setPositionAddressArray_OnlyOriginData.Length == 0)
                    {
                        WriteMotionLog(3, "", String.Concat("SetPosition Command :: setPosition[OriginData] 定義的tag在MIPC Config內有缺,因此無法執行移動命令"));
                        return false;
                    }
                    
                    if (localData.MoveControlData.LocateControlData.SlamLocateOK && originSlamPosition != null)
                    {
                        byteValueArray.Add(BitConverter.GetBytes(((float)originSlamPosition.Position.X)));
                        byteValueArray.Add(BitConverter.GetBytes(((float)originSlamPosition.Position.Y)));
                        byteValueArray.Add(BitConverter.GetBytes(((float)originSlamPosition.Angle)));
                        byteValueArray.Add(BitConverter.GetBytes(((float)1)));
                    }
                    else
                    {
                        byteValueArray.Add(BitConverter.GetBytes(0));
                        byteValueArray.Add(BitConverter.GetBytes(0));
                        byteValueArray.Add(BitConverter.GetBytes(0));
                        byteValueArray.Add(BitConverter.GetBytes(0));
                    }

                    ipcHeartbeatNumber++;
                    byteValueArray.Add(BitConverter.GetBytes((float)(ipcHeartbeatNumber)));

                    heartbeatTimer.Restart();

                    sendAndReceive.Send = Write_非連續(setPositionAddressArray_OnlyOriginData, (UInt16)setPositionAddressArray_OnlyOriginData.Length, byteValueArray);
                }
                else
                {
                    float x = locateAGVPosition == null ? 0 : (float)(locateAGVPosition.AGVPosition.Position.Y);
                    float y = locateAGVPosition == null ? 0 : (float)(locateAGVPosition.AGVPosition.Position.X);
                    float angle = locateAGVPosition == null ? 0 : (float)(locateAGVPosition.AGVPosition.Angle);

                    if (setPositionAddressArray.Length == 0)
                    {
                        WriteMotionLog(3, "", String.Concat("SetPosition Command :: setPosition 定義的tag在MIPC Config內有缺,因此無法執行移動命令"));
                        return false;
                    }
                    
                    byteValueArray.Add(BitConverter.GetBytes(x));
                    byteValueArray.Add(BitConverter.GetBytes(y));
                    byteValueArray.Add(BitConverter.GetBytes(angle));

                    // timeStmap.
                    byteValueArray.Add(BitConverter.GetBytes(
                        locateAGVPosition == null ? 0 :
                        GetSendTimeStamp(locateAGVPosition.GetDataTime, locateAGVPosition.ScanTime)));

                    // start.
                    byteValueArray.Add(BitConverter.GetBytes(((float)1)));

                    if (localData.MoveControlData.LocateControlData.SlamLocateOK && originSlamPosition != null)
                    {
                        byteValueArray.Add(BitConverter.GetBytes(((float)originSlamPosition.Position.X)));
                        byteValueArray.Add(BitConverter.GetBytes(((float)originSlamPosition.Position.Y)));
                        byteValueArray.Add(BitConverter.GetBytes(((float)originSlamPosition.Angle)));
                        byteValueArray.Add(BitConverter.GetBytes(((float)1)));
                    }
                    else
                    {
                        byteValueArray.Add(BitConverter.GetBytes(0));
                        byteValueArray.Add(BitConverter.GetBytes(0));
                        byteValueArray.Add(BitConverter.GetBytes(0));
                        byteValueArray.Add(BitConverter.GetBytes(0));
                    }

                    ipcHeartbeatNumber++;
                    byteValueArray.Add(BitConverter.GetBytes((float)(ipcHeartbeatNumber)));

                    heartbeatTimer.Restart();
                    
                    sendAndReceive.Send = Write_非連續(setPositionAddressArray, (UInt16)setPositionAddressArray.Length, byteValueArray);
                }

                AddWriteQueue(EnumMIPCSocketName.Normal.ToString(), sendAndReceive);

                if (!waitResult)
                    return true;
                else
                {
                    Stopwatch timer = new Stopwatch();
                    timer.Restart();

                    while (sendAndReceive.Result == EnumSendAndRecieve.None)
                    {
                        if (timer.ElapsedMilliseconds > config.CommandTimeoutValue)
                        {
                            WriteMotionLog(3, "", String.Concat("timeout"));
                            return false;
                        }

                        Thread.Sleep(1);
                    }

                    return sendAndReceive.Result == EnumSendAndRecieve.OK;
                }
            }
            catch (Exception ex)
            {
                WriteMotionLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }

        public string MotionCommand(string command)
        {
            try
            {
                if (localData.AutoManual != EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null)
                {
                    SendAndReceive data = new SendAndReceive();
                    data.Send = new ModbusData();
                    data.Send.ByteData = Encoding.ASCII.GetBytes(command);
                    data.Send.ReceiveLength = 256;
                    data.IsMotionCommand = true;

                    AddWriteQueue(EnumMIPCSocketName.MotionCommand.ToString(), data);

                    Stopwatch timer = new Stopwatch();
                    timer.Restart();

                    while (data.Result == EnumSendAndRecieve.None)
                    {
                        if (timer.ElapsedMilliseconds > config.CommandTimeoutValue)
                            return "Timeout";

                        Thread.Sleep(1);
                    }

                    return System.Text.Encoding.ASCII.GetString(data.Receive.ByteData, 0, 256);
                }
                else
                    return "Manual 下才可使用";
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return "Exception";
            }
        }

        public bool SetMIPCReady(bool ready)
        {
            if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.MIPCReady.ToString()))
            {
                if (ready)
                {
                    if (localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.MIPCReady.ToString()) == 0)
                    {
                        WriteLog(7, "", "MIPCReady on");
                        return SendMIPCDataByIPCTagName(new List<EnumMecanumIPCdefaultTag>() { EnumMecanumIPCdefaultTag.MIPCReady }, new List<float>() { 1 });
                    }
                    else
                        return true;
                }
                else
                {
                    if (localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.MIPCReady.ToString()) == 1)
                    {
                        WriteLog(7, "", "MIPCReady off");
                        return SendMIPCDataByIPCTagName(new List<EnumMecanumIPCdefaultTag>() { EnumMecanumIPCdefaultTag.MIPCReady }, new List<float>() { 0 });
                    }
                    else
                        return true;

                }
            }
            else if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.MIPCNotReady.ToString()))
            {
                if (ready)
                {
                    if (localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.MIPCNotReady.ToString()) == 1)
                    {
                        WriteLog(7, "", "MIPCNotReady off");
                        return SendMIPCDataByIPCTagName(new List<EnumMecanumIPCdefaultTag>() { EnumMecanumIPCdefaultTag.MIPCNotReady }, new List<float>() { 0 });
                    }
                }
                else
                {
                    if (localData.MIPCData.GetDataByIPCTagName(EnumMecanumIPCdefaultTag.MIPCNotReady.ToString()) == 0)
                    {
                        WriteLog(7, "", "MIPCNotReady on");
                        return SendMIPCDataByIPCTagName(new List<EnumMecanumIPCdefaultTag>() { EnumMecanumIPCdefaultTag.MIPCNotReady }, new List<float>() { 1 });
                    }
                }

                return true;
            }
            else
            {
                WriteLog(7, "", "無ShutDown tag");
                return false;
            }
        }
        #endregion

        public bool SendMIPCDataByMIPCTagName(List<string> tagNameList, List<float> dataList, bool waitResult = true)
        {
            try
            {
                if (tagNameList.Count != dataList.Count)
                {
                    WriteLog(3, "", String.Concat("tagNameList count : ", tagNameList.Count.ToString("0"),
                                                  ", dataList count : ", dataList.Count.ToString("0"), " 數量不相符"));
                    return false;
                }

                if (localData.SimulateMode)
                {
                    string debugString = "SendMIPCDataByMIPCTagName : ";

                    for (int i = 0; i < tagNameList.Count; i++)
                    {
                        debugString = String.Concat(debugString, "\r\n", tagNameList[i], " = ", dataList[i].ToString("0.0"));
                    }

                    WriteLog(7, "", debugString);
                    return true;
                }
                else if (logMode)
                {
                    string debugString = "SendMIPCDataByMIPCTagName : ";

                    for (int i = 0; i < tagNameList.Count; i++)
                    {
                        debugString = String.Concat(debugString, "\r\n", tagNameList[i], " = ", dataList[i].ToString("0.0"));
                    }

                    WriteLog(7, "", debugString);
                }

                uint[] tagAddressList = new uint[tagNameList.Count];
                List<Byte[]> byteValueArray = new List<byte[]>();

                for (int i = 0; i < tagNameList.Count; i++)
                {
                    tagAddressList[i] = allDataByMIPCTagName[tagNameList[i]].Address;
                    byteValueArray.Add(BitConverter.GetBytes(dataList[i]));
                }

                SendAndReceive sendAndReceive = new SendAndReceive();

                sendAndReceive.Send = Write_非連續(tagAddressList, (UInt16)tagAddressList.Length, byteValueArray);
                AddWriteQueue(EnumMIPCSocketName.Normal.ToString(), sendAndReceive);

                if (!waitResult)
                    return true;

                Stopwatch timer = new Stopwatch();
                timer.Restart();

                while (sendAndReceive.Result == EnumSendAndRecieve.None)
                {
                    if (timer.ElapsedMilliseconds > config.CommandTimeoutValue)
                    {
                        WriteLog(3, "", String.Concat("timeout"));
                        return false;
                    }

                    Thread.Sleep(1);
                }

                return sendAndReceive.Result == EnumSendAndRecieve.OK;
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }

        public bool SendMIPCDataByIPCTagName(List<EnumMecanumIPCdefaultTag> IPCTagNameList, List<float> dataList, bool waitResult = true)
        {
            try
            {
                if (IPCTagNameList.Count != dataList.Count)
                {
                    WriteLog(3, "", String.Concat("tagNameList count : ", IPCTagNameList.Count.ToString("0"),
                                                  ", dataList count : ", dataList.Count.ToString("0"), " 數量不相符"));
                    return false;
                }
                else if (logMode)
                {
                    string debugString = "SendMIPCDataByMIPCTagName : ";

                    for (int i = 0; i < IPCTagNameList.Count; i++)
                    {
                        debugString = String.Concat(debugString, "\r\n", IPCTagNameList[i].ToString(), " = ", dataList[i].ToString("0.0"));
                    }

                    WriteLog(7, "", debugString);
                }

                uint[] tagAddressList = new uint[IPCTagNameList.Count];
                List<Byte[]> byteValueArray = new List<byte[]>();

                for (int i = 0; i < IPCTagNameList.Count; i++)
                {
                    tagAddressList[i] = allDataByIPCTagName[IPCTagNameList[i].ToString()].Address;
                    byteValueArray.Add(BitConverter.GetBytes(dataList[i]));
                }

                SendAndReceive sendAndReceive = new SendAndReceive();

                sendAndReceive.Send = Write_非連續(tagAddressList, (UInt16)tagAddressList.Length, byteValueArray);
                AddWriteQueue(EnumMIPCSocketName.Normal.ToString(), sendAndReceive);

                if (!waitResult)
                    return true;

                Stopwatch timer = new Stopwatch();
                timer.Restart();

                while (sendAndReceive.Result == EnumSendAndRecieve.None)
                {
                    if (timer.ElapsedMilliseconds > config.CommandTimeoutValue)
                    {
                        WriteLog(3, "", String.Concat("timeout"));
                        return false;
                    }

                    Thread.Sleep(1);
                }

                return sendAndReceive.Result == EnumSendAndRecieve.OK;
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }

        #region NewSendPackage.
        public ModbusData Read_連續(UInt32 startAddress, UInt16 length)
        {
            try
            {
                lock (newSendLockObject)
                {
                    ModbusData returnModbusData = new ModbusData();
                    returnModbusData.SeqNumber = count;
                    returnModbusData.FunctionCode = 0x01;
                    returnModbusData.StartAddress = startAddress;
                    returnModbusData.DataLength = length;
                    returnModbusData.ReceiveLength = 12 + 4 * length;

                    returnModbusData.InitialByteData();
                    count++;
                    return returnModbusData;
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return null;
            }
        }

        public ModbusData Read_非連續()
        {
            try
            {
                lock (newSendLockObject)
                {
                    ModbusData returnModbusData = null;

                    count++;

                    return returnModbusData;
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return null;
            }
        }

        public ModbusData Write_連續(UInt32 startAddress, UInt16 length, UInt32[] writeData)
        {
            try
            {
                lock (newSendLockObject)
                {
                    ModbusData returnModbusData = new ModbusData();
                    returnModbusData.SeqNumber = count;
                    returnModbusData.FunctionCode = 0x81;
                    returnModbusData.StartAddress = startAddress;
                    returnModbusData.DataLength = length;
                    returnModbusData.ReceiveLength = 12;

                    returnModbusData.DataBuffer = new byte[length * 4];

                    byte[] tempArray;

                    for (int i = 0; i < length; i++)
                    {
                        tempArray = BitConverter.GetBytes(writeData[i]);
                        tempArray.CopyTo(returnModbusData.DataBuffer, i * 4);
                    }

                    returnModbusData.InitialByteData();
                    count++;

                    return returnModbusData;
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return null;
            }
        }

        public ModbusData Write_連續(UInt32 startAddress, UInt16 length, List<Byte[]> writeData)
        {
            try
            {
                lock (newSendLockObject)
                {
                    if (length != writeData.Count)
                    {
                        WriteLog(3, "", "addressList.Count != writeData.Count");
                        return null;
                    }

                    ModbusData returnModbusData = new ModbusData();
                    returnModbusData.SeqNumber = count;
                    returnModbusData.FunctionCode = 0x81;
                    returnModbusData.StartAddress = startAddress;
                    returnModbusData.DataLength = length;
                    returnModbusData.ReceiveLength = 12;

                    returnModbusData.DataBuffer = new byte[length * 4];

                    for (int i = 0; i < length; i++)
                    {
                        returnModbusData.DataBuffer[i * 4] = writeData[i][0];
                        returnModbusData.DataBuffer[i * 4 + 1] = writeData[i][1];
                        returnModbusData.DataBuffer[i * 4 + 2] = writeData[i][2];
                        returnModbusData.DataBuffer[i * 4 + 3] = writeData[i][3];
                    }

                    returnModbusData.InitialByteData();
                    count++;

                    return returnModbusData;
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return null;
            }
        }

        public ModbusData Write_非連續(UInt32[] addressList, UInt16 length, List<Byte[]> writeData)
        {
            try
            {
                lock (newSendLockObject)
                {
                    if (addressList == null || writeData == null || addressList.Length != writeData.Count || length != writeData.Count)
                    {
                        WriteLog(3, "", "addressList.Count != writeData.Count");
                        return null;
                    }

                    ModbusData returnModbusData = new ModbusData();
                    returnModbusData.SeqNumber = count;
                    returnModbusData.FunctionCode = 0x83;
                    returnModbusData.StartAddress = 0;
                    returnModbusData.DataLength = length;

                    returnModbusData.ReceiveLength = 12;

                    returnModbusData.DataBuffer = new byte[length * 4 * 2];

                    byte[] tempArray;

                    for (int i = 0; i < length; i++)
                    {
                        tempArray = BitConverter.GetBytes(addressList[i]);
                        tempArray.CopyTo(returnModbusData.DataBuffer, i * 4);
                    }

                    for (int i = 0; i < length; i++)
                    {
                        returnModbusData.DataBuffer[length * 4 + i * 4] = writeData[i][0];
                        returnModbusData.DataBuffer[length * 4 + i * 4 + 1] = writeData[i][1];
                        returnModbusData.DataBuffer[length * 4 + i * 4 + 2] = writeData[i][2];
                        returnModbusData.DataBuffer[length * 4 + i * 4 + 3] = writeData[i][3];
                    }

                    returnModbusData.InitialByteData();
                    count++;

                    return returnModbusData;
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return null;
            }
        }

        public ModbusData Write_非連續(UInt32[] addressList, UInt16 length, Int32[] writeData)
        {
            try
            {
                lock (newSendLockObject)
                {
                    if (addressList == null || writeData == null || addressList.Length != writeData.Length)
                    {
                        WriteLog(3, "", "addressList.Count != writeData.Count");
                        return null;
                    }

                    ModbusData returnModbusData = new ModbusData();
                    returnModbusData.SeqNumber = count;
                    returnModbusData.FunctionCode = 0x83;
                    returnModbusData.StartAddress = 0;
                    returnModbusData.DataLength = length;

                    returnModbusData.ReceiveLength = 12;

                    returnModbusData.DataBuffer = new byte[length * 4 * 2];

                    byte[] tempArray;

                    for (int i = 0; i < length; i++)
                    {
                        tempArray = BitConverter.GetBytes(addressList[i]);
                        tempArray.CopyTo(returnModbusData.DataBuffer, i * 4);
                    }

                    for (int i = 0; i < length; i++)
                    {
                        tempArray = BitConverter.GetBytes(writeData[i]);
                        tempArray.CopyTo(returnModbusData.DataBuffer, length * 4 + i * 4);
                    }

                    returnModbusData.InitialByteData();
                    count++;

                    return returnModbusData;
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return null;
            }
        }
        #endregion

        #region BatteryCSV.
        private void BatteryCSVThread()
        {
            Stopwatch timer = new Stopwatch();
            string logString;
            DateTime now;

            try
            {
                while (Status != EnumControlStatus.Closing && Status != EnumControlStatus.Error)
                {
                    timer.Restart();

                    if (allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Battery_SOC.ToString()) &&
                        allDataByIPCTagName[EnumMecanumIPCdefaultTag.Battery_SOC.ToString()].Object != null &&
                        allDataByIPCTagName.ContainsKey(EnumMecanumIPCdefaultTag.Battery_V.ToString()) &&
                        allDataByIPCTagName[EnumMecanumIPCdefaultTag.Battery_V.ToString()].Object != null)
                    {
                        logString = "";
                        now = DateTime.Now;
                        logString = now.ToString("yyyy/MM/dd HH:mm:ss");

                        string status = "Idle";
                        string stepOrVelocity = "";

                        LoadUnloadCommandData loadUnloadCommand = localData.LoadUnloadData.LoadUnloadCommand;

                        if (loadUnloadCommand != null)
                        {
                            status = "LoadUnload";
                            stepOrVelocity = loadUnloadCommand.StepString;
                        }
                        else
                        {
                            MoveCommandData moveCommand = localData.MoveControlData.MoveCommand;

                            if (moveCommand != null)
                            {
                                status = moveCommand.MoveStatus.ToString();

                                if (status == EnumMoveStatus.Moving.ToString())
                                    status = String.Concat(status, "_", localData.MIPCData.MoveControlDirection.ToString());

                                if (moveCommand.MoveStatus == EnumMoveStatus.SpinTurn)
                                    stepOrVelocity = localData.MoveControlData.MotionControlData.ThetaVelocity.ToString("0");
                                else
                                    stepOrVelocity = localData.MoveControlData.MotionControlData.LineVelocity.ToString("0");
                            }
                        }

                        logString = String.Concat(logString, ",", status);
                        logString = String.Concat(logString, ",", stepOrVelocity);

                        if (localData.MIPCData.Charging)
                            logString = String.Concat(logString, ",", "Charging-", localData.MIPCData.LocalChargingCount.ToString("0"));
                        else
                            logString = String.Concat(logString, ",", "");

                        logString = String.Concat(logString, ",", localData.BatteryInfo.Battery_SOC.ToString("0.00"));
                        logString = String.Concat(logString, ",", localData.BatteryInfo.Battery_V.ToString("0.00"));
                        logString = String.Concat(logString, ",", localData.BatteryInfo.Battery_A.ToString("0.00"));
                        logString = String.Concat(logString, ",", localData.BatteryInfo.Battery_溫度1.ToString("0.00"));
                        logString = String.Concat(logString, ",", localData.BatteryInfo.Battery_溫度2.ToString("0.00"));
                        logString = String.Concat(logString, ",", localData.BatteryInfo.Meter_V.ToString("0.00"));
                        logString = String.Concat(logString, ",", localData.BatteryInfo.Meter_A.ToString("0.00"));
                        logString = String.Concat(logString, ",", localData.BatteryInfo.Meter_W.ToString("0.00"));
                        logString = String.Concat(logString, ",", localData.BatteryInfo.Meter_WH.ToString("0.00"));

                        for (int i = 0; i < 15; i++)
                            logString = String.Concat(logString, ",", localData.BatteryInfo.CellArray[i].ToString("0"));

                        logger.LogString(logString);
                    }

                    while (timer.ElapsedMilliseconds < config.BatteryCSVInterval)
                        Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exceptrion : ", ex.ToString()));
            }
        }
        #endregion

        public void StartCharging(EnumStageDirection direction)
        {
            switch (direction)
            {
                case EnumStageDirection.Left:
                    if (localData.MIPCData.CanLeftCharging)
                        localData.MIPCData.LeftChargingPIO.StartPIO();
                    break;
                case EnumStageDirection.Right:
                    if (localData.MIPCData.CanRightCharging)
                        localData.MIPCData.RightChargingPIO.StartPIO();
                    break;
                default:
                    break;
            }
        }

        public void StopCharging()
        {
            if (localData.MIPCData.CanRightCharging)
                localData.MIPCData.RightChargingPIO.StopPIO();

            if (localData.MIPCData.CanLeftCharging)
                localData.MIPCData.LeftChargingPIO.StopPIO();
        }

        private void InitailCharging()
        {
            switch (localData.MainFlowConfig.AGVType)
            {
                case EnumAGVType.UMTC:
                    localData.MIPCData.RightChargingPIO = new PIOFlow_UMTC_Charging();
                    localData.MIPCData.RightChargingPIO.Initial(alarmHandler, this, "RightCharging", "", "Charging");
                    localData.MIPCData.CanRightCharging = true;
                    break;
                case EnumAGVType.AGC:
                    break;
                case EnumAGVType.PTI:
                    break;
                case EnumAGVType.ATS:
                    localData.MIPCData.RightChargingPIO = new PIOFlow_ATS_Charging();
                    localData.MIPCData.RightChargingPIO.Initial(alarmHandler, this, "RightCharging", "", "Charging");
                    localData.MIPCData.CanRightCharging = true;
                    break;
                default:
                    break;
            }
        }

        public bool WriteConfig(string groupID, string path)
        {
            if (localData.AutoManual == EnumAutoState.Manual &&
                localData.MoveControlData.MoveCommand == null &&
                localData.LoadUnloadData.LoadUnloadCommand == null &&
                !localData.MoveControlData.MotionControlData.JoystickMode)
            {
                if (AllDataByClassification.ContainsKey(groupID))
                {
                    try
                    {
                        List<string> writeData = new List<string>();

                        foreach (MIPCData holly in AllDataByClassification[groupID])
                        {
                            writeData.Add(String.Concat(holly.DataName, ",", ((float)holly.Object).ToString("0.000")));
                        }

                        using (StreamWriter outputFile = new StreamWriter(path))
                        {
                            for (int i = 0; i < writeData.Count; i++)
                                outputFile.WriteLine(writeData[i]);
                        }

                        return true;
                    }
                    catch { }
                }
            }

            return false;
        }

        public bool SetConfig(string path)
        {
            if (localData.AutoManual == EnumAutoState.Manual &&
                localData.MoveControlData.MoveCommand == null &&
                localData.LoadUnloadData.LoadUnloadCommand == null &&
                !localData.MoveControlData.MotionControlData.JoystickMode)
            {
                try
                {
                    string[] allRows = File.ReadAllLines(path);
                    string[] yo;
                    string title = "";
                    float value = 0;
                    bool result;

                    string allMessage = "匯入參數 : ";

                    for (int i = 0; i < allRows.Length; i++)
                    {
                        yo = Regex.Split(allRows[i], ",", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));

                        try
                        {
                            if (yo.Length == 2)
                            {
                                title = yo[0];
                                value = float.Parse(yo[1]);

                                result = SendMIPCDataByMIPCTagName(new List<string>() { title }, new List<float>() { value });

                                allMessage = String.Concat(allMessage, "\r\n", "Line ", i.ToString(), " , ", (result ? "成功" : "失敗"), ", Data : ", allRows[i]);
                            }
                        }
                        catch
                        {
                            allMessage = String.Concat(allMessage, "\r\n", "Line ", i.ToString(), " , Exceptrion, Data : ", allRows[i]);
                        }
                    }

                    WriteMotionLog(7, "", allMessage);
                    return true;
                }
                catch { }
            }

            return false;
        }
    }
}