using Mirle.Agv.MiddlePackage.Umtc.Model;
using Mirle.Agv.MiddlePackage.Umtc.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mirle.Agv.MiddlePackage.Umtc.Battery
{
    public class NullObjBatteryHandler : IBatteryHandler
    {
        public event EventHandler<BatteryStatus> OnUpdateBatteryStatusEvent;

        public event EventHandler<ChargeArgs> StartChargeEvent;
        public event EventHandler<MidRequestArgs> StopChargeEvent;
        public event EventHandler<BatteryStatus> OnBatteryStatusRequestEvent;

        public BatteryStatus BatteryStatus { get; set; }

        public NullObjBatteryHandler(BatteryStatus batteryStatus)
        {
            this.BatteryStatus = batteryStatus;
        }

        public void SetPercentageTo(int percentage)
        {
            BatteryStatus.Percentage = percentage;
            OnUpdateBatteryStatusEvent?.Invoke(this, BatteryStatus);
        }

        public void StartCharge(EnumAddressDirection chargeDirection)
        {
            Vehicle.Instance.CheckStartChargeReplyEnd = false;
            Task.Run(() =>
            {
                try
                {
                    SpinWait.SpinUntil(() => false, 2000);
                    BatteryStatus.IsCharging = true;
                    OnUpdateBatteryStatusEvent?.Invoke(this, BatteryStatus);

                    while (BatteryStatus.Percentage < 100 && BatteryStatus.IsCharging)
                    {
                        SpinWait.SpinUntil(() => false, 2000);

                        BatteryStatus.Percentage = Math.Min(BatteryStatus.Percentage + 10, 100);

                        OnUpdateBatteryStatusEvent?.Invoke(this, BatteryStatus);
                    }
                }
                catch (Exception ex)
                {
                    HandlerLogError(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message);
                }

                SpinWait.SpinUntil(() => false, 2000);

                BatteryStatus.IsCharging = false;
                OnUpdateBatteryStatusEvent?.Invoke(this, BatteryStatus);
                Vehicle.Instance.CheckStartChargeReplyEnd = true;
            });
        }

        public void StopCharge()
        {
            if (BatteryStatus.IsCharging)
            {
                Task.Run(() =>
                {
                    SpinWait.SpinUntil(() => false, 2000);
                    BatteryStatus.IsCharging = false;
                });
            }
            else
            {
                OnUpdateBatteryStatusEvent?.Invoke(this, BatteryStatus);
            }
        }

        public void GetBatteryAndChargeStatus()
        {
            OnUpdateBatteryStatusEvent?.Invoke(this, BatteryStatus);          
        }

        public void SetBatteryStatus(BatteryStatus batteryStatus)
        {
            this.BatteryStatus = batteryStatus;
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
