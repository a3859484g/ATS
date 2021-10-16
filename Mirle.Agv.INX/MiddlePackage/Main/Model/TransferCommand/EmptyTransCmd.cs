using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mirle.Agv.MiddlePackage.Umtc.Controller;

namespace Mirle.Agv.MiddlePackage.Umtc.Model.TransferSteps
{

    public class EmptyTransferStep : TransferStep
    {
        public EmptyTransferStep() : this("")
        {
        }
        public EmptyTransferStep(string cmdId) : base(cmdId) => type = EnumTransferStepType.Empty;
    }
}
