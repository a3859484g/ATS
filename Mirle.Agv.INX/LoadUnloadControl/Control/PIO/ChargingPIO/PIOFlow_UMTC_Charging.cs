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
    public class PIOFlow_UMTC_Charging : PIOFlow_Charging
    {
        private string chargingCommand = "PIO-out01";

        private string go = "PIO-in00";
        private string enable = "PIO-in01";
        private string warning = "PIO-in02";
        private string requestForFreeCommand = "PIO-in03";
        private string op = "PIO-in04";
        private string hp = "PIO-in05";
        private string error = "PIO-in06";
        private string fullCharging = "PIO-in07";
        
        private Thread thread = null;
        
        private double delayTime = 0;

        private bool allTagInMIPCConfig = true;

        private double sendCommandChargingNotOnTimeout = 20000;

        private double sendCommandStopChargingNotOffTimeout = 20000;

        private bool logMode = true;

        public override void Initial(AlarmHandler alarmHandler, MIPCControlHandler mipcControl, string pioName, string pioDirection, string normalLogName)
        {
            this.normalLogName = normalLogName;
            ConfirmSensor = String.Concat("ConfirmSensor", pioDirection);
            ChargingSaftey = String.Concat("ChargingSaftey", pioDirection);
            this.alarmHandler = alarmHandler;
            PIOName = pioName;
            this.mipcControl = mipcControl;
            device = MethodInfo.GetCurrentMethod().ReflectedType.Name;

            ConfirmSensor = String.Concat(ConfirmSensor, pioDirection);
            ChargingSaftey = String.Concat(ChargingSaftey, pioDirection);

            #region output (String + pioDirection)
            chargingCommand = String.Concat(chargingCommand, pioDirection);

            PIOOutputTagList.Add(chargingCommand);
            PIOOutputNameList.Add("ChargingCommand");
            #endregion

            #region input (String + pioDirection)
            // Output Name = String.Concat(Output Name, pioDirection);
            go = String.Concat(go, pioDirection);
            enable = String.Concat(enable, pioDirection);
            warning = String.Concat(warning, pioDirection);
            requestForFreeCommand = String.Concat(requestForFreeCommand, pioDirection);
            op = String.Concat(op, pioDirection);
            hp = String.Concat(hp, pioDirection);
            error = String.Concat(error, pioDirection);
            fullCharging = String.Concat(fullCharging, pioDirection);

            PIOInputTagList.Add(go);
            PIOInputNameList.Add("GO");
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
            allIOData.Add(go, new DataDelayAndChange(delayTime, EnumDelayType.OnDelay));
            allIOData.Add(enable, new DataDelayAndChange(delayTime, EnumDelayType.OnDelay));
            allIOData.Add(warning, new DataDelayAndChange(delayTime, EnumDelayType.OnDelay));
            allIOData.Add(requestForFreeCommand, new DataDelayAndChange(delayTime, EnumDelayType.OnDelay));
            allIOData.Add(op, new DataDelayAndChange(delayTime, EnumDelayType.OnDelay));
            allIOData.Add(hp, new DataDelayAndChange(delayTime, EnumDelayType.OnDelay));
            allIOData.Add(error, new DataDelayAndChange(delayTime, EnumDelayType.OnDelay));
            allIOData.Add(fullCharging, new DataDelayAndChange(delayTime, EnumDelayType.OnDelay));

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
        }

        public override void StartPIO()
        {
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

            if (thread == null || !thread.IsAlive)
            {
                Charging = true;
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
            if (localData.BatteryInfo.Battery_SOC >= localData.BatteryConfig.HighBattery_SOC ||
                localData.BatteryInfo.Battery_V >= localData.BatteryConfig.HighBattery_Voltage)
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
            if (localData.MIPCData.SafetySensorStatus == EnumSafetyLevel.IPCEMO ||
                localData.MIPCData.SafetySensorStatus == EnumSafetyLevel.EMO)
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
                SetAlarmCode(EnumLoadUnloadControlErrorCode.Charging溫度過高);
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
                                            CheckChargingA();
                                            CheckAlarm();
                                            CheckWarning();
                                            CheckRequestForFreeCommand();
                                            CheckFullCharging();
                                            CheckBatteryFullCharging();
                                            CheckStopCharging();
                                            CheckSafetySensorStatus();
                                            Check溫度();
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
                                            CheckChargingA();
                                            CheckAlarm();
                                            CheckWarning();
                                            CheckRequestForFreeCommand();
                                            CheckFullCharging();
                                            CheckBatteryFullCharging();
                                            CheckStopCharging();
                                            CheckOPOn();
                                            CheckSafetySensorStatus();
                                            Check溫度();
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
                                        CheckChargingA();
                                        CheckAlarm();
                                        CheckWarning();
                                        CheckRequestForFreeCommand();
                                        CheckFullCharging();
                                        CheckBatteryFullCharging();
                                        CheckStopCharging();
                                        CheckOPOn();
                                        CheckSafetySensorStatus();
                                        Check溫度();
                                    }
                                    break;

                                case EnumChargingStatus.SendStopCharging:
                                    mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { ChargingSaftey, chargingCommand }, new List<float> { 0, 0 });
                                    ChargingStep = EnumChargingStatus.WaitHPOn;
                                    timer.Restart();
                                    break;

                                case EnumChargingStatus.WaitHPOn:
                                    if (allIOData[hp].Data)
                                        ChargingStep = EnumChargingStatus.Idle;

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

        public override void PIOTestOnOff(string tag, bool onOff)
        {
            if (localData.AutoManual == EnumAutoState.Manual && !Charging)
            {
                if (tag != chargingCommand)
                {
                    if (onOff)
                        mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { tag }, new List<float>() { 1 });
                    else
                        mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { tag }, new List<float>() { 0 });
                }
            }
        }

        public override bool GetPIOStatueByTag(string tag)
        {
            return (float)localData.MIPCData.GetDataByMIPCTagName(tag) == 1;
        }
    }
}
