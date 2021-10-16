using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class StageNumberToBarcodeReaderSetting
    {
        public int StageNumber { get; set; } = -1;
        public string BarcodeReaderMode { get; set; } = "";
        public double PixelToMM_X { get; set; } = 0;
        public double PixelToMM_Y { get; set; } = 0;
    }
}
