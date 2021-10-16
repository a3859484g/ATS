using Mirle.Agv.INX.Controller.Tools;
using Mirle.Agv.INX.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Controller
{
    public class UpdateControlHandler
    {
        private AlarmHandler alarmHandler;
        private ComputeFunction computeFunction = ComputeFunction.Instance;
        private LoggerAgent loggerAgent = LoggerAgent.Instance;
        private LocalData localData = LocalData.Instance;
        private string device = MethodInfo.GetCurrentMethod().ReflectedType.Name;
        private string normalLogName;

        public UpdateControlHandler(AlarmHandler alarmHandler, string normalLogName)
        {
            this.alarmHandler = alarmHandler;
            this.normalLogName = normalLogName;
        }

        public void WriteLog(int logLevel, string carrierId, string message, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            LogFormat logFormat = new LogFormat(normalLogName, logLevel.ToString(), memberName, device, carrierId, message);

            loggerAgent.Log(logFormat.Category, logFormat);

            if (logLevel <= localData.ErrorLevel)
            {
                logFormat = new LogFormat(localData.ErrorLogName, logLevel.ToString(), memberName, device, carrierId, message);
                loggerAgent.Log(logFormat.Category, logFormat);
            }
        }

        private double HalfMoveRange
        {
            get
            {
                return localData.MoveControlData.MoveControlConfig.SectionRange;
                //return localData.MoveControlData.CreateMoveCommandConfig.SafteyDistance[EnumCommandType.Move] / 2;
            }
        }

        private Dictionary<string, MapSection> AllSection
        {
            get
            {
                return localData.TheMapInfo.AllSection;
            }
        }

        private Dictionary<string, MapAddress> AllAddress
        {
            get
            {
                return localData.TheMapInfo.AllAddress;
            }
        }

        private Dictionary<EnumMoveControlSafetyType, SafetyData> MoveControlSafety
        {
            get
            {
                return localData.MoveControlData.MoveControlConfig.Safety;
            }
        }

        private void SetInSectionValue(VehicleLocation newLocate, ThetaSectionDeviation data)
        {
            if (newLocate.RealDistanceFormSectionHead < -HalfMoveRange)
                newLocate.InSection = false;
            else if (AllSection.ContainsKey(newLocate.NowSection) &&
                     newLocate.RealDistanceFormSectionHead > AllSection[newLocate.NowSection].Distance + HalfMoveRange)
                newLocate.InSection = false;

            if (data != null)
            {
                double a = newLocate.InSection ? 1 : 2;

                if (MoveControlSafety[EnumMoveControlSafetyType.OntimeReviseSectionDeviationLine].Enable &&
                    Math.Abs(data.SectionDeviation) > (MoveControlSafety[EnumMoveControlSafetyType.OntimeReviseSectionDeviationLine].Range / a))
                    newLocate.InSection = false;
                else if (MoveControlSafety[EnumMoveControlSafetyType.OntimeReviseTheta].Enable &&
                         Math.Abs(data.Theta) > (MoveControlSafety[EnumMoveControlSafetyType.OntimeReviseTheta].Range / a) &&
                         !(newLocate.InAddress && AllAddress.ContainsKey(newLocate.LastAddress) && AllAddress[newLocate.LastAddress].CanSpin))
                    newLocate.InSection = false;
            }
            else
                newLocate.InSection = false;
        }

        #region Update Address/Section - Manual mode.
        private DateTime lastFindSectionTime = DateTime.Now;
        private Thread findSectionThread = null;

        private void FindSectionThread()
        {
            VehicleLocation newVehicleLocation = new VehicleLocation();
            string sectionID = localData.Location.NowSection;
            string addressID = localData.Location.LastAddress;
            MapAGVPosition now = localData.Real;
            MapPosition temp;
            MapSection section;
            double distanceToHead;
            double distanceToTail;

            if (now == null)
                return;

            if (localData.TheMapInfo.AllSection.ContainsKey(sectionID))
            {
                #region 是否還在目前Section內 (判斷條件 : 頭尾不能超出,寬度為MoveControlConfig.SectionRange 預設是50mm, 非RTurn且 角度誤差在MoveControlConfig.SectionAllowDeltaTheta 預設是5度).
                section = localData.TheMapInfo.AllSection[sectionID];
                temp = computeFunction.GetTransferPosition(section, now.Position);

                if (0 <= temp.X && temp.X <= section.Distance && Math.Abs(temp.Y) <= localData.MoveControlData.MoveControlConfig.SectionRange &&
                    Math.Abs(computeFunction.GetCurrectAngle(section.FromVehicleAngle - now.Angle)) <= localData.MoveControlData.MoveControlConfig.SectionAllowDeltaTheta)
                {
                    if (section.FromVehicleAngle == section.ToVehicleAngle)
                    {
                        if (Math.Abs(computeFunction.GetCurrectAngle(section.FromVehicleAngle - now.Angle)) <= localData.MoveControlData.MoveControlConfig.SectionAllowDeltaTheta)
                        {
                            newVehicleLocation.DistanceFormSectionHead = temp.X;
                            newVehicleLocation.RealDistanceFormSectionHead = temp.X;
                            newVehicleLocation.NowSection = sectionID;
                            newVehicleLocation.LastAddress = addressID;

                            distanceToHead = temp.X;
                            distanceToTail = section.Distance - temp.X;

                            if (distanceToHead <= localData.MoveControlData.MoveControlConfig.InPositionRange && distanceToHead <= distanceToTail)
                            {
                                newVehicleLocation.InAddress = true;
                                newVehicleLocation.LastAddress = section.FromAddress.Id;
                            }
                            else if (distanceToTail <= localData.MoveControlData.MoveControlConfig.InPositionRange && distanceToTail <= distanceToHead)
                            {
                                newVehicleLocation.InAddress = true;
                                newVehicleLocation.LastAddress = section.ToAddress.Id;
                            }
                            else
                                newVehicleLocation.InAddress = false;

                            double sectionDeviation = temp.Y;
                            double theta = computeFunction.GetCurrectAngle(now.Angle - localData.TheMapInfo.AllSection[section.Id].FromVehicleAngle);
                            ThetaSectionDeviation tempData = new ThetaSectionDeviation(theta, sectionDeviation);

                            SetInSectionValue(newVehicleLocation, tempData);
                            localData.Location = newVehicleLocation;
                            //WriteLog(7, "", String.Concat("Section不變更 : ", sectionID));
                            localData.LastAGVPosition = now;
                            return;
                        }
                    }
                }
                #endregion

                #region 搜尋是否在相鄰Section內 (判斷條件 : 頭尾不能超出,寬度為MoveControlConfig.SectionRange 預設是50mm, 非RTurn且 角度誤差在MoveControlConfig.SectionAllowDeltaTheta 預設是5度).
                for (int i = 0; i < localData.TheMapInfo.AllSection[sectionID].NearbySection.Count; i++)
                {
                    section = localData.TheMapInfo.AllSection[sectionID].NearbySection[i];

                    if (section.FromVehicleAngle == section.ToVehicleAngle)
                    {
                        temp = computeFunction.GetTransferPosition(section, now.Position);

                        if (0 <= temp.X && temp.X <= section.Distance && Math.Abs(temp.Y) <= localData.MoveControlData.MoveControlConfig.SectionRange &&
                        Math.Abs(computeFunction.GetCurrectAngle(section.FromVehicleAngle - now.Angle)) <= localData.MoveControlData.MoveControlConfig.SectionAllowDeltaTheta)
                        {
                            if (Math.Abs(computeFunction.GetCurrectAngle(section.FromVehicleAngle - now.Angle)) <= localData.MoveControlData.MoveControlConfig.SectionAllowDeltaTheta)
                            {
                                newVehicleLocation.DistanceFormSectionHead = temp.X;
                                newVehicleLocation.RealDistanceFormSectionHead = temp.X;
                                newVehicleLocation.NowSection = section.Id;

                                distanceToHead = temp.X;
                                distanceToTail = section.Distance - temp.X;

                                if (distanceToHead <= localData.MoveControlData.MoveControlConfig.InPositionRange && distanceToHead <= distanceToTail)
                                {
                                    newVehicleLocation.InAddress = true;
                                    newVehicleLocation.LastAddress = section.FromAddress.Id;
                                }
                                else if (distanceToTail <= localData.MoveControlData.MoveControlConfig.InPositionRange && distanceToTail <= distanceToHead)
                                {
                                    newVehicleLocation.InAddress = true;
                                    newVehicleLocation.LastAddress = section.ToAddress.Id;
                                }
                                else
                                {
                                    newVehicleLocation.InAddress = false;

                                    if (section.FromAddress == localData.TheMapInfo.AllSection[sectionID].FromAddress ||
                                        section.FromAddress == localData.TheMapInfo.AllSection[sectionID].ToAddress)
                                        newVehicleLocation.LastAddress = section.FromAddress.Id;
                                    else
                                        newVehicleLocation.LastAddress = section.ToAddress.Id;
                                }

                                double sectionDeviation = temp.Y;
                                double theta = computeFunction.GetCurrectAngle(now.Angle - localData.TheMapInfo.AllSection[section.Id].FromVehicleAngle);
                                ThetaSectionDeviation tempData = new ThetaSectionDeviation(theta, sectionDeviation);
                                SetInSectionValue(newVehicleLocation, tempData);

                                localData.Location = newVehicleLocation;
                                //WriteLog(7, "", String.Concat("切換Section(相鄰) : ", newVehicleLocation.NowSection));
                                localData.LastAGVPosition = now;
                                return;
                            }
                        }
                    }
                }
                #endregion
            }

            #region Find All (條件 : 頭尾可超出 MoveControlConfig.SectionRange 預設是50mm, 寬度在MoveControlConfig.SectionWidthRange 預設是500mm, 非RTurn且 角度誤差在MoveControlConfig.SectionAllowDeltaTheta 預設是5度 中 找尋數值最接近的)
            MapSection minSection = null;
            MapPosition minPosition = null;
            double min = -1;
            double tempMin;
            double deltaAngle;

            foreach (MapSection tempSection in localData.TheMapInfo.AllSection.Values)
            {
                if (tempSection.FromVehicleAngle == tempSection.ToVehicleAngle)
                {
                    temp = computeFunction.GetTransferPosition(tempSection, now.Position);

                    if (-localData.MoveControlData.MoveControlConfig.SectionRange <= temp.X && temp.X <= (tempSection.Distance + localData.MoveControlData.MoveControlConfig.SectionRange) &&
                        Math.Abs(temp.Y) <= localData.MoveControlData.MoveControlConfig.SectionWidthRange)
                    {
                        deltaAngle = Math.Abs(computeFunction.GetCurrectAngle(tempSection.FromVehicleAngle - now.Angle));

                        tempMin = Math.Abs(temp.Y) + deltaAngle * 50;

                        if (temp.X < 0)
                            tempMin = tempMin - temp.X;
                        else if (temp.X > tempSection.Distance)
                            tempMin = tempMin + temp.X - tempSection.Distance;

                        if (min == -1 || tempMin < min)
                        {
                            min = tempMin;
                            minSection = tempSection;
                            minPosition = temp;
                        }
                    }
                }
            }

            if (min == -1)
            {
                if (localData.MoveControlData.MoveControlConfig.LosePositionSetNullAddressSection)
                {
                    newVehicleLocation.DistanceFormSectionHead = 0;
                    newVehicleLocation.LastAddress = "";
                    newVehicleLocation.NowSection = "";
                    newVehicleLocation.InAddress = false;
                    newVehicleLocation.InSection = false;

                    localData.Location = newVehicleLocation;
                }

                WriteLog(3, "", String.Concat("目前位置 : ", computeFunction.GetMapAGVPositionStringWithAngle(now), " 搜尋不到所在Section"));
                localData.LastAGVPosition = now;
            }
            else
            {
                newVehicleLocation.NowSection = minSection.Id;
                newVehicleLocation.RealDistanceFormSectionHead = minPosition.X;

                if (minPosition.X < 0)
                    newVehicleLocation.DistanceFormSectionHead = 0;
                else if (minPosition.X > minSection.Distance)
                    newVehicleLocation.DistanceFormSectionHead = minSection.Distance;
                else
                    newVehicleLocation.DistanceFormSectionHead = minPosition.X;

                distanceToHead = Math.Abs(minPosition.X);
                distanceToTail = Math.Abs(minSection.Distance - minPosition.X);

                if (distanceToHead <= localData.MoveControlData.MoveControlConfig.InPositionRange && distanceToHead <= distanceToTail)
                {
                    newVehicleLocation.LastAddress = minSection.FromAddress.Id;
                    newVehicleLocation.InAddress = true;
                }
                else if (distanceToTail < localData.MoveControlData.MoveControlConfig.InPositionRange && distanceToTail < distanceToHead)
                {
                    newVehicleLocation.LastAddress = minSection.ToAddress.Id;
                    newVehicleLocation.InAddress = true;
                }
                else
                {
                    if (distanceToHead <= distanceToTail)
                        newVehicleLocation.LastAddress = minSection.FromAddress.Id;
                    else
                        newVehicleLocation.LastAddress = minSection.ToAddress.Id;

                    newVehicleLocation.InAddress = false;
                }

                double sectionDeviation = minPosition.Y;
                double theta = computeFunction.GetCurrectAngle(now.Angle - localData.TheMapInfo.AllSection[minSection.Id].FromVehicleAngle);
                ThetaSectionDeviation tempData = new ThetaSectionDeviation(theta, sectionDeviation);
                SetInSectionValue(newVehicleLocation, tempData);

                localData.Location = newVehicleLocation;
                //WriteLog(7, "", String.Concat("切換Section(All) : ", newVehicleLocation.NowSection));
                localData.LastAGVPosition = now;
            }
            #endregion
        }

        private void UpdateAddressSection_ManualMode()
        {
            VehicleLocation nowLocateion = localData.Location;
            string sectionID = "";

            if (nowLocateion != null)
                sectionID = nowLocateion.NowSection;

            /*
            if (localData.Real != null && localData.LastAGVPosition != null && !localData.MoveControlData.ErrorBit)
            {
                double distance = computeFunction.GetTwoPositionDistance(localData.Real, localData.LastAGVPosition);

                if (distance <= localData.MoveControlData.MoveControlConfig.InPositionRange &&
                    Math.Abs(computeFunction.GetCurrectAngle(localData.Real.Angle - localData.LastAGVPosition.Angle)) <= localData.MoveControlData.MoveControlConfig.SectionAllowDeltaTheta &&
                    localData.TheMapInfo.AllSection.ContainsKey(sectionID) && localData.TheMapInfo.AllSection[sectionID].FromVehicleAngle == localData.TheMapInfo.AllSection[sectionID].ToVehicleAngle &&
                    Math.Abs(computeFunction.GetCurrectAngle(localData.Real.Angle - localData.TheMapInfo.AllSection[sectionID].FromVehicleAngle)) <= localData.MoveControlData.MoveControlConfig.Safety[EnumMoveControlSafetyType.OntimeReviseTheta].Range)
                {
                    lastFindSectionTime = DateTime.Now;

                    sectionID = localData.Location.NowSection;

                    if (localData.TheMapInfo.AllSection.ContainsKey(sectionID))
                    {
                        if (localData.TheMapInfo.AllSection[sectionID].FromVehicleAngle == localData.TheMapInfo.AllSection[sectionID].ToVehicleAngle)
                        {
                            MapPosition temp = computeFunction.GetTransferPosition(localData.TheMapInfo.AllSection[sectionID], localData.Real.Position);

                            double sectionDeviation = temp.Y;
                            double theta = computeFunction.GetCurrectAngle(localData.Real.Angle - localData.TheMapInfo.AllSection[sectionID].FromVehicleAngle);
                            thetaSectionDeviation = new ThetaSectionDeviation(theta, sectionDeviation);
                        }
                    }

                    return;
                }
            }
            */

            if (localData.Real == null)
            {
                if (localData.MoveControlData.MoveControlConfig.LosePositionSetNullAddressSection)
                {
                    VehicleLocation newLocate = new VehicleLocation();
                    newLocate.InSection = false;
                    localData.Location = newLocate;
                    localData.LastAGVPosition = localData.Real;
                }
            }
            else
            {
                if ((DateTime.Now - lastFindSectionTime).TotalMilliseconds > localData.MoveControlData.MoveControlConfig.TimeValueConfig.IntervalTimeList[EnumIntervalTimeType.ManualFindSectionInterval])
                {
                    lastFindSectionTime = DateTime.Now;

                    if (findSectionThread == null || !findSectionThread.IsAlive)
                    {
                        findSectionThread = new Thread(FindSectionThread);
                        findSectionThread.Start();
                    }
                    else
                        WriteLog(3, "", String.Concat("Find now Section Interval : ", localData.MoveControlData.MoveControlConfig.TimeValueConfig.IntervalTimeList[EnumIntervalTimeType.ManualFindSectionInterval].ToString("0"), ", 上一次搜尋還未結束"));

                    if (localData.TheMapInfo.AllSection.ContainsKey(sectionID))
                    {
                        MapPosition temp = computeFunction.GetTransferPosition(localData.TheMapInfo.AllSection[sectionID], localData.Real.Position);

                        double sectionDeviation = temp.Y;
                        double theta = computeFunction.GetCurrectAngle(localData.Real.Angle - localData.TheMapInfo.AllSection[sectionID].FromVehicleAngle);
                        thetaSectionDeviation = new ThetaSectionDeviation(theta, sectionDeviation);
                    }
                }
                else
                {
                    if (localData.TheMapInfo.AllSection.ContainsKey(sectionID))
                    {
                        VehicleLocation newLocate = new VehicleLocation();
                        newLocate.NowSection = sectionID;
                        MapPosition temp = computeFunction.GetTransferPosition(localData.TheMapInfo.AllSection[sectionID], localData.Real.Position);

                        newLocate.RealDistanceFormSectionHead = temp.X;

                        if (temp.X < 0)
                            temp.X = 0;
                        else if (temp.X > localData.TheMapInfo.AllSection[sectionID].Distance)
                            temp.X = localData.TheMapInfo.AllSection[sectionID].Distance;

                        if (Math.Abs(temp.X) <= localData.MoveControlData.MoveControlConfig.InPositionRange)
                        {
                            newLocate.InAddress = true;
                            newLocate.LastAddress = localData.TheMapInfo.AllSection[sectionID].FromAddress.Id;
                        }
                        else if (Math.Abs(localData.TheMapInfo.AllSection[sectionID].Distance - temp.X) <= localData.MoveControlData.MoveControlConfig.InPositionRange)
                        {
                            newLocate.InAddress = true;
                            newLocate.LastAddress = localData.TheMapInfo.AllSection[sectionID].ToAddress.Id;
                        }
                        else
                            newLocate.LastAddress = nowLocateion.LastAddress;

                        newLocate.DistanceFormSectionHead = temp.X;

                        double sectionDeviation = temp.Y;
                        double theta = computeFunction.GetCurrectAngle(localData.Real.Angle - localData.TheMapInfo.AllSection[sectionID].FromVehicleAngle);
                        thetaSectionDeviation = new ThetaSectionDeviation(theta, sectionDeviation);

                        SetInSectionValue(newLocate, thetaSectionDeviation);
                        localData.Location = newLocate;
                    }
                }
            }
        }
        #endregion

        #region Update Address/Section - MoveControl moving flow.
        private void UpdateAddressSection_MoveControlMovingFlow(SectionLine sectionLine)
        {
            if (sectionLine == null || localData.Real == null)
                return;

            VehicleLocation newVehicleLocation = new VehicleLocation();

            MapPosition temp;
            bool overSection = false;

            if (localData.CoverMIPCBug)
            {
                if (localData.MoveControlData.LocateControlData.LocateAGVPosition != null)
                    temp = computeFunction.GetTransferPosition(sectionLine.Section, localData.MoveControlData.LocateControlData.LocateAGVPosition.AGVPosition.Position);
                else
                    temp = computeFunction.GetTransferPosition(sectionLine.Section, localData.Real.Position);
            }
            else
            {
                temp = computeFunction.GetTransferPosition(sectionLine.Section, localData.Real.Position);
            }

            if (temp.X < 0)
            {
                if (!sectionLine.SectionDirFlag)
                    overSection = true;

                newVehicleLocation.DistanceFormSectionHead = 0;
            }
            else if (temp.X > sectionLine.Section.Distance)
            {
                if (sectionLine.SectionDirFlag)
                    overSection = true;

                newVehicleLocation.DistanceFormSectionHead = sectionLine.Section.Distance;
            }
            else
                newVehicleLocation.DistanceFormSectionHead = temp.X;

            newVehicleLocation.NowSection = sectionLine.Section.Id;

            if ((localData.MoveControlData.MoveCommand.CommandStatus == EnumMoveCommandStartStatus.Reporting ||
                 localData.MoveControlData.MoveCommand.CommandStatus == EnumMoveCommandStartStatus.End) &&
                 localData.MoveControlData.MoveCommand.MoveStatus != EnumMoveStatus.Error)
            {
                newVehicleLocation.InAddress = localData.Location.InAddress;
                newVehicleLocation.LastAddress = localData.Location.LastAddress;
            }
            else
            {
                if (sectionLine.SectionDirFlag)
                {
                    if (Math.Abs(temp.X) <= localData.MoveControlData.MoveControlConfig.InPositionRange)
                    {
                        newVehicleLocation.InAddress = true;

                        if (localData.TheMapInfo.AllAddress.ContainsKey(sectionLine.Start.Id))
                            newVehicleLocation.LastAddress = sectionLine.Start.Id;
                        else
                            newVehicleLocation.LastAddress = localData.Location.LastAddress;
                    }
                    else if (Math.Abs(temp.X - sectionLine.Section.Distance) <= localData.MoveControlData.MoveControlConfig.InPositionRange)
                    {
                        newVehicleLocation.InAddress = true;

                        if (localData.TheMapInfo.AllAddress.ContainsKey(sectionLine.End.Id))
                            newVehicleLocation.LastAddress = sectionLine.End.Id;
                        else
                            newVehicleLocation.LastAddress = localData.Location.LastAddress;
                    }
                    else
                    {
                        if (localData.TheMapInfo.AllAddress.ContainsKey(sectionLine.Start.Id))
                            newVehicleLocation.LastAddress = sectionLine.Start.Id;
                        else
                            newVehicleLocation.LastAddress = localData.Location.LastAddress;

                        newVehicleLocation.InAddress = false;
                    }
                }
                else
                {
                    if (Math.Abs(temp.X - sectionLine.Section.Distance) <= localData.MoveControlData.MoveControlConfig.InPositionRange)
                    {
                        newVehicleLocation.InAddress = true;

                        if (localData.TheMapInfo.AllAddress.ContainsKey(sectionLine.Start.Id))
                            newVehicleLocation.LastAddress = sectionLine.Start.Id;
                        else
                            newVehicleLocation.LastAddress = localData.Location.LastAddress;
                    }
                    else if (Math.Abs(temp.X) <= localData.MoveControlData.MoveControlConfig.InPositionRange)
                    {
                        newVehicleLocation.InAddress = true;

                        if (localData.TheMapInfo.AllAddress.ContainsKey(sectionLine.End.Id))
                            newVehicleLocation.LastAddress = sectionLine.End.Id;
                        else
                            newVehicleLocation.LastAddress = localData.Location.LastAddress;
                    }
                    else
                    {
                        newVehicleLocation.InAddress = false;

                        if (localData.TheMapInfo.AllAddress.ContainsKey(sectionLine.Start.Id))
                            newVehicleLocation.LastAddress = sectionLine.Start.Id;
                        else
                            newVehicleLocation.LastAddress = localData.Location.LastAddress;
                    }
                }
            }

            if (localData.MoveControlData.MoveCommand.MoveStatus == EnumMoveStatus.Moving)
            {
                double sectionDeviation = temp.Y;
                double theta = 0;

                if (localData.CoverMIPCBug)
                {
                    if (localData.MoveControlData.LocateControlData.LocateAGVPosition != null)
                        theta = computeFunction.GetCurrectAngle(localData.MoveControlData.LocateControlData.LocateAGVPosition.AGVPosition.Angle - sectionLine.Section.FromVehicleAngle);
                    else
                        theta = computeFunction.GetCurrectAngle(localData.Real.Angle - sectionLine.Section.FromVehicleAngle);
                }
                else
                {
                    theta = computeFunction.GetCurrectAngle(localData.Real.Angle - sectionLine.Section.FromVehicleAngle);
                }

                thetaSectionDeviation = new ThetaSectionDeviation(theta, sectionDeviation);
            }

            localData.MoveControlData.MoveCommand.CommandEncoder = sectionLine.EncoderAddSectionDistanceStart + (sectionLine.SectionDirFlag ? temp.X : -temp.X);

            if (localData.CoverMIPCBug)
            {
                if (localData.MoveControlData.LocateControlData.LocateAGVPosition != null)
                {
                    double deltaTime = ((DateTime.Now - localData.MoveControlData.LocateControlData.LocateAGVPosition.GetDataTime).TotalMilliseconds +
                                        localData.MoveControlData.LocateControlData.LocateAGVPosition.ScanTime) / 1000;

                    localData.MoveControlData.MoveCommand.CommandEncoder += Math.Abs(localData.MoveControlData.MotionControlData.LineVelocity * deltaTime);
                    localData.Real.Angle = computeFunction.GetCurrectAngle(localData.MoveControlData.LocateControlData.LocateAGVPosition.AGVPosition.Angle +
                                                                           deltaTime * localData.MoveControlData.MotionControlData.ThetaVelocity);
                }
            }

            localData.Location = newVehicleLocation;
            localData.LastAGVPosition = localData.Real;

            MoveCommandData moveCmd = localData.MoveControlData.MoveCommand;

            if (overSection && moveCmd.MoveStatus == EnumMoveStatus.Error)
            {
                if (moveCmd.IndexOflisSectionLine + 1 < moveCmd.SectionLineList.Count)
                {
                    moveCmd.IndexOflisSectionLine++;
                    WriteLog(5, "", "EMSControl ing changeNext Section");
                    UpdateAddressSection_MoveControlMovingFlow(moveCmd.SectionLineList[moveCmd.IndexOflisSectionLine]);
                }
            }
        }

        private void UpdateAddressSection_MoveControlMovingFlow_Error()
        {
            SectionLine sectionLine = localData.MoveControlData.MoveCommand.SectionLineList[localData.MoveControlData.MoveCommand.IndexOflisSectionLine];

            VehicleLocation newVehicleLocation = new VehicleLocation();

            MapPosition temp;

            if (localData.CoverMIPCBug)
            {
                if (localData.MoveControlData.LocateControlData.LocateAGVPosition != null)
                    temp = computeFunction.GetTransferPosition(sectionLine.Section, localData.MoveControlData.LocateControlData.LocateAGVPosition.AGVPosition.Position);
                else
                    temp = computeFunction.GetTransferPosition(sectionLine.Section, localData.Real.Position);
            }
            else
            {
                temp = computeFunction.GetTransferPosition(sectionLine.Section, localData.Real.Position);
            }

            if (temp.X < 0)
                newVehicleLocation.DistanceFormSectionHead = 0;
            else if (temp.X > sectionLine.Section.Distance)
                newVehicleLocation.DistanceFormSectionHead = sectionLine.Section.Distance;
            else
                newVehicleLocation.DistanceFormSectionHead = temp.X;

            newVehicleLocation.NowSection = sectionLine.Section.Id;

            if ((localData.MoveControlData.MoveCommand.CommandStatus == EnumMoveCommandStartStatus.Reporting ||
                 localData.MoveControlData.MoveCommand.CommandStatus == EnumMoveCommandStartStatus.End) &&
                 localData.MoveControlData.MoveCommand.MoveStatus != EnumMoveStatus.Error)
            {
                newVehicleLocation.InAddress = localData.Location.InAddress;
                newVehicleLocation.LastAddress = localData.Location.LastAddress;
            }
            else
            {
                if (sectionLine.SectionDirFlag)
                {
                    if (Math.Abs(temp.X) <= localData.MoveControlData.MoveControlConfig.InPositionRange)
                    {
                        newVehicleLocation.InAddress = true;

                        if (localData.TheMapInfo.AllAddress.ContainsKey(sectionLine.Start.Id))
                            newVehicleLocation.LastAddress = sectionLine.Start.Id;
                        else
                            newVehicleLocation.LastAddress = localData.Location.LastAddress;
                    }
                    else if (Math.Abs(temp.X - sectionLine.Section.Distance) <= localData.MoveControlData.MoveControlConfig.InPositionRange)
                    {
                        newVehicleLocation.InAddress = true;

                        if (localData.TheMapInfo.AllAddress.ContainsKey(sectionLine.End.Id))
                            newVehicleLocation.LastAddress = sectionLine.End.Id;
                        else
                            newVehicleLocation.LastAddress = localData.Location.LastAddress;
                    }
                    else
                    {
                        if (localData.TheMapInfo.AllAddress.ContainsKey(sectionLine.Start.Id))
                            newVehicleLocation.LastAddress = sectionLine.Start.Id;
                        else
                            newVehicleLocation.LastAddress = localData.Location.LastAddress;

                        newVehicleLocation.InAddress = false;
                    }
                }
                else
                {
                    if (Math.Abs(temp.X - sectionLine.Section.Distance) <= localData.MoveControlData.MoveControlConfig.InPositionRange)
                    {
                        newVehicleLocation.InAddress = true;

                        if (localData.TheMapInfo.AllAddress.ContainsKey(sectionLine.Start.Id))
                            newVehicleLocation.LastAddress = sectionLine.Start.Id;
                        else
                            newVehicleLocation.LastAddress = localData.Location.LastAddress;
                    }
                    else if (Math.Abs(temp.X) >= localData.MoveControlData.MoveControlConfig.InPositionRange)
                    {
                        newVehicleLocation.InAddress = true;

                        if (localData.TheMapInfo.AllAddress.ContainsKey(sectionLine.End.Id))
                            newVehicleLocation.LastAddress = sectionLine.End.Id;
                        else
                            newVehicleLocation.LastAddress = localData.Location.LastAddress;
                    }
                    else
                    {
                        newVehicleLocation.InAddress = false;

                        if (localData.TheMapInfo.AllAddress.ContainsKey(sectionLine.Start.Id))
                            newVehicleLocation.LastAddress = sectionLine.Start.Id;
                        else
                            newVehicleLocation.LastAddress = localData.Location.LastAddress;
                    }
                }
                //if (localData.TheMapInfo.AllAddress.ContainsKey(sectionLine.Start.Id))
                //    newVehicleLocation.LastAddress = sectionLine.Start.Id;
                //else
                //    newVehicleLocation.LastAddress = localData.Location.LastAddress;
            }

            if (localData.MoveControlData.MoveCommand.MoveStatus == EnumMoveStatus.Moving)
            {
                double sectionDeviation = temp.Y;
                double theta = 0;

                if (localData.CoverMIPCBug)
                {
                    if (localData.MoveControlData.LocateControlData.LocateAGVPosition != null)
                        theta = computeFunction.GetCurrectAngle(localData.MoveControlData.LocateControlData.LocateAGVPosition.AGVPosition.Angle - sectionLine.Section.FromVehicleAngle);
                    else
                        theta = computeFunction.GetCurrectAngle(localData.Real.Angle - sectionLine.Section.FromVehicleAngle);
                }
                else
                {
                    theta = computeFunction.GetCurrectAngle(localData.Real.Angle - sectionLine.Section.FromVehicleAngle);
                }

                thetaSectionDeviation = new ThetaSectionDeviation(theta, sectionDeviation);
            }

            localData.MoveControlData.MoveCommand.CommandEncoder = sectionLine.EncoderAddSectionDistanceStart + (sectionLine.SectionDirFlag ? temp.X : -temp.X);

            if (localData.CoverMIPCBug)
            {
                if (localData.MoveControlData.LocateControlData.LocateAGVPosition != null)
                {
                    double deltaTime = ((DateTime.Now - localData.MoveControlData.LocateControlData.LocateAGVPosition.GetDataTime).TotalMilliseconds +
                                        localData.MoveControlData.LocateControlData.LocateAGVPosition.ScanTime) / 1000;

                    localData.MoveControlData.MoveCommand.CommandEncoder += Math.Abs(localData.MoveControlData.MotionControlData.LineVelocity * deltaTime);
                    localData.Real.Angle = computeFunction.GetCurrectAngle(localData.MoveControlData.LocateControlData.LocateAGVPosition.AGVPosition.Angle +
                                                                           deltaTime * localData.MoveControlData.MotionControlData.ThetaVelocity);
                }
            }

            localData.Location = newVehicleLocation;
            localData.LastAGVPosition = localData.Real;
        }

        private void SimulateReal_SimulateMode(SectionLine sectionLine)
        {
            if (sectionLine == null)
                return;

            double commandEncoder = localData.MoveControlData.MoveCommand.CommandEncoder + localData.MoveControlData.MotionControlData.SimulateLineVelocity * localData.MoveControlData.MoveControlConfig.TimeValueConfig.IntervalTimeList[EnumIntervalTimeType.MoveControlThreadInterval] / 1000;
            commandEncoder -= sectionLine.EncoderAddSectionDistanceStart;

            if (!sectionLine.SectionDirFlag)
                commandEncoder = -commandEncoder;

            MapAGVPosition fakeReal = new MapAGVPosition();

            double percent = commandEncoder / sectionLine.Section.Distance;

            fakeReal.Angle = computeFunction.GetCurrectAngle(sectionLine.Section.FromVehicleAngle +
                             computeFunction.GetCurrectAngle(sectionLine.Section.ToVehicleAngle - sectionLine.Section.FromVehicleAngle) * percent);

            fakeReal.Position = new MapPosition(sectionLine.Section.FromAddress.AGVPosition.Position.X +
                                                (sectionLine.Section.ToAddress.AGVPosition.Position.X -
                                                 sectionLine.Section.FromAddress.AGVPosition.Position.X) * percent,
                                                sectionLine.Section.FromAddress.AGVPosition.Position.Y +
                                                (sectionLine.Section.ToAddress.AGVPosition.Position.Y -
                                                 sectionLine.Section.FromAddress.AGVPosition.Position.Y) * percent);

            localData.MoveControlData.MotionControlData.LineVelocity = localData.MoveControlData.MotionControlData.SimulateLineVelocity;
            localData.MoveControlData.MotionControlData.MoveStatus = (localData.MoveControlData.MotionControlData.SimulateLineVelocity != 0) ? EnumAxisMoveStatus.Move : EnumAxisMoveStatus.Stop;

            localData.Real = fakeReal;
        }

        #endregion

        private ThetaSectionDeviation thetaSectionDeviation = null;

        public void UpdateAllData(SectionLine sectionLine)
        {
            thetaSectionDeviation = null;

            if (localData.MoveControlData.MoveCommand != null)
            {
                if (localData.SimulateMode)
                    SimulateReal_SimulateMode(sectionLine);

                UpdateAddressSection_MoveControlMovingFlow(sectionLine);

                localData.LastAGVPosition = localData.Real;
            }
            else
            {
                if (localData.AutoManual == EnumAutoState.Manual)
                    UpdateAddressSection_ManualMode();
            }

            localData.MoveControlData.ThetaSectionDeviation = thetaSectionDeviation;
        }
    }
}
