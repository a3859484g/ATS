using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class AlarmCodeAndSetOrReset
    {
        public int AlarmCodeID { get; set; } = 0;
        public bool Trigger { get; set; } = false;
        public bool Change { get; set; } = false;

        public void SetAlarmOnOff(bool onOff)
        {
            if (onOff != Trigger)
            {
                Change = true;
                Trigger = onOff;
            }
            else
                Change = false;
        }
    }
}
