using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class LoadUnloadAxisData
    {
        public string Name { get; set; } = "";
        public double HomeOffset { get; set; } = 0;
        public double PlausUnit { get; set; } = 0;
        public double WorkingPosition { get; set; } = 0;
        public double AutoVelocity { get; set; } = 0;
        public double AutoAcceleration { get; set; } = 0;
        public double AutoDeceleration { get; set; } = 0;
        public double JogHigh { get; set; } = 0;
        public double JogNormal { get; set; } = 0;
        public double JogLow { get; set; } = 0;
        public double HomeVelocity_High { get; set; } = 0;
        public double HomeVelocity { get; set; } = 0;

        public double PosLimit { get; set; } = 1;
        public double NagLimit { get; set; } = -1;

        public Dictionary<EnumLoadUnloadJogSpeed, double> JogSpeed = new Dictionary<EnumLoadUnloadJogSpeed, double>();
    }
}
