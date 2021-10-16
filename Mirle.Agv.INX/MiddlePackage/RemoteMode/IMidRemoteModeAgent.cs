using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.MiddlePackage.Umtc.RemoteMode
{
    interface IMidRemoteModeAgent
    {
        event EventHandler<Model.AutoStateArgs> OnAutoStateRequestEvent;
        event EventHandler<bool> OnAgvcConnectionChangedEvent;

        void SetAutoState(EnumAutoState autoState);
    }
}
