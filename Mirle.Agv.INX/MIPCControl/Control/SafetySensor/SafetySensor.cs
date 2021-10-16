using Mirle.Agv.INX.Controller;
using Mirle.Agv.INX.Controller.Tools;
using Mirle.Agv.INX.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Control
{
    public class SafetySensor
    {
        protected ComputeFunction computeFunction = ComputeFunction.Instance;
        protected LoggerAgent loggerAgent = LoggerAgent.Instance;
        protected LocalData localData = LocalData.Instance;
        protected MIPCControlHandler mipcControl = null;

        public EnumSafetySensorType Type { get; set; } = EnumSafetySensorType.None;
        protected EnumMovingDirection movingDirection = EnumMovingDirection.Initial;

        public uint Status { get; set; } = 0;
        /// 0b 0 0 0 0 0 0 0 0 0(9bit)
        ///  Error / Warn / EMO / IPCEMO / EMS / SlowStop / 降速-低 / 降速-高 / Normal

        protected int safetyLevelCount = Enum.GetNames(typeof(EnumSafetyLevel)).Count();
        protected uint maxStatusValue = ((uint)Math.Pow(2, Enum.GetNames(typeof(EnumSafetyLevel)).Count()) - 1);

        private uint byPassAlarm = 0;
        public uint ByPassAlarm
        {
            get
            {
                return byPassAlarm;
            }

            set
            {
                if (value != byPassAlarm)
                {
                    byPassAlarm = value;
                    WriteLog(7, "", String.Concat((Config == null ? "" : Config.Device), " ByPassAlarm = ", (byPassAlarm == 1).ToString()));
                }
            }
        }

        private uint byPassStatus = 0;
        public uint ByPassStatus
        {
            get
            {
                return byPassStatus;
            }

            set
            {
                if (value != byPassStatus)
                {
                    byPassStatus = value;
                    WriteLog(7, "", String.Concat((Config == null ? "" : Config.Device), " ByPassStatus = ", (byPassStatus == 1).ToString()));
                }
            }
        }
        protected string normalLogName = "SafetySensor";
        protected string device = "";

        protected AlarmHandler alarmHandler;

        public SafetySensorData Config { get; set; } = null;

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

        protected void SetAlarmCode(EnumMIPCControlErrorCode alarmCode)
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

        protected void ResetAlarmCode(EnumMIPCControlErrorCode alarmCode)
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

        public virtual void Initial(MIPCControlHandler mipcControl, AlarmHandler alarmHandler, SafetySensorData config)
        {

        }

        public virtual bool ChangeMovingDirection(EnumMovingDirection newDirection)
        {
            return true;
        }

        public virtual void ChangeMovingDirection_AddList(EnumMovingDirection newDirection)
        {

        }

        public virtual void UpdateStatus()
        {

        }

        public void ByPassBySafetyLevel(EnumSafetyLevel byPassLevel)
        {

        }
    }
}
