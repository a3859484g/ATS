using System;

namespace Mirle.Agv.MiddlePackage.Umtc.Alarms
{
    interface IMidAlarmAgent
    {
        event EventHandler<Model.AlarmArgs> OnMiddlePackageSetAlarmEvent;
        event EventHandler OnMiddlePackageResetAlarmEvent;

        void SetAlarmFromLocalPackage(Model.AlarmArgs alarmArgs);
        void ResetAlarmFromLocalPackage();
    }
}
