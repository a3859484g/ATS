using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class AlignmentValueData
    {
        /// 原始資料.
        /// 
        public double LaserF_Origin { get; set; } = 0;
        public double LaserB_Origin { get; set; } = 0;
        
        public double LaserF { get; set; } = 0;
        public double LaserB { get; set; } = 0;

        public MapPosition BarcodePosition { get; set; } = new MapPosition();
        public MapPosition OriginBarodePosition { get; set; } = new MapPosition();

        public string BarcodeNumber { get; set; } = "";

        public EnumStageDirection Direction { get; set; } = EnumStageDirection.None;
        public int StageNumber { get; set; } = 0;

        public double LASER_P { get; set; } = 0;  //Liu++
        public double P { get; set; } = 0;
        public double Y { get; set; } = 0;
        public double Theta { get; set; } = 0;
        public double Z { get; set; } = 0;
        public bool AlignmentVlaue { get; set; } = false;
    }
}
