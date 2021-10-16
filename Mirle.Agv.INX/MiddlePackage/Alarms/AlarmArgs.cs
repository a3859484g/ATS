using System;

namespace Mirle.Agv.MiddlePackage.Umtc.Model
{
    public class AlarmArgs : EventArgs
    {
        public int ErrorCode { get; set; } = 0;
        public bool IsAlarm { get; set; } = false;
    }
}
