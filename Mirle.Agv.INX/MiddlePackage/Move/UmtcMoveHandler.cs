using Mirle.Agv.MiddlePackage.Umtc.Model;
using Mirle.Agv.MiddlePackage.Umtc.Tools;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mirle.Agv.MiddlePackage.Umtc.Move
{
    public class UmtcMoveHandler : IMoveHandler
    {
        public event EventHandler<AddressArrivalArgs> OnUpdateAddressArrivalArgsEvent;
        public event EventHandler<bool> OnOpPauseOrResumeEvent;

        public event EventHandler<MoveCommandArgs> SetupMoveCommandInfoEvent;
        public event EventHandler<string> ReservePartMoveEvent;
        public event EventHandler<MidRequestArgs> IsReadyForMoveCommandRequestEvent;
        public event EventHandler<MidRequestArgs> PauseMoveEvent;
        public event EventHandler<MidRequestArgs> ResumeMoveEvent;
        public event EventHandler<MidRequestArgs> CancelMoveEvent;
        public event EventHandler<AddressArrivalArgs> OnAddressArrivalArgsRequestEvent;
        public event EventHandler<string> OnSectionPassEvent;

        public AddressArrivalArgs AddressArrivalArgs { get; set; } = new AddressArrivalArgs();

        public Task TracAddressEngineTask { get; set; }
        public bool IsTracAddressEnginePause { get; set; }

        public ConcurrentQueue<AddressArrivalArgs> TracAddressQueue { get; set; } = new ConcurrentQueue<AddressArrivalArgs>();

        public UmtcMoveHandler()
        {
            IsTracAddressEnginePause = false;
            TracAddressEngineTask = new Task(TracAddressEngine);
            TracAddressEngineTask.Start();
        }

        public void GetAddressArrivalArgs()
        {
            OnAddressArrivalArgsRequestEvent?.Invoke(default, AddressArrivalArgs);
            OnUpdateAddressArrivalArgsEvent?.Invoke(default, AddressArrivalArgs);
        }

        public void InitialPosition()
        {
            //TODO Set Initial Position for simulator
            GetAddressArrivalArgs();
        }

        public void SetMovingGuide(MovingGuide movingGuide)
        {
            MidRequestArgs isReady = new MidRequestArgs();

            #region 原本寫法.
            //IsReadyForMoveCommandRequestEvent?.Invoke(default, isReady);
            //SpinWait.SpinUntil(() => isReady.IsOk, 30 * 1000);
            #endregion

            #region 修改後.
            Stopwatch timer = new Stopwatch();
            IsReadyForMoveCommandRequestEvent?.Invoke(default, isReady);

            while (!isReady.IsOk && timer.ElapsedMilliseconds < 3 * 1000)
            {
                Thread.Sleep(100);
                IsReadyForMoveCommandRequestEvent?.Invoke(default, isReady);
            }
            #endregion

            if (isReady.IsOk)
            {
                MoveCommandArgs moveCommandArgs = new MoveCommandArgs()
                {
                    CommandId = Model.Vehicle.Instance.TransferCommand.CommandId,
                    SectionIds = movingGuide.GuideSectionIds,
                    AddressIds = movingGuide.GuideAddressIds
                };

                SetupMoveCommandInfoEvent?.Invoke(default, moveCommandArgs);
                //SpinWait.SpinUntil(() => moveCommandArgs.RequestArgs.IsOk, 20 * 1000);

                if (moveCommandArgs.RequestArgs.IsOk)
                {
                    HandlerLogMsg(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, $"MovePackage accept move command.");
                }
                else
                {
                    //HandlerLogMsg(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, $"MovePackage move command NG.");
                    throw new Exception($"MovePackage reject move command. {moveCommandArgs.RequestArgs.ErrorMsg}");
                }
            }
            else
            {
                throw new Exception($"MovePackage is not ready for setting moving guide. {isReady.ErrorMsg}.");
            }
        }

        public void ReserveOkPartMove(MapSection mapSection)
        {
            ReservePartMoveEvent?.Invoke(default, mapSection.Id);
        }

        public void PauseMove()
        {
            MidRequestArgs requestArgs = new MidRequestArgs();
            PauseMoveEvent?.Invoke(default, requestArgs);
            SpinWait.SpinUntil(() => requestArgs.IsOk, 10 * 1000);
            if (requestArgs.IsOk)
            {
                if (AddressArrivalArgs.EnumMoveState == EnumMoveState.Busy)
                {
                    AddressArrivalArgs.EnumMoveState = EnumMoveState.Pause;
                    OnUpdateAddressArrivalArgsEvent?.Invoke(default, AddressArrivalArgs);
                }
            }
            else
            {
                throw new Exception($"MovePackage can not pause move. {requestArgs.ErrorMsg}.");
            }
        }

        public void ResumeMove()
        {
            MidRequestArgs requestArgs = new MidRequestArgs();
            ResumeMoveEvent?.Invoke(default, requestArgs);
            SpinWait.SpinUntil(() => requestArgs.IsOk, 10 * 1000);
            if (requestArgs.IsOk)
            {
                if (AddressArrivalArgs.EnumMoveState == EnumMoveState.Pause)
                {
                    AddressArrivalArgs.EnumMoveState = EnumMoveState.Busy;
                    OnUpdateAddressArrivalArgsEvent?.Invoke(default, AddressArrivalArgs);
                }
            }
            else
            {
                throw new Exception($"MovePackage can not resume move. {requestArgs.ErrorMsg}.");
            }
        }

        public void StopMove(EnumMoveStopType StopType)
        {
            Task.Run(() =>
            {
                MidRequestArgs requestArgs = new MidRequestArgs();
                CancelMoveEvent?.Invoke(default, requestArgs);
                SpinWait.SpinUntil(() => requestArgs.IsOk, 10 * 1000);
                if (requestArgs.IsOk)
                {
                    if (StopType == EnumMoveStopType.AvoidStop)
                    {

                    }
                    else
                    {
                        if (AddressArrivalArgs.EnumMoveState == EnumMoveState.Busy)
                        {
                            AddressArrivalArgs.EnumAddressArrival = EnumAddressArrival.Arrival;
                            AddressArrivalArgs.EnumMoveState = EnumMoveState.Idle;
                            OnUpdateAddressArrivalArgsEvent?.Invoke(default, AddressArrivalArgs);
                        }
                    }
                }
                else
                {
                    HandlerLogError(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, $"MovePackage can not cancel move. {requestArgs.ErrorMsg}.");
                }
            });
        }

        public void PassAddress(string addressId)
        {
            try
            {
                MapAddress address = Vehicle.Instance.MapInfo.addressMap[addressId];
                AddressArrivalArgs.EnumAddressArrival = EnumAddressArrival.Arrival;
                AddressArrivalArgs.MapPosition = address.Position;

                TracAddressQueue.Enqueue(new AddressArrivalArgs(AddressArrivalArgs));
                OnSectionPassEvent?.Invoke(this, addressId);
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
                TracAddressQueue.Enqueue(new AddressArrivalArgs(AddressArrivalArgs));
            }
            catch (Exception ex)
            {
                HandlerLogError(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }
        public bool AskReadyForMoveCommandRequest()
        {
            MidRequestArgs isReady = new MidRequestArgs();

            #region 原本寫法.
            //IsReadyForMoveCommandRequestEvent?.Invoke(default, isReady);
            //SpinWait.SpinUntil(() => isReady.IsOk, 10 * 1000);
            #endregion

            #region 修改後.
            Stopwatch timer = new Stopwatch();
            IsReadyForMoveCommandRequestEvent?.Invoke(default, isReady);

            while (!isReady.IsOk && timer.ElapsedMilliseconds < 5 * 1000)
            {
                Thread.Sleep(100);
                IsReadyForMoveCommandRequestEvent?.Invoke(default, isReady);
            }
            #endregion

            if (isReady.IsOk)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private void TracAddressEngine()
        {
            while (true)
            {
                if (IsTracAddressEnginePause)
                {
                    SpinWait.SpinUntil(() => !IsTracAddressEnginePause, 1000);
                    continue;
                }

                try
                {
                    if (TracAddressQueue.Any())
                    {
                        TracAddressQueue.TryDequeue(out AddressArrivalArgs addressArrivalArgs);
                        OnUpdateAddressArrivalArgsEvent?.Invoke(this, addressArrivalArgs);
                    }
                    else
                    {
                        OnAddressArrivalArgsRequestEvent?.Invoke(default, AddressArrivalArgs);
                        TracAddressQueue.Enqueue(AddressArrivalArgs);
                    }
                }
                catch (Exception ex)
                {
                    HandlerLogError(GetType().Name + ":" + System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message);
                }

                SpinWait.SpinUntil(() => false, Vehicle.Instance.MainFlowConfig.TrackPositionSleepTimeMs);
            }

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
