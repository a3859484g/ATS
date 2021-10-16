using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.MiddlePackage.Umtc.Model
{
    public class MoveCommandArgs : EventArgs
    {
        public MidRequestArgs RequestArgs { get; set; } = new MidRequestArgs();
        public string CommandId { get; set; } = "";
        public List<string> SectionIds { get; set; } = new List<string>();
        public List<string> AddressIds { get; set; } = new List<string>();
    }
}
