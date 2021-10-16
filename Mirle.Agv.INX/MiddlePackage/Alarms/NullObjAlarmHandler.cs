using Mirle.Agv.MiddlePackage.Umtc.Controller;
using Mirle.Agv.MiddlePackage.Umtc.Model;
using Mirle.Agv.MiddlePackage.Umtc.Tools;
using System;
using System.Linq;

namespace Mirle.Agv.MiddlePackage.Umtc.Alarms
{
    public class NullObjAlarmHandler : IAlarmHandler
    {
        public event EventHandler<Model.AlarmArgs> OnSetAlarmToAgvcEvent;
        public event EventHandler OnResetAlarmToAgvcEvent;

        public event EventHandler<AlarmArgs> OnMiddlePackageSetAlarmEvent;
        public event EventHandler OnMiddlePackageResetAlarmEvent;

        public MainAlarmHandler MainAlarmHandler { get; set; } = new MainAlarmHandler();

        public void ResetAllAlarmsFromAgvc()
        {
            MainAlarmHandler.ResetAllAlarms();
        }

        public void ResetAllAlarmsFromAgvm()
        {
            MainAlarmHandler.ResetAllAlarms();
            OnResetAlarmToAgvcEvent?.Invoke(this, default);
        }

        public void SetAlarmFromAgvm(EnumMiddlerAlarmCode alarmCode)
        {
            int errorCode = (int)alarmCode;

            MainAlarmHandler.SetAlarm(errorCode);
            if (!MainAlarmHandler.dicHappeningAlarms.ContainsKey(errorCode))
            {
                MainAlarmHandler.SetAlarm(errorCode);

                var isAlarm = MainAlarmHandler.IsAlarm(errorCode);

                OnSetAlarmToAgvcEvent?.Invoke(this, new AlarmArgs()
                {
                    ErrorCode = errorCode,
                    IsAlarm = isAlarm,
                });
            }
        }

        public string GetLastAlarmMsg()
        {
            return MainAlarmHandler.LastAlarmMsg;
        }

        public string GetAlarmLogMsg()
        {
            return MainAlarmHandler.GetAlarmLog();
        }

        public string GetAlarmHistoryLogMsg()
        {
            return MainAlarmHandler.GetAlarmHistoryLog();
        }

        public bool HasHappeningAlarm()
        {
            return MainAlarmHandler.dicHappeningAlarms.Any();
        }

        public void SetAlarmFromLocalPackage(AlarmArgs alarmArgs)
        {
            if (!MainAlarmHandler.dicHappeningAlarms.ContainsKey(alarmArgs.ErrorCode))
            {
                MainAlarmHandler.SetAlarm(alarmArgs.ErrorCode);
                OnSetAlarmToAgvcEvent?.Invoke(default, alarmArgs);

                HandlerLogMsg(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, MainAlarmHandler.GetAlarmText(alarmArgs.ErrorCode));
            }
        }

        public void ResetAlarmFromLocalPackage()
        {
            MainAlarmHandler.ResetAllAlarms();
            OnResetAlarmToAgvcEvent?.Invoke(default, default);
        }

        private NLog.Logger _handlerLogger = NLog.LogManager.GetLogger("Package");

        public void HandlerLogMsg(string classMethodName, string msg)
        {
            _handlerLogger.Debug($"[{Model.Vehicle.Instance.SoftwareVersion}][{Model.Vehicle.Instance.AgvcConnectorConfig.ClientName}][{classMethodName}][{msg}]");
        }

        public void HandlerLogError(string classMethodName, string msg)
        {
            _handlerLogger.Error($"[{Model.Vehicle.Instance.SoftwareVersion}][{Model.Vehicle.Instance.AgvcConnectorConfig.ClientName}][{classMethodName}][{msg}]");
        }
    }
}
