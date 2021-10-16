using Mirle.Agv.INX.Controller.Tools;
using Mirle.Agv.INX.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Mirle.Agv.INX.Controller
{
    public class MapHandler
    {
        private string normalLogName = "";
        private string device = MethodInfo.GetCurrentMethod().ReflectedType.Name;
        private ComputeFunction computeFunction = ComputeFunction.Instance;

        private LoggerAgent loggerAgent = LoggerAgent.Instance;
        private MapInfo mapInfo;

        private LocalData localData = LocalData.Instance;

        #region aaddress.
        private string address_ID = "Id";
        private string address_PositionX = "PositionX";
        private string address_PositionY = "PositionY";
        private string address_Angle = "Angle";
        private string address_LoadUnloadDirection = "LoadUnloadDirection";
        private string address_StageNumber = "StageNumber";
        private string address_NeedPIO = "NeedPIO";
        private string address_ChargingDirection = "ChargingDirection";
        private string address_RFPIOChannelID = "RFPIOChannelID";
        private string address_RFPIODeviceID = "RFPIODeviceID";
        private string address_AddressName = "AddressName";
        #endregion

        #region section.
        private string section_ID = "Id";
        private string section_FromAddress = "FromAddress";
        private string section_ToAddress = "ToAddress";
        private string section_Speed = "Speed";
        private string section_FromVehicleAngle = "FromVehicleAngle";
        private string section_ToVehicleAngle = "ToVehicleAngle";
        #endregion

        private string addressCSVFileName = "AADDRESS.csv";
        private string sectionCSVFileName = "ASECTION.csv";

        public MapHandler(string normalLogName)
        {
            this.normalLogName = normalLogName;
            InitialMap();
        }

        private void InitialMap()
        {
            mapInfo = new MapInfo();
            ReadAddressCsv(Path.Combine(localData.MapConfig.FileDirectory, addressCSVFileName));
            ReadSectionCsv(Path.Combine(localData.MapConfig.FileDirectory, sectionCSVFileName));
            ProcessSectionData();
            ProcessAddressInsideSectionAndNearbySection();
            ProcessSectionNearbySection();

            localData.TheMapInfo = mapInfo;
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

        #region 讀取Address.csv / Section.csv .
        public void ReadAddressCsv(string aaddressPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(aaddressPath))
                {
                    WriteLog(5, "", "MapConfig (FileDirectory + AddressCSVFileName) is null or whiteSpace");
                    return;
                }

                string[] allRows = File.ReadAllLines(aaddressPath);

                try
                {
                    using (StreamWriter outputFile = new StreamWriter(Path.Combine(Environment.CurrentDirectory, addressCSVFileName)))
                    {
                        for (int i = 0; i < allRows.Length; i++)
                            outputFile.WriteLine(allRows[i]);
                    }
                }
                catch { }


                if (allRows == null || allRows.Length < 1)
                {
                    WriteLog(5, "", String.Concat("aadress.csv line == 0"));
                    return;
                }

                string[] titleRow = allRows[0].Split(',');
                allRows = allRows.Skip(1).ToArray();

                int nRows = allRows.Length;
                int nColumns = titleRow.Length;

                Dictionary<string, int> dicHeaderIndexes = new Dictionary<string, int>();

                for (int i = 0; i < nColumns; i++)
                {
                    string keyword = titleRow[i].Trim();

                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        if (dicHeaderIndexes.ContainsKey(keyword))
                        {
                            WriteLog(3, "", String.Concat("Title : ", keyword, " repeat"));
                            return;
                        }
                        else
                            dicHeaderIndexes.Add(keyword, i);
                    }
                }

                if (dicHeaderIndexes.ContainsKey(address_ID) && dicHeaderIndexes.ContainsKey(address_PositionX) &&
                    dicHeaderIndexes.ContainsKey(address_PositionY))
                {
                }
                else
                {
                    WriteLog(3, "", String.Concat(aaddressPath, " title 必須要有", address_ID, ",", address_PositionX, ",", address_PositionY, ",", address_Angle));
                    return;
                }

                int index = 0;
                EnumStageDirection tempDirection;
                bool tempBool;
                int tempInt;

                for (; index < nRows; index++)
                {
                    string[] getThisRow = allRows[index].Split(',');
                    MapAddress oneRow = new MapAddress();

                    try
                    {
                        oneRow.Id = getThisRow[dicHeaderIndexes[address_ID]];
                        oneRow.AGVPosition.Position.X = double.Parse(getThisRow[dicHeaderIndexes[address_PositionX]]);
                        oneRow.AGVPosition.Position.Y = double.Parse(getThisRow[dicHeaderIndexes[address_PositionY]]);
                        oneRow.AGVPosition.Angle = double.Parse(getThisRow[dicHeaderIndexes[address_Angle]]);
                        
                        if (dicHeaderIndexes.ContainsKey(address_LoadUnloadDirection))
                        {
                            if (Enum.TryParse(getThisRow[dicHeaderIndexes[address_LoadUnloadDirection]], out tempDirection))
                                oneRow.LoadUnloadDirection = tempDirection;
                            else
                                WriteLog(3, "", String.Concat("LoadUnloadDirection Enum.TryParse 失敗 : ", getThisRow[dicHeaderIndexes[address_LoadUnloadDirection]]));
                        }

                        if (dicHeaderIndexes.ContainsKey(address_NeedPIO))
                        {
                            if (bool.TryParse(getThisRow[dicHeaderIndexes[address_NeedPIO]], out tempBool))
                                oneRow.NeedPIO = tempBool;
                            else
                                WriteLog(3, "", String.Concat("NeedPIO bool.TryParse 失敗 : ", getThisRow[dicHeaderIndexes[address_NeedPIO]]));
                        }

                        if (dicHeaderIndexes.ContainsKey(address_StageNumber))
                        {
                            if (Int32.TryParse(getThisRow[dicHeaderIndexes[address_StageNumber]], out tempInt))
                                oneRow.StageNumber = tempInt;
                            else
                                WriteLog(3, "", String.Concat("StageNumber Int32.TryParse 失敗 : ", getThisRow[dicHeaderIndexes[address_StageNumber]]));
                        }

                        if (dicHeaderIndexes.ContainsKey(address_ChargingDirection))
                        {
                            if (Enum.TryParse(getThisRow[dicHeaderIndexes[address_ChargingDirection]], out tempDirection))
                                oneRow.ChargingDirection = tempDirection;
                            else
                                WriteLog(3, "", String.Concat("ChargingDirection Enum.TryParse 失敗 : ", getThisRow[dicHeaderIndexes[address_ChargingDirection]]));
                        }

                        if (dicHeaderIndexes.ContainsKey(address_RFPIOChannelID))
                            oneRow.RFPIOChannelID = getThisRow[dicHeaderIndexes[address_RFPIOChannelID]];

                        if (dicHeaderIndexes.ContainsKey(address_RFPIODeviceID))
                            oneRow.RFPIODeviceID = getThisRow[dicHeaderIndexes[address_RFPIODeviceID]];

                        if (dicHeaderIndexes.ContainsKey(address_AddressName))
                            oneRow.AddressName = getThisRow[dicHeaderIndexes[address_AddressName]];

                        mapInfo.AllAddress.Add(oneRow.Id, oneRow);
                    }
                    catch (Exception ex)
                    {
                        WriteLog(3, "", String.Concat("Exception (line = ", (index + 2).ToString(), ") : ", ex.ToString()));
                    }
                }

                WriteLog(7, "", String.Concat("ReadAddressCsv Success, address count : ", mapInfo.AllAddress.Count.ToString("0")));
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        public void ReadSectionCsv(string sectionPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sectionPath))
                {
                    WriteLog(5, "", "MapConfig (FileDirectory + SectionCSVFileName) is null or whiteSpace");
                    return;
                }

                string[] allRows = File.ReadAllLines(sectionPath);

                try
                {
                    using (StreamWriter outputFile = new StreamWriter(Path.Combine(Environment.CurrentDirectory, sectionCSVFileName)))
                    {
                        for (int i = 0; i < allRows.Length; i++)
                            outputFile.WriteLine(allRows[i]);
                    }
                }
                catch { }

                if (allRows == null || allRows.Length < 1)
                {
                    WriteLog(5, "", String.Concat("section.csv line == 0"));
                    return;
                }

                string[] titleRow = allRows[0].Split(',');
                allRows = allRows.Skip(1).ToArray();

                int nRows = allRows.Length;
                int nColumns = titleRow.Length;

                Dictionary<string, int> dicHeaderIndexes = new Dictionary<string, int>();

                for (int i = 0; i < nColumns; i++)
                {
                    string keyword = titleRow[i].Trim();
                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        if (dicHeaderIndexes.ContainsKey(keyword))
                        {
                            WriteLog(3, "", String.Concat("Title : ", keyword, " repeat"));
                            return;
                        }
                        else
                            dicHeaderIndexes.Add(keyword, i);
                    }
                }

                if (dicHeaderIndexes.ContainsKey(section_ID) && dicHeaderIndexes.ContainsKey(section_FromAddress) &&
                    dicHeaderIndexes.ContainsKey(section_ToAddress) && dicHeaderIndexes.ContainsKey(section_Speed) &&
                    dicHeaderIndexes.ContainsKey(section_FromVehicleAngle) && dicHeaderIndexes.ContainsKey(section_ToVehicleAngle))
                {
                }
                else
                {
                    WriteLog(3, "", String.Concat(sectionPath, " title 必須要有", section_ID, ",", section_FromAddress, ",", section_ToAddress, ",", section_Speed, ",", section_FromVehicleAngle, ",", section_ToVehicleAngle));
                    return;
                }

                int index = 0;

                for (; index < nRows; index++)
                {
                    string[] getThisRow = allRows[index].Split(',');
                    MapSection oneRow = new MapSection();

                    try
                    {

                        if (!mapInfo.AllAddress.ContainsKey(getThisRow[dicHeaderIndexes[section_FromAddress]]))
                            WriteLog(3, "", String.Concat("line : ", (index + 2).ToString(), ", ", section_FromAddress, " not find in address list"));
                        else if (!mapInfo.AllAddress.ContainsKey(getThisRow[dicHeaderIndexes[section_ToAddress]]))
                            WriteLog(3, "", String.Concat("line : ", (index + 2).ToString(), ", ", section_ToAddress, " not find in address list"));
                        else
                        {

                            oneRow.Id = getThisRow[dicHeaderIndexes[section_ID]];
                            oneRow.FromAddress = mapInfo.AllAddress[getThisRow[dicHeaderIndexes[section_FromAddress]]];
                            oneRow.ToAddress = mapInfo.AllAddress[getThisRow[dicHeaderIndexes[section_ToAddress]]];
                            oneRow.FromVehicleAngle = double.Parse(getThisRow[dicHeaderIndexes[section_FromVehicleAngle]]);
                            oneRow.ToVehicleAngle = double.Parse(getThisRow[dicHeaderIndexes[section_ToVehicleAngle]]);
                            oneRow.Distance = computeFunction.GetDistanceFormTwoAGVPosition(oneRow.FromAddress.AGVPosition, oneRow.ToAddress.AGVPosition);
                            oneRow.Speed = double.Parse(getThisRow[dicHeaderIndexes[section_Speed]]);

                            mapInfo.AllSection.Add(oneRow.Id, oneRow);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteLog(3, "", String.Concat("Exception (line = ", (index + 2).ToString(), ") : ", ex.ToString()));
                    }
                }

                WriteLog(7, "", String.Concat("ReadAddressCsv Success, section count : ", mapInfo.AllSection.Count.ToString("0")));
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }
        #endregion

        #region 處理相鄰Section ( address 和 section皆有).
        private void ProcessSectionData()
        {
            try
            {
                foreach (MapSection section in mapInfo.AllSection.Values)
                {
                    double sectionAngle = computeFunction.ComputeAngle(section.FromAddress.AGVPosition, section.ToAddress.AGVPosition);

                    section.SectionAngle = sectionAngle;
                    section.CosTheta = Math.Cos(sectionAngle / 180 * Math.PI);
                    section.SinTheta = Math.Sin(sectionAngle / 180 * Math.PI);
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        private void ProcessAddressInsideSectionAndNearbySection()
        {
            try
            {
                double angle;

                foreach (MapAddress address in mapInfo.AllAddress.Values)
                {
                    if (mapInfo.AllSection.ContainsKey(address.InsideSectionId))
                        address.InsideSection = mapInfo.AllSection[address.InsideSectionId];


                    foreach (MapSection section in mapInfo.AllSection.Values)
                    {
                        if (section.FromAddress == address || section.ToAddress == address)
                        {
                            if (!address.NearbySection.Contains(section))
                                address.NearbySection.Add(section);
                        }
                    }

                    foreach (MapSection section in address.NearbySection)
                    {
                        if (section.FromAddress == address)
                            angle = section.FromVehicleAngle;
                        else
                            angle = section.ToVehicleAngle;

                        if (address.AGVPosition.Angle != angle)
                            address.CanSpin = true;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        private void ProcessSectionNearbySection()
        {
            try
            {
                foreach (MapSection section in mapInfo.AllSection.Values)
                {
                    foreach (MapSection section2 in mapInfo.AllSection.Values)
                    {
                        if (section.FromAddress == section2.FromAddress || section.FromAddress == section2.ToAddress ||
                            section.ToAddress == section2.FromAddress || section.ToAddress == section2.ToAddress)
                        {
                            if (!section.NearbySection.Contains(section2))
                                section.NearbySection.Add(section2);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }
        #endregion
    }
}
