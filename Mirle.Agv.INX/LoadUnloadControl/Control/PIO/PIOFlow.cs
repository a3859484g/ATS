using Mirle.Agv.INX.Controller;
using Mirle.Agv.INX.Controller.Tools;
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
    public class PIOFlow
    {
        protected LoggerAgent loggerAgent = LoggerAgent.Instance;
        protected LocalData localData = LocalData.Instance;

        public bool IsRFPIO { get; set; } = false;
        public EnumPIOStatus Status { get; set; } = EnumPIOStatus.Idle;
        public EnumPIOStatus Timeout { get; set; } = EnumPIOStatus.None;
        protected string device = "";

        public List<string> PIOInputTagList = new List<string>();
        public List<string> PIOOutputTagList = new List<string>();

        public List<string> PIOInputNameList = new List<string>();
        public List<string> PIOOutputNameList = new List<string>();

        protected MIPCControlHandler mipcControl = null;
        protected string normalLogName = "";
        private string normalLogName_PIO = "PIO";
        public string PIOName { get; set; } = "";
        public int PIO_SleepTime { get; set; } = 10;
        protected AlarmHandler alarmHandler = null;
        protected bool stopPIO = false;

        protected Dictionary<string, DataDelayAndChange> allIOData = new Dictionary<string, DataDelayAndChange>();

        protected Thread pioFlowThread = null;
        public List<PIODataAndTime> PIOHistory { get; set; } = new List<PIODataAndTime>();

        public virtual void Initial(AlarmHandler alarmHandler, MIPCControlHandler mipcControl, string pioName, string pioDirection, string normalLogName)
        {
            this.normalLogName = normalLogName;
            PIOName = pioName;
            this.mipcControl = mipcControl;
            device = MethodInfo.GetCurrentMethod().ReflectedType.Name;

            localData.MIPCData.AllDataByMIPCTagName.ContainsKey("XXX");

            float result = localData.MIPCData.GetDataByMIPCTagName("XXX"); // 取資料.


            List<string> tagNameList = new List<string>() { "XXX" };
            List<float> valueList = new List<float>() { 0 };

            if (!mipcControl.SendMIPCDataByMIPCTagName(tagNameList, valueList))
            {
                //retry
            }
        }

        protected void SetAlarmCode(EnumLoadUnloadControlErrorCode alarmCode)
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

        protected void ResetAlarmCode(EnumLoadUnloadControlErrorCode alarmCode)
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

        protected void WriteLog(int logLevel, string carrierId, string message, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            LogFormat logFormat = new LogFormat(normalLogName, logLevel.ToString(), memberName, device, carrierId, message);

            loggerAgent.Log(logFormat.Category, logFormat);

            if (logLevel <= localData.ErrorLevel)
            {
                logFormat = new LogFormat(localData.ErrorLogName, logLevel.ToString(), memberName, device, carrierId, message);
                loggerAgent.Log(logFormat.Category, logFormat);
            }
        }

        protected void PIOLog(string message, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            LogFormat logFormat = new LogFormat(normalLogName_PIO, "7", memberName, device, "", message);

            loggerAgent.Log(logFormat.Category, logFormat);

        }

        public virtual void AlarmCodeClear()
        {

        }

        public void ResetPIO()
        {
            stopPIO = false;

            if (pioFlowThread != null && pioFlowThread.IsAlive)
            {
                WriteLog(5, "", "pioFlowThread isAlive, Abort Thread");
                pioFlowThread.Abort();

                List<float> resetValue = new List<float>();

                for (int i = 0; i < PIOOutputTagList.Count; i++)
                    resetValue.Add(0);

                mipcControl.SendMIPCDataByMIPCTagName(PIOOutputTagList, resetValue);
            }

            for (int i = 0; i < PIOInputTagList.Count; i++)
            {
                allIOData[PIOInputTagList[i]].Data = false;
            }

            Timeout = EnumPIOStatus.None;
            Status = EnumPIOStatus.Idle;
            PIOHistory = new List<PIODataAndTime>();
        }

        public virtual void StopPIO()
        {
            stopPIO = true;
        }

        public virtual void StartPIO()
        {

        }

        public virtual void PIOFlow_Load_UnLoad(EnumLoadUnload Load_UnLoad)
        {
        }

        public virtual bool CanStartPIO
        {
            get
            {
                return true;
            }
        }

        public virtual void PIOTestOnOff(string tag, bool onOff)
        {
        }

        public virtual bool GetPIOStatueByTag(string tag)
        {
            return false;
        }

        public virtual void ChangeRFPIOChannelByAddressID(string addressID)
        {
        }

        public virtual bool ChangeRFPIOChannel(string deviceID, string chanelID)
        {
            return false;
        }
    }
}
