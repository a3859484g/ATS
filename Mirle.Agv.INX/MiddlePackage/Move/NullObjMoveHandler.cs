using Mirle.Agv.MiddlePackage.Umtc.Model;
using Mirle.Agv.MiddlePackage.Umtc.Tools;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mirle.Agv.MiddlePackage.Umtc.Move
{
    public class NullObjMoveHandler : IMoveHandler
    {
        public event EventHandler<AddressArrivalArgs> OnUpdateAddressArrivalArgsEvent;
        public event EventHandler<bool> OnOpPauseOrResumeEvent;

        public event EventHandler<MoveCommandArgs> SetupMoveCommandInfoEvent;
        public event EventHandler<string> ReservePartMoveEvent;
        public event EventHandler<MidRequestArgs> IsReadyForMoveCommandRequestEvent;
        public event EventHandler<AddressArrivalArgs> OnAddressArrivalArgsRequestEvent;
        public event EventHandler<MidRequestArgs> PauseMoveEvent;
        public event EventHandler<MidRequestArgs> ResumeMoveEvent;
        public event EventHandler<MidRequestArgs> CancelMoveEvent;
        public event EventHandler<string> OnSectionPassEvent;

        public MapInfo MapInfo { get; set; }
        public MovingGuide MovingGuide { get; set; }
        public AddressArrivalArgs AddressArrivalArgs { get; set; }
        public bool IsFakeMoveEnginePause { get; set; }
        public Task FakeMoveEngineTask { get; set; }
        public ConcurrentQueue<AddressArrivalArgs> FakeMoveArrivalQueue { get; set; }

        public NullObjMoveHandler(MapInfo mapInfo)
        {
            this.MapInfo = mapInfo;
            FakeMoveArrivalQueue = new ConcurrentQueue<AddressArrivalArgs>();
            IsFakeMoveEnginePause = false;
            FakeMoveEngineTask = new Task(FakeMoveEngine);
            FakeMoveEngineTask.Start();
            InitialAddressArrivalArgs();
        }

        private void InitialAddressArrivalArgs()
        {
            var section = MapInfo.sectionMap.First().Value;
            AddressArrivalArgs = new AddressArrivalArgs()
            {
                MapSectionID = section.Id,
                MapAddressID = section.HeadAddress.Id,
                MapPosition = section.HeadAddress.Position,
                VehicleDistanceSinceHead = 0,
                HeadAngle = (int)section.HeadAddress.VehicleHeadAngle
            };
        }

        public void GetAddressArrivalArgs()
        {
            OnUpdateAddressArrivalArgsEvent?.Invoke(default, AddressArrivalArgs);
        }

        public void SetMovingGuide(MovingGuide movingGuide)
        {
            MovingGuide = movingGuide;
        }

        public void ReserveOkPartMove(MapSection mapSection)
        {
            try
            {
                int sectionIndex = MovingGuide.GuideSectionIds.FindIndex(x => x.Trim() == mapSection.Id.Trim());
                if (sectionIndex < 0)
                {
                    throw new Exception($"ReserveOkPartMove fail.[{mapSection.Id.Trim()}].[{MovingGuide.GuideSectionIds.GetJsonInfo()}]");
                }
                bool isEndSection = sectionIndex == MovingGuide.GuideSectionIds.Count - 1;

                AddressArrivalArgs positionArgs = new AddressArrivalArgs();
                positionArgs.EnumAddressArrival = isEndSection ? EnumAddressArrival.EndArrival : EnumAddressArrival.Arrival;
                positionArgs.EnumMoveState = isEndSection ? EnumMoveState.Idle : EnumMoveState.Busy;                
                positionArgs.MapSectionID = mapSection.Id;                
                var address = mapSection.CmdDirection == EnumCommandDirection.Backward ? mapSection.HeadAddress : mapSection.TailAddress;
                positionArgs.MapAddressID = address.Id;
                positionArgs.MapPosition = address.Position;
                positionArgs.VehicleDistanceSinceHead = mapSection.CmdDirection == EnumCommandDirection.Backward ? 0 : mapSection.HeadToTailDistance;
                positionArgs.HeadAngle = (int)address.VehicleHeadAngle;

                FakeMoveArrivalQueue.Enqueue(positionArgs);
            }
            catch (Exception ex)
            {
                HandlerLogError(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        public void PauseMove()
        {
            IsFakeMoveEnginePause = true;
            if (AddressArrivalArgs.EnumMoveState == EnumMoveState.Busy)
            {
                AddressArrivalArgs.EnumAddressArrival = EnumAddressArrival.Arrival;
                AddressArrivalArgs.EnumMoveState = EnumMoveState.Pause;
                OnUpdateAddressArrivalArgsEvent?.Invoke(default, AddressArrivalArgs);
            }
        }

        public void ResumeMove()
        {
            if (AddressArrivalArgs.EnumMoveState == EnumMoveState.Pause)
            {
                AddressArrivalArgs.EnumAddressArrival = EnumAddressArrival.Arrival;
                AddressArrivalArgs.EnumMoveState = EnumMoveState.Busy;
                OnUpdateAddressArrivalArgsEvent?.Invoke(default, AddressArrivalArgs);
            }
            IsFakeMoveEnginePause = false;
        }

        public void StopMove(EnumMoveStopType StopType)
        {
            FakeMoveArrivalQueue = new ConcurrentQueue<AddressArrivalArgs>();

            if (AddressArrivalArgs.EnumMoveState == EnumMoveState.Busy)
            {
                AddressArrivalArgs.EnumAddressArrival = EnumAddressArrival.Fail;
                AddressArrivalArgs.EnumMoveState = EnumMoveState.Error;
            }
            else
            {
                AddressArrivalArgs.EnumAddressArrival = EnumAddressArrival.Arrival;
                AddressArrivalArgs.EnumMoveState = EnumMoveState.Idle;
            }
            FakeMoveArrivalQueue.Enqueue(AddressArrivalArgs);
        }

        private void FakeMoveEngine()
        {
            while (true)
            {
                if (IsFakeMoveEnginePause)
                {
                    SpinWait.SpinUntil(() => !IsFakeMoveEnginePause, 1000);
                    continue;
                }

                try
                {
                    if (FakeMoveArrivalQueue.Any())
                    {
                        FakeMoveArrivalQueue.TryDequeue(out AddressArrivalArgs addressArrivalArgs);                       
                        AddressArrivalArgs = addressArrivalArgs;
                        OnUpdateAddressArrivalArgsEvent?.Invoke(this, AddressArrivalArgs);
                    }
                    else
                    {
                        FakeMoveArrivalQueue.Enqueue(AddressArrivalArgs);
                    }
                }
                catch (Exception ex)
                {
                    HandlerLogError(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message);
                }

                SpinWait.SpinUntil(() => false, 5000);
            }
        }

        public void InitialPosition()
        {
            FakeMoveArrivalQueue.Enqueue(AddressArrivalArgs);
        }

        public void PassAddress(string addressId)
        {
            try
            {
                MapAddress address = Vehicle.Instance.MapInfo.addressMap[addressId];
                AddressArrivalArgs.EnumAddressArrival = EnumAddressArrival.Arrival;
                AddressArrivalArgs.MapPosition = address.Position;


                OnUpdateAddressArrivalArgsEvent?.Invoke(default, AddressArrivalArgs);
            }
            catch (Exception ex)
            {
                HandlerLogError(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        public void MoveComplete(AddressArrivalArgs arrivalArgs)
        {
            try
            {
                AddressArrivalArgs = arrivalArgs;
                OnUpdateAddressArrivalArgsEvent?.Invoke(default, AddressArrivalArgs);
            }
            catch (Exception ex)
            {
                HandlerLogError(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }
        public bool AskReadyForMoveCommandRequest()
        {
            return false;
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
