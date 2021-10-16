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
    class PIOFlow_ATS_Charging : PIOFlow_Charging
    {

        public string chargingCommand = "CPIO-out01";

        public string go = "CPIO-in00";
        public string enable = "CPIO-in01";
        public string warning = "CPIO-in02";
        public string requestForFreeCommand = "CPIO-in03";
        public string op = "CPIO-in04";
        public string hp = "CPIO-in05";
        public string error = "CPIO-in06";
        public string fullCharging = "CPIO-in07";

        public string PIOSelect = "CPIO-Select";
        public string RFPIOCmd = "RFPIO_Cmd";
        public string RFPIOSetID = "RFPIO_SetID";
        public string RFPIOSetCH = "RFPIO_SetCH";

        //public string chargingSafety = "";
        //public string confimSensor = "";

        private Thread thread = null;

        private Dictionary<string, DataDelayAndChange> allIOData = new Dictionary<string, DataDelayAndChange>();

        private double delayTime = 0;

        private bool allTagInMIPCConfig = true;

        private double sendCommandChargingNotOnTimeout = 20000;

        private double sendCommandStopChargingNotOffTimeout = 20000;

        private bool logMode = true;

        private PIO_Cantops CPIO = new PIO_Cantops();

        public override void Initial(AlarmHandler alarmHandler, MIPCControlHandler mipcControl, string pioName, string pioDirection, string normalLogName)
        {
            this.normalLogName = normalLogName;
            ConfirmSensor = "ConfirmSensor";
            ChargingSaftey = "ChargingSaftey";


            this.alarmHandler = alarmHandler;
            PIOName = pioName;
            this.mipcControl = mipcControl;
            device = MethodInfo.GetCurrentMethod().ReflectedType.Name;

            ConfirmSensor = String.Concat(ConfirmSensor, pioDirection);
            ChargingSaftey = String.Concat(ChargingSaftey, pioDirection);

            #region INPUT (String + pioDirection)
            chargingCommand = String.Concat(chargingCommand, pioDirection);

            PIOOutputTagList.Add(chargingCommand);
            PIOOutputNameList.Add("ChargingCommand");
            #endregion

            #region OUTPUT (String + pioDirection)
            // Output Name = String.Concat(Output Name, pioDirection);
            enable = String.Concat(enable, pioDirection);
            warning = String.Concat(warning, pioDirection);
            requestForFreeCommand = String.Concat(requestForFreeCommand, pioDirection);
            op = String.Concat(op, pioDirection);
            hp = String.Concat(hp, pioDirection);
            error = String.Concat(error, pioDirection);
            fullCharging = String.Concat(fullCharging, pioDirection);

            PIOInputTagList.Add(enable);
            PIOInputNameList.Add("Enable");
            PIOInputTagList.Add(warning);
            PIOInputNameList.Add("Warning");
            PIOInputTagList.Add(requestForFreeCommand);
            PIOInputNameList.Add("RequestForFreeCommand");
            PIOInputTagList.Add(op);
            PIOInputNameList.Add("OP");
            PIOInputTagList.Add(hp);
            PIOInputNameList.Add("HP");
            PIOInputTagList.Add(error);
            PIOInputNameList.Add("Error");
            PIOInputTagList.Add(fullCharging);
            PIOInputNameList.Add("FullCharging");
            #endregion

            allIOData.Add(chargingCommand, new DataDelayAndChange(delayTime, EnumDelayType.OnDelay));
            allIOData.Add(enable, new DataDelayAndChange(delayTime, EnumDelayType.OnDelay));
            allIOData.Add(warning, new DataDelayAndChange(delayTime, EnumDelayType.OnDelay));
            allIOData.Add(requestForFreeCommand, new DataDelayAndChange(delayTime, EnumDelayType.OnDelay));
            allIOData.Add(op, new DataDelayAndChange(delayTime, EnumDelayType.OnDelay));
            allIOData.Add(hp, new DataDelayAndChange(delayTime, EnumDelayType.OnDelay));
            allIOData.Add(error, new DataDelayAndChange(delayTime, EnumDelayType.OnDelay));
            allIOData.Add(fullCharging, new DataDelayAndChange(delayTime, EnumDelayType.OnDelay));

            //string errorMessage = "";

            for (int i = 0; i < PIOOutputTagList.Count; i++)
            {
                if (!localData.MIPCData.AllDataByMIPCTagName.ContainsKey(PIOOutputTagList[i]))
                {
                    WriteLog(3, "", String.Concat("Tag : ", PIOOutputTagList[i], "並不再MIPC Config內"));
                    allTagInMIPCConfig = false;
                }
            }

            for (int i = 0; i < PIOInputTagList.Count; i++)
            {
                if (!localData.MIPCData.AllDataByMIPCTagName.ContainsKey(PIOInputTagList[i]))
                {
                    WriteLog(3, "", String.Concat("Tag : ", PIOOutputTagList[i], "並不再MIPC Config內"));
                    allTagInMIPCConfig = false;
                }
            }

            if (!localData.MIPCData.AllDataByMIPCTagName.ContainsKey(ConfirmSensor))
            {
                WriteLog(3, "", String.Concat("Tag : ", ConfirmSensor, "並不再MIPC Config內"));
                allTagInMIPCConfig = false;
            }

            if (!localData.MIPCData.AllDataByMIPCTagName.ContainsKey(ChargingSaftey))
            {
                WriteLog(3, "", String.Concat("Tag : ", ChargingSaftey, "並不再MIPC Config內"));
                allTagInMIPCConfig = false;
            }


            //if(CPIO.Fun_Rs232_OpenPort(1))
            /*
             if (CPIO.ConnectSocket("192.168.29.234:2001", ref errorMessage))
             {
                 WriteLog(7, "", "Cantops PIO開啟Port成功");
                 mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { PIOSelect }, new List<float> { 1 });
                 if (CPIO.Fun_RFPIO_Socket_SetCH_ID("010", "000001"))
                 {
                     WriteLog(7, "", "Cantops PIO設定CH和ID成功");
                     mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { PIOSelect }, new List<float> { 0 });
                 }
                 else
                 {
                     WriteLog(3, "", "Cantops PIO設定CH和ID失敗");
                 }

             }
             else
             {
                 WriteLog(3, "", "Cantops PIO開啟Port失敗");
             }
            */

        }

        public override void StartPIO()
        {
            string chargerStationAddressID = "";

            if (!allTagInMIPCConfig)
            {
                WriteLog(3, "", "因為有Tag不存在於MIPC Config內, 因此無法充電");
                return;
            }

            if (localData.MoveControlData.MoveCommand != null)
            {
                WriteLog(3, "", "移動命令中, 無法充電");
                return;
            }

            if (localData.MoveControlData.MotionControlData.JoystickMode)
            {
                WriteLog(3, "", "搖桿模式中, 無法充電");
                return;
            }

            if (localData.MoveControlData.MotionControlData.AllServoOff != localData.MoveControlData.MotionControlData.AllServoStatus)
            {
                WriteLog(3, "", "走行輪非全部ServoOff, 無法充電");
                return;
            }

            if (ManualChargingSafetyOn)
            {
                WriteLog(3, "", "手動充電中, 無法自動充電流程");
                return;
            }

            //addressID = localData.Location.LastAddress;

            if(localData.MIPCData.GetDataByMIPCTagName(go) != 1)
            {
                MapAGVPosition now = localData.Real;
                
                if(now != null)
                {
                    foreach (MapAddress address in localData.TheMapInfo.AllAddress.Values)
                    {
                        if (address.ChargingDirection != EnumStageDirection.None)
                        {
                            if (ComputeFunction.Instance.GetDistanceFormTwoAGVPosition(now, address.AGVPosition) < 200 &&
                                 Math.Abs(ComputeFunction.Instance.GetCurrectAngle(now.Angle - address.AGVPosition.Angle)) < 5)
                            {
                                chargerStationAddressID = address.Id;
                                break;
                            }
                        }
                    }
                }
                
                if(chargerStationAddressID != "")
                {
                    switch (localData.TheMapInfo.AllAddress[chargerStationAddressID].ChargingDirection)
                    {
                        case EnumStageDirection.Left:
                            if (localData.MIPCData.CanLeftCharging)
                                localData.MIPCData.LeftChargingPIO.ChangeRFPIOChannelByAddressID(chargerStationAddressID);
                            break;
                        case EnumStageDirection.Right:
                            if (localData.MIPCData.CanRightCharging)
                                localData.MIPCData.RightChargingPIO.ChangeRFPIOChannelByAddressID(chargerStationAddressID);
                            break;
                    }

                }
                else
                {
                    WriteLog(3, "", "不在充電站點上，無法自動充電流程");
                    return;
                }


                //if(localData.TheMapInfo.AllAddress[chargerStationAddressID].ChargingDirection != EnumStageDirection.None)
                //{

                //    switch (localData.TheMapInfo.AllAddress[chargerStationAddressID].ChargingDirection)
                //    {
                //        case EnumStageDirection.Left:
                //            if (localData.MIPCData.CanLeftCharging)
                //                localData.MIPCData.LeftChargingPIO.ChangeRFPIOChannelByAddressID(chargerStationAddressID);
                //            break;
                //        case EnumStageDirection.Right:
                //            if (localData.MIPCData.CanRightCharging)
                //                localData.MIPCData.RightChargingPIO.ChangeRFPIOChannelByAddressID(chargerStationAddressID);
                //            break;
                //    }
                //}
                //else
                //{
                //    WriteLog(3, "", "不在充電站點上，無法自動充電流程");
                //    return;
                //}
                ////WriteLog(3, "", "設定RFPIO失敗，無法自動充電流程");
                ////return;
            }


            if (thread == null || !thread.IsAlive)
            {
                Charging = true;
                Status = EnumPIOStatus.T1;
                stopCharging = false;
                thread = new Thread(ChargingThread);
                thread.Start();
            }
        }

        public override void StopPIO()
        {
            stopCharging = true;

            if (ManualChargingSafetyOn)
                ChargingSafetyOnOff(false);
        }

        private bool stopCharging = false;

        private void UpdateInputAll()
        {
            foreach (string mipcTag in allIOData.Keys)
            {
                allIOData[mipcTag].Data = (localData.MIPCData.GetDataByMIPCTagName(mipcTag) != 0);

                if (allIOData[mipcTag].Change)
                    WriteLog(7, "", String.Concat(mipcTag, " Change to ", (allIOData[mipcTag].data ? "on" : "off")));
            }
        }

        private void CheckAlarm()
        {
            if (allIOData[error].Data)
            {
                SetAlarmCode(EnumLoadUnloadControlErrorCode.ChargerStationAlarm);
                ChargingStep = EnumChargingStatus.SendStopCharging;
            }
        }

        private void CheckWarning()
        {
            if (allIOData[warning].Data)
            {
                SetAlarmCode(EnumLoadUnloadControlErrorCode.ChargerStationWarning);
                ChargingStep = EnumChargingStatus.SendStopCharging;
            }
        }

        private void CheckRequestForFreeCommand()
        {
            if (allIOData[requestForFreeCommand].Data)
                ChargingStep = EnumChargingStatus.SendStopCharging;
        }

        private void CheckFullCharging()
        {
            if (allIOData[fullCharging].Data)
            {
                SetAlarmCode(EnumLoadUnloadControlErrorCode.ChargerStationFullCharging);
                ChargingStep = EnumChargingStatus.SendStopCharging;
            }
        }

        private void CheckBatteryFullCharging()
        {
            if (localData.BatteryInfo.Battery_SOC > localData.BatteryConfig.HighBattery_SOC ||
                localData.BatteryInfo.Battery_V > localData.BatteryConfig.HighBattery_Voltage)
            {
                ChargingStep = EnumChargingStatus.SendStopCharging;
            }
        }

        private void CheckChargingA()
        {
            if ((Math.Abs(localData.BatteryInfo.Battery_A) + Math.Abs(localData.BatteryInfo.Meter_A)) > localData.BatteryConfig.ChargingMaxCurrent)
            {
                SetAlarmCode(EnumLoadUnloadControlErrorCode.ChargingOverA);
                ChargingStep = EnumChargingStatus.SendStopCharging;
            }
        }

        private void CheckOPOn()
        {
            if (!allIOData[op].Data)
            {
                SetAlarmCode(EnumLoadUnloadControlErrorCode.ChargingWith_OPOff);
                ChargingStep = EnumChargingStatus.SendStopCharging;
            }
        }

        private void CheckSafetySensorStatus()
        {
            if (localData.MIPCData.SafetySensorStatus >= EnumSafetyLevel.IPCEMO)
            {
                SetAlarmCode(EnumLoadUnloadControlErrorCode.ChargingWith_SensorEMO);
                ChargingStep = EnumChargingStatus.SendStopCharging;
            }
        }

        public void Check溫度()
        {
            if ((localData.BatteryInfo.Battery_溫度1 > 0 && localData.BatteryInfo.Battery_溫度1 > localData.BatteryConfig.ChargingMaxTemperature) ||
                (localData.BatteryInfo.Battery_溫度2 > 0 && localData.BatteryInfo.Battery_溫度2 > localData.BatteryConfig.ChargingMaxTemperature))
            {
                SetAlarmCode(EnumLoadUnloadControlErrorCode.ChargingWith_SensorEMO);
                ChargingStep = EnumChargingStatus.SendStopCharging;
            }
        }

        private void CheckStopCharging()
        {
            if (stopCharging)
                ChargingStep = EnumChargingStatus.SendStopCharging;
        }

        private void ChargingThread()
        {
            bool chargingOK = false;

            try
            {
                DateTime startChargingTime = DateTime.Now;
                double startV = localData.BatteryInfo.Battery_V;
                double startSOC = localData.BatteryInfo.Battery_SOC;

                Stopwatch timer = new Stopwatch();
                UpdateInputAll();
                bool sendWaitHPTimeout = false;
                //bool forceCharging = true;  //電池資料未讀前取消
                EnumChargingStatus lastStatus = EnumChargingStatus.Idle;

                EnumAutoState autoManual = localData.AutoManual;

                if (GetConfirmSensorOnOff)
                {
                    if (allIOData[enable].Data)
                    {
                        if (allIOData[hp].Data)
                        {
                            if (!allIOData[error].Data)
                            {
                                ChargingStep = EnumChargingStatus.SendCharingCommand;
                                chargingOK = true;
                            }
                            else
                                ChargingStep = EnumChargingStatus.Idle;
                        }
                        else
                        {
                            ChargingStep = EnumChargingStatus.WaitHPOn;
                        }

                        while (ChargingStep != EnumChargingStatus.Idle)
                        {
                            UpdateInputAll();

                            switch (ChargingStep)
                            {
                                case EnumChargingStatus.SendCharingCommand:
                                    // 送充電指令.
                                    mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { chargingCommand }, new List<float> { 1 });

                                    ChargingStep = EnumChargingStatus.WaitOPOn;
                                    timer.Restart();
                                    break;

                                case EnumChargingStatus.WaitOPOn:
                                    if (allIOData[op].Data)
                                    {
                                        ChargingStep = EnumChargingStatus.WaitA;
                                        mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { ChargingSaftey }, new List<float> { 1 });
                                    }
                                    else
                                    {
                                        if (timer.ElapsedMilliseconds > sendCommandChargingNotOnTimeout)
                                        {
                                            SetAlarmCode(EnumLoadUnloadControlErrorCode.ChargerStationWaitOPTimeout);
                                            ChargingStep = EnumChargingStatus.SendStopCharging;
                                        }
                                        else
                                        {
                                            //CheckChargingA();   //電池資料未讀前取消
                                            CheckAlarm();
                                            CheckWarning();
                                            CheckRequestForFreeCommand();
                                            CheckFullCharging();
                                            //CheckBatteryFullCharging(); //電池資料未讀前取消
                                            //CheckStopCharging();
                                            CheckSafetySensorStatus();    //電池資料未讀前取消
                                            //Check溫度();        //電池資料未讀前取消
                                        }
                                    }

                                    break;

                                case EnumChargingStatus.WaitA:
                                    //ChargingStep = EnumChargingStatus.Charging;
                                    //break;
                                    if (localData.BatteryInfo.Battery_A > 0)
                                    {
                                        ChargingStep = EnumChargingStatus.Charging;
                                        timer.Restart();
                                    }
                                    else
                                    {
                                        if (timer.ElapsedMilliseconds > sendCommandChargingNotOnTimeout)
                                        {
                                            SetAlarmCode(EnumLoadUnloadControlErrorCode.ChargerStationWaitChargingTimeout);
                                            ChargingStep = EnumChargingStatus.SendStopCharging;
                                        }
                                        else
                                        {
                                            CheckChargingA();   //電池資料未讀前取消
                                            CheckAlarm();
                                            CheckWarning();
                                            CheckRequestForFreeCommand();
                                            CheckFullCharging();
                                            CheckBatteryFullCharging();  //電池資料未讀前取消
                                            CheckStopCharging();
                                            CheckOPOn();
                                            CheckSafetySensorStatus();    //電池資料未讀前取消
                                            Check溫度();    //電池資料未讀前取消
                                        }
                                    }
                                    break;

                                case EnumChargingStatus.Charging:
                                    if (timer.ElapsedMilliseconds > 120 * 60 * 1000)
                                    {
                                        SetAlarmCode(EnumLoadUnloadControlErrorCode.ChargingTimeout);
                                        ChargingStep = EnumChargingStatus.SendStopCharging;
                                    }
                                    else
                                    {
                                        CheckChargingA();   //電池資料未讀前取消
                                        CheckAlarm();
                                        CheckWarning();
                                        CheckRequestForFreeCommand();
                                        CheckFullCharging();
                                        CheckBatteryFullCharging();  //電池資料未讀前取消
                                        CheckStopCharging();
                                        CheckOPOn();
                                        CheckSafetySensorStatus();    //電池資料未讀前取消
                                        Check溫度();    //電池資料未讀前取消
                                    }
                                    break;

                                case EnumChargingStatus.SendStopCharging:
                                    mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { ChargingSaftey, chargingCommand }, new List<float> { 0, 0 });   
                                    ChargingStep = EnumChargingStatus.WaitHPOn;

                                    timer.Restart();
                                    break;

                                case EnumChargingStatus.WaitHPOn:
                                    if (allIOData[hp].Data)
                                    {
                                        mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { PIOSelect }, new List<float> { 1 });
                                        ChargingStep = EnumChargingStatus.Idle;
                                    }
                                        

                                    if (!sendWaitHPTimeout && timer.ElapsedMilliseconds > sendCommandStopChargingNotOffTimeout)
                                    {
                                        sendWaitHPTimeout = true;
                                        SetAlarmCode(EnumLoadUnloadControlErrorCode.ChargerStationWaitHPTimeout);
                                    }

                                    break;

                                default:
                                    break;
                            }

                            if (logMode)
                            {
                                if (lastStatus != ChargingStep)
                                {
                                    WriteLog(7, "", String.Concat("ChargingStep Change to : ", ChargingStep.ToString()));
                                    lastStatus = ChargingStep;
                                }
                            }

                            Thread.Sleep(PIO_SleepTime);
                        }

                        if (chargingOK)
                        {
                            //Time(年月日),CommandType,CommandID,StartTime,EndTime,DeltaTime,StartSOC,StartV,EndSOC,EndV,Result,ErrorCode,是否Alarm,AutoManual
                            commandRecordLogger.LogString(String.Concat(DateTime.Now.ToString("yyyy/MM/dd"), ",", "Charging", ",",
                                                                        "Charging-", localData.MIPCData.LocalChargingCount.ToString("0"), ",",
                                                                        startChargingTime.ToString("HH:mm:ss"), ",", DateTime.Now.ToString("HH:mm:ss"), ",",
                                                                        (DateTime.Now - startChargingTime).TotalSeconds.ToString("0.00"), "s,",
                                                                        startSOC.ToString("0"), ",", localData.BatteryInfo.Battery_SOC.ToString("0"), ",",
                                                                        startV.ToString("0.0"), ",", localData.BatteryInfo.Battery_V.ToString("0.0"), ",",
                                                                        "", ",", "", ",", "", ",", autoManual.ToString()));
                            localData.MIPCData.AddChargingMessage =
                                String.Concat(startChargingTime.ToString("HH:mm:ss"), " ~ ", DateTime.Now.ToString("HH:mm:ss"),
                                              ", V : ", startV.ToString("0.0"), " ~ ", localData.BatteryInfo.Battery_V.ToString("0.0"),
                                              ", SOC : ", startSOC.ToString("0"), " ~ ", localData.BatteryInfo.Battery_SOC.ToString("0"));
                        }
                    }
                    else
                    {
                        SetAlarmCode(EnumLoadUnloadControlErrorCode.ChargerStationNotService);
                    }
                }
                else
                {
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.ChargingConfrimSensorNotOn);
                }
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("Exception : ", ex.ToString()));
                mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { ChargingSaftey, chargingCommand }, new List<float> { 0, 0 });
            }

            Status = EnumPIOStatus.Idle;
            Charging = false;

            if (chargingOK)
                localData.MIPCData.LocalChargingCount++;
        }

        public override void AlarmCodeClear()
        {
            if (!Charging)
                ResetAlarmCode(EnumLoadUnloadControlErrorCode.ChargerStationWaitHPTimeout);

            ResetAlarmCode(EnumLoadUnloadControlErrorCode.ChargingConfrimSensorNotOn);
            ResetAlarmCode(EnumLoadUnloadControlErrorCode.ChargerStationNotService);
            ResetAlarmCode(EnumLoadUnloadControlErrorCode.ChargerStationWaitOPTimeout);
            ResetAlarmCode(EnumLoadUnloadControlErrorCode.ChargerStationWaitChargingTimeout);
            ResetAlarmCode(EnumLoadUnloadControlErrorCode.ChargerStationAlarm);
            ResetAlarmCode(EnumLoadUnloadControlErrorCode.ChargerStationWarning);
            ResetAlarmCode(EnumLoadUnloadControlErrorCode.ChargerStationFullCharging);
            ResetAlarmCode(EnumLoadUnloadControlErrorCode.ChargingTimeout);
            ResetAlarmCode(EnumLoadUnloadControlErrorCode.ChargingWith_OPOff);
            ResetAlarmCode(EnumLoadUnloadControlErrorCode.ChargingWith_SensorEMO);
            ResetAlarmCode(EnumLoadUnloadControlErrorCode.ChargingOverA);
            ResetAlarmCode(EnumLoadUnloadControlErrorCode.Charging溫度過高);
        }

        public override void ChargingSafetyOnOff(bool OnOff)
        {
            if (OnOff)
            {
                if (localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null &&
                    !localData.MoveControlData.MotionControlData.JoystickMode &&
                    localData.MoveControlData.MotionControlData.AllServoOff == localData.MoveControlData.MotionControlData.AllServoStatus)
                {
                    if (!ManualChargingSafetyOn)
                    {
                        if (GetConfirmSensorOnOff)
                        {
                            if (mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { ChargingSaftey }, new List<float> { 1 }))
                            {
                                ManualChargingSafetyOn = true;
                                Charging = true;
                            }
                        }
                        else
                        {
                            SetAlarmCode(EnumLoadUnloadControlErrorCode.ChargingConfrimSensorNotOn);
                        }
                    }
                }
            }
            else
            {
                if (ManualChargingSafetyOn)
                {
                    if (mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { ChargingSaftey }, new List<float> { 0 }))
                    {
                        ManualChargingSafetyOn = false;
                        Charging = false;
                    }
                }
            }
        }

        public override bool GetChargingSafetyOnOff
        {
            get
            {
                return localData.MIPCData.GetDataByMIPCTagName(ChargingSaftey) != 0;
            }
        }

        public override bool GetConfirmSensorOnOff
        {
            get
            {
                return localData.MIPCData.GetDataByMIPCTagName(ConfirmSensor) == 0;
            }
        }

        public override bool GetPIOStatueByTag(string tag)
        {
            return (float)localData.MIPCData.GetDataByMIPCTagName(tag) == 1;
        }

        public override bool ChangeRFPIOChannel(string deviceID, string channelID)
        {
            Stopwatch setRFPIOTimer = new Stopwatch();
            setRFPIOTimer.Restart();
            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { PIOSelect }, new List<float> { 1 });

            while (setRFPIOTimer.ElapsedMilliseconds < 500)
                Thread.Sleep(1);

            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { RFPIOSetCH }, new List<float> { 10 });
            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { RFPIOSetID }, new List<float> { 1 });

            setRFPIOTimer.Restart();
            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { RFPIOCmd }, new List<float> { 1 });
            while (setRFPIOTimer.ElapsedMilliseconds < 500)
                Thread.Sleep(1);

            setRFPIOTimer.Restart();

            while (localData.MIPCData.GetDataByMIPCTagName(RFPIOCmd) != 2)
            {
                if (setRFPIOTimer.ElapsedMilliseconds >= 1000)
                {
                    WriteLog(3, "", "設定RFPIO Timeout");
                    return false;
                }
            }

            if (localData.MIPCData.GetDataByMIPCTagName(RFPIOCmd) == 2)
            {
                mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { PIOSelect }, new List<float> { 0 });
                setRFPIOTimer.Restart();
                while (localData.MIPCData.GetDataByMIPCTagName(go) != 1)
                {
                    if (setRFPIOTimer.ElapsedMilliseconds >= 2000)
                    {
                        WriteLog(3, "", "RFPIO GO Timeout");
                        return false;
                    }
                }
                WriteLog(7, "", "設定RFPIO 成功");
                return true;
            }
            else
                return false;
        }

        public override void ChangeRFPIOChannelByAddressID(string addressID)
        {
            float RFPIOCHValue;
            float RFPIOIDValue;
            string hexStringRFPIOID = "";
            
            if(localData.TheMapInfo.AllAddress.ContainsKey(addressID))
            {
                
                if(!float.TryParse(localData.TheMapInfo.AllAddress[addressID].RFPIOChannelID , out RFPIOCHValue))
                {
                    WriteLog(3, "", "轉換RFPIO CH to float失敗");
                }

                hexStringRFPIOID = localData.TheMapInfo.AllAddress[addressID].RFPIODeviceID;
                RFPIOIDValue = (float)(Convert.ToInt32(hexStringRFPIOID, 16));
                //if (!float.TryParse(localData.TheMapInfo.AllAddress[addressID].RFPIODeviceID, out RFPIOIDValue))
                //{
                //    WriteLog(3, "", "轉換RFPIO ID to float失敗");
                //}

                Stopwatch setRFPIOTimer = new Stopwatch();
                setRFPIOTimer.Restart();
                mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { PIOSelect }, new List<float> { 1 });

                while (setRFPIOTimer.ElapsedMilliseconds < 500)
                    Thread.Sleep(1);

                mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { RFPIOSetCH }, new List<float> { RFPIOCHValue });
                mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { RFPIOSetID }, new List<float> { RFPIOIDValue });

                setRFPIOTimer.Restart();
                mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { RFPIOCmd }, new List<float> { 1 });
                while (setRFPIOTimer.ElapsedMilliseconds < 500)
                    Thread.Sleep(1);

                setRFPIOTimer.Restart();

                while (localData.MIPCData.GetDataByMIPCTagName(RFPIOCmd) != 2)
                {
                    if (setRFPIOTimer.ElapsedMilliseconds >= 1000)
                    {
                        WriteLog(3, "", "設定RFPIO Timeout");
                    }
                }

                if (localData.MIPCData.GetDataByMIPCTagName(RFPIOCmd) == 2)
                {
                    mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { PIOSelect }, new List<float> { 0 });
                    setRFPIOTimer.Restart();
                    while (localData.MIPCData.GetDataByMIPCTagName(go) != 1)
                    {
                        if (setRFPIOTimer.ElapsedMilliseconds >= 2000)
                        {
                            WriteLog(3, "", "RFPIO GO Timeout");
                            return;
                        }
                    }
                    WriteLog(7, "", "設定RFPIO 成功");
                }


            }
        } 
    }



}


