using Mirle.Agv.MiddlePackage.Umtc.Model;
using Mirle.Agv.MiddlePackage.Umtc.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.MiddlePackage.Umtc.Robot
{
     interface IRobotHandler : IMessageHandler,IMidRobotAgent
    {
         event EventHandler<Model.CarrierSlotStatus> OnUpdateCarrierSlotStatusEvent;
         event EventHandler<Model.RobotStatus> OnUpdateRobotStatusEvent;
         event EventHandler<EnumRobotEndType> OnRobotEndEvent;
         event EventHandler<object> OnRobotLoadCompleteEvent; //liu0407 LULComplete修改
         event EventHandler<object> OnRobotUnloadCompleteEvent;

        void DoRobotCommandFor(Model.TransferSteps.RobotCommand robotCommand);
         void ClearRobotCommand();
         void GetRobotAndCarrierSlotStatus();
         void CarrierRenameTo(Model.CarrierSlotStatus carrierSlotStatus);

       
    }
}
