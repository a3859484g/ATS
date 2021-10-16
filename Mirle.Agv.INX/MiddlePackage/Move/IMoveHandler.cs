using Mirle.Agv.MiddlePackage.Umtc.Model;
using Mirle.Agv.MiddlePackage.Umtc.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.MiddlePackage.Umtc.Move
{
    interface IMoveHandler : Tools.IMessageHandler, IMidMoveAgent
    {
        event EventHandler<Model.AddressArrivalArgs> OnUpdateAddressArrivalArgsEvent;

        event EventHandler<bool> OnOpPauseOrResumeEvent;

        event EventHandler<string> OnSectionPassEvent;

        void SetMovingGuide(Model.MovingGuide movingGuide);
        void ReserveOkPartMove(Model.MapSection mapSection);
        void StopMove(EnumMoveStopType StopType); //liu
        void PauseMove();
        void ResumeMove();
        void GetAddressArrivalArgs();
        void InitialPosition();
        bool AskReadyForMoveCommandRequest();//liu
    }
}
