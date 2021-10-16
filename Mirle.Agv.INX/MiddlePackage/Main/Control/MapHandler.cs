using Mirle.Agv.MiddlePackage.Umtc.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Mirle.Agv.MiddlePackage.Umtc.Controller
{
    public class MapHandler
    {
        public Vehicle Vehicle { get; set; } = Vehicle.Instance;
        public string SectionPath { get; set; }
        public string AddressPath { get; set; }
        public string PortIdMapPath { get; set; }
        public string AgvStationPath { get; set; }
        public string SectionBeamDisablePath { get; set; }

        private string lastReadAdrId = "";
        private string lastReadSecId = "";
        private string lastReadPortId = "";
        private string failAgvStationId = "";
        private string failAddressIdInReadAgvStationFile = "";

        public MapHandler()
        {
            SectionPath = Path.Combine(Vehicle.MapConfig.FolderName, Vehicle.MapConfig.SectionFileName);
            AddressPath = Path.Combine(Vehicle.MapConfig.FolderName, Vehicle.MapConfig.AddressFileName);
            PortIdMapPath = Path.Combine(Vehicle.MapConfig.FolderName, Vehicle.MapConfig.PortIdMapFileName);
            AgvStationPath = Path.Combine(Vehicle.MapConfig.FolderName, Vehicle.MapConfig.AgvStationFileName);
            SectionBeamDisablePath = Path.Combine(Vehicle.MapConfig.FolderName, Vehicle.MapConfig.SectionBeamDisablePathFileName);

            LoadMapInfo();
        }

        public void LoadMapInfo()
        {
            ReadAddressCsv();
            ReadPortIdMapCsv();
            ReadAgvStationCsv();
            ReadSectionCsv();
        }

        public void ReadAddressCsv()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(AddressPath))
                {
                    throw new Exception($"IsAddressPathNull={string.IsNullOrWhiteSpace(AddressPath)}");
                }
                Vehicle.MapInfo.addressMap.Clear();
                Vehicle.MapInfo.chargerAddressMap.Clear();

                string[] allRows = File.ReadAllLines(AddressPath);
                if (allRows == null || allRows.Length < 2)
                {
                    throw new Exception($"There are no address in file");
                }

                string[] titleRow = allRows[0].Split(',');
                allRows = allRows.Skip(1).ToArray();

                int nRows = allRows.Length;
                int nColumns = titleRow.Length;

                Dictionary<string, int> dicHeaderIndexes = new Dictionary<string, int>();
                for (int i = 0; i < nColumns; i++)
                {
                    var keyword = titleRow[i].Trim();
                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        dicHeaderIndexes.Add(keyword, i);
                    }
                }

                for (int i = 0; i < nRows; i++)
                {
                    string[] getThisRow = allRows[i].Split(',');
                    MapAddress oneRow = new MapAddress();
                    MapAddressOffset offset = new MapAddressOffset();
                    try
                    {
                        oneRow.Id = getThisRow[dicHeaderIndexes["Id"]];
                        oneRow.Position.X = double.Parse(getThisRow[dicHeaderIndexes["PositionX"]]);
                        oneRow.Position.Y = double.Parse(getThisRow[dicHeaderIndexes["PositionY"]]);
                        if (dicHeaderIndexes.ContainsKey("LoadUnloadDirection"))
                        {
                            oneRow.LoadUnloadDirection = oneRow.AddressDirectionParse(getThisRow[dicHeaderIndexes["LoadUnloadDirection"]]);
                        }
                        if (dicHeaderIndexes.ContainsKey("GateType"))
                        {
                            oneRow.GateType = getThisRow[dicHeaderIndexes["GateType"]];
                        }
                        if (dicHeaderIndexes.ContainsKey("ChargingDirection"))
                        {
                            oneRow.ChargingDirection = oneRow.AddressDirectionParse(getThisRow[dicHeaderIndexes["ChargingDirection"]]);
                        }
                        if (dicHeaderIndexes.ContainsKey("PioDirection"))
                        {
                            oneRow.PioDirection = oneRow.AddressDirectionParse(getThisRow[dicHeaderIndexes["PioDirection"]]);
                        }
                        if (dicHeaderIndexes.ContainsKey("InsideSectionId"))
                        {
                            oneRow.InsideSectionId = FitZero(getThisRow[dicHeaderIndexes["InsideSectionId"]]);
                        }
                        if (dicHeaderIndexes.ContainsKey("OffsetX"))
                        {
                            offset.OffsetX = double.Parse(getThisRow[dicHeaderIndexes["OffsetX"]]);
                            offset.OffsetY = double.Parse(getThisRow[dicHeaderIndexes["OffsetY"]]);
                            offset.OffsetTheta = double.Parse(getThisRow[dicHeaderIndexes["OffsetTheta"]]);
                        }
                        oneRow.AddressOffset = offset;
                        if (dicHeaderIndexes.ContainsKey("VehicleHeadAngle"))
                        {
                            oneRow.VehicleHeadAngle = double.Parse(getThisRow[dicHeaderIndexes["VehicleHeadAngle"]]);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"LoadAddressCsv read oneRow : [lastReadAdrId={lastReadAdrId}][{ex.Message}]");
                    }

                    lastReadAdrId = oneRow.Id;
                    Vehicle.MapInfo.addressMap.TryAdd(oneRow.Id, oneRow);
                    if (oneRow.IsCharger())
                    {
                        Vehicle.MapInfo.chargerAddressMap.Add(oneRow);
                    }
                    Vehicle.MapInfo.gateTypeMap.Add(oneRow.Id, oneRow.GateType);

                }

                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"Load Address File Ok. [lastReadAdrId={lastReadAdrId}]");
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"LoadAddressCsv : [lastReadAdrId={lastReadAdrId}][{ex.Message}]");
            }
        }

        public void ReadPortIdMapCsv()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(PortIdMapPath))
                {
                    return;
                }

                //foreach (var address in Vehicle.Mapinfo.addressMap.Values)
                //{
                //    address.PortIdMap.Clear();
                //}

                string[] allRows = File.ReadAllLines(PortIdMapPath);
                if (allRows == null || allRows.Length < 2)
                {
                    return;
                }

                string[] titleRow = allRows[0].Split(',');
                allRows = allRows.Skip(1).ToArray();

                int nRows = allRows.Length;
                int nColumns = titleRow.Length;

                Dictionary<string, int> dicHeaderIndexes = new Dictionary<string, int>();
                for (int i = 0; i < nColumns; i++)
                {
                    var keyword = titleRow[i].Trim();
                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        dicHeaderIndexes.Add(keyword, i);
                    }
                }
                for (int i = 0; i < nRows; i++)
                {
                    string[] getThisRow = allRows[i].Split(',');
                    try
                    {
                        string portId = getThisRow[dicHeaderIndexes["Id"]];
                        lastReadPortId = portId;
                        string addressId = getThisRow[dicHeaderIndexes["AddressId"]];
                        string portNumber = getThisRow[dicHeaderIndexes["PortNumber"]];
                        bool isVitualPort = false;
                        if (dicHeaderIndexes.ContainsKey("IsVitualPort"))
                        {
                            isVitualPort = bool.Parse(getThisRow[dicHeaderIndexes["IsVitualPort"]]);
                        }
                        MapPort port = new MapPort()
                        {
                            ID = portId,
                            ReferenceAddressId = addressId,
                            Number = portNumber,
                            IsVitualPort = isVitualPort
                        };

                        if (!Vehicle.MapInfo.portMap.ContainsKey(port.ID))
                        {
                            Vehicle.MapInfo.portMap.Add(port.ID, port);
                        }

                        //if (Vehicle.Mapinfo.addressMap.ContainsKey(addressId))
                        //{
                        //    Vehicle.Mapinfo.addressMap[addressId].PortIdMap.Add(port.ID, port);
                        //}

                    }
                    catch (Exception ex)
                    {
                        LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"lastReadPortId=[{lastReadPortId}]" + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"lastReadPortId=[{lastReadPortId}]" + ex.Message);
            }
        }

        private string FitZero(string v)
        {
            int sectionIdToInt = int.Parse(v);
            return sectionIdToInt.ToString("0000");
        }

        private void ReadAgvStationCsv()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(AgvStationPath))
                {
                    return;
                }              

                string[] allRows = File.ReadAllLines(AgvStationPath);
                if (allRows == null || allRows.Length < 2)
                {
                    return;
                }

                string[] titleRow = allRows[0].Split(',');
                allRows = allRows.Skip(1).ToArray();

                int nRows = allRows.Length;
                int nColumns = titleRow.Length;

                Dictionary<string, int> dicHeaderIndexes = new Dictionary<string, int>();
                for (int i = 0; i < nColumns; i++)
                {
                    var keyword = titleRow[i].Trim();
                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        dicHeaderIndexes.Add(keyword, i);
                    }
                }
                for (int i = 0; i < nRows; i++)
                {
                    string[] getThisRow = allRows[i].Split(',');
                    try
                    {
                        string stationId = getThisRow[dicHeaderIndexes["Id"]];
                        failAgvStationId = stationId;
                        string addressId = getThisRow[dicHeaderIndexes["AddressId"]];
                        failAddressIdInReadAgvStationFile = addressId;                     

                        if (Vehicle.MapInfo.addressMap.ContainsKey(addressId))
                        {
                            Vehicle.MapInfo.addressMap[addressId].AgvStationId = stationId;
                            if (Vehicle.MapInfo.agvStationMap.ContainsKey(stationId))
                            {
                                Vehicle.MapInfo.agvStationMap[stationId].AddressIds.Add(addressId);
                            }
                            else
                            {
                                MapAgvStation agvStation = new MapAgvStation();
                                agvStation.ID = stationId;
                                agvStation.AddressIds.Add(addressId);
                                Vehicle.MapInfo.agvStationMap.Add(stationId, agvStation);
                            }
                        }
                        else
                        {
                            throw new Exception("Address ID not in the addressMap.");
                        }

                    }
                    catch (Exception ex)
                    {
                        LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"FailStationId=[{failAgvStationId}], FailAddressId=[{failAddressIdInReadAgvStationFile}]. " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"lastReadPortId=[{lastReadPortId}]" + ex.Message);
            }
        }

        public void ReadSectionCsv()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SectionPath))
                {
                    throw new Exception($"IsSectionPathNull={string.IsNullOrWhiteSpace(SectionPath)}");
                }
                Vehicle.MapInfo.sectionMap.Clear();

                string[] allRows = File.ReadAllLines(SectionPath);
                if (allRows == null || allRows.Length < 2)
                {
                    throw new Exception($"There are no section in file");
                }

                string[] titleRow = allRows[0].Split(',');
                allRows = allRows.Skip(1).ToArray();

                int nRows = allRows.Length;
                int nColumns = titleRow.Length;

                Dictionary<string, int> dicHeaderIndexes = new Dictionary<string, int>();
                //Id, FromAddress, ToAddress, Speed, Type, PermitDirection
                for (int i = 0; i < nColumns; i++)
                {
                    var keyword = titleRow[i].Trim();
                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        dicHeaderIndexes.Add(keyword, i);
                    }
                }

                for (int i = 0; i < nRows; i++)
                {
                    string[] getThisRow = allRows[i].Split(',');
                    MapSection oneRow = new MapSection();
                    try
                    {
                        oneRow.Id = getThisRow[dicHeaderIndexes["Id"]];
                        if (!Vehicle.MapInfo.addressMap.ContainsKey(getThisRow[dicHeaderIndexes["FromAddress"]]))
                        {
                            LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"LoadSectionCsv read oneRow fail, headAddress is not in the map : [secId={oneRow.Id}][headAddress={getThisRow[dicHeaderIndexes["FromAddress"]]}]");
                        }
                        oneRow.HeadAddress = Vehicle.MapInfo.addressMap[getThisRow[dicHeaderIndexes["FromAddress"]]];
                        oneRow.InsideAddresses.Add(oneRow.HeadAddress);
                        if (!Vehicle.MapInfo.addressMap.ContainsKey(getThisRow[dicHeaderIndexes["ToAddress"]]))
                        {
                            LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"LoadSectionCsv read oneRow fail, tailAddress is not in the map : [secId={oneRow.Id}][tailAddress={getThisRow[dicHeaderIndexes["ToAddress"]]}]");
                        }
                        oneRow.TailAddress = Vehicle.MapInfo.addressMap[getThisRow[dicHeaderIndexes["ToAddress"]]];
                        oneRow.InsideAddresses.Add(oneRow.TailAddress);
                        oneRow.HeadToTailDistance = GetDistance(oneRow.HeadAddress.Position, oneRow.TailAddress.Position);
                        if (dicHeaderIndexes.ContainsKey("Speed"))
                        {
                            oneRow.Speed = double.Parse(getThisRow[dicHeaderIndexes["Speed"]]);
                        }
                        if (dicHeaderIndexes.ContainsKey("Type"))
                        {
                            oneRow.Type = oneRow.SectionTypeParse(getThisRow[dicHeaderIndexes["Type"]]);
                        }  
                    }
                    catch (Exception ex)
                    {
                        LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"LoadSectionCsv read oneRow fail : [lastReadSecId={lastReadSecId}][{ex.Message}]");
                    }

                    lastReadSecId = oneRow.Id;
                    Vehicle.MapInfo.sectionMap.Add(oneRow.Id, oneRow);
                }

                //LoadBeamSensorDisable();

                AddInsideAddresses();

                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"Load Section File Ok. [lastReadSecId={lastReadSecId}]");
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"LoadSectionCsv : [lastReadSecId={lastReadSecId}][{ex.Message}]");
            }
        }

        private void WriteAddressBackup()
        {
            var directionName = Path.GetDirectoryName(AddressPath);
            if (!Directory.Exists(directionName))
            {
                Directory.CreateDirectory(directionName);
            }

            var backupPath = Path.ChangeExtension(AddressPath, ".backup.csv");

            string titleRow = "Id,PositionX,PositionY,TransferPortDirection,GateType,PioDirection,ChargeDirection,CanSpin,IsTR50,InsideSectionId,OffsetX,OffsetY,OffsetTheta,VehicleHeadAngle" + Environment.NewLine;
            File.WriteAllText(backupPath, titleRow);
            List<string> lineInfos = new List<string>();
            foreach (var item in Vehicle.MapInfo.addressMap.Values)
            {
                var lineInfo = string.Format("{0},{1:F0},{2:F0},{3},{4},{5},{6},{7},{8},{9:0000},{10:F2},{11:F2},{12:F2},{13:N0}",
                    item.Id, item.Position.X, item.Position.Y, item.LoadUnloadDirection, item.GateType, item.PioDirection, item.ChargingDirection,
                    item.CanSpin, item.IsTR50,
                   int.Parse(item.InsideSectionId), item.AddressOffset.OffsetX, item.AddressOffset.OffsetY, item.AddressOffset.OffsetTheta,
                   item.VehicleHeadAngle
                    );
                lineInfos.Add(lineInfo);
            }
            File.AppendAllLines(backupPath, lineInfos);
        }

        private void WriteSectionBackup()
        {
            var directionName = Path.GetDirectoryName(SectionPath);
            if (!Directory.Exists(directionName))
            {
                Directory.CreateDirectory(directionName);
            }

            var backupPath = Path.ChangeExtension(SectionPath, ".backup.csv");

            string titleRow = "Id,FromAddress,ToAddress,Speed,Type" + Environment.NewLine;
            File.WriteAllText(backupPath, titleRow);
            List<string> lineInfos = new List<string>();
            foreach (var item in Vehicle.MapInfo.sectionMap.Values)
            {
                var lineInfo = string.Format("{0},{1},{2},{3},{4}",
                    item.Id, item.HeadAddress.Id, item.TailAddress.Id, item.Speed, item.Type
                    );
                lineInfos.Add(lineInfo);
            }
            File.AppendAllLines(backupPath, lineInfos);
        }

        private void AddInsideAddresses()
        {
            try
            {
                foreach (var adr in Vehicle.MapInfo.addressMap.Values)
                {
                    if (Vehicle.MapInfo.sectionMap.ContainsKey(adr.InsideSectionId))
                    {
                        Vehicle.MapInfo.sectionMap[adr.InsideSectionId].InsideAddresses.Add(adr);
                    }
                }

                LogDebug(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, $"AddInsideAddresses Ok.");
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name,
                    $"AddInsideAddresses FAIL at Sec[{lastReadSecId}] and Adr[{lastReadAdrId}]" + ex.Message);
            }
        }

        public double GetDistance(MapPosition aPosition, MapPosition bPosition)
        {
            var diffX = Math.Abs(aPosition.X - bPosition.X);
            var diffY = Math.Abs(aPosition.Y - bPosition.Y);
            return Math.Sqrt((diffX * diffX) + (diffY * diffY));
        }

        #region Log

        private NLog.Logger _transferLogger = NLog.LogManager.GetLogger("Transfer");

        private void LogException(string classMethodName, string exMsg)
        {
            //mirleLogger.Log(new LogFormat("MainError", "5", classMethodName, "Device", "CarrierID", exMsg));

            _transferLogger.Error($"[{Model.Vehicle.Instance.SoftwareVersion}][{Model.Vehicle.Instance.AgvcConnectorConfig.ClientName}][{classMethodName}][{exMsg}]");
        }

        private void LogDebug(string classMethodName, string msg)
        {
            try
            {
                //mirleLogger.Log(new LogFormat("MainDebug", "5", classMethodName, "DeviceID", "CarrierID", msg));

                _transferLogger.Debug($"[{Vehicle.SoftwareVersion}][{Vehicle.AgvcConnectorConfig.ClientName}][{classMethodName}][{msg}]");
            }
            catch (Exception)
            {
            }
        }

        #endregion
    }

}
