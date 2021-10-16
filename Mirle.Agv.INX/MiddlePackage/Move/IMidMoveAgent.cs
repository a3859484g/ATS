using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.MiddlePackage.Umtc.Move
{
    interface IMidMoveAgent
    {
        event EventHandler<Model.MoveCommandArgs> SetupMoveCommandInfoEvent;
        event EventHandler<string> ReservePartMoveEvent;
        event EventHandler<Model.MidRequestArgs> IsReadyForMoveCommandRequestEvent;
        event EventHandler<Model.AddressArrivalArgs> OnAddressArrivalArgsRequestEvent;

        event EventHandler<Model.MidRequestArgs> PauseMoveEvent;
        event EventHandler<Model.MidRequestArgs> ResumeMoveEvent;
        event EventHandler<Model.MidRequestArgs> CancelMoveEvent;

        void PassAddress(string addressId);
        void MoveComplete(Model.AddressArrivalArgs arrivalArgs);
    } 
}
