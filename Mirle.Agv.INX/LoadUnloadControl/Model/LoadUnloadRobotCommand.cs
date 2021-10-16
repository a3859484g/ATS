using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class LoadUnloadRobotCommand
    {
        public string Id;
        public string Name;
        public float Port1Load { get; set; } = 0;
        public float Port1Unload { get; set; } = 0;
        public float Port2Load { get; set; } = 0;
        public float Port2Unload { get; set; } = 0;
        //public Dictionary<string, LoadUnloadRobotCommand> LDULDRobotCommand = new Dictionary<string, LoadUnloadRobotCommand>();

    }
}