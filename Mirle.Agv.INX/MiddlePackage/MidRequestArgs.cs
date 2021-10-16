using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.MiddlePackage.Umtc.Model
{
    public class MidRequestArgs : EventArgs
    {
        public bool IsOk { get; set; } = false;
        public string ErrorMsg { get; set; } = "";
    }
}
