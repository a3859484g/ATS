using Mirle.Agv.INX.Controller;
using Mirle.Agv.INX.Controller.Tools;
using Mirle.Agv.INX.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace Mirle.Agv.INX.Control
{
    public class LoadUnload_UMTC : LoadUnload
    {
        private BarcodeReader_Keyence sr1000_R = null;
        private DistanceSensor_Keyence distanceSensor_R = null;
        private BarcodeReader_Keyence cstIDReader = null;

        private double delayTime = 0;

        private Dictionary<string, Dictionary<EnumLoadUnloadAxisCommandType, string>> axisCommandString = new Dictionary<string, Dictionary<EnumLoadUnloadAxisCommandType, string>>();

        private List<string> jogStopStringList = new List<string>();
        private List<float> jogStopValueList = new List<float>();

        private double loadUnloadZOffset = 1.0;

        private Dictionary<string, string> axisPosLimitSensor = new Dictionary<string, string>();
        private Dictionary<string, string> axisNagLimitSensor = new Dictionary<string, string>();
        private Dictionary<string, string> axisHomeSensor = new Dictionary<string, string>();

        private Dictionary<string, double> inPositionRange = new Dictionary<string, double>();

        private Dictionary<string, EnumAxisMoveStatus> axisPreStatus = new Dictionary<string, EnumAxisMoveStatus>();
        private Dictionary<string, Stopwatch> axisPreTimer = new Dictionary<string, Stopwatch>();
        private double axisStatusDelayTime = 1000;

        private bool logMode = true;

        private bool initialEnd = false;

        private double readCSTZEncoder = 300;

        public override void Initial(MIPCControlHandler mipcControl, AlarmHandler alarmHandler)
        {
            this.alarmHandler = alarmHandler;
            this.mipcControl = mipcControl;
            device = MethodInfo.GetCurrentMethod().ReflectedType.Name;

            ReadPIOTimeoutCSV();
            //ReadAddressOffsetCSV();
            ReadLoadUnloadOffsetConfigXML();

            InitialStageCSV();
            InitialAxisData();
            InitialRollerData();

            InitialStageNumberToBarcodeReaderSettingXML();
            CheckAllStringInMIPCConfig();

            RightPIO = new PIOFlow_UMTC_LoadUnloadSemi();
            RightPIO.Initial(alarmHandler, mipcControl, "Right", "", normalLogName);
            CanRightLoadUnload = true;

            ConnectAlignmentDevice();
            initialEnd = true;
        }

        #region 預設設定值, 減少Config存在.
        private void InitialStageCSV()
        {
            StageData stageData;

            #region Stage-0.
            stageData = new StageData();
            stageData.Name = "Type1";
            stageData.Direction = EnumStageDirection.Right;
            stageData.StargeNumber = 0;
            stageData.Benchmark_Y = 180;
            stageData.Encoder_Z = 308.1;
            RightStageDataList.Add(stageData.StargeNumber, stageData);
            #endregion

            #region Stage-1.
            stageData = new StageData();
            stageData.Name = "Type2";
            stageData.Direction = EnumStageDirection.Right;
            stageData.StargeNumber = 1;
            stageData.Benchmark_Y = 280;
            stageData.Encoder_Z = 306.5;
            RightStageDataList.Add(stageData.StargeNumber, stageData);
            #endregion

            #region Stage-2.
            stageData = new StageData();
            stageData.Name = "Type3";
            stageData.Direction = EnumStageDirection.Right;
            stageData.StargeNumber = 2;
            stageData.Benchmark_Y = 380;
            stageData.Encoder_Z = 303;
            RightStageDataList.Add(stageData.StargeNumber, stageData);
            #endregion

            ReadStargeCSV();
        }

        private void InitialStageNumberToBarcodeReaderSettingXML()
        {
            #region BarcodeReader ID to RealDistance.
            BarcodeReaderIDToRealDistance.Add("001", -240);
            BarcodeReaderIDToRealDistance.Add("002", -180);
            BarcodeReaderIDToRealDistance.Add("003", -120);
            BarcodeReaderIDToRealDistance.Add("004", -60);
            BarcodeReaderIDToRealDistance.Add("005", 0);
            BarcodeReaderIDToRealDistance.Add("006", 60);
            BarcodeReaderIDToRealDistance.Add("007", 120);
            BarcodeReaderIDToRealDistance.Add("008", 180);
            BarcodeReaderIDToRealDistance.Add("009", 240);

            BarcodeReaderIDToRealDistance.Add("101", -240);
            BarcodeReaderIDToRealDistance.Add("102", -180);
            BarcodeReaderIDToRealDistance.Add("103", -120);
            BarcodeReaderIDToRealDistance.Add("104", -60);
            BarcodeReaderIDToRealDistance.Add("105", 0);
            BarcodeReaderIDToRealDistance.Add("106", 60);
            BarcodeReaderIDToRealDistance.Add("107", 120);
            BarcodeReaderIDToRealDistance.Add("108", 180);
            BarcodeReaderIDToRealDistance.Add("109", 240);

            BarcodeReaderIDToRealDistance.Add("201", -240);
            BarcodeReaderIDToRealDistance.Add("202", -180);
            BarcodeReaderIDToRealDistance.Add("203", -120);
            BarcodeReaderIDToRealDistance.Add("204", -60);
            BarcodeReaderIDToRealDistance.Add("205", 0);
            BarcodeReaderIDToRealDistance.Add("206", 60);
            BarcodeReaderIDToRealDistance.Add("207", 120);
            BarcodeReaderIDToRealDistance.Add("208", 180);
            BarcodeReaderIDToRealDistance.Add("209", 240);

            BarcodeReaderIDToRealDistance.Add("301", -240);
            BarcodeReaderIDToRealDistance.Add("302", -180);
            BarcodeReaderIDToRealDistance.Add("303", -120);
            BarcodeReaderIDToRealDistance.Add("304", -60);
            BarcodeReaderIDToRealDistance.Add("305", 0);
            BarcodeReaderIDToRealDistance.Add("306", 60);
            BarcodeReaderIDToRealDistance.Add("307", 120);
            BarcodeReaderIDToRealDistance.Add("308", 180);
            BarcodeReaderIDToRealDistance.Add("309", 240);
            #endregion

            StageNumberToBarcodeReaderSetting temp;

            #region Stage-0.
            temp = new StageNumberToBarcodeReaderSetting();
            temp.StageNumber = 0;
            temp.BarcodeReaderMode = "1";
            temp.PixelToMM_X = 9.766666666666666666666666666666;
            temp.PixelToMM_Y = 9.766666666666666666666666666666;
            RightStageBarocdeReaderSetting.Add(temp.StageNumber, temp);
            #endregion

            #region Stage-1.
            temp = new StageNumberToBarcodeReaderSetting();
            temp.StageNumber = 1;
            temp.BarcodeReaderMode = "2";
            temp.PixelToMM_X = 6.8166666666666666666666666666667;
            temp.PixelToMM_Y = 6.8166666666666666666666666666667;
            RightStageBarocdeReaderSetting.Add(temp.StageNumber, temp);
            #endregion

            #region Stage-2.
            temp = new StageNumberToBarcodeReaderSetting();
            temp.StageNumber = 2;
            temp.BarcodeReaderMode = "3";
            temp.PixelToMM_X = 5.1;
            temp.PixelToMM_Y = 5.1;
            RightStageBarocdeReaderSetting.Add(temp.StageNumber, temp);
            #endregion

            ReadStageNumberToBarcodeReaderSettingXML();
        }

        private void InitialAxisData()
        {
            ReadAxisData();
        }
        #endregion

        public override void CloseLoadUnload()
        {
        }

        #region Axis Sensor Name.
        private string CV入料 = EnumProfaceStringTag.Roller_CV入料.ToString();
        private string CV減速 = EnumProfaceStringTag.Roller_CV減速.ToString();
        private string CV停止 = EnumProfaceStringTag.Roller_CV停止.ToString();

        private string Z軸上極限 = EnumProfaceStringTag.Z軸正極限.ToString();
        private string Z軸上位 = EnumProfaceStringTag.Z軸_Slave_原點.ToString();
        private string Z軸下位 = EnumProfaceStringTag.Z軸原點.ToString();
        private string Z軸下極限 = EnumProfaceStringTag.Z軸負極限.ToString();

        private string P軸前極限 = EnumProfaceStringTag.P軸正極限.ToString();
        private string P軸原點 = EnumProfaceStringTag.P軸原點.ToString();
        private string P軸後極限 = EnumProfaceStringTag.P軸負極限.ToString();

        private string 順時極限 = EnumProfaceStringTag.Theta軸正極限.ToString();
        private string Theta軸原點 = EnumProfaceStringTag.Theta軸原點.ToString();
        private string 逆時極限 = EnumProfaceStringTag.Theta軸負極限.ToString();
        #endregion

        private string Z軸從軸上極限 = EnumProfaceStringTag.Z軸_Slave_正極限.ToString();
        private string Z軸從軸下極限 = EnumProfaceStringTag.Z軸_Slave_負極限.ToString();

        private DataDelayAndChange Getway通訊正常_Z軸 = new DataDelayAndChange(5000, EnumDelayType.OnDelay);
        private DataDelayAndChange Getway通訊正常_Z軸_Slave = new DataDelayAndChange(5000, EnumDelayType.OnDelay);
        private DataDelayAndChange Getway通訊正常_P軸 = new DataDelayAndChange(5000, EnumDelayType.OnDelay);
        private DataDelayAndChange Getway通訊正常_Theta軸 = new DataDelayAndChange(5000, EnumDelayType.OnDelay);
        private DataDelayAndChange Getway通訊正常_Roller = new DataDelayAndChange(5000, EnumDelayType.OnDelay);

        #region InitialRollerData.
        private void InitialRollerData()
        {
            Getway通訊正常_Z軸.Data = true;
            Getway通訊正常_Z軸_Slave.Data = true;
            Getway通訊正常_P軸.Data = true;
            Getway通訊正常_Theta軸.Data = true;
            Getway通訊正常_Roller.Data = true;

            CanPause = true;
            BreakenStepMode = true;

            HomeText = String.Concat("1. ", P軸原點, " 和 ", Theta軸原點, " 都On\r\n",
                                     "2. ", Z軸上極限, " 或 ", Z軸上位, " 其中一個On\r\n",
                                     "( 以上其中一條滿足即可 )");
            axisPreStatus.Add(EnumLoadUnloadAxisName.Z軸.ToString(), EnumAxisMoveStatus.None);
            axisPreStatus.Add(EnumLoadUnloadAxisName.P軸.ToString(), EnumAxisMoveStatus.None);
            axisPreStatus.Add(EnumLoadUnloadAxisName.Theta軸.ToString(), EnumAxisMoveStatus.None);
            axisPreStatus.Add(EnumLoadUnloadAxisName.Roller.ToString(), EnumAxisMoveStatus.None);

            axisPreTimer.Add(EnumLoadUnloadAxisName.Z軸.ToString(), new Stopwatch());
            axisPreTimer.Add(EnumLoadUnloadAxisName.P軸.ToString(), new Stopwatch());
            axisPreTimer.Add(EnumLoadUnloadAxisName.Theta軸.ToString(), new Stopwatch());
            axisPreTimer.Add(EnumLoadUnloadAxisName.Roller.ToString(), new Stopwatch());

            inPositionRange.Add(EnumLoadUnloadAxisName.Z軸.ToString(), 5);
            inPositionRange.Add(EnumLoadUnloadAxisName.Z軸_Slave.ToString(), 5);
            inPositionRange.Add(EnumLoadUnloadAxisName.P軸.ToString(), 5);
            inPositionRange.Add(EnumLoadUnloadAxisName.Roller.ToString(), 10);
            inPositionRange.Add(EnumLoadUnloadAxisName.Theta軸.ToString(), 0.1);

            FeedbackAxisList.Add(EnumLoadUnloadAxisName.Z軸.ToString());
            FeedbackAxisList.Add(EnumLoadUnloadAxisName.Z軸_Slave.ToString());
            AxisList.Add(EnumLoadUnloadAxisName.Z軸.ToString());
            axisPosLimitSensor.Add(EnumLoadUnloadAxisName.Z軸.ToString(), Z軸上極限);
            axisNagLimitSensor.Add(EnumLoadUnloadAxisName.Z軸.ToString(), Z軸下極限);
            axisHomeSensor.Add(EnumLoadUnloadAxisName.Z軸.ToString(), Z軸下位);

            AxisCanJog.Add(false);
            AxisPosName.Add(EnumProfaceStringTag.Z軸Jog正.ToString());
            AxisNagName.Add(EnumProfaceStringTag.Z軸Jog負.ToString());
            AxisSensorList.Add(AxisList[AxisList.Count - 1], new List<string>() { Z軸上極限, Z軸上位, Z軸下位, Z軸下極限 });
            AxisSensorDataList.Add(AxisList[AxisList.Count - 1], new List<DataDelayAndChange>() { new DataDelayAndChange(delayTime, EnumDelayType.OnDelay), new DataDelayAndChange(delayTime, EnumDelayType.OnDelay), new DataDelayAndChange(delayTime, EnumDelayType.OnDelay), new DataDelayAndChange(delayTime, EnumDelayType.OnDelay) });

            FeedbackAxisList.Add(EnumLoadUnloadAxisName.P軸.ToString());
            AxisList.Add(EnumLoadUnloadAxisName.P軸.ToString());
            axisPosLimitSensor.Add(EnumLoadUnloadAxisName.P軸.ToString(), P軸前極限);
            axisNagLimitSensor.Add(EnumLoadUnloadAxisName.P軸.ToString(), P軸後極限);
            axisHomeSensor.Add(EnumLoadUnloadAxisName.P軸.ToString(), P軸原點);

            AxisCanJog.Add(false);
            AxisPosName.Add(EnumProfaceStringTag.P軸Jog正.ToString());
            AxisNagName.Add(EnumProfaceStringTag.P軸Jog負.ToString());
            AxisSensorList.Add(AxisList[AxisList.Count - 1], new List<string>() { P軸前極限, P軸原點, P軸後極限 });
            AxisSensorDataList.Add(AxisList[AxisList.Count - 1], new List<DataDelayAndChange>() { new DataDelayAndChange(delayTime, EnumDelayType.OnDelay), new DataDelayAndChange(delayTime, EnumDelayType.OnDelay), new DataDelayAndChange(delayTime, EnumDelayType.OnDelay) });

            FeedbackAxisList.Add(EnumLoadUnloadAxisName.Theta軸.ToString());
            AxisList.Add(EnumLoadUnloadAxisName.Theta軸.ToString());
            axisPosLimitSensor.Add(EnumLoadUnloadAxisName.Theta軸.ToString(), 順時極限);
            axisNagLimitSensor.Add(EnumLoadUnloadAxisName.Theta軸.ToString(), 逆時極限);
            axisHomeSensor.Add(EnumLoadUnloadAxisName.Theta軸.ToString(), Theta軸原點);

            AxisCanJog.Add(false);
            AxisPosName.Add(EnumProfaceStringTag.Theta軸Jog正.ToString());
            AxisNagName.Add(EnumProfaceStringTag.Theta軸Jog負.ToString());
            AxisSensorList.Add(AxisList[AxisList.Count - 1], new List<string>() { 順時極限, Theta軸原點, 逆時極限 });
            AxisSensorDataList.Add(AxisList[AxisList.Count - 1], new List<DataDelayAndChange>() { new DataDelayAndChange(delayTime, EnumDelayType.OnDelay), new DataDelayAndChange(delayTime, EnumDelayType.OnDelay), new DataDelayAndChange(delayTime, EnumDelayType.OnDelay) });

            FeedbackAxisList.Add(EnumLoadUnloadAxisName.Roller.ToString());
            AxisList.Add(EnumLoadUnloadAxisName.Roller.ToString());
            axisPosLimitSensor.Add(EnumLoadUnloadAxisName.Roller.ToString(), "");
            axisNagLimitSensor.Add(EnumLoadUnloadAxisName.Roller.ToString(), "");

            AxisCanJog.Add(false);
            AxisPosName.Add(EnumProfaceStringTag.RollerJog正.ToString());
            AxisNagName.Add(EnumProfaceStringTag.RollerJog負.ToString());
            AxisSensorList.Add(AxisList[AxisList.Count - 1], new List<string>() { CV入料, CV減速, CV停止 });
            AxisSensorDataList.Add(AxisList[AxisList.Count - 1], new List<DataDelayAndChange>() { new DataDelayAndChange(delayTime, EnumDelayType.OnDelay), new DataDelayAndChange(delayTime, EnumDelayType.OnDelay), new DataDelayAndChange(delayTime, EnumDelayType.OnDelay) });

            Dictionary<EnumLoadUnloadAxisCommandType, string> tempD;

            for (int i = 0; i < AxisList.Count; i++)
            {
                axisNameToStopTagList.Add(AxisList[i], new List<string>());
                axisNameToStopValueList.Add(AxisList[i], new List<float>());

                AllAxisSensorData.Add(AxisList[i], new Dictionary<string, DataDelayAndChange>());

                for (int j = 0; j < AxisSensorDataList[AxisList[i]].Count; j++)
                    AllAxisSensorData[AxisList[i]].Add(AxisSensorList[AxisList[i]][j], AxisSensorDataList[AxisList[i]][j]);

                localData.LoadUnloadData.CVFeedbackData.Add(AxisList[i], null);
                localData.LoadUnloadData.CVEncoderOffsetValue.Add(AxisList[i], 0);
                localData.LoadUnloadData.CVHomeOffsetValue.Add(AxisList[i], 0);

                if (AxisList[i] == EnumLoadUnloadAxisName.Z軸.ToString())
                {
                    localData.LoadUnloadData.CVFeedbackData.Add(EnumLoadUnloadAxisName.Z軸_Slave.ToString(), null);
                    localData.LoadUnloadData.CVEncoderOffsetValue.Add(EnumLoadUnloadAxisName.Z軸_Slave.ToString(), 0);
                    localData.LoadUnloadData.CVHomeOffsetValue.Add(EnumLoadUnloadAxisName.Z軸_Slave.ToString(), 0);
                }

                tempD = new Dictionary<EnumLoadUnloadAxisCommandType, string>();

                foreach (EnumLoadUnloadAxisCommandType item in (EnumLoadUnloadAxisCommandType[])Enum.GetValues(typeof(EnumLoadUnloadAxisCommandType)))
                {
                    tempD.Add(item, String.Concat(AxisList[i], "_", item.ToString()));
                }

                axisCommandString.Add(AxisList[i], tempD);

                jogStopStringList.Add(tempD[EnumLoadUnloadAxisCommandType.Deceleration]);
                jogStopStringList.Add(tempD[EnumLoadUnloadAxisCommandType.StopCommand]);
                jogStopValueList.Add((float)axisDataList[AxisList[i]].AutoDeceleration);
                jogStopValueList.Add(1);

                axisNameToStopTagList[AxisList[i]].Add(tempD[EnumLoadUnloadAxisCommandType.Deceleration]);
                axisNameToStopTagList[AxisList[i]].Add(tempD[EnumLoadUnloadAxisCommandType.StopCommand]);

                axisNameToStopValueList[AxisList[i]].Add((float)axisDataList[AxisList[i]].AutoDeceleration);
                axisNameToStopValueList[AxisList[i]].Add(1);
            }
        }

        private Dictionary<string, List<string>> axisNameToStopTagList = new Dictionary<string, List<string>>();
        private Dictionary<string, List<float>> axisNameToStopValueList = new Dictionary<string, List<float>>();

        private void CheckAllStringInMIPCConfig()
        {
            for (int i = 0; i < AxisList.Count; i++)
            {
                if (!axisDataList.ContainsKey(AxisList[i]))
                {
                    WriteLog(3, "", String.Concat("AxisDataList.csv內 無 AxisName = ", AxisList[i]));
                    configAllOK = false;
                }
            }

            if (!configAllOK)
                SetAlarmCode(EnumLoadUnloadControlErrorCode.取放貨初始化失敗_MIPCTag缺少);
        }
        #endregion

        public bool CheckAixsIsServoOn(string axisName)
        {
            AxisFeedbackData temp = localData.LoadUnloadData.CVFeedbackData[axisName];
            AxisFeedbackData temp2 = (axisName == EnumLoadUnloadAxisName.Z軸.ToString() ? localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()] : temp);

            if (temp == null || temp2 == null)
                return false;
            else if (temp.AxisServoOnOff == EnumAxisServoOnOff.ServoOff || temp2.AxisServoOnOff == EnumAxisServoOnOff.ServoOff)
                return false;
            else
                return true;
        }

        private List<EnumLoadUnloadControlErrorCode> alarmCodeClearList = new List<EnumLoadUnloadControlErrorCode>()
        {
            EnumLoadUnloadControlErrorCode.TA1_Timeout,
            EnumLoadUnloadControlErrorCode.TA2_Timeout,
            EnumLoadUnloadControlErrorCode.TA3_Timeout,
            EnumLoadUnloadControlErrorCode.TP1_Timeout,
            EnumLoadUnloadControlErrorCode.TP2_Timeout,
            EnumLoadUnloadControlErrorCode.TP3_Timeout,
            EnumLoadUnloadControlErrorCode.TP4_Timeout,
            EnumLoadUnloadControlErrorCode.TP5_Timeout,

            EnumLoadUnloadControlErrorCode.取放貨中EQPIOOff,
            EnumLoadUnloadControlErrorCode.取放命令與EQRequest不相符,
            EnumLoadUnloadControlErrorCode.RollerStop後CV訊號異常,
            EnumLoadUnloadControlErrorCode.取放貨中CV檢知異常,
            EnumLoadUnloadControlErrorCode.取放貨中EMS,
            EnumLoadUnloadControlErrorCode.取貨_LoadingOn異常,
            EnumLoadUnloadControlErrorCode.放貨_LoadingOff異常,
            EnumLoadUnloadControlErrorCode.AlignmentNG,
            EnumLoadUnloadControlErrorCode.HomeSensor未On,
            EnumLoadUnloadControlErrorCode.Z軸上定位未On,
            EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail,
            EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange,
            EnumLoadUnloadControlErrorCode.AlignmentValueNG,
            EnumLoadUnloadControlErrorCode.取放中極限觸發,
            EnumLoadUnloadControlErrorCode.取放中軸異常,
            EnumLoadUnloadControlErrorCode.取放中安全迴路異常,
            EnumLoadUnloadControlErrorCode.RollerStopTimeout,
            EnumLoadUnloadControlErrorCode.ServoOnTimeout,
            EnumLoadUnloadControlErrorCode.Port站ES或HO_AVBLNotOn,
            EnumLoadUnloadControlErrorCode.取放貨站點資訊異常,
            EnumLoadUnloadControlErrorCode.取放貨異常_Z軸升降中CVSensor異常,

            EnumLoadUnloadControlErrorCode.回Home失敗_CVSensor狀態異常,
            EnumLoadUnloadControlErrorCode.回Home失敗_CST回CV_Timeout,
            EnumLoadUnloadControlErrorCode.回Home失敗_P軸不在Home,
            EnumLoadUnloadControlErrorCode.回Home失敗_Theta軸不再Home,
            EnumLoadUnloadControlErrorCode.回Home失敗_Z軸不在上定位,
            EnumLoadUnloadControlErrorCode.回Home失敗_ServoOn_Timeout,
            EnumLoadUnloadControlErrorCode.回Home失敗_Exception,
            EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗,
            EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止,
            EnumLoadUnloadControlErrorCode.回Home失敗_極限Sensor未On,
            EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout,
            EnumLoadUnloadControlErrorCode.回Home失敗_HomeSensor未on,
            EnumLoadUnloadControlErrorCode.回Home失敗_HomeSensor未off
        };

        protected override void AlarmCodeClear()
        {
            if (localData.AutoManual == EnumAutoState.Manual)
            {
                for (int i = 0; i < alarmCodeClearList.Count; i++)
                    ResetAlarmCode(alarmCodeClearList[i]);
            }
            else
            {
                for (int i = 0; i < alarmCodeClearList.Count; i++)
                {
                    if (alarmHandler.AlarmCodeTable.ContainsKey((int)alarmCodeClearList[i]) &&
                        alarmHandler.AlarmCodeTable[(int)alarmCodeClearList[i]].Level == EnumAlarmLevel.Alarm)
                    {
                    }
                    else
                        ResetAlarmCode(alarmCodeClearList[i]);
                }
            }
        }

        public override void ResetAlarm()
        {
            AlarmCodeClear();

            if (localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null &&
                !reconnectedAndResetAxisError)
            {
                ResetAlarm_ReConnectAndResetAxisError();
            }
        }

        private void ResetAxis(EnumLoadUnloadAxisName axisName)
        {
            if (axisName == EnumLoadUnloadAxisName.Z軸)
            {
                if (CheckAxisError(axisName) || CheckAxisError(EnumLoadUnloadAxisName.Z軸_Slave))
                {
                    ServoOff(axisName.ToString(), true, 2000);
                    ServoOn(axisName.ToString(), true, 2000);
                    ServoOff(axisName.ToString(), true, 2000);
                    ServoOn(axisName.ToString(), true, 2000);
                }
            }
            else if (CheckAxisError(axisName))
            {
                ServoOff(axisName.ToString(), true, 2000);
                ServoOn(axisName.ToString(), true, 2000);
                ServoOff(axisName.ToString(), true, 2000);
                ServoOn(axisName.ToString(), true, 2000);
            }
        }

        private bool reconnectedAndResetAxisError = false;

        private void ResetAlarm_ReConnectAndResetAxisError()
        {
            reconnectedAndResetAxisError = true;
            ConnectAlignmentDevice();

            if (!localData.LoadUnloadData.ForkHome && localData.MIPCData.U動力電)
            {
                ResetAxis(EnumLoadUnloadAxisName.P軸);
                ResetAxis(EnumLoadUnloadAxisName.Theta軸);
                ResetAxis(EnumLoadUnloadAxisName.Roller);
                ResetAxis(EnumLoadUnloadAxisName.Z軸);
            }

            reconnectedAndResetAxisError = false;
        }

        #region 補正元件連線.
        private void ConnectAlignmentDevice()
        {
            if (localData.SimulateMode)
                return;

            string errorMessage = "";

            if (sr1000_R == null)
            {
                sr1000_R = new BarcodeReader_Keyence();

                if (!sr1000_R.Connect("192.168.29.216", ref errorMessage))
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.取放貨補正元件_BarcoderReader連線失敗);
            }
            else
            {
                if (!sr1000_R.Connected)
                {
                    if (sr1000_R.Connect("192.168.29.216", ref errorMessage))
                        ResetAlarmCode(EnumLoadUnloadControlErrorCode.取放貨補正元件_BarcoderReader連線失敗);
                }
                else if (sr1000_R.Error)
                {
                    sr1000_R.ResetError();

                    if (!sr1000_R.Error)
                        ResetAlarmCode(EnumLoadUnloadControlErrorCode.取放貨補正元件_BarcoderReader斷線);
                }
            }

            if (cstIDReader == null)
            {
                cstIDReader = new BarcodeReader_Keyence();

                if (!cstIDReader.Connect("192.168.29.123", ref errorMessage))
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.CSTIDReader_連線失敗);
            }
            else
            {
                if (!cstIDReader.Connected)
                {
                    if (cstIDReader.Connect("192.168.29.123", ref errorMessage))
                        ResetAlarmCode(EnumLoadUnloadControlErrorCode.CSTIDReader_連線失敗);
                }
                else if (cstIDReader.Error)
                {
                    cstIDReader.ResetError();

                    if (!cstIDReader.Error)
                        ResetAlarmCode(EnumLoadUnloadControlErrorCode.CSTIDReader_斷線);
                }
            }

            if (distanceSensor_R == null)
            {
                distanceSensor_R = new DistanceSensor_Keyence();
                distanceSensor_R.Initial("192.168.29.218");
            }

            if (!distanceSensor_R.Connected)
            {
                if (distanceSensor_R.Connect())
                    ResetAlarmCode(EnumLoadUnloadControlErrorCode.取放貨補正元件_雷射測距連線失敗);
                else
                    ResetAlarmCode(EnumLoadUnloadControlErrorCode.取放貨補正元件_雷射測距連線失敗);
            }
        }
        #endregion

        private EnumSafetyLevel GetSafetySensorStatus()
        {
            if (localData.MIPCData.SafetySensorStatus == EnumSafetyLevel.EMO ||
                localData.MIPCData.SafetySensorStatus == EnumSafetyLevel.IPCEMO)
                return EnumSafetyLevel.EMO;
            else if (localData.MIPCData.SafetySensorStatus == EnumSafetyLevel.EMS)
                return EnumSafetyLevel.EMO;
            else if (localData.MIPCData.SafetySensorStatus == EnumSafetyLevel.SlowStop)
                return EnumSafetyLevel.Normal;
            else if (localData.LoadUnloadData.LoadUnloadCommand.Pause)
                return EnumSafetyLevel.SlowStop;
            else
                return EnumSafetyLevel.Normal;
        }

        private EnumLoadUnloadControlErrorCode GetPIOAlarmCode()
        {
            EnumLoadUnloadControlErrorCode alarmCode = EnumLoadUnloadControlErrorCode.None;

            switch (RightPIO.Timeout)
            {
                case EnumPIOStatus.TA1:
                    alarmCode = EnumLoadUnloadControlErrorCode.TA1_Timeout;
                    break;
                case EnumPIOStatus.TA2:
                    alarmCode = EnumLoadUnloadControlErrorCode.TA2_Timeout;
                    break;
                case EnumPIOStatus.TA3:
                    alarmCode = EnumLoadUnloadControlErrorCode.TA3_Timeout;
                    break;
                case EnumPIOStatus.TP1:
                    alarmCode = EnumLoadUnloadControlErrorCode.TP1_Timeout;
                    break;
                case EnumPIOStatus.TP2:
                    alarmCode = EnumLoadUnloadControlErrorCode.TP2_Timeout;
                    break;
                case EnumPIOStatus.TP3:
                    alarmCode = EnumLoadUnloadControlErrorCode.TP3_Timeout;
                    break;
                case EnumPIOStatus.TP4:
                    alarmCode = EnumLoadUnloadControlErrorCode.TP4_Timeout;
                    break;
                case EnumPIOStatus.TP5:
                    alarmCode = EnumLoadUnloadControlErrorCode.TP5_Timeout;
                    break;
                case EnumPIOStatus.LoadUnloadSingalError:
                    alarmCode = EnumLoadUnloadControlErrorCode.取放命令與EQRequest不相符;
                    break;
                default:
                    break;
            }

            localData.LoadUnloadData.LoadUnloadCommand.PIOResult = RightPIO.Timeout;
            return alarmCode;
        }

        private void SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode errorCode, EnumLoadUnloadErrorLevel level)
        {
            SetAlarmCode(errorCode);

            LoadUnloadCommandData command = localData.LoadUnloadData.LoadUnloadCommand;

            if (command.ErrorLevel == level)
                return;

            switch (command.ErrorLevel)
            {
                case EnumLoadUnloadErrorLevel.None:
                    command.ErrorCode = errorCode;
                    command.ErrorLevel = level;

                    break;
                case EnumLoadUnloadErrorLevel.PrePIOError:
                    if (level == EnumLoadUnloadErrorLevel.Error)
                    {
                        command.ErrorLevel = level;
                        command.ErrorCode = errorCode;
                    }

                    break;
                case EnumLoadUnloadErrorLevel.AfterPIOError:
                    if (level == EnumLoadUnloadErrorLevel.Error)
                    {
                        command.ErrorLevel = EnumLoadUnloadErrorLevel.AfterPIOErrorAndActionError;
                        command.ErrorCode = errorCode;
                    }

                    break;
                case EnumLoadUnloadErrorLevel.AfterPIOErrorAndActionError:
                    break;
                case EnumLoadUnloadErrorLevel.Error:
                    break;
            }
        }

        private bool CanZAxisStopInPassLine()
        {
            if (localData.MainFlowConfig.HomeInUpOrDownPosition &&
                (!localData.MainFlowConfig.CheckPassLineSensor || AllAxisSensorData[EnumLoadUnloadAxisName.Z軸.ToString()][Z軸上位].Data) &&
                !localData.LoadUnloadData.Loading)
            {
                // 基本條件 : 開啟功能 & 上定位Sensor on.
                LoadUnloadCommandData command = localData.LoadUnloadData.LoadUnloadCommand;

                switch (command.Action)
                {
                    case EnumLoadUnload.Load:
                        if (command.ErrorLevel == EnumLoadUnloadErrorLevel.PrePIOError)
                            return true;
                        else
                            WriteLog(5, "", "不應該進這邊");

                        break;
                    case EnumLoadUnload.Unload:
                        if (command.ErrorLevel == EnumLoadUnloadErrorLevel.AfterPIOError ||
                            command.ErrorLevel == EnumLoadUnloadErrorLevel.AfterPIOErrorAndActionError ||
                            command.ErrorLevel == EnumLoadUnloadErrorLevel.None)
                            return true;
                        else
                            WriteLog(5, "", "不應該進這邊");

                        break;
                }
            }

            return false;
        }

        public override void LoadUnloadStart()
        {
            double waitRollerStopTimeout = 5000;
            Stopwatch timer = new Stopwatch();
            LoadUnloadCommandData command = localData.LoadUnloadData.LoadUnloadCommand;

            EnumSafetyLevel lastStatus = EnumSafetyLevel.Normal;
            EnumSafetyLevel nowStatus = EnumSafetyLevel.Normal;

            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step1_檢查Loading;
            command.StepString = ((EnumUMTCLoadUnloadStatus)command.StatusStep).ToString();

            WriteLog(7, "", String.Concat("Step Change to : ", ((EnumUMTCLoadUnloadStatus)command.StatusStep).ToString()));

            int alignmentCount = 0;
            int maxAlignmentCount = 5;

            double encoderAbs_P = 0;
            double encoderAbs_Z = 0;
            double encoderAbs_Theta = 0;
            double encoderAbs_Y = 0;
            AlignmentValueData temp;

            Stopwatch rollerErrorFlowTimer = new Stopwatch();

            Stopwatch errorDelayTimer = new Stopwatch();

            while (command.StatusStep != (int)EnumUMTCLoadUnloadStatus.Step0_Idle)
            {
                UpdateForkHomeStatus();
                nowStatus = GetSafetySensorStatus();

                switch (command.StatusStep)
                {
                    case (int)EnumUMTCLoadUnloadStatus.Step1_檢查Loading:
                        #region Step1_檢查Loading.
                        UpdateForkHomeStatus();

                        switch (command.Action)
                        {
                            case EnumLoadUnload.Load:
                                if (localData.AutoManual == EnumAutoState.Manual)
                                {
                                    if (localData.LoadUnloadData.Loading)
                                    {
                                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step26_LoadUnloadEnd;
                                        SetAlarmCodeAndSetCommandErrorCode(
                                            EnumLoadUnloadControlErrorCode.取貨_LoadingOn異常,
                                            EnumLoadUnloadErrorLevel.PrePIOError);
                                    }
                                    else
                                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step2_CheckAlignmentValue;
                                }
                                else
                                {
                                    if (localData.LoadUnloadData.Loading /*|| localData.LoadUnloadData.Loading_LogicFlag*/)
                                    {
                                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step26_LoadUnloadEnd;
                                        SetAlarmCodeAndSetCommandErrorCode(
                                            EnumLoadUnloadControlErrorCode.取貨_LoadingOn異常,
                                            EnumLoadUnloadErrorLevel.PrePIOError);
                                    }
                                    else
                                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step2_CheckAlignmentValue;
                                }
                                break;
                            case EnumLoadUnload.Unload:
                                if (localData.AutoManual == EnumAutoState.Manual)
                                {
                                    if (!localData.LoadUnloadData.Loading)
                                    {
                                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step26_LoadUnloadEnd;
                                        SetAlarmCodeAndSetCommandErrorCode(
                                            EnumLoadUnloadControlErrorCode.放貨_LoadingOff異常,
                                            EnumLoadUnloadErrorLevel.PrePIOError);
                                    }
                                    else
                                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step2_CheckAlignmentValue;
                                }
                                else
                                {
                                    if (!localData.LoadUnloadData.Loading /*|| !localData.LoadUnloadData.Loading_LogicFlag*/)
                                    {
                                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step26_LoadUnloadEnd;
                                        SetAlarmCodeAndSetCommandErrorCode(
                                            EnumLoadUnloadControlErrorCode.放貨_LoadingOff異常,
                                            EnumLoadUnloadErrorLevel.PrePIOError);
                                    }
                                    else
                                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step2_CheckAlignmentValue;
                                }

                                break;
                            case EnumLoadUnload.ReadCSTID:
                                if (!localData.LoadUnloadData.Loading)
                                {
                                    command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step26_LoadUnloadEnd;
                                    SetAlarmCodeAndSetCommandErrorCode(
                                        EnumLoadUnloadControlErrorCode.放貨_LoadingOff異常,
                                        EnumLoadUnloadErrorLevel.PrePIOError);
                                }
                                else
                                    command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step4_確認Encoder補正範圍;
                                break;
                        }

                        if (command.StatusStep != (int)EnumUMTCLoadUnloadStatus.Step26_LoadUnloadEnd)
                        {
                            if (command.NeedPIO && !RightPIO.CanStartPIO)
                            {
                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step26_LoadUnloadEnd;
                                SetAlarmCodeAndSetCommandErrorCode(
                                    EnumLoadUnloadControlErrorCode.Port站ES或HO_AVBLNotOn,
                                    EnumLoadUnloadErrorLevel.PrePIOError);
                            }
                            else if (command.Action != EnumLoadUnload.ReadCSTID && !RightStageDataList.ContainsKey(command.StageNumber))
                            {
                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step26_LoadUnloadEnd;
                                SetAlarmCodeAndSetCommandErrorCode(
                                    EnumLoadUnloadControlErrorCode.取放貨站點資訊異常,
                                    EnumLoadUnloadErrorLevel.PrePIOError);
                            }
                        }

                        if (command.NeedPIO)
                            RightPIO.ResetPIO();
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step2_CheckAlignmentValue:
                        #region Step2_CheckAlignmentValue.
                        if (command.UsingAlignmentValue)
                        {
                            if (localData.TheMapInfo.AllAddress.ContainsKey(command.AddressID) &&
                                (localData.TheMapInfo.AllAddress[command.AddressID].LoadUnloadDirection != EnumStageDirection.None ||
                                 localData.TheMapInfo.AllAddress[command.AddressID].ChargingDirection != EnumStageDirection.None))
                                CheckAlingmentValueByAddressID(command.AddressID);
                            else
                                CheckAlingmentValue(command.StageDirection, command.StageNumber);

                            temp = localData.LoadUnloadData.AlignmentValue;

                            if (temp != null && temp.AlignmentVlaue)
                            {
                                alignmentCount = 0;
                                command.CommandStartAlignmentValue = temp;
                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step4_確認Encoder補正範圍;
                                WriteLog(7, "", String.Concat("AlignmentValue :: P = ", temp.P.ToString("0.0"),
                                                            ", Y = ", temp.Y.ToString("0.0"),
                                                            ", Theta = ", temp.Theta.ToString("0.0"),
                                                            ", Z = ", temp.Z.ToString("0.0")));
                            }
                            else
                            {
                                alignmentCount++;

                                if (alignmentCount >= maxAlignmentCount)
                                {
                                    command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step26_LoadUnloadEnd;
                                    SetAlarmCodeAndSetCommandErrorCode(
                                        EnumLoadUnloadControlErrorCode.AlignmentNG,
                                        EnumLoadUnloadErrorLevel.PrePIOError);
                                }
                            }
                        }
                        else
                        {
                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step4_確認Encoder補正範圍;
                        }
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step4_確認Encoder補正範圍:
                        #region Step3_確認Encoder補正範圍.
                        if (command.Action == EnumLoadUnload.ReadCSTID)
                        {
                            encoderAbs_Z = readCSTZEncoder;
                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step5_ServoOn;
                        }
                        else if (command.UsingAlignmentValue)
                        {
                            encoderAbs_P = RightStageDataList[command.StageNumber].Encoder_P + (command.CommandStartAlignmentValue.P / Math.Cos(command.CommandStartAlignmentValue.Theta * Math.PI / 180));
                            encoderAbs_Z = RightStageDataList[command.StageNumber].Encoder_Z + command.CommandStartAlignmentValue.Z + RightStageDataList[command.StageNumber].Benchmark_Z;
                            encoderAbs_Theta = RightStageDataList[command.StageNumber].Encoder_Theta - command.CommandStartAlignmentValue.Theta;
                            encoderAbs_Y = RightStageDataList[command.StageNumber].Encoder_Y + command.CommandStartAlignmentValue.Y;

                            WriteLog(7, "", String.Concat("Encoder P : ", encoderAbs_P.ToString("0.00"),
                                                          "Encoder Y : ", encoderAbs_Y.ToString("0.00"),
                                                          "Encoder Theta : ", encoderAbs_Theta.ToString("0.00"),
                                                          "Encoder Z : ", encoderAbs_Z.ToString("0.00")));

                            if (axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PosLimit >= encoderAbs_P &&
                                   encoderAbs_P >= axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].NagLimit &&
                                axisDataList[EnumLoadUnloadAxisName.Z軸.ToString()].PosLimit >= encoderAbs_Z &&
                                   encoderAbs_Z >= axisDataList[EnumLoadUnloadAxisName.Z軸.ToString()].NagLimit &&
                                axisDataList[EnumLoadUnloadAxisName.Theta軸.ToString()].PosLimit >= encoderAbs_Theta &&
                                   encoderAbs_Theta >= axisDataList[EnumLoadUnloadAxisName.Theta軸.ToString()].NagLimit &&
                                axisDataList[EnumLoadUnloadAxisName.Roller.ToString()].PosLimit >= encoderAbs_Y &&
                                   encoderAbs_Y >= axisDataList[EnumLoadUnloadAxisName.Roller.ToString()].NagLimit)
                            {
                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step5_ServoOn;
                            }
                            else
                            {
                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step26_LoadUnloadEnd;
                                SetAlarmCodeAndSetCommandErrorCode(
                                    EnumLoadUnloadControlErrorCode.AlignmentValueNG,
                                    EnumLoadUnloadErrorLevel.PrePIOError);
                            }
                        }
                        else
                        {
                            encoderAbs_Z = RightStageDataList[command.StageNumber].Encoder_Z;

                            if (encoderAbs_Z > readCSTZEncoder)
                                encoderAbs_Z = readCSTZEncoder;

                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step5_ServoOn;
                        }

                        switch (command.Action)
                        {
                            case EnumLoadUnload.Load:
                                encoderAbs_Z -= loadUnloadZOffset;
                                break;
                            case EnumLoadUnload.Unload:
                                encoderAbs_Z += loadUnloadZOffset;
                                break;
                        }
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step5_ServoOn:
                        #region Step4_ServoOn.
                        if (localData.MainFlowConfig.PowerSavingMode)
                        {
                            if (CheckAixsIsServoOn(EnumLoadUnloadAxisName.Z軸.ToString()) && CheckAixsIsServoOn(EnumLoadUnloadAxisName.Roller.ToString()))
                            {
                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step6_WaitServoOn;
                                timer.Restart();
                            }
                            else if (ServoOn(EnumLoadUnloadAxisName.Z軸.ToString(), false, 0) &&
                                     ServoOn(EnumLoadUnloadAxisName.Roller.ToString(), false, 0))
                            {
                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step6_WaitServoOn;
                                timer.Restart();
                            }
                            else
                            {
                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step26_LoadUnloadEnd;
                                SetAlarmCodeAndSetCommandErrorCode(
                                    EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail,
                                    EnumLoadUnloadErrorLevel.PrePIOError);
                            }
                        }
                        else
                        {
                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step6_WaitServoOn;
                            timer.Restart();
                        }
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step6_WaitServoOn:
                        #region Step5_WaitServoOn.
                        if (CheckAixsIsServoOn(EnumLoadUnloadAxisName.Z軸.ToString()) && CheckAixsIsServoOn(EnumLoadUnloadAxisName.Roller.ToString()))
                        {
                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step7_Busy_Z軸移動;
                            timer.Restart();

                            if (command.BreakenStopMode)
                                command.GoNext = false;
                        }
                        else if (timer.ElapsedMilliseconds > 3000)
                        {
                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step26_LoadUnloadEnd;
                            SetAlarmCodeAndSetCommandErrorCode(
                                EnumLoadUnloadControlErrorCode.ServoOnTimeout,
                                EnumLoadUnloadErrorLevel.PrePIOError);
                        }
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step7_Busy_Z軸移動:
                        #region Step6_Busy_Z軸移動.
                        if (!cv_Normal.Data)
                        {
                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                            SetAlarmCodeAndSetCommandErrorCode(
                                EnumLoadUnloadControlErrorCode.取放貨異常_Z軸升降中CVSensor異常,
                                EnumLoadUnloadErrorLevel.Error);
                        }
                        else if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                        {
                            if (CVPtoP(EnumLoadUnloadAxisName.Z軸.ToString(),
                                encoderAbs_Z,
                                command.SpeedPercent,
                                false, 0))
                            {
                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step8_Wait_等待Z軸移動完成;
                            }
                            else
                            {
                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                SetAlarmCodeAndSetCommandErrorCode(
                                    EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail,
                                    EnumLoadUnloadErrorLevel.Error);
                            }
                        }
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step8_Wait_等待Z軸移動完成:
                        #region Step7_Wait_等待Z軸移動完成.
                        if (!cv_Normal.Data)
                        {
                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                            SetAlarmCodeAndSetCommandErrorCode(
                                EnumLoadUnloadControlErrorCode.取放貨異常_Z軸升降中CVSensor異常,
                                EnumLoadUnloadErrorLevel.Error);
                        }
                        else if (lastStatus != nowStatus)
                        {
                            if (nowStatus == EnumSafetyLevel.Normal)
                            {
                                if (!CVPtoP(EnumLoadUnloadAxisName.Z軸.ToString(), encoderAbs_Z, command.SpeedPercent, false, 0))
                                {
                                    command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                    SetAlarmCodeAndSetCommandErrorCode(
                                        EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail,
                                        EnumLoadUnloadErrorLevel.Error);
                                }
                            }
                            else
                            {
                                if (!JogStop())
                                {
                                    command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                    SetAlarmCodeAndSetCommandErrorCode(
                                        EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail,
                                        EnumLoadUnloadErrorLevel.Error);
                                }
                            }
                        }
                        else
                        {
                            if (nowStatus == EnumSafetyLevel.Normal)
                            {
                                if (GetMoveStatusByCVAxisName(EnumLoadUnloadAxisName.Z軸.ToString()) == EnumAxisMoveStatus.Stop)
                                {
                                    if (Math.Abs(localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].Position -
                                                 encoderAbs_Z) <= inPositionRange[EnumLoadUnloadAxisName.Z軸.ToString()])
                                    {
                                        if (localData.MainFlowConfig.CheckPassLineSensor && command.Action != EnumLoadUnload.ReadCSTID &&
                                            !AllAxisSensorData[EnumLoadUnloadAxisName.Z軸.ToString()][Z軸上位].Data)
                                        {
                                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                            SetAlarmCodeAndSetCommandErrorCode(
                                                EnumLoadUnloadControlErrorCode.Z軸上定位未On,
                                                EnumLoadUnloadErrorLevel.Error);
                                        }
                                        else
                                            localData.LoadUnloadData.LoadUnloadCommand.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step9_PIO_PIOStart;
                                    }
                                    else
                                    {
                                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                        SetAlarmCodeAndSetCommandErrorCode(
                                            EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange,
                                            EnumLoadUnloadErrorLevel.Error);
                                    }
                                }
                            }
                        }
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step9_PIO_PIOStart:
                        #region Step8_PIO_PIOStart.
                        if (command.NeedPIO)
                            RightPIO.PIOFlow_Load_UnLoad(command.Action);

                        if (command.BreakenStopMode)
                            command.GoNext = false;

                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step10_Busy_P軸Theta軸補正;
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step10_Busy_P軸Theta軸補正:
                        #region Step9_Busy_P軸Theta軸補正.
                        if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                        {
                            if (CVPtoP(EnumLoadUnloadAxisName.P軸.ToString(), encoderAbs_P, command.SpeedPercent, false, 0) &&
                                CVPtoP(EnumLoadUnloadAxisName.Theta軸.ToString(), encoderAbs_Theta, command.SpeedPercent, false, 0))
                            {
                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step11_Wait_等待P軸Theta軸補正完成;
                            }
                            else
                            {
                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                SetAlarmCodeAndSetCommandErrorCode(
                                    EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail,
                                    EnumLoadUnloadErrorLevel.Error);
                            }
                        }
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step11_Wait_等待P軸Theta軸補正完成:
                        #region Step10_Wait_等待P軸Theta軸補正完成.
                        if (lastStatus != nowStatus)
                        {
                            if (nowStatus == EnumSafetyLevel.Normal)
                            {
                                if (!CVPtoP(EnumLoadUnloadAxisName.P軸.ToString(), encoderAbs_P, command.SpeedPercent, false, 0) ||
                                    !CVPtoP(EnumLoadUnloadAxisName.Theta軸.ToString(), encoderAbs_Theta, command.SpeedPercent, false, 0))
                                {
                                    command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                    SetAlarmCodeAndSetCommandErrorCode(
                                        EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail,
                                        EnumLoadUnloadErrorLevel.Error);
                                }
                            }
                            else
                            {
                                if (!JogStop())
                                {
                                    command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                    SetAlarmCodeAndSetCommandErrorCode(
                                        EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail,
                                        EnumLoadUnloadErrorLevel.Error);
                                }
                            }
                        }
                        else
                        {
                            if (nowStatus == EnumSafetyLevel.Normal)
                            {
                                if (GetMoveStatusByCVAxisName(EnumLoadUnloadAxisName.P軸.ToString()) == EnumAxisMoveStatus.Stop &&
                                    GetMoveStatusByCVAxisName(EnumLoadUnloadAxisName.Theta軸.ToString()) == EnumAxisMoveStatus.Stop)
                                {
                                    if (Math.Abs(localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position -
                                                 encoderAbs_P) <= inPositionRange[EnumLoadUnloadAxisName.P軸.ToString()] &&
                                        Math.Abs(localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].Position -
                                                 encoderAbs_Theta) <= inPositionRange[EnumLoadUnloadAxisName.Theta軸.ToString()])
                                    {
                                        if (command.Action == EnumLoadUnload.ReadCSTID)
                                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step19_ReadCSTID;
                                        else
                                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step12_PIO_WaitReady;
                                    }
                                    else
                                    {
                                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                        SetAlarmCodeAndSetCommandErrorCode(
                                            EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange,
                                            EnumLoadUnloadErrorLevel.Error);
                                    }
                                }
                            }
                        }
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step12_PIO_WaitReady:
                        #region Step9_PIO_WaitReady.
                        if (command.NeedPIO)
                        {
                            if (RightPIO.Status == EnumPIOStatus.TP2)
                            {
                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step13_Busy_RollerStart;

                                if (command.BreakenStopMode)
                                    command.WaitFlag = true;
                            }
                            else
                            {
                                if (RightPIO.Timeout != EnumPIOStatus.None)
                                {
                                    command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step20_Busy_P軸Theta軸回Home;
                                    SetAlarmCodeAndSetCommandErrorCode(
                                        GetPIOAlarmCode(),
                                        EnumLoadUnloadErrorLevel.PrePIOError);

                                    if (command.BreakenStopMode)
                                        command.GoNext = false;
                                }
                            }
                        }
                        else
                        {
                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step13_Busy_RollerStart;

                            if (command.BreakenStopMode)
                                command.GoNext = false;
                        }
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step13_Busy_RollerStart:
                        #region Step12_Busy_RollerStart.
                        if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                        {
                            if (command.NeedPIO)
                                ((PIOFlow_UMTC_LoadUnloadSemi)RightPIO).SendBusy = true;

                            if (!localData.LoadUnloadData.NotForkBusyAction)
                            {
                                switch (command.Action)
                                {
                                    case EnumLoadUnload.Load:
                                        if (AxisJog(EnumLoadUnloadAxisName.Roller.ToString(), true, 1))
                                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step14_Busy_Roller_Sensor1;
                                        break;
                                    case EnumLoadUnload.Unload:
                                        //Thread.Sleep(3000);

                                        if (AxisJog(EnumLoadUnloadAxisName.Roller.ToString(), false, 1))
                                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step14_Busy_Roller_Sensor1;
                                        break;
                                }
                            }
                        }

                        if (command.NeedPIO && RightPIO.Timeout != EnumPIOStatus.None)
                        {
                            SetAlarmCodeAndSetCommandErrorCode(
                                GetPIOAlarmCode(),
                                EnumLoadUnloadErrorLevel.Error);
                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                        }
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step14_Busy_Roller_Sensor1:
                        #region Step13_Busy_Roller_Sensor1.
                        switch (command.Action)
                        {
                            case EnumLoadUnload.Load:
                                if (AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV入料].Data)
                                    command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step15_Busy_Roller_Sensor2;
                                else if (AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV減速].Data ||
                                         AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV停止].Data)
                                {
                                    SetAlarmCode(EnumLoadUnloadControlErrorCode.取放貨中CV檢知異常);
                                    command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step15_Busy_Roller_Sensor2;
                                }
                                break;
                            case EnumLoadUnload.Unload:
                                if (!AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV停止].Data)
                                    command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step15_Busy_Roller_Sensor2;
                                else if (!AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV減速].Data)
                                {
                                    SetAlarmCode(EnumLoadUnloadControlErrorCode.取放貨中CV檢知異常);
                                    command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step15_Busy_Roller_Sensor2;
                                }
                                break;
                            default:
                                WriteLog(7, "", "Precheck 未實作");
                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                break;
                        }

                        if (command.NeedPIO && RightPIO.Timeout != EnumPIOStatus.None)
                        {
                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                            SetAlarmCodeAndSetCommandErrorCode(
                                GetPIOAlarmCode(),
                                EnumLoadUnloadErrorLevel.Error);
                        }
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step15_Busy_Roller_Sensor2:
                        #region Step14_Busy_Roller_Sensor2.
                        switch (command.Action)
                        {
                            case EnumLoadUnload.Load:
                                if (AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV減速].Data)
                                {
                                    if (AxisJog(EnumLoadUnloadAxisName.Roller.ToString(), true, axisDataList[EnumLoadUnloadAxisName.Roller.ToString()].HomeVelocity_High))
                                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step16_Busy_Roller_Sensor3;
                                    else
                                    {
                                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                        SetAlarmCodeAndSetCommandErrorCode(
                                            EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗,
                                            EnumLoadUnloadErrorLevel.Error);
                                    }
                                }
                                else if (AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV停止].Data)
                                {
                                    SetAlarmCode(EnumLoadUnloadControlErrorCode.取放貨中CV檢知異常);

                                    if (AxisJog(EnumLoadUnloadAxisName.Roller.ToString(), true, axisDataList[EnumLoadUnloadAxisName.Roller.ToString()].HomeVelocity_High))
                                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step16_Busy_Roller_Sensor3;
                                    else
                                    {
                                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                        SetAlarmCodeAndSetCommandErrorCode(
                                            EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗,
                                            EnumLoadUnloadErrorLevel.Error);
                                    }
                                }
                                break;
                            case EnumLoadUnload.Unload:
                                if (!AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV減速].Data)
                                    command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step16_Busy_Roller_Sensor3;
                                else if (AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV停止].Data)
                                    SetAlarmCode(EnumLoadUnloadControlErrorCode.取放貨中CV檢知異常);
                                break;
                            default:
                                WriteLog(7, "", "Precheck 未實作");
                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                break;
                        }

                        if (command.NeedPIO && RightPIO.Timeout != EnumPIOStatus.None)
                        {
                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                            SetAlarmCodeAndSetCommandErrorCode(
                                GetPIOAlarmCode(),
                                EnumLoadUnloadErrorLevel.Error);
                        }
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step16_Busy_Roller_Sensor3:
                        #region Step15_Busy_Roller_Sensor3.
                        switch (command.Action)
                        {
                            case EnumLoadUnload.Load:
                                if (AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV停止].Data)
                                {
                                    Thread.Sleep(500);

                                    if (JogStop())
                                    {
                                        if (AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV減速].Data)
                                        {
                                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step17_Wait_Roller_Stop;
                                            timer.Restart();
                                        }
                                        else
                                        {
                                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                            SetAlarmCodeAndSetCommandErrorCode(
                                                EnumLoadUnloadControlErrorCode.RollerStop後CV訊號異常,
                                                EnumLoadUnloadErrorLevel.Error);
                                        }
                                    }
                                }
                                else if (!AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV減速].Data)
                                    SetAlarmCode(EnumLoadUnloadControlErrorCode.取放貨中CV檢知異常);
                                break;
                            case EnumLoadUnload.Unload:
                                if (!AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV入料].Data)
                                {
                                    if (JogStop())
                                    {
                                        if (!AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV減速].Data &&
                                            !AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV停止].Data)
                                        {
                                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step17_Wait_Roller_Stop;
                                            timer.Restart();
                                        }
                                        else
                                        {
                                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                            SetAlarmCodeAndSetCommandErrorCode(
                                                EnumLoadUnloadControlErrorCode.RollerStop後CV訊號異常,
                                                EnumLoadUnloadErrorLevel.Error);
                                        }
                                    }
                                }
                                else if (AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV減速].Data ||
                                         AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV停止].Data)
                                    SetAlarmCode(EnumLoadUnloadControlErrorCode.取放貨中CV檢知異常);

                                break;
                        }

                        if (command.NeedPIO && RightPIO.Timeout != EnumPIOStatus.None)
                        {
                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                            SetAlarmCodeAndSetCommandErrorCode(
                                GetPIOAlarmCode(),
                                EnumLoadUnloadErrorLevel.Error);
                        }
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step17_Wait_Roller_Stop:
                        #region Step16_Wait_Roller_Stop.
                        if (command.NeedPIO)
                        {
                            if (GetMoveStatusByCVAxisName(EnumLoadUnloadAxisName.Roller.ToString()) == EnumAxisMoveStatus.Stop)
                            {
                                if (RightPIO.Status == EnumPIOStatus.TP4)
                                {
                                    command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step18_PIO_PIOContinue;
                                }
                                else if (RightPIO.Status == EnumPIOStatus.NG || RightPIO.Status == EnumPIOStatus.Error ||
                                        RightPIO.Status == EnumPIOStatus.SendAllOff || RightPIO.Status == EnumPIOStatus.WaitAllOff)
                                {
                                    command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                    SetAlarmCodeAndSetCommandErrorCode(
                                        GetPIOAlarmCode(),
                                        EnumLoadUnloadErrorLevel.Error);
                                }
                            }
                            else
                            {
                                if (timer.ElapsedMilliseconds > waitRollerStopTimeout)
                                {
                                    timer.Stop();
                                    command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                    SetAlarmCodeAndSetCommandErrorCode(
                                        EnumLoadUnloadControlErrorCode.RollerStopTimeout,
                                        EnumLoadUnloadErrorLevel.Error);
                                }
                            }
                        }
                        else
                        {
                            if (GetMoveStatusByCVAxisName(EnumLoadUnloadAxisName.Roller.ToString()) == EnumAxisMoveStatus.Stop)
                            {
                                timer.Stop();
                                Thread.Sleep(3000);
                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step18_PIO_PIOContinue;
                            }
                            else
                            {
                                if (timer.ElapsedMilliseconds > waitRollerStopTimeout)
                                {
                                    timer.Stop();
                                    command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                    SetAlarmCodeAndSetCommandErrorCode(
                                        EnumLoadUnloadControlErrorCode.RollerStopTimeout,
                                        EnumLoadUnloadErrorLevel.Error);
                                }
                            }
                        }
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step18_PIO_PIOContinue:
                        #region Step17_PIO_PIOContinue.
                        if (command.NeedPIO)
                        {
                            if (RightPIO.Status == EnumPIOStatus.TP4)
                            {
                                ((PIOFlow_UMTC_LoadUnloadSemi)RightPIO).SendComplete = true;
                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step19_ReadCSTID;
                            }
                            else if (RightPIO.Timeout != EnumPIOStatus.None)
                            {
                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                SetAlarmCodeAndSetCommandErrorCode(
                                    GetPIOAlarmCode(),
                                    EnumLoadUnloadErrorLevel.Error);
                            }
                        }
                        else
                        {
                            Thread.Sleep(3000);
                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step19_ReadCSTID;
                        }
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step19_ReadCSTID:
                        #region Step18_ReadCSTID.
                        UpdateLoadingAndCSTID();

                        if (command.BreakenStopMode)
                            command.GoNext = false;

                        localData.LoadUnloadData.LoadUnloadCommand.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step20_Busy_P軸Theta軸回Home;
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step20_Busy_P軸Theta軸回Home:
                        #region Step19_Busy_P軸Theta軸回Home.
                        if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                        {
                            if (CVPtoP(EnumLoadUnloadAxisName.P軸.ToString(), 0, command.SpeedPercent, false, 0) &&
                                CVPtoP(EnumLoadUnloadAxisName.Theta軸.ToString(), 0, command.SpeedPercent, false, 0))
                            {
                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step21_Wait_等待P軸Theta軸回Home完成;
                            }
                            else
                            {
                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                SetAlarmCodeAndSetCommandErrorCode(
                                    EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail,
                                    EnumLoadUnloadErrorLevel.Error);
                            }
                        }
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step21_Wait_等待P軸Theta軸回Home完成:
                        #region Step20_Wait_等待P軸Theta軸回Home完成.
                        if (lastStatus != nowStatus)
                        {
                            if (nowStatus == EnumSafetyLevel.Normal)
                            {
                                if (!CVPtoP(EnumLoadUnloadAxisName.P軸.ToString(), 0, command.SpeedPercent, false, 0) ||
                                    !CVPtoP(EnumLoadUnloadAxisName.Theta軸.ToString(), 0, command.SpeedPercent, false, 0))
                                {
                                    command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                    SetAlarmCodeAndSetCommandErrorCode(
                                        EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail,
                                        EnumLoadUnloadErrorLevel.Error);
                                }
                            }
                            else
                            {
                                if (!JogStop())
                                {
                                    command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                    SetAlarmCodeAndSetCommandErrorCode(
                                        EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail,
                                        EnumLoadUnloadErrorLevel.Error);
                                }
                            }
                        }
                        else
                        {
                            if (nowStatus == EnumSafetyLevel.Normal)
                            {
                                if (GetMoveStatusByCVAxisName(EnumLoadUnloadAxisName.P軸.ToString()) == EnumAxisMoveStatus.Stop &&
                                    GetMoveStatusByCVAxisName(EnumLoadUnloadAxisName.Theta軸.ToString()) == EnumAxisMoveStatus.Stop)
                                {
                                    if (Math.Abs(localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position) <= inPositionRange[EnumLoadUnloadAxisName.P軸.ToString()] &&
                                        Math.Abs(localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].Position) <= inPositionRange[EnumLoadUnloadAxisName.Theta軸.ToString()])
                                    {
                                        if (AllAxisSensorData[EnumLoadUnloadAxisName.P軸.ToString()][axisHomeSensor[EnumLoadUnloadAxisName.P軸.ToString()]].Data &&
                                            AllAxisSensorData[EnumLoadUnloadAxisName.Theta軸.ToString()][axisHomeSensor[EnumLoadUnloadAxisName.Theta軸.ToString()]].Data)
                                        {
                                            if (command.PIOResult == EnumPIOStatus.None)
                                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step22_PIO_WaitEnd;
                                            else
                                            {
                                                WriteLog(5, "", "前PIO異常,跳過PIO Continue");
                                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step23_Busy_Z軸回Home;
                                            }
                                        }
                                        else
                                        {
                                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                            SetAlarmCodeAndSetCommandErrorCode(
                                                EnumLoadUnloadControlErrorCode.HomeSensor未On,
                                                EnumLoadUnloadErrorLevel.Error);
                                        }
                                    }
                                    else
                                    {
                                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                        SetAlarmCodeAndSetCommandErrorCode(
                                            EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange,
                                            EnumLoadUnloadErrorLevel.Error);
                                    }
                                }
                            }
                        }
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step22_PIO_WaitEnd:
                        #region Step21_PIO_WaitEnd.
                        if (command.NeedPIO)
                        {
                            if (command.BreakenStopMode)
                                command.GoNext = false;

                            if (RightPIO.Status == EnumPIOStatus.Complete)
                            {
                                switch (command.Action)
                                {
                                    case EnumLoadUnload.Load:
                                        SendLoadCompleteEventToMiddler();
                                        break;
                                    case EnumLoadUnload.Unload:
                                        SendUnloadCompleteEventToMiddler();
                                        break;
                                }

                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step23_Busy_Z軸回Home;

                                if (command.BreakenStopMode)
                                    command.GoNext = false;
                            }
                            else if (RightPIO.Status == EnumPIOStatus.NG)
                            {
                                /// 後PIO 當作命令完成但是會發timeout.
                                if (alarmHandler.AlarmCodeTable.ContainsKey((int)GetPIOAlarmCode()) &&
                                    alarmHandler.AlarmCodeTable[(int)GetPIOAlarmCode()].Level == EnumAlarmLevel.Warn)
                                {
                                    switch (command.Action)
                                    {
                                        case EnumLoadUnload.Load:
                                            SendLoadCompleteEventToMiddler();
                                            break;
                                        case EnumLoadUnload.Unload:
                                            SendUnloadCompleteEventToMiddler();
                                            break;
                                    }

                                    SetAlarmCodeAndSetCommandErrorCode(
                                        GetPIOAlarmCode(),
                                        EnumLoadUnloadErrorLevel.AfterPIOError);
                                }

                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step23_Busy_Z軸回Home;
                            }
                        }
                        else
                        {
                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step23_Busy_Z軸回Home;

                            if (command.BreakenStopMode)
                                command.GoNext = false;
                        }
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step23_Busy_Z軸回Home:
                        #region Step22_Busy_Z軸回Home.
                        if (!cv_Normal.Data)
                        {
                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                            SetAlarmCodeAndSetCommandErrorCode(
                                EnumLoadUnloadControlErrorCode.取放貨異常_Z軸升降中CVSensor異常,
                                EnumLoadUnloadErrorLevel.Error);
                        }
                        else if (CanZAxisStopInPassLine())
                        {
                            WriteLog(7, "", "開啟上定位可當Home模式且符合條件(車上無CST且上定位On)");
                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step25_後CheckAlignmentValue;
                        }
                        else if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                        {
                            if (CVPtoP(EnumLoadUnloadAxisName.Z軸.ToString(), 0, command.SpeedPercent, false, 0))
                            {
                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step24_Wait_等待Z軸回Home完成;
                            }
                            else
                            {
                                command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                SetAlarmCodeAndSetCommandErrorCode(
                                    EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail,
                                    EnumLoadUnloadErrorLevel.Error);
                            }
                        }
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step24_Wait_等待Z軸回Home完成:
                        #region Step23_Wait_等待Z軸回Home完成.
                        if (!cv_Normal.Data)
                        {
                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                            SetAlarmCodeAndSetCommandErrorCode(
                                EnumLoadUnloadControlErrorCode.取放貨異常_Z軸升降中CVSensor異常,
                                EnumLoadUnloadErrorLevel.Error);
                        }
                        else if (lastStatus != nowStatus)
                        {
                            if (nowStatus == EnumSafetyLevel.Normal)
                            {
                                if (!CVPtoP(EnumLoadUnloadAxisName.Z軸.ToString(), 0, command.SpeedPercent, false, 0))
                                {
                                    command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                    SetAlarmCodeAndSetCommandErrorCode(
                                        EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail,
                                        EnumLoadUnloadErrorLevel.Error);
                                }
                            }
                            else
                            {
                                if (!JogStop())
                                {
                                    command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                    SetAlarmCodeAndSetCommandErrorCode(
                                        EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail,
                                        EnumLoadUnloadErrorLevel.Error);
                                }
                            }
                        }
                        else
                        {
                            if (nowStatus == EnumSafetyLevel.Normal)
                            {
                                if (GetMoveStatusByCVAxisName(EnumLoadUnloadAxisName.Z軸.ToString()) == EnumAxisMoveStatus.Stop)
                                {
                                    if (Math.Abs(localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].Position) <= inPositionRange[EnumLoadUnloadAxisName.Z軸.ToString()])
                                    {

                                        if (AllAxisSensorData[EnumLoadUnloadAxisName.P軸.ToString()][axisHomeSensor[EnumLoadUnloadAxisName.P軸.ToString()]].Data &&
                                            AllAxisSensorData[EnumLoadUnloadAxisName.Theta軸.ToString()][axisHomeSensor[EnumLoadUnloadAxisName.Theta軸.ToString()]].Data &&
                                            AllAxisSensorData[EnumLoadUnloadAxisName.Z軸.ToString()][axisHomeSensor[EnumLoadUnloadAxisName.Z軸.ToString()]].Data)
                                        {
                                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step25_後CheckAlignmentValue;
                                        }
                                        else
                                        {
                                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                            SetAlarmCodeAndSetCommandErrorCode(
                                                EnumLoadUnloadControlErrorCode.HomeSensor未On,
                                                EnumLoadUnloadErrorLevel.Error);
                                        }
                                    }
                                    else
                                    {
                                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                                        SetAlarmCodeAndSetCommandErrorCode(
                                            EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange,
                                            EnumLoadUnloadErrorLevel.Error);
                                    }
                                }
                            }
                        }
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step25_後CheckAlignmentValue:
                        #region Step25_後CheckAlignmentValue
                        if (localData.LoadUnloadData.LoadUnloadCommand.UsingAlignmentValue)
                        {
                            CheckAlingmentValueByAddressID(command.AddressID);
                            localData.LoadUnloadData.LoadUnloadCommand.CommandEndAlignmentValue = localData.LoadUnloadData.AlignmentValue;

                            if (localData.LoadUnloadData.LoadUnloadCommand.CommandEndAlignmentValue != null && localData.LoadUnloadData.LoadUnloadCommand.CommandEndAlignmentValue.AlignmentVlaue)
                                WriteLog(7, "", String.Concat("補正數值(後) : P = ", localData.LoadUnloadData.LoadUnloadCommand.CommandEndAlignmentValue.P.ToString("0.00"),
                                                                           ", Y = ", localData.LoadUnloadData.LoadUnloadCommand.CommandEndAlignmentValue.Y.ToString("0.00"),
                                                                           ", Theta = ", localData.LoadUnloadData.LoadUnloadCommand.CommandEndAlignmentValue.Theta.ToString("0.00"),
                                                                           ", Z = ", localData.LoadUnloadData.LoadUnloadCommand.CommandEndAlignmentValue.Z.ToString("0.00")));
                            else
                                WriteLog(3, "", "後補正無資料");
                        }

                        localData.LoadUnloadData.LoadUnloadCommand.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step26_LoadUnloadEnd;
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.Step26_LoadUnloadEnd:
                        #region Step26_LoadUnloadEnd.
                        command.CommandEndTime = DateTime.Now;
                        command.EndSOC = localData.BatteryInfo.Battery_SOC;
                        command.EndVoltage = localData.BatteryInfo.Battery_V;

                        switch (command.ErrorLevel)
                        {
                            case EnumLoadUnloadErrorLevel.None:
                                command.CommandResult = EnumLoadUnloadComplete.End;
                                break;
                            case EnumLoadUnloadErrorLevel.PrePIOError:
                                command.CommandResult = EnumLoadUnloadComplete.Interlock;
                                break;
                            case EnumLoadUnloadErrorLevel.AfterPIOError:
                                command.CommandResult = EnumLoadUnloadComplete.End;
                                break;
                            case EnumLoadUnloadErrorLevel.AfterPIOErrorAndActionError:
                                command.CommandResult = EnumLoadUnloadComplete.End;
                                break;
                            case EnumLoadUnloadErrorLevel.Error:
                                command.CommandResult = EnumLoadUnloadComplete.Error;
                                break;
                        }

                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step0_Idle;
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop:
                        #region ErrorStep1_Busy_AllStop.
                        JogStop();

                        if (command.NeedPIO)
                            RightPIO.StopPIO();

                        errorDelayTimer.Restart();
                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep2_Wait_TwoSecond;
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.ErrorStep2_Wait_TwoSecond:
                        #region ErrorStep2_Wait_TwoSecond.
                        if (errorDelayTimer.ElapsedMilliseconds >= 2000)
                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep3_Wait_AllStop;
                        #endregion
                        break;
                    case (int)EnumUMTCLoadUnloadStatus.ErrorStep3_Wait_AllStop:
                        #region ErrorStep3_Wait_AllStop.
                        if (GetMoveStatusByCVAxisName(EnumLoadUnloadAxisName.Z軸.ToString()) == EnumAxisMoveStatus.Stop &&
                            GetMoveStatusByCVAxisName(EnumLoadUnloadAxisName.P軸.ToString()) == EnumAxisMoveStatus.Stop &&
                            GetMoveStatusByCVAxisName(EnumLoadUnloadAxisName.Roller.ToString()) == EnumAxisMoveStatus.Stop &&
                            GetMoveStatusByCVAxisName(EnumLoadUnloadAxisName.Theta軸.ToString()) == EnumAxisMoveStatus.Stop)
                        {
                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step26_LoadUnloadEnd;
                        }
                        else if (errorDelayTimer.ElapsedMilliseconds > 5000)
                        {
                            WriteLog(5, "", "停不下來, 斷動力店");
                            mipcControl.SetMIPCReady(false);
                            SetAlarmCode(EnumLoadUnloadControlErrorCode.RollerStopTimeout);
                            command.StatusStep = (int)EnumUMTCLoadUnloadStatus.Step26_LoadUnloadEnd;
                        }
                        #endregion
                        break;
                }

                lastStatus = nowStatus;

                #region StopRequest.
                if (command.StatusStep != (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop &&
                    command.StatusStep != (int)EnumUMTCLoadUnloadStatus.ErrorStep2_Wait_TwoSecond &&
                    command.StatusStep != (int)EnumUMTCLoadUnloadStatus.ErrorStep3_Wait_AllStop &&
                    command.StatusStep != (int)EnumUMTCLoadUnloadStatus.Step26_LoadUnloadEnd &&
                    command.StatusStep != (int)EnumUMTCLoadUnloadStatus.Step0_Idle)
                {
                    if (localData.LoadUnloadData.LoadUnloadCommand.StopRequest || nowStatus == EnumSafetyLevel.EMO)
                    {
                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                        SetAlarmCodeAndSetCommandErrorCode(
                            EnumLoadUnloadControlErrorCode.取放貨中EMS,
                            EnumLoadUnloadErrorLevel.Error);
                    }
                    else if (!localData.MIPCData.U動力電)
                    {
                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                        SetAlarmCodeAndSetCommandErrorCode(
                            EnumLoadUnloadControlErrorCode.取放中安全迴路異常,
                            EnumLoadUnloadErrorLevel.Error);
                    }
                    else if (AllAxisSensorData[EnumLoadUnloadAxisName.P軸.ToString()][axisPosLimitSensor[EnumLoadUnloadAxisName.P軸.ToString()]].Data ||
                             AllAxisSensorData[EnumLoadUnloadAxisName.P軸.ToString()][axisNagLimitSensor[EnumLoadUnloadAxisName.P軸.ToString()]].Data ||
                             AllAxisSensorData[EnumLoadUnloadAxisName.Theta軸.ToString()][axisPosLimitSensor[EnumLoadUnloadAxisName.Theta軸.ToString()]].Data ||
                             AllAxisSensorData[EnumLoadUnloadAxisName.Theta軸.ToString()][axisNagLimitSensor[EnumLoadUnloadAxisName.Theta軸.ToString()]].Data ||
                             AllAxisSensorData[EnumLoadUnloadAxisName.Z軸.ToString()][axisPosLimitSensor[EnumLoadUnloadAxisName.Z軸.ToString()]].Data ||
                             AllAxisSensorData[EnumLoadUnloadAxisName.Z軸.ToString()][axisNagLimitSensor[EnumLoadUnloadAxisName.Z軸.ToString()]].Data)
                    {
                        command.StatusStep = (int)EnumUMTCLoadUnloadStatus.ErrorStep1_Busy_AllStop;
                        SetAlarmCodeAndSetCommandErrorCode(
                            EnumLoadUnloadControlErrorCode.取放中極限觸發,
                            EnumLoadUnloadErrorLevel.Error);
                    }
                }
                #endregion

                if (command.StepString != ((EnumUMTCLoadUnloadStatus)command.StatusStep).ToString())
                {
                    if (logMode)
                        WriteLog(7, "", String.Concat("Step Change to : ", ((EnumUMTCLoadUnloadStatus)command.StatusStep).ToString()));

                    command.StepString = ((EnumUMTCLoadUnloadStatus)command.StatusStep).ToString();
                }
                else
                    Thread.Sleep(sleepTime);
            }

            if (localData.MainFlowConfig.PowerSavingMode)
            {
                if (CheckAixsIsServoOn(EnumLoadUnloadAxisName.Z軸.ToString()) ||
                    CheckAixsIsServoOn(EnumLoadUnloadAxisName.Roller.ToString()))
                {
                    ServoOff(EnumLoadUnloadAxisName.Z軸.ToString(), (localData.AutoManual == EnumAutoState.Auto ? false : true), 1000);
                    ServoOff(EnumLoadUnloadAxisName.Roller.ToString(), (localData.AutoManual == EnumAutoState.Auto ? false : true), 1000);
                }
            }

            command.PIOHistory = RightPIO.PIOHistory;
        }

        public override bool ClearCommand()
        {
            RightPIO.ResetPIO();
            LoadUnloadCommandData temp = localData.LoadUnloadData.LoadUnloadCommand;

            if (temp == null)
                return true;
            else
            {
                if (temp.StatusStep == (int)EnumUMTCLoadUnloadStatus.Step0_Idle)
                {
                    localData.LoadUnloadData.LoadUnloadCommand = null;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        #region AlignmentValue.
        private int lastRightStageNumber = -1;
        private string sr1000_R_Mode = "";

        private void GetBarcodeXAndY(EnumStageDirection direction, int stageNumber, ref string barcodeID,
                                     ref double barcodeX, ref double barcodeY, ref double originX, ref double originY,
                                     double laser_F, double laser_B)
        {
            double laser = 0;

            if (laser_F != 0 && laser_B != 0)
                laser = (laser_F + laser_B) / 2;

            string barcodeString = "";
            string errorMessage = "";

            if (sr1000_R.ReadBarcode(ref barcodeString, 200, ref errorMessage))
            {
                string[] splitResult = Regex.Split(barcodeString, "[: / ,]+", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));

                if (splitResult.Length == 4)
                {
                    int barcodeLocateStageNumber = -1;

                    barcodeID = splitResult[0];
                    originX = double.Parse(splitResult[1]);
                    originY = double.Parse(splitResult[2]);

                    if (Int32.Parse(Regex.Replace(splitResult[3], "[^0-9]", "")) < 200)
                    {
                        barcodeLocateStageNumber = stageNumber;

                        if (RightStageBarocdeReaderSetting.ContainsKey(barcodeLocateStageNumber) &&
                            BarcodeReaderIDToRealDistance.ContainsKey(barcodeID))
                        {
                            double basedY = alignmentDeviceOffset.BarcodeReader_Right.Y;
                            double basedX = alignmentDeviceOffset.BarcodeReader_Right.X;

                            if (laser != 0)
                            {
                                basedX += (laser - 180) / 200 * alignmentDeviceOffset.BarcodeReader_Right_X_Delta;
                                basedY += (laser - 180) / 200 * alignmentDeviceOffset.BarcodeReader_Right_Y_Delta;
                            }

                            barcodeY = (double.Parse(splitResult[2]) - basedY) / RightStageBarocdeReaderSetting[stageNumber].PixelToMM_Y;

                            barcodeX = (double.Parse(splitResult[1]) - basedX) / RightStageBarocdeReaderSetting[stageNumber].PixelToMM_X +
                                       BarcodeReaderIDToRealDistance[barcodeID];

                            barcodeY += alignmentDeviceOffset.BarcodeReader_Right_Z;
                        }
                        else
                        {
                            barcodeID = "";
                            WriteLog(5, "", String.Concat("stageNumber : ", barcodeLocateStageNumber.ToString("0"), ", 找不到BarocdeID : ", barcodeID));
                        }
                    }
                    else
                    {
                        barcodeID = "";
                        WriteLog(5, "", String.Concat("Scant Time > 100 不使用資料"));
                    }
                }
                else
                    WriteLog(5, "", String.Concat("SR1000 Split.Length != 4, data : ", barcodeString, ", Length : ", splitResult.Length.ToString("0")));
            }
            else
            {
                if (sr1000_R.Error)
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.取放貨補正元件_BarcoderReader斷線);

                sr1000_R.StopReadBarcode(ref barcodeString, ref errorMessage);
                WriteLog(5, "", String.Concat("SR1000 Read Fail : ", errorMessage));
            }
        }

        private void GetLaserValue(EnumStageDirection direction, ref double laser_F, ref double laser_B,
                                   ref double laser_F_Origin, ref double laser_B_Origin, int stageNumber)
        {
            string data = "";

            if (distanceSensor_R.GetDistanceSensorData(ref data))
            {
                string[] splitResult = Regex.Split(data, ",", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));

                if (splitResult.Length == 5)
                {
                    double mag = (RightStageDataList[stageNumber].Benchmark_Y - 180) / 200;

                    if (splitResult[1] == "02" || splitResult[1] == "01" || splitResult[1] == "04")
                    {
                        laser_B_Origin = 300 - (double.Parse(splitResult[2]) / 100);
                        laser_B = laser_B_Origin - alignmentDeviceOffset.LaserB_Right - alignmentDeviceOffset.LaserB_Right_Offset * mag;
                    }

                    if (splitResult[3] == "02" || splitResult[3] == "01" || splitResult[3] == "04")
                    {
                        laser_F_Origin = 300 - (double.Parse(splitResult[4]) / 100);
                        laser_F = laser_F_Origin - alignmentDeviceOffset.LaserF_Right - alignmentDeviceOffset.LaserF_Right_Offset * mag;
                    }
                }
            }
        }

        private void GetAlignmentValue(EnumStageDirection direction, int stageNumber)
        {
            AlignmentValueData temp = new AlignmentValueData();
            double barcodeX = 0;
            double barcodeY = 0;
            string barcodeID = "";
            double laser_F = 0;
            double laser_B = 0;
            double originX = 0;
            double originY = 0;
            double laser_F_Origin = 0;
            double laser_B_Origin = 0;

            GetLaserValue(direction, ref laser_F, ref laser_B, ref laser_F_Origin, ref laser_B_Origin, stageNumber);
            GetBarcodeXAndY(direction, stageNumber, ref barcodeID, ref barcodeX, ref barcodeY, ref originX, ref originY, laser_F, laser_B);

            temp.LaserF = laser_F;
            temp.LaserB = laser_B;
            temp.LaserF_Origin = laser_F_Origin;
            temp.LaserB_Origin = laser_B_Origin;
            temp.BarcodePosition = new MapPosition(barcodeX, barcodeY);
            temp.OriginBarodePosition = new MapPosition(originX, originY);
            temp.BarcodeNumber = barcodeID;
            temp.Direction = direction;
            temp.StageNumber = stageNumber;

            if (barcodeID != "" && laser_F != 0 && laser_B != 0)
            {
                double theta = Math.Atan((laser_B - laser_F) /
                               Math.Abs(alignmentDeviceOffset.LaserB_Right_Locate - alignmentDeviceOffset.LaserF_Right_Locate)) * 180 / Math.PI;

                if (Math.Abs(theta) < 3)
                {
                    double p = barcodeX - Math.Sin(theta / 180 * Math.PI) * ((laser_F + laser_B) / 2 + alignmentDeviceOffset.BasedDistance) - RightStageDataList[stageNumber].Benchmark_P;
                    double y = Math.Cos(theta / 180 * Math.PI) * ((laser_F + laser_B) / 2 + alignmentDeviceOffset.BasedDistance) - alignmentDeviceOffset.BasedDistance - RightStageDataList[stageNumber].Benchmark_Y;
                    double z = barcodeY - RightStageDataList[stageNumber].Benchmark_Z;
                    theta -= RightStageDataList[stageNumber].Benchmark_Theta;
                    temp.P = p;
                    temp.Y = y;
                    temp.Theta = theta;
                    temp.Z = z;

                    if (AllAddressOffset.ContainsKey(checkAddressID))
                    {
                        temp.P -= AllAddressOffset[checkAddressID].P;
                        temp.Theta -= AllAddressOffset[checkAddressID].Theta;
                        temp.Y -= AllAddressOffset[checkAddressID].Y;
                        temp.Z = -AllAddressOffset[checkAddressID].Z;
                    }

                    temp.AlignmentVlaue = true;
                }
                else
                {
                    WriteLog(7, "", String.Concat("Theta過大 : ", theta.ToString()));
                }
            }

            localData.LoadUnloadData.AlignmentValue = temp;
        }

        private void CheckAlignmentValueOrigin(EnumStageDirection direction, int stageNumber)
        {
            try
            {
                string errorMessage = "";

                switch (direction)
                {
                    case EnumStageDirection.Left:
                        WriteLog(7, "", "UMTC沒有右補正計算功能");
                        localData.LoadUnloadData.AlignmentValue = null;

                        break;
                    case EnumStageDirection.Right:
                        if (lastRightStageNumber != stageNumber)
                        {
                            if (RightStageBarocdeReaderSetting.ContainsKey(stageNumber))
                            {
                                lastRightStageNumber = stageNumber;

                                if (RightStageBarocdeReaderSetting[stageNumber].BarcodeReaderMode != sr1000_R_Mode)
                                {
                                    if (sr1000_R != null)
                                    {
                                        if (sr1000_R.ChangeMode(RightStageBarocdeReaderSetting[stageNumber].BarcodeReaderMode, ref errorMessage))
                                            sr1000_R_Mode = RightStageBarocdeReaderSetting[stageNumber].BarcodeReaderMode;
                                        else
                                            WriteLog(3, "", String.Concat("Change Mode Fail ErrorMessage : ", errorMessage));
                                    }
                                }
                            }
                            else
                                WriteLog(3, "", String.Concat("Fatal Error, RightStageBarocdeReaderSetting 中沒有StatgeNumber = ", stageNumber.ToString()));
                        }

                        GetAlignmentValue(direction, stageNumber);

                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        public override void CheckAlingmentValue(EnumStageDirection direction, int stageNumber)
        {
            checkAddressID = "";
            CheckAlignmentValueOrigin(direction, stageNumber);

            AlignmentValueData temp = localData.LoadUnloadData.AlignmentValue;

            string message = "Aligment Data : ";

            if (temp != null)
            {
                message = String.Concat(message, " LaserF = ", temp.LaserF.ToString("0.0"), ", LaserB = ", temp.LaserB.ToString("0.0"));

                if (temp.BarcodeNumber != "")
                    message = String.Concat(message, ", BarcodeID = ", temp.BarcodeNumber, " ( ", temp.BarcodePosition.X.ToString("0.0"), ", ", temp.BarcodePosition.Y.ToString("0.0"), " )");
                else
                    message = String.Concat(message, ", No Barcode");

                if (temp.AlignmentVlaue)
                    message = String.Concat(message, ", P = ", temp.P.ToString("0.0"), ", Y = ", temp.Y.ToString("0.0"), ", Theta = ", temp.Theta.ToString("0.0"), ", Z = ", temp.Z.ToString("0.0"));
                else
                    message = String.Concat(message, ", No Aligment Data");
            }
            else
                message = String.Concat(message, " No Data");

            WriteLog(7, "", message);
        }

        private void SwitchAlingmentValue(EnumStageDirection direction, int stageNumber)
        {
            try
            {
                string errorMessage = "";

                switch (direction)
                {
                    case EnumStageDirection.Left:
                        WriteLog(7, "", "UMTC沒有右補正計算功能");

                        break;
                    case EnumStageDirection.Right:
                        if (lastRightStageNumber != stageNumber)
                        {
                            if (RightStageBarocdeReaderSetting.ContainsKey(stageNumber))
                            {
                                lastRightStageNumber = stageNumber;

                                if (RightStageBarocdeReaderSetting[stageNumber].BarcodeReaderMode != sr1000_R_Mode)
                                {
                                    if (sr1000_R != null)
                                    {
                                        if (sr1000_R.ChangeMode(RightStageBarocdeReaderSetting[stageNumber].BarcodeReaderMode, ref errorMessage))
                                            sr1000_R_Mode = RightStageBarocdeReaderSetting[stageNumber].BarcodeReaderMode;
                                        else
                                            WriteLog(3, "", String.Concat("Change Mode Fail ErrorMessage : ", errorMessage));
                                    }
                                }
                            }
                            else
                                WriteLog(3, "", String.Concat("Fatal Error, RightStageBarocdeReaderSetting 中沒有StatgeNumber = ", stageNumber.ToString()));
                        }

                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        public override void SwitchAlignmentValueByAddressID(string addressID)
        {
            if (localData.TheMapInfo.AllAddress.ContainsKey(addressID))
                SwitchAlingmentValue(localData.TheMapInfo.AllAddress[addressID].LoadUnloadDirection, localData.TheMapInfo.AllAddress[addressID].StageNumber);
        }

        public override void CheckAlingmentValueByAddressID(string addressID)
        {
            if (localData.TheMapInfo.AllAddress.ContainsKey(addressID))
            {
                checkAddressID = addressID;

                if (localData.TheMapInfo.AllAddress[addressID].LoadUnloadDirection != EnumStageDirection.None)
                    CheckAlignmentValueOrigin(localData.TheMapInfo.AllAddress[addressID].LoadUnloadDirection, localData.TheMapInfo.AllAddress[addressID].StageNumber);
                else if (localData.TheMapInfo.AllAddress[addressID].ChargingDirection != EnumStageDirection.None)
                    CheckAlignmentValueOrigin(localData.TheMapInfo.AllAddress[addressID].ChargingDirection, localData.TheMapInfo.AllAddress[addressID].StageNumber);
            }
        }
        #endregion

        #region GetMoveStatusByCVAxisName.
        private EnumAxisMoveStatus GetMoveStatusByCVAxisName(string axisNameString)
        {
            AxisFeedbackData temp = localData.LoadUnloadData.CVFeedbackData[axisNameString];

            if (temp == null)
                return EnumAxisMoveStatus.None;
            else
            {
                switch (axisPreStatus[axisNameString])
                {
                    case EnumAxisMoveStatus.PreMove:
                    case EnumAxisMoveStatus.PreStop:
                    case EnumAxisMoveStatus.PreStop_Force:
                        return axisPreStatus[axisNameString];

                    default:
                        return temp.AxisMoveStaus;
                }
            }
        }
        #endregion

        private bool CVPtoP(string axisNameString, double encoder, double speedPercent, bool waitStop, double timeoutValue)
        {
            if (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString() && localData.LoadUnloadData.Z軸主從誤差過大)
                return false;

            AxisFeedbackData temp = localData.LoadUnloadData.CVFeedbackData[axisNameString];
            AxisFeedbackData temp2 = (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString() ? localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()] : temp);

            if (temp == null || temp2 == null)
                return false;
            else if (temp.AxisServoOnOff == EnumAxisServoOnOff.ServoOff || temp2.AxisServoOnOff == EnumAxisServoOnOff.ServoOff)
                return false;

            if (mipcControl.SendMIPCDataByMIPCTagName(
                    new List<string>() {
                        axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.PlausUnit] ,
                        axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.Velocity] ,
                        axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.Acceleration],
                        axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.Deceleration],
                        axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.TargetPosition],
                        axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.PositionCommand]
                    },
                    new List<float>() {
                        (float)(axisDataList[axisNameString].PlausUnit),
                        (float)(axisDataList[axisNameString].AutoVelocity * speedPercent),
                        (float)(axisDataList[axisNameString].AutoAcceleration /** speedPercent*/),
                        (float)(axisDataList[axisNameString].AutoDeceleration /** speedPercent*/),
                        (float)(encoder + localData.LoadUnloadData.CVHomeOffsetValue[axisNameString] + localData.LoadUnloadData.CVEncoderOffsetValue[axisNameString]),
                        1
                    }))
            {
                axisPreTimer[axisNameString].Restart();
                axisPreStatus[axisNameString] = EnumAxisMoveStatus.PreMove;

                if (waitStop)
                {
                    Stopwatch timer = new Stopwatch();

                    while (GetMoveStatusByCVAxisName(axisNameString) != EnumAxisMoveStatus.Stop)
                    {
                        if (timer.ElapsedMilliseconds > timeoutValue)
                            return false;

                        Thread.Sleep(sleepTime);
                    }

                    return true;
                }
                else
                    return true;
            }
            else
                return false;
        }

        /// AxisJog
        ///     參數 : 軸名稱, 方向, 速度
        ///     return : boolean ( true : MIPC 指令傳送成功, false : MIPC 指令傳送失敗 or Not Servo On).
        #region AxisJog.
        private bool AxisJog(string axisNameString, bool direction, double speedPercent)
        {
            AxisFeedbackData temp = localData.LoadUnloadData.CVFeedbackData[axisNameString];
            AxisFeedbackData temp2 = (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString() ? localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()] : temp);

            if (temp == null || temp2 == null)
                return false;
            else if (temp.AxisServoOnOff == EnumAxisServoOnOff.ServoOff || temp2.AxisServoOnOff == EnumAxisServoOnOff.ServoOff)
                return false;

            if (mipcControl.SendMIPCDataByMIPCTagName(
                    new List<string>() {
                        axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.PlausUnit] ,
                        axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.Velocity] ,
                        axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.Acceleration],
                        axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.Deceleration],
                        axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.VelocityCommand]
                    },
                    new List<float>() {
                        (float)(axisDataList[axisNameString].PlausUnit),
                        (float)(axisDataList[axisNameString].AutoVelocity * ( direction ? 1 : -1 ) *  (float)(speedPercent)),
                        (float)(axisDataList[axisNameString].AutoAcceleration /** speedPercent*/),
                        (float)(axisDataList[axisNameString].AutoDeceleration /** speedPercent*/),
                        1
                    }))
            {
                axisPreTimer[axisNameString].Restart();
                axisPreStatus[axisNameString] = EnumAxisMoveStatus.PreMove;
                return true;
            }
            else
                return false;
        }
        #endregion

        /// ServoOn
        ///     參數 : 軸名稱, 是否等待ServoOn完成, timeout數值
        ///     return : boolean ( true : 如果不等待ServoOn完成則代表 MIPC 指令傳送成功, 等待ServoOn完成代表現在是ServoOn狀態).
        ///     如果是ServoOn Z軸, 則會一起ServoOn Z軸_Slave.
        #region ServoOn
        private bool ServoOn(string axisNameString, bool waitServoOn, double timeout)
        {
            if (localData.LoadUnloadData.CVFeedbackData == null ||
                !localData.LoadUnloadData.CVFeedbackData.ContainsKey(axisNameString) ||
                localData.LoadUnloadData.CVFeedbackData[axisNameString] == null)
                return false;

            List<string> tagNameList = new List<string>();
            List<float> valueList = new List<float>();

            if (logMode)
                WriteLog(7, "", String.Concat(axisNameString, "Servo on, waitServoOn : ", waitServoOn.ToString()));

            tagNameList.Add(axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.ServoOn]);
            valueList.Add((float)((int)EnumMIPCServoOnOffValue.ServoOn));

            if (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString())
            {
                tagNameList.Add(String.Concat(EnumLoadUnloadAxisName.Z軸_Slave.ToString(), "_", EnumLoadUnloadAxisCommandType.ServoOn));
                valueList.Add((float)((int)EnumMIPCServoOnOffValue.ServoOn));
            }

            if (!mipcControl.SendMIPCDataByMIPCTagName(tagNameList, valueList))
            {
                WriteLog(5, "", String.Concat("CV : ", axisNameString, " ServoOn Send MIPC Fail"));
                return false;
            }

            if (waitServoOn)
            {
                if (logMode)
                    WriteLog(7, "", "wait servo on");

                Stopwatch timer = new Stopwatch();
                timer.Restart();

                if (localData.LoadUnloadData.CVFeedbackData.ContainsKey(axisNameString))
                {
                    if (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString() &&
                        !localData.LoadUnloadData.CVFeedbackData.ContainsKey(EnumLoadUnloadAxisName.Z軸_Slave.ToString()))
                    {
                        WriteLog(5, "", String.Concat("CV : ", axisNameString, " FeedbackData not in CVFeedbackData"));
                        return false;
                    }

                    AxisFeedbackData temp = localData.LoadUnloadData.CVFeedbackData[axisNameString];
                    AxisFeedbackData temp2 = null;

                    if (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString())
                        temp2 = localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()];
                    else
                        temp2 = temp;

                    while (!(temp.AxisServoOnOff == EnumAxisServoOnOff.ServoOn && temp2.AxisServoOnOff == EnumAxisServoOnOff.ServoOn))
                    {
                        if (timer.ElapsedMilliseconds > timeout)
                        {
                            WriteLog(5, "", String.Concat("CV : ", axisNameString, " Wait ServoOn Timeout"));
                            return false;
                        }

                        Thread.Sleep(sleepTime);

                        temp = localData.LoadUnloadData.CVFeedbackData[axisNameString];

                        if (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString())
                            temp2 = localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()];
                        else
                            temp2 = temp;
                    }
                }
                else
                {
                    WriteLog(5, "", String.Concat("CV : ", axisNameString, " FeedbackData not in CVFeedbackData"));
                    return false;
                }
            }

            return true;
        }

        private bool ServoOff(string axisNameString, bool waitServoOn, double timeout)
        {
            List<string> tagNameList = new List<string>();
            List<float> valueList = new List<float>();


            if (logMode)
                WriteLog(7, "", String.Concat(axisNameString, "Servo off, waitServoOff : ", waitServoOn.ToString()));

            tagNameList.Add(axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.ServoOn]);
            valueList.Add((float)((int)EnumMIPCServoOnOffValue.ServoOff));

            if (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString())
            {
                tagNameList.Add(String.Concat(EnumLoadUnloadAxisName.Z軸_Slave.ToString(), "_", EnumLoadUnloadAxisCommandType.ServoOn));
                valueList.Add((float)((int)EnumMIPCServoOnOffValue.ServoOff));
            }

            if (!mipcControl.SendMIPCDataByMIPCTagName(tagNameList, valueList))
            {
                WriteLog(5, "", String.Concat("CV : ", axisNameString, " ServoOff Send MIPC Fail"));
                return false;
            }

            if (waitServoOn)
            {
                if (logMode)
                    WriteLog(7, "", "wait servo off");

                Stopwatch timer = new Stopwatch();
                timer.Restart();

                if (localData.LoadUnloadData.CVFeedbackData.ContainsKey(axisNameString))
                {
                    if (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString() &&
                        !localData.LoadUnloadData.CVFeedbackData.ContainsKey(EnumLoadUnloadAxisName.Z軸_Slave.ToString()))
                    {
                        WriteLog(5, "", String.Concat("CV : ", axisNameString, " FeedbackData not in CVFeedbackData"));
                        return false;
                    }

                    AxisFeedbackData temp = localData.LoadUnloadData.CVFeedbackData[axisNameString];
                    AxisFeedbackData temp2 = null;

                    if (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString())
                        temp2 = localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()];
                    else
                        temp2 = temp;

                    while (!(temp.AxisServoOnOff == EnumAxisServoOnOff.ServoOff && temp2.AxisServoOnOff == EnumAxisServoOnOff.ServoOff))
                    {
                        if (timer.ElapsedMilliseconds > timeout)
                        {
                            WriteLog(5, "", String.Concat("CV : ", axisNameString, " Wait ServoOff Timeout"));
                            return false;
                        }

                        Thread.Sleep(sleepTime);

                        temp = localData.LoadUnloadData.CVFeedbackData[axisNameString];

                        if (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString())
                            temp2 = localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()];
                        else
                            temp2 = temp;
                    }
                }
                else
                {
                    WriteLog(5, "", String.Concat("CV : ", axisNameString, " FeedbackData not in CVFeedbackData"));
                    return false;
                }
            }

            return true;
        }
        #endregion

        public override void Jog(int indexOfAxis, bool direction)
        {
            try
            {
                if (!configAllOK)
                    return;

                if (AxisList[indexOfAxis] == EnumLoadUnloadAxisName.Z軸.ToString() && localData.LoadUnloadData.Z軸主從誤差過大)
                    return;

                if (!CheckAixsIsServoOn(AxisList[indexOfAxis]))
                    ServoOn(AxisList[indexOfAxis], false, 0);
                else
                {
                    if (!AxisJog(AxisList[indexOfAxis], direction, (JogByPass ? axisDataList[AxisList[indexOfAxis]].JogSpeed[EnumLoadUnloadJogSpeed.Low] : axisDataList[AxisList[indexOfAxis]].JogSpeed[JogSpeed])))
                        WriteLog(5, "", String.Concat("Jog Fail"));
                }
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        public override void Jog_相對(string axisName, double deltaEncoder)
        {
            try
            {
                if (!configAllOK)
                    return;

                if (axisName == EnumLoadUnloadAxisName.Z軸.ToString() && localData.LoadUnloadData.Z軸主從誤差過大)
                    return;

                if (!CheckAixsIsServoOn(axisName))
                    ServoOn(axisName, false, 0);
                else
                {
                    if (!CVPtoP(axisName, localData.LoadUnloadData.CVFeedbackData[axisName].Position + deltaEncoder, 0.1, false, 0))
                        WriteLog(5, "", String.Concat("Jog_相對 Fail"));
                }
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        public override void Jog_NoSafety(string axisName, bool direction)
        {
            try
            {
                if (!configAllOK)
                    return;

                if (!mipcControl.SendMIPCDataByMIPCTagName(
                        new List<string>() {
                           axisCommandString[axisName][EnumLoadUnloadAxisCommandType.PlausUnit] ,
                           axisCommandString[axisName][EnumLoadUnloadAxisCommandType.Velocity] ,
                           axisCommandString[axisName][EnumLoadUnloadAxisCommandType.Acceleration],
                           axisCommandString[axisName][EnumLoadUnloadAxisCommandType.Deceleration],
                           axisCommandString[axisName][EnumLoadUnloadAxisCommandType.VelocityCommand]
                        },
                        new List<float>() {
                           (float)(axisDataList[axisName].PlausUnit),
                           (float)(0.1* ( direction ? 1 : -1 ) ),
                           (float)(axisDataList[axisName].AutoAcceleration ),
                           (float)(axisDataList[axisName].AutoDeceleration ),
                           1
                        }))
                    WriteLog(5, "", String.Concat("Jog_NoSafety Fail"));
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        public override void Jog_相對_NoSafety(string axisName, double deltaEncoder)
        {
            try
            {
                if (!configAllOK)
                    return;

                if (mipcControl.SendMIPCDataByMIPCTagName(
                        new List<string>() {
                        axisCommandString[axisName][EnumLoadUnloadAxisCommandType.PlausUnit] ,
                        axisCommandString[axisName][EnumLoadUnloadAxisCommandType.Velocity] ,
                        axisCommandString[axisName][EnumLoadUnloadAxisCommandType.Acceleration],
                        axisCommandString[axisName][EnumLoadUnloadAxisCommandType.Deceleration],
                        axisCommandString[axisName][EnumLoadUnloadAxisCommandType.TargetPosition],
                        axisCommandString[axisName][EnumLoadUnloadAxisCommandType.PositionCommand]
                        },
                        new List<float>() {
                        (float)(axisDataList[axisName].PlausUnit),
                        (float)(0.1),
                        (float)(axisDataList[axisName].AutoAcceleration /** speedPercent*/),
                        (float)(axisDataList[axisName].AutoDeceleration /** speedPercent*/),
                        (float)(deltaEncoder + localData.LoadUnloadData.CVFeedbackData[axisName].OriginPosition),
                        1
                        }))
                    WriteLog(5, "", String.Concat("Jog_相對_NoSafety Fail"));
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        public override double GetDeltaZ
        {
            get
            {
                if (localData.LoadUnloadData.CVFeedbackData != null &&
                    localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()] != null &&
                    localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()] != null)
                    return Math.Abs(localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].Position - localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()].Position);
                else
                    return 0;
            }
        }

        public double GetDeltaZValue
        {
            get
            {
                if (localData.LoadUnloadData.CVFeedbackData != null &&
                    localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()] != null &&
                    localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()] != null)
                    return localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].Position - localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()].Position;
                else
                    return 0;
            }
        }

        private bool Stop(EnumLoadUnloadAxisName axis)
        {
            if (!configAllOK)
                return false; ;

            if (mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { axisCommandString[axis.ToString()][EnumLoadUnloadAxisCommandType.StopCommand] }, new List<float>() { 1 }))
            {
                axisPreTimer[axis.ToString()].Restart();

                if (axisPreStatus[axis.ToString()] != EnumAxisMoveStatus.PreMove)
                    axisPreStatus[axis.ToString()] = EnumAxisMoveStatus.PreStop;
                else
                    axisPreStatus[axis.ToString()] = EnumAxisMoveStatus.PreStop_Force;

                return true;
            }
            else
            {
                WriteLog(5, "", String.Concat("Stop Fail"));
                return false;
            }
        }

        public override bool JogStop()
        {
            if (!configAllOK)
                return false; ;

            if (mipcControl.SendMIPCDataByMIPCTagName(jogStopStringList, jogStopValueList))
            {
                axisPreTimer[EnumLoadUnloadAxisName.P軸.ToString()].Restart();

                if (axisPreStatus[EnumLoadUnloadAxisName.P軸.ToString()] != EnumAxisMoveStatus.PreMove)
                    axisPreStatus[EnumLoadUnloadAxisName.P軸.ToString()] = EnumAxisMoveStatus.PreStop;
                else
                    axisPreStatus[EnumLoadUnloadAxisName.P軸.ToString()] = EnumAxisMoveStatus.PreStop_Force;

                axisPreTimer[EnumLoadUnloadAxisName.Theta軸.ToString()].Restart();

                if (axisPreStatus[EnumLoadUnloadAxisName.Theta軸.ToString()] != EnumAxisMoveStatus.PreMove)
                    axisPreStatus[EnumLoadUnloadAxisName.Theta軸.ToString()] = EnumAxisMoveStatus.PreStop;
                else
                    axisPreStatus[EnumLoadUnloadAxisName.Theta軸.ToString()] = EnumAxisMoveStatus.PreStop_Force;

                axisPreTimer[EnumLoadUnloadAxisName.Z軸.ToString()].Restart();

                if (axisPreStatus[EnumLoadUnloadAxisName.Z軸.ToString()] != EnumAxisMoveStatus.PreMove)
                    axisPreStatus[EnumLoadUnloadAxisName.Z軸.ToString()] = EnumAxisMoveStatus.PreStop;
                else
                    axisPreStatus[EnumLoadUnloadAxisName.Z軸.ToString()] = EnumAxisMoveStatus.PreStop_Force;

                axisPreTimer[EnumLoadUnloadAxisName.Roller.ToString()].Restart();

                if (axisPreStatus[EnumLoadUnloadAxisName.Roller.ToString()] != EnumAxisMoveStatus.PreMove)
                    axisPreStatus[EnumLoadUnloadAxisName.Roller.ToString()] = EnumAxisMoveStatus.PreStop;
                else
                    axisPreStatus[EnumLoadUnloadAxisName.Roller.ToString()] = EnumAxisMoveStatus.PreStop_Force;

                return true;
            }
            else
            {
                WriteLog(5, "", String.Concat("LoadUnload Jog Stop Fail"));
                return false;
            }
        }

        public bool AxisNameStop(string axisName)
        {
            if (!configAllOK)
                return false;

            if (axisNameToStopTagList.ContainsKey(axisName) &&
                mipcControl.SendMIPCDataByMIPCTagName(axisNameToStopTagList[axisName], axisNameToStopValueList[axisName]))
            {
                axisPreTimer[axisName].Restart();
                axisPreStatus[axisName] = EnumAxisMoveStatus.PreStop;
                return true;
            }
            else
            {
                WriteLog(5, "", String.Concat(axisName, " Stop Fail"));
                return false;
            }
        }

        private bool CheckAxisError(EnumLoadUnloadAxisName axisName)
        {
            if (localData.LoadUnloadData != null && localData.LoadUnloadData.CVFeedbackData[axisName.ToString()] != null)
                return localData.LoadUnloadData.CVFeedbackData[axisName.ToString()].MF != 0;
            else
                return false;
        }

        private void CheckAxisStatusByAxisName(EnumLoadUnloadAxisName axisName, EnumLoadUnloadControlErrorCode alarmCode)
        {
            if (CheckAxisError(axisName))
            {
                switch (axisName)
                {
                    case EnumLoadUnloadAxisName.Z軸:
                    case EnumLoadUnloadAxisName.Z軸_Slave:
                        if (GetMoveStatusByCVAxisName(EnumLoadUnloadAxisName.Z軸.ToString()) == EnumAxisMoveStatus.Move)
                            Stop(EnumLoadUnloadAxisName.Z軸);
                        break;
                    default:
                        if (GetMoveStatusByCVAxisName(axisName.ToString()) == EnumAxisMoveStatus.Move)
                            Stop(axisName);
                        break;
                }

                SetAlarmCode(alarmCode);
            }
            else
                ResetAlarmCode(alarmCode);
        }

        private double GetQA(EnumLoadUnloadAxisName axisName)
        {
            if (localData.LoadUnloadData != null && !localData.LoadUnloadData.CVFeedbackData.ContainsKey(axisName.ToString()) &&
                localData.LoadUnloadData.CVFeedbackData[axisName.ToString()] != null)
                return localData.LoadUnloadData.CVFeedbackData[axisName.ToString()].QA;
            else
                return 0;
        }

        private double z軸過電流閥值 = 20;
        private double z軸停止Encoder允許誤差 = 3;
        private double z軸移動中Encoder允許誤差 = 50;
        private double z軸過電流DelayTime = 300;
        private bool z軸Delaying = false;
        private bool z軸_SlaveDelaying = false;
        private Stopwatch z軸DelayTimer = new Stopwatch();
        private Stopwatch z軸_SlaveDelayTimer = new Stopwatch();
        private Stopwatch 主軸與從軸資料不同步DelayTimer = new Stopwatch();
        private double 主軸與從軸資料不同步DelayTime = 1000;
        private bool 主軸與從軸資料不同步Delaying = false;

        private void CheckAxisStatus()
        {
            #region Getway Z軸.
            Getway通訊正常_Z軸.Data = localData.MIPCData.GetDataByMIPCTagName("Gateway#6通訊異常警報") != 1;

            if (Getway通訊正常_Z軸.Data)
                ResetAlarmCode(EnumLoadUnloadControlErrorCode.Getway通訊異常_Z軸);
            else
                SetAlarmCode(EnumLoadUnloadControlErrorCode.Getway通訊異常_Z軸);
            #endregion
            #region Getway Z軸_Slave.
            Getway通訊正常_Z軸_Slave.Data = localData.MIPCData.GetDataByMIPCTagName("Gateway#7通訊異常警報") != 1;

            if (Getway通訊正常_Z軸_Slave.Data)
                ResetAlarmCode(EnumLoadUnloadControlErrorCode.Getway通訊異常_Z軸_Slave);
            else
                SetAlarmCode(EnumLoadUnloadControlErrorCode.Getway通訊異常_Z軸_Slave);
            #endregion
            #region Getway P軸.
            Getway通訊正常_P軸.Data = localData.MIPCData.GetDataByMIPCTagName("Gateway#8通訊異常警報") != 1;

            if (Getway通訊正常_P軸.Data)
                ResetAlarmCode(EnumLoadUnloadControlErrorCode.Getway通訊異常_P軸);
            else
                SetAlarmCode(EnumLoadUnloadControlErrorCode.Getway通訊異常_P軸);
            #endregion
            #region Getway Theta軸.
            Getway通訊正常_Theta軸.Data = localData.MIPCData.GetDataByMIPCTagName("Gateway#9通訊異常警報") != 1;

            if (Getway通訊正常_Theta軸.Data)
                ResetAlarmCode(EnumLoadUnloadControlErrorCode.Getway通訊異常_Theta軸);
            else
                SetAlarmCode(EnumLoadUnloadControlErrorCode.Getway通訊異常_Theta軸);
            #endregion
            #region Getway Theta軸.
            Getway通訊正常_Roller.Data = localData.MIPCData.GetDataByMIPCTagName("Gateway#10通訊異常警報") != 1;

            if (Getway通訊正常_Roller.Data)
                ResetAlarmCode(EnumLoadUnloadControlErrorCode.Getway通訊異常_Roller);
            else
                SetAlarmCode(EnumLoadUnloadControlErrorCode.Getway通訊異常_Roller);
            #endregion

            if (!reconnectedAndResetAxisError)
            {
                CheckAxisStatusByAxisName(EnumLoadUnloadAxisName.P軸, EnumLoadUnloadControlErrorCode.P軸驅動器異常);
                CheckAxisStatusByAxisName(EnumLoadUnloadAxisName.Theta軸, EnumLoadUnloadControlErrorCode.Theta軸驅動器異常);
                CheckAxisStatusByAxisName(EnumLoadUnloadAxisName.Roller, EnumLoadUnloadControlErrorCode.Roller驅動器異常);
                CheckAxisStatusByAxisName(EnumLoadUnloadAxisName.Z軸, EnumLoadUnloadControlErrorCode.Z軸驅動器異常);
                CheckAxisStatusByAxisName(EnumLoadUnloadAxisName.Z軸_Slave, EnumLoadUnloadControlErrorCode.Z軸_Slave驅動器異常);
            }

            if (localData.LoadUnloadData.平衡Z軸)
                SetAlarmCode(EnumLoadUnloadControlErrorCode.平衡Z軸模式中);
            else
                ResetAlarmCode(EnumLoadUnloadControlErrorCode.平衡Z軸模式中);

            if (GetMoveStatusByCVAxisName(EnumLoadUnloadAxisName.Z軸.ToString()) != EnumAxisMoveStatus.Stop)
            {
                主軸與從軸資料不同步Delaying = false;

                if (localData.MIPCData.GetDataByMIPCTagName(Z軸從軸上極限) == 0)
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.Z軸Slave上極限觸發);
                else
                    ResetAlarmCode(EnumLoadUnloadControlErrorCode.Z軸Slave上極限觸發);

                if (localData.MIPCData.GetDataByMIPCTagName(Z軸從軸下極限) == 0)
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.Z軸Slave下極限觸發);
                else
                    ResetAlarmCode(EnumLoadUnloadControlErrorCode.Z軸Slave下極限觸發);

                if (!localData.LoadUnloadData.平衡Z軸)
                {
                    if (localData.MIPCData.GetDataByMIPCTagName(Z軸從軸上極限) == 0)
                    {
                        if (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].Velocity > 0)
                            Stop(EnumLoadUnloadAxisName.Z軸);
                    }
                    else if (localData.MIPCData.GetDataByMIPCTagName(Z軸從軸下極限) == 0)
                    {
                        if (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].Velocity < 0)
                            Stop(EnumLoadUnloadAxisName.Z軸);
                    }

                    #region Z軸過電流保護?.
                    if (GetQA(EnumLoadUnloadAxisName.Z軸) > z軸過電流閥值)
                    {
                        if (z軸Delaying)
                        {
                            if (z軸DelayTimer.ElapsedMilliseconds > z軸過電流DelayTime)
                            {
                                Stop(EnumLoadUnloadAxisName.Z軸);
                                SetAlarmCode(EnumLoadUnloadControlErrorCode.Z軸電流過大);
                            }
                        }
                        else
                        {
                            z軸DelayTimer.Restart();
                            z軸Delaying = true;
                        }
                    }
                    else
                    {
                        ResetAlarmCode(EnumLoadUnloadControlErrorCode.Z軸電流過大);
                        z軸DelayTimer.Stop();
                        z軸Delaying = false;
                    }
                    #endregion

                    #region Z軸Slave過電流保護?.
                    if (GetQA(EnumLoadUnloadAxisName.Z軸_Slave) > z軸過電流閥值)
                    {
                        if (z軸_SlaveDelaying)
                        {
                            if (z軸_SlaveDelayTimer.ElapsedMilliseconds > z軸過電流DelayTime)
                            {
                                Stop(EnumLoadUnloadAxisName.Z軸);
                                SetAlarmCode(EnumLoadUnloadControlErrorCode.Z軸_Slave電流過大);
                            }
                        }
                        else
                        {
                            z軸_SlaveDelayTimer.Restart();
                            z軸_SlaveDelaying = true;
                        }
                    }
                    else
                    {
                        ResetAlarmCode(EnumLoadUnloadControlErrorCode.Z軸_Slave電流過大);
                        z軸_SlaveDelayTimer.Stop();
                        z軸_SlaveDelaying = false;
                    }
                    #endregion

                    if (localData.LoadUnloadData.z軸EncoderHome)
                    {
                        if (GetDeltaZ > z軸移動中Encoder允許誤差)
                        {
                            Stop(EnumLoadUnloadAxisName.Z軸);

                            if (!localData.LoadUnloadData.Z軸主從誤差過大)
                                mipcControl.SendMIPCDataByIPCTagName(new List<EnumMecanumIPCdefaultTag>() { EnumMecanumIPCdefaultTag.Z軸Encoder歪掉 }, new List<float>() { (float)GetDeltaZValue });
                        }
                    }

                    if (localData.LoadUnloadData.Z軸主從誤差過大)
                        Stop(EnumLoadUnloadAxisName.Z軸);
                }
            }
            else
            {
                if (主軸與從軸資料不同步Delaying)
                {
                    if (主軸與從軸資料不同步DelayTimer.ElapsedMilliseconds > 主軸與從軸資料不同步DelayTime)
                    {
                        主軸與從軸資料不同步DelayTimer.Stop();

                        z軸DelayTimer.Stop();
                        z軸Delaying = false;

                        z軸_SlaveDelayTimer.Stop();
                        z軸_SlaveDelaying = false;

                        if (localData.LoadUnloadData.z軸EncoderHome)
                        {
                            if (GetDeltaZ > z軸停止Encoder允許誤差)
                            {
                                if (!localData.LoadUnloadData.Z軸主從誤差過大)
                                    mipcControl.SendMIPCDataByIPCTagName(new List<EnumMecanumIPCdefaultTag>() { EnumMecanumIPCdefaultTag.Z軸Encoder歪掉 }, new List<float>() { (float)GetDeltaZValue });
                            }
                        }
                    }
                }
                else
                {
                    主軸與從軸資料不同步DelayTimer.Restart();
                    主軸與從軸資料不同步Delaying = true;
                }
            }

            if (localData.LoadUnloadData.Z軸主從誤差過大)
                SetAlarmCode(EnumLoadUnloadControlErrorCode.Z軸主從Encoder落差過大);
            else
                ResetAlarmCode(EnumLoadUnloadControlErrorCode.Z軸主從Encoder落差過大);
        }

        bool 確認CVEncoder = false;

        private void CheckCVEncoderHome()
        {
            if (!確認CVEncoder && localData.LoadUnloadData.EncoderOffset讀取 && localData.AutoManual == EnumAutoState.Manual)
            {
                確認CVEncoder = true;

                if (localData.LoadUnloadData.Encoder已回Home)
                {
                    localData.LoadUnloadData.CVEncoderOffsetValue[EnumLoadUnloadAxisName.Z軸.ToString()] = localData.LoadUnloadData.Z軸EncoderOffset;
                    localData.LoadUnloadData.CVHomeOffsetValue[EnumLoadUnloadAxisName.Z軸.ToString()] = axisDataList[EnumLoadUnloadAxisName.Z軸.ToString()].HomeOffset;

                    localData.LoadUnloadData.CVEncoderOffsetValue[EnumLoadUnloadAxisName.Z軸_Slave.ToString()] = localData.LoadUnloadData.Z軸_SlaveEncoderOffset;
                    //localData.LoadUnloadData.CVHomeOffsetValue[EnumLoadUnloadAxisName.Z軸_Slave.ToString()] = axisDataList[EnumLoadUnloadAxisName.Z軸_Slave.ToString()].HomeOffset;

                    localData.LoadUnloadData.CVEncoderOffsetValue[EnumLoadUnloadAxisName.P軸.ToString()] = localData.LoadUnloadData.P軸EncoderOffset;
                    localData.LoadUnloadData.CVHomeOffsetValue[EnumLoadUnloadAxisName.P軸.ToString()] = axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].HomeOffset;

                    localData.LoadUnloadData.CVEncoderOffsetValue[EnumLoadUnloadAxisName.Theta軸.ToString()] = localData.LoadUnloadData.Theta軸EncoderOffset;
                    localData.LoadUnloadData.CVHomeOffsetValue[EnumLoadUnloadAxisName.Theta軸.ToString()] = axisDataList[EnumLoadUnloadAxisName.Theta軸.ToString()].HomeOffset;

                    Thread.Sleep(200);
                    localData.LoadUnloadData.z軸EncoderHome = true;
                    localData.LoadUnloadData.p軸EncoderHome = true;
                    localData.LoadUnloadData.theta軸EncoderHome = true;
                }
            }
        }

        public override void UpdateLoadingAndCSTID()
        {
            if (localData.LoadUnloadData.LoadUnloadCommand == null)
                return;

            if (AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV停止].Data &&
                AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV減速].Data)
            {
                cv_Normal.Data = true;
                localData.LoadUnloadData.Loading = true;
            }
            else if (!AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV停止].Data &&
                     !AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV減速].Data &&
                     !AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV入料].Data)
            {
                cv_Normal.Data = true;
                localData.LoadUnloadData.Loading = false;
            }
            else
            {
                cv_Normal.Data = false;
                localData.LoadUnloadData.Loading = true;
            }

            if (localData.LoadUnloadData.LoadUnloadCommand != null)
            {
                if (cv_Normal.Data && localData.AutoManual == EnumAutoState.Auto)
                    localData.LoadUnloadData.Loading_LogicFlag = localData.LoadUnloadData.Loading;
            }

            if (cstIDReader != null && localData.LoadUnloadData.Loading)
            {
                LoadUnloadCommandData cmd = localData.LoadUnloadData.LoadUnloadCommand;

                string message = "";
                string errorMessage = "";

                if (cstIDReader.ReadBarcode(ref message, 300, ref errorMessage) ||
                    cstIDReader.ReadBarcode(ref message, 300, ref errorMessage) ||
                    cstIDReader.ReadBarcode(ref message, 300, ref errorMessage))
                {
                    if (message.Length > 2)
                        localData.LoadUnloadData.CstID = message.Trim();

                    WriteLog(7, "", String.Concat("CSTID Read Success : ", localData.LoadUnloadData.CstID));

                    if (cmd != null)
                        cmd.CSTID = localData.LoadUnloadData.CstID;
                }
                else
                {
                    if (cstIDReader.Error)
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.CSTIDReader_斷線);

                    cstIDReader.StopReadBarcode(ref message, ref errorMessage);
                    localData.LoadUnloadData.CstID = "";
                    WriteLog(7, "", "CSTID Read Fail");

                    if (cmd != null)
                        cmd.ReadFail = true;
                }
            }
            else
                localData.LoadUnloadData.CstID = "";
        }

        private void UpdatePreActiionByCVAxisName(string axisNameString)
        {
            AxisFeedbackData temp = localData.LoadUnloadData.CVFeedbackData[axisNameString];

            if (temp != null)
            {
                switch (axisPreStatus[axisNameString])
                {
                    case EnumAxisMoveStatus.PreMove:
                        if (temp.AxisMoveStaus == EnumAxisMoveStatus.Move || axisPreTimer[axisNameString].ElapsedMilliseconds > axisStatusDelayTime)
                            axisPreStatus[axisNameString] = EnumAxisMoveStatus.None;

                        break;
                    case EnumAxisMoveStatus.PreStop:
                        if (temp.AxisMoveStaus == EnumAxisMoveStatus.Stop || axisPreTimer[axisNameString].ElapsedMilliseconds > axisStatusDelayTime)
                            axisPreStatus[axisNameString] = EnumAxisMoveStatus.None;

                        break;
                    case EnumAxisMoveStatus.PreStop_Force:
                        if (axisPreTimer[axisNameString].ElapsedMilliseconds > axisStatusDelayTime)
                            axisPreStatus[axisNameString] = EnumAxisMoveStatus.None;

                        break;
                    default:
                        break;
                }
            }
        }

        public override void UpdateForkHomeStatus()
        {
            if (localData.SimulateMode)
                return;

            #region Update Axis Status.
            UpdatePreActiionByCVAxisName(EnumLoadUnloadAxisName.Z軸.ToString());
            UpdatePreActiionByCVAxisName(EnumLoadUnloadAxisName.P軸.ToString());
            UpdatePreActiionByCVAxisName(EnumLoadUnloadAxisName.Theta軸.ToString());
            UpdatePreActiionByCVAxisName(EnumLoadUnloadAxisName.Roller.ToString());
            #endregion

            CheckCVEncoderHome();
            CheckAxisStatus();

            for (int i = 0; i < AxisList.Count; i++)
            {
                for (int j = 0; j < AxisSensorList[AxisList[i]].Count; j++)
                {
                    if (AxisList[i] != "Roller")
                        AxisSensorDataList[AxisList[i]][j].Data = localData.MIPCData.GetDataByMIPCTagName(AxisSensorList[AxisList[i]][j]) == 0;
                    else
                        AxisSensorDataList[AxisList[i]][j].Data = localData.MIPCData.GetDataByMIPCTagName(AxisSensorList[AxisList[i]][j]) != 0;

                    if (AxisSensorDataList[AxisList[i]][j].Change)
                    {
                        WriteLog(7, "", String.Concat(AxisSensorList[AxisList[i]][j], " Change to ",
                                                      (AxisSensorDataList[AxisList[i]][j].data ? "on" : "off")));
                    }
                }
            }

            if (AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV停止].Data &&
                AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV減速].Data)
            {
                cv_Normal.Data = true;
                localData.LoadUnloadData.Loading = true;
            }
            else if (!AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV停止].Data &&
                     !AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV減速].Data &&
                     !AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV入料].Data)
            {
                cv_Normal.Data = true;
                localData.LoadUnloadData.Loading = false;
            }
            else
            {
                cv_Normal.Data = false;
                localData.LoadUnloadData.Loading = true;
            }

            if (localData.LoadUnloadData.LoadUnloadCommand == null)
            {
                if (!cv_Normal.Data)
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.CVSensor異常);
                else
                    ResetAlarmCode(EnumLoadUnloadControlErrorCode.CVSensor異常);
            }
            else
                ResetAlarmCode(EnumLoadUnloadControlErrorCode.CVSensor異常);

            if (localData.LoadUnloadData.p軸EncoderHome && localData.LoadUnloadData.z軸EncoderHome && localData.LoadUnloadData.theta軸EncoderHome)
            {
                //AxisFeedbackData temp = localData.
                string axisNameString = EnumLoadUnloadAxisName.Z軸.ToString();
                string roller = EnumLoadUnloadAxisName.Roller.ToString();

                if (localData.MainFlowConfig.PowerSavingMode)
                    z軸Home = AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data;
                else
                    z軸Home = Math.Abs(localData.LoadUnloadData.CVFeedbackData[axisNameString].Position) <= inPositionRange[axisNameString] &&
                              AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data &&
                              CheckAixsIsServoOn(axisNameString) &&
                              CheckAixsIsServoOn(roller);

                if (localData.MainFlowConfig.HomeInUpOrDownPosition && !localData.LoadUnloadData.Loading && !z軸Home)
                { // 合法在上麵.
                    if (localData.MainFlowConfig.PowerSavingMode)
                        z軸Home = (!localData.MainFlowConfig.CheckPassLineSensor || AllAxisSensorData[EnumLoadUnloadAxisName.Z軸.ToString()][Z軸上位].Data);
                    else
                        z軸Home = (!localData.MainFlowConfig.CheckPassLineSensor || AllAxisSensorData[EnumLoadUnloadAxisName.Z軸.ToString()][Z軸上位].Data) &&
                                  CheckAixsIsServoOn(axisNameString) &&
                                  CheckAixsIsServoOn(roller);
                }

                axisNameString = EnumLoadUnloadAxisName.P軸.ToString();
                p軸Home = Math.Abs(localData.LoadUnloadData.CVFeedbackData[axisNameString].Position) <= inPositionRange[axisNameString] &&
                          AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data && CheckAixsIsServoOn(axisNameString);

                axisNameString = EnumLoadUnloadAxisName.Theta軸.ToString();
                theta軸Home = Math.Abs(localData.LoadUnloadData.CVFeedbackData[axisNameString].Position) <= inPositionRange[axisNameString] &&
                              AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data && CheckAixsIsServoOn(axisNameString);

                localData.LoadUnloadData.ForkHome = z軸Home && p軸Home && theta軸Home &&
                                                    !localData.LoadUnloadData.Z軸主從誤差過大;
            }
            else
                localData.LoadUnloadData.ForkHome = false;

            if (localData.LoadUnloadData.LoadUnloadCommand == null && !localData.LoadUnloadData.ForkHome)
                SetAlarmCode(EnumLoadUnloadControlErrorCode.Fork不在Home點);
            else
                ResetAlarmCode(EnumLoadUnloadControlErrorCode.Fork不在Home點);

            if (localData.MoveControlData.MotionControlData.JoystickMode || localData.MoveControlData.MoveCommand != null ||
                localData.LoadUnloadData.LoadUnloadCommand != null)
            {
                for (int i = 0; i < AxisList.Count; i++)
                    AxisCanJog[i] = false;
            }
            else if (JogByPass)
            {
                for (int i = 0; i < AxisList.Count; i++)
                    AxisCanJog[i] = true;
            }
            else
            {
                for (int i = 0; i < AxisList.Count; i++)
                {
                    if (AxisList[i] == EnumLoadUnloadAxisName.Z軸.ToString())
                    {
                        if (AllAxisSensorData[EnumLoadUnloadAxisName.P軸.ToString()][P軸原點].Data &&
                            AllAxisSensorData[EnumLoadUnloadAxisName.Theta軸.ToString()][Theta軸原點].Data)
                            AxisCanJog[i] = true;
                        else
                            AxisCanJog[i] = false;
                    }
                    else if (AxisList[i] == EnumLoadUnloadAxisName.Roller.ToString())
                    {
                        AxisCanJog[i] = true;
                    }
                    else
                    {
                        if (AllAxisSensorData[EnumLoadUnloadAxisName.Z軸.ToString()][Z軸上極限].Data ||
                            AllAxisSensorData[EnumLoadUnloadAxisName.Z軸.ToString()][Z軸上位].Data)
                            AxisCanJog[i] = true;
                        else
                            AxisCanJog[i] = false;
                    }
                }
            }

            if (localData.AutoManual == EnumAutoState.Auto)
            {
                if (localData.LoadUnloadData.LoadUnloadCommand == null)
                {
                    //if (localData.LoadUnloadData.Loading_LogicFlag != localData.LoadUnloadData.Loading)
                    //    SetAlarmCode(EnumLoadUnloadControlErrorCode.Loading邏輯和Sensor不相符);
                    //else
                    //    ResetAlarmCode(EnumLoadUnloadControlErrorCode.Loading邏輯和Sensor不相符);
                }
            }
            else
            {
                ResetAlarmCode(EnumLoadUnloadControlErrorCode.Loading邏輯和Sensor不相符);

                if (localData.LoadUnloadData.LoadUnloadCommand == null)
                {
                    if (!localData.LoadUnloadData.Loading)
                        localData.LoadUnloadData.CstID = "";
                }
            }
        }

        private bool SetMessageAndWaitGoNext(string message)
        {
            localData.LoadUnloadData.ResetAlignmentDeviceToZeroMessage = message;
            localData.LoadUnloadData.ResetAlignmentDeviceToZeroGoNext = false;
            localData.LoadUnloadData.ResetAlignmentDeviceToZeroCanNextStep = true;

            while (!localData.LoadUnloadData.ResetAlignmentDeviceToZeroGoNext)
            {
                if (localData.LoadUnloadData.HomeStop)
                {
                    alignmentDeviceOffset = tempOldConfig;
                    localData.LoadUnloadData.Homing = false;
                    localData.LoadUnloadData.ResetZero = false;
                    return false;
                }
            }

            return true;
        }

        private LoadUnloadOffset tempOldConfig;

        private void SetAlignmentDeviceToZeroThread()
        {
            tempOldConfig = alignmentDeviceOffset;
            alignmentDeviceOffset = new LoadUnloadOffset();
            alignmentDeviceOffset.BasedDistance = tempOldConfig.BasedDistance;
            alignmentDeviceOffset.LaserF_Right_Locate = tempOldConfig.LaserF_Right_Locate;
            alignmentDeviceOffset.LaserB_Right_Locate = tempOldConfig.LaserB_Right_Locate;
            alignmentDeviceOffset.BarcodeReader_Right = new MapPosition(640, 512);

            try
            {
                if (!SetMessageAndWaitGoNext("請放置與雷射測距距離大約為180mm的治具(短的治具)後按下一步"))
                {
                    localData.LoadUnloadData.ResetAlignmentDeviceToZeroMessage = "人為停止";
                    return;
                }

                int maxRetryCount = 5;
                AlignmentValueData stage0Right = null;
                AlignmentValueData stage2Right = null;

                #region 取得短治具 StageNumber = 0 ( 180mm ).
                for (int i = 0; i < maxRetryCount; i++)
                {
                    CheckAlingmentValue(EnumStageDirection.Right, 0);
                    stage0Right = localData.LoadUnloadData.AlignmentValue;

                    if (stage0Right != null && stage0Right.AlignmentVlaue)
                        break;
                    else if (localData.LoadUnloadData.HomeStop)
                    {
                        alignmentDeviceOffset = tempOldConfig;
                        localData.LoadUnloadData.Homing = false;
                        localData.LoadUnloadData.ResetZero = false;
                        return;
                    }
                }

                if (stage0Right == null || !stage0Right.AlignmentVlaue)
                {
                    alignmentDeviceOffset = tempOldConfig;
                    localData.LoadUnloadData.Homing = false;
                    localData.LoadUnloadData.ResetZero = false;
                    localData.LoadUnloadData.ResetAlignmentDeviceToZeroMessage = "取不到補正資料";
                    return;
                }
                #endregion

                if (!SetMessageAndWaitGoNext("請放置與雷射測距距離大約為380mm的治具(長的治具)後按下一步"))
                {
                    localData.LoadUnloadData.ResetAlignmentDeviceToZeroMessage = "人為停止";
                    return;
                }

                #region 取得短治具 StageNumber = 2 ( 380mm ).
                for (int i = 0; i < maxRetryCount; i++)
                {
                    CheckAlingmentValue(EnumStageDirection.Right, 2);
                    stage2Right = localData.LoadUnloadData.AlignmentValue;

                    if (stage2Right != null && stage2Right.AlignmentVlaue)
                        break;
                    else if (localData.LoadUnloadData.HomeStop)
                    {
                        alignmentDeviceOffset = tempOldConfig;
                        localData.LoadUnloadData.Homing = false;
                        localData.LoadUnloadData.ResetZero = false;
                        return;
                    }
                }

                if (stage2Right == null || !stage2Right.AlignmentVlaue)
                {
                    alignmentDeviceOffset = tempOldConfig;
                    localData.LoadUnloadData.Homing = false;
                    localData.LoadUnloadData.ResetZero = false;
                    localData.LoadUnloadData.ResetAlignmentDeviceToZeroMessage = "取不到補正資料";
                    return;
                }
                #endregion

                string logMessage = String.Concat("\r\nLaser F : 180 = ", stage0Right.LaserF_Origin.ToString("0.000"),
                                                              ", 380 = ", stage2Right.LaserF_Origin.ToString("0.000"),
                                                              ", Delta = ", (stage2Right.LaserF_Origin - stage0Right.LaserF_Origin).ToString("0.000"),
                                                           "\r\nLaser B : 180 = ", stage0Right.LaserB_Origin.ToString("0.000"),
                                                                       ", 380 = ", stage2Right.LaserB_Origin.ToString("0.000"),
                                                                       ", Delta = ", (stage2Right.LaserB_Origin - stage0Right.LaserB_Origin).ToString("0.000"));
                WriteLog(7, "", logMessage);

                WriteLog(7, "", String.Concat("Alignment 0 Data : \r\n\tOrigin(X,Y) = ( ", stage0Right.OriginBarodePosition.X.ToString("0"), ", ", stage0Right.OriginBarodePosition.Y.ToString("0"), " )",
                                              "\r\n\tBarcode(X,Y) = ( ", stage0Right.BarcodePosition.X.ToString("0.00"), ", ", stage0Right.BarcodePosition.Y.ToString("0.00"), " )"));

                WriteLog(7, "", String.Concat("Alignment 2 Data : \r\n\tOrigin(X,Y) = ( ", stage2Right.OriginBarodePosition.X.ToString("0"), ", ", stage2Right.OriginBarodePosition.Y.ToString("0"), " )",
                                              "\r\n\tBarcode(X,Y) = ( ", stage2Right.BarcodePosition.X.ToString("0.00"), ", ", stage2Right.BarcodePosition.Y.ToString("0.00"), " )"));

                alignmentDeviceOffset.BarcodeReader_Right.X = stage0Right.OriginBarodePosition.X;
                alignmentDeviceOffset.BarcodeReader_Right.Y = stage0Right.OriginBarodePosition.Y;

                alignmentDeviceOffset.BarcodeReader_Right_X_Delta = stage2Right.OriginBarodePosition.X - stage0Right.OriginBarodePosition.X;
                alignmentDeviceOffset.BarcodeReader_Right_Y_Delta = stage2Right.OriginBarodePosition.Y - stage0Right.OriginBarodePosition.Y;

                alignmentDeviceOffset.LaserF_Right = stage0Right.LaserF_Origin - 180;
                alignmentDeviceOffset.LaserF_Right_Offset = (stage2Right.LaserF_Origin - 380) - alignmentDeviceOffset.LaserF_Right;

                alignmentDeviceOffset.LaserB_Right = stage0Right.LaserB_Origin - 180;
                alignmentDeviceOffset.LaserB_Right_Offset = (stage2Right.LaserB_Origin - 380) - alignmentDeviceOffset.LaserB_Right;

                WriteAlignmentDeviceOffset();

                localData.LoadUnloadData.Homing = false;
                localData.LoadUnloadData.ResetZero = false;
                localData.LoadUnloadData.ResetAlignmentDeviceToZeroMessage = "Barcode角度/Laser長度基準/BarcodeX位置調整完畢";
            }
            catch (Exception ex)
            {
                WriteLog(5, "", String.Concat("Exception : ", ex.ToString()));
                alignmentDeviceOffset = tempOldConfig;
                localData.LoadUnloadData.Homing = false;
                localData.LoadUnloadData.ResetZero = false;
                localData.LoadUnloadData.ResetAlignmentDeviceToZeroMessage = "異常結束";
            }
        }

        public override void SetAlignmentDeviceToZero(EnumStageDirection direction)
        {
            if (localData.LoginLevel >= EnumLoginLevel.Admin && localData.AutoManual == EnumAutoState.Manual &&
                localData.MoveControlData.MoveCommand == null && localData.LoadUnloadData.LoadUnloadCommand == null &&
                !localData.MoveControlData.MotionControlData.JoystickMode && !localData.MIPCData.Charging &&
                !localData.LoadUnloadData.Homing)
            {
                if (homeThread == null || !homeThread.IsAlive)
                {
                    localData.LoadUnloadData.ResetZero = true;
                    localData.LoadUnloadData.Homing = true;
                    localData.LoadUnloadData.HomeStop = false;
                    homeThread = new Thread(SetAlignmentDeviceToZeroThread);
                    homeThread.Start();
                }
                else
                    localData.LoadUnloadData.ResetAlignmentDeviceToZeroMessage = "不符合調整條件";
            }
            else
                localData.LoadUnloadData.ResetAlignmentDeviceToZeroMessage = "不符合調整條件";
        }

        #region 回Home流程.
        public override void Home()
        {
            if (localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null &&
                localData.LoadUnloadData.LoadUnloadCommand == null && !localData.LoadUnloadData.Homing &&
                !CheckAxisError(EnumLoadUnloadAxisName.Z軸) && !CheckAxisError(EnumLoadUnloadAxisName.Z軸_Slave) &&
                !CheckAxisError(EnumLoadUnloadAxisName.P軸) && !CheckAxisError(EnumLoadUnloadAxisName.Theta軸) &&
                !CheckAxisError(EnumLoadUnloadAxisName.Roller) &&
                !localData.LoadUnloadData.Z軸主從誤差過大)
            {
                if (!reconnectedAndResetAxisError)
                {
                    WriteLog(7, "", "Home");
                    localData.LoadUnloadData.Homing = true;
                    localData.LoadUnloadData.HomeStop = false;
                    homeThread = new Thread(HomeThread);
                    homeThread.Start();
                }
            }
        }

        public override void Home_Initial()
        {
            if (localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null &&
                localData.LoadUnloadData.LoadUnloadCommand == null && !localData.LoadUnloadData.Homing &&
                !CheckAxisError(EnumLoadUnloadAxisName.Z軸) && !CheckAxisError(EnumLoadUnloadAxisName.Z軸_Slave) &&
                !CheckAxisError(EnumLoadUnloadAxisName.P軸) && !CheckAxisError(EnumLoadUnloadAxisName.Theta軸) &&
                !CheckAxisError(EnumLoadUnloadAxisName.Roller) &&
                !localData.LoadUnloadData.Z軸主從誤差過大)
            {
                if (!reconnectedAndResetAxisError)
                {
                    WriteLog(7, "", "Home-Initial");
                    mipcControl.SendMIPCDataByIPCTagName(
                    new List<EnumMecanumIPCdefaultTag>() { EnumMecanumIPCdefaultTag.Encoder已回Home },
                    new List<float>() { 0 });

                    localData.LoadUnloadData.z軸EncoderHome = false;
                    localData.LoadUnloadData.p軸EncoderHome = false;
                    localData.LoadUnloadData.theta軸EncoderHome = false;
                    localData.LoadUnloadData.Homing = true;
                    localData.LoadUnloadData.HomeStop = false;
                    homeThread = new Thread(HomeThread);
                    homeThread.Start();
                }
            }
        }

        private double homeTimeout = 10000000;

        private bool HomeJog(string axisNameString, bool dirFlag, double speed)
        {
            if (AxisJog(axisNameString, dirFlag, speed))
                return true;
            else
            {
                AxisNameStop(axisNameString);
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                localData.LoadUnloadData.Homing = false;
                SetFindHome(axisNameString, false);
                return false;
            }
        }

        private bool HomeStopOrNoPower(string axisNameString)
        {
            if (localData.LoadUnloadData.HomeStop)
            {
                AxisNameStop(axisNameString);
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                localData.LoadUnloadData.Homing = false;
                return true;
            }
            else if (!localData.MIPCData.U動力電)
            {
                AxisNameStop(axisNameString);
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                localData.LoadUnloadData.Homing = false;
                return true;
            }
            else if (CheckAxisError(EnumLoadUnloadAxisName.Z軸) || CheckAxisError(EnumLoadUnloadAxisName.Z軸_Slave) &&
                     CheckAxisError(EnumLoadUnloadAxisName.P軸) || CheckAxisError(EnumLoadUnloadAxisName.Theta軸) &&
                     CheckAxisError(EnumLoadUnloadAxisName.Roller))
            {
                AxisNameStop(axisNameString);
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                localData.LoadUnloadData.Homing = false;
                return true;
            }
            else
                return false;
        }

        #region bool JogToPosLimit(string axisNameString)
        private bool JogToPosLimit(string axisNameString)
        {
            if (!HomeJog(axisNameString, true, axisDataList[axisNameString].HomeVelocity_High))
                return false;

            Stopwatch timer = new Stopwatch();
            timer.Restart();

            while (!AllAxisSensorData[axisNameString][axisPosLimitSensor[axisNameString]].Data)
            {
                if (GetMoveStatusByCVAxisName(axisNameString) == EnumAxisMoveStatus.Stop)
                {
                    Thread.Sleep(100);
                    break;
                }
                else if (timer.ElapsedMilliseconds > homeTimeout)
                {
                    AxisNameStop(axisNameString);
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                    localData.LoadUnloadData.Homing = false;
                    return false;
                }
                else if (HomeStopOrNoPower(axisNameString))
                    return false;

                Thread.Sleep(sleepTime);
            }

            if (!AxisNameStop(axisNameString))
            {
                AxisNameStop(axisNameString);
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                localData.LoadUnloadData.Homing = false;
                SetFindHome(axisNameString, false);
                return false;
            }

            if (AllAxisSensorData[axisNameString][axisPosLimitSensor[axisNameString]].Data)
                return true;
            else
            {
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_極限Sensor未On);
                localData.LoadUnloadData.Homing = false;
                return false;
            }
        }
        #endregion

        private bool SetFindHome(string axisNameString, bool onOff)
        {
            if (mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.Home] }, new List<float>() { (onOff ? 1 : 0) }))
                return true;
            else
            {
                AxisNameStop(axisNameString);
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                localData.LoadUnloadData.Homing = false;
                mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.Home] }, new List<float>() { 0 });

                return false;
            }
        }

        #region bool GoHome(string axisNameString)
        private bool GoHome(string axisNameString, double speedPercent = 0.5)
        {
            AxisFeedbackData temp = localData.LoadUnloadData.CVFeedbackData[axisNameString];

            if (logMode)
                WriteLog(7, "", "下p to p命令");

            if (!CVPtoP(axisNameString, 0, speedPercent, false, 0))
            {
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                localData.LoadUnloadData.Homing = false;
                return false;
            }

            Stopwatch timer = new Stopwatch();
            timer.Restart();

            if (logMode)
                WriteLog(7, "", String.Concat("Axis : ", axisNameString, "開始等待Stop訊號"));

            while (GetMoveStatusByCVAxisName(axisNameString) != EnumAxisMoveStatus.Stop)
            {
                if (timer.ElapsedMilliseconds > homeTimeout)
                {
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                    localData.LoadUnloadData.Homing = false;
                    return false;
                }
                else if (HomeStopOrNoPower(axisNameString))
                    return false;

                Thread.Sleep(sleepTime);
            }

            if (logMode)
                WriteLog(7, "", String.Concat("Axis : ", axisNameString, "等待結束"));

            if (AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data)
            {
                return true;
            }
            else
            {
                if (logMode)
                    WriteLog(7, "", String.Concat("Axis : ", axisNameString, ", SensorName : ", axisHomeSensor[axisNameString],
                                                ", Data : ", AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data.ToString()));

                JogStop();
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_HomeSensor未on);
                localData.LoadUnloadData.Homing = false;
                return false;
            }
        }
        #endregion

        #region bool FindHome(string axisNameString)
        private bool FindHome(string axisNameString)
        {
            Stopwatch timer = new Stopwatch();

            if (AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data)
            {
                #region Step2-往正方向慢速移動3秒.
                WriteLog(7, "", String.Concat(axisNameString, " 目前在HomeSensor, 快速往外拉後在慢速找一次"));

                if (!HomeJog(axisNameString, true, axisDataList[axisNameString].HomeVelocity_High))
                    return false;

                timer.Restart();

                #region 等待HomeSensorOff.
                WriteLog(7, "", String.Concat(axisNameString, "等待HomeSensor off"));

                while (AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data)
                {
                    if (timer.ElapsedMilliseconds > homeTimeout)
                    {
                        AxisNameStop(axisNameString);
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }
                    else if (HomeStopOrNoPower(axisNameString))
                        return false;

                    Thread.Sleep(sleepTime);
                }
                #endregion

                WriteLog(7, "", String.Concat(axisNameString, "HomeSensor off"));

                if (!AxisNameStop(axisNameString))
                {
                    AxisNameStop(axisNameString);
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }
                #endregion
            }
            else
            {
                #region Step1-快速找HomeSensor.
                WriteLog(7, "", String.Concat(axisNameString, " 回Home, 先快速找HomeSensor"));

                if (!SetFindHome(axisNameString, true) || !HomeJog(axisNameString, false, axisDataList[axisNameString].HomeVelocity_High))
                {
                    SetFindHome(axisNameString, false);
                    return false;
                }

                WriteLog(7, "", String.Concat(axisNameString, " 等待Stop(HomeSensor on )"));

                timer.Restart();

                while (!AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data)
                {
                    if (GetMoveStatusByCVAxisName(axisNameString) == EnumAxisMoveStatus.Stop)
                    {
                        Thread.Sleep(100);
                        break;
                    }
                    else if (timer.ElapsedMilliseconds > homeTimeout)
                    {
                        AxisNameStop(axisNameString);
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }
                    else if (HomeStopOrNoPower(axisNameString))
                    {
                        SetFindHome(axisNameString, false);
                        return false;
                    }

                    Thread.Sleep(sleepTime);
                }

                if (AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data)
                {
                    while (localData.LoadUnloadData.CVFeedbackData[axisNameString].HM1 != 0)
                    {
                        if (HomeStopOrNoPower(axisNameString))
                        {
                            SetFindHome(axisNameString, false);
                            return false;
                        }

                        Thread.Sleep(sleepTime);
                    }
                }
                else
                {
                    AxisNameStop(axisNameString);
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_HomeSensor未on);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                AxisNameStop(axisNameString);
                #endregion

                #region Step2-往正方向慢速移動.
                if (logMode)
                    WriteLog(7, "", String.Concat(axisNameString, " 找到HomeSensor, 慢速往外拉3秒後在慢速找一次"));

                if (!HomeJog(axisNameString, true, axisDataList[axisNameString].HomeVelocity))
                {
                    SetFindHome(axisNameString, false);
                    return false;
                }

                timer.Restart();

                #region 等待HomeSensorOff.
                if (logMode)
                    WriteLog(7, "", String.Concat(axisNameString, " 等待HomeSensor off"));

                while (AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data)
                {
                    if (timer.ElapsedMilliseconds > homeTimeout)
                    {
                        AxisNameStop(axisNameString);
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }
                    else if (HomeStopOrNoPower(axisNameString))
                    {
                        SetFindHome(axisNameString, false);
                        return false;
                    }

                    Thread.Sleep(sleepTime);
                }

                if (AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data)
                {
                    AxisNameStop(axisNameString);
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_HomeSensor未off);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }
                #endregion

                if (!AxisNameStop(axisNameString))
                {
                    AxisNameStop(axisNameString);
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }
                #endregion
            }

            #region Step3-慢速找Home偏正方向的交界點.
            WriteLog(7, "", String.Concat(axisNameString, " 慢速找Home偏正方向的交界點"));

            if (!SetFindHome(axisNameString, true) || !HomeJog(axisNameString, false, axisDataList[axisNameString].HomeVelocity))
            {
                SetFindHome(axisNameString, false);
                return false;
            }

            timer.Restart();

            while (!AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data)
            {
                if (GetMoveStatusByCVAxisName(axisNameString) == EnumAxisMoveStatus.Stop)
                {
                    Thread.Sleep(100);
                    break;
                }
                else if (timer.ElapsedMilliseconds > homeTimeout)
                {
                    AxisNameStop(axisNameString);
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }
                else if (HomeStopOrNoPower(axisNameString))
                {
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(sleepTime);
            }

            if (!AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data)
            {
                AxisNameStop(axisNameString);
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_HomeSensor未on);
                localData.LoadUnloadData.Homing = false;
                SetFindHome(axisNameString, false);
                return false;
            }

            while (localData.LoadUnloadData.CVFeedbackData[axisNameString].HM1 != 0)
            {
                if (HomeStopOrNoPower(axisNameString))
                {
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(sleepTime);
            }

            WriteLog(7, "", String.Concat(axisNameString, " 快速Encoder 差距 : ", localData.LoadUnloadData.CVFeedbackData[axisNameString].HM7.ToString("0.0000")));

            if (!AxisNameStop(axisNameString))
            {
                AxisNameStop(axisNameString);
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                localData.LoadUnloadData.Homing = false;
                SetFindHome(axisNameString, false);
                return false;
            }
            #endregion

            #region Step4-往負方向移動,直到到達HomeSensor外.
            WriteLog(7, "", String.Concat(axisNameString, " 往負方向移動,直到到達HomeSensor外"));

            if (!HomeJog(axisNameString, false, axisDataList[axisNameString].HomeVelocity_High))
            {
                SetFindHome(axisNameString, false);
                return false;
            }

            timer.Restart();

            #region 先移動1秒.
            WriteLog(7, "", String.Concat(axisNameString, "先移動1秒"));

            while (timer.ElapsedMilliseconds < 1000)
            {
                if (HomeStopOrNoPower(axisNameString))
                {
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(sleepTime);
            }
            #endregion

            #region 等待HomeSensorOff.
            WriteLog(7, "", String.Concat(axisNameString, "等待HomeSensor off"));

            while (AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data &&
                   GetMoveStatusByCVAxisName(axisNameString) != EnumAxisMoveStatus.Stop)
            {
                if (timer.ElapsedMilliseconds > homeTimeout)
                {
                    AxisNameStop(axisNameString);
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }
                else if (HomeStopOrNoPower(axisNameString))
                {
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(sleepTime);
            }

            if (AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data)
            {
                AxisNameStop(axisNameString);
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_HomeSensor未off);
                localData.LoadUnloadData.Homing = false;
                SetFindHome(axisNameString, false);
                return false;
            }
            #endregion
            #endregion

            #region Step5-慢速找Home偏向負方向的交界點.
            WriteLog(7, "", String.Concat(axisNameString, "慢速找Home偏向負方向的交界點"));

            if (!SetFindHome(axisNameString, true) || !HomeJog(axisNameString, true, axisDataList[axisNameString].HomeVelocity))
            {
                SetFindHome(axisNameString, false);
                return false;
            }

            timer.Restart();

            while (!AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data &&
                   GetMoveStatusByCVAxisName(axisNameString) != EnumAxisMoveStatus.Stop)
            {
                if (timer.ElapsedMilliseconds > homeTimeout)
                {
                    AxisNameStop(axisNameString);
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }
                else if (HomeStopOrNoPower(axisNameString))
                {
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(sleepTime);
            }

            if (!AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data)
            {
                AxisNameStop(axisNameString);
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_HomeSensor未on);
                localData.LoadUnloadData.Homing = false;
                SetFindHome(axisNameString, false);
                return false;
            }

            while (localData.LoadUnloadData.CVFeedbackData[axisNameString].HM1 != 0)
            {
                if (HomeStopOrNoPower(axisNameString))
                {
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(sleepTime);
            }

            double length = Math.Abs(localData.LoadUnloadData.CVFeedbackData[axisNameString].HM7);

            if (logMode)
                WriteLog(7, "", String.Concat(axisNameString, "HomeSensor 長度 : ", length));

            if (!AxisNameStop(axisNameString))
            {
                AxisNameStop(axisNameString);
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                localData.LoadUnloadData.Homing = false;
                SetFindHome(axisNameString, false);
                return false;
            }

            localData.LoadUnloadData.CVEncoderOffsetValue[axisNameString] = length / 2;
            localData.LoadUnloadData.CVHomeOffsetValue[axisNameString] = axisDataList[axisNameString].HomeOffset;
            SetFindHome(axisNameString, false);
            #endregion

            #region Step6-GoHome.
            if (logMode)
                WriteLog(7, "", String.Concat(axisNameString, " GoHome"));

            return GoHome(axisNameString);
            #endregion
        }

        #endregion

        public Thread homeThread = null;

        private bool z軸Home = false;                 //inPosition 
        private bool p軸Home = false;                 //inPosition 
        private bool theta軸Home = false;             //inPosition 

        private DataDelayAndChange cv_Normal = new DataDelayAndChange(1000, EnumDelayType.OnDelay);
        //private bool cv_Normal = false;             //inPosition 

        private void AxisFindHome(string axisNameString)
        {
            if (!AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data)
            {
                WriteLog(7, "", String.Concat(axisNameString, "碰正極限Sensor"));

                if (!JogToPosLimit(axisNameString))
                    return;

                WriteLog(7, "", String.Concat(axisNameString, "碰正極限Sensor完成"));
            }

            WriteLog(7, "", String.Concat(axisNameString, "找原點"));

            if (!FindHome(axisNameString.ToString()))
                return;

            if (axisNameString == EnumLoadUnloadAxisName.P軸.ToString())
                localData.LoadUnloadData.p軸EncoderHome = true;
            else if (axisNameString == EnumLoadUnloadAxisName.Theta軸.ToString())
                localData.LoadUnloadData.theta軸EncoderHome = true;
            else if (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString())
                localData.LoadUnloadData.z軸EncoderHome = true;

            WriteLog(7, "", String.Concat(axisNameString, "找原點完成"));
        }

        private void P軸回HomeThread()
        {
            AxisFindHome(EnumLoadUnloadAxisName.P軸.ToString());
        }

        private void Theta回HomeThread()
        {
            AxisFindHome(EnumLoadUnloadAxisName.Theta軸.ToString());
        }

        #region 回HomeThread.
        private void ZHome()
        {
            if (logMode)
                WriteLog(7, "", "找Z軸原點");

            if (!FindHome(EnumLoadUnloadAxisName.Z軸.ToString()))
                return;

            Thread.Sleep(500);
            localData.LoadUnloadData.CVHomeOffsetValue[EnumLoadUnloadAxisName.Z軸_Slave.ToString()] = 0;

            localData.LoadUnloadData.CVEncoderOffsetValue[EnumLoadUnloadAxisName.Z軸_Slave.ToString()] =
                      localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()].OriginPosition;

            Thread.Sleep(500);
            localData.LoadUnloadData.z軸EncoderHome = true;
        }

        private void PThetaHome()
        {
            if (!localData.LoadUnloadData.p軸EncoderHome &&
                AllAxisSensorData[EnumLoadUnloadAxisName.P軸.ToString()][axisHomeSensor[EnumLoadUnloadAxisName.P軸.ToString()]].Data &&
                !localData.LoadUnloadData.theta軸EncoderHome &&
                AllAxisSensorData[EnumLoadUnloadAxisName.Theta軸.ToString()][axisHomeSensor[EnumLoadUnloadAxisName.Theta軸.ToString()]].Data)
            {
                if (logMode)
                    WriteLog(7, "", "P/Theta同時回Home");

                Thread p軸HomeThread = new Thread(P軸回HomeThread);
                Thread thet軸HomeThred = new Thread(Theta回HomeThread);

                p軸HomeThread.Start();
                thet軸HomeThred.Start();

                while (p軸HomeThread.IsAlive || thet軸HomeThred.IsAlive)
                    Thread.Sleep(500);

                if (!localData.LoadUnloadData.p軸EncoderHome || !localData.LoadUnloadData.theta軸EncoderHome)
                {
                    localData.LoadUnloadData.Homing = false;
                    return;
                }
            }
            else
            {
                if (!localData.LoadUnloadData.p軸EncoderHome)
                {
                    P軸回HomeThread();

                    if (!localData.LoadUnloadData.p軸EncoderHome)
                    {
                        localData.LoadUnloadData.Homing = false;
                        return;
                    }
                }
                else
                {
                    if (!GoHome(EnumLoadUnloadAxisName.P軸.ToString(), 1))
                        return;
                }

                if (!localData.LoadUnloadData.theta軸EncoderHome)
                {
                    Theta回HomeThread();

                    if (!localData.LoadUnloadData.theta軸EncoderHome)
                    {
                        localData.LoadUnloadData.Homing = false;
                        return;
                    }
                }
                else
                {
                    if (!GoHome(EnumLoadUnloadAxisName.Theta軸.ToString(), 1))
                        return;
                }
            }
        }

        private void HomeThread()
        {
            try
            {
                SetFindHome(EnumLoadUnloadAxisName.Z軸.ToString(), false);
                SetFindHome(EnumLoadUnloadAxisName.P軸.ToString(), false);
                SetFindHome(EnumLoadUnloadAxisName.Theta軸.ToString(), false);
                Stopwatch timer = new Stopwatch();

                JogStop();

                #region CST卡在一半.
                bool cstInCV = AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV入料].Data ||
                               AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV減速].Data ||
                               AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV停止].Data;

                if (cstInCV)
                {
                    WriteLog(7, "", "回Home流程, CV上有CST");

                    if (AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV減速].Data &&
                        AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV停止].Data)
                        WriteLog(7, "", "CV減速、CV停止 Sensor on");
                    else
                    {
                        WriteLog(7, "", "CST 不在停止點");

                        if (AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV停止].Data)
                        {
                            WriteLog(7, "", "CST 不在停止點, 但是CV停止卻on, 異常");
                            SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_CVSensor狀態異常);
                            localData.LoadUnloadData.Homing = false;
                            return;
                        }

                        WriteLog(7, "", "CST 不在CV安全位置, 開始CST回CV流程");
                        WriteLog(7, "", "CV Servo on Roller");

                        if (!ServoOn(EnumLoadUnloadAxisName.Roller.ToString(), true, 2000))
                        {
                            SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_ServoOn_Timeout);
                            localData.LoadUnloadData.Homing = false;
                            return;
                        }

                        #region Stop訊號.
                        if (localData.LoadUnloadData.HomeStop)
                        {
                            JogStop();
                            SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                            localData.LoadUnloadData.Homing = false;
                            return;
                        }
                        #endregion

                        #region CST 回 CV.
                        timer.Restart();

                        WriteLog(7, "", "下Roller迴轉命令");

                        if (!AxisJog(EnumLoadUnloadAxisName.Roller.ToString(), true, axisDataList[EnumLoadUnloadAxisName.Roller.ToString()].HomeVelocity_High))
                        {
                            JogStop();
                            SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                            localData.LoadUnloadData.Homing = false;
                            return;
                        }

                        WriteLog(7, "", "等待CV停止 On");

                        while (!AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV停止].Data &&
                               GetMoveStatusByCVAxisName(EnumLoadUnloadAxisName.Roller.ToString()) != EnumAxisMoveStatus.Stop)
                        {
                            if (timer.ElapsedMilliseconds > 20000)
                            {
                                JogStop();
                                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_CST回CV_Timeout);
                                localData.LoadUnloadData.Homing = false;
                                return;
                            }

                            #region Stop訊號.
                            if (localData.LoadUnloadData.HomeStop)
                            {
                                JogStop();
                                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                                localData.LoadUnloadData.Homing = false;
                                return;
                            }
                            #endregion

                            Thread.Sleep(sleepTime);
                        }

                        WriteLog(7, "", "CV停止 On");

                        JogStop();

                        if (AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV停止].Data &&
                            AllAxisSensorData[EnumLoadUnloadAxisName.Roller.ToString()][CV減速].Data)
                            WriteLog(7, "", "CST 回 CV 完成");
                        else
                        {
                            WriteLog(7, "", "狀態應該為CV停止、CV減速 on, CV入料 off, 異常");
                            SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_CVSensor狀態異常);
                            localData.LoadUnloadData.Homing = false;
                            return;
                        }
                        #endregion
                    }
                }
                #endregion

                #region Stop訊號.
                if (localData.LoadUnloadData.HomeStop)
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                    localData.LoadUnloadData.Homing = false;
                    return;
                }
                #endregion

                #region ServoOn All.
                WriteLog(7, "", "CV Servo on all");

                if (!ServoOn(EnumLoadUnloadAxisName.Roller.ToString(), true, 2000) || !ServoOn(EnumLoadUnloadAxisName.Z軸.ToString(), true, 2000) ||
                    !ServoOn(EnumLoadUnloadAxisName.Theta軸.ToString(), true, 2000) || !ServoOn(EnumLoadUnloadAxisName.P軸.ToString(), true, 2000))
                {
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_ServoOn_Timeout);
                    localData.LoadUnloadData.Homing = false;
                    return;
                }
                #endregion

                WriteLog(7, "", "Servo on all End");

                #region Stop訊號.
                if (localData.LoadUnloadData.HomeStop)
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                    localData.LoadUnloadData.Homing = false;
                    return;
                }
                #endregion

                if (localData.LoadUnloadData.z軸EncoderHome && localData.LoadUnloadData.p軸EncoderHome && localData.LoadUnloadData.theta軸EncoderHome)
                {
                    WriteLog(7, "", "因三軸Encoder回Home已做過, 因此直接回Home");

                    if (!GoHome(EnumLoadUnloadAxisName.P軸.ToString(), 1) ||
                        !GoHome(EnumLoadUnloadAxisName.Theta軸.ToString(), 1) ||
                        !GoHome(EnumLoadUnloadAxisName.Z軸.ToString(), 1))
                        return;
                }
                else
                {
                    if (AllAxisSensorData[EnumLoadUnloadAxisName.Z軸.ToString()][axisPosLimitSensor[EnumLoadUnloadAxisName.Z軸.ToString()]].Data ||
                        AllAxisSensorData[EnumLoadUnloadAxisName.Z軸.ToString()][Z軸上位].Data)
                        WriteLog(7, "", "Z軸在 上定位 or 上極限, 可以回Home");
                    else if (AllAxisSensorData[EnumLoadUnloadAxisName.Theta軸.ToString()][axisHomeSensor[EnumLoadUnloadAxisName.Theta軸.ToString()]].Data &&
                             AllAxisSensorData[EnumLoadUnloadAxisName.P軸.ToString()][axisHomeSensor[EnumLoadUnloadAxisName.P軸.ToString()]].Data)
                        WriteLog(7, "", "P軸、Theta軸在Home Sensor上, 可以回Home");
                    else
                    {
                        if (AllAxisSensorData[EnumLoadUnloadAxisName.P軸.ToString()][axisPosLimitSensor[EnumLoadUnloadAxisName.P軸.ToString()]].Data)
                        {
                            JogStop();
                            SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_Theta軸不再Home);
                            localData.LoadUnloadData.Homing = false;
                            return;
                        }
                        else if (AllAxisSensorData[EnumLoadUnloadAxisName.Theta軸.ToString()][axisPosLimitSensor[EnumLoadUnloadAxisName.Theta軸.ToString()]].Data)
                        {
                            JogStop();
                            SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_P軸不在Home);
                            localData.LoadUnloadData.Homing = false;
                            return;
                        }
                        else
                        {
                            JogStop();
                            SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_Z軸不在上定位);
                            localData.LoadUnloadData.Homing = false;
                            return;
                        }
                    }

                    if (AllAxisSensorData[EnumLoadUnloadAxisName.Z軸.ToString()][axisHomeSensor[EnumLoadUnloadAxisName.Z軸.ToString()]].Data)
                    { // z軸先回Home.
                        ZHome();

                        if (!localData.LoadUnloadData.z軸EncoderHome)
                        {
                            localData.LoadUnloadData.Homing = false;
                            return;
                        }

                        if (!localData.LoadUnloadData.p軸EncoderHome ||
                            !localData.LoadUnloadData.theta軸EncoderHome)
                        {
                            if (!CVPtoP(EnumLoadUnloadAxisName.Z軸.ToString(), readCSTZEncoder, 1, false, 0))
                            {
                                AxisNameStop(EnumLoadUnloadAxisName.Z軸.ToString());
                                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                                localData.LoadUnloadData.Homing = false;
                                return;
                            }

                            while (GetMoveStatusByCVAxisName(EnumLoadUnloadAxisName.Z軸.ToString()) != EnumAxisMoveStatus.Stop)
                            {
                                if (HomeStopOrNoPower(EnumLoadUnloadAxisName.Z軸.ToString()))
                                {
                                    localData.LoadUnloadData.Homing = false;
                                    return;
                                }

                                Thread.Sleep(500);
                            }

                            if (!AllAxisSensorData[EnumLoadUnloadAxisName.Z軸.ToString()][axisPosLimitSensor[EnumLoadUnloadAxisName.Z軸.ToString()]].Data &&
                                !AllAxisSensorData[EnumLoadUnloadAxisName.Z軸.ToString()][Z軸上位].Data)
                            {
                                if (!JogToPosLimit(EnumLoadUnloadAxisName.Z軸.ToString()))
                                {
                                    localData.LoadUnloadData.Homing = false;
                                    return;
                                }
                            }

                            PThetaHome();

                            if (!localData.LoadUnloadData.p軸EncoderHome ||
                                !localData.LoadUnloadData.theta軸EncoderHome)
                            {
                                localData.LoadUnloadData.Homing = false;
                                return;
                            }

                            if (!GoHome(EnumLoadUnloadAxisName.Z軸.ToString(), 1))
                            {
                                localData.LoadUnloadData.Homing = false;
                                return;
                            }
                        }
                        else
                        {
                            if (!GoHome(EnumLoadUnloadAxisName.P軸.ToString(), 1))
                            {
                                localData.LoadUnloadData.Homing = false;
                                return;
                            }

                            if (!GoHome(EnumLoadUnloadAxisName.Theta軸.ToString(), 1))
                            {
                                localData.LoadUnloadData.Homing = false;
                                return;
                            }
                        }
                    }
                    else
                    {
                        WriteLog(7, "", "碰Z軸正極限");

                        if (!JogToPosLimit(EnumLoadUnloadAxisName.Z軸.ToString()))
                        {
                            localData.LoadUnloadData.Homing = false;
                            return;
                        }

                        WriteLog(7, "", "碰Z軸正極限結束");

                        PThetaHome();

                        if (!localData.LoadUnloadData.p軸EncoderHome ||
                            !localData.LoadUnloadData.theta軸EncoderHome)
                        {
                            localData.LoadUnloadData.Homing = false;
                            return;
                        }

                        ZHome();

                        if (!localData.LoadUnloadData.z軸EncoderHome)
                        {
                            localData.LoadUnloadData.Homing = false;
                            return;
                        }
                    }

                    mipcControl.SendMIPCDataByIPCTagName(
                        new List<EnumMecanumIPCdefaultTag>() {
                            EnumMecanumIPCdefaultTag.Z軸EncoderOffset, EnumMecanumIPCdefaultTag.Z軸_SlaveEncoderOffset,
                            EnumMecanumIPCdefaultTag.P軸EncoderOffset, EnumMecanumIPCdefaultTag.Theta軸EncoderOffset,
                            EnumMecanumIPCdefaultTag.Encoder已回Home},
                        new List<float>()
                        {
                            (float)localData.LoadUnloadData.CVEncoderOffsetValue[EnumLoadUnloadAxisName.Z軸.ToString()],
                            (float)localData.LoadUnloadData.CVEncoderOffsetValue[EnumLoadUnloadAxisName.Z軸_Slave.ToString()],
                            (float)localData.LoadUnloadData.CVEncoderOffsetValue[EnumLoadUnloadAxisName.P軸.ToString()],
                            (float)localData.LoadUnloadData.CVEncoderOffsetValue[EnumLoadUnloadAxisName.Theta軸.ToString()],
                            1
                        });
                }

                if (localData.MainFlowConfig.PowerSavingMode)
                {
                    ServoOff(EnumLoadUnloadAxisName.Z軸.ToString(), true, 3000);
                    ServoOff(EnumLoadUnloadAxisName.Roller.ToString(), true, 3000);
                }

                localData.LoadUnloadData.Homing = false;
            }
            catch (Exception ex)
            {
                JogStop();
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_Exception);
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                localData.LoadUnloadData.Homing = false;
            }
        }
        #endregion
        #endregion

        #region 找尋HomeSensor Offset.
        public override void FindHomeSensorOffsetByEncoderInHome()
        {
            if (localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null &&
                localData.LoadUnloadData.LoadUnloadCommand == null && !localData.LoadUnloadData.Homing)
            {
                localData.LoadUnloadData.Homing = true;
                localData.LoadUnloadData.HomeStop = false;
                homeThread = new Thread(FindHomeSensorOffsetByEncoderInHomeThread);
                homeThread.Start();
            }
        }

        private bool FindHomeSensoroffsetByEncoderByAxisName(string axisNameString)
        {
            Stopwatch timer = new Stopwatch();
            Thread.Sleep(1000);
            double nowEncoder = localData.LoadUnloadData.CVFeedbackData[axisNameString].OriginPosition;

            double length_HomeToHomePos;
            double length_HomeSensor;
            double HomeSensor_Offset;

            #region Step1-往正方向移動,直到到達HomeSensor外.
            if (!AxisJog(axisNameString, true, axisDataList[axisNameString].HomeVelocity))
            {
                JogStop();
                localData.LoadUnloadData.Homing = false;
                return false;
            }

            timer.Restart();

            while (AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data)
            {
                if (timer.ElapsedMilliseconds > homeTimeout)
                {
                    JogStop();
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }
                else if (localData.LoadUnloadData.HomeStop)
                {
                    JogStop();
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(sleepTime);
            }

            timer.Restart();

            while (timer.ElapsedMilliseconds < 1000)
            {
                if (localData.LoadUnloadData.HomeStop)
                {
                    JogStop();
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(sleepTime);
            }

            if (!JogStop())
            {
                JogStop();
                localData.LoadUnloadData.Homing = false;
                SetFindHome(axisNameString, false);
                return false;
            }
            #endregion

            #region Step2-慢速找Home偏向正方向的交界點.
            if (!SetFindHome(axisNameString, true) || !AxisJog(axisNameString, false, 0.01))
            {
                JogStop();
                localData.LoadUnloadData.Homing = false;
                SetFindHome(axisNameString, false);
                return false;
            }

            timer.Restart();

            while (!AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data)
            {
                if (timer.ElapsedMilliseconds > homeTimeout)
                {
                    JogStop();
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }
                else if (localData.LoadUnloadData.HomeStop)
                {
                    JogStop();
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(sleepTime);
            }

            while (localData.LoadUnloadData.CVFeedbackData[axisNameString].HM1 != 0)
            {
                if (localData.LoadUnloadData.HomeStop)
                {
                    JogStop();
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(sleepTime);
            }
            #endregion

            length_HomeToHomePos = Math.Abs(localData.LoadUnloadData.CVFeedbackData[axisNameString].HM7 - nowEncoder);

            #region Step3-往負方向移動,直到到達HomeSensor外.
            if (!AxisJog(axisNameString, false, axisDataList[axisNameString].HomeVelocity))
            {
                JogStop();
                localData.LoadUnloadData.Homing = false;
                SetFindHome(axisNameString, false);
                return false;
            }

            timer.Restart();

            while (timer.ElapsedMilliseconds < 1000)
            {
                if (localData.LoadUnloadData.HomeStop)
                {
                    JogStop();
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(sleepTime);
            }

            while (AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data)
            {
                if (timer.ElapsedMilliseconds > homeTimeout)
                {
                    JogStop();
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }
                else if (localData.LoadUnloadData.HomeStop)
                {
                    JogStop();
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(sleepTime);
            }

            timer.Restart();

            while (timer.ElapsedMilliseconds < 1000)
            {
                if (localData.LoadUnloadData.HomeStop)
                {
                    JogStop();
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(sleepTime);
            }

            if (!JogStop())
            {
                JogStop();
                localData.LoadUnloadData.Homing = false;
                SetFindHome(axisNameString, false);
                return false;
            }
            #endregion

            #region Step4-慢速找Home偏向負方向的交界點.
            if (!SetFindHome(axisNameString, true) || !AxisJog(axisNameString, true, 0.01))
            {
                JogStop();
                localData.LoadUnloadData.Homing = false;
                SetFindHome(axisNameString, false);
                return false;
            }

            timer.Restart();

            while (!AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data)
            {
                if (timer.ElapsedMilliseconds > homeTimeout)
                {
                    JogStop();
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }
                else if (localData.LoadUnloadData.HomeStop)
                {
                    JogStop();
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(sleepTime);
            }

            while (localData.LoadUnloadData.CVFeedbackData[axisNameString].HM1 != 0)
            {
                if (localData.LoadUnloadData.HomeStop)
                {
                    JogStop();
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(sleepTime);
            }
            #endregion

            length_HomeSensor = Math.Abs(localData.LoadUnloadData.CVFeedbackData[axisNameString].HM7);

            HomeSensor_Offset = -(length_HomeToHomePos - length_HomeSensor / 2);

            WriteLog(7, "", String.Concat("狗片長度 : ", length_HomeSensor.ToString("0.00"),
                                          ", 原點到YO距離 : ", length_HomeToHomePos.ToString("0.00"),
                                          ", HomeOffset : ", HomeSensor_Offset.ToString("0.00")));

            axisDataList[axisNameString].HomeOffset = HomeSensor_Offset;
            localData.LoadUnloadData.CVHomeOffsetValue[axisNameString] = HomeSensor_Offset;
            return true;
        }

        private void FindHomeSensorOffsetByEncoderInHomeThread()
        {
            try
            {
                localData.LoadUnloadData.p軸EncoderHome = false;
                localData.LoadUnloadData.z軸EncoderHome = false;
                localData.LoadUnloadData.theta軸EncoderHome = false;

                if (!ServoOn(EnumLoadUnloadAxisName.Theta軸.ToString(), true, 2000) ||
                    !ServoOn(EnumLoadUnloadAxisName.P軸.ToString(), true, 2000) ||
                    !ServoOn(EnumLoadUnloadAxisName.Z軸.ToString(), true, 2000))
                {
                    localData.LoadUnloadData.Homing = false;
                    return;
                }

                if (FindHomeSensoroffsetByEncoderByAxisName(EnumLoadUnloadAxisName.Z軸.ToString()) &&
                    JogToPosLimit(EnumLoadUnloadAxisName.Z軸.ToString()) &&
                    FindHomeSensoroffsetByEncoderByAxisName(EnumLoadUnloadAxisName.P軸.ToString()) &&
                    FindHomeSensoroffsetByEncoderByAxisName(EnumLoadUnloadAxisName.Theta軸.ToString()))
                {
                    WriteLog(7, "", "找HomeOffset成功");
                    WriteAxisData();
                }
                else
                    WriteLog(3, "", "找HomeOffset失敗");

                localData.LoadUnloadData.Homing = false;
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                localData.LoadUnloadData.Homing = false;
            }
        }
        #endregion

        public override void LoadUnloadPreAction()
        {
            if (localData.MainFlowConfig.PowerSavingMode)
            {
                ServoOn(EnumLoadUnloadAxisName.Z軸.ToString(), false, 0);
                ServoOn(EnumLoadUnloadAxisName.Roller.ToString(), false, 0);
            }
        }

        public override void SetAddressAlignmentOffset(string addressID)
        {
            try
            {
                AlignmentValueData alignmentValue = null;

                for (int i = 0; i < 3; i++)
                {
                    if (localData.TheMapInfo.AllAddress[addressID].LoadUnloadDirection != EnumStageDirection.None)
                        CheckAlingmentValue(localData.TheMapInfo.AllAddress[addressID].LoadUnloadDirection, localData.TheMapInfo.AllAddress[addressID].StageNumber);
                    else
                        CheckAlingmentValue(localData.TheMapInfo.AllAddress[addressID].ChargingDirection, localData.TheMapInfo.AllAddress[addressID].StageNumber);

                    alignmentValue = localData.LoadUnloadData.AlignmentValue;

                    if (alignmentValue != null && alignmentValue.AlignmentVlaue)
                        break;
                }

                if (alignmentValue != null && alignmentValue.AlignmentVlaue)
                {
                    double realP = localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position - RightStageDataList[localData.TheMapInfo.AllAddress[addressID].StageNumber].Encoder_P;

                    double realTheta = RightStageDataList[localData.TheMapInfo.AllAddress[addressID].StageNumber].Encoder_Theta - localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].Position;
                    realP = realP * Math.Cos(realTheta * Math.PI / 180);
                    double realZ = (RightStageDataList[localData.TheMapInfo.AllAddress[addressID].StageNumber].Encoder_Z -
                                    RightStageDataList[localData.TheMapInfo.AllAddress[addressID].StageNumber].Benchmark_Z) -
                                    localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].Position;

                    double 站點Offset基準P = alignmentValue.P - realP;
                    double 站點Offset基準Theta = alignmentValue.Theta - realTheta;
                    double 站點Offset基準Z = realZ;

                    AddressAlignmentValueOffset newOffset = new AddressAlignmentValueOffset();
                    newOffset.P = 站點Offset基準P;
                    newOffset.Theta = 站點Offset基準Theta;
                    newOffset.Z = 站點Offset基準Z;

                    if (AllAddressOffset.ContainsKey(addressID))
                        AllAddressOffset[addressID] = newOffset;
                    else
                        AllAddressOffset.Add(addressID, newOffset);

                    AllAddressOffsetChange = true;

                    WriteAddressOffsetCSV();
                }
            }
            catch (Exception ex)
            {
                WriteLog(5, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        private Dictionary<string, double> lastEC = new Dictionary<string, double>();
        private Dictionary<string, double> lastMF = new Dictionary<string, double>();

        #region CSV.
        public override void WriteCSV()
        {
            if (initialEnd)
            {
                string csvLog = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff");

                LoadUnloadCommandData temp = localData.LoadUnloadData.LoadUnloadCommand;
                csvLog = String.Concat(csvLog, ",", temp == null ? "" : temp.StepString);
                AxisFeedbackData axisData = null;
                string axisNameString = "";
                bool allOK = true;

                for (int i = 0; i < FeedbackAxisList.Count; i++)
                {
                    axisNameString = FeedbackAxisList[i];
                    axisData = localData.LoadUnloadData.CVFeedbackData[axisNameString];

                    if (axisData != null)
                    {
                        csvLog = String.Concat(csvLog, ",", axisData.Position.ToString("0.000"));
                        csvLog = String.Concat(csvLog, ",", axisData.Velocity.ToString("0.0"));
                        csvLog = String.Concat(csvLog, ",", axisData.AxisMoveStaus.ToString());

                        if (axisNameString == EnumLoadUnloadAxisName.Z軸_Slave.ToString())
                            csvLog = String.Concat(csvLog, ",", GetMoveStatusByCVAxisName(EnumLoadUnloadAxisName.Z軸.ToString()).ToString());
                        else
                            csvLog = String.Concat(csvLog, ",", GetMoveStatusByCVAxisName(axisNameString).ToString());

                        csvLog = String.Concat(csvLog, ",", axisData.AxisServoOnOff.ToString());
                        csvLog = String.Concat(csvLog, ",", axisData.V.ToString("0.000"));
                        csvLog = String.Concat(csvLog, ",", axisData.DA.ToString("0.000"));
                        csvLog = String.Concat(csvLog, ",", axisData.QA.ToString("0.000"));
                        csvLog = String.Concat(csvLog, ",", axisData.Temperature.ToString("0.000"));

                        #region EC.
                        if (lastEC.ContainsKey(axisNameString))
                        {
                            if (axisData.EC != lastEC[axisNameString])
                            {
                                WriteLog(5, "", String.Concat(axisNameString, " EC(Change) : ", axisData.EC.ToString("0")));
                                lastEC[axisNameString] = axisData.EC;
                            }
                        }
                        else
                        {
                            WriteLog(7, "", String.Concat(axisNameString, " EC(Initial) : ", axisData.EC.ToString("0")));
                            lastEC.Add(axisNameString, axisData.EC);
                        }
                        #endregion

                        #region MF.
                        if (lastMF.ContainsKey(axisNameString))
                        {
                            if (axisData.MF != lastMF[axisNameString])
                            {
                                WriteLog(5, "", String.Concat(axisNameString, " MF(Change) : ", axisData.MF.ToString("0")));
                                lastMF[axisNameString] = axisData.MF;
                            }
                        }
                        else
                        {
                            WriteLog(7, "", String.Concat(axisNameString, " MF(Initial) : ", axisData.MF.ToString("0")));
                            lastMF.Add(axisNameString, axisData.MF);
                        }
                        #endregion
                    }
                    else
                    {
                        allOK = false;
                        csvLog = String.Concat(csvLog, ",,,,,,,,,");
                    }
                }

                if (allOK)
                    logger.LogString(csvLog);
            }
        }
        #endregion
    }
}
