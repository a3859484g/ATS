using Mirle.Agv.MiddlePackage.Umtc.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.MiddlePackage.Umtc.Robot
{
    interface IMidRobotAgent
    {
        event EventHandler<Model.TransferSteps.RobotCommand> DoRobotCommandEvent;
        event EventHandler<Model.MidRequestArgs> ClearRobotCommandEvent;
        event EventHandler<Model.CarrierSlotStatus> OnCarrierRenameEvent;

        event EventHandler<Model.CarrierSlotStatus> OnCarrierSlotStatusRequestEvent;
        event EventHandler<Model.RobotStatus> OnRobotStatusRequestEvent;
        event EventHandler<Model.MidRequestArgs> IsReadyForRobotCommandRequestEvent;

        void ForkComplete(EnumRobotEndType robotEndType);
        void LoadComplete();
        void UnloadComplete();
        void SetCarrierSlotStatus(CarrierSlotStatus carrierSlotStatus);
        void SetRobotStatus(RobotStatus robotStatus);
    }
}
