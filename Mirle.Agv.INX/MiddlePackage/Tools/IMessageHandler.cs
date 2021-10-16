using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.MiddlePackage.Umtc.Tools
{
    interface IMessageHandler
    {
        void HandlerLogMsg(string classMethodName, string msg);
        void HandlerLogError(string classMethodName, string msg);
    }
}
