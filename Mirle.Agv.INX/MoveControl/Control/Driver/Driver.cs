using Mirle.Agv.INX.Controller.Tools;
using Mirle.Agv.INX.Model;
using Mirle.Agv.INX.Model.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Controller
{
    public class Driver
    {
        protected LocalData localData = LocalData.Instance;
        protected ComputeFunction computeFunction = ComputeFunction.Instance;
        protected bool resetAlarm = false;
        protected Thread pollingThread;

        protected string normalLogName = "";
        protected string device = "";

        protected LoggerAgent loggerAgent = LoggerAgent.Instance;
        protected AlarmHandler alarmHandler;

        protected EnumControlStatus status = EnumControlStatus.NotInitial;
        public bool PollingOnOff { get; set; } = true;

        public System.Diagnostics.Stopwatch pollingTimer = new System.Diagnostics.Stopwatch();

        public EnumControlStatus Status
        {
            get
            {
                if (resetAlarm)
                    return EnumControlStatus.ResetAlarm;

                return status;
            }
        }

        #region WriteLog & SendAlarmCode.
        protected void SetAlarmCode(EnumMoveCommandControlErrorCode alarmCode)
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

        protected void ResetAlarmCode(EnumMoveCommandControlErrorCode alarmCode)
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
        #endregion

        //virtual public void InitailDriver()

        virtual public void ConnectDriver()
        {
        }

        virtual public void CloseDriver()
        {
        }

        virtual public void ResetAlarm()
        {
        }
    }
}
