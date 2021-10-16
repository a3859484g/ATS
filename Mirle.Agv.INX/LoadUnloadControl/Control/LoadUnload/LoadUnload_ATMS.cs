using Mirle.Agv.INX.Controller;
using Mirle.Agv.INX.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Control
{
    public class LoadUnload_ATMS : LoadUnload
    {
        // 我不知道有沒有CST ID Reader 也不知道是不是Keyence的.
        private BarcodeReader_Keyence cstIDReader = null;

        private bool logMode = true;

        private bool initialEnd = false;

        public override void Initial(MIPCControlHandler mipcControl, AlarmHandler alarmHandler)
        {
            localData.LoadUnloadData.AllLoading.Add(EnumCstInAGVLocate.Left, false);
            localData.LoadUnloadData.AllLoadingFlag.Add(EnumCstInAGVLocate.Left, false);
            localData.LoadUnloadData.AllCstID.Add(EnumCstInAGVLocate.Left, "");

            this.alarmHandler = alarmHandler;
            this.mipcControl = mipcControl;
            device = MethodInfo.GetCurrentMethod().ReflectedType.Name;

            ReadPIOTimeoutCSV();
            InitialRobotData();
            CheckAllStringInMIPCConfig();

            RightPIO = new PIOFlow_ATMS_LoadUnload();
            RightPIO.Initial(alarmHandler, mipcControl, "Right", "", normalLogName);
            CanRightLoadUnload = true;

            LeftPIO = new PIOFlow_ATMS_LoadUnload();
            LeftPIO.Initial(alarmHandler, mipcControl, "Right", "", normalLogName);
            CanLeftLoadUnload = true;

            ConnectCSTIDReader();
            initialEnd = true;
        }

        public override void CloseLoadUnload()
        {
        }


        #region InitialRobotData.
        private void InitialRobotData()
        {
            CanPause = true;
            BreakenStepMode = true;

            HomeText = "";
            // Robot回Home的限制?.

            /// 如果有手臂馬達資訊在log.
            //FeedbackAxisList.Add(EnumLoadUnloadAxisName.Z軸.ToString());
            //AxisList.Add(EnumLoadUnloadAxisName.Z軸.ToString());
            //AxisCanJog.Add(false);

            /// 可以Jog在加. ( 正負方向的名稱 ). 
            //AxisPosName.Add("上升");
            //AxisNagName.Add("下降");

            /// 手臂Sensor如果要顯示.(名稱. 儲存資料(資料結構能選擇是否延遲).
            //AxisSensorList.Add(AxisList[AxisList.Count - 1], new List<string>() { Z軸上極限, Z軸上位, Z軸下位, Z軸下極限 });
            //AxisSensorDataList.Add(AxisList[AxisList.Count - 1], new List<DataDelayAndChange>() { new DataDelayAndChange(delayTime, EnumDelayType.OnDelay), new DataDelayAndChange(delayTime, EnumDelayType.OnDelay), new DataDelayAndChange(delayTime, EnumDelayType.OnDelay), new DataDelayAndChange(delayTime, EnumDelayType.OnDelay) });

        }

        private void CheckAllStringInMIPCConfig()
        {
            // 看是否要檢查預設的手臂命令系列是否有在csv內定義.

            //for (int i = 0; i < AxisList.Count; i++)
            //{
            //    if (!axisDataList.ContainsKey(AxisList[i]))
            //    {
            //        WriteLog(3, "", String.Concat("AxisDataList.csv內 無 AxisName = ", AxisList[i]));
            //        configAllOK = false;
            //    }
            //}

            if (!configAllOK)
                SetAlarmCode(EnumLoadUnloadControlErrorCode.取放貨初始化失敗_MIPCTag缺少);
        }
        #endregion


        private List<EnumLoadUnloadControlErrorCode> alarmCodeClearList = new List<EnumLoadUnloadControlErrorCode>()
        {
            /// ResetAlarm時要自動清除的AlarmCode列表.
            //EnumLoadUnloadControlErrorCode.TA1_Timeout,
            //EnumLoadUnloadControlErrorCode.TA2_Timeout,
            //EnumLoadUnloadControlErrorCode.TA3_Timeout,
            //EnumLoadUnloadControlErrorCode.TP1_Timeout,
            //EnumLoadUnloadControlErrorCode.TP2_Timeout,
            //EnumLoadUnloadControlErrorCode.TP3_Timeout,
            //EnumLoadUnloadControlErrorCode.TP4_Timeout,
            //EnumLoadUnloadControlErrorCode.TP5_Timeout,

            //EnumLoadUnloadControlErrorCode.取放貨中EQPIOOff,
            //EnumLoadUnloadControlErrorCode.取放命令與EQRequest不相符,
            //EnumLoadUnloadControlErrorCode.RollerStop後CV訊號異常,
            //EnumLoadUnloadControlErrorCode.取放貨中CV檢知異常,
            //EnumLoadUnloadControlErrorCode.取放貨中EMS,
            //EnumLoadUnloadControlErrorCode.取貨_LoadingOn異常,
            //EnumLoadUnloadControlErrorCode.放貨_LoadingOff異常,
            //EnumLoadUnloadControlErrorCode.AlignmentNG,
            //EnumLoadUnloadControlErrorCode.HomeSensor未On,
            //EnumLoadUnloadControlErrorCode.Z軸上定位未On,
            //EnumLoadUnloadControlErrorCode.取放貨異常_MIPCCommandReturnFail,
            //EnumLoadUnloadControlErrorCode.動作結束但不在InpositionRange,
            //EnumLoadUnloadControlErrorCode.AlignmentValueNG,
            //EnumLoadUnloadControlErrorCode.取放中極限觸發,
            //EnumLoadUnloadControlErrorCode.取放中軸異常,
            //EnumLoadUnloadControlErrorCode.取放中安全迴路異常,
            //EnumLoadUnloadControlErrorCode.RollerStopTimeout,
            //EnumLoadUnloadControlErrorCode.ServoOnTimeout,
            //EnumLoadUnloadControlErrorCode.Port站ES或HO_AVBLNotOn,
            //EnumLoadUnloadControlErrorCode.取放貨站點資訊異常,
            //EnumLoadUnloadControlErrorCode.取放貨異常_Z軸升降中CVSensor異常,

            //EnumLoadUnloadControlErrorCode.回Home失敗_CVSensor狀態異常,
            //EnumLoadUnloadControlErrorCode.回Home失敗_CST回CV_Timeout,
            //EnumLoadUnloadControlErrorCode.回Home失敗_P軸不在Home,
            //EnumLoadUnloadControlErrorCode.回Home失敗_Theta軸不再Home,
            //EnumLoadUnloadControlErrorCode.回Home失敗_Z軸不在上定位,
            //EnumLoadUnloadControlErrorCode.回Home失敗_ServoOn_Timeout,
            //EnumLoadUnloadControlErrorCode.回Home失敗_Exception,
            //EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗,
            //EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止,
            //EnumLoadUnloadControlErrorCode.回Home失敗_極限Sensor未On,
            //EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout,
            //EnumLoadUnloadControlErrorCode.回Home失敗_HomeSensor未on,
            //EnumLoadUnloadControlErrorCode.回Home失敗_HomeSensor未off
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

        private bool reconnectedAndResetAxisError = false;

        private void ResetAlarm_ReConnectAndResetAxisError()
        {
            reconnectedAndResetAxisError = true;
            ConnectCSTIDReader();

            /// 有動力電 需要手臂復規時. 下命令的地方.

            reconnectedAndResetAxisError = false;
        }

        #region Connect CST ID Reader.
        private void ConnectCSTIDReader()
        {
            /// 沒有補正元件, 看是否有CST ID Reader.

            if (localData.SimulateMode)
                return;

            //string errorMessage = "";

            //if (cstIDReader == null)
            //{
            //    cstIDReader = new BarcodeReader_Keyence();

            //    if (!cstIDReader.Connect("192.168.29.123", ref errorMessage))
            //        SetAlarmCode(EnumLoadUnloadControlErrorCode.CSTIDReader_連線失敗);
            //}
            //else
            //{
            //    if (!cstIDReader.Connected)
            //    {
            //        if (cstIDReader.Connect("192.168.29.123", ref errorMessage))
            //            ResetAlarmCode(EnumLoadUnloadControlErrorCode.CSTIDReader_連線失敗);
            //    }
            //    else if (cstIDReader.Error)
            //    {
            //        cstIDReader.ResetError();

            //        if (!cstIDReader.Error)
            //            ResetAlarmCode(EnumLoadUnloadControlErrorCode.CSTIDReader_斷線);
            //    }
            //}
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

        private EnumLoadUnloadControlErrorCode GetPIOAlarmCode()
        {
            /// 這邊要看ATMS 的PIO timeout名稱定義. 做適當的修改.
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
                default:
                    break;
            }

            localData.LoadUnloadData.LoadUnloadCommand.PIOResult = RightPIO.Timeout;
            return alarmCode;
        }

        private void SetAlarmCodeAndSetCommandErrorCode(EnumLoadUnloadControlErrorCode errorCode, EnumLoadUnloadErrorLevel level)
        {
            /// 來判斷是 prePIO異常/Busy中異常/後PIO異常/後續動作異常 來判斷要上報什麼命令 Interlock/二重格?(TPC有,UMTC沒有)/異常/正常 給Middler.
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

        public override void LoadUnloadStart()
        {
            /// 開始命令?.
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

        public override void UpdateLoadingAndCSTID()
        {
            /// 更新 車上是否有貨物, 如果無命令狀態可以直接讀取到CSTID的話 這邊可以去Trigger 去抓CST ID.
            /// 記得要更新到AGV車上

            if (localData.LoadUnloadData.LoadUnloadCommand == null)
                return;

            /// Loading是單純的點位資料, LogicFlag 是否要使用都可以, 這個我是當作理論上車上有無CST(僅在Auto下才會判斷)
            /// 類似如果Auto下取貨成功 在沒有取放命令中, LogicFlag和 Loading不一樣 會有異常.
            /// 就使用 原本存放的地方 當作 左或1號儲存區, 有帶Buffer的當作 右或2號儲存區. 

            //localData.LoadUnloadData.Loading = true;
            //localData.LoadUnloadData.Loading_LogicFlag = true;
            //localData.LoadUnloadData.CstID = "";

            //localData.LoadUnloadData.Loading_Buffer = true;
            //localData.LoadUnloadData.Loading_LogicFlag_Buffer = true;
            //localData.LoadUnloadData.CstID_Buffer = "";
        }

        public override void UpdateForkHomeStatus()
        {
            ///取得手臂資料狀態?.
        }

        #region 回Home流程.
        public override void Home()
        {
            if (localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null &&
                localData.LoadUnloadData.LoadUnloadCommand == null && !localData.LoadUnloadData.Homing)
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
                localData.LoadUnloadData.LoadUnloadCommand == null && !localData.LoadUnloadData.Homing)
            {
                if (!reconnectedAndResetAxisError)
                {
                    WriteLog(7, "", "Home-Initial");
                    localData.LoadUnloadData.Homing = true;
                    localData.LoadUnloadData.HomeStop = false;
                    homeThread = new Thread(HomeThread);
                    homeThread.Start();
                }
            }
        }

        public Thread homeThread = null;

        #region 回HomeThread.
        private void HomeThread()
        {
            try
            {
                ///
                /// ....
                /// ....
                /// ....
                /// 

                localData.LoadUnloadData.Homing = false;
            }
            catch (Exception ex)
            {
                SetAlarmCode(EnumLoadUnloadControlErrorCode.回Home失敗_Exception);
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                localData.LoadUnloadData.Homing = false;
            }
        }
        #endregion
        #endregion

        public override void LoadUnloadPreAction()
        {
            /// 如果有到站前要搶時間需要偷跑但是手臂不會實際運作的時候放這邊,
            /// 以UMTC為例 在省電模式下會ServoOff 有剎車的Z軸和Roller, 這邊就是偷一點ServoOn的時間.
        }

        #region CSV.
        public override void WriteCSV()
        {
            if (initialEnd)
            {
                /// csvlog放入想要存的資料 類似 , 記得資料中間要加,號.
                //string csvLog = "";

                //logger.LogString(csvLog);
            }
        }
        #endregion
    }
}
