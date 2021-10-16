using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class StageData
    {
        public string Name { get; set; } = "";
        public EnumStageDirection Direction { get; set; } = EnumStageDirection.None;
        public int StargeNumber { get; set; } = 0;
        public double Benchmark_P { get; set; } = 0;
        public double Benchmark_Y { get; set; } = 0;
        public double Benchmark_Theta { get; set; } = 0;
        public double Benchmark_Z { get; set; } = 0;

        public double Encoder_P { get; set; } = 0;
        public double Encoder_Y { get; set; } = 0;
        public double Encoder_Theta { get; set; } = 0;
        public double Encoder_Z { get; set; } = 0;
    }
}
