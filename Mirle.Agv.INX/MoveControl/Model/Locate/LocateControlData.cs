using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class LocateControlData
    {
        public LocateAGVPosition LocateAGVPosition { get; set; } = null;
        public MapAGVPosition SlamOriginPosition { get; set; } = null;
        public LocateAGVPosition SelectOrder { get; set; } = null;

        public bool SetMIPCPositionOK { get; set; } = false;
        public bool SlamLocateOK { get; set; } = false;
        public MapAGVPosition AutoSetSlamPositionData { get; set; } = null;
        public EnumSlamAutoSetPosition AutoSetPositionStatus { get; set; } = EnumSlamAutoSetPosition.WaitMIPCSlamData;
    }
}
