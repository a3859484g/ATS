using Mirle.Agv.INX.Control;
using Mirle.Agv.INX.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Controller
{
    public class LocateDriver_AlignmentValue : LocateDriver
    {
        protected LocateAGVPosition now = null;

        public LoadUnload LoadUnload { get; set; } = null;

        override public void InitialDriver(LocateDriverConfig driverConfig, AlarmHandler alarmHandler, string normalLogName)
        {
            this.normalLogName = normalLogName;
            this.DriverConfig = driverConfig;
            this.alarmHandler = alarmHandler;
            device = driverConfig.LocateDriverType.ToString();
            PollingOnOff = false;

            if (LoadUnload != null)
            {
                status = EnumControlStatus.Ready;
                pollingThread = new Thread(PollingThread);
                pollingThread.Start();
            }
        }

        //private string finadAddressID = "48003";
        private string finadAddressID = "";
        //private EnumStageDirection direction = EnumStageDirection.Right;
        private EnumStageDirection direction = EnumStageDirection.None;

        public void SwitchAlignmentValueAdress(string addressID)
        {
            if (localData.TheMapInfo.AllAddress.ContainsKey(addressID) &&
                (localData.TheMapInfo.AllAddress[addressID].LoadUnloadDirection != EnumStageDirection.None ||
                 localData.TheMapInfo.AllAddress[addressID].ChargingDirection != EnumStageDirection.None))
            {
                finadAddressID = addressID;

                LoadUnload.SwitchAlignmentValueByAddressID(finadAddressID);
            }
        }

        public void SetPollingAlignmentValueAddress(string addressID)
        {
            if (localData.TheMapInfo.AllAddress.ContainsKey(addressID) &&
                (localData.TheMapInfo.AllAddress[addressID].LoadUnloadDirection != EnumStageDirection.None ||
                 localData.TheMapInfo.AllAddress[addressID].ChargingDirection != EnumStageDirection.None))
            {
                //WriteLog(7, "", String.Concat("Set Address : ", addressID));

                finadAddressID = addressID;

                if (localData.TheMapInfo.AllAddress[addressID].LoadUnloadDirection != EnumStageDirection.None)
                    direction = localData.TheMapInfo.AllAddress[addressID].LoadUnloadDirection;
                else if (localData.TheMapInfo.AllAddress[addressID].ChargingDirection != EnumStageDirection.None)
                    direction = localData.TheMapInfo.AllAddress[addressID].ChargingDirection;

                PollingOnOff = true;
            }
        }

        private void PollingThread()
        {
            try
            {
                LocateAGVPosition newLocateAGVPosition;
                AlignmentValueData temp;
                DateTime sendTime;

                while (Status != EnumControlStatus.Closing)
                {
                    newLocateAGVPosition = null;

                    if (PollingOnOff)
                    {
                        if (LoadUnload != null)
                        {
                            sendTime = DateTime.Now;
                            LoadUnload.CheckAlingmentValueByAddressID(finadAddressID);
                            temp = localData.LoadUnloadData.AlignmentValue;

                            if (temp != null && temp.AlignmentVlaue)
                            {
                                newLocateAGVPosition = new LocateAGVPosition();
                                newLocateAGVPosition.GetDataTime = sendTime;
                                newLocateAGVPosition.Type = EnumAGVPositionType.Normal;
                                newLocateAGVPosition.Device = DriverConfig.Device;
                                newLocateAGVPosition.Order = DriverConfig.Order;

                                newLocateAGVPosition.AGVPosition.Angle = localData.TheMapInfo.AllAddress[finadAddressID].AGVPosition.Angle + temp.Theta;

                                if (Math.Abs(temp.P) <= 300 && Math.Abs(temp.Y) <= 300 && Math.Abs(temp.Theta) < 5)
                                {
                                    if (direction == EnumStageDirection.Right)
                                    {
                                        newLocateAGVPosition.AGVPosition.Position.X = localData.TheMapInfo.AllAddress[finadAddressID].AGVPosition.Position.X -
                                            Math.Cos(localData.TheMapInfo.AllAddress[finadAddressID].AGVPosition.Angle / 180 * Math.PI) * temp.P +
                                            Math.Sin(localData.TheMapInfo.AllAddress[finadAddressID].AGVPosition.Angle / 180 * Math.PI) * temp.Y;


                                        newLocateAGVPosition.AGVPosition.Position.Y = localData.TheMapInfo.AllAddress[finadAddressID].AGVPosition.Position.Y -
                                            Math.Sin(localData.TheMapInfo.AllAddress[finadAddressID].AGVPosition.Angle / 180 * Math.PI) * temp.P -
                                            Math.Cos(localData.TheMapInfo.AllAddress[finadAddressID].AGVPosition.Angle / 180 * Math.PI) * temp.Y;
                                    }
                                    else
                                    {
                                        newLocateAGVPosition.AGVPosition.Position.X = localData.TheMapInfo.AllAddress[finadAddressID].AGVPosition.Position.X +
                                            Math.Cos(localData.TheMapInfo.AllAddress[finadAddressID].AGVPosition.Angle / 180 * Math.PI) * temp.P -
                                            Math.Sin(localData.TheMapInfo.AllAddress[finadAddressID].AGVPosition.Angle / 180 * Math.PI) * temp.Y;


                                        newLocateAGVPosition.AGVPosition.Position.Y = localData.TheMapInfo.AllAddress[finadAddressID].AGVPosition.Position.Y +
                                            Math.Sin(localData.TheMapInfo.AllAddress[finadAddressID].AGVPosition.Angle / 180 * Math.PI) * temp.P +
                                            Math.Cos(localData.TheMapInfo.AllAddress[finadAddressID].AGVPosition.Angle / 180 * Math.PI) * temp.Y;
                                    }
                                }
                                else
                                    newLocateAGVPosition = null;
                                //public MapAGVPosition AGVPosition { get; set; } = new MapAGVPosition();
                            }
                        }
                    }

                    now = newLocateAGVPosition;

                    Thread.Sleep(5);
                }
            }
            catch (Exception ex)
            {
                WriteLog(1, "", String.Concat("Exception : ", ex.ToString()));
                now = null;
            }
        }

        override public LocateAGVPosition GetLocateAGVPosition
        {
            get
            {
                return now;
            }
        }
    }
}
