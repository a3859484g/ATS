using Mirle.Agv.MiddlePackage.Umtc;
using Mirle.Agv.MiddlePackage.Umtc.Controller;
using Mirle.Agv.MiddlePackage.Umtc.Model;
using Mirle.Agv.MiddlePackage.Umtc.Model.TransferSteps;
using Mirle.Agv.MiddlePackage.Umtc.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.MiddlePackage.Umtc.Robot
{
    public class NullObjRobotHandler : IRobotHandler
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

        public RobotStatus RobotStatus { get; set; }
        public CarrierSlotStatus CarrierSlotStatus { get; set; }

        public NullObjRobotHandler(RobotStatus robotStatus, CarrierSlotStatus carrierSlotStatus)
        {
            this.RobotStatus = robotStatus;
            this.CarrierSlotStatus = carrierSlotStatus;
        }

        public void ClearRobotCommand()
        {

        }

        public void DoRobotCommandFor(RobotCommand robotCommand)
        {
            Task.Run(() =>
            {
                if (robotCommand.GetTransferStepType() == EnumTransferStepType.Load)
                {
                    RobotStatus = new RobotStatus() { EnumRobotState = EnumRobotState.Busy, IsHome = false };

                    OnUpdateRobotStatusEvent?.Invoke(this, RobotStatus);
                    HandlerLogMsg(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, $"OnRobotStatusRequestEvent.[{RobotStatus.GetJsonInfo()}]");

                    System.Threading.Thread.Sleep(2000);

                    CarrierSlotStatus = new CarrierSlotStatus() { CarrierId = robotCommand.CassetteId, EnumCarrierSlotState = EnumCarrierSlotState.Loading, SlotNumber = robotCommand.SlotNumber };

                    OnUpdateCarrierSlotStatusEvent?.Invoke(this, CarrierSlotStatus);
                    HandlerLogMsg(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, $"OnCarrierSlotStatusRequestEvent.[{CarrierSlotStatus.GetJsonInfo()}]");

                    System.Threading.Thread.Sleep(2000);

                    RobotStatus = new RobotStatus() { EnumRobotState = EnumRobotState.Idle, IsHome = true };

                    OnUpdateRobotStatusEvent?.Invoke(this, RobotStatus);
                    HandlerLogMsg(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, $"OnRobotStatusRequestEvent.[{RobotStatus.GetJsonInfo()}]");

                    ForkComplete(EnumRobotEndType.Finished);
                }
                else if (robotCommand.GetTransferStepType() == EnumTransferStepType.Unload)
                {
                    RobotStatus = new RobotStatus() { EnumRobotState = EnumRobotState.Busy, IsHome = false };

                    OnUpdateRobotStatusEvent?.Invoke(this, RobotStatus);
                    HandlerLogMsg(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, $"OnRobotStatusRequestEvent.[{RobotStatus.GetJsonInfo()}]");

                    System.Threading.Thread.Sleep(2000);

                    CarrierSlotStatus = new CarrierSlotStatus() { CarrierId = "", EnumCarrierSlotState = EnumCarrierSlotState.Empty, SlotNumber = robotCommand.SlotNumber };

                    OnUpdateCarrierSlotStatusEvent?.Invoke(this, CarrierSlotStatus);
                    HandlerLogMsg(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, $"OnCarrierSlotStatusRequestEvent.[{CarrierSlotStatus.GetJsonInfo()}]");

                    System.Threading.Thread.Sleep(2000);

                    RobotStatus = new RobotStatus() { EnumRobotState = EnumRobotState.Idle, IsHome = true };

                    OnUpdateRobotStatusEvent?.Invoke(this, RobotStatus);
                    HandlerLogMsg(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, $"OnRobotStatusRequestEvent.[{RobotStatus.GetJsonInfo()}]");

                    ForkComplete(EnumRobotEndType.Finished);
                }
            });
        }

        public void GetRobotAndCarrierSlotStatus()
        {
            OnUpdateRobotStatusEvent?.Invoke(this, RobotStatus);
            OnUpdateCarrierSlotStatusEvent?.Invoke(this, CarrierSlotStatus);
        }

        public void CarrierRenameTo(CarrierSlotStatus carrierSlotStatus)
        {
            CarrierSlotStatus = carrierSlotStatus;
            OnUpdateCarrierSlotStatusEvent?.Invoke(this, CarrierSlotStatus);
        }

        public void ForkComplete(EnumRobotEndType robotEndType)
        {
            OnRobotEndEvent?.Invoke(default, robotEndType);
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
            CarrierSlotStatus = carrierSlotStatus;
            OnUpdateCarrierSlotStatusEvent?.Invoke(default, CarrierSlotStatus);
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
