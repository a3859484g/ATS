using Mirle.Agv.MiddlePackage.Umtc.Model;
using Mirle.Agv.MiddlePackage.Umtc.Model.TransferSteps;
using Mirle.Agv.MiddlePackage.Umtc.Tools;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mirle.Agv.MiddlePackage.Umtc.Robot
{
    public class UtmcRobotHandler : IRobotHandler
    {
        public event EventHandler<CarrierSlotStatus> OnUpdateCarrierSlotStatusEvent;
        public event EventHandler<RobotStatus> OnUpdateRobotStatusEvent;
        public event EventHandler<EnumRobotEndType> OnRobotEndEvent;

        public event EventHandler<RobotCommand> DoRobotCommandEvent;
        public event EventHandler<MidRequestArgs> ClearRobotCommandEvent;
        public event EventHandler<CarrierSlotStatus> OnCarrierRenameEvent;
        public event EventHandler<CarrierSlotStatus> OnCarrierSlotStatusRequestEvent;
        public event EventHandler<RobotStatus> OnRobotStatusRequestEvent;
        public event EventHandler<MidRequestArgs> IsReadyForRobotCommandRequestEvent;
        
        public event EventHandler<object> OnRobotLoadCompleteEvent; //liu0407 LULComplete修改
        public event EventHandler<object> OnRobotUnloadCompleteEvent;

        public CarrierSlotStatus CarrierSlotLeft { get; set; } = new CarrierSlotStatus(EnumSlotNumber.L);
        public CarrierSlotStatus CarrierSlotRight { get; set; } = new CarrierSlotStatus(EnumSlotNumber.R);

        public RobotStatus RobotStatus { get; set; } = new RobotStatus();
        public void ClearRobotCommand()
        {
            MidRequestArgs requestArgs = new MidRequestArgs();
            ClearRobotCommandEvent?.Invoke(default, requestArgs);
            if (!requestArgs.IsOk)
            {
                HandlerLogError(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, $"RobotPackage can not clear robot command. {requestArgs.ErrorMsg}.");
            }
        }
        public void DoRobotCommandFor(RobotCommand robotCommand)
        {
            MidRequestArgs isReady = new MidRequestArgs();
            int retrycount = 0;
            while (!isReady.IsOk && retrycount <5)
            {
            IsReadyForRobotCommandRequestEvent?.Invoke(default, isReady);
                retrycount++;
                SpinWait.SpinUntil(() => false, 1000);
            }
            if (isReady.IsOk)
            {
                Task.Run(() =>
                {
                    DoRobotCommandEvent?.Invoke(default, robotCommand);
                });
            }
            else
            {
                throw new Exception($"RobotPackage is not ready for robot command. {isReady.ErrorMsg}.");
            }
        }
        public void GetRobotAndCarrierSlotStatus()
        {
            try
            {
                OnRobotStatusRequestEvent?.Invoke(default, RobotStatus);
                HandlerLogMsg(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, $"OnRobotStatusRequestEvent.[{RobotStatus.GetJsonInfo()}]");
                OnUpdateRobotStatusEvent?.Invoke(default, RobotStatus);

                OnCarrierSlotStatusRequestEvent?.Invoke(default, CarrierSlotLeft);
                HandlerLogMsg(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, $"OnCarrierSlotStatusRequestEvent.Left.[{CarrierSlotLeft.GetJsonInfo()}]");
                OnUpdateCarrierSlotStatusEvent?.Invoke(default, CarrierSlotLeft);

                OnCarrierSlotStatusRequestEvent?.Invoke(default, CarrierSlotRight);
                HandlerLogMsg(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, $"OnCarrierSlotStatusRequestEvent.Right.[{CarrierSlotRight.GetJsonInfo()}]");
                OnUpdateCarrierSlotStatusEvent?.Invoke(default, CarrierSlotRight);
            }
            catch (Exception ex)
            {
                HandlerLogError(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }
        public void CarrierRenameTo(CarrierSlotStatus carrierSlotStatus)
        {
            OnCarrierRenameEvent?.Invoke(default, carrierSlotStatus);
            HandlerLogMsg(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, $"OnCarrierRenameEvent.[{carrierSlotStatus.GetJsonInfo()}]");

        }
        public void ForkComplete(EnumRobotEndType robotEndType)
        {
            OnRobotEndEvent?.Invoke(this, robotEndType);
            HandlerLogMsg(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, $"OnRobotEndEvent.[{robotEndType.GetJsonInfo()}]");
        }

        public void LoadComplete() //liu 0407 LULComplete修改
        {
            OnRobotLoadCompleteEvent.Invoke(default, default);
            HandlerLogMsg(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, $"OnRobotLoadCompleteEvent.");
        }

        public void UnloadComplete() //liu 0407 LULComplete修改
        {
            OnRobotUnloadCompleteEvent.Invoke(default, default);
            HandlerLogMsg(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, $"OnRobotUnloadCompleteEvent.");
        }

        public void SetCarrierSlotStatus(CarrierSlotStatus carrierSlotStatus)
        {
            switch (carrierSlotStatus.SlotNumber)
            {
                case EnumSlotNumber.L:
                    CarrierSlotLeft = carrierSlotStatus;
                    OnUpdateCarrierSlotStatusEvent?.Invoke(default, CarrierSlotLeft);
                    break;
                case EnumSlotNumber.R:
                    CarrierSlotRight = carrierSlotStatus;
                    OnUpdateCarrierSlotStatusEvent?.Invoke(default, CarrierSlotRight);
                    break;
                default:
                    break;
            }          
        }

        public void SetRobotStatus(RobotStatus robotStatus)
        {
            RobotStatus = robotStatus;
            OnUpdateRobotStatusEvent?.Invoke(default, RobotStatus);
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
