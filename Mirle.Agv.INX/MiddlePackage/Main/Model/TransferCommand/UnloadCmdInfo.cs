using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mirle.Agv.MiddlePackage.Umtc.Controller;

namespace Mirle.Agv.MiddlePackage.Umtc.Model.TransferSteps
{

    public class UnloadCmdInfo : RobotCommand
    {
        public UnloadCmdInfo(AgvcTransferCommand transferCommand) : base(transferCommand)
        {
            this.type = EnumTransferStepType.Unload;
            this.PortAddressId = transferCommand.UnloadAddressId;
        }
    }
}
