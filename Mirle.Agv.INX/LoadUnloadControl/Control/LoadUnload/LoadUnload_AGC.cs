using Mirle.Agv.INX.Controller;
using Mirle.Agv.INX.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Control
{
    public class LoadUnload_AGC : LoadUnload
    {
        private PIOFlow_AGC_LoadUnload rightPIO = null;

        private string rollerSpeedTag = "AGC_RollerSpeed";
        private string rollerNagTag = "AGC_RollerNag";
        private string rollerStartTag = "AGC_RollerStart";

        private string cv_IN = "CV-入料";
        private string cv_ChangeLowSpeed = "CV-減速";
        private string cv_Loading = "CV-在席";
        private string cv_Stop = "CV-停止";

        public DataDelayAndChange CV_IN = new DataDelayAndChange(0, EnumDelayType.OnDelay);
        public DataDelayAndChange CV_ChangeLowSpeed = new DataDelayAndChange(0, EnumDelayType.OnDelay);
        public DataDelayAndChange CV_Loading = new DataDelayAndChange(0, EnumDelayType.OnDelay);
        public DataDelayAndChange CV_Stop = new DataDelayAndChange(0, EnumDelayType.OnDelay);

        private bool mipcTagNameOK = false;

        private double speedHigh = 100;
        private double cvLowSpeed = 20;

        BarcodeReader_Datalogic BarcodeReader_Datalogic = new BarcodeReader_Datalogic();

        public override void Initial(MIPCControlHandler mipcControl, AlarmHandler alarmHandler)
        {
            this.alarmHandler = alarmHandler;
            this.mipcControl = mipcControl;
            device = MethodInfo.GetCurrentMethod().ReflectedType.Name;

            RightPIO = new PIOFlow_AGC_LoadUnload();
            RightPIO.Initial(alarmHandler, mipcControl, "Right", "", normalLogName);
            rightPIO = (PIOFlow_AGC_LoadUnload)RightPIO;
            CanRightLoadUnload = true;

            if (localData.MIPCData.AllDataByMIPCTagName.ContainsKey(rollerSpeedTag) &&
                localData.MIPCData.AllDataByMIPCTagName.ContainsKey(rollerNagTag) &&
                localData.MIPCData.AllDataByMIPCTagName.ContainsKey(rollerStartTag))
                mipcTagNameOK = true;
            else
            {
                WriteLog(3, "", String.Concat("必須要有Tag : ", rollerSpeedTag, ", ", rollerNagTag, ", ", rollerStartTag));
            }

            if (!localData.SimulateMode)
            {
                string result = "";
                BarcodeReader_Datalogic.Connect("192.168.29.216:51236", ref result);
                BarcodeReader_Datalogic.ReadBarcode(ref result, 1000, ref result);
            }
        }

        private void SendTimeout()
        {
            switch (rightPIO.Timeout)
            {
                case EnumPIOStatus.T1:
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.T1_Timeout);
                    break;
                case EnumPIOStatus.T2:
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.T2_Timeout);
                    break;
                case EnumPIOStatus.T3:
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.T3_Timeout);
                    break;
                case EnumPIOStatus.T4:
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.T4_Timeout);
                    break;
                case EnumPIOStatus.T5:
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.T5_Timeout);
                    break;
                case EnumPIOStatus.T6:
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.T6_Timeout);
                    break;
                case EnumPIOStatus.T7:
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.T7_Timeout);
                    break;
                case EnumPIOStatus.T8:
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.T8_Timeout);
                    break;
                case EnumPIOStatus.T9:
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.T9_Timeout);
                    break;
                case EnumPIOStatus.T10:
                    SetAlarmCode(EnumLoadUnloadControlErrorCode.T10_Timeout);
                    break;
                default:
                    break;
            }

            RollerStop();
            localData.LoadUnloadData.LoadUnloadCommand.CommandResult = EnumLoadUnloadComplete.Error;
        }

        private void CV_SensorErrorSendAlarmCode()
        {
            WriteLog(3, "", "取貨 未動作前CV檢知on 異常結束");
            rightPIO.StopPIO();

            SetAlarmCode(EnumLoadUnloadControlErrorCode.取放貨中CV檢知異常);
            RollerStop();
            localData.LoadUnloadData.LoadUnloadCommand.CommandResult = EnumLoadUnloadComplete.Error;
        }

        private void LoadUnloadEMS()
        {
            WriteLog(3, "", "取貨 未動作前CV檢知on 異常結束");
            rightPIO.StopPIO();

            SetAlarmCode(EnumLoadUnloadControlErrorCode.取放貨中EMS);
            RollerStop();
            localData.LoadUnloadData.LoadUnloadCommand.CommandResult = EnumLoadUnloadComplete.Error;
        }

        public override void LoadUnloadStart()
        {
            if (!mipcTagNameOK)
            {
                localData.LoadUnloadData.LoadUnloadCommand.CommandResult = EnumLoadUnloadComplete.Error;
                return;
            }

            rightPIO.ResetPIO();

            switch (localData.LoadUnloadData.LoadUnloadCommand.Action)
            {
                case EnumLoadUnload.Load:
                    rightPIO.PIOFlow_Load_UnLoad(EnumLoadUnload.Load);
                    while (rightPIO.Status != EnumPIOStatus.T2)
                    {
                        if (localData.LoadUnloadData.LoadUnloadCommand.StopRequest)
                        {
                            LoadUnloadEMS();
                            return;
                        }

                        if (rightPIO.Status == EnumPIOStatus.NG)
                        {
                            SendTimeout();
                            return;
                        }

                        UpdateForkHomeStatus();

                        if (CV_IN.Data || CV_Loading.Data || CV_ChangeLowSpeed.Data || CV_Stop.Data)
                        {
                            CV_SensorErrorSendAlarmCode();
                            return;
                        }

                        Thread.Sleep(sleepTime);
                    }

                    RollerStart(true, speedHigh);
                    rightPIO.res_Permit = true;

                    //localData.LoadUnloadData.LoadUnloadCommand.StatusStep = 0;
                    WriteLog(7, "", "等待入帳");

                    while (rightPIO.Status != EnumPIOStatus.NG)
                    {
                        if (localData.LoadUnloadData.LoadUnloadCommand.StopRequest)
                        {
                            LoadUnloadEMS();
                            return;
                        }

                        UpdateForkHomeStatus();

                        if (!localData.LoadUnloadData.LoadUnloadCommand.IsLowspeed)
                        {
                            if (CV_Loading.Data)
                            {
                                WriteLog(7, "", "Roller降速");
                                RollerStart(true, cvLowSpeed);
                                localData.LoadUnloadData.LoadUnloadCommand.IsLowspeed = true;
                            }
                        }

                        if (CV_Stop.Data && CV_Loading.Data && CV_ChangeLowSpeed.Data)
                        {
                            WriteLog(7, "", "Roller Stop");
                            RollerStop();
                            WriteLog(7, "", "SendFinish");
                            rightPIO.res_Finish = true;
                            break;
                        }

                        Thread.Sleep(sleepTime);
                    }
                    while (rightPIO.Status != EnumPIOStatus.NG)
                    {
                        if (rightPIO.Status == EnumPIOStatus.Complete)
                        {
                            rightPIO.res_Permit = false;
                            rightPIO.res_Finish = false;
                            localData.LoadUnloadData.LoadUnloadCommand.CommandResult = EnumLoadUnloadComplete.End;
                            return;
                        }
                        Thread.Sleep(sleepTime);
                    }
                    RollerStop();
                    SendTimeout();

                    break;
                case EnumLoadUnload.Unload:
                    rightPIO.PIOFlow_Load_UnLoad(EnumLoadUnload.Unload);

                    while (rightPIO.Status != EnumPIOStatus.T3)
                    {
                        if (localData.LoadUnloadData.LoadUnloadCommand.StopRequest)
                        {
                            LoadUnloadEMS();
                            return;
                        }

                        if (rightPIO.Status == EnumPIOStatus.NG)
                        {
                            SendTimeout();
                            return;
                        }

                        Thread.Sleep(sleepTime);
                    }

                    RollerStart(false, speedHigh);

                    //localData.LoadUnloadData.LoadUnloadCommand.StatusStep = 0;
                    WriteLog(7, "", "等待Stop Off");
                    while (rightPIO.Status != EnumPIOStatus.NG)
                    {
                        if (localData.LoadUnloadData.LoadUnloadCommand.StopRequest)
                        {
                            LoadUnloadEMS();
                            return;
                        }

                        UpdateForkHomeStatus();

                        if (rightPIO.Status == EnumPIOStatus.Complete)
                        {
                            WriteLog(7, "", "Roller Stop");
                            RollerStop();

                            if (CV_IN.Data || CV_Loading.Data || CV_ChangeLowSpeed.Data || CV_Stop.Data)
                            {
                                WriteLog(7, "", "CV_SensorError");
                                CV_SensorErrorSendAlarmCode();
                                return;
                            }
                            WriteLog(7, "", "SendFinish");
                            localData.LoadUnloadData.LoadUnloadCommand.CommandResult = EnumLoadUnloadComplete.End;
                            return;
                        }
                        
                        Thread.Sleep(sleepTime);
                    }

                    RollerStop();
                    SendTimeout();

                    break;
                default:
                    WriteLog(3, "", "Comaand Not Load or Unload");
                    localData.LoadUnloadData.LoadUnloadCommand.CommandResult = EnumLoadUnloadComplete.Error;
                    return;
            }
        }

        private bool RollerStart(bool dir, double speed)
        {
            if (speed < 0)
                speed = 1;
            else if (speed > 100)
                speed = 100;

            float float_Speed = (float)(speed / 10);

            return mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { rollerSpeedTag, rollerNagTag, rollerStartTag }, new List<float>() { float_Speed, (dir ? 0 : 1), 1 });
        }

        private bool RollerStop()
        {
            return mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { rollerStartTag }, new List<float>() { 0 });
        }

        public override bool ClearCommand()
        {
            LoadUnloadCommandData temp = localData.LoadUnloadData.LoadUnloadCommand;

            if (temp != null)
            {
                if (temp.CommandResult != EnumLoadUnloadComplete.None)
                {
                    WriteLog(7, "", "清除LoadUnloadCommand");
                    localData.LoadUnloadData.LoadUnloadCommand = null;
                }
            }

            return true;
        }

        public override void CheckAlingmentValue(EnumStageDirection direction, int stageNumber)
        {
        }

        //public override bool Jog()
        //{
        //    return false;
        //}

        public override void UpdateLoadingAndCSTID()
        {
            CV_Loading.Data = (localData.MIPCData.GetDataByMIPCTagName(cv_Loading) == 0);

            if (CV_Loading.Change)
                WriteLog(7, "", String.Concat(cv_Loading, " Change to ", (CV_Loading.data ? "on" : "off")));

            localData.LoadUnloadData.Loading = CV_Loading.Data;
            localData.LoadUnloadData.CstID = "";
        }

        public override void UpdateForkHomeStatus()
        {
            CV_IN.Data = (localData.MIPCData.GetDataByMIPCTagName(cv_IN) == 0);

            if (CV_IN.Change)
                WriteLog(7, "", String.Concat(cv_IN, " Change to ", (CV_IN.data ? "on" : "off")));

            CV_ChangeLowSpeed.Data = (localData.MIPCData.GetDataByMIPCTagName(cv_ChangeLowSpeed) == 0);

            if (CV_ChangeLowSpeed.Change)
                WriteLog(7, "", String.Concat(cv_ChangeLowSpeed, " Change to ", (CV_ChangeLowSpeed.data ? "on" : "off")));

            CV_Loading.Data = (localData.MIPCData.GetDataByMIPCTagName(cv_Loading) == 0);

            if (CV_Loading.Change)
                WriteLog(7, "", String.Concat(cv_Loading, " Change to ", (CV_Loading.data ? "on" : "off")));

            CV_Stop.Data = (localData.MIPCData.GetDataByMIPCTagName(cv_Stop) == 0);

            if (CV_Stop.Change)
                WriteLog(7, "", String.Concat(cv_Stop, " Change to ", (CV_Stop.data ? "on" : "off")));

            localData.LoadUnloadData.Loading = CV_Loading.Data;
            localData.LoadUnloadData.ForkHome = true;
        }
    }
}
