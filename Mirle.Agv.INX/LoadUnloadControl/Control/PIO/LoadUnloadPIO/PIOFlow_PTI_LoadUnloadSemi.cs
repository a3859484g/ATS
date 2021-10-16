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
    public class PIOFlow_PTI_LoadUnloadSemi : PIOFlow
    {
        private string Select = ("Select(EQ)");
        private string VALID = ("VALID(EQ)");
        private string CS_0 = ("CS_0(EQ)");
        private string CS_1 = ("CS_1(EQ)");
        private string AM_AVBL = ("AM_AVBL(EQ)");
        private string TR_REQ = ("TR_REQ(EQ)");
        private string BUSY = ("BUSY(EQ)");
        private string COMPT = ("COMPT(EQ)");
        private string CONT = ("CONT(EQ)");

        private string GO = ("GO(EQ)");
        private string L_REQ = ("L_REQ(EQ)");
        private string U_REQ = ("U_REQ(EQ)");
        private string VA = ("VA(EQ)");
        private string READY = ("READY(EQ)");
        private string VS_0 = ("VS_0(EQ)");
        private string VS_1 = ("VS_1(EQ)");
        private string HO_AVBL = ("HO_AVBL(EQ)");
        private string ES = ("ES(EQ)");

        //private string VALID    = ("VALID _R(EQ)");
        //private string CS_0      = ("CS_0 _R(EQ)");
        //private string CS_1       = ("CS_1_R(EQ)");
        //private string AM_AVBL = ("AM_AVBL_R(EQ)");
        //private string TR_REQ   = ("TR_REQ_R(EQ)");
        //private string BUSY      = ("BUSY _R(EQ)");
        //private string COMPT     = ("COMPT_R(EQ)");
        //private string CONT      = ("CONT _R(EQ)");

        //private string GO          = ("GO _R(EQ)");
        //private string L_REQ    = ("L_REQ _R(EQ)");
        //private string U_REQ    = ("U_REQ _R(EQ)");
        //private string VA          = ("VA _R(EQ)");
        //private string READY     = ("READY_R(EQ)");
        //private string VS_0      = ("VS_0 _R(EQ)");
        //private string VS_1      = ("VS_1 _R(EQ)");
        //private string HO_AVBL = ("HO_AVBL_R(EQ)");
        //private string ES          = ("ES _R(EQ)");

        public bool SendBusy = false;
        public bool SendComplete = false;

        private Stopwatch PIO_Timer = new Stopwatch();
        private PIO_Cantops PIO_Device= null;

        public override void Initial(AlarmHandler alarmHandler, MIPCControlHandler mipcControl, string pioName, string pioDirection, string normalLogName)
        {
            ConnectPIODevice(pioDirection);
            this.normalLogName = normalLogName;

            int sensorDelayTime = 0;

            if (localData.LoadUnloadData.PIOTimeoutList.Count != 8)
                localData.LoadUnloadData.PIOTimeoutList = new List<double>() { 60000, 20000, 20000, 20000, 500000, 500000, 20000, 20000 };

            if (localData.LoadUnloadData.PIOTimeoutTageList.Count != 8 ||
                !localData.LoadUnloadData.PIOTimeoutTageList.Contains(EnumPIOStatus.TA1) ||
                !localData.LoadUnloadData.PIOTimeoutTageList.Contains(EnumPIOStatus.TA2) ||
                !localData.LoadUnloadData.PIOTimeoutTageList.Contains(EnumPIOStatus.TA3) ||
                !localData.LoadUnloadData.PIOTimeoutTageList.Contains(EnumPIOStatus.TP1) ||
                !localData.LoadUnloadData.PIOTimeoutTageList.Contains(EnumPIOStatus.TP2) ||
                !localData.LoadUnloadData.PIOTimeoutTageList.Contains(EnumPIOStatus.TP3) ||
                !localData.LoadUnloadData.PIOTimeoutTageList.Contains(EnumPIOStatus.TP4) ||
                !localData.LoadUnloadData.PIOTimeoutTageList.Contains(EnumPIOStatus.TP5))
                localData.LoadUnloadData.PIOTimeoutTageList = new List<EnumPIOStatus>() {
                                               EnumPIOStatus.TA1, EnumPIOStatus.TP1,
                                               EnumPIOStatus.TA2, EnumPIOStatus.TP2,
                                               EnumPIOStatus.TP3, EnumPIOStatus.TP4,
                                               EnumPIOStatus.TA3, EnumPIOStatus.TP5};

            localData.LoadUnloadData.PIOStatusToFlowIndex.Add(EnumPIOStatus.TA1, 0);
            localData.LoadUnloadData.PIOStatusToFlowIndex.Add(EnumPIOStatus.TP1, 1);
            localData.LoadUnloadData.PIOStatusToFlowIndex.Add(EnumPIOStatus.TA2, 2);
            localData.LoadUnloadData.PIOStatusToFlowIndex.Add(EnumPIOStatus.TP2, 3);
            localData.LoadUnloadData.PIOStatusToFlowIndex.Add(EnumPIOStatus.TP3, 4);
            localData.LoadUnloadData.PIOStatusToFlowIndex.Add(EnumPIOStatus.TP4, 5);
            localData.LoadUnloadData.PIOStatusToFlowIndex.Add(EnumPIOStatus.TA3, 6);
            localData.LoadUnloadData.PIOStatusToFlowIndex.Add(EnumPIOStatus.TP5, 7);

            this.alarmHandler = alarmHandler;
            PIOName = pioName;
            this.mipcControl = mipcControl;
            device = MethodInfo.GetCurrentMethod().ReflectedType.Name;

            #region output (String + pioDirection)
            // Input Name = String.Concat(Input Name, pioDirection);
            GO = String.Concat(GO, pioDirection);
            VALID = String.Concat(VALID, pioDirection);
            CS_0 = String.Concat(CS_0, pioDirection);
            CS_1 = String.Concat(CS_1, pioDirection);
            AM_AVBL = String.Concat(AM_AVBL, pioDirection);
            TR_REQ = String.Concat(TR_REQ, pioDirection);
            BUSY = String.Concat(BUSY, pioDirection);
            COMPT = String.Concat(COMPT, pioDirection);
            CONT = String.Concat(CONT, pioDirection);

            allIOData.Add(Select, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));
            allIOData.Add(VALID, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));
            allIOData.Add(CS_0, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));
            allIOData.Add(CS_1, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));
            allIOData.Add(AM_AVBL, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));
            allIOData.Add(TR_REQ, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));
            allIOData.Add(BUSY, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));
            allIOData.Add(COMPT, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));
            allIOData.Add(CONT, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));

            PIOOutputTagList.AddRange(new string[] { Select,VALID, CS_0, CS_1, AM_AVBL, TR_REQ, BUSY, COMPT, CONT });
            PIOOutputNameList.AddRange(new string[] {"Select", "VALID", "CS_0", "CS_1", "AM_AVBL", "TR_REQ", "BUSY", "COMPT", "CONT" });
            #endregion

            #region input (String + pioDirection)
            // Output Name = String.Concat(Output Name, pioDirection);
            L_REQ = String.Concat(L_REQ, pioDirection);
            U_REQ = String.Concat(U_REQ, pioDirection);
            VA = String.Concat(VA, pioDirection);
            READY = String.Concat(READY, pioDirection);
            VS_0 = String.Concat(VS_0, pioDirection);
            VS_1 = String.Concat(VS_1, pioDirection);
            HO_AVBL = String.Concat(HO_AVBL, pioDirection);
            ES = String.Concat(ES, pioDirection);

            allIOData.Add(GO, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));
            allIOData.Add(L_REQ, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));
            allIOData.Add(U_REQ, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));
            allIOData.Add(VA, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));
            allIOData.Add(READY, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));
            allIOData.Add(VS_0, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));
            allIOData.Add(VS_1, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));
            allIOData.Add(HO_AVBL, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));
            allIOData.Add(ES, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));

            PIOInputTagList.AddRange(new string[] { GO, L_REQ, U_REQ, VA, READY, VS_0, VS_1, HO_AVBL, ES });
            PIOInputNameList.AddRange(new string[] { "GO", "L_REQ", "U_REQ", "VA", "READY", "VS_0", "VS_1", "HO_AVBL", "ES" });
            #endregion
        }
        #region
        private bool ConnectPIODevice(string pioDirection)
        {
            int ComPort = 1, BaudRate = 38400;
            PIO_Device = new PIO_Cantops();
            if (PIO_Device.Fun_Rs232_OpenPort(ComPort, BaudRate))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion
        public override bool CanStartPIO
        {
            get
            {
                return localData.MIPCData.GetDataByMIPCTagName(HO_AVBL) != 0 &&
                       localData.MIPCData.GetDataByMIPCTagName(ES) != 0;
            }
        }

        public override void PIOFlow_Load_UnLoad(EnumLoadUnload Load_UnLoad)
        {
            if (pioFlowThread == null || !pioFlowThread.IsAlive)
            {
                loadUnload = Load_UnLoad;
                pioFlowThread = new Thread(PIOFlow_Load_UnLoadThread);
                pioFlowThread.Start();
            }
            else
                WriteLog(5, "", "pioFlowThread isAlive, Error");
        }

        public override void StopPIO()
        {
            mipcControl.SendMIPCDataByMIPCTagName(
                                  new List<string>() { VALID, CS_0, CS_1, TR_REQ, BUSY, COMPT, CONT, AM_AVBL },
                                  new List<float>() { 0, 0, 0, 0, 0, 0, 0, 0 });
        }

        private bool SendAllOff()
        {
            return mipcControl.SendMIPCDataByMIPCTagName(
                                  new List<string>() { VALID, CS_0, CS_1, TR_REQ, BUSY, COMPT, CONT, AM_AVBL },
                                  new List<float>() { 0, 0, 0, 0, 0, 0, 0, 0 });
        }

        private void UpdatePIO()
        {
            for (int i = 0; i < PIOInputTagList.Count; i++)
            {
                allIOData[PIOInputTagList[i]].Data = localData.MIPCData.GetDataByMIPCTagName(PIOInputTagList[i]) != 0;

                if (allIOData[PIOInputTagList[i]].Change)
                    WriteLog(7, "", String.Concat(PIOInputNameList[i], " Change to ", (allIOData[PIOInputTagList[i]].data ? "On" : "Off")));
            }

            for (int i = 0; i < PIOOutputTagList.Count; i++)
            {
                allIOData[PIOOutputTagList[i]].Data = localData.MIPCData.GetDataByMIPCTagName(PIOOutputTagList[i]) != 0;

                if (allIOData[PIOOutputTagList[i]].Change)
                    WriteLog(7, "", String.Concat(PIOOutputNameList[i], " Change to ", (allIOData[PIOOutputTagList[i]].data ? "On" : "Off")));
            }
        }

        private EnumLoadUnload loadUnload = EnumLoadUnload.Load;

        public void PIO_SerChannel_ID(string Address)
        {
            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { Select }, new List<float>() { 1 });
            string Channel = "";
            string ID = "";
            Channel = localData.TheMapInfo.AllAddress[Address].RFPIODeviceID;
            ID = localData.TheMapInfo.AllAddress[Address].RFPIOChannelID;
            PIO_Device.Fun_RFPIO_SetCH_ID(Channel, ID);
            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { Select }, new List<float>() { 0 });

        }


        public void PIO_Close()
        {
            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { Select }, new List<float>() { 1 });
            Thread.Sleep(1000);
            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { Select }, new List<float>() { 0 });

        }
        public void PIOFlow_Load_UnLoadThread()
        {
            WriteLog(7, "", String.Concat("PIO Start : ", loadUnload.ToString()));
            Timeout = EnumPIOStatus.None;
            PIO_Timer.Restart();
            EnumPIOStatus lastStatus = EnumPIOStatus.Idle;

            while (Status != EnumPIOStatus.Complete && Status != EnumPIOStatus.NG)
            {
                UpdatePIO();

                switch (Status)
                {
                    case EnumPIOStatus.Idle:
                        #region 發送 CS_0/CS_1 & VALID On.
                        if (mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { CS_0 }, new List<float>() { 1 }) &&
                            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { VALID }, new List<float>() { 1 }))
                        {
                            Status = EnumPIOStatus.TA1;
                            PIO_Timer.Restart();
                        }
                        else
                            WriteLog(5, "", String.Concat(Status.ToString(), "Send CS & VALID mipc傳送失敗"));
                        #endregion
                        break;
                    case EnumPIOStatus.TA1:
                        #region 等待 L_REQ/U_REQ On # TA1 Timeout.
                        //if (!allIOData[HO_AVBL].Data)
                        //{
                        //    Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                        //    Status = EnumPIOStatus.Error;
                        //    WriteLog(5, "", "HO_AVBL Off");
                        ////}
                        ///*else*/ if (!allIOData[ES].Data)
                        //{
                        //    Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                        //    Status = EnumPIOStatus.Error;
                        //    WriteLog(5, "", "ES Off");
                        //}
                   /*     else*/ if (PIO_Timer.ElapsedMilliseconds > localData.LoadUnloadData.PIOTimeoutList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]])
                        {
                            Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                            Status = EnumPIOStatus.Error;
                            WriteLog(5, "", String.Concat(Timeout.ToString(), " timeout "));
                        }
                        else if (allIOData[L_REQ].Data || allIOData[U_REQ].Data)
                        {
                            bool dataOK = true;
                            switch (loadUnload)
                            {
                                case EnumLoadUnload.Load:
                                    if (allIOData[L_REQ].Data)
                                        dataOK = false;
                                    break;
                                case EnumLoadUnload.Unload:
                                    if (allIOData[U_REQ].Data)
                                        dataOK = false;
                                    break;
                                default:
                                    break;
                            }

                            if (dataOK)
                            {
                                Status = EnumPIOStatus.TP1;
                                PIO_Timer.Restart();
                            }
                            else
                            {
                                Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                                Status = EnumPIOStatus.Error;
                                WriteLog(5, "", String.Concat("L_REQ/U_REQ 和命令不相符, Action = ", loadUnload.ToString(),
                                                              ", L_REQ = ", allIOData[L_REQ].Data.ToString(),
                                                              ", U_REQ = ", allIOData[U_REQ].Data.ToString()));
                            }
                        }
                        #endregion
                        break;
                    case EnumPIOStatus.TP1:
                        #region 發送 TR_REQ on # TP1 Timeout(加減用,避免永遠送不出去卡死問題).
                        if (!allIOData[HO_AVBL].Data)
                        {
                            Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                            Status = EnumPIOStatus.Error;
                            WriteLog(5, "", "HO_AVBL Off");
                        }
                        else if (!allIOData[ES].Data)
                        {
                            Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                            Status = EnumPIOStatus.Error;
                            WriteLog(5, "", "ES Off");
                        }
                        else if ((!allIOData[L_REQ].Data && !allIOData[U_REQ].Data))
                        {
                            Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                            Status = EnumPIOStatus.Error;
                            WriteLog(5, "", "input Sensor Off");
                        }
                        else if (PIO_Timer.ElapsedMilliseconds > localData.LoadUnloadData.PIOTimeoutList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]])
                        {
                            Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                            Status = EnumPIOStatus.Error;
                            WriteLog(5, "", String.Concat(Timeout.ToString(), " timeout "));
                        }
                        else if (!localData.LoadUnloadData.NotSendTR_REQ &&
                            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { TR_REQ }, new List<float>() { 1 }))
                        {
                            Status = EnumPIOStatus.TA2;
                            PIO_Timer.Restart();
                        }
                        #endregion
                        break;
                    case EnumPIOStatus.TA2:
                        #region 等待 Ready On # TA2 Timeout.
                        if (!allIOData[HO_AVBL].Data)
                        {
                            Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                            Status = EnumPIOStatus.Error;
                            WriteLog(5, "", "HO_AVBL Off");
                        }
                        else if (!allIOData[ES].Data)
                        {
                            Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                            Status = EnumPIOStatus.Error;
                            WriteLog(5, "", "ES Off");
                        }
                        else if ((!allIOData[L_REQ].Data && !allIOData[U_REQ].Data))
                        {
                            Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                            Status = EnumPIOStatus.Error;
                            WriteLog(5, "", "input Sensor Off");
                        }
                        else if (PIO_Timer.ElapsedMilliseconds > localData.LoadUnloadData.PIOTimeoutList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]])
                        {
                            Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                            Status = EnumPIOStatus.Error;
                            WriteLog(5, "", String.Concat(Timeout.ToString(), " timeout "));
                        }
                        else if (allIOData[READY].Data)
                        {
                            SendBusy = false;
                            Status = EnumPIOStatus.TP2;
                            PIO_Timer.Restart();
                        }
                        #endregion
                        break;
                    case EnumPIOStatus.TP2:
                        #region 等待LoadUnload允許發Busy & 發送Busy On # TP2 Timeout.
                        if (!allIOData[HO_AVBL].Data)
                        {
                            Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                            Status = EnumPIOStatus.Error;
                            WriteLog(5, "", "HO_AVBL Off");
                        }
                        else if (!allIOData[ES].Data)
                        {
                            Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                            Status = EnumPIOStatus.Error;
                            WriteLog(5, "", "ES Off");
                        }
                        else if ((!allIOData[L_REQ].Data && !allIOData[U_REQ].Data) || !allIOData[READY].Data)
                        {
                            Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                            Status = EnumPIOStatus.Error;
                            WriteLog(5, "", "input Sensor Off");
                        }
                        //else if (PIO_Timer.ElapsedMilliseconds > localData.LoadUnloadData.PIOTimeoutList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]])
                        //{
                        //    Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                        //    Status = EnumPIOStatus.Error;
                        //    WriteLog(5, "", String.Concat(Timeout.ToString(), " timeout "));
                        //}
                        else if (!localData.LoadUnloadData.NotSendBUSY && SendBusy &&
                             mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { BUSY }, new List<float>() { 1 }))
                        {
                            Status = EnumPIOStatus.TP3;
                            PIO_Timer.Restart();
                        }
                        #endregion
                        break;
                    case EnumPIOStatus.TP3:
                        #region 等待對方L_REQ/U_REQ Off # T4Timeout.
                        if (!allIOData[HO_AVBL].Data)
                        {
                            Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                            Status = EnumPIOStatus.Error;
                            WriteLog(5, "", "HO_AVBL Off");
                        }
                        else if (!allIOData[ES].Data)
                        {
                            Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                            Status = EnumPIOStatus.Error;
                            WriteLog(5, "", "ES Off");
                        }
                        else if (!allIOData[READY].Data)
                        {
                            Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                            Status = EnumPIOStatus.Error;
                            WriteLog(5, "", "input Sensor Off");
                        }
                        else if (PIO_Timer.ElapsedMilliseconds > localData.LoadUnloadData.PIOTimeoutList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]])
                        {
                            Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                            Status = EnumPIOStatus.Error;
                            WriteLog(5, "", String.Concat(Timeout.ToString(), " timeout "));
                        }
                        else if (!allIOData[L_REQ].Data && !allIOData[U_REQ].Data)
                        {
                            SendComplete = false;
                            Status = EnumPIOStatus.TP4;
                            PIO_Timer.Restart();
                        }
                        #endregion
                        break;
                    case EnumPIOStatus.TP4:
                        #region 等待LoadUnload允許發Complete & 發送 BUSY Off / COMPT On / TR_REQ Off # TP4 Timeout.
                        if (!allIOData[HO_AVBL].Data)
                        {
                            Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                            Status = EnumPIOStatus.Error;
                            WriteLog(5, "", "HO_AVBL Off");
                        }
                        else if (!allIOData[ES].Data)
                        {
                            Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                            Status = EnumPIOStatus.Error;
                            WriteLog(5, "", "ES Off");
                        }
                        else if (!allIOData[READY].Data)
                        {
                            Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                            Status = EnumPIOStatus.Error;
                            WriteLog(5, "", "input Sensor Off");
                        }
                        else if (PIO_Timer.ElapsedMilliseconds > localData.LoadUnloadData.PIOTimeoutList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]])
                        {
                            Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                            Status = EnumPIOStatus.Error;
                            WriteLog(5, "", String.Concat(Timeout.ToString(), " timeout "));
                        }
                        else if (!localData.LoadUnloadData.NotSendCOMPT && SendComplete &&
                             mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { BUSY }, new List<float>() { 0 }) &&
                             mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { TR_REQ }, new List<float>() { 0 }) &&
                             mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { COMPT }, new List<float>() { 1 }))
                        {
                            Status = EnumPIOStatus.TA3;
                            PIO_Timer.Restart();
                        }
                        #endregion
                        break;
                    case EnumPIOStatus.TA3:
                        #region 等待對方READY Off # TA3 Timeout.
                        //if (!allIOData[HO_AVBL].Data)
                        //{
                        //    Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                        //    Status = EnumPIOStatus.Error;
                        //    WriteLog(5, "", "HO_AVBL Off");
                        //}
                        //else if (!allIOData[ES].Data)
                        //{
                        //    Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                        //    Status = EnumPIOStatus.Error;
                        //    WriteLog(5, "", "ES Off");
                        //}
                        //else
                        if (PIO_Timer.ElapsedMilliseconds > localData.LoadUnloadData.PIOTimeoutList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]])
                        {
                            Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                            Status = EnumPIOStatus.Error;
                            WriteLog(5, "", String.Concat(Timeout.ToString(), " timeout "));
                        }
                        else if (!allIOData[READY].Data)
                        {
                            Status = EnumPIOStatus.TP5;
                            PIO_Timer.Restart();
                        }
                        #endregion
                        break;
                    case EnumPIOStatus.TP5:
                        #region 發送 COMPT Off/CS Off/VALID Off # T6Timeout.
                        //if (!allIOData[HO_AVBL].Data)
                        //{
                        //    Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                        //    Status = EnumPIOStatus.Error;
                        //    WriteLog(5, "", "HO_AVBL Off");
                        //}
                        //else if (!allIOData[ES].Data)
                        //{
                        //    Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                        //    Status = EnumPIOStatus.Error;
                        //    WriteLog(5, "", "ES Off");
                        //}
                        //else 
                        if (PIO_Timer.ElapsedMilliseconds > localData.LoadUnloadData.PIOTimeoutList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]])
                        {
                            Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                            Status = EnumPIOStatus.Error;
                            WriteLog(5, "", String.Concat(Timeout.ToString(), " timeout "));
                        }
                        else if (!localData.LoadUnloadData.NotSendAllOff &&
                             mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { VALID }, new List<float>() { 0 }) &&
                             mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { COMPT }, new List<float>() { 0 }) &&
                             mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { CS_0 }, new List<float>() { 0 }))
                        {
                            PIO_Close();
                               Status = EnumPIOStatus.Complete;
                        }
                        #endregion
                        break;

                    case EnumPIOStatus.Error:
                        Status = EnumPIOStatus.SendAllOff;
                        break;

                    case EnumPIOStatus.SendAllOff:
                        if (SendAllOff())
                        {
                            Status = EnumPIOStatus.WaitAllOff;
                            PIO_Timer.Restart();
                        }
                        break;

                    case EnumPIOStatus.WaitAllOff:
                        if (!allIOData[L_REQ].Data && !allIOData[U_REQ].Data &&
                            !allIOData[VA].Data && !allIOData[READY].Data &&
                            !allIOData[VS_0].Data && !allIOData[VS_1].Data)
                        {
                            Status = EnumPIOStatus.NG;
                        }
                        else
                        {
                            if (PIO_Timer.ElapsedMilliseconds > 3000)
                            {
                                WriteLog(3, "", String.Concat("Wait EQ All Off Timeout"));
                                Status = EnumPIOStatus.NG;
                            }
                        }

                        break;

                    case EnumPIOStatus.NG:
                        break;

                    default:
                        break;
                }

                if (lastStatus != Status)
                {
                    lastStatus = Status;
                    WriteLog(7, "", String.Concat("PIO Status Change to ", Status.ToString()));
                }

                SpinWait.SpinUntil(() => false, PIO_SleepTime);
            }
        }
    }
}