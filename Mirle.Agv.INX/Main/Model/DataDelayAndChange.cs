using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class DataDelayAndChange
    {
        private EnumDelayType type;
        private double delayTime = 0;

        public bool data = false;

        public bool Change = false;

        private Stopwatch delayTimer = new Stopwatch();

        private bool delaying = false;

        public bool Data
        {
            get
            {
                if (delaying)
                {
                    if (delayTimer.ElapsedMilliseconds > delayTime)
                    {
                        delaying = false;
                        delayTimer.Stop();
                        return data;
                    }
                    else
                    {
                        switch (type)
                        {
                            case EnumDelayType.OnDelay:
                                return true;
                            case EnumDelayType.OffDelay:
                                return false;
                            default:
                                return data;
                        }
                    }
                }
                else
                    return data;
            }

            set
            {
                if (value != data)
                {
                    switch (type)
                    {
                        case EnumDelayType.OnDelay:
                            if (!value)
                            {
                                delayTimer.Restart();
                                delaying = true;
                            }
                            else
                            {
                                delayTimer.Stop();
                                delaying = false;
                            }

                            break;
                        case EnumDelayType.OffDelay:
                            if (value)
                            {
                                delayTimer.Restart();
                                delaying = true;
                            }
                            else
                            {
                                delayTimer.Stop();
                                delaying = false;
                            }

                            break;
                        default:
                            break;
                    }

                    data = value;
                    Change = true;
                }
                else
                    Change = false;
            }
        }

        public DataDelayAndChange(double delayTime, EnumDelayType type)
        {
            this.delayTime = delayTime;
            this.type = type;

            if (type == EnumDelayType.OffDelay)
                data = true;
        }
    }
}
