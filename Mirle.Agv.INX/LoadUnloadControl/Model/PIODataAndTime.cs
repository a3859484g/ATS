using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class PIODataAndTime
    {
        public DateTime Time { get; set; }
        public uint Input { get; set; }
        public uint Output { get; set; }

        public PIODataAndTime(uint input, uint output)
        {
            Time = DateTime.Now;
            Input = input;
            Output = output;
        }
    }
}
