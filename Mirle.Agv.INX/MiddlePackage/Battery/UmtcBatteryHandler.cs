using Mirle.Agv.MiddlePackage.Umtc.Model;
using Mirle.Agv.MiddlePackage.Umtc.Tools;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mirle.Agv.MiddlePackage.Umtc.Battery
{
    public class UmtcBatteryHandler : IBatteryHandler
    {
        public event EventHandler<BatteryStatus> OnUpdateBatteryStatusEvent;
        public event EventHandler<ChargeArgs> StartChargeEvent;
        public event EventHandler<MidRequestArgs> StopChargeEvent;
        public event EventHandler<BatteryStatus> OnBatteryStatusRequestEvent;

        public BatteryStatus BatteryStatus { get; set; } = new BatteryStatus();

        public void GetBatteryAndChargeStatus()
        {
            OnBatteryStatusRequestEvent?.Invoke(default, BatteryStatus);
            OnUpdateBatteryStatusEvent?.Invoke(default, BatteryStatus);
        }

        public void SetPercentageTo(int percentage)
        {
            //TODO : LocalPackage.MainFlowHandler.SetPercentageTo(percentage);
        }

        public void StartCharge(EnumAddressDirection chargeDirection)
        {
            Vehicle.Instance.CheckStartChargeReplyEnd = false;

            OnBatteryStatusRequestEvent?.Invoke(default, BatteryStatus);

            if (!BatteryStatus.IsCharging)
            {
                Task.Run(() =>
                {
                    ChargeArgs chargeArgs = new ChargeArgs() { AddressId = Vehicle.Instance.MoveStatus.LastAddress.Id };
                    StartChargeEvent?.Invoke(default, chargeArgs);
                    SpinWait.SpinUntil(() => chargeArgs.RequestArgs.IsOk, 30 * 1000);
                    OnBatteryStatusRequestEvent?.Invoke(default, BatteryStatus);
                    OnUpdateBatteryStatusEvent?.Invoke(default, BatteryStatus);

                    if (!chargeArgs.RequestArgs.IsOk)
                    {
                        HandlerLogError(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, $"BatteryPackage start charge fail. {chargeArgs.RequestArgs.ErrorMsg}.");
                    }

                    Vehicle.Instance.CheckStartChargeReplyEnd = true;
                });               
            }
            else
            {
                Vehicle.Instance.CheckStartChargeReplyEnd = true;
            }
        }

        public void StopCharge()
        {
            Vehicle.Instance.CheckStopChargeReplyEnd = false;
            OnBatteryStatusRequestEvent?.Invoke(default, BatteryStatus);
            if (BatteryStatus.IsCharging)
            {
                Task.Run(() =>
                {

                    MidRequestArgs requestArgs = new MidRequestArgs();
                    StopChargeEvent?.Invoke(default, requestArgs);
                    SpinWait.SpinUntil(() => requestArgs.IsOk, Vehicle.Instance.MainFlowConfig.StopChargeWaitingTimeoutMs);
                    if (requestArgs.IsOk)
                    {
                        OnBatteryStatusRequestEvent?.Invoke(default, BatteryStatus);
                        OnUpdateBatteryStatusEvent?.Invoke(default, BatteryStatus);
                    }
                    else
                    {
                        HandlerLogError(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, $"BatteryPackage stop charge fail. {requestArgs.ErrorMsg}.");
                    }
                    Vehicle.Instance.CheckStopChargeReplyEnd = true;
                });
            }
            else
            {
                Vehicle.Instance.CheckStopChargeReplyEnd = true;
            }
        }

        public void SetBatteryStatus(BatteryStatus batteryStatus)
        {
            this.BatteryStatus = batteryStatus;
            OnUpdateBatteryStatusEvent?.Invoke(default, BatteryStatus);
        }

        public void SetHighPercentageThreshold(int thd)
        {
            Vehicle.Instance.MainFlowConfig.HighPowerPercentage = thd;
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
