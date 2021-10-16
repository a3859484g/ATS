using Mirle.Agv.MiddlePackage.Umtc.Tools;
using System;
using System.Threading.Tasks;

namespace Mirle.Agv.MiddlePackage.Umtc.RemoteMode
{
    public class UmtcRemoteModeHandler : RemoteMode.IRemoteModeHandler
    {
        public event EventHandler<Umtc.EnumAutoState> OnModeChangeEvent;

        public event EventHandler<Model.AutoStateArgs> OnAutoStateRequestEvent;
        public event EventHandler<bool> OnAgvcConnectionChangedEvent;

        public void AgvcConnectionChanged(bool isConnection)
        {
            Task.Run(() => OnAgvcConnectionChangedEvent?.Invoke(default, isConnection));
        }

        public void GetAutoState()
        {
            Model.AutoStateArgs autoStateArgs = new Model.AutoStateArgs();
            OnAutoStateRequestEvent?.Invoke(default, autoStateArgs);
            OnModeChangeEvent?.Invoke(default, autoStateArgs.AutoState);
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
