using Mirle.Agv.INX.Controller;
using Mirle.Agv.INX.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace Mirle.Agv.INX.Control
{
    public class LoadUnload_PTI : LoadUnload
    {
        private BarcodeReader_Keyence sr1000_R = null;
        private DistanceSensor_Keyence distanceSensor_R = null;

        private RFIDReader_OMRON cstIDReader = null;

        private bool alignmentDevicOK = false;

        private double delayTime = 0;

        private Dictionary<string, Dictionary<EnumLoadUnloadAxisCommandType, string>> axisCommandString = new Dictionary<string, Dictionary<EnumLoadUnloadAxisCommandType, string>>();

        private List<string> jogStopStringList = new List<string>();
        private List<float> jogStopValueList = new List<float>();

        private double loadUnloadZOffset = 1;

        private Dictionary<string, string> axisPosLimitSensor = new Dictionary<string, string>();
        private Dictionary<string, string> axisNagLimitSensor = new Dictionary<string, string>();
        private Dictionary<string, string> axisHomeSensor = new Dictionary<string, string>();


        private Dictionary<string, double> inPositionRange = new Dictionary<string, double>();

        private Dictionary<string, EnumAxisMoveStatus> axisPreStatus = new Dictionary<string, EnumAxisMoveStatus>();
        private Dictionary<string, Stopwatch> axisPreTimer = new Dictionary<string, Stopwatch>();
        private double axisStatusDelayTime = 1000;

        private bool logMode = true;

        private bool initialEnd = false;

        public override void Initial(MIPCControlHandler mipcControl, AlarmHandler alarmHandler)
        {
            this.alarmHandler = alarmHandler;
            this.mipcControl = mipcControl;
            device = MethodInfo.GetCurrentMethod().ReflectedType.Name;

            ReadAddressOffsetCSV();
            ReadLoadUnloadOffsetConfigXML();
            ReadStargeCSV();
            ReadAxisData();
            InitialRollerData();
            ReadStageNumberToBarcodeReaderSettingXML();
            CheckAllStringInMIPCConfig();

            RightPIO = new PIOFlow_PTI_LoadUnloadSemi();
            RightPIO.Initial(alarmHandler, mipcControl, "Right", "", normalLogName);
            CanLeftLoadUnload = true;
            CanRightLoadUnload = true;

            ConnectAlignmentDevice();
            initialEnd = true;
        }

        public override void CloseLoadUnload()
        {
        }

        #region Axis Sensor Name.
        private string P軸前定位 = "P軸前定位";
        private string P軸原點 = "P軸原點";
        private string P軸後定位 = "P軸後定位";
        private string Z軸上位檢知 = "Z軸上位檢知";
        private string Z軸下位檢知 = "Z軸下位檢知";
        private string Y軸前定位 = "S軸前定位";
        private string Y軸後定位 = "S軸後定位";
        private string θ軸前定位 = "θ軸前定位";
        private string θ軸原點 = "θ軸原點";
        private string θ軸後定位 = "θ軸後定位";
        private string 二重格檢知_L = "二重格檢知-F";
        private string 二重格檢知_R = "二重格檢知-B";
        private string CST座_在席檢知 = "CST座 FOUP 在席檢知";
        private string FORK_在席檢知 = "FORK FOUP 在席檢知";
        #endregion

        #region InitialRollerData.
        private void InitialRollerData()
        {
            CanPause = true;
            BreakenStepMode = true;
            HomeText = String.Concat("須滿足. ", "Y軸後定位", "On\r\n", "Theta軸原點", "On\r\n", "P軸原點", "On\r\n");
            axisPreStatus.Add(EnumLoadUnloadAxisName.Z軸.ToString(), EnumAxisMoveStatus.None);
            axisPreStatus.Add(EnumLoadUnloadAxisName.P軸.ToString(), EnumAxisMoveStatus.None);
            axisPreStatus.Add(EnumLoadUnloadAxisName.Theta軸.ToString(), EnumAxisMoveStatus.None);
            axisPreStatus.Add(EnumLoadUnloadAxisName.Y軸.ToString(), EnumAxisMoveStatus.None);

            axisPreTimer.Add(EnumLoadUnloadAxisName.Z軸.ToString(), new Stopwatch());
            axisPreTimer.Add(EnumLoadUnloadAxisName.P軸.ToString(), new Stopwatch());
            axisPreTimer.Add(EnumLoadUnloadAxisName.Theta軸.ToString(), new Stopwatch());
            axisPreTimer.Add(EnumLoadUnloadAxisName.Y軸.ToString(), new Stopwatch());

            inPositionRange.Add(EnumLoadUnloadAxisName.Z軸.ToString(), 5);
            inPositionRange.Add(EnumLoadUnloadAxisName.P軸.ToString(), 5);
            inPositionRange.Add(EnumLoadUnloadAxisName.Theta軸.ToString(), 10);
            inPositionRange.Add(EnumLoadUnloadAxisName.Y軸.ToString(), 0.1);

            FeedbackAxisList.Add(EnumLoadUnloadAxisName.Z軸.ToString());
            AxisList.Add(EnumLoadUnloadAxisName.Z軸.ToString());
            axisPosLimitSensor.Add(EnumLoadUnloadAxisName.Z軸.ToString(), Z軸上位檢知);
            axisNagLimitSensor.Add(EnumLoadUnloadAxisName.Z軸.ToString(), Z軸下位檢知);
            axisHomeSensor.Add(EnumLoadUnloadAxisName.Z軸.ToString(), Z軸下位檢知);

            AxisCanJog.Add(false);
            AxisPosName.Add("上升");
            AxisNagName.Add("下降");
            AxisSensorList.Add(AxisList[AxisList.Count - 1], new List<string>() { Z軸上位檢知, Z軸下位檢知 });
            AxisSensorDataList.Add(AxisList[AxisList.Count - 1], new List<DataDelayAndChange>() { new DataDelayAndChange(delayTime, EnumDelayType.OnDelay), new DataDelayAndChange(delayTime, EnumDelayType.OnDelay) });

            FeedbackAxisList.Add(EnumLoadUnloadAxisName.P軸.ToString());
            AxisList.Add(EnumLoadUnloadAxisName.P軸.ToString());
            axisPosLimitSensor.Add(EnumLoadUnloadAxisName.P軸.ToString(), P軸前定位);
            axisNagLimitSensor.Add(EnumLoadUnloadAxisName.P軸.ToString(), P軸後定位);
            axisHomeSensor.Add(EnumLoadUnloadAxisName.P軸.ToString(), P軸原點);

            AxisCanJog.Add(false);
            AxisPosName.Add("往前");
            AxisNagName.Add("往後");
            AxisSensorList.Add(AxisList[AxisList.Count - 1], new List<string>() { P軸前定位, P軸原點, P軸後定位 });
            AxisSensorDataList.Add(AxisList[AxisList.Count - 1], new List<DataDelayAndChange>() { new DataDelayAndChange(delayTime, EnumDelayType.OnDelay), new DataDelayAndChange(delayTime, EnumDelayType.OnDelay), new DataDelayAndChange(delayTime, EnumDelayType.OnDelay) });

            FeedbackAxisList.Add(EnumLoadUnloadAxisName.Theta軸.ToString());
            AxisList.Add(EnumLoadUnloadAxisName.Theta軸.ToString());
            axisPosLimitSensor.Add(EnumLoadUnloadAxisName.Theta軸.ToString(), θ軸前定位);
            axisNagLimitSensor.Add(EnumLoadUnloadAxisName.Theta軸.ToString(), θ軸後定位);
            axisHomeSensor.Add(EnumLoadUnloadAxisName.Theta軸.ToString(), θ軸原點);
            AxisCanJog.Add(false);
            AxisPosName.Add("逆時");
            AxisNagName.Add("順時");
            AxisSensorList.Add(AxisList[AxisList.Count - 1], new List<string>() { θ軸前定位, θ軸原點, θ軸後定位 });
            AxisSensorDataList.Add(AxisList[AxisList.Count - 1], new List<DataDelayAndChange>() { new DataDelayAndChange(delayTime, EnumDelayType.OnDelay), new DataDelayAndChange(delayTime, EnumDelayType.OnDelay), new DataDelayAndChange(delayTime, EnumDelayType.OnDelay) });

            FeedbackAxisList.Add(EnumLoadUnloadAxisName.Y軸.ToString());
            AxisList.Add(EnumLoadUnloadAxisName.Y軸.ToString());
            axisPosLimitSensor.Add(EnumLoadUnloadAxisName.Y軸.ToString(), Y軸前定位);
            axisNagLimitSensor.Add(EnumLoadUnloadAxisName.Y軸.ToString(), Y軸後定位);
            axisHomeSensor.Add(EnumLoadUnloadAxisName.Y軸.ToString(), Y軸後定位);

            AxisCanJog.Add(false);
            AxisPosName.Add("伸出");
            AxisNagName.Add("收回");
            AxisSensorList.Add(AxisList[AxisList.Count - 1], new List<string>() { Y軸前定位, Y軸後定位, FORK_在席檢知, CST座_在席檢知, 二重格檢知_L, 二重格檢知_R });
            AxisSensorDataList.Add(AxisList[AxisList.Count - 1], new List<DataDelayAndChange>() { new DataDelayAndChange(delayTime, EnumDelayType.OnDelay), new DataDelayAndChange(delayTime, EnumDelayType.OnDelay), new DataDelayAndChange(delayTime, EnumDelayType.OnDelay), new DataDelayAndChange(delayTime, EnumDelayType.OnDelay), new DataDelayAndChange(delayTime, EnumDelayType.OnDelay), new DataDelayAndChange(delayTime, EnumDelayType.OnDelay) });

            Dictionary<EnumLoadUnloadAxisCommandType, string> tempD;

            for (int i = 0; i < AxisList.Count; i++)
            {
                AllAxisSensorData.Add(AxisList[i], new Dictionary<string, DataDelayAndChange>());

                for (int j = 0; j < AxisSensorDataList[AxisList[i]].Count; j++)
                    AllAxisSensorData[AxisList[i]].Add(AxisSensorList[AxisList[i]][j], AxisSensorDataList[AxisList[i]][j]);

                localData.LoadUnloadData.CVFeedbackData.Add(AxisList[i], null);
                localData.LoadUnloadData.CVEncoderOffsetValue.Add(AxisList[i], 0);
                localData.LoadUnloadData.CVHomeOffsetValue.Add(AxisList[i], 0);


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
                //WriteLog(3, "", "找問題結束" + i.ToString());
            }

        }

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
        }
        #endregion

        private Thread resetAlarmThread = null;

        public bool CheckAixsIsServoOn(string axisName)
        {
            AxisFeedbackData temp = localData.LoadUnloadData.CVFeedbackData[axisName];

            if (temp == null)
                return false;
            else if (temp.AxisServoOnOff == EnumAxisServoOnOff.ServoOff)
                return false;
            else
                return true;
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
            EnumLoadUnloadControlErrorCode.回Home失敗_HomeSensor未off,
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
                        ;
                    else
                        ResetAlarmCode(alarmCodeClearList[i]);
                }
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
                ResetAxis(EnumLoadUnloadAxisName.Y軸);
                ResetAxis(EnumLoadUnloadAxisName.Z軸);
            }

            reconnectedAndResetAxisError = false;
        }


        private void ResetAxis(EnumLoadUnloadAxisName axisName)
        {
            if (CheckAxisError(axisName))
            {
                WriteLog(7, "", String.Concat(axisName.ToString(), "U Error"));
                ServoOff(axisName.ToString(), true, 2000);
                ServoOn(axisName.ToString(), true, 2000);
            }

            if (!CheckAixsIsServoOn(axisName.ToString()))
                ServoOn(axisName.ToString(), true, 2000);
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
                {
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.取放貨補正元件_BarcoderReader連線失敗);
                    WriteLog(5, "", "AlignmentDevice : SR1000 連線失敗");
                }
                else
                    WriteLog(7, "", String.Concat("連線成功!"));
            }
            else
            {
                if (!sr1000_R.Connected)
                {
                    if (!sr1000_R.Connect("192.168.29.216", ref errorMessage))
                    {
                        ResetAlarmCode(EnumLoadUnloadControlErrorCode.取放貨補正元件_BarcoderReader連線失敗);
                        WriteLog(5, "", "AlignmentDevice : SR1000 連線失敗");
                    }
                    else
                        WriteLog(7, "", String.Concat("連線成功!"));
                }
                else if (sr1000_R.Error)
                {
                    sr1000_R.ResetError();

                    if (sr1000_R.Error)
                        ResetAlarmCode(EnumLoadUnloadControlErrorCode.取放貨補正元件_BarcoderReader斷線);
                }
            }

            if (cstIDReader == null)
            {
                cstIDReader = new RFIDReader_OMRON();

                if (!cstIDReader.Connect("192.168.1.200:7090", ref errorMessage))
                {
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.CSTIDReader_連線失敗);
                    WriteLog(5, "", "AlignmentDevice : STD Reader 連線失敗");
                }
                else
                    WriteLog(7, "", String.Concat("連線成功!"));
            }
            else
            {
                if (!cstIDReader.Connected)
                {
                    if (!cstIDReader.Connect("192.168.1.200:7090", ref errorMessage))
                    {
                        ResetAlarmCode(EnumLoadUnloadControlErrorCode.CSTIDReader_連線失敗);
                        WriteLog(5, "", "AlignmentDevice : STD Reader 連線失敗");
                    }
                    else
                        WriteLog(7, "", String.Concat("連線成功!"));
                }
                else if (cstIDReader.Error)
                {
                    cstIDReader.ResetError();

                    if (cstIDReader.Error)
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
                if (!distanceSensor_R.Connect())
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.取放貨補正元件_雷射測距連線失敗);
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
            else if (localData.MIPCData.SafetySensorStatus == EnumSafetyLevel.SlowStop || localData.LoadUnloadData.LoadUnloadCommand.Pause)
                return EnumSafetyLevel.Normal;
            else
                return EnumSafetyLevel.Normal;
        }

        private void SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode errorCode, EnumLoadUnloadErrorLevel level = EnumLoadUnloadErrorLevel.Error)
        {
            SetAlarmCode(errorCode);

            LoadUnloadCommandData command = localData.LoadUnloadData.LoadUnloadCommand;

            if (command.ErrorLevel == level)
                return;

            switch (command.ErrorLevel)
            {
                case EnumLoadUnloadErrorLevel.None:
                    command.ErrorLevel = level;

                    break;
                case EnumLoadUnloadErrorLevel.PrePIOError:
                    if (level == EnumLoadUnloadErrorLevel.Error)
                        command.ErrorLevel = level;

                    break;
                case EnumLoadUnloadErrorLevel.AfterPIOError:
                    if (level == EnumLoadUnloadErrorLevel.Error)
                        command.ErrorLevel = EnumLoadUnloadErrorLevel.AfterPIOErrorAndActionError;

                    break;
                case EnumLoadUnloadErrorLevel.AfterPIOErrorAndActionError:
                    break;
                case EnumLoadUnloadErrorLevel.Error:
                    break;
            }
        }

        public enum EnumPTILoadStatus
        {
            準備發呆 = -1,
            發呆 = -2,
            Step_確認命令成功 = -3,
            Step0_Idle = 0,
            Step_PIO開始 = 1,
            Step_PIOSendBusy = 2,
            Step1_檢查Loading_二重閣 = 3,
            Step2_Theta旋轉90度 = 4,
            Step3_等待Theta旋轉90度完成 = 5,
            Step_CheckAlignmentValue_theta = 6,
            Step_確認Encoder補正範圍_theta = 7,
            Step_Theta軸補正 = 8,
            Step_等待Theta軸補正完成 = 9,
            Step_確認Encoder補正範圍 = 10,
            Step_CheckAlignmentValue = 11,
            Step_角度偏差過大尋找BCR模式 = 12,
            Step_P軸補正 = 13,
            Step_等待P軸補正完成 = 14,
            Step_回原點ForRetry補正 = 15,
            Step_等待回原點ForRetry補正 = 16,
            Step8_Fork伸出 = 17,
            Step9_等待Fork伸出完成 = 18,
            Step10_Z軸上升 = 19,
            Step11_等待Z軸上升完成 = 20,
            Step12_Fork收回 = 21,
            Step13_等待Fork收回完成 = 22,
            Step_PIOSendComplete = 23,
            Step14_Theta軸回0度_P軸回原點 = 24,
            Step15_等待Theta軸回0度_P軸回原點完成 = 25,
            Step16_Z軸下降 = 26,
            Step17_等待Z軸下降 = 27,
            Step18_LoadComplete = 28,
            Step999_LoadFail = 999,
            Step_EmptyRetrival = 998,
            Step_PIO異常處理流程 = 997,

        }

        public enum EnumPTIUnloadStatus
        {
            準備發呆 = -1,
            發呆 = -2,
            Step_確認命令成功 = -3,
            Step0_Idle = 0,
            Step_PIO開始 = 1,
            Step_PIOSendBusy = 2,
            Step1_檢查Loading_二重閣 = 3,
            Step2_Z軸上升 = 4,
            Step3_等待Z軸上升完成 = 5,
            Step4_Theta旋轉90度 = 6,
            Step5_等待Theta旋轉90度完成 = 7,
            Step_CheckAlignmentValue_theta = 8,
            Step_確認Encoder補正範圍_theta = 9,
            Step_Theta軸補正 = 10,
            Step_等待Theta軸補正完成 = 11,
            Step_確認Encoder補正範圍 = 12,
            Step_CheckAlignmentValue = 13,
            Step_角度偏差過大尋找BCR模式 = 14,
            Step_P軸補正 = 15,
            Step_等待P軸補正完成 = 16,
            Step_回原點ForRetry補正 = 17,
            Step_等待回原點ForRetry補正 = 18,
            Step10_Fork伸出 = 19,
            Step11_等待Fork伸出完成 = 20,
            Step12_Z軸下降 = 21,
            Step13_等待Z軸下降 = 22,
            Step14_Fork收回 = 23,
            Step15_等待Fork收回完成 = 24,
            Step_PIOSendComplete = 25,
            Step16_Theta軸回0度_P軸回原點 = 26,
            Step17_等待Theta軸回0度_P軸回原點完成 = 27,
            Step18_UnloadComplete = 28,
            Step_ForPrePIOErrorZ軸下降 = 29,
            Step_ForPrePIOError等待Z軸下降 = 30,
            Step_ForPrePIOErrorTheta軸回0度_P軸回原點 = 32,
            Step_ForPrePIOError等待Theta軸回0度_P軸回原點完成 = 33,
            Step_Doublestorage = 998,
            Step999_LoadFail = 999,
            Step_PIO異常處理流程 = 997,
        }

        public override void LoadUnloadStart()
        {
            double waitRollerStopTimeout = 5000;
            Stopwatch timer = new Stopwatch();
            LoadUnloadCommandData command = localData.LoadUnloadData.LoadUnloadCommand;

            EnumSafetyLevel lastStatus = EnumSafetyLevel.Normal;
            EnumSafetyLevel nowStatus = EnumSafetyLevel.Normal;

            command.StatusStep = 1;
            command.StepString = ((EnumUMTCLoadUnloadStatus)command.StatusStep).ToString();

            WriteLog(7, "", String.Concat("Step Change to : ", ((EnumUMTCLoadUnloadStatus)command.StatusStep).ToString()));

            double 欲移動位置 = 0;
            int NextStep = 0;
            int NextStepFor尋找BCR = 0;
            int NextStepForconfirmcommand = 0;
            int 發呆數 = 0;
            int 已發呆數 = 0;
            int alignmentCount = 0;
            int maxAlignmentCount = 5;
            int retrycount = 0;
            int CMDRetryCount = 0;
            int 補正retryCount = 0;
            double encoderAbs_P = 0;
            double encoderAbs_Z = 0;
            double encoderAbs_Theta = 0;
            double encoderAbs_Y = 0;
            double firsttheta = 0;
            int firstthetais0_axispjogrange = 0;
            int firstthetais0_axispjogdirection = 1;
            int firstthetais0_axispnextposition = 0;
            bool ignorePIOError = false;
            EnumLoadUnloadErrorLevel PIOErrorStatus = EnumLoadUnloadErrorLevel.None;
            AlignmentValueData temp;

            Stopwatch rollerErrorFlowTimer = new Stopwatch();
            Stopwatch MotionTimer = new Stopwatch();
            int motiotimeoutvalue = 60000; 
            ((PIOFlow_PTI_LoadUnloadSemi)RightPIO).PIO_Close();
            while (command.StatusStep != (int)EnumUMTCLoadUnloadStatus.Step0_Idle)
            {
                Thread.Sleep(10);
                UpdateForkHomeStatus();
                nowStatus = GetSafetySensorStatus();

                switch (command.Action)
                {
                    case EnumLoadUnload.Load:

                        #region PIO異常

                        if (ignorePIOError == false)
                        {
                            if (command.StatusStep != (int)EnumPTILoadStatus.Step999_LoadFail)
                            {
                                if (RightPIO.Timeout != EnumPIOStatus.None)
                                {
                                    if ((command.StatusStep > 0 && (int)command.StatusStep < (int)EnumPTILoadStatus.Step8_Fork伸出) ||
                                            ((int)command.StatusStep < 0 && (int)NextStep < (int)EnumPTILoadStatus.Step8_Fork伸出)
                                            )
                                    {
                                        #region PrePIOError
                                        ignorePIOError = true;
                                        PIOErrorStatus = EnumLoadUnloadErrorLevel.PrePIOError;
                                        JogStop();
                                        Thread.Sleep(1000);
                                        command.StatusStep = (int)EnumPTILoadStatus.Step13_等待Fork收回完成;
                                        #endregion
                                    }
                                    else if (((int)command.StatusStep > 0 && (int)command.StatusStep < (int)EnumPTILoadStatus.Step13_等待Fork收回完成 + 1) ||
                                            ((int)command.StatusStep < 0 && (int)NextStep < (int)EnumPTILoadStatus.Step13_等待Fork收回完成 + 1)
                                             )
                                    {
                                        #region PIOError 
                                        ignorePIOError = false;
                                        PIOErrorStatus = EnumLoadUnloadErrorLevel.Error;
                                        JogStop();
                                        command.StatusStep = (int)EnumPTILoadStatus.Step999_LoadFail;
                                        #endregion
                                    }
                                    else
                                    {
                                        #region AftPIOError 
                                        ignorePIOError = true;
                                        PIOErrorStatus = EnumLoadUnloadErrorLevel.AfterPIOError;
                                        #endregion
                                    }
                                }
                            }
                        }
                        #endregion

                        switch (localData.LoadUnloadData.LoadUnloadCommand.StatusStep)
                        {
                            case (int)EnumPTILoadStatus.準備發呆:
                                已發呆數 = 0;
                                command.StatusStep = (int)EnumPTILoadStatus.發呆;
                                break;

                            case (int)EnumPTILoadStatus.發呆:
                                if (已發呆數 == 發呆數 * 0.5)
                                {
                                    發呆數 = 0;
                                    command.StatusStep = NextStep;
                                    NextStep = 0;
                                    break;
                                }
                                已發呆數++;
                                break;
                            case (int)EnumPTILoadStatus.Step_確認命令成功:
                                Thread.Sleep(500);
                                if (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].PositionCommand == 0 &&
                                   localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].PositionCommand == 0 &&
                                    localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Y軸.ToString()].PositionCommand == 0 &&
                                    localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].PositionCommand == 0
                                     )
                                {
                                    CMDRetryCount = 0;
                                    command.StatusStep = (int)EnumPTILoadStatus.準備發呆;
                                    break;
                                }
                                else if (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].PositionCommand == -1 ||
                                   localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].PositionCommand == -1 ||
                                    localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Y軸.ToString()].PositionCommand == -1 ||
                                    localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].PositionCommand == -1
                                     )
                                {
                                    if (CMDRetryCount > 3)
                                    {
                                        command.StatusStep = 999;
                                        break;
                                    }
                                    CMDRetryCount++;
                                    command.StatusStep = NextStepForconfirmcommand;
                                    break;
                                }
                                else
                                {

                                }
                                    break;
                            case (int)EnumPTILoadStatus.Step_PIO開始:
                                if (command.BreakenStopMode)
                                    command.GoNext = false;

                                if (command.NeedPIO && !ignorePIOError)
                                {
                                    ((PIOFlow_PTI_LoadUnloadSemi)RightPIO).PIO_SerChannel_ID(command.AddressID);
                                    RightPIO.PIOFlow_Load_UnLoad(command.Action);
                                    command.StatusStep = (int)EnumPTILoadStatus.Step_PIOSendBusy;
                                }
                                else
                                {
                                    command.StatusStep = (int)EnumPTILoadStatus.Step_PIOSendBusy;
                                }
                                break;


                            case (int)EnumPTILoadStatus.Step_PIOSendBusy:
                                if (command.NeedPIO && !ignorePIOError)
                                {
                                    if (RightPIO.Status == EnumPIOStatus.TP2)
                                    {
                                        ((PIOFlow_PTI_LoadUnloadSemi)RightPIO).SendBusy = true;

                                        NextStep = (int)EnumPTILoadStatus.Step1_檢查Loading_二重閣;
                                        command.StatusStep = (int)EnumPTILoadStatus.準備發呆;
                                        發呆數 = 10;
                                    }
                                    else
                                    {
                                    }
                                }
                                else
                                {
                                    NextStep = (int)EnumPTILoadStatus.Step1_檢查Loading_二重閣;
                                    command.StatusStep = (int)EnumPTILoadStatus.準備發呆;
                                }
                                break;
                            case (int)EnumPTILoadStatus.Step1_檢查Loading_二重閣:
                                if (AllAxisSensorData[EnumLoadUnloadAxisName.Y軸.ToString()][CST座_在席檢知].data == true)
                                {
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取貨_LoadingOn異常);
                                    command.StatusStep = (int)EnumPTILoadStatus.Step999_LoadFail;
                                    break;
                                }
                                switch (command.StageDirection)
                                {
                                    case EnumStageDirection.Left:
                                        if (AllAxisSensorData[EnumLoadUnloadAxisName.Y軸.ToString()][二重格檢知_L].data == true)
                                        {
                                            command.StatusStep = (int)EnumPTILoadStatus.Step2_Theta旋轉90度;
                                        }
                                        else
                                        {
                                            command.StatusStep = (int)EnumPTILoadStatus.Step_EmptyRetrival;
                                        }
                                        break;
                                    case EnumStageDirection.Right:
                                        if (AllAxisSensorData[EnumLoadUnloadAxisName.Y軸.ToString()][二重格檢知_R].data == true)
                                        {
                                            command.StatusStep = (int)EnumPTILoadStatus.Step2_Theta旋轉90度;
                                        }
                                        else
                                        {
                                            command.StatusStep = (int)EnumPTILoadStatus.Step_EmptyRetrival;
                                        }
                                        break;
                                }
                                break;
                            case (int)EnumPTILoadStatus.Step2_Theta旋轉90度:
                                if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                                {
                                    欲移動位置 = 90.5 - 補正retryCount*0.05;
                                    switch (command.StageDirection)
                                    {
                                        case EnumStageDirection.Left:
                                            欲移動位置 = 欲移動位置 * 1;
                                            break;
                                        case EnumStageDirection.Right:
                                            欲移動位置 = 欲移動位置 * -1;
                                            break;
                                    }
                                    if (!CVPtoP(EnumLoadUnloadAxisName.Theta軸.ToString(), 欲移動位置, 1, false, 0))
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                        command.StatusStep = 999;
                                    }
                                    else
                                    {
                                        //if (command.NeedPIO)
                                        //{
                                        //    RightPIO.PIOFlow_Load_UnLoad(command.Action);
                                        //}
                                        MotionTimer.Restart();
                                        NextStep = (int)EnumPTILoadStatus.Step3_等待Theta旋轉90度完成;
                                        NextStepForconfirmcommand = (int)EnumPTILoadStatus.Step2_Theta旋轉90度;
                                        command.StatusStep = (int)EnumPTILoadStatus.Step_確認命令成功;
                                        發呆數 = 150;
                                    }
                                }
                                break;
                            case (int)EnumPTILoadStatus.Step3_等待Theta旋轉90度完成:
                                if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop))
                                {
                                    if (retrycount < 3)
                                    {
                                        if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.Theta軸.ToString()].PlausUnit)) - 欲移動位置 < 1)
                                        {
                                            MotionTimer.Stop();
                                            command.StatusStep = (int)EnumPTILoadStatus.Step_CheckAlignmentValue_theta;
                                            retrycount = 0;
                                            break;
                                        }
                                        else
                                        {
                                            NextStep = (int)EnumPTILoadStatus.Step3_等待Theta旋轉90度完成;
                                            command.StatusStep = (int)EnumPTILoadStatus.準備發呆;
                                            發呆數 = 200;
                                            retrycount++;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange);
                                        command.StatusStep = 999;
                                        break;
                                    }
                                }
                                if (MotionTimer.ElapsedMilliseconds > motiotimeoutvalue)
                                {
                                    JogStop();
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.RollerStopTimeout);
                                    command.StatusStep = 999;
                                }
                                break;
                            case (int)EnumPTILoadStatus.Step_CheckAlignmentValue_theta:
                                    CheckAlingmentValueByAddressID(command.AddressID);
                                    //CheckAlingmentValue(command.StageDirection, command.StageNumber);
                                    temp = localData.LoadUnloadData.AlignmentValue;

                                if (temp != null && temp.AlignmentVlaue)
                                {
                                    alignmentCount = 0;
                                    command.CommandStartAlignmentValue = temp;
                                    command.StatusStep = (int)EnumPTILoadStatus.Step_確認Encoder補正範圍_theta;
                                }
                                else
                                {
                                    alignmentCount++;

                                    if (alignmentCount >= maxAlignmentCount)
                                    {
                                        if (temp != null && (temp.LaserF == 0 || temp.LaserB == 0))
                                        {
                                            alignmentCount = 0;
                                            SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.AlignmentValueNG);
                                            command.StatusStep = (int)EnumPTILoadStatus.Step999_LoadFail;
                                        }
                                        else
                                        {
                                            firsttheta = 0;
                                            alignmentCount = 0;
                                            NextStepFor尋找BCR = (int)EnumPTILoadStatus.Step_CheckAlignmentValue_theta;
                                            command.StatusStep = (int)EnumPTILoadStatus.Step_角度偏差過大尋找BCR模式;
                                        }
                                    }
                                }
                                break;
                            case (int)EnumPTILoadStatus.Step_確認Encoder補正範圍_theta:
                                #region Step3_確認Encoder補正範圍.
                                if (command.UsingAlignmentValue)
                                {
                                    switch (command.StageDirection)
                                    {
                                        case EnumStageDirection.Left:
                                            encoderAbs_P = 0 + (command.CommandStartAlignmentValue.LASER_P / Math.Cos(command.CommandStartAlignmentValue.Theta * Math.PI / 180));
                                            encoderAbs_Theta = LeftStageDataList[command.StageNumber].Encoder_Theta + command.CommandStartAlignmentValue.Theta;
                                            break;
                                        case EnumStageDirection.Right:
                                            encoderAbs_P = 0 - (command.CommandStartAlignmentValue.LASER_P / Math.Cos(command.CommandStartAlignmentValue.Theta * Math.PI / 180));
                                            encoderAbs_Theta = RightStageDataList[command.StageNumber].Encoder_Theta + command.CommandStartAlignmentValue.Theta;
                                            break;
                                    }
                                    //encoderAbs_Z = RightStageDataList[command.StageNumber].Encoder_Z +/* command.CommandStartAlignmentValue.Z +*/ RightStageDataList[command.StageNumber].Benchmark_Z;
                                    //encoderAbs_Y = RightStageDataList[command.StageNumber].Encoder_Y + command.CommandStartAlignmentValue.Y;
                                    firsttheta = command.CommandStartAlignmentValue.Theta;
                                    WriteLog(7, "", String.Concat("Encoder P : ", encoderAbs_P.ToString("0.00"),
                                                                  "Encoder Theta : ", encoderAbs_Theta.ToString("0.00"),
                                                                  "Encoder Z : ", encoderAbs_Z.ToString("0.00")));


                                    if (axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PosLimit >= encoderAbs_P &&
                                           encoderAbs_P >= axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].NagLimit &&
                                        //axisDataList[EnumLoadUnloadAxisName.Z軸.ToString()].PosLimit >= encoderAbs_Z &&
                                        //   encoderAbs_Z >= axisDataList[EnumLoadUnloadAxisName.Z軸.ToString()].NagLimit &&
                                        axisDataList[EnumLoadUnloadAxisName.Theta軸.ToString()].PosLimit >= encoderAbs_Theta &&
                                           encoderAbs_Theta >= axisDataList[EnumLoadUnloadAxisName.Theta軸.ToString()].NagLimit/* &&*/
                                                                                                                              //axisDataList[EnumLoadUnloadAxisName.Y軸.ToString()].PosLimit >= encoderAbs_Y &&
                                                                                                                              //   encoderAbs_Y >= axisDataList[EnumLoadUnloadAxisName.Y軸.ToString()].NagLimit
                                        )
                                    {
                                        command.StatusStep = (int)EnumPTILoadStatus.Step_Theta軸補正;

                                        if (command.BreakenStopMode)
                                            command.GoNext = false;
                                    }
                                    else
                                    {
                                        command.StatusStep = (int)EnumPTILoadStatus.Step999_LoadFail;
                                    }
                                }
                                else
                                {
                                    command.StatusStep = (int)EnumPTILoadStatus.Step999_LoadFail;
                                }
                                #endregion
                                break;
                            case (int)EnumPTILoadStatus.Step_Theta軸補正:
                                if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                                {
                                    if (!CVPtoP(EnumLoadUnloadAxisName.Theta軸.ToString(), encoderAbs_Theta, 0.2, false, 0))
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                        command.StatusStep = 999;
                                    }
                                    else
                                    {
                                        Thread.Sleep(1000);
                                        if (!CVPtoP(EnumLoadUnloadAxisName.P軸.ToString(), encoderAbs_P, 0.2, false, 0))
                                        {
                                            JogStop();
                                            SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                            command.StatusStep = 999;
                                        }
                                        else
                                        {
                                            MotionTimer.Restart();
                                            NextStep = (int)EnumPTILoadStatus.Step_等待Theta軸補正完成;
                                            NextStepForconfirmcommand = (int)EnumPTILoadStatus.Step_Theta軸補正;
                                            command.StatusStep = (int)EnumPTILoadStatus.Step_確認命令成功;
                                            發呆數 = 100;
                                        }
                                    }
                                }
                                break;
                            case (int)EnumPTILoadStatus.Step_等待Theta軸補正完成:
                                if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop) &&
                                    (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop))
                                {
                                    if (retrycount < 3)
                                    {
                                        if (Math.Abs((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.Theta軸.ToString()].PlausUnit)) - encoderAbs_Theta) < 1 &&
                                            Math.Abs((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PlausUnit)) - encoderAbs_P) < 1)
                                        {
                                            MotionTimer.Stop();
                                            command.StatusStep = (int)EnumPTILoadStatus.Step_CheckAlignmentValue;
                                            retrycount = 0;
                                            break;
                                        }
                                        else
                                        {
                                            NextStep = (int)EnumPTILoadStatus.Step_等待Theta軸補正完成;
                                            command.StatusStep = (int)EnumPTILoadStatus.準備發呆;
                                            發呆數 = 200;
                                            retrycount++;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange);
                                        command.StatusStep = 999;
                                        break;
                                    }
                                }
                                
                                if (MotionTimer.ElapsedMilliseconds > motiotimeoutvalue)
                                {
                                    JogStop();
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.RollerStopTimeout);
                                    command.StatusStep = 999;
                                }
                                break;

                            case (int)EnumPTILoadStatus.Step_CheckAlignmentValue:
                                    CheckAlingmentValueByAddressID(command.AddressID);
                                    //CheckAlingmentValue(command.StageDirection, command.StageNumber);
                                    temp = localData.LoadUnloadData.AlignmentValue;

                                if (temp != null && temp.AlignmentVlaue)
                                {
                                    alignmentCount = 0;
                                    command.CommandStartAlignmentValue = temp;
                                    command.StatusStep = (int)EnumPTILoadStatus.Step_確認Encoder補正範圍;
                                }
                                else
                                {
                                    alignmentCount++;

                                    if (alignmentCount >= maxAlignmentCount)
                                    {
                                        if (temp != null && (temp.LaserF == 0 || temp.LaserB == 0))
                                        {
                                            alignmentCount = 0;
                                            SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.AlignmentValueNG);
                                            command.StatusStep = (int)EnumPTILoadStatus.Step999_LoadFail;
                                        }
                                        else
                                        {
                                            alignmentCount = 0;
                                            NextStepFor尋找BCR = (int)EnumPTILoadStatus.Step_CheckAlignmentValue;
                                            command.StatusStep = (int)EnumPTILoadStatus.Step_角度偏差過大尋找BCR模式;
                                        }
                                    }
                                }
                                break;
                            case (int)EnumPTILoadStatus.Step_確認Encoder補正範圍:
                                #region Step3_確認Encoder補正範圍.
                                if (command.UsingAlignmentValue)
                                {
                                    if (Math.Abs(command.CommandStartAlignmentValue.Theta) < 0.1)
                                    {
                                        switch (command.StageDirection)
                                        {
                                            case EnumStageDirection.Left:
                                                encoderAbs_P = LeftStageDataList[command.StageNumber].Encoder_P + (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PlausUnit)) - (command.CommandStartAlignmentValue.P / Math.Cos(command.CommandStartAlignmentValue.Theta * Math.PI / 180));

                                                encoderAbs_Z = LeftStageDataList[command.StageNumber].Encoder_Z +/* command.CommandStartAlignmentValue.Z +*/ LeftStageDataList[command.StageNumber].Benchmark_Z;
                                                encoderAbs_Theta = LeftStageDataList[command.StageNumber].Encoder_Theta - command.CommandStartAlignmentValue.Theta;
                                                encoderAbs_Y = LeftStageDataList[command.StageNumber].Encoder_Y + command.CommandStartAlignmentValue.Y;
                                                break;
                                            case EnumStageDirection.Right:
                                                encoderAbs_P = RightStageDataList[command.StageNumber].Encoder_P + (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PlausUnit)) - (command.CommandStartAlignmentValue.P / Math.Cos(command.CommandStartAlignmentValue.Theta * Math.PI / 180));
                                                //encoderAbs_P -= 1;
                                                encoderAbs_Z = RightStageDataList[command.StageNumber].Encoder_Z +/* command.CommandStartAlignmentValue.Z +*/ RightStageDataList[command.StageNumber].Benchmark_Z;
                                                encoderAbs_Theta = RightStageDataList[command.StageNumber].Encoder_Theta - command.CommandStartAlignmentValue.Theta;
                                                encoderAbs_Y = RightStageDataList[command.StageNumber].Encoder_Y + command.CommandStartAlignmentValue.Y;
                                                break;
                                        }
                                        WriteLog(7, "", String.Concat("Encoder P : ", encoderAbs_P.ToString("0.00"),
                                                                      "Encoder Theta : ", encoderAbs_Theta.ToString("0.00"),
                                                                      "Encoder Z : ", encoderAbs_Z.ToString("0.00")));

                                        if (axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PosLimit >= encoderAbs_P &&
                                               encoderAbs_P >= axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].NagLimit &&
                                               axisDataList[EnumLoadUnloadAxisName.Z軸.ToString()].PosLimit >= encoderAbs_Z &&
                                               encoderAbs_Z >= axisDataList[EnumLoadUnloadAxisName.Z軸.ToString()].NagLimit &&
                                               axisDataList[EnumLoadUnloadAxisName.Theta軸.ToString()].PosLimit >= encoderAbs_Theta &&
                                               encoderAbs_Theta >= axisDataList[EnumLoadUnloadAxisName.Theta軸.ToString()].NagLimit &&
                                               axisDataList[EnumLoadUnloadAxisName.Y軸.ToString()].PosLimit >= encoderAbs_Y &&
                                               encoderAbs_Y >= axisDataList[EnumLoadUnloadAxisName.Y軸.ToString()].NagLimit
                                            )
                                        {
                                            補正retryCount = 0;
                                            command.StatusStep = (int)EnumPTILoadStatus.Step_P軸補正;

                                            if (command.BreakenStopMode)
                                                command.GoNext = false;
                                        }
                                        else
                                        {
                                            command.StatusStep = (int)EnumPTILoadStatus.Step999_LoadFail;
                                        }
                                    }
                                    else
                                    {
                                        if (補正retryCount > 3)
                                        {
                                            SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.AlignmentValueNG);
                                            command.StatusStep = (int)EnumPTILoadStatus.Step999_LoadFail;
                                            break;
                                        }
                                        補正retryCount++;
                                        command.StatusStep = (int)EnumPTILoadStatus.Step_回原點ForRetry補正;
                                    }
                                }
                                else
                                {
                                    command.StatusStep = (int)EnumPTILoadStatus.Step999_LoadFail;
                                }
                                #endregion
                                break;
                            case (int)EnumPTILoadStatus.Step_回原點ForRetry補正:
                                if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                                {
                                    if (!CVPtoP(EnumLoadUnloadAxisName.P軸.ToString(), 0, 1, false, 0))
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                        command.StatusStep = 999;
                                    }
                                    else
                                    {
                                        Thread.Sleep(1000);
                                        if (!CVPtoP(EnumLoadUnloadAxisName.Theta軸.ToString(), 0, 1, false, 0))
                                        {
                                            JogStop();
                                            SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                            command.StatusStep = 999;
                                        }
                                        else
                                        {
                                            MotionTimer.Restart();
                                            NextStep = (int)EnumPTILoadStatus.Step_等待回原點ForRetry補正;
                                            NextStepForconfirmcommand = (int)EnumPTILoadStatus.Step_回原點ForRetry補正;
                                            command.StatusStep = (int)EnumPTILoadStatus.Step_確認命令成功;
                                            發呆數 = 150;
                                        }
                                    }
                                }
                                break;
                            case (int)EnumPTILoadStatus.Step_等待回原點ForRetry補正:
                                if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop) &&
                                (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop))
                                {
                                    if (retrycount < 3)
                                    {
                                        if (Math.Abs((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.Theta軸.ToString()].PlausUnit)) - 0) < 1 &&
                                       Math.Abs((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PlausUnit)) - 0) < 1)
                                        {
                                            if (command.BreakenStopMode)
                                                command.GoNext = false;

                                            MotionTimer.Stop();
                                            command.StatusStep = (int)EnumPTILoadStatus.Step2_Theta旋轉90度;
                                            retrycount = 0;
                                            break;
                                        }
                                        else
                                        {
                                            NextStep = (int)EnumPTILoadStatus.Step_等待回原點ForRetry補正;
                                            command.StatusStep = (int)EnumPTILoadStatus.準備發呆;
                                            發呆數 = 200;
                                            retrycount++;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange);
                                        command.StatusStep = 999;
                                    }
                                }
                                if (MotionTimer.ElapsedMilliseconds > motiotimeoutvalue)
                                {
                                    JogStop();
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.RollerStopTimeout);
                                    command.StatusStep = 999;
                                }
                                break;
                            case (int)EnumPTILoadStatus.Step_角度偏差過大尋找BCR模式:
                                if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop))
                                {
                                    CheckAlingmentValueByAddressID(command.AddressID);
                                    //CheckAlingmentValue(command.StageDirection, command.StageNumber);
                                    temp = localData.LoadUnloadData.AlignmentValue;

                                    if (temp != null && temp.AlignmentVlaue)
                                    {
                                        NextStep = NextStepFor尋找BCR;
                                        NextStepFor尋找BCR = 0;
                                        command.StatusStep = (int)EnumPTILoadStatus.準備發呆;
                                        發呆數 = 100;
                                    }
                                    else
                                    {
                                        if (firsttheta == 0)
                                        {
                                            firstthetais0_axispjogrange += 5;
                                            firstthetais0_axispjogdirection *= -1;
                                            firstthetais0_axispnextposition = firstthetais0_axispnextposition + firstthetais0_axispjogrange * firstthetais0_axispjogdirection;
                                            encoderAbs_P = firstthetais0_axispnextposition;

                                        }
                                        else
                                        {
                                            switch (command.StageDirection)
                                            {
                                                case EnumStageDirection.Left:
                                                    if (firsttheta < 0)
                                                    {
                                                        encoderAbs_P = (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PlausUnit)) - 5;
                                                    }
                                                    else
                                                    {
                                                        encoderAbs_P = (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PlausUnit)) + 5;
                                                    }
                                                    break;
                                                case EnumStageDirection.Right:
                                                    if (firsttheta < 0)
                                                    {
                                                        encoderAbs_P = (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PlausUnit)) + 5;
                                                    }
                                                    else
                                                    {
                                                        encoderAbs_P = (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PlausUnit)) - 5;
                                                    }
                                                    break;
                                            }
                                        }
                                        if (!CVPtoP(EnumLoadUnloadAxisName.P軸.ToString(), encoderAbs_P, 1, false, 0))
                                        {
                                        }
                                        else
                                        {
                                            NextStep = (int)EnumPTILoadStatus.Step_角度偏差過大尋找BCR模式;
                                            command.StatusStep = (int)EnumPTILoadStatus.準備發呆;
                                            發呆數 = 100;
                                        }
                                    }
                                }
                                break;
                            case (int)EnumPTILoadStatus.Step_P軸補正:
                                if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                                {
                                    if (!CVPtoP(EnumLoadUnloadAxisName.P軸.ToString(), encoderAbs_P, 0.3, false, 0))
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                        command.StatusStep = 999;
                                    }
                                    else
                                    {
                                        MotionTimer.Restart();
                                        NextStep = (int)EnumPTILoadStatus.Step_等待P軸補正完成;
                                        NextStepForconfirmcommand = (int)EnumPTILoadStatus.Step_P軸補正;
                                        command.StatusStep = (int)EnumPTILoadStatus.Step_確認命令成功;
                                        發呆數 = 100;
                                    }
                                }
                                break;
                            case (int)EnumPTILoadStatus.Step_等待P軸補正完成:
                                if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop))
                                {
                                    if (retrycount < 3)
                                    {
                                        if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PlausUnit)) - encoderAbs_P < 1)
                                        {
                                            if (command.BreakenStopMode)
                                                command.GoNext = false;

                                            MotionTimer.Stop();
                                            command.StatusStep = (int)EnumPTILoadStatus.Step8_Fork伸出;
                                            retrycount = 0;
                                            break;
                                        }
                                        else
                                        {
                                            NextStep = (int)EnumPTILoadStatus.Step_等待P軸補正完成;
                                            command.StatusStep = (int)EnumPTILoadStatus.準備發呆;
                                            發呆數 = 200;
                                            retrycount++;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange);
                                        command.StatusStep = 999;
                                        break;
                                    }
                                }
                                if (AllAxisSensorData[EnumLoadUnloadAxisName.P軸.ToString()][P軸前定位].data == true ||
                                   AllAxisSensorData[EnumLoadUnloadAxisName.P軸.ToString()][P軸後定位].data == true)
                                {
                                    JogStop();
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放中極限觸發);
                                    command.StatusStep = 999;
                                }
                                    if (MotionTimer.ElapsedMilliseconds > motiotimeoutvalue)
                                {
                                    JogStop();
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.RollerStopTimeout);
                                    command.StatusStep = 999;
                                }
                                break;
                            case (int)EnumPTILoadStatus.Step8_Fork伸出:
                                if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                                {
                                    if (!CVPtoP(EnumLoadUnloadAxisName.Y軸.ToString(), encoderAbs_Y, 0.5, false, 0))
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                        command.StatusStep = 999;
                                    }
                                    else
                                    {
                                        MotionTimer.Restart();
                                        NextStep = (int)EnumPTILoadStatus.Step9_等待Fork伸出完成;
                                        NextStepForconfirmcommand = (int)EnumPTILoadStatus.Step8_Fork伸出;
                                        command.StatusStep = (int)EnumPTILoadStatus.Step_確認命令成功;
                                        發呆數 = 200;
                                    }
                                }
                                break;
                            case (int)EnumPTILoadStatus.Step9_等待Fork伸出完成:
                                if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Y軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop))
                                {
                                    if (retrycount < 3)
                                    {
                                        if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Y軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.Y軸.ToString()].PlausUnit)) - encoderAbs_Y < 1)
                                        {
                                            if (command.BreakenStopMode)
                                                command.GoNext = false;

                                            MotionTimer.Stop();
                                            command.StatusStep = (int)EnumPTILoadStatus.Step10_Z軸上升;
                                            retrycount = 0;
                                            break;
                                        }
                                        else
                                        {
                                            NextStep = (int)EnumPTILoadStatus.Step9_等待Fork伸出完成;
                                            command.StatusStep = (int)EnumPTILoadStatus.準備發呆;
                                            發呆數 = 200;
                                            retrycount++;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange);
                                        command.StatusStep = 999;
                                    }
                                }
                                if (MotionTimer.ElapsedMilliseconds > motiotimeoutvalue)
                                {
                                    JogStop();
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.RollerStopTimeout);
                                    command.StatusStep = 999;
                                }
                                break;
                            case (int)EnumPTILoadStatus.Step10_Z軸上升:
                                if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                                {
                                    if (!CVPtoP(EnumLoadUnloadAxisName.Z軸.ToString(), encoderAbs_Z, 1, false, 0))
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                        command.StatusStep = 999;
                                    }
                                    else
                                    {
                                        MotionTimer.Restart();
                                        NextStep = (int)EnumPTILoadStatus.Step11_等待Z軸上升完成;
                                        NextStepForconfirmcommand = (int)EnumPTILoadStatus.Step10_Z軸上升;
                                        command.StatusStep = (int)EnumPTILoadStatus.Step_確認命令成功;
                                        發呆數 = 150;
                                    }
                                }
                                break;
                            case (int)EnumPTILoadStatus.Step11_等待Z軸上升完成:
                                if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop))
                                {
                                    if (retrycount < 3)
                                    {
                                        if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.Z軸.ToString()].PlausUnit)) - encoderAbs_Z < 1)
                                        {
                                            if (command.BreakenStopMode)
                                                command.GoNext = false;

                                            MotionTimer.Stop();
                                            command.StatusStep = (int)EnumPTILoadStatus.Step12_Fork收回;
                                            retrycount = 0;
                                            break;
                                        }
                                        else
                                        {
                                            NextStep = (int)EnumPTILoadStatus.Step11_等待Z軸上升完成;
                                            command.StatusStep = (int)EnumPTILoadStatus.準備發呆;
                                            發呆數 =200;
                                            retrycount++;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange);
                                        command.StatusStep = 999;
                                    }
                                }
                                if (MotionTimer.ElapsedMilliseconds > motiotimeoutvalue)
                                {
                                    JogStop();
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.RollerStopTimeout);
                                    command.StatusStep = 999;
                                }
                                break;
                            case (int)EnumPTILoadStatus.Step12_Fork收回:
                                if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                                {
                                    if (!CVPtoP(EnumLoadUnloadAxisName.Y軸.ToString(), 0, 1, false, 0))
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                        command.StatusStep = 999;
                                    }
                                    else
                                    {
                                        MotionTimer.Restart();
                                        NextStep = (int)EnumPTILoadStatus.Step13_等待Fork收回完成;
                                        NextStepForconfirmcommand = (int)EnumPTILoadStatus.Step12_Fork收回;
                                        command.StatusStep = (int)EnumPTILoadStatus.Step_確認命令成功;
                                        發呆數 = 150;
                                    }
                                }
                                break;
                            case (int)EnumPTILoadStatus.Step13_等待Fork收回完成:
                                if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Y軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop))
                                {
                                    if (retrycount < 3)
                                    {
                                        if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Y軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.Y軸.ToString()].PlausUnit)) - 0 < 1)
                                        {

                                            if (command.BreakenStopMode)
                                                command.GoNext = false;

                                            MotionTimer.Stop();
                                            command.StatusStep = (int)EnumPTILoadStatus.Step_PIOSendComplete;
                                            retrycount = 0;
                                            break;
                                        }
                                        else
                                        {
                                            NextStep = (int)EnumPTILoadStatus.Step13_等待Fork收回完成;
                                            command.StatusStep = (int)EnumPTILoadStatus.準備發呆;
                                            發呆數 = 200;
                                            retrycount++;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange);
                                        command.StatusStep = 999;
                                        break;
                                    }
                                }
                                if (AllAxisSensorData[EnumLoadUnloadAxisName.Y軸.ToString()][axisNagLimitSensor[EnumLoadUnloadAxisName.Y軸.ToString()]].Data)
                                {
                                    if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Y軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.Y軸.ToString()].PlausUnit)) - 0 < 1.5)
                                    {
                                        JogStop();
                                        NextStep = (int)EnumPTILoadStatus.Step13_等待Fork收回完成;
                                        command.StatusStep = (int)EnumPTILoadStatus.準備發呆;
                                        發呆數 = 50;
                                        break;
                                    }
                                }
                                if (MotionTimer.ElapsedMilliseconds > motiotimeoutvalue)
                                {
                                    JogStop();
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.RollerStopTimeout);
                                    command.StatusStep = 999;
                                    break;
                                }
                                break;

                            case (int)EnumPTILoadStatus.Step_PIOSendComplete:
                                if (command.NeedPIO && !ignorePIOError)
                                {
                                    if (((PIOFlow_PTI_LoadUnloadSemi)RightPIO).Status == EnumPIOStatus.TP4)
                                    {
                                        ((PIOFlow_PTI_LoadUnloadSemi)RightPIO).SendComplete = true;
                                        NextStep = (int)EnumPTILoadStatus.Step14_Theta軸回0度_P軸回原點;
                                        command.StatusStep = (int)EnumPTILoadStatus.準備發呆;
                                        發呆數 = 50;
                                    }
                                }
                                else
                                {
                                    NextStep = (int)EnumPTILoadStatus.Step14_Theta軸回0度_P軸回原點;
                                    command.StatusStep = (int)EnumPTILoadStatus.準備發呆;
                                }
                                break;
                            case (int)EnumPTILoadStatus.Step14_Theta軸回0度_P軸回原點:
                                if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                                {
                                    if (!CVPtoP(EnumLoadUnloadAxisName.P軸.ToString(), 0, 0.3, false, 0))
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                        command.StatusStep = 999;
                                    }
                                    else
                                    {
                                        Thread.Sleep(1000);
                                        if (!CVPtoP(EnumLoadUnloadAxisName.Theta軸.ToString(), 0, 1, false, 0))
                                        {
                                            JogStop();
                                            SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                            command.StatusStep = 999;
                                        }
                                        else
                                        {
                                            MotionTimer.Restart();
                                            NextStep = (int)EnumPTILoadStatus.Step15_等待Theta軸回0度_P軸回原點完成;
                                            NextStepForconfirmcommand = (int)EnumPTILoadStatus.Step14_Theta軸回0度_P軸回原點;
                                            command.StatusStep = (int)EnumPTILoadStatus.Step_確認命令成功;
                                            發呆數 = 150;
                                        }
                                    }
                                }
                                break;
                            case (int)EnumPTILoadStatus.Step15_等待Theta軸回0度_P軸回原點完成:
                                if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop) &&
                                    (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop))
                                {
                                    if (retrycount < 3)
                                    {
                                        if (Math.Abs((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.Theta軸.ToString()].PlausUnit)) - 0) < 1 &&
                                       Math.Abs((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PlausUnit)) - 0) < 1)
                                        {
                                            if (command.BreakenStopMode)
                                                command.GoNext = false;

                                            MotionTimer.Stop();
                                            command.StatusStep = (int)EnumPTILoadStatus.Step16_Z軸下降;
                                            retrycount = 0;
                                            break;
                                        }
                                        else
                                        {
                                            NextStep = (int)EnumPTILoadStatus.Step15_等待Theta軸回0度_P軸回原點完成;
                                            command.StatusStep = (int)EnumPTILoadStatus.準備發呆;
                                            發呆數 = 200;
                                            retrycount++;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange);
                                        command.StatusStep = 999;
                                    }
                                }
                                if (MotionTimer.ElapsedMilliseconds > motiotimeoutvalue)
                                {
                                    JogStop();
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.RollerStopTimeout);
                                    command.StatusStep = 999;
                                }
                                break;
                            case (int)EnumPTILoadStatus.Step16_Z軸下降:
                                if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                                {
                                    if (!CVPtoP(EnumLoadUnloadAxisName.Z軸.ToString(), 0, 1, false, 0))
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                        command.StatusStep = 999;
                                    }
                                    else
                                    {
                                        MotionTimer.Restart();
                                        NextStep = (int)EnumPTILoadStatus.Step17_等待Z軸下降;
                                        NextStepForconfirmcommand = (int)EnumPTILoadStatus.Step16_Z軸下降;
                                        command.StatusStep = (int)EnumPTILoadStatus.Step_確認命令成功;
                                        發呆數 = 150;
                                    }
                                }
                                break;
                            case (int)EnumPTILoadStatus.Step17_等待Z軸下降:
                                if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop))
                                {
                                    if (retrycount < 3)
                                    {
                                        if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.Z軸.ToString()].PlausUnit)) - 0 < 1)
                                        {
                                            if (command.BreakenStopMode)
                                                command.GoNext = false;

                                            MotionTimer.Stop();
                                            command.StatusStep = (int)EnumPTILoadStatus.Step18_LoadComplete;
                                            retrycount = 0;
                                            break;
                                        }
                                        else
                                        {
                                            NextStep = (int)EnumPTILoadStatus.Step17_等待Z軸下降;
                                            command.StatusStep = (int)EnumPTILoadStatus.準備發呆;
                                            發呆數 = 200;
                                            retrycount++;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange);
                                        command.StatusStep = 999;
                                    }
                                }
                                if (MotionTimer.ElapsedMilliseconds > motiotimeoutvalue)
                                {
                                    JogStop();
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.RollerStopTimeout);
                                    command.StatusStep = 999;
                                }
                                break;
                            case (int)EnumPTILoadStatus.Step18_LoadComplete:
                                if (command.NeedPIO)
                                {
                                    if (((PIOFlow_PTI_LoadUnloadSemi)RightPIO).Status == EnumPIOStatus.Complete)
                                    {
                                        command.CommandEndTime = DateTime.Now;
                                        UpdateLoadingAndCSTID();
                                        if (command.CommandResult != EnumLoadUnloadComplete.Error)
                                            command.CommandResult = EnumLoadUnloadComplete.End;

                                        if (command.NeedPIO)
                                            command.PIOResult = RightPIO.Timeout;

                                        command.StatusStep = (int)EnumPTILoadStatus.Step0_Idle;
                                        return;
                                    }
                                    if (PIOErrorStatus == EnumLoadUnloadErrorLevel.PrePIOError)
                                    {
                                        command.CommandEndTime = DateTime.Now;
                                        UpdateLoadingAndCSTID();
                                        if (command.CommandResult != EnumLoadUnloadComplete.Error)
                                            command.CommandResult = EnumLoadUnloadComplete.Interlock;

                                        if (command.NeedPIO)
                                            command.PIOResult = RightPIO.Timeout;

                                        command.StatusStep = (int)EnumPTILoadStatus.Step0_Idle;
                                        return;
                                    }
                                    else if (PIOErrorStatus == EnumLoadUnloadErrorLevel.AfterPIOError)
                                    {
                                        command.CommandEndTime = DateTime.Now;
                                        UpdateLoadingAndCSTID();
                                        if (command.CommandResult != EnumLoadUnloadComplete.Error)
                                            command.CommandResult = EnumLoadUnloadComplete.End;

                                        if (command.NeedPIO)
                                            command.PIOResult = RightPIO.Timeout;

                                        command.StatusStep = (int)EnumPTILoadStatus.Step0_Idle;
                                        return;
                                    }
                                }
                                else
                                {
                                    command.CommandEndTime = DateTime.Now;
                                    UpdateLoadingAndCSTID();
                                    if (command.CommandResult != EnumLoadUnloadComplete.Error)
                                        command.CommandResult = EnumLoadUnloadComplete.End;

                                    if (command.NeedPIO)
                                        command.PIOResult = RightPIO.Timeout;

                                    command.StatusStep = (int)EnumPTILoadStatus.Step0_Idle;
                                    return;
                                }
                                break;
                            case (int)EnumPTILoadStatus.Step_EmptyRetrival:
                                command.CommandEndTime = DateTime.Now;
                                UpdateLoadingAndCSTID();
                                if (command.CommandResult != EnumLoadUnloadComplete.Error)
                                    command.CommandResult = EnumLoadUnloadComplete.EmptyRetrival;

                                if (command.NeedPIO)
                                    command.PIOResult = RightPIO.Timeout;

                                command.StatusStep = (int)EnumPTILoadStatus.Step0_Idle;
                                break;
                            case (int)EnumPTILoadStatus.Step999_LoadFail:
                                JogStop();
                                command.CommandEndTime = DateTime.Now;
                                command.CommandResult = EnumLoadUnloadComplete.Error;
                                command.StatusStep = (int)EnumPTILoadStatus.Step0_Idle;
                                return;
                        }

                        if (command.StepString != ((EnumPTILoadStatus)command.StatusStep).ToString())
                        {
                            if (logMode)
                                WriteLog(7, "", String.Concat("Step Change to : ", ((EnumPTILoadStatus)command.StatusStep).ToString()));

                            command.StepString = ((EnumPTILoadStatus)command.StatusStep).ToString();
                        }
                        break;
                    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    case EnumLoadUnload.Unload:


                        #region PIO異常

                        if (ignorePIOError == false)
                        {
                            if (command.StatusStep != (int)EnumPTIUnloadStatus.Step999_LoadFail)
                            {
                                if (RightPIO.Timeout != EnumPIOStatus.None)
                                {
                                    if ((command.StatusStep > 0 && (int)command.StatusStep < (int)EnumPTIUnloadStatus.Step10_Fork伸出) ||
                                            ((int)command.StatusStep < 0 && (int)NextStep < (int)EnumPTIUnloadStatus.Step10_Fork伸出)
                                            )
                                    {
                                        #region PrePIOError
                                        ignorePIOError = true;
                                        PIOErrorStatus = EnumLoadUnloadErrorLevel.PrePIOError;
                                        JogStop();
                                        Thread.Sleep(1000);
                                        command.StatusStep = (int)EnumPTIUnloadStatus.Step_ForPrePIOErrorTheta軸回0度_P軸回原點;
                                        #endregion
                                    }
                                    else if (((int)command.StatusStep > 0 && (int)command.StatusStep < (int)EnumPTIUnloadStatus.Step14_Fork收回 + 1) ||
                                            ((int)command.StatusStep < 0 && (int)NextStep < (int)EnumPTIUnloadStatus.Step14_Fork收回 + 1)
                                             )
                                    {
                                        #region PIOError 
                                        ignorePIOError = false;
                                        PIOErrorStatus = EnumLoadUnloadErrorLevel.Error;
                                        JogStop();
                                        command.StatusStep = (int)EnumPTIUnloadStatus.Step999_LoadFail;
                                        #endregion
                                    }
                                    else
                                    {
                                        #region AftPIOError 
                                        ignorePIOError = true;
                                        PIOErrorStatus = EnumLoadUnloadErrorLevel.AfterPIOError;
                                        #endregion
                                    }
                                }
                            }
                        }
                        #endregion

                        switch (localData.LoadUnloadData.LoadUnloadCommand.StatusStep)
                        {
                            case (int)EnumPTIUnloadStatus.準備發呆:
                                已發呆數 = 0;
                                command.StatusStep = (int)EnumPTIUnloadStatus.發呆;
                                break;

                            case (int)EnumPTIUnloadStatus.發呆:
                                if (已發呆數 == 發呆數 * 0.5)
                                {
                                    發呆數 = 0;
                                    command.StatusStep = NextStep;
                                    NextStep = 0;
                                    break;
                                }
                                已發呆數++;
                                break;

                            case (int)EnumPTIUnloadStatus.Step_確認命令成功:
                                Thread.Sleep(500);
                                if (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].PositionCommand == 0 &&
                                   localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].PositionCommand == 0 &&
                                    localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Y軸.ToString()].PositionCommand == 0 &&
                                    localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].PositionCommand == 0
                                     )
                                {
                                    CMDRetryCount = 0;
                                    command.StatusStep = (int)EnumPTIUnloadStatus.準備發呆;
                                    break;
                                }
                                else if (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].PositionCommand == -1 ||
                                   localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].PositionCommand == -1 ||
                                    localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Y軸.ToString()].PositionCommand == -1 ||
                                    localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].PositionCommand == -1
                                     )
                                {
                                    if (CMDRetryCount > 3)
                                    {
                                        command.StatusStep = 999;
                                        break;
                                    }
                                    CMDRetryCount++;
                                    command.StatusStep = NextStepForconfirmcommand;
                                    break;
                                }
                                else
                                {

                                }
                                break;

                            case (int)EnumPTIUnloadStatus.Step_PIO開始:
                                if (command.BreakenStopMode)
                                    command.GoNext = false;

                                if (command.NeedPIO && !ignorePIOError)
                                {
                                    ((PIOFlow_PTI_LoadUnloadSemi)RightPIO).PIO_SerChannel_ID(command.AddressID);

                                    RightPIO.PIOFlow_Load_UnLoad(command.Action);
                                    command.StatusStep = (int)EnumPTIUnloadStatus.Step_PIOSendBusy;
                                }
                                else
                                {
                                    command.StatusStep = (int)EnumPTIUnloadStatus.Step_PIOSendBusy;
                                }
                                break;

                            case (int)EnumPTIUnloadStatus.Step_PIOSendBusy:
                                if (command.NeedPIO && !ignorePIOError)
                                {
                                    if (RightPIO.Status == EnumPIOStatus.TP2)
                                    {
                                        ((PIOFlow_PTI_LoadUnloadSemi)RightPIO).SendBusy = true;

                                        NextStep = (int)EnumPTIUnloadStatus.Step1_檢查Loading_二重閣;
                                        command.StatusStep = (int)EnumPTIUnloadStatus.準備發呆;
                                        發呆數 = 100;
                                    }
                                    else
                                    {
                                    }
                                }
                                else
                                {
                                    NextStep = (int)EnumPTIUnloadStatus.Step1_檢查Loading_二重閣;
                                    command.StatusStep = (int)EnumPTIUnloadStatus.準備發呆;
                                }
                                break;
                            case (int)EnumPTIUnloadStatus.Step1_檢查Loading_二重閣:
                                if (AllAxisSensorData[EnumLoadUnloadAxisName.Y軸.ToString()][CST座_在席檢知].data == false)
                                {
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.放貨_LoadingOff異常);
                                    command.StatusStep = (int)EnumPTIUnloadStatus.Step999_LoadFail;
                                }
                                switch (command.StageDirection)
                                {
                                    case EnumStageDirection.Left:
                                        if (AllAxisSensorData[EnumLoadUnloadAxisName.Y軸.ToString()][二重格檢知_L].data == false)
                                        {
                                            command.StatusStep = (int)EnumPTIUnloadStatus.Step2_Z軸上升;
                                        }
                                        else
                                        {
                                            command.StatusStep = (int)EnumPTIUnloadStatus.Step_Doublestorage;
                                        }
                                        break;
                                    case EnumStageDirection.Right:
                                        if (AllAxisSensorData[EnumLoadUnloadAxisName.Y軸.ToString()][二重格檢知_R].data == false)
                                        {
                                            command.StatusStep = (int)EnumPTIUnloadStatus.Step2_Z軸上升;
                                        }
                                        else
                                        {
                                            command.StatusStep = (int)EnumPTIUnloadStatus.Step_Doublestorage;
                                        }
                                        break;
                                }
                                break;
                            case (int)EnumPTIUnloadStatus.Step2_Z軸上升:
                                if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                                {
                                    switch (command.StageDirection)
                                    {
                                        case EnumStageDirection.Left:
                                            encoderAbs_Z = LeftStageDataList[command.StageNumber].Encoder_Z +/* command.CommandStartAlignmentValue.Z +*/ LeftStageDataList[command.StageNumber].Benchmark_Z;
                                            break;
                                        case EnumStageDirection.Right:
                                            encoderAbs_Z = RightStageDataList[command.StageNumber].Encoder_Z +/* command.CommandStartAlignmentValue.Z +*/ RightStageDataList[command.StageNumber].Benchmark_Z;
                                           break;
                                    }
                                    if (!CVPtoP(EnumLoadUnloadAxisName.Z軸.ToString(), encoderAbs_Z, 1, false, 0))
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                        command.StatusStep = 999;
                                    }
                                    else
                                    {
                                        MotionTimer.Restart();
                                        NextStep = (int)EnumPTIUnloadStatus.Step3_等待Z軸上升完成;
                                        NextStepForconfirmcommand = (int)EnumPTIUnloadStatus.Step2_Z軸上升;
                                        command.StatusStep = (int)EnumPTIUnloadStatus.Step_確認命令成功;
                                        發呆數 = 150;
                                    }
                                }
                                break;
                            case (int)EnumPTIUnloadStatus.Step3_等待Z軸上升完成:
                                if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop))
                                {
                                    if (retrycount < 3)
                                    {
                                        if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.Z軸.ToString()].PlausUnit)) - encoderAbs_Z < 1)
                                        {
                                            if (command.BreakenStopMode)
                                                command.GoNext = false;

                                            MotionTimer.Stop();
                                            command.StatusStep = (int)EnumPTIUnloadStatus.Step4_Theta旋轉90度;
                                            retrycount = 0;
                                            break;
                                        }
                                        else
                                        {
                                            NextStep = (int)EnumPTIUnloadStatus.Step3_等待Z軸上升完成;
                                            command.StatusStep = (int)EnumPTIUnloadStatus.準備發呆;
                                            發呆數 = 200;
                                            retrycount++;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange);
                                        command.StatusStep = 999;
                                    }
                                }
                                if (MotionTimer.ElapsedMilliseconds > motiotimeoutvalue)
                                {
                                    JogStop();
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.RollerStopTimeout);
                                    command.StatusStep = 999;
                                }
                                break;
                            case (int)EnumPTIUnloadStatus.Step4_Theta旋轉90度:
                                if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                                {
                                    欲移動位置 = 90.5 - 補正retryCount * 0.05;
                                    switch (command.StageDirection)
                                    {
                                        case EnumStageDirection.Left:
                                            欲移動位置 = 欲移動位置 * 1;
                                            break;
                                        case EnumStageDirection.Right:
                                            欲移動位置 = 欲移動位置 * -1;
                                            break;
                                    }
                                    if (!CVPtoP(EnumLoadUnloadAxisName.Theta軸.ToString(), 欲移動位置, 1, false, 0))
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                        command.StatusStep = 999;
                                    }
                                    else
                                    {
                                        MotionTimer.Restart();
                                        NextStep = (int)EnumPTIUnloadStatus.Step5_等待Theta旋轉90度完成;
                                        NextStepForconfirmcommand = (int)EnumPTIUnloadStatus.Step4_Theta旋轉90度;
                                        command.StatusStep = (int)EnumPTIUnloadStatus.Step_確認命令成功;
                                        發呆數 = 150;
                                    }
                                }
                                break;
                            case (int)EnumPTIUnloadStatus.Step5_等待Theta旋轉90度完成:
                                if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop))
                                {
                                    if (retrycount < 3)
                                    {
                                        if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.Theta軸.ToString()].PlausUnit)) - 欲移動位置 < 1)
                                        {
                                            MotionTimer.Stop();
                                            command.StatusStep = (int)EnumPTIUnloadStatus.Step_CheckAlignmentValue_theta;
                                            retrycount = 0;
                                            break;
                                        }
                                        else
                                        {
                                            NextStep = (int)EnumPTIUnloadStatus.Step5_等待Theta旋轉90度完成;
                                            command.StatusStep = (int)EnumPTIUnloadStatus.準備發呆;
                                            發呆數 = 200;
                                            retrycount++;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange);
                                        command.StatusStep = 999;
                                    }
                                }
                                if (MotionTimer.ElapsedMilliseconds > motiotimeoutvalue)
                                {
                                    JogStop();
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.RollerStopTimeout);
                                    command.StatusStep = 999;
                                }
                                break;
                            /////////////////////////////////////////////////////////////////////////////////////////////////////////////
                            case (int)EnumPTIUnloadStatus.Step_CheckAlignmentValue_theta:
                                    CheckAlingmentValueByAddressID(command.AddressID);
                                    //CheckAlingmentValue(command.StageDirection, command.StageNumber);
                                    temp = localData.LoadUnloadData.AlignmentValue;

                                if (temp != null && temp.AlignmentVlaue)
                                {
                                    alignmentCount = 0;
                                    command.CommandStartAlignmentValue = temp;
                                    command.StatusStep = (int)EnumPTIUnloadStatus.Step_確認Encoder補正範圍_theta;
                                }
                                else
                                {
                                    alignmentCount++;

                                    if (alignmentCount >= maxAlignmentCount)
                                    {
                                        if (temp != null && (temp.LaserF == 0 || temp.LaserB == 0))
                                        {
                                            alignmentCount = 0;
                                            SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.AlignmentValueNG);
                                            command.StatusStep = (int)EnumPTILoadStatus.Step999_LoadFail;
                                        }
                                        else
                                        {
                                            firsttheta = 0;
                                            alignmentCount = 0;
                                            NextStepFor尋找BCR = (int)EnumPTIUnloadStatus.Step_CheckAlignmentValue_theta;
                                            command.StatusStep = (int)EnumPTIUnloadStatus.Step_角度偏差過大尋找BCR模式;
                                        }
                                    }
                                }
                                break;
                            case (int)EnumPTIUnloadStatus.Step_確認Encoder補正範圍_theta:
                                #region Step3_確認Encoder補正範圍.
                                if (command.UsingAlignmentValue)
                                {
                                    switch (command.StageDirection)
                                    {
                                        case EnumStageDirection.Left:
                                            encoderAbs_P = 0 + (command.CommandStartAlignmentValue.LASER_P / Math.Cos(command.CommandStartAlignmentValue.Theta * Math.PI / 180));
                                            encoderAbs_Theta = LeftStageDataList[command.StageNumber].Encoder_Theta + command.CommandStartAlignmentValue.Theta;
                                            break;
                                        case EnumStageDirection.Right:
                                            encoderAbs_P = 0 - (command.CommandStartAlignmentValue.LASER_P / Math.Cos(command.CommandStartAlignmentValue.Theta * Math.PI / 180));
                                            encoderAbs_Theta = RightStageDataList[command.StageNumber].Encoder_Theta + command.CommandStartAlignmentValue.Theta;
                                            break;
                                    }
                                    //encoderAbs_Z = RightStageDataList[command.StageNumber].Encoder_Z +/* command.CommandStartAlignmentValue.Z +*/ RightStageDataList[command.StageNumber].Benchmark_Z;
                                    //encoderAbs_Y = RightStageDataList[command.StageNumber].Encoder_Y + command.CommandStartAlignmentValue.Y;
                                    firsttheta = command.CommandStartAlignmentValue.Theta;
                                    WriteLog(7, "", String.Concat("Encoder P : ", encoderAbs_P.ToString("0.00"),
                                                                  "Encoder Theta : ", encoderAbs_Theta.ToString("0.00"),
                                                                  "Encoder Z : ", encoderAbs_Z.ToString("0.00")));


                                    if (axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PosLimit >= encoderAbs_P &&
                                           encoderAbs_P >= axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].NagLimit &&
                                        //axisDataList[EnumLoadUnloadAxisName.Z軸.ToString()].PosLimit >= encoderAbs_Z &&
                                        //   encoderAbs_Z >= axisDataList[EnumLoadUnloadAxisName.Z軸.ToString()].NagLimit &&
                                        axisDataList[EnumLoadUnloadAxisName.Theta軸.ToString()].PosLimit >= encoderAbs_Theta &&
                                           encoderAbs_Theta >= axisDataList[EnumLoadUnloadAxisName.Theta軸.ToString()].NagLimit/* &&*/
                                                                                                                              //axisDataList[EnumLoadUnloadAxisName.Y軸.ToString()].PosLimit >= encoderAbs_Y &&
                                                                                                                              //   encoderAbs_Y >= axisDataList[EnumLoadUnloadAxisName.Y軸.ToString()].NagLimit
                                        )
                                    {
                                        command.StatusStep = (int)EnumPTIUnloadStatus.Step_Theta軸補正;

                                        if (command.BreakenStopMode)
                                            command.GoNext = false;
                                    }
                                    else
                                    {
                                        command.StatusStep = (int)EnumPTIUnloadStatus.Step999_LoadFail;
                                    }
                                }
                                else
                                {
                                    command.StatusStep = (int)EnumPTIUnloadStatus.Step999_LoadFail;
                                }
                                #endregion
                                break;
                            case (int)EnumPTIUnloadStatus.Step_Theta軸補正:
                                if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                                {
                                    if (!CVPtoP(EnumLoadUnloadAxisName.Theta軸.ToString(), encoderAbs_Theta, 0.3, false, 0))
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                        command.StatusStep = 999;
                                    }
                                    else
                                    {
                                        Thread.Sleep(1000);
                                        if (!CVPtoP(EnumLoadUnloadAxisName.P軸.ToString(), encoderAbs_P, 0.3, false, 0))
                                        {
                                            JogStop();
                                            SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                            command.StatusStep = 999;
                                        }
                                        else
                                        {
                                            MotionTimer.Restart();
                                            NextStep = (int)EnumPTIUnloadStatus.Step_等待Theta軸補正完成;
                                            NextStepForconfirmcommand = (int)EnumPTIUnloadStatus.Step_Theta軸補正;
                                            command.StatusStep = (int)EnumPTIUnloadStatus.Step_確認命令成功;
                                            發呆數 = 100;
                                        }
                                    }
                                }
                                break;
                            case (int)EnumPTIUnloadStatus.Step_等待Theta軸補正完成:
                                if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop) &&
                                    (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop))
                                {
                                    if (retrycount < 3)
                                    {
                                        if (Math.Abs((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.Theta軸.ToString()].PlausUnit)) - encoderAbs_Theta) < 1 &&
                                          Math.Abs((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PlausUnit)) - encoderAbs_P) < 1)
                                        {
                                            MotionTimer.Stop();
                                            command.StatusStep = (int)EnumPTIUnloadStatus.Step_CheckAlignmentValue;
                                            retrycount = 0;
                                            break;
                                        }
                                        else
                                        {
                                            NextStep = (int)EnumPTIUnloadStatus.Step_等待Theta軸補正完成;
                                            command.StatusStep = (int)EnumPTIUnloadStatus.準備發呆;
                                            發呆數 = 200;
                                            retrycount++;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange);
                                        command.StatusStep = 999;
                                    }
                                }
                                if (MotionTimer.ElapsedMilliseconds > motiotimeoutvalue)
                                {
                                    JogStop();
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.RollerStopTimeout);
                                    command.StatusStep = 999;
                                }
                                break;

                            case (int)EnumPTIUnloadStatus.Step_CheckAlignmentValue:
                                    CheckAlingmentValueByAddressID(command.AddressID);
                                    //CheckAlingmentValue(command.StageDirection, command.StageNumber);
                                    temp = localData.LoadUnloadData.AlignmentValue;

                                if (temp != null && temp.AlignmentVlaue)
                                {
                                    alignmentCount = 0;
                                    command.CommandStartAlignmentValue = temp;
                                    command.StatusStep = (int)EnumPTIUnloadStatus.Step_確認Encoder補正範圍;
                                }
                                else
                                {
                                    alignmentCount++;

                                    if (alignmentCount >= maxAlignmentCount)
                                    {
                                        if (temp != null && (temp.LaserF == 0 || temp.LaserB == 0))
                                        {
                                            alignmentCount = 0;
                                            SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.AlignmentValueNG);
                                            command.StatusStep = (int)EnumPTILoadStatus.Step999_LoadFail;
                                        }
                                        else
                                        {
                                            alignmentCount = 0;
                                            NextStepFor尋找BCR = (int)EnumPTIUnloadStatus.Step_CheckAlignmentValue;
                                            command.StatusStep = (int)EnumPTIUnloadStatus.Step_角度偏差過大尋找BCR模式;
                                        }
                                    }
                                }
                                break;
                            case (int)EnumPTIUnloadStatus.Step_確認Encoder補正範圍:
                                #region Step3_確認Encoder補正範圍.
                                if (command.UsingAlignmentValue)
                                {
                                    if (Math.Abs(command.CommandStartAlignmentValue.Theta) < 0.1)
                                    {
                                        switch (command.StageDirection)
                                        {
                                            case EnumStageDirection.Left:
                                                encoderAbs_P = LeftStageDataList[command.StageNumber].Encoder_P + (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PlausUnit)) - (command.CommandStartAlignmentValue.P / Math.Cos(command.CommandStartAlignmentValue.Theta * Math.PI / 180));
                                                //encoderAbs_P -= 5;
                                                encoderAbs_Z = LeftStageDataList[command.StageNumber].Encoder_Z +/* command.CommandStartAlignmentValue.Z +*/ LeftStageDataList[command.StageNumber].Benchmark_Z;
                                                encoderAbs_Theta = LeftStageDataList[command.StageNumber].Encoder_Theta - command.CommandStartAlignmentValue.Theta;
                                                encoderAbs_Y = LeftStageDataList[command.StageNumber].Encoder_Y + command.CommandStartAlignmentValue.Y;
                                                break;
                                            case EnumStageDirection.Right:
                                                encoderAbs_P = RightStageDataList[command.StageNumber].Encoder_P + (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PlausUnit)) - (command.CommandStartAlignmentValue.P / Math.Cos(command.CommandStartAlignmentValue.Theta * Math.PI / 180));

                                                encoderAbs_Z = RightStageDataList[command.StageNumber].Encoder_Z +/* command.CommandStartAlignmentValue.Z +*/ RightStageDataList[command.StageNumber].Benchmark_Z;
                                                encoderAbs_Theta = RightStageDataList[command.StageNumber].Encoder_Theta - command.CommandStartAlignmentValue.Theta;
                                                encoderAbs_Y = RightStageDataList[command.StageNumber].Encoder_Y + command.CommandStartAlignmentValue.Y;
                                                break;
                                        }
                                        WriteLog(7, "", String.Concat("Encoder P : ", encoderAbs_P.ToString("0.00"),
                                                                      "Encoder Theta : ", encoderAbs_Theta.ToString("0.00"),
                                                                      "Encoder Z : ", encoderAbs_Z.ToString("0.00")));

                                        if (axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PosLimit >= encoderAbs_P &&
                                               encoderAbs_P >= axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].NagLimit &&
                                               axisDataList[EnumLoadUnloadAxisName.Z軸.ToString()].PosLimit >= encoderAbs_Z &&
                                               encoderAbs_Z >= axisDataList[EnumLoadUnloadAxisName.Z軸.ToString()].NagLimit &&
                                               axisDataList[EnumLoadUnloadAxisName.Theta軸.ToString()].PosLimit >= encoderAbs_Theta &&
                                               encoderAbs_Theta >= axisDataList[EnumLoadUnloadAxisName.Theta軸.ToString()].NagLimit &&
                                               axisDataList[EnumLoadUnloadAxisName.Y軸.ToString()].PosLimit >= encoderAbs_Y &&
                                               encoderAbs_Y >= axisDataList[EnumLoadUnloadAxisName.Y軸.ToString()].NagLimit
                                            )
                                        {
                                            補正retryCount = 0;
                                            command.StatusStep = (int)EnumPTIUnloadStatus.Step_P軸補正;

                                            if (command.BreakenStopMode)
                                                command.GoNext = false;
                                        }
                                        else
                                        {
                                            command.StatusStep = (int)EnumPTIUnloadStatus.Step999_LoadFail;
                                        }
                                    }
                                    else
                                    {
                                        if (補正retryCount > 3)
                                        {
                                            SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.AlignmentValueNG);
                                            command.StatusStep = (int)EnumPTILoadStatus.Step999_LoadFail;
                                            break;
                                        }
                                        補正retryCount++;
                                        command.StatusStep = (int)EnumPTIUnloadStatus.Step_回原點ForRetry補正;
                                    }
                                }
                                else
                                {
                                    command.StatusStep = (int)EnumPTIUnloadStatus.Step999_LoadFail;
                                }
                                #endregion
                                break;

                            case (int)EnumPTIUnloadStatus.Step_回原點ForRetry補正:
                                if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                                {
                                    if (!CVPtoP(EnumLoadUnloadAxisName.P軸.ToString(), 0, 1, false, 0))
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                        command.StatusStep = 999;
                                    }
                                    else
                                    {
                                        Thread.Sleep(1000);
                                        if (!CVPtoP(EnumLoadUnloadAxisName.Theta軸.ToString(), 0, 1, false, 0))
                                        {
                                            JogStop();
                                            SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                            command.StatusStep = 999;
                                        }
                                        else
                                        {
                                            MotionTimer.Restart();
                                            NextStep = (int)EnumPTIUnloadStatus.Step_等待回原點ForRetry補正;
                                            NextStepForconfirmcommand = (int)EnumPTIUnloadStatus.Step_回原點ForRetry補正;
                                            command.StatusStep = (int)EnumPTIUnloadStatus.Step_確認命令成功;
                                            發呆數 = 150;
                                        }
                                    }
                                }
                                break;
                            case (int)EnumPTIUnloadStatus.Step_等待回原點ForRetry補正:
                                if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop) &&
                                (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop))
                                {
                                    if (retrycount < 3)
                                    {
                                        if (Math.Abs((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.Theta軸.ToString()].PlausUnit)) - 0) < 1 &&
                                       Math.Abs((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PlausUnit)) - 0) < 1)
                                        {
                                            if (command.BreakenStopMode)
                                                command.GoNext = false;

                                            MotionTimer.Stop();
                                            command.StatusStep = (int)EnumPTIUnloadStatus.Step4_Theta旋轉90度;
                                            retrycount = 0;
                                            break;
                                        }
                                        else
                                        {
                                            NextStep = (int)EnumPTIUnloadStatus.Step_等待回原點ForRetry補正;
                                            command.StatusStep = (int)EnumPTIUnloadStatus.準備發呆;
                                            發呆數 = 200;
                                            retrycount++;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange);
                                        command.StatusStep = 999;
                                    }
                                }
                                if (MotionTimer.ElapsedMilliseconds > motiotimeoutvalue)
                                {
                                    JogStop();
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.RollerStopTimeout);
                                    command.StatusStep = 999;
                                }
                                break;
                            case (int)EnumPTIUnloadStatus.Step_角度偏差過大尋找BCR模式:
                                if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop))
                                {
                                    CheckAlingmentValueByAddressID(command.AddressID);
                                    //CheckAlingmentValue(command.StageDirection, command.StageNumber);
                                    temp = localData.LoadUnloadData.AlignmentValue;

                                    if (temp != null && temp.AlignmentVlaue)
                                    {
                                        NextStep = NextStepFor尋找BCR;
                                        NextStepFor尋找BCR = 0;
                                        command.StatusStep = (int)EnumPTIUnloadStatus.準備發呆;
                                        發呆數 = 100;
                                    }
                                    else
                                    {
                                        encoderAbs_P = 0;
                                        if (firsttheta == 0)
                                        {
                                            firstthetais0_axispjogrange += 5;
                                            firstthetais0_axispjogdirection *= -1;
                                            firstthetais0_axispnextposition = firstthetais0_axispnextposition + firstthetais0_axispjogrange * firstthetais0_axispjogdirection;
                                            encoderAbs_P = firstthetais0_axispnextposition;

                                        }
                                        else
                                        {
                                            switch (command.StageDirection)
                                            {
                                                case EnumStageDirection.Left:
                                                if (firsttheta < 0)
                                                    {
                                                        encoderAbs_P = (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PlausUnit)) - 5;
                                                    }
                                                    else
                                                    {
                                                        encoderAbs_P = (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PlausUnit)) + 5;
                                                    }
                                                    break;
                                                case EnumStageDirection.Right:
                                                    if (firsttheta < 0)
                                                    {
                                                        encoderAbs_P = (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PlausUnit)) + 5;
                                                    }
                                                    else
                                                    {
                                                        encoderAbs_P = (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PlausUnit)) - 5;
                                                    }
                                                    break;
                                            }
                                        }
                                        if (!CVPtoP(EnumLoadUnloadAxisName.P軸.ToString(), encoderAbs_P, 1, false, 0))
                                        {
                                        }
                                        else
                                        {
                                            NextStep = (int)EnumPTIUnloadStatus.Step_角度偏差過大尋找BCR模式;
                                            command.StatusStep = (int)EnumPTIUnloadStatus.準備發呆;
                                            發呆數 = 100;
                                        }
                                    }
                                }
                                break;
                            case (int)EnumPTIUnloadStatus.Step_P軸補正:
                                if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                                {
                                    if (!CVPtoP(EnumLoadUnloadAxisName.P軸.ToString(), encoderAbs_P, 0.3, false, 0))
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                        command.StatusStep = 999;
                                    }
                                    else
                                    {
                                        MotionTimer.Restart();
                                        NextStep = (int)EnumPTIUnloadStatus.Step_等待P軸補正完成;
                                        NextStepForconfirmcommand = (int)EnumPTIUnloadStatus.Step_P軸補正;
                                        command.StatusStep = (int)EnumPTIUnloadStatus.Step_確認命令成功;
                                        發呆數 = 100;
                                    }
                                }
                                break;
                            case (int)EnumPTIUnloadStatus.Step_等待P軸補正完成:
                                if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop))
                                {
                                    if (retrycount < 3)
                                    {
                                        if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PlausUnit)) - encoderAbs_P < 1)
                                        {
                                            if (command.BreakenStopMode)
                                                command.GoNext = false;

                                            MotionTimer.Stop();
                                            command.StatusStep = (int)EnumPTIUnloadStatus.Step10_Fork伸出;
                                            retrycount = 0;
                                            break;
                                        }
                                        else
                                        {
                                            NextStep = (int)EnumPTIUnloadStatus.Step_等待P軸補正完成;
                                            command.StatusStep = (int)EnumPTIUnloadStatus.準備發呆;
                                            發呆數 = 200;
                                            retrycount++;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange);
                                        command.StatusStep = 999;
                                    }
                                }

                                if (AllAxisSensorData[EnumLoadUnloadAxisName.P軸.ToString()][P軸前定位].data == true ||
                                   AllAxisSensorData[EnumLoadUnloadAxisName.P軸.ToString()][P軸後定位].data == true)
                                {
                                    JogStop();
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放中極限觸發);
                                    command.StatusStep = 999;
                                }
                                if (MotionTimer.ElapsedMilliseconds > motiotimeoutvalue)
                                {
                                    JogStop();
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.RollerStopTimeout);
                                    command.StatusStep = 999;
                                }
                                break;
                            /////////////////////////////////////////////////////////////////////////////////////////////////////////////


                            case (int)EnumPTIUnloadStatus.Step10_Fork伸出:
                                if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                                {
                                    if (!CVPtoP(EnumLoadUnloadAxisName.Y軸.ToString(), encoderAbs_Y, 0.5, false, 0))
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                        command.StatusStep = 999;
                                    }
                                    else
                                    {
                                        MotionTimer.Restart();
                                        NextStep = (int)EnumPTIUnloadStatus.Step11_等待Fork伸出完成;
                                        NextStepForconfirmcommand = (int)EnumPTIUnloadStatus.Step10_Fork伸出;
                                        command.StatusStep = (int)EnumPTIUnloadStatus.Step_確認命令成功;
                                        發呆數 = 150;
                                    }
                                }
                                break;
                            case (int)EnumPTIUnloadStatus.Step11_等待Fork伸出完成:
                                if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Y軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop))
                                {
                                    if (retrycount < 3)
                                    {
                                        if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Y軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.Y軸.ToString()].PlausUnit)) - encoderAbs_Y < 1)
                                        {
                                            if (command.BreakenStopMode)
                                                command.GoNext = false;

                                            MotionTimer.Stop();
                                            command.StatusStep = (int)EnumPTIUnloadStatus.Step12_Z軸下降;
                                            retrycount = 0;
                                            break;
                                        }
                                        else
                                        {
                                            NextStep = (int)EnumPTIUnloadStatus.Step11_等待Fork伸出完成;
                                            command.StatusStep = (int)EnumPTIUnloadStatus.準備發呆;
                                            發呆數 = 200;
                                            retrycount++;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange);
                                        command.StatusStep = 999;
                                    }
                                }
                                if (MotionTimer.ElapsedMilliseconds > motiotimeoutvalue)
                                {
                                    JogStop();
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.RollerStopTimeout);
                                    command.StatusStep = 999;
                                }
                                break;
                            case (int)EnumPTIUnloadStatus.Step12_Z軸下降:
                                if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                                {
                                    if (!CVPtoP(EnumLoadUnloadAxisName.Z軸.ToString(), 0, 1, false, 0))
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                        command.StatusStep = 999;
                                    }
                                    else
                                    {
                                        MotionTimer.Restart();
                                        NextStep = (int)EnumPTIUnloadStatus.Step13_等待Z軸下降;
                                        NextStepForconfirmcommand = (int)EnumPTIUnloadStatus.Step12_Z軸下降;
                                        command.StatusStep = (int)EnumPTIUnloadStatus.Step_確認命令成功;
                                        發呆數 = 150;
                                    }
                                }
                                break;
                            case (int)EnumPTIUnloadStatus.Step13_等待Z軸下降:
                                if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop))
                                {
                                    if (retrycount < 3)
                                    {
                                        if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.Z軸.ToString()].PlausUnit)) - 0 < 1)
                                        {
                                            if (AllAxisSensorData[EnumLoadUnloadAxisName.Y軸.ToString()][FORK_在席檢知].data)
                                            {
                                                JogStop();
                                                SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.Loading邏輯和Sensor不相符);
                                                command.StatusStep = 999;
                                                break;
                                            }
                                            if (command.BreakenStopMode)
                                                command.GoNext = false;

                                            MotionTimer.Stop();
                                            command.StatusStep = (int)EnumPTIUnloadStatus.Step14_Fork收回;
                                            retrycount = 0;
                                            break;
                                        }
                                        else
                                        {
                                            NextStep = (int)EnumPTIUnloadStatus.Step13_等待Z軸下降;
                                            command.StatusStep = (int)EnumPTIUnloadStatus.準備發呆;
                                            發呆數 = 200;
                                            retrycount++;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange);
                                        command.StatusStep = 999;
                                    }
                                }
                                if (MotionTimer.ElapsedMilliseconds > motiotimeoutvalue)
                                {
                                    JogStop();
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.RollerStopTimeout);
                                    command.StatusStep = 999;
                                }
                                break;
                            case (int)EnumPTIUnloadStatus.Step14_Fork收回:
                                if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                                {
                                    if (!CVPtoP(EnumLoadUnloadAxisName.Y軸.ToString(), 0, 1, false, 0))
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                        command.StatusStep = 999;
                                    }
                                    else
                                    {
                                        MotionTimer.Restart();
                                        NextStep = (int)EnumPTIUnloadStatus.Step15_等待Fork收回完成;
                                        NextStepForconfirmcommand = (int)EnumPTIUnloadStatus.Step14_Fork收回;
                                        command.StatusStep = (int)EnumPTIUnloadStatus.Step_確認命令成功;
                                        發呆數 = 150;
                                    }
                                }
                                break;
                            case (int)EnumPTIUnloadStatus.Step15_等待Fork收回完成:
                                if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Y軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop))
                                {
                                    if (retrycount < 3)
                                    {
                                        if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Y軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.Y軸.ToString()].PlausUnit)) - 0 < 1)
                                        {
                                            if (command.BreakenStopMode)
                                                command.GoNext = false;

                                            MotionTimer.Stop();
                                            command.StatusStep = (int)EnumPTIUnloadStatus.Step_PIOSendComplete;
                                            retrycount = 0;
                                            break;
                                        }
                                        else
                                        {
                                            NextStep = (int)EnumPTIUnloadStatus.Step15_等待Fork收回完成;
                                            command.StatusStep = (int)EnumPTIUnloadStatus.準備發呆;
                                            發呆數 = 200;
                                            retrycount++;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange);
                                        command.StatusStep = 999;
                                        break;
                                    }
                                }
                                if (AllAxisSensorData[EnumLoadUnloadAxisName.Y軸.ToString()][axisNagLimitSensor[EnumLoadUnloadAxisName.Y軸.ToString()]].Data)
                                {
                                    if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Y軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.Y軸.ToString()].PlausUnit)) - 0 < 1.5)
                                    {
                                        JogStop();
                                        NextStep = (int)EnumPTIUnloadStatus.Step15_等待Fork收回完成;
                                        command.StatusStep = (int)EnumPTIUnloadStatus.準備發呆;
                                        發呆數 = 50;
                                        break;
                                    }
                                }
                                if (MotionTimer.ElapsedMilliseconds > motiotimeoutvalue)
                                {
                                    JogStop();
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.RollerStopTimeout);
                                    command.StatusStep = 999;
                                    break;
                                }
                                break;
                            case (int)EnumPTIUnloadStatus.Step_PIOSendComplete:
                                if (command.NeedPIO && !ignorePIOError)
                                {
                                    if (((PIOFlow_PTI_LoadUnloadSemi)RightPIO).Status == EnumPIOStatus.TP4)
                                    {
                                        ((PIOFlow_PTI_LoadUnloadSemi)RightPIO).SendComplete = true;
                                        NextStep = (int)EnumPTIUnloadStatus.Step16_Theta軸回0度_P軸回原點;
                                        command.StatusStep = (int)EnumPTIUnloadStatus.準備發呆;
                                        發呆數 = 50;
                                    }
                                }
                                else
                                {
                                    NextStep = (int)EnumPTIUnloadStatus.Step16_Theta軸回0度_P軸回原點;
                                    command.StatusStep = (int)EnumPTIUnloadStatus.準備發呆;
                                }
                                break;
                            case (int)EnumPTIUnloadStatus.Step16_Theta軸回0度_P軸回原點:
                                if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                                {

                                    if (!CVPtoP(EnumLoadUnloadAxisName.P軸.ToString(), 0, 0.3, false, 0))
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                        command.StatusStep = 999;
                                    }
                                    else
                                    {
                                        Thread.Sleep(1000);
                                        if (!CVPtoP(EnumLoadUnloadAxisName.Theta軸.ToString(), 0, 0.3, false, 0))
                                        {
                                            JogStop();
                                            SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                            command.StatusStep = 999;
                                        }
                                        else
                                        {
                                            MotionTimer.Restart();
                                            NextStep = (int)EnumPTIUnloadStatus.Step17_等待Theta軸回0度_P軸回原點完成;
                                            NextStepForconfirmcommand = (int)EnumPTIUnloadStatus.Step16_Theta軸回0度_P軸回原點;
                                            command.StatusStep = (int)EnumPTIUnloadStatus.Step_確認命令成功;
                                            發呆數 = 150;
                                        }
                                    }
                                }
                                break;
                            case (int)EnumPTIUnloadStatus.Step17_等待Theta軸回0度_P軸回原點完成:
                                if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop) &&
                                    (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop))
                                {
                                    if (retrycount < 3)
                                    {
                                        if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.Theta軸.ToString()].PlausUnit)) - 0 < 1 &&
                                          (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PlausUnit)) - 0 < 1)
                                        {
                                            if (command.BreakenStopMode)
                                                command.GoNext = false;

                                            MotionTimer.Stop();
                                            command.StatusStep = (int)EnumPTIUnloadStatus.Step18_UnloadComplete;
                                            retrycount = 0;
                                            break;
                                        }
                                        else
                                        {
                                            NextStep = (int)EnumPTIUnloadStatus.Step17_等待Theta軸回0度_P軸回原點完成;
                                            command.StatusStep = (int)EnumPTIUnloadStatus.準備發呆;
                                            發呆數 = 200;
                                            retrycount++;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange);
                                        command.StatusStep = 999;
                                    }
                                }
                                if (MotionTimer.ElapsedMilliseconds > motiotimeoutvalue)
                                {
                                    JogStop();
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.RollerStopTimeout);
                                    command.StatusStep = 999;
                                }
                                break;
                            case (int)EnumPTIUnloadStatus.Step18_UnloadComplete:
                                if (command.NeedPIO)
                                {
                                    if (((PIOFlow_PTI_LoadUnloadSemi)RightPIO).Status == EnumPIOStatus.Complete)
                                    {
                                        UpdateLoadingAndCSTID();
                                        command.CommandEndTime = DateTime.Now;

                                        if (command.CommandResult != EnumLoadUnloadComplete.Error)
                                            command.CommandResult = EnumLoadUnloadComplete.End;

                                        if (command.NeedPIO)
                                            command.PIOResult = RightPIO.Timeout;

                                        command.StatusStep = (int)EnumPTIUnloadStatus.Step0_Idle;
                                        return;
                                    }
                                    if (PIOErrorStatus == EnumLoadUnloadErrorLevel.PrePIOError)
                                    {
                                        command.CommandEndTime = DateTime.Now;
                                        UpdateLoadingAndCSTID();
                                        if (command.CommandResult != EnumLoadUnloadComplete.Error)
                                            command.CommandResult = EnumLoadUnloadComplete.Interlock;

                                        if (command.NeedPIO)
                                            command.PIOResult = RightPIO.Timeout;

                                        command.StatusStep = (int)EnumPTILoadStatus.Step0_Idle;
                                        return;
                                    }
                                    else if (PIOErrorStatus == EnumLoadUnloadErrorLevel.AfterPIOError)
                                    {
                                        command.CommandEndTime = DateTime.Now;
                                        UpdateLoadingAndCSTID();
                                        if (command.CommandResult != EnumLoadUnloadComplete.Error)
                                            command.CommandResult = EnumLoadUnloadComplete.End;

                                        if (command.NeedPIO)
                                            command.PIOResult = RightPIO.Timeout;

                                        command.StatusStep = (int)EnumPTILoadStatus.Step0_Idle;
                                        return;
                                    }
                                }
                                else
                                {
                                    UpdateLoadingAndCSTID();
                                    command.CommandEndTime = DateTime.Now;

                                    if (command.CommandResult != EnumLoadUnloadComplete.Error)
                                        command.CommandResult = EnumLoadUnloadComplete.End;

                                    if (command.NeedPIO)
                                        command.PIOResult = RightPIO.Timeout;

                                    command.StatusStep = (int)EnumPTIUnloadStatus.Step0_Idle;
                                    return;
                                }
                                break;

                            case (int)EnumPTIUnloadStatus.Step_Doublestorage:
                                UpdateLoadingAndCSTID();
                                command.CommandEndTime = DateTime.Now;

                                if (command.CommandResult != EnumLoadUnloadComplete.Error)
                                    command.CommandResult = EnumLoadUnloadComplete.DoubleStorage;

                                if (command.NeedPIO)
                                    command.PIOResult = RightPIO.Timeout;

                                command.StatusStep = (int)EnumPTIUnloadStatus.Step0_Idle;
                                return;
                            case (int)EnumPTIUnloadStatus.Step999_LoadFail:
                                JogStop();
                                command.CommandEndTime = DateTime.Now;
                                command.CommandResult = EnumLoadUnloadComplete.Error;
                                command.StatusStep = (int)EnumPTILoadStatus.Step0_Idle;
                                return;
                            case (int)EnumPTIUnloadStatus.Step_ForPrePIOErrorTheta軸回0度_P軸回原點:
                                if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                                {
                                    if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Y軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.Y軸.ToString()].PlausUnit)) - 0 > 1)
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.Fork不在Home點);
                                        command.StatusStep = 999;
                                    }
                                    if (!CVPtoP(EnumLoadUnloadAxisName.P軸.ToString(), 0, 1, false, 0))
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                        command.StatusStep = 999;
                                    }
                                    else
                                    {
                                        Thread.Sleep(1000);
                                        if (!CVPtoP(EnumLoadUnloadAxisName.Theta軸.ToString(), 0, 1, false, 0))
                                        {
                                            JogStop();
                                            SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                            command.StatusStep = 999;
                                        }
                                        else
                                        {
                                            MotionTimer.Restart();
                                            NextStep = (int)EnumPTIUnloadStatus.Step_ForPrePIOError等待Theta軸回0度_P軸回原點完成;
                                            NextStepForconfirmcommand = (int)EnumPTIUnloadStatus.Step_ForPrePIOErrorTheta軸回0度_P軸回原點;
                                            command.StatusStep = (int)EnumPTIUnloadStatus.Step_確認命令成功;
                                            發呆數 = 150;
                                        }
                                    }
                                }
                                break;
                            case (int)EnumPTIUnloadStatus.Step_ForPrePIOError等待Theta軸回0度_P軸回原點完成:
                                if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop) &&
                                    (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop))
                                {
                                    if (retrycount < 3)
                                    {
                                        if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.Theta軸.ToString()].PlausUnit)) - 0 < 1 &&
                                          (localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.P軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].PlausUnit)) - 0 < 1)
                                        {
                                            if (command.BreakenStopMode)
                                                command.GoNext = false;

                                            MotionTimer.Stop();
                                            command.StatusStep = (int)EnumPTIUnloadStatus.Step_ForPrePIOErrorZ軸下降;
                                            retrycount = 0;
                                            break;
                                        }
                                        else
                                        {
                                            NextStep = (int)EnumPTIUnloadStatus.Step_ForPrePIOError等待Theta軸回0度_P軸回原點完成;
                                            command.StatusStep = (int)EnumPTIUnloadStatus.準備發呆;
                                            發呆數 = 200;
                                            retrycount++;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange);
                                        command.StatusStep = 999;
                                    }
                                }
                                if (MotionTimer.ElapsedMilliseconds > motiotimeoutvalue)
                                {
                                    JogStop();
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.RollerStopTimeout);
                                    command.StatusStep = 999;
                                }
                                break;


                            case (int)EnumPTIUnloadStatus.Step_ForPrePIOErrorZ軸下降:
                                if (nowStatus == EnumSafetyLevel.Normal && (!command.BreakenStopMode || command.GoNext))
                                {
                                    if (!CVPtoP(EnumLoadUnloadAxisName.Z軸.ToString(), 0, 1, false, 0))
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail);
                                        command.StatusStep = 999;
                                    }
                                    else
                                    {
                                        MotionTimer.Restart();
                                        NextStep = (int)EnumPTIUnloadStatus.Step_ForPrePIOError等待Z軸下降;
                                        NextStepForconfirmcommand = (int)EnumPTIUnloadStatus.Step_ForPrePIOErrorZ軸下降;
                                        command.StatusStep = (int)EnumPTIUnloadStatus.Step_確認命令成功;
                                        發呆數 = 150;
                                    }
                                }
                                break;
                            case (int)EnumPTIUnloadStatus.Step_ForPrePIOError等待Z軸下降:
                                if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].AxisMoveStaus == EnumAxisMoveStatus.Stop))
                                {
                                    if (retrycount < 3)
                                    {
                                        if ((localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].Position / (float)(axisDataList[EnumLoadUnloadAxisName.Z軸.ToString()].PlausUnit)) - 0 < 1)
                                        {
                                            if (command.BreakenStopMode)
                                                command.GoNext = false;

                                            MotionTimer.Stop();
                                            command.StatusStep = (int)EnumPTIUnloadStatus.Step18_UnloadComplete;
                                            retrycount = 0;
                                            break;
                                        }
                                        else
                                        {
                                            NextStep = (int)EnumPTIUnloadStatus.Step_ForPrePIOError等待Z軸下降;
                                            command.StatusStep = (int)EnumPTIUnloadStatus.準備發呆;
                                            發呆數 = 200;
                                            retrycount++;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        JogStop();
                                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange);
                                        command.StatusStep = 999;
                                    }
                                }
                                if (MotionTimer.ElapsedMilliseconds > motiotimeoutvalue)
                                {
                                    JogStop();
                                    SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.RollerStopTimeout);
                                    command.StatusStep = 999;
                                }
                                break;
                        }

                        if (command.StepString != ((EnumPTIUnloadStatus)command.StatusStep).ToString())
                        {
                            if (logMode)
                                WriteLog(7, "", String.Concat("Step Change to : ", ((EnumPTIUnloadStatus)command.StatusStep).ToString()));

                            command.StepString = ((EnumPTIUnloadStatus)command.StatusStep).ToString();
                        }
                        break;
                    default:
                        SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode.拒絕取放命令_LoadUnloadControlNotReady);
                        break;
                }

            }

            if (localData.LoadUnloadData.LoadUnloadCommand.ErrorCode != EnumLoadUnloadControlErrorCode.None)
                SetAlarmCodeAndSetCommandErrorCode(localData.LoadUnloadData.LoadUnloadCommand.ErrorCode);
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
                                     ref double barcodeX, ref double barcodeY, ref double originX, ref double originY)
        {
            string barcodeString = "";
            string errorMessage = "";

            if (sr1000_R.ReadBarcode(ref barcodeString, 200, ref errorMessage))
            {
                string[] splitResult = Regex.Split(barcodeString, "[: / ,]+", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));

                if (splitResult.Length == 4 && BarcodeReaderIDToRealDistance.ContainsKey(splitResult[0]))
                {
                    if (Int32.Parse(Regex.Replace(splitResult[3], "[^0-9]", "")) < 200)
                    {
                        barcodeID = splitResult[0];
                        originY = double.Parse(splitResult[1]);
                        originX = double.Parse(splitResult[2]);

                        switch (direction)
                        {
                            case EnumStageDirection.Left:
                                barcodeX = ((double.Parse(splitResult[2]) - alignmentDeviceOffset.BarcodeReader_Right.Y) / RightStageBarocdeReaderSetting[stageNumber].PixelToMM_Y +
        BarcodeReaderIDToRealDistance[barcodeID])*-1;
                                break;
                            case EnumStageDirection.Right:
                                barcodeX = (double.Parse(splitResult[2]) - alignmentDeviceOffset.BarcodeReader_Right.Y) / RightStageBarocdeReaderSetting[stageNumber].PixelToMM_Y +
       BarcodeReaderIDToRealDistance[barcodeID];
                                break;
                        }
                       

                        barcodeY = (double.Parse(splitResult[1]) - alignmentDeviceOffset.BarcodeReader_Right.X) / RightStageBarocdeReaderSetting[stageNumber].PixelToMM_X;
                    }
                    else
                        WriteLog(5, "", String.Concat("Scant Time > 100 不使用資料"));
                }
                else
                    WriteLog(5, "", String.Concat("SR1000 Split.Length != 4, data : ", barcodeString, ", Length : ", splitResult.Length.ToString("0")));
            }
            else
            {
                sr1000_R.StopReadBarcode(ref barcodeString, ref errorMessage);
                WriteLog(5, "", String.Concat("SR1000 Read Fail : ", errorMessage));
            }
        }

        private void GetLaserValue(EnumStageDirection direction, ref double laser_F, ref double laser_B)
        {
            string data = "";

            if (distanceSensor_R.GetDistanceSensorData(ref data))
            {
                //WriteLog(7, "", data);

                string[] splitResult = Regex.Split(data, ",", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));

                if (splitResult.Length == 5)
                {
                    if (splitResult[1] == "02" || splitResult[1] == "01" || splitResult[1] == "04")
                        laser_B = (double.Parse(splitResult[2]) / 100) + alignmentDeviceOffset.LaserB_Right;
                    
                    //laser_F = 300 - (double.Parse(splitResult[2]) / 100);

                    if (splitResult[3] == "02" || splitResult[3] == "01" || splitResult[3] == "04")
                        laser_F = (double.Parse(splitResult[4]) / 100) + alignmentDeviceOffset.LaserF_Right; ;
                    //laser_B = 300 - (double.Parse(splitResult[4]) / 100);
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

            GetBarcodeXAndY(direction, stageNumber, ref barcodeID, ref barcodeX, ref barcodeY, ref originX, ref originY);
            GetLaserValue(direction, ref laser_F, ref laser_B);

            temp.LaserF = laser_F;
            temp.LaserB = laser_B;
            temp.BarcodePosition = new MapPosition(barcodeX, barcodeY);
            temp.OriginBarodePosition = new MapPosition(originX, originY);
            temp.BarcodeNumber = barcodeID;
            temp.Direction = direction;
            temp.StageNumber = stageNumber;
            if (laser_F == 0)
                WriteLog(5, "", String.Concat("DistanceSensor F read fail"));

            if (laser_B == 0)
                WriteLog(5, "", String.Concat("DistanceSensor B read fail"));

            if (barcodeID != "" && laser_F != 0 && laser_B != 0)
            {
                double theta = Math.Atan((laser_B - laser_F) /
                               Math.Abs(alignmentDeviceOffset.LaserB_Right_Locate - alignmentDeviceOffset.LaserF_Right_Locate)) * 180 / Math.PI;

                if (Math.Abs(theta) < 3)
                {
                    double p=0;
                    double just_laser_p=0;
                    double y=0;
                    double z=0;
                    switch (direction)
                    {
                        case EnumStageDirection.Left:
                            //theta = theta * -1;
                            p = barcodeX - Math.Sin(theta / 180 * Math.PI) * ((laser_F + laser_B) / 2 + alignmentDeviceOffset.BasedDistance)
                                - LeftStageDataList[stageNumber].Benchmark_P;
                             just_laser_p = Math.Sin(theta / 180 * Math.PI) * ((laser_F + laser_B) / 2 + alignmentDeviceOffset.BasedDistance);
                             y = 90 - (Math.Acos((Math.Pow(((laser_F + laser_B) / 2) + LeftStageDataList[stageNumber].Benchmark_Y, 2) - 2 * Math.Pow(300, 2))
                                / (2 * Math.Pow(300, 2)))) / 2 * 180 / Math.PI;
                             z = barcodeY - LeftStageDataList[stageNumber].Benchmark_Z;
                            theta -= LeftStageDataList[stageNumber].Benchmark_Theta;
                            break;
                        case EnumStageDirection.Right:
                             p = barcodeX - Math.Sin(theta / 180 * Math.PI) * ((laser_F + laser_B) / 2 + alignmentDeviceOffset.BasedDistance)
                                - RightStageDataList[stageNumber].Benchmark_P;
                             just_laser_p = Math.Sin(theta / 180 * Math.PI) * ((laser_F + laser_B) / 2 + alignmentDeviceOffset.BasedDistance);
                             y = 90 - (Math.Acos((Math.Pow(((laser_F + laser_B) / 2) + RightStageDataList[stageNumber].Benchmark_Y, 2) - 2 * Math.Pow(300, 2))
                                / (2 * Math.Pow(300, 2)))) / 2 * 180 / Math.PI;
                             z = barcodeY - RightStageDataList[stageNumber].Benchmark_Z;
                            theta -= RightStageDataList[stageNumber].Benchmark_Theta;
                            break;
                    }
                    temp.LASER_P = just_laser_p;
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
                    WriteLog(7, "", String.Concat("補償紀錄 : P:", p, ",LaserP:", just_laser_p, ",Y:", y, ",Theta:", theta,"LASER_F:",laser_F, ",LASER_B:",laser_B));

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
                        if (lastRightStageNumber != stageNumber)
                        {
                            if (RightStageBarocdeReaderSetting.ContainsKey(stageNumber))
                            {
                                lastRightStageNumber = stageNumber;

                                if (RightStageBarocdeReaderSetting[stageNumber].BarcodeReaderMode != sr1000_R_Mode)
                                {
                                    sr1000_R_Mode = RightStageBarocdeReaderSetting[stageNumber].BarcodeReaderMode;

                                    if (sr1000_R != null)
                                    {
                                        if (!sr1000_R.ChangeMode(sr1000_R_Mode, ref errorMessage))
                                            WriteLog(3, "", String.Concat("Change Mode Fail ErrorMessage : ", errorMessage));
                                    }
                                }
                            }
                            else
                                WriteLog(3, "", String.Concat("Fatal Error, RightStageBarocdeReaderSetting 中沒有StatgeNumber = ", stageNumber.ToString()));
                        }

                        GetAlignmentValue(direction, stageNumber);

                        break;
                    case EnumStageDirection.Right:
                        if (lastRightStageNumber != stageNumber)
                        {
                            if (RightStageBarocdeReaderSetting.ContainsKey(stageNumber))
                            {
                                lastRightStageNumber = stageNumber;

                                if (RightStageBarocdeReaderSetting[stageNumber].BarcodeReaderMode != sr1000_R_Mode)
                                {
                                    sr1000_R_Mode = RightStageBarocdeReaderSetting[stageNumber].BarcodeReaderMode;

                                    if (sr1000_R != null)
                                    {
                                        if (!sr1000_R.ChangeMode(sr1000_R_Mode, ref errorMessage))
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
                                    sr1000_R_Mode = RightStageBarocdeReaderSetting[stageNumber].BarcodeReaderMode;

                                    if (sr1000_R != null)
                                    {
                                        if (!sr1000_R.ChangeMode(sr1000_R_Mode, ref errorMessage))
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
                        if (temp.AxisMoveStaus == EnumAxisMoveStatus.Move || axisPreTimer[axisNameString].ElapsedMilliseconds > axisStatusDelayTime)
                        {
                            axisPreStatus[axisNameString] = EnumAxisMoveStatus.None;
                            return temp.AxisMoveStaus;
                        }
                        else
                            return axisPreStatus[axisNameString];

                    case EnumAxisMoveStatus.PreStop:
                        if (temp.AxisMoveStaus == EnumAxisMoveStatus.Stop || axisPreTimer[axisNameString].ElapsedMilliseconds > axisStatusDelayTime)
                        {
                            axisPreStatus[axisNameString] = EnumAxisMoveStatus.None;
                            return temp.AxisMoveStaus;
                        }
                        else
                            return axisPreStatus[axisNameString];

                    default:
                        axisPreStatus[axisNameString] = EnumAxisMoveStatus.None;
                        return temp.AxisMoveStaus;
                }
            }
        }
        #endregion

        private bool CVPtoP(string axisNameString, double encoder, double speedPercent, bool waitStop, double timeoutValue)
        {
            AxisFeedbackData temp = localData.LoadUnloadData.CVFeedbackData[axisNameString];

            if (temp == null)
                return false;
            else if (temp.AxisServoOnOff == EnumAxisServoOnOff.ServoOff)
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
                        (float)(axisDataList[axisNameString].AutoAcceleration * speedPercent),
                        (float)(axisDataList[axisNameString].AutoDeceleration * speedPercent),
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

            if (temp == null)
                return false;
            //else if (temp.AxisServoOnOff == EnumAxisServoOnOff.ServoOff)
            //    return false;

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
                    AxisFeedbackData temp = localData.LoadUnloadData.CVFeedbackData[axisNameString];
                    
                    while (!(temp.AxisServoOnOff == EnumAxisServoOnOff.ServoOn))
                    {
                        if (timer.ElapsedMilliseconds > timeout)
                        {
                            WriteLog(5, "", String.Concat("CV : ", axisNameString, " Wait ServoOn Timeout"));
                            return false;
                        }

                        Thread.Sleep(sleepTime);

                        temp = localData.LoadUnloadData.CVFeedbackData[axisNameString];
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
                WriteLog(7, "", String.Concat(axisNameString, "Servo off, waitServoOn : ", waitServoOn.ToString()));

            tagNameList.Add(axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.ServoOn]);
            valueList.Add((float)((int)EnumMIPCServoOnOffValue.ServoOn));

            if (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString())
            {
                tagNameList.Add(String.Concat(EnumLoadUnloadAxisName.Z軸_Slave.ToString(), "_", EnumLoadUnloadAxisCommandType.ServoOn));
                valueList.Add((float)((int)EnumMIPCServoOnOffValue.ServoOff));
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
        #endregion

        public override void Jog(int indexOfAxis, bool direction)
        {
            try
            {
                if (!configAllOK)
                    return;

                if (!CheckAixsIsServoOn(AxisList[indexOfAxis]))
                    ServoOn(AxisList[indexOfAxis], false, 0);
                else
                {
                    if (!AxisJog(AxisList[indexOfAxis], direction, (JogByPass ? axisDataList[AxisList[indexOfAxis]].JogSpeed[EnumLoadUnloadJogSpeed.Low] : axisDataList[AxisList[indexOfAxis]].JogSpeed[JogSpeed])))
                        WriteLog(5, "", String.Concat("CV Jog Fail"));
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

                if (!CheckAixsIsServoOn(axisName))
                    ServoOn(axisName, false, 0);
                else
                {
                    if (!CVPtoP(axisName, localData.LoadUnloadData.CVFeedbackData[axisName].Position + deltaEncoder, 0.1, false, 0))
                        WriteLog(5, "", String.Concat("CV Jog Fail"));
                }
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
                axisPreStatus[axis.ToString()] = EnumAxisMoveStatus.PreStop;
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
                axisPreStatus[EnumLoadUnloadAxisName.P軸.ToString()] = EnumAxisMoveStatus.PreStop;

                axisPreTimer[EnumLoadUnloadAxisName.Theta軸.ToString()].Restart();
                axisPreStatus[EnumLoadUnloadAxisName.Theta軸.ToString()] = EnumAxisMoveStatus.PreStop;

                axisPreTimer[EnumLoadUnloadAxisName.Z軸.ToString()].Restart();
                axisPreStatus[EnumLoadUnloadAxisName.Z軸.ToString()] = EnumAxisMoveStatus.PreStop;

                axisPreTimer[EnumLoadUnloadAxisName.Y軸.ToString()].Restart();
                axisPreStatus[EnumLoadUnloadAxisName.Y軸.ToString()] = EnumAxisMoveStatus.PreStop;

                return true;
            }
            else
            {
                WriteLog(5, "", String.Concat("LoadUnload Jog Stop Fail"));
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
                        Stop(EnumLoadUnloadAxisName.Z軸);
                        break;
                    default:
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


        private double 晟淇親自指導所定的電流閥值 = 15;
        private double 擊敗的停止Encoder允許誤差 = 3;
        private double 晟淇要的過電流DelayTime = 300;
        private bool z軸Delaying = false;
        private bool z軸_SlaveDelaying = false;
        private Stopwatch z軸DelayTimer = new Stopwatch();
        private Stopwatch z軸_SlaveDelayTimer = new Stopwatch();
        private Stopwatch TMD從軸DelayTimer = new Stopwatch();
        private double TMD從軸DelayTime = 500;
        private bool TMD從軸Delaying啦 = false;

        private void CheckAxisStatus()
        {
            //    if (resetAlarmThread != null && resetAlarmThread.IsAlive)
            //        ;
            //    else
            //    {
            //        CheckAxisStatusByAxisName(EnumLoadUnloadAxisName.P軸, EnumLoadUnloadControlErrorCode.P軸驅動器異常);
            //        CheckAxisStatusByAxisName(EnumLoadUnloadAxisName.Theta軸, EnumLoadUnloadControlErrorCode.Theta軸驅動器異常);
            //        CheckAxisStatusByAxisName(EnumLoadUnloadAxisName.Y軸, EnumLoadUnloadControlErrorCode.Y軸驅動器異常);
            //        CheckAxisStatusByAxisName(EnumLoadUnloadAxisName.Z軸, EnumLoadUnloadControlErrorCode.Z軸驅動器異常);
            //    }

            //if (GetMoveStatusByCVAxisName(EnumLoadUnloadAxisName.Z軸.ToString()) != EnumAxisMoveStatus.Stop)
            //{
            //    TMD從軸Delaying啦 = false;

            //    if (!localData.LoadUnloadData.拯救Z軸YO)
            //    {
            //        #region Z軸過電流保護?.
            //        if (GetQA(EnumLoadUnloadAxisName.Z軸) > 晟淇親自指導所定的電流閥值)
            //        {
            //            if (z軸Delaying)
            //            {
            //                if (z軸DelayTimer.ElapsedMilliseconds > 晟淇要的過電流DelayTime)
            //                {
            //                    Stop(EnumLoadUnloadAxisName.Z軸);
            //                    SendAlarmCode(EnumLoadUnloadControlErrorCode.Z軸電流過大);
            //                }
            //            }
            //            else
            //            {
            //                z軸DelayTimer.Restart();
            //                z軸Delaying = true;
            //            }
            //        }
            //        else
            //        {
            //            z軸DelayTimer.Stop();
            //            z軸Delaying = false;
            //        }
            //        #endregion

            //        #region Z軸Slave過電流保護?.
            //        if (GetQA(EnumLoadUnloadAxisName.Z軸_Slave) > 晟淇親自指導所定的電流閥值)
            //        {
            //            if (z軸_SlaveDelaying)
            //            {
            //                if (z軸_SlaveDelayTimer.ElapsedMilliseconds > 晟淇要的過電流DelayTime)
            //                {
            //                    Stop(EnumLoadUnloadAxisName.Z軸);
            //                    SendAlarmCode(EnumLoadUnloadControlErrorCode.Z軸_Slave電流過大);
            //                }
            //            }
            //            else
            //            {
            //                z軸_SlaveDelayTimer.Restart();
            //                z軸_SlaveDelaying = true;
            //            }
            //        }
            //        else
            //        {
            //            z軸_SlaveDelayTimer.Stop();
            //            z軸_SlaveDelaying = false;
            //        }
            //        #endregion

            //        if (localData.LoadUnloadData.Z軸爆炸啦)
            //        {
            //            Stop(EnumLoadUnloadAxisName.Z軸);

            //            SendAlarmCode(EnumLoadUnloadControlErrorCode.Z軸主從Encoder落差過大);
            //        }
            //    }
            //}
            //else
            //{
            //    if (TMD從軸Delaying啦)
            //    {
            //        if (TMD從軸DelayTimer.ElapsedMilliseconds > TMD從軸DelayTime)
            //        {
            //            TMD從軸DelayTimer.Stop();

            //            z軸DelayTimer.Stop();
            //            z軸Delaying = false;

            //            z軸_SlaveDelayTimer.Stop();
            //            z軸_SlaveDelaying = false;

            //            if (z軸EncoderHome)
            //            {
            //                if (GetDeltaZ > 擊敗的停止Encoder允許誤差 && !localData.LoadUnloadData.Z軸爆炸啦)
            //                {
            //                    SendAlarmCode(EnumLoadUnloadControlErrorCode.Z軸主從Encoder落差過大);

            //                    //if (mipcControl.SendMIPCDataByIPCTagName(new List<EnumMecanumIPCdefaultTag>() { EnumMecanumIPCdefaultTag.Z軸Encoder歪掉 }, new List<float>() { (float)GetDeltaZValue }))
            //                    //    localData.LoadUnloadData.Z軸爆炸啦 = true; // 塞爆晟淇的斷電保護.
            //                }
            //            }
            //        }
            //    }
            //    else
            //    {
            //        TMD從軸DelayTimer.Restart();
            //        TMD從軸Delaying啦 = true;
            //    }
            //}

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

                    localData.LoadUnloadData.CVEncoderOffsetValue[EnumLoadUnloadAxisName.P軸.ToString()] = localData.LoadUnloadData.P軸EncoderOffset;
                    localData.LoadUnloadData.CVHomeOffsetValue[EnumLoadUnloadAxisName.P軸.ToString()] = axisDataList[EnumLoadUnloadAxisName.P軸.ToString()].HomeOffset;

                    localData.LoadUnloadData.CVEncoderOffsetValue[EnumLoadUnloadAxisName.Theta軸.ToString()] = localData.LoadUnloadData.Theta軸EncoderOffset;
                    localData.LoadUnloadData.CVHomeOffsetValue[EnumLoadUnloadAxisName.Theta軸.ToString()] = axisDataList[EnumLoadUnloadAxisName.Theta軸.ToString()].HomeOffset;

                    localData.LoadUnloadData.CVEncoderOffsetValue[EnumLoadUnloadAxisName.Y軸.ToString()] = localData.LoadUnloadData.Y軸EncoderOffset;
                    localData.LoadUnloadData.CVHomeOffsetValue[EnumLoadUnloadAxisName.Y軸.ToString()] = axisDataList[EnumLoadUnloadAxisName.Y軸.ToString()].HomeOffset;

                    Thread.Sleep(200);
                    z軸EncoderHome = true;
                    p軸EncoderHome = true;
                    theta軸EncoderHome = true;
                    y軸EncoderHome = true;
                }
            }
        }

        public override void UpdateLoadingAndCSTID()
        {
            if (AllAxisSensorData[EnumLoadUnloadAxisName.Y軸.ToString()][CST座_在席檢知].Data ||
                AllAxisSensorData[EnumLoadUnloadAxisName.Y軸.ToString()][FORK_在席檢知].Data)
            {
                localData.LoadUnloadData.Loading = true;
            }
            else
            {
                localData.LoadUnloadData.Loading = false;
            }

            if (localData.LoadUnloadData.LoadUnloadCommand != null)
            {
                if (localData.AutoManual == EnumAutoState.Auto)
                    localData.LoadUnloadData.Loading_LogicFlag = localData.LoadUnloadData.Loading;
            }

            if (cstIDReader != null)
            {
                string message = "";
                string errorMessage = "";

                if (cstIDReader.ReadBarcode(ref message, 300, ref errorMessage))
                {
                    localData.LoadUnloadData.CstID = message;
                    WriteLog(7, "", String.Concat("CSTID Read Scuess : ", localData.LoadUnloadData.CstID.ToLower()));
                }
                else
                {
                    cstIDReader.StopReadBarcode(ref message, ref errorMessage);
                    localData.LoadUnloadData.CstID = "";
                    WriteLog(7, "", "CSTID Read Fail");
                }
            }
            else
            {
                localData.LoadUnloadData.CstID = "08SA0001";
            }
        }

        public override void UpdateForkHomeStatus()
        {
            CheckCVEncoderHome();
            CheckAxisStatus();
            try
            {
                for (int i = 0; i < AxisList.Count; i++)
                {
                    for (int j = 0; j < AxisSensorList[AxisList[i]].Count; j++)
                    {
                        if (AxisSensorList[AxisList[i]][j].ToString() == FORK_在席檢知 ||
                            AxisSensorList[AxisList[i]][j].ToString() == 二重格檢知_L ||
                            AxisSensorList[AxisList[i]][j].ToString() == 二重格檢知_R)
                        {
                            AxisSensorDataList[AxisList[i]][j].Data = localData.MIPCData.GetDataByMIPCTagName(AxisSensorList[AxisList[i]][j]) == 1;
                        }
                        else
                        {
                            AxisSensorDataList[AxisList[i]][j].Data = localData.MIPCData.GetDataByMIPCTagName(AxisSensorList[AxisList[i]][j]) == 0;
                        }
                        if (AxisSensorDataList[AxisList[i]][j].Change)
                        {
                            WriteLog(7, "", String.Concat(AxisSensorList[AxisList[i]][j], " Change to ",
                                                          (AxisSensorDataList[AxisList[i]][j].data ? "on" : "off")));
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            if (AllAxisSensorData[EnumLoadUnloadAxisName.Y軸.ToString()][CST座_在席檢知].Data ||
                AllAxisSensorData[EnumLoadUnloadAxisName.Y軸.ToString()][FORK_在席檢知].Data)
            {
                localData.LoadUnloadData.Loading = true;
            }
            else
            {
                localData.LoadUnloadData.Loading = false;
            }


            if (p軸EncoderHome && z軸EncoderHome && theta軸EncoderHome && y軸EncoderHome &&
                CheckAixsIsServoOn(EnumLoadUnloadAxisName.P軸.ToString()) && CheckAixsIsServoOn(EnumLoadUnloadAxisName.Theta軸.ToString()) &&
                CheckAixsIsServoOn(EnumLoadUnloadAxisName.Z軸.ToString()) && CheckAixsIsServoOn(EnumLoadUnloadAxisName.Y軸.ToString()))
            {
                string axisNameString = EnumLoadUnloadAxisName.Z軸.ToString();

                z軸Home = Math.Abs(localData.LoadUnloadData.CVFeedbackData[axisNameString].Position) / (float)(axisDataList[axisNameString].PlausUnit) <= inPositionRange[axisNameString];

                axisNameString = EnumLoadUnloadAxisName.P軸.ToString();
                p軸Home = Math.Abs(localData.LoadUnloadData.CVFeedbackData[axisNameString].Position) / (float)(axisDataList[axisNameString].PlausUnit) <= inPositionRange[axisNameString] && AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data;

                axisNameString = EnumLoadUnloadAxisName.Theta軸.ToString();
                theta軸Home = Math.Abs(localData.LoadUnloadData.CVFeedbackData[axisNameString].Position) / (float)(axisDataList[axisNameString].PlausUnit) <= inPositionRange[axisNameString] && AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data;

                axisNameString = EnumLoadUnloadAxisName.Y軸.ToString();
                y軸Home = Math.Abs(localData.LoadUnloadData.CVFeedbackData[axisNameString].Position) / (float)(axisDataList[axisNameString].PlausUnit) <= inPositionRange[axisNameString];


                localData.LoadUnloadData.ForkHome = z軸Home && p軸Home && theta軸Home && y軸Home;
            }
            else
                localData.LoadUnloadData.ForkHome = false;

            //if (localData.LoadUnloadData.LoadUnloadCommand == null && !localData.LoadUnloadData.ForkHome)
            //    SendAlarmCode(EnumLoadUnloadControlErrorCode.Fork不在Home點);

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
                    AxisCanJog[i] = true;
                }
            }

            if (localData.AutoManual == EnumAutoState.Auto)
            {
                if (localData.LoadUnloadData.LoadUnloadCommand == null)
                {
                    if (localData.LoadUnloadData.Loading_LogicFlag != localData.LoadUnloadData.Loading)
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.Loading邏輯和Sensor不相符);
                    else
                        ResetAlarmCode(EnumLoadUnloadControlErrorCode.Loading邏輯和Sensor不相符);
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
        #region 回Home流程.
        public override void Home()
        {
            if (localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null &&
                localData.LoadUnloadData.LoadUnloadCommand == null && !localData.LoadUnloadData.Homing)
            {
                localData.LoadUnloadData.Homing = true;
                localData.LoadUnloadData.HomeStop = false;
                homeThread = new Thread(HomeThread);
                homeThread.Start();
            }
        }

        public override void Home_Initial()
        {
            if (localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null &&
                localData.LoadUnloadData.LoadUnloadCommand == null && !localData.LoadUnloadData.Homing)
            {
                mipcControl.SendMIPCDataByIPCTagName(
                    new List<EnumMecanumIPCdefaultTag>() { EnumMecanumIPCdefaultTag.Encoder已回Home },
                    new List<float>() { 0 });

                z軸EncoderHome = false;
                p軸EncoderHome = false;
                theta軸EncoderHome = false;
                localData.LoadUnloadData.Homing = true;
                localData.LoadUnloadData.HomeStop = false;
                homeThread = new Thread(HomeThread);
                homeThread.Start();
            }
        }

        private double homeTimeout = 10000000;

        #region bool JogToLimit(string axisNameString)
        private bool JogToPosLimit(string axisNameString)
        {
            if (AxisJog(axisNameString, true, axisDataList[axisNameString].HomeVelocity_High))
            {
                Stopwatch timer = new Stopwatch();
                timer.Restart();

                while (!AllAxisSensorData[axisNameString][axisPosLimitSensor[axisNameString]].Data)
                {
                    if (timer.ElapsedMilliseconds > homeTimeout)
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                        localData.LoadUnloadData.Homing = false;
                        return false;
                    }
                    else if (localData.LoadUnloadData.HomeStop)
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                        localData.LoadUnloadData.Homing = false;
                        return false;
                    }

                    Thread.Sleep(sleepTime);
                }

                if (!JogStop())
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                    localData.LoadUnloadData.Homing = false;
                    return false;
                }
                return true;
            }
            else
            {
                JogStop();
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                localData.LoadUnloadData.Homing = false;
                return false;
            }
        }

        private bool JogToNagLimit(string axisNameString)
        {
            if (AxisJog(axisNameString, false, axisDataList[axisNameString].HomeVelocity_High))
            {
                Stopwatch timer = new Stopwatch();
                timer.Restart();

                while (!AllAxisSensorData[axisNameString][axisNagLimitSensor[axisNameString]].Data)
                {
                    if (timer.ElapsedMilliseconds > homeTimeout)
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                        localData.LoadUnloadData.Homing = false;
                        return false;
                    }
                    else if (localData.LoadUnloadData.HomeStop)
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                        localData.LoadUnloadData.Homing = false;
                        return false;
                    }

                    Thread.Sleep(sleepTime);
                }

                JogStop();

                return true;
            }
            else
            {
                JogStop();
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                localData.LoadUnloadData.Homing = false;
                return false;
            }
        }
        #endregion

        private bool SetFindHome(string axisNameString, bool onOff)
        {
            return mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.Home] }, new List<float>() { (onOff ? 1 : 0) });
        }

        private bool SetFindHomeMethod(string axisNameString, int homemethod)
        {
            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() {
                    axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.Velocity],
                    axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.Acceleration],
                    axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.Deceleration],
                    axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.PlausUnit],
                    axisCommandString[axisNameString][EnumLoadUnloadAxisCommandType.Home]},
            new List<float>() {
                      (float)(axisDataList[axisNameString].AutoVelocity) *(float)(axisDataList[axisNameString].HomeVelocity),
                      (float)(axisDataList[axisNameString].AutoAcceleration),
                      (float)(axisDataList[axisNameString].AutoDeceleration),
                      (float)(axisDataList[axisNameString].PlausUnit),
                      homemethod });


            return true;
        }

        #region bool GoHome(string axisNameString)
        private bool GoHome(string axisNameString)
        {
            AxisFeedbackData temp = localData.LoadUnloadData.CVFeedbackData[axisNameString];

            //if (Math.Abs(temp.Position) < inPositionRange[axisNameString])
            {
                if (logMode)
                    WriteLog(7, "", "下p to p命令");

                if (!CVPtoP(axisNameString, 0, 0.5, false, 0))
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
                    else if (localData.LoadUnloadData.HomeStop)
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                        localData.LoadUnloadData.Homing = false;
                        return false;
                    }

                    Thread.Sleep(sleepTime);
                }

                if (logMode)
                    WriteLog(7, "", String.Concat("Axis : ", axisNameString, "等待結束"));
            }

            if (AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data)
            {
                return true;
            }
            else
            {
                if (axisNameString == EnumLoadUnloadAxisName.Z軸.ToString() ||
                    axisNameString == EnumLoadUnloadAxisName.Y軸.ToString())
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
        }
        #endregion

        #region bool FindHome(string axisNameString)
        private bool FindHome(string axisNameString)
        {
            Stopwatch timer = new Stopwatch();

            if (AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data)
            {
                #region Step2-往正方向慢速移動3秒.
                if (logMode)
                    WriteLog(7, "", "目前在HomeSensor, 快速往外拉後在慢速找一次");

                if (!AxisJog(axisNameString, false, axisDataList[axisNameString].HomeVelocity))
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                timer.Restart();

                #region 等待HomeSensorOff.
                if (logMode)
                    WriteLog(7, "", "等待HomeSensor off");

                while (AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data)
                {
                    if (timer.ElapsedMilliseconds > homeTimeout)
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }
                    else if (localData.LoadUnloadData.HomeStop)
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }

                    Thread.Sleep(sleepTime);
                }
                #endregion

                if (!JogStop())
                {
                    JogStop();
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
                if (logMode)
                    WriteLog(7, "", String.Concat(axisNameString, " 回Home, 先快速找HomeSensor"));

                //if (!SetFindHome(axisNameString, true) || !AxisJog(axisNameString, false, axisDataList[axisNameString].HomeVelocity_High))
                //{
                //    JogStop();
                //    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                //    localData.LoadUnloadData.Homing = false;
                //    SetFindHome(axisNameString, false);
                //    return false;
                //}
                if (!AxisJog(axisNameString, false, axisDataList[axisNameString].HomeVelocity_High))
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                if (logMode)
                    WriteLog(7, "", "等待Stop(HomeSensor on )");

                timer.Restart();

                while (!AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data)
                {
                    if (timer.ElapsedMilliseconds > homeTimeout)
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }
                    else if (localData.LoadUnloadData.HomeStop)
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }

                    Thread.Sleep(sleepTime);
                }

                //while (localData.LoadUnloadData.CVFeedbackData[axisNameString].HM1 != 0)
                //{
                //    if (localData.LoadUnloadData.HomeStop)
                //    {
                //        JogStop();
                //        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                //        localData.LoadUnloadData.Homing = false;
                //        SetFindHome(axisNameString, false);
                //        return false;
                //    }

                //    Thread.Sleep(sleepTime);
                //}

                if (!JogStop())
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }
                #endregion

                //#region Step2-往正方向慢速移動3秒.
                //if (logMode)
                //    WriteLog(7, "", "找到HomeSensor, 慢速往外拉3秒後在慢速找一次");

                //if (!AxisJog(axisNameString, true, axisDataList[axisNameString].HomeVelocity))
                //{
                //    JogStop();
                //    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                //    localData.LoadUnloadData.Homing = false;
                //    SetFindHome(axisNameString, false);
                //    return false;
                //}

                //timer.Restart();

                //#region 等待HomeSensorOff.
                //if (logMode)
                //    WriteLog(7, "", "等待HomeSensor off");

                //while (AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data)
                //{
                //    if (timer.ElapsedMilliseconds > homeTimeout)
                //    {
                //        JogStop();
                //        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                //        localData.LoadUnloadData.Homing = false;
                //        SetFindHome(axisNameString, false);
                //        return false;
                //    }
                //    else if (localData.LoadUnloadData.HomeStop)
                //    {
                //        JogStop();
                //        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                //        localData.LoadUnloadData.Homing = false;
                //        SetFindHome(axisNameString, false);
                //        return false;
                //    }

                //    Thread.Sleep(sleepTime);
                //}
                //#endregion

                //Thread.Sleep(100);
                ////timer.Restart();
                ////while (timer.ElapsedMilliseconds < 500)
                ////{
                ////    if (localData.LoadUnloadData.HomeStop)
                ////    {
                ////        JogStop();
                ////        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                ////        localData.LoadUnloadData.Homing = false;
                ////        SetFindHome(axisNameString, false);
                ////        return false;
                ////    }

                ////    Thread.Sleep(sleepTime);
                ////}

                //if (!JogStop())
                //{
                //    JogStop();
                //    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                //    localData.LoadUnloadData.Homing = false;
                //    SetFindHome(axisNameString, false);
                //    return false;
                //}
                //#endregion
            }

            Thread.Sleep(500);
            #region Step6-SetHome.
            WriteLog(7, "", axisNameString + "SetPosition為0");
            if (!SetFindHomeMethod(axisNameString, 19))
            {
                JogStop();
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                localData.LoadUnloadData.Homing = false;
                SetFindHome(axisNameString, false);
                return false;
            }

            Thread.Sleep(500);
            timer.Restart();

            while (localData.LoadUnloadData.CVFeedbackData[axisNameString].AxisMoveStaus != EnumAxisMoveStatus.Stop)
            {
                if (timer.ElapsedMilliseconds > homeTimeout)
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }
                else if (localData.LoadUnloadData.HomeStop)
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(sleepTime);
            }

            if (localData.LoadUnloadData.CVFeedbackData[axisNameString].Position / (float)(axisDataList[axisNameString].PlausUnit) < 100)
            {
                int tempvalueforhomeing = 0;
                while (!(AllAxisSensorData[axisNameString][axisHomeSensor[axisNameString]].Data))
                {
                    Thread.Sleep(500);
                    tempvalueforhomeing += 1;
                    if (!CVPtoP(axisNameString, tempvalueforhomeing, axisDataList[axisNameString].HomeVelocity, false, 0))
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }

                    Thread.Sleep(500);
                    timer.Restart();

                    while (localData.LoadUnloadData.CVFeedbackData[axisNameString].AxisMoveStaus != EnumAxisMoveStatus.Stop)
                    {
                        if (timer.ElapsedMilliseconds > homeTimeout)
                        {
                            JogStop();
                            SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                            localData.LoadUnloadData.Homing = false;
                            SetFindHome(axisNameString, false);
                            return false;
                        }
                        else if (localData.LoadUnloadData.HomeStop)
                        {
                            JogStop();
                            SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                            localData.LoadUnloadData.Homing = false;
                            SetFindHome(axisNameString, false);
                            return false;
                        }

                        Thread.Sleep(sleepTime);
                    }
                }

                Thread.Sleep(500);

                if (!CVPtoP(axisNameString, 1.5, axisDataList[axisNameString].HomeVelocity, false, 0))
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(500);
                timer.Restart();

                while (localData.LoadUnloadData.CVFeedbackData[axisNameString].AxisMoveStaus != EnumAxisMoveStatus.Stop)
                {
                    if (timer.ElapsedMilliseconds > homeTimeout)
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }
                    else if (localData.LoadUnloadData.HomeStop)
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }

                    Thread.Sleep(sleepTime);
                }


                Thread.Sleep(500);

                if (!SetFindHomeMethod(axisNameString, 35))
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(500);
                timer.Restart();

                while (localData.LoadUnloadData.CVFeedbackData[axisNameString].AxisMoveStaus != EnumAxisMoveStatus.Stop)
                {
                    if (timer.ElapsedMilliseconds > homeTimeout)
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }
                    else if (localData.LoadUnloadData.HomeStop)
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }

                    Thread.Sleep(sleepTime);
                }

                if (localData.LoadUnloadData.CVFeedbackData[axisNameString].Position / (float)(axisDataList[axisNameString].PlausUnit) < 100)
                {
                    return true;
                }
                else
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_HomeSensor未on);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }
            }
            else
            {
                JogStop();
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                localData.LoadUnloadData.Homing = false;
                SetFindHome(axisNameString, false);
                return false;
            }
            #endregion
        }

        private bool FindHome_NagLimit(string axisNameString)
        {
            Stopwatch timer = new Stopwatch();

            if (AllAxisSensorData[axisNameString][axisNagLimitSensor[axisNameString]].Data)
            {
                if (logMode)
                    WriteLog(7, "", axisNameString + "慢速找負極限往正的交界點");

                //while (true)
                //{
                //    Thread.Sleep(500);
                //}
                if (!AxisJog(axisNameString, true, axisDataList[axisNameString].HomeVelocity))
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(500);
                timer.Restart();

                while (AllAxisSensorData[axisNameString][axisNagLimitSensor[axisNameString]].Data)
                {
                    if (timer.ElapsedMilliseconds > homeTimeout)
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }
                    else if (localData.LoadUnloadData.HomeStop)
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }

                    Thread.Sleep(sleepTime);
                }

                Thread.Sleep(500);
                if (!JogStop())
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                WriteLog(7, "", axisNameString + "SetPosition為0");
                if (!SetFindHomeMethod(axisNameString, 17))
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(500);
                timer.Restart();

                while (localData.LoadUnloadData.CVFeedbackData[axisNameString].AxisMoveStaus != EnumAxisMoveStatus.Stop)
                {
                    if (timer.ElapsedMilliseconds > homeTimeout)
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }
                    else if (localData.LoadUnloadData.HomeStop)
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }

                    Thread.Sleep(sleepTime);
                }

                if (localData.LoadUnloadData.CVFeedbackData[axisNameString].Position / (float)(axisDataList[axisNameString].PlausUnit) < 100)
                {
                    int tempvalueforhomeing = 0;
                    while (!(AllAxisSensorData[axisNameString][axisNagLimitSensor[axisNameString]].Data))
                    {
                        Thread.Sleep(500);
                        tempvalueforhomeing -= 1;
                        if (!CVPtoP(axisNameString, tempvalueforhomeing, axisDataList[axisNameString].HomeVelocity, false, 0))
                        {
                            JogStop();
                            SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                            localData.LoadUnloadData.Homing = false;
                            SetFindHome(axisNameString, false);
                            return false;
                        }

                        Thread.Sleep(500);
                        timer.Restart();

                        while (localData.LoadUnloadData.CVFeedbackData[axisNameString].AxisMoveStaus != EnumAxisMoveStatus.Stop)
                        {
                            if (timer.ElapsedMilliseconds > homeTimeout)
                            {
                                JogStop();
                                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                                localData.LoadUnloadData.Homing = false;
                                SetFindHome(axisNameString, false);
                                return false;
                            }
                            else if (localData.LoadUnloadData.HomeStop)
                            {
                                JogStop();
                                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                                localData.LoadUnloadData.Homing = false;
                                SetFindHome(axisNameString, false);
                                return false;
                            }
                            else if ((AllAxisSensorData[axisNameString][axisNagLimitSensor[axisNameString]].Data))
                            {
                                JogStop();
                            }
                            Thread.Sleep(sleepTime);
                        }
                    }

                    Thread.Sleep(500);

                    if (!SetFindHomeMethod(axisNameString, 35))
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }

                    Thread.Sleep(500);
                    timer.Restart();

                    while (localData.LoadUnloadData.CVFeedbackData[axisNameString].AxisMoveStaus != EnumAxisMoveStatus.Stop)
                    {
                        if (timer.ElapsedMilliseconds > homeTimeout)
                        {
                            JogStop();
                            SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                            localData.LoadUnloadData.Homing = false;
                            SetFindHome(axisNameString, false);
                            return false;
                        }
                        else if (localData.LoadUnloadData.HomeStop)
                        {
                            JogStop();
                            SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                            localData.LoadUnloadData.Homing = false;
                            SetFindHome(axisNameString, false);
                            return false;
                        }

                        Thread.Sleep(sleepTime);
                    }

                    if (localData.LoadUnloadData.CVFeedbackData[axisNameString].Position / (float)(axisDataList[axisNameString].PlausUnit) < 100)
                    {
                        return true;
                    }
                    else
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_HomeSensor未on);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }
                }
                else
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }
            }
            else
            {
                JogStop();
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_HomeSensor未on);
                localData.LoadUnloadData.Homing = false;
                SetFindHome(axisNameString, false);
                return false;
            }
        }


        private bool FindHome_PosLimit(string axisNameString)
        {
            Stopwatch timer = new Stopwatch();

            if (AllAxisSensorData[axisNameString][axisNagLimitSensor[axisNameString]].Data)
            {
                if (logMode)
                    WriteLog(7, "", axisNameString + "慢速找負極限往正的交界點");

                //while (true)
                //{
                //    Thread.Sleep(500);
                //}
                if (!AxisJog(axisNameString, false, axisDataList[axisNameString].HomeVelocity))
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(500);
                timer.Restart();

                while (AllAxisSensorData[axisNameString][axisPosLimitSensor[axisNameString]].Data)
                {
                    if (timer.ElapsedMilliseconds > homeTimeout)
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }
                    else if (localData.LoadUnloadData.HomeStop)
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }

                    Thread.Sleep(sleepTime);
                }

                Thread.Sleep(500);
                if (!JogStop())
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                WriteLog(7, "", axisNameString + "SetPosition為0");
                if (!SetFindHomeMethod(axisNameString, 18))
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(500);
                timer.Restart();

                while (localData.LoadUnloadData.CVFeedbackData[axisNameString].AxisMoveStaus != EnumAxisMoveStatus.Stop)
                {
                    if (timer.ElapsedMilliseconds > homeTimeout)
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }
                    else if (localData.LoadUnloadData.HomeStop)
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }

                    Thread.Sleep(sleepTime);
                }

                if (localData.LoadUnloadData.CVFeedbackData[axisNameString].Position / (float)(axisDataList[axisNameString].PlausUnit) < 100)
                {
                    return true;
                }
                else
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }
            }
            else
            {
                JogStop();
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_HomeSensor未on);
                localData.LoadUnloadData.Homing = false;
                SetFindHome(axisNameString, false);
                return false;
            }
        }

        private bool FindHome_PosLimit_Simple(string axisNameString)
        {
            Stopwatch timer = new Stopwatch();

            WriteLog(7, "", axisNameString + "SetPosition為0");
            if (!SetFindHomeMethod(axisNameString, 18))
            {
                JogStop();
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                localData.LoadUnloadData.Homing = false;
                SetFindHome(axisNameString, false);
                return false;
            }

            Thread.Sleep(5000);
            timer.Restart();

            while (localData.LoadUnloadData.CVFeedbackData[axisNameString].AxisMoveStaus != EnumAxisMoveStatus.Stop)
            {
                if (timer.ElapsedMilliseconds > homeTimeout)
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }
                else if (localData.LoadUnloadData.HomeStop)
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(sleepTime);
            }

            if (localData.LoadUnloadData.CVFeedbackData[axisNameString].Position / (float)(axisDataList[axisNameString].PlausUnit) < 100)
            {

                Thread.Sleep(2000);
                WriteLog(7, "", axisNameString + "SetPosition為0");
                if (!CVPtoP(axisNameString, -75, 0.5, false, 0))
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(2000);
                timer.Restart();

                while (localData.LoadUnloadData.CVFeedbackData[axisNameString].AxisMoveStaus != EnumAxisMoveStatus.Stop)
                {
                    if (timer.ElapsedMilliseconds > homeTimeout)
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }
                    else if (localData.LoadUnloadData.HomeStop)
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止);
                        localData.LoadUnloadData.Homing = false;
                        SetFindHome(axisNameString, false);
                        return false;
                    }

                    Thread.Sleep(sleepTime);
                }

                if (!SetFindHomeMethod(axisNameString, 35))
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }

                Thread.Sleep(2000);

                if (localData.LoadUnloadData.CVFeedbackData[axisNameString].Position / (float)(axisDataList[axisNameString].PlausUnit) < 100)
                {
                    return true;
                }
                else
                {
                    JogStop();
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                    localData.LoadUnloadData.Homing = false;
                    SetFindHome(axisNameString, false);
                    return false;
                }
            }
            else
            {
                JogStop();
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗);
                localData.LoadUnloadData.Homing = false;
                SetFindHome(axisNameString, false);
                return false;
            }
        }
        #endregion

        public Thread homeThread = null;

        private bool z軸EncoderHome = false;
        private bool p軸EncoderHome = false;
        private bool theta軸EncoderHome = false;
        private bool y軸EncoderHome = false;

        private bool z軸Home = false;                 //inPosition 
        private bool p軸Home = false;                 //inPosition 
        private bool theta軸Home = false;             //inPosition 
        private bool y軸Home = false;             //inPosition 

        #region 回HomeThread.
        private void HomeThread()
        {
            try
            {
                //SetFindHome(EnumLoadUnloadAxisName.Z軸.ToString(), false);
                //SetFindHome(EnumLoadUnloadAxisName.P軸.ToString(), false);
                //SetFindHome(EnumLoadUnloadAxisName.Theta軸.ToString(), false);
                Stopwatch timer = new Stopwatch();

                JogStop();

                #region CST卡在一半.

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
                if (logMode)
                    WriteLog(7, "", "CV Servo on all");

                if (!ServoOn(EnumLoadUnloadAxisName.Y軸.ToString(), true, 2000) || !ServoOn(EnumLoadUnloadAxisName.Z軸.ToString(), true, 2000) ||
                    !ServoOn(EnumLoadUnloadAxisName.Theta軸.ToString(), true, 2000) || !ServoOn(EnumLoadUnloadAxisName.P軸.ToString(), true, 2000))
                {
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_ServoOn_Timeout);
                    localData.LoadUnloadData.Homing = false;
                    return;
                }
                #endregion

                if (logMode)
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

                if (z軸EncoderHome && p軸EncoderHome && theta軸EncoderHome && y軸EncoderHome)
                {
                    if (logMode)
                        WriteLog(7, "", "因三軸Encoder回Home已做過, 因此直接回Home");

                    if (!GoHome(EnumLoadUnloadAxisName.P軸.ToString()) ||
                        !GoHome(EnumLoadUnloadAxisName.Theta軸.ToString()) ||
                        !GoHome(EnumLoadUnloadAxisName.Z軸.ToString()) ||
                        !GoHome(EnumLoadUnloadAxisName.Y軸.ToString()))
                        return;
                }
                else
                {
                    if (AllAxisSensorData[EnumLoadUnloadAxisName.Y軸.ToString()][axisNagLimitSensor[EnumLoadUnloadAxisName.Y軸.ToString()]].Data &&
                        AllAxisSensorData[EnumLoadUnloadAxisName.P軸.ToString()][axisHomeSensor[EnumLoadUnloadAxisName.P軸.ToString()]].Data &&
                        AllAxisSensorData[EnumLoadUnloadAxisName.Theta軸.ToString()][axisHomeSensor[EnumLoadUnloadAxisName.Theta軸.ToString()]].Data)
                    {
                        if (logMode)
                            WriteLog(7, "", "Y軸在 縮回極限, 可以回Home");
                    }
                    else
                    {
                        JogStop();
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_Y軸不在極限);
                        localData.LoadUnloadData.Homing = false;
                        return;
                    }

                    //if (AllAxisSensorData[EnumLoadUnloadAxisName.Y軸.ToString()][CST座_FOUP_在席檢知].data)
                    //{
                    //if (AllAxisSensorData[EnumLoadUnloadAxisName.Y軸.ToString()][axisPosLimitSensor[EnumLoadUnloadAxisName.Y軸.ToString()]].Data)
                    //{
                    //    if (logMode)
                    //        WriteLog(7, "", "碰Z軸上極限");

                    //    if (!JogToPosLimit(EnumLoadUnloadAxisName.Z軸.ToString()))
                    //        return;

                    //    if (logMode)
                    //        WriteLog(7, "", "碰Z軸上極限結束");

                    //    //////////////////////////////////////////////////////////////////////////////////////
                    //    Thread.Sleep(1000);
                    //    if (logMode)
                    //        WriteLog(7, "", "碰Y軸縮回極限");

                    //    if (!JogToNagLimit(EnumLoadUnloadAxisName.Y軸.ToString()))
                    //        return;

                    //    if (logMode)
                    //        WriteLog(7, "", "碰Y軸縮回極限結束");
                    //    //////////////////////////////////////////////////////////////////////////////////////
                    //    Thread.Sleep(1000);
                    //    if (logMode)
                    //        WriteLog(7, "", "找Y軸原點_縮回極限");

                    //    if (!FindHome_NagLimit(EnumLoadUnloadAxisName.Y軸.ToString()))
                    //        return;

                    //    if (logMode)
                    //        WriteLog(7, "", "找Y軸原點_縮回極限結束");
                    //    //////////////////////////////////////////////////////////////////////////////////////
                    //    Thread.Sleep(1000);
                    //    if (logMode)
                    //        WriteLog(7, "", "找Z軸原點_下極限");

                    //    if (!FindHome_PosLimit_Simple(EnumLoadUnloadAxisName.Z軸.ToString()))
                    //        return;

                    //    if (logMode)
                    //        WriteLog(7, "", "找Z軸原點_下極限結束");
                    //    //////////////////////////////////////////////////////////////////////////////////////
                    //    Thread.Sleep(1000);
                    //}
                    //else
                    //{
                    if (logMode)
                        WriteLog(7, "", "碰Z軸下極限");

                    if (!JogToPosLimit(EnumLoadUnloadAxisName.Z軸.ToString()))
                        return;

                    if (logMode)
                        WriteLog(7, "", "碰Z軸下極限結束");
                    //////////////////////////////////////////////////////////////////////////////////////
                    Thread.Sleep(2000);
                    if (logMode)
                        WriteLog(7, "", "找Z軸原點_下極限");

                    if (!FindHome_PosLimit_Simple(EnumLoadUnloadAxisName.Z軸.ToString()))
                        return;

                    if (logMode)
                        WriteLog(7, "", "找Z軸原點_下極限結束");
                    //////////////////////////////////////////////////////////////////////////////////////
                    Thread.Sleep(2000);
                    if (logMode)
                        WriteLog(7, "", "碰Y軸縮回極限");

                    if (!JogToNagLimit(EnumLoadUnloadAxisName.Y軸.ToString()))
                        return;

                    if (logMode)
                        WriteLog(7, "", "碰Y軸縮回極限結束");
                    //////////////////////////////////////////////////////////////////////////////////////
                    Thread.Sleep(2000);
                    if (logMode)
                        WriteLog(7, "", "找Y軸原點_縮回極限");

                    if (!FindHome_NagLimit(EnumLoadUnloadAxisName.Y軸.ToString()))
                        return;

                    if (logMode)
                        WriteLog(7, "", "找Y軸原點_縮回極限結束");
                    //////////////////////////////////////////////////////////////////////////////////////
                    Thread.Sleep(2000);
                    //}
                    //}

                    if (!AllAxisSensorData[EnumLoadUnloadAxisName.P軸.ToString()][axisHomeSensor[EnumLoadUnloadAxisName.P軸.ToString()]].Data)
                    {
                        if (logMode)
                            WriteLog(7, "", "碰P軸正極限");

                        if (!JogToPosLimit(EnumLoadUnloadAxisName.P軸.ToString()))
                            return;

                        if (logMode)
                            WriteLog(7, "", "碰P軸正極限結束");
                        Thread.Sleep(2000);
                    }

                    if (logMode)
                        WriteLog(7, "", "找P軸原點");

                    if (!FindHome(EnumLoadUnloadAxisName.P軸.ToString()))
                        return;

                    Thread.Sleep(1000);
                    if (!AllAxisSensorData[EnumLoadUnloadAxisName.Theta軸.ToString()][axisHomeSensor[EnumLoadUnloadAxisName.Theta軸.ToString()]].Data)
                    {
                        if (logMode)
                            WriteLog(7, "", "碰Theta軸正極限");

                        if (!JogToPosLimit(EnumLoadUnloadAxisName.Theta軸.ToString()))

                            return;
                        if (logMode)
                            WriteLog(7, "", "碰Theta軸正極限結束");
                        Thread.Sleep(2000);
                    }

                    if (logMode)
                        WriteLog(7, "", "找Theta軸原點");

                    if (!FindHome(EnumLoadUnloadAxisName.Theta軸.ToString()))
                        return;

                    Thread.Sleep(500);
                    p軸EncoderHome = true;
                    theta軸EncoderHome = true;
                    z軸EncoderHome = true;
                    y軸EncoderHome = true;
                    mipcControl.SendMIPCDataByIPCTagName(
                        new List<EnumMecanumIPCdefaultTag>() {
                            EnumMecanumIPCdefaultTag.Z軸EncoderOffset, EnumMecanumIPCdefaultTag.Y軸EncoderOffset,
                            EnumMecanumIPCdefaultTag.P軸EncoderOffset, EnumMecanumIPCdefaultTag.Theta軸EncoderOffset,
                            EnumMecanumIPCdefaultTag.Encoder已回Home},
                        new List<float>()
                        {
                            (float)localData.LoadUnloadData.CVEncoderOffsetValue[EnumLoadUnloadAxisName.Z軸.ToString()],
                            (float)localData.LoadUnloadData.CVEncoderOffsetValue[EnumLoadUnloadAxisName.Y軸.ToString()],
                            (float)localData.LoadUnloadData.CVEncoderOffsetValue[EnumLoadUnloadAxisName.P軸.ToString()],
                            (float)localData.LoadUnloadData.CVEncoderOffsetValue[EnumLoadUnloadAxisName.Theta軸.ToString()],
                            1
                        });
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
                //homeThread = new Thread(FindHomeSensorOffsetByEncoderInHomeThread);
                //homeThread.Start();
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

            #region Step3-往正方向移動,直到到達HomeSensor外.
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
                    realP = realP * Math.Cos(alignmentValue.Theta * Math.PI / 180);

                    double realTheta = -(localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].Position - RightStageDataList[localData.TheMapInfo.AllAddress[addressID].StageNumber].Encoder_Theta);
                    double realZ = (RightStageDataList[localData.TheMapInfo.AllAddress[addressID].StageNumber].Encoder_Z -
                                    RightStageDataList[localData.TheMapInfo.AllAddress[addressID].StageNumber].Benchmark_Z) -
                                    localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()].Position;

                    // alignP - 基準 = 真實P
                    // encoderAbs_Theta = RightStageDataList[command.StageNumber].Encoder_Theta - command.CommandStartAlignmentValue.Theta;

                    //WriteLog(7, "", String.Concat("localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].Position = ", localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Theta軸.ToString()].Position.ToString("0.000"),
                    //                              ", RightStageDataList[localData.TheMapInfo.AllAddress[addressID].StageNumber].Encoder_Theta = ", RightStageDataList[localData.TheMapInfo.AllAddress[addressID].StageNumber].Encoder_Theta.ToString("0.000")));
                    //WriteLog(7, "", String.Concat("alignmentValue.Theta = ", alignmentValue.Theta.ToString("0.000"), ", realTheta = ", realTheta.ToString("0.000")));
                    double 站點Offset基準P = alignmentValue.P - realP;
                    double 站點Offset基準Theta = alignmentValue.Theta - realTheta;
                    double 站點Offset基準Z = -realZ;

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

        #endregion
        #region CSV.
        public override void WriteCSV()
        {
            if (initialEnd)
            {
                string csvLog = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff");

                LoadUnloadCommandData temp = localData.LoadUnloadData.LoadUnloadCommand;

                csvLog = String.Concat(csvLog, ",", temp == null ? "" : temp.StepString);

                AxisFeedbackData axisData = null;

                string axisNamString = "";

                for (int i = 0; i < FeedbackAxisList.Count; i++)
                {
                    axisNamString = FeedbackAxisList[i];
                    axisData = localData.LoadUnloadData.CVFeedbackData[axisNamString];

                    if (axisData != null)
                    {
                        csvLog = String.Concat(csvLog, ",", axisData.Position.ToString("0.000"));
                        csvLog = String.Concat(csvLog, ",", axisData.Velocity.ToString("0.0"));
                        csvLog = String.Concat(csvLog, ",", axisData.AxisMoveStaus.ToString());

                        if (axisNamString == EnumLoadUnloadAxisName.Z軸_Slave.ToString())
                            csvLog = String.Concat(csvLog, ",", GetMoveStatusByCVAxisName(EnumLoadUnloadAxisName.Z軸.ToString()).ToString());
                        else
                            csvLog = String.Concat(csvLog, ",", GetMoveStatusByCVAxisName(axisNamString).ToString());

                        csvLog = String.Concat(csvLog, ",", axisData.AxisServoOnOff.ToString());
                        csvLog = String.Concat(csvLog, ",", axisData.V.ToString("0.000"));
                        csvLog = String.Concat(csvLog, ",", axisData.DA.ToString("0.000"));
                        csvLog = String.Concat(csvLog, ",", axisData.QA.ToString("0.000"));
                        csvLog = String.Concat(csvLog, ",", axisData.EC.ToString("0.000"));
                        csvLog = String.Concat(csvLog, ",", axisData.MF.ToString("0.000"));
                        csvLog = String.Concat(csvLog, ",", axisData.Temperature.ToString("0.000"));
                    }
                    else
                    {
                        csvLog = String.Concat(csvLog, ",");
                        csvLog = String.Concat(csvLog, ",");
                        csvLog = String.Concat(csvLog, ",");
                        csvLog = String.Concat(csvLog, ",");
                        csvLog = String.Concat(csvLog, ",");
                        csvLog = String.Concat(csvLog, ",");
                        csvLog = String.Concat(csvLog, ",");
                        csvLog = String.Concat(csvLog, ",");
                        csvLog = String.Concat(csvLog, ",");
                        csvLog = String.Concat(csvLog, ",");
                        csvLog = String.Concat(csvLog, ",");
                    }
                }

                logger.LogString(csvLog);
            }
        }
        #endregion
    }
}
