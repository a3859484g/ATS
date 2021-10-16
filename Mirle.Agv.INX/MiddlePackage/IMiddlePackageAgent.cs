using Mirle.Agv.MiddlePackage.Umtc.Battery;
using Mirle.Agv.MiddlePackage.Umtc.Model;
using Mirle.Agv.MiddlePackage.Umtc.Model.TransferSteps;
using System;

namespace Mirle.Agv.MiddlePackage.Umtc
{
    interface IMiddlePackageAgent : Robot.IMidRobotAgent, Battery.IMidBatteryAgent, Move.IMidMoveAgent, RemoteMode.IMidRemoteModeAgent, Alarms.IMidAlarmAgent
    {
        Model.Vehicle GetVehicle();
    }

    abstract class MiddlePackageAgent : IMiddlePackageAgent
    {
        public abstract event EventHandler<RobotCommand> DoRobotCommandEvent;
        public abstract event EventHandler<MidRequestArgs> ClearRobotCommandEvent;
        public abstract event EventHandler<CarrierSlotStatus> OnCarrierRenameEvent;
        public abstract event EventHandler<CarrierSlotStatus> OnCarrierSlotStatusRequestEvent;
        public abstract event EventHandler<RobotStatus> OnRobotStatusRequestEvent;

        public abstract event EventHandler<MidRequestArgs> IsReadyForRobotCommandRequestEvent;
        public abstract event EventHandler<ChargeArgs> StartChargeEvent;
        public abstract event EventHandler<MidRequestArgs> StopChargeEvent;
        public abstract event EventHandler<BatteryStatus> OnBatteryStatusRequestEvent;

        public abstract event EventHandler<MoveCommandArgs> SetupMoveCommandInfoEvent;
        public abstract event EventHandler<string> ReservePartMoveEvent;
        public abstract event EventHandler<MidRequestArgs> IsReadyForMoveCommandRequestEvent;
        public abstract event EventHandler<AddressArrivalArgs> OnAddressArrivalArgsRequestEvent;
        public abstract event EventHandler<MidRequestArgs> PauseMoveEvent;
        public abstract event EventHandler<MidRequestArgs> ResumeMoveEvent;
        public abstract event EventHandler<MidRequestArgs> CancelMoveEvent;

        public abstract event EventHandler<AutoStateArgs> OnAutoStateRequestEvent;
        public abstract event EventHandler<bool> OnAgvcConnectionChangedEvent;
        public abstract event EventHandler<AlarmArgs> OnMiddlePackageSetAlarmEvent;
        public abstract event EventHandler OnMiddlePackageResetAlarmEvent;

        public abstract void ForkComplete(EnumRobotEndType robotEndType);
        public abstract Vehicle GetVehicle();
        public abstract void MoveComplete(AddressArrivalArgs arrivalArgs);
        public abstract void PassAddress(string addressId);
        public abstract void ResetAlarmFromLocalPackage();
        public abstract void SetAlarmFromLocalPackage(AlarmArgs alarmArgs);
        public abstract void SetAutoState(EnumAutoState autoState);
        public abstract void SetBatteryStatus(BatteryStatus batteryStatus);
        public abstract void SetCarrierSlotStatus(CarrierSlotStatus carrierSlotStatus);
        public abstract void SetHighPercentageThreshold(int thd);
        public abstract void SetRobotStatus(RobotStatus robotStatus);
        public abstract void LoadComplete();
        public abstract void UnloadComplete();
    }
}
