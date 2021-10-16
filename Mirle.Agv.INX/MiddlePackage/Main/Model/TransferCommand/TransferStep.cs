using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mirle.Agv.MiddlePackage.Umtc.Controller;
using Mirle.Agv.MiddlePackage.Umtc.Model.Configs;

namespace Mirle.Agv.MiddlePackage.Umtc.Model.TransferSteps
{

    public abstract class TransferStep
    {
        protected EnumTransferStepType type = EnumTransferStepType.Empty;
        public string CmdId { get; set; } = "";

        public TransferStep(string cmdId)
        {
            this.CmdId = cmdId;
        }

        public EnumTransferStepType GetTransferStepType() { return type; }
    }
}
