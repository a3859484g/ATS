using System;

namespace Mirle.Agv.MiddlePackage.Umtc.Alarms
{
    interface IAlarmHandler : Tools.IMessageHandler, IMidAlarmAgent
    {
        event EventHandler<Model.AlarmArgs> OnSetAlarmToAgvcEvent;
        event EventHandler OnResetAlarmToAgvcEvent;

        void SetAlarmFromAgvm(EnumMiddlerAlarmCode errorCode);
        void ResetAllAlarmsFromAgvm();
        void ResetAllAlarmsFromAgvc();
        string GetLastAlarmMsg();
        string GetAlarmLogMsg();
        string GetAlarmHistoryLogMsg();
        bool HasHappeningAlarm();
    }
}
