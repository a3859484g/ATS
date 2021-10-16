using System;

namespace Mirle.Agv.MiddlePackage.Umtc.Model
{
    public class Alarm
    {
        public int Id { get; set; }
        public string AlarmText { get; set; } = "Unknow";
        public EnumAlarmLevel Level { get; set; } = EnumAlarmLevel.Warn;
        public string Description { get; set; } = "Unknow";
        public DateTime SetTime { get; set; }
        public DateTime ResetTime { get; set; }

    }
}
