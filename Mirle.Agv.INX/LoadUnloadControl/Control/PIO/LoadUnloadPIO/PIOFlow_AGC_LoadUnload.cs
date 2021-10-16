using Mirle.Agv.INX.Controller;
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
    public class PIOFlow_AGC_LoadUnload : PIOFlow
    {
        private string PLC_Request = "PLC Request";
        private string PLC_HiSpeed = "PLC HiSpeed";
        private string PLC_Permit = "PLC Permit";
        private string PLC_Finish = "PLC Finish";

        private string AGV_Request = "AGV Request";
        private string AGV_HiSpeed = "AGV HiSpeed";
        private string AGV_Permit = "AGV Permit";
        private string AGV_Finish = "AGV Finish";

        private bool _PLC_Request;
        private bool _PLC_HiSpeed;
        private bool _AGV_Permit;
        private bool _AGV_Finish;

        private bool _AGV_Request;
        private bool _AGV_HiSpeed;
        private bool _PLC_Permit;
        private bool _PLC_Finish;

        public bool res_Permit;
        public bool res_Finish;

        Dictionary<string, string> TimeoutLog = new Dictionary<string, string>();
        private List<string> tmp_Name = new List<string>();
        private List<bool> tmp_New = new List<bool>();
        private List<bool> tmp_Old = new List<bool>();

        private Stopwatch PIO_Timer = new Stopwatch();
        private long ts = 0;
        private Int32 PIO_Timeout = 6000000;

        public override void Initial(AlarmHandler alarmHandler, MIPCControlHandler mipcControl, string pioName, string pioDirection, string normalLogName)
        {
            this.alarmHandler = alarmHandler;
            PIOName = pioName;
            this.mipcControl = mipcControl;
            device = MethodInfo.GetCurrentMethod().ReflectedType.Name;

            // Input Name = String.Concat(Input Name, pioDirection);
            PLC_Request = String.Concat(PLC_Request, pioDirection);
            PLC_HiSpeed = String.Concat(PLC_HiSpeed, pioDirection);
            PLC_Permit = String.Concat(PLC_Permit, pioDirection);
            PLC_Finish = String.Concat(PLC_Finish, pioDirection);

            PIOInputTagList.AddRange(new string[] { PLC_Request, PLC_HiSpeed, PLC_Permit, PLC_Finish });
            PIOInputNameList.AddRange(new string[] { "PLC_Request", "PLC_HiSpeed", "PLC_Permit", "PLC_Finish" });

            // Output Name = String.Concat(Output Name, pioDirection);
            AGV_Request = String.Concat(AGV_Request, pioDirection);
            AGV_HiSpeed = String.Concat(AGV_HiSpeed, pioDirection);
            AGV_Permit = String.Concat(AGV_Permit, pioDirection);
            AGV_Finish = String.Concat(AGV_Finish, pioDirection);

            PIOOutputTagList.AddRange(new string[] { AGV_Request, AGV_HiSpeed, AGV_Permit, AGV_Finish });
            PIOOutputNameList.AddRange(new string[] { "AGV_Request", "AGV_HiSpeed", "AGV_Permit", "AGV_Finish" });
        }

        public override void PIOFlow_Load_UnLoad(EnumLoadUnload Load_UnLoad)
        {
            switch (Load_UnLoad)
            {
                #region Load
                case EnumLoadUnload.Load:
                    tmp_New = new List<bool>();
                    tmp_Old = new List<bool>();
                    tmp_Name = new List<string>();
                    Status = EnumPIOStatus.T1;

                    Thread T_PIO_Load = new Thread(LoadFlow);
                    T_PIO_Load.IsBackground = true;
                    T_PIO_Load.Start();

                    break;
                #endregion

                #region UnLoad
                case EnumLoadUnload.Unload:
                    tmp_New = new List<bool>();
                    tmp_Old = new List<bool>();
                    tmp_Name = new List<string>();
                    Status = EnumPIOStatus.T1;

                    Thread T_PIO_UnLoad = new Thread(UnLoadFlow);
                    T_PIO_UnLoad.IsBackground = true;
                    T_PIO_UnLoad.Start();

                    break;
                    #endregion
            }
        }

        public override void StopPIO()
        {
            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_Request }, new List<float>() { 0 });
            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_HiSpeed }, new List<float>() { 0 });
            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_Permit }, new List<float>() { 0 });
            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_Finish }, new List<float>() { 0 });
        }

        private void UpdatePIO()
        {
            _PLC_Request = localData.MIPCData.GetDataByMIPCTagName(PLC_Request) != 0;
            _PLC_HiSpeed = localData.MIPCData.GetDataByMIPCTagName(PLC_HiSpeed) != 0;
            _PLC_Permit = localData.MIPCData.GetDataByMIPCTagName(PLC_Permit) != 0;
            _PLC_Finish = localData.MIPCData.GetDataByMIPCTagName(PLC_Finish) != 0;

            _AGV_Request = localData.MIPCData.GetDataByMIPCTagName(AGV_Request) != 0;
            _AGV_HiSpeed = localData.MIPCData.GetDataByMIPCTagName(AGV_HiSpeed) != 0;
            _AGV_Permit = localData.MIPCData.GetDataByMIPCTagName(AGV_Permit) != 0;
            _AGV_Finish = localData.MIPCData.GetDataByMIPCTagName(AGV_Finish) != 0;
        }

        private void LoadFlow()
        {
            PIO_Timer.Restart();
            tmp_Name.AddRange(new string[] { PLC_Request, PLC_HiSpeed, AGV_Permit, AGV_Finish });
            tmp_New.Clear();
            tmp_Old.Clear();

            TimeoutLog.Clear();

            do
            {
                UpdatePIO();
                ts = PIO_Timer.ElapsedMilliseconds;
                tmp_New.AddRange(new bool[] { _PLC_Request, _PLC_HiSpeed, _AGV_Permit, _AGV_Finish });

                #region 紀錄變化的IO狀態
                if (tmp_Old.Count == 0)
                {
                    tmp_Old = tmp_New;
                    tmp_New.Clear();
                }
                else
                {
                    for (int i = 0; i < tmp_New.Count; i++)
                    {
                        if (tmp_New[i].Equals(tmp_Old[i]) == false)
                        {
                            PIOLog(DateTime.Now.ToString(), String.Format("{0}:{1}", tmp_Name[i], tmp_New));
                            tmp_Old[i] = tmp_New[i];
                        }
                    }
                    tmp_New.Clear();
                }
                #endregion

                switch (Status)
                {
                    #region PIO Load T1
                    case EnumPIOStatus.T1:
                        WriteLog(7, "", "Status T1");
                        if (_PLC_Request == true && _PLC_HiSpeed == true)
                        {
                            PIO_Timer.Restart();
                            Status = EnumPIOStatus.T2;
                            WriteLog(7, "", string.Format("{0}:{1}, {2}:{3} - Enter T2", PLC_Request, _PLC_Request, PLC_HiSpeed, _PLC_HiSpeed));
                        }
                        else
                        {
                            // Timeout
                            if (ts >= PIO_Timeout)
                            {
                                PIO_Timer.Reset();
                                Status = EnumPIOStatus.NG;

                                TimeoutLog.Add("Datetime", DateTime.Now.ToString());
                                TimeoutLog.Add("TimeoutLevel", EnumPIOStatus.T1.ToString());
                                TimeoutLog.Add("TimeoutValue", PIO_Timeout.ToString());
                            }
                        }
                        break;
                    #endregion

                    #region PIO Load T2 || Enter Busy
                    case EnumPIOStatus.T2:
                        WriteLog(7, "", "Status T2");
                        if (res_Permit == true)
                        {
                            if (_AGV_Permit == false)
                            {
                                PIO_Timer.Restart();
                                mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_Permit }, new List<float>() { 1 });
                            }

                            if (_AGV_Permit == true)
                            {
                                PIO_Timer.Restart();
                                Status = EnumPIOStatus.T3;
                                WriteLog(7, "", string.Format("{0}:{1} - Enter T3", AGV_Permit, _AGV_Permit));
                            }
                            else
                            {
                                if (_PLC_Request == false && _PLC_HiSpeed == false)
                                {
                                    WriteLog(5, "", string.Format("{0}:{1}, {2}:{3}", PLC_Request, _PLC_Request, PLC_HiSpeed, _PLC_HiSpeed));
                                }
                                // Timeout
                                if (ts >= PIO_Timeout)
                                {
                                    PIO_Timer.Reset();
                                    Status = EnumPIOStatus.NG;

                                    TimeoutLog.Add("Datetime", DateTime.Now.ToString());
                                    TimeoutLog.Add("TimeoutLevel", EnumPIOStatus.T2.ToString());
                                    TimeoutLog.Add("TimeoutValue", PIO_Timeout.ToString());
                                }
                            }
                        }
                        else
                        {
                            PIO_Timer.Reset();
                            Status = EnumPIOStatus.NG;

                            TimeoutLog.Add("Datetime", DateTime.Now.ToString());
                            TimeoutLog.Add("TimeoutLevel", "res_Permit Timeout");
                            TimeoutLog.Add("TimeoutValue", PIO_Timeout.ToString());
                        }
                        break;
                    #endregion

                    #region PIO Load T3
                    case EnumPIOStatus.T3:
                        WriteLog(7, "", "Status T3");
                        if (res_Finish == true)
                        {
                            if (_AGV_Finish == false)
                            {
                                PIO_Timer.Restart();
                                mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_Finish }, new List<float>() { 1 });
                            }

                            if (_AGV_Finish == true)
                            {
                                PIO_Timer.Restart();
                                Status = EnumPIOStatus.T4;
                                WriteLog(7, "", string.Format("{0}:{1} - Enter T4", AGV_Finish, _AGV_Finish));
                            }
                            else
                            {
                                if (_AGV_Permit == false)
                                {
                                    WriteLog(5, "", string.Format("{0}:{1}", AGV_Permit, _AGV_Permit));
                                }
                                // Timeout
                                if (ts >= PIO_Timeout)
                                {
                                    PIO_Timer.Reset();
                                    Status = EnumPIOStatus.NG;

                                    TimeoutLog.Add("Datetime", DateTime.Now.ToString());
                                    TimeoutLog.Add("TimeoutLevel", EnumPIOStatus.T3.ToString());
                                    TimeoutLog.Add("TimeoutValue", PIO_Timeout.ToString());
                                }
                            }
                        }
                        else
                        {
                            if (ts >= PIO_Timeout)
                            {
                                PIO_Timer.Reset();
                                Status = EnumPIOStatus.NG;

                                TimeoutLog.Add("Datetime", DateTime.Now.ToString());
                                TimeoutLog.Add("TimeoutLevel", "res_Finish Timeout");
                                TimeoutLog.Add("TimeoutValue", PIO_Timeout.ToString());
                            }
                        }

                        break;
                    #endregion

                    #region PIO Load T4
                    case EnumPIOStatus.T4:
                        WriteLog(7, "", "Status T4");
                        if (_AGV_Permit == true)
                        {
                            PIO_Timer.Restart();
                            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_Permit }, new List<float>() { 0 });
                        }

                        if (_PLC_Request == false && _PLC_HiSpeed == false && _AGV_Permit == false)
                        {
                            PIO_Timer.Restart();
                            Status = EnumPIOStatus.T5;
                            WriteLog(7, "", string.Format("{0}:{1}, {2}:{3}, {4}:{5} - Enter T5", PLC_Request, _PLC_Request, PLC_HiSpeed, _PLC_HiSpeed, AGV_Permit, _AGV_Permit));
                        }
                        else
                        {
                            if (_AGV_Finish == false)
                            {
                                WriteLog(5, "", string.Format("{0}:{1}", AGV_Finish, _AGV_Finish));
                            }
                            // Timeout
                            if (ts >= PIO_Timeout)
                            {
                                PIO_Timer.Reset();
                                Status = EnumPIOStatus.NG;

                                TimeoutLog.Add("Datetime", DateTime.Now.ToString());
                                TimeoutLog.Add("TimeoutLevel", EnumPIOStatus.T4.ToString());
                                TimeoutLog.Add("TimeoutValue", PIO_Timeout.ToString());
                            }
                        }
                        break;
                    #endregion

                    #region PIO Load T5 || Enter Complete
                    case EnumPIOStatus.T5:
                        if (_AGV_Finish)
                        {
                            PIO_Timer.Restart();
                            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_Finish }, new List<float>() { 0 });
                        }

                        if (_AGV_Finish == false)
                        {
                            PIO_Timer.Reset();
                            Status = EnumPIOStatus.Complete;
                            WriteLog(7, "", string.Format("{0}:{1} - Enter Complete", AGV_Finish, _AGV_Finish));

                            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_Permit }, new List<float>() { 0 });
                            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_Finish }, new List<float>() { 0 });
                        }
                        else
                        {
                            if (_PLC_Request == true && _PLC_HiSpeed == true && _AGV_Permit == true)
                            {
                                WriteLog(5, "", string.Format("{0}:{1}, {2}:{3}, {4}:{5}", PLC_Request, _PLC_Request, PLC_HiSpeed, _PLC_HiSpeed, AGV_Permit, _AGV_Permit));
                            }
                            // Timeout
                            if (ts >= PIO_Timeout)
                            {
                                PIO_Timer.Reset();
                                Status = EnumPIOStatus.NG;

                                TimeoutLog.Add("Datetime", DateTime.Now.ToString());
                                TimeoutLog.Add("TimeoutLevel", EnumPIOStatus.T5.ToString());
                                TimeoutLog.Add("TimeoutValue", PIO_Timeout.ToString());
                            }
                        }
                        break;
                    #endregion

                    #region PIO Load T6 || PLC Abnormal
                    case EnumPIOStatus.T6:

                        break;
                    #endregion

                    #region PIO Load T7 || AGV Abnormal
                    case EnumPIOStatus.T7:

                        break;
                    #endregion

                    #region PIO Load NG
                    case EnumPIOStatus.NG:
                        mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_Request }, new List<float>() { 0 });
                        mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_HiSpeed }, new List<float>() { 0 });
                        mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_Permit }, new List<float>() { 0 });
                        mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_Finish }, new List<float>() { 0 });
                        PIO_Timer.Reset();
                        break;
                        #endregion
                }
                Thread.Sleep(PIO_SleepTime);
            } while (Status != EnumPIOStatus.NG && Status != EnumPIOStatus.Complete);
            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_Permit }, new List<float>() { 0 });
            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_Finish }, new List<float>() { 0 });
            WriteLog(7, "", "Complete Load");
        }

        private void UnLoadFlow()
        {
            PIO_Timer.Restart();
            tmp_Name.AddRange(new string[] { AGV_Request, AGV_HiSpeed, PLC_Permit, PLC_Finish });
            tmp_New.Clear();
            tmp_Old.Clear();

            TimeoutLog.Clear();

            do
            {
                UpdatePIO();
                ts = PIO_Timer.ElapsedMilliseconds;
                tmp_New.AddRange(new bool[] { _AGV_Request, _AGV_HiSpeed, _PLC_Permit, _PLC_Finish });

                #region 紀錄變化的IO狀態'
                if (tmp_Old.Count == 0)
                {
                    tmp_Old = tmp_New;
                    tmp_New.Clear();
                }
                else
                {
                    for (int i = 0; i < tmp_New.Count; i++)
                    {
                        if (tmp_New[i].Equals(tmp_Old[i]) == false)
                        {
                            PIOLog(DateTime.Now.ToString(), String.Format("{0}:{1}", tmp_Name[i], tmp_New));
                            tmp_Old[i] = tmp_New[i];
                        }
                    }
                    tmp_New.Clear();
                }
                #endregion
                
                switch (Status)
                {
                    #region PIO Load T1
                    case EnumPIOStatus.T1:
                        if (_AGV_Request == false || _AGV_HiSpeed == false)
                        {
                            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_Request }, new List<float>() { 1 });
                            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_HiSpeed }, new List<float>() { 1 });
                        }

                        if (_AGV_Request == true && _AGV_HiSpeed == true)
                        {
                            PIO_Timer.Restart();
                            Status = EnumPIOStatus.T2;
                            WriteLog(7, "", string.Format("{0}:{1}, {2}:{3} - Enter T2", AGV_Request, _AGV_Request, AGV_HiSpeed, _AGV_HiSpeed));
                        }
                        else
                        {
                            // Timeout
                            if (ts >= PIO_Timeout)
                            {
                                PIO_Timer.Reset();
                                Status = EnumPIOStatus.NG;

                                TimeoutLog.Add("Datetime", DateTime.Now.ToString());
                                TimeoutLog.Add("TimeoutLevel", EnumPIOStatus.T1.ToString());
                                TimeoutLog.Add("TimeoutValue", PIO_Timeout.ToString());
                            }
                        }
                        break;
                    #endregion

                    #region PIO Load T2
                    case EnumPIOStatus.T2:
                        if (_PLC_Permit == true)
                        {
                            PIO_Timer.Restart();
                            Status = EnumPIOStatus.T3;
                            WriteLog(7, "", string.Format("{0}:{1} - Enter T3", PLC_Permit, _PLC_Permit));
                        }
                        else
                        {
                            if (_AGV_Request == false && _AGV_HiSpeed == false)
                            {
                                WriteLog(5, "", string.Format("{0}:{1}, {2}:{3}", AGV_Request, _AGV_Request, AGV_HiSpeed, _AGV_HiSpeed));
                            }
                            // Timeout
                            if (ts >= PIO_Timeout)
                            {
                                PIO_Timer.Reset();
                                Status = EnumPIOStatus.NG;

                                TimeoutLog.Add("Datetime", DateTime.Now.ToString());
                                TimeoutLog.Add("TimeoutLevel", EnumPIOStatus.T2.ToString());
                                TimeoutLog.Add("TimeoutValue", PIO_Timeout.ToString());
                            }
                        }
                        break;
                    #endregion

                    #region PIO Load T3
                    case EnumPIOStatus.T3:
                        if (_PLC_Finish == true)
                        {
                            PIO_Timer.Restart();
                            Status = EnumPIOStatus.T4;
                            WriteLog(7, "", string.Format("{0}:{1} - Enter T4", PLC_Finish, _PLC_Finish));
                        }
                        else
                        {
                            if (_PLC_Permit == false)
                            {
                                WriteLog(5, "", string.Format("{0}:{1}", PLC_Permit, _PLC_Permit));
                            }
                            // Timeout
                            if (ts >= PIO_Timeout)
                            {
                                PIO_Timer.Reset();
                                Status = EnumPIOStatus.NG;

                                TimeoutLog.Add("Datetime", DateTime.Now.ToString());
                                TimeoutLog.Add("TimeoutLevel", EnumPIOStatus.T3.ToString());
                                TimeoutLog.Add("TimeoutValue", PIO_Timeout.ToString());
                            }
                        }
                        break;
                    #endregion

                    #region PIO Load T4
                    case EnumPIOStatus.T4:
                        if (_AGV_Request == true || _AGV_HiSpeed == true)
                        {
                            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_Request }, new List<float>() { 0 });
                            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_HiSpeed }, new List<float>() { 0 });
                        }

                        if (_AGV_Request == false && _AGV_HiSpeed == false && _PLC_Permit == false)
                        {
                            PIO_Timer.Restart();
                            Status = EnumPIOStatus.T5;
                            WriteLog(7, "", string.Format("{0}:{1}, {2}:{3}, {4}:{5} - Enter T5", AGV_Request, _AGV_Request, AGV_HiSpeed, _AGV_HiSpeed, PLC_Permit, _PLC_Permit));
                        }
                        else
                        {
                            if (_PLC_Finish == false)
                            {
                                WriteLog(5, "", string.Format("{0}:{1}", PLC_Finish, _PLC_Finish));
                            }
                            // Timeout
                            if (ts >= PIO_Timeout)
                            {
                                PIO_Timer.Reset();
                                Status = EnumPIOStatus.NG;

                                TimeoutLog.Add("Datetime", DateTime.Now.ToString());
                                TimeoutLog.Add("TimeoutLevel", EnumPIOStatus.T4.ToString());
                                TimeoutLog.Add("TimeoutValue", PIO_Timeout.ToString());
                            }
                        }
                        break;
                    #endregion

                    #region PIO Load T5 || Enter Complete
                    case EnumPIOStatus.T5:
                        if (_PLC_Finish == false)
                        {
                            PIO_Timer.Reset();
                            Status = EnumPIOStatus.Complete;
                            WriteLog(7, "", string.Format("{0}:{1} - Enter Complete", PLC_Finish, _PLC_Finish));
                        }
                        else
                        {
                            if (_AGV_Request == true && _AGV_HiSpeed == true && _PLC_Permit == true)
                            {
                                WriteLog(5, "", string.Format("{0}:{1}, {2}:{3}, {4}:{5}", AGV_Request, _AGV_Request, AGV_HiSpeed, _AGV_HiSpeed, PLC_Permit, _PLC_Permit));
                            }
                            // Timeout
                            if (ts >= PIO_Timeout)
                            {
                                PIO_Timer.Reset();
                                Status = EnumPIOStatus.NG;

                                TimeoutLog.Add("Datetime", DateTime.Now.ToString());
                                TimeoutLog.Add("TimeoutLevel", EnumPIOStatus.T5.ToString());
                                TimeoutLog.Add("TimeoutValue", PIO_Timeout.ToString());
                            }
                        }
                        break;
                    #endregion

                    #region PIO Load T6 || PLC Abnormal
                    case EnumPIOStatus.T6:

                        break;
                    #endregion

                    #region PIO Load T7 || AGV Abnormal
                    case EnumPIOStatus.T7:

                        break;
                    #endregion

                    #region PIO Load NG
                    case EnumPIOStatus.NG:
                        mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_Request }, new List<float>() { 0 });
                        mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_HiSpeed }, new List<float>() { 0 });
                        mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_Permit }, new List<float>() { 0 });
                        mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_Finish }, new List<float>() { 0 });
                        PIO_Timer.Reset();
                        break;
                        #endregion
                }
                Thread.Sleep(PIO_SleepTime);
            } while (Status != EnumPIOStatus.NG && Status != EnumPIOStatus.Complete);
            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_Request }, new List<float>() { 0 });
            mipcControl.SendMIPCDataByMIPCTagName(new List<string>() { AGV_HiSpeed }, new List<float>() { 0 });
            WriteLog(7, "", "Complete UnLoad");
        }

        public override void PIOTestOnOff(string tag, bool onOff)
        {
            if (localData.AutoManual == EnumAutoState.Manual && Status == EnumPIOStatus.Idle)
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