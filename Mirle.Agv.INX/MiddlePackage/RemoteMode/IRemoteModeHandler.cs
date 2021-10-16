using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.MiddlePackage.Umtc.RemoteMode
{
    interface IRemoteModeHandler : Tools.IMessageHandler, IMidRemoteModeAgent
    {
        event EventHandler<EnumAutoState> OnModeChangeEvent;

        void AgvcConnectionChanged(bool isConnection);

        void GetAutoState();
    }
}
