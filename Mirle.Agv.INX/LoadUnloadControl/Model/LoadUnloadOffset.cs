using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class LoadUnloadOffset
    {
        public double BasedDistance { get; set; } = 0;

        public double LaserF_Right { get; set; } = 0;
        public double LaserF_Right_Locate { get; set; } = 0;
        public double LaserF_Right_Offset { get; set; } = 0;

        public double LaserB_Right { get; set; } = 0;
        public double LaserB_Right_Locate { get; set; } = 0;
        public double LaserB_Right_Offset { get; set; } = 0;

        public double LaserF_Left { get; set; } = 0;
        public double LaserF_Left_Locate { get; set; } = 0;
        public double LaserF_Left_Offset { get; set; } = 0;

        public double LaserB_Left { get; set; } = 0;
        public double LaserB_Left_Locate { get; set; } = 0;
        public double LaserB_Left_Offset { get; set; } = 0;

        public MapPosition BarcodeReader_Right { get; set; } = new MapPosition();
        public MapPosition BarcodeReader_Left { get; set; } = new MapPosition();

        public double BarcodeReader_Right_X_Delta { get; set; } = 0;
        public double BarcodeReader_Right_Y_Delta { get; set; } = 0;

        public double BarcodeReader_Right_Z { get; set; } = 0;

        public double BarcodeReader_Left_X_Delta { get; set; } = 0;
        public double BarcodeReader_Left_Y_Delta { get; set; } = 0;

        public double BarcodeReader_Left_Z { get; set; } = 0;
    }
}
