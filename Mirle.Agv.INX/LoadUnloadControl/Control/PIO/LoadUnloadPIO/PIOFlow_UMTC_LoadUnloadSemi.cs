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
    public class PIOFlow_UMTC_LoadUnloadSemi : PIOFlow
    {
        private string VALID = "PIO-out01";
        private string CS_0 = "PIO-out02";
        private string CS_1 = "PIO-out03";
        private string AM_AVBL = "PIO-out04";
        private string TR_REQ = "PIO-out05";
        private string BUSY = "PIO-out06";
        private string COMPT = "PIO-out07";
        private string CONT = "PIO-out08";

        private string GO = "PIO-in00";
        private string L_REQ = "PIO-in01";
        private string U_REQ = "PIO-in02";
        private string VA = "PIO-in03";
        private string READY = "PIO-in04";
        private string VS_0 = "PIO-in05";
        private string VS_1 = "PIO-in06";
        private string HO_AVBL = "PIO-in07";
        private string ES = "PIO-in08";

        public bool SendBusy = false;
        public bool SendComplete = false;

        private Stopwatch PIO_Timer = new Stopwatch();

        public override void Initial(AlarmHandler alarmHandler, MIPCControlHandler mipcControl, string pioName, string pioDirection, string normalLogName)
        {
            this.normalLogName = normalLogName;

            int sensorDelayTime = 0;

            if (localData.LoadUnloadData.PIOTimeoutList.Count != 8)
                localData.LoadUnloadData.PIOTimeoutList = new List<double>() { 2000, 2000, 2000, 2000, 50000, 50000, 2000, 2000 };

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

            if (!localData.LoadUnloadData.PIOStatusToFlowIndex.ContainsKey(EnumPIOStatus.TA1))
                localData.LoadUnloadData.PIOStatusToFlowIndex.Add(EnumPIOStatus.TA1, 0);

            if (!localData.LoadUnloadData.PIOStatusToFlowIndex.ContainsKey(EnumPIOStatus.TP1))
                localData.LoadUnloadData.PIOStatusToFlowIndex.Add(EnumPIOStatus.TP1, 1);

            if (!localData.LoadUnloadData.PIOStatusToFlowIndex.ContainsKey(EnumPIOStatus.TA2))
                localData.LoadUnloadData.PIOStatusToFlowIndex.Add(EnumPIOStatus.TA2, 2);

            if (!localData.LoadUnloadData.PIOStatusToFlowIndex.ContainsKey(EnumPIOStatus.TP2))
                localData.LoadUnloadData.PIOStatusToFlowIndex.Add(EnumPIOStatus.TP2, 3);

            if (!localData.LoadUnloadData.PIOStatusToFlowIndex.ContainsKey(EnumPIOStatus.TP3))
                localData.LoadUnloadData.PIOStatusToFlowIndex.Add(EnumPIOStatus.TP3, 4);

            if (!localData.LoadUnloadData.PIOStatusToFlowIndex.ContainsKey(EnumPIOStatus.TP4))
                localData.LoadUnloadData.PIOStatusToFlowIndex.Add(EnumPIOStatus.TP4, 5);

            if (!localData.LoadUnloadData.PIOStatusToFlowIndex.ContainsKey(EnumPIOStatus.TA3))
                localData.LoadUnloadData.PIOStatusToFlowIndex.Add(EnumPIOStatus.TA3, 6);

            if (!localData.LoadUnloadData.PIOStatusToFlowIndex.ContainsKey(EnumPIOStatus.TP5))
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


            allIOData.Add(VALID, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));
            allIOData.Add(CS_0, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));
            allIOData.Add(CS_1, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));
            allIOData.Add(AM_AVBL, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));
            allIOData.Add(TR_REQ, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));
            allIOData.Add(BUSY, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));
            allIOData.Add(COMPT, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));
            allIOData.Add(CONT, new DataDelayAndChange(sensorDelayTime, EnumDelayType.OnDelay));

            PIOOutputTagList.AddRange(new string[] { VALID, CS_0, CS_1, AM_AVBL, TR_REQ, BUSY, COMPT, CONT });
            PIOOutputNameList.AddRange(new string[] { "VALID", "CS_0", "CS_1", "AM_AVBL", "TR_REQ", "BUSY", "COMPT", "CONT" });
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

        public override bool CanStartPIO
        {
            get
            {
                return localData.MIPCData.GetDataByMIPCTagName(HO_AVBL) != 0 &&
                       localData.MIPCData.GetDataByMIPCTagName(ES) != 0 &&
                       localData.MIPCData.GetDataByMIPCTagName(READY) == 0 &&
                       localData.MIPCData.GetDataByMIPCTagName(L_REQ) == 0 &&
                       localData.MIPCData.GetDataByMIPCTagName(U_REQ) == 0;
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
            bool change = false;

            uint inputUint = 0;
            uint outputUint = 0;

            for (int i = 0; i < PIOInputTagList.Count; i++)
            {
                allIOData[PIOInputTagList[i]].Data = localData.MIPCData.GetDataByMIPCTagName(PIOInputTagList[i]) != 0;

                inputUint = (uint)((inputUint << 1) + (allIOData[PIOInputTagList[i]].data ? 1 : 0));

                if (allIOData[PIOInputTagList[i]].Change)
                {
                    WriteLog(7, "", String.Concat(PIOInputNameList[i], " Change to ", (allIOData[PIOInputTagList[i]].data ? "On" : "Off")));
                    change = true;
                }
            }

            for (int i = 0; i < PIOOutputTagList.Count; i++)
            {
                allIOData[PIOOutputTagList[i]].Data = localData.MIPCData.GetDataByMIPCTagName(PIOOutputTagList[i]) != 0;

                outputUint = (uint)((outputUint << 1) + (allIOData[PIOOutputTagList[i]].data ? 1 : 0));

                if (allIOData[PIOOutputTagList[i]].Change)
                {
                    WriteLog(7, "", String.Concat(PIOOutputNameList[i], " Change to ", (allIOData[PIOOutputTagList[i]].data ? "On" : "Off")));
                    change = true;
                }
            }

            if (change)
                PIOHistory.Add(new PIODataAndTime(inputUint, outputUint));
        }

        private EnumLoadUnload loadUnload = EnumLoadUnload.Load;

        public void PIOFlow_Load_UnLoadThread()
        {
            WriteLog(7, "", String.Concat("PIO Start : ", loadUnload.ToString()));
            Timeout = EnumPIOStatus.None;
            Stopwatch sleepTimer = new Stopwatch();
            PIO_Timer.Restart();
            EnumPIOStatus lastStatus = EnumPIOStatus.Idle;

            PIOHistory = new List<PIODataAndTime>();

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
                        else if (PIO_Timer.ElapsedMilliseconds > localData.LoadUnloadData.PIOTimeoutList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]])
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
                                //Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                                Timeout = EnumPIOStatus.LoadUnloadSingalError;
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
                        else if (PIO_Timer.ElapsedMilliseconds > localData.LoadUnloadData.PIOTimeoutList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]])
                        {
                            Timeout = localData.LoadUnloadData.PIOTimeoutTageList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]];
                            Status = EnumPIOStatus.Error;
                            WriteLog(5, "", String.Concat(Timeout.ToString(), " timeout "));
                        }
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
                        else if (PIO_Timer.ElapsedMilliseconds > localData.LoadUnloadData.PIOTimeoutList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]])
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
                        else if (PIO_Timer.ElapsedMilliseconds > localData.LoadUnloadData.PIOTimeoutList[localData.LoadUnloadData.PIOStatusToFlowIndex[Status]])
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

                sleepTimer.Restart();

                while (sleepTimer.ElapsedMilliseconds < PIO_SleepTime)
                    Thread.Sleep(1);
                //SpinWait.SpinUntil(() => false, PIO_SleepTime);
            }
        }

        public override void PIOTestOnOff(string tag, bool onOff)
        {
            if (localData.AutoManual == EnumAutoState.Manual && localData.LoadUnloadData.LoadUnloadCommand == null)
            {
                if (onOff)
                    mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { tag }, new List<float>() { 1 });
                else
                    mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { tag }, new List<float>() { 0 });
            }
        }

        public override bool GetPIOStatueByTag(string tag)
        {
            return (float)localData.MIPCData.GetDataByMIPCTagName(tag) == 1;
        }
    }
}