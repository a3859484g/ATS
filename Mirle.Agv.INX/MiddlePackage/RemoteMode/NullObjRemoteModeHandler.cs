using Mirle.Agv.MiddlePackage.Umtc.Model;
using Mirle.Agv.MiddlePackage.Umtc.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.MiddlePackage.Umtc.RemoteMode
{
    public class NullObjRemoteModeHandler : IRemoteModeHandler
    {
        public event EventHandler<EnumAutoState> OnModeChangeEvent;
        public event EventHandler<AutoStateArgs> OnAutoStateRequestEvent;
        public event EventHandler<bool> OnAgvcConnectionChangedEvent;

        public EnumAutoState AutoState { get; set; } = EnumAutoState.Manual;
        public void AgvcConnectionChanged(bool isConnection)
        {
            AutoState = EnumAutoState.Manual;
            OnModeChangeEvent?.Invoke(default, AutoState);
        }

        public void GetAutoState()
        {
            OnModeChangeEvent?.Invoke(default, AutoState);
        }

        public void SetAutoState(EnumAutoState autoState)
        {
            OnModeChangeEvent?.Invoke(this, autoState);          
        }

        private NLog.Logger _handlerLogger = NLog.LogManager.GetLogger("Package");

        public void HandlerLogMsg(string classMethodName, string msg)
        {
            _handlerLogger.Debug($"[{Model.Vehicle.Instance.SoftwareVersion}][{Model.Vehicle.Instance.AgvcConnectorConfig.ClientName}][{classMethodName}][{msg}]");
        }

        public void HandlerLogError(string classMethodName, string msg)
        {
            _handlerLogger.Error($"[{Model.Vehicle.Instance.SoftwareVersion}][{Model.Vehicle.Instance.AgvcConnectorConfig.ClientName}][{classMethodName}][{msg}]");
        }
    }
}
