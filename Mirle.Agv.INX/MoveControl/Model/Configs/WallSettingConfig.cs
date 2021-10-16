using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class WallSettingConfig
    {
        public double MaxInterval { get; set; } = 5000;
        public double MinInterval { get; set; } = 50;
        public double InWallInterval { get; set; } = 200;
        public int SleepTime { get; set; } = 50;
        public int SleepTimeMin { get; set; } = 5;
        public double MapScale { get; set; } = 0.05;
        public double MapBorderLength { get; set; } = 2000;
    }
}
