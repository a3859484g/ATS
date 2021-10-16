using Mirle.Agv.INX.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Controller
{
    public class LocateDriver_SLAM : LocateDriver
    {
        protected bool changeSlamPosition = false;
        protected LocateAGVPosition originAGVPosition = null;
        protected LocateAGVPosition offsetAGVPosition = null;
        protected LocateAGVPosition nowAGVPosition = null;

        protected Dictionary<string, MapAGVPosition> slamPosition = new Dictionary<string, MapAGVPosition>();
        protected Dictionary<string, MapAGVPosition> setSlamPosition = new Dictionary<string, MapAGVPosition>();
        protected Dictionary<string, SectionLine> findSectionList = new Dictionary<string, SectionLine>();
        protected Dictionary<string, double> sectionSLAMAngleRange = new Dictionary<string, double>();
        protected Dictionary<string, double> sectionSLAMAngle = new Dictionary<string, double>();

        protected Dictionary<string, SLAMTransfer> sectionSLAMTransferLit = new Dictionary<string, SLAMTransfer>();

        protected Dictionary<string, List<string>> sectionConnectSectionList = new Dictionary<string, List<string>>();

        protected string readSlamPositionPath = "";
        protected string writeSlamPositionPath = "";

        //protected SLAMTransfer transferData_Manual = new SLAMTransfer();
        protected string manualSection = "";

        protected Thread searchSectionThread = null;
        protected DateTime lastSearchSectionTime = DateTime.Now;

        protected SLAMOffseet slamOffset = null;
        protected bool setPositioning = false;
        protected bool setPositionEnd = false;
        protected MapAGVPosition lastOriginPosition = null;
        protected double sectionRange = 0;

        override public void InitialDriver(LocateDriverConfig driverConfig, AlarmHandler alarmHandler, string normalLogName)
        {
            LocateType = EnumLocateType.SLAM;

            this.normalLogName = normalLogName;
            this.DriverConfig = driverConfig;
            this.alarmHandler = alarmHandler;
            device = driverConfig.LocateDriverType.ToString();
            InitialConfig(driverConfig.Path);
        }

        #region 確認Sectoint長度是否正常.
        protected void CheckSectionDistance(double sectionDistanceMagnification, double sectionDistanceConstant)
        {
            double realDistance;
            double slamDistacne;
            double allowDistanceDelta;

            foreach (MapSection section in localData.TheMapInfo.AllSection.Values)
            {
                if (section.FromVehicleAngle == section.ToVehicleAngle &&
                    slamPosition[section.FromAddress.Id] != null && slamPosition[section.ToAddress.Id] != null)
                {
                    realDistance = computeFunction.GetTwoPositionDistance(section.FromAddress.AGVPosition, section.ToAddress.AGVPosition);
                    slamDistacne = computeFunction.GetTwoPositionDistance(slamPosition[section.FromAddress.Id].Position, slamPosition[section.ToAddress.Id].Position);
                    allowDistanceDelta = realDistance * sectionDistanceMagnification + sectionDistanceConstant;

                    if (Math.Abs(realDistance - slamDistacne) > allowDistanceDelta)
                    {
                        WriteLog(3, "", String.Concat("Section : ", section.Id, " 長度異常, 圖資長度 = ", realDistance.ToString("0"), ", Slam圖資長度 = ", slamDistacne.ToString("0"),
                                                       ", config.SectionDistanceMagnification = ", sectionDistanceMagnification.ToString("0.00"),
                                                       ", config.SectionDistanceConstant = ", sectionDistanceConstant.ToString("0")));
                    }
                }
            }
        }
        #endregion

        #region 設定透過Slam資料搜尋所在Section所使用的資料.
        protected void SetFindSectionData()
        {
            SectionLine slamSection;
            SLAMTransfer slamTransfer;
            MapAddress from;
            MapAddress to;
            double sectionAngle;
            double sectionDistance;

            foreach (string key in slamPosition.Keys)
            {
                if (localData.SimulateMode)
                    localData.TheMapInfo.AllAddress[key].Enalbe = true;
                else
                {
                    if (localData.TheMapInfo.AllAddress.ContainsKey(key))
                        localData.TheMapInfo.AllAddress[key].Enalbe = (slamPosition[key] != null);
                }
            }

            string cantMovingSection = "";

            foreach (MapSection section in localData.TheMapInfo.AllSection.Values)
            {
                if (slamPosition.ContainsKey(section.FromAddress.Id) && slamPosition[section.FromAddress.Id] != null &&
                    slamPosition.ContainsKey(section.ToAddress.Id) && slamPosition[section.ToAddress.Id] != null)
                {
                    from = new MapAddress();
                    from.Id = section.FromAddress.Id;
                    from.AGVPosition = slamPosition[section.FromAddress.Id];

                    to = new MapAddress();
                    to.Id = section.ToAddress.Id;
                    to.AGVPosition = slamPosition[section.ToAddress.Id];

                    sectionAngle = computeFunction.ComputeAngle(from.AGVPosition, to.AGVPosition);
                    sectionDistance = computeFunction.GetDistanceFormTwoAGVPosition(from.AGVPosition, to.AGVPosition);

                    slamSection = new SectionLine(section, from, to, sectionAngle, sectionDistance, 0, true, 0);

                    if (findSectionList.ContainsKey(section.Id))
                        findSectionList[section.Id] = slamSection;
                    else
                        findSectionList.Add(section.Id, slamSection);

                    slamTransfer = new SLAMTransfer();
                    slamTransfer.Step1Offset = slamPosition[section.FromAddress.Id].Position;

                    if (sectionSLAMTransferLit.ContainsKey(section.Id))
                        sectionSLAMTransferLit[section.Id] = GetTransferBySection(section);
                    else
                        sectionSLAMTransferLit.Add(section.Id, GetTransferBySection(section));

                    SLAMTransfer temp = sectionSLAMTransferLit[section.Id];
                    WriteLog(7, "", String.Concat("Section : ", section.Id,
                       ", Step1Offset (", computeFunction.GetMapPositionString(temp.Step1Offset, "0"),
                       "), Step2Cos : ", temp.Step2Cos.ToString("0.0"),
                       ", Step2Sin : ", temp.Step2Sin.ToString("0.0"),
                       ", Step3Mag : ", temp.Step3Mag.ToString("0.0"),
                       ", Step4Offset (", computeFunction.GetMapPositionString(temp.Step4Offset, "0"),
                       "), ThetaOffset : ", temp.ThetaOffset.ToString("0.0"),
                       ", ThetaOffsetStart : ", temp.ThetaOffsetStart.ToString("0.0"),
                       ", ThetaOffsetEnd : ", temp.ThetaOffsetEnd.ToString("0.0"),
                       ", Distance : ", temp.Distance.ToString("0.0")));
                }
                else
                {
                    if (cantMovingSection != "")
                        cantMovingSection = String.Concat(cantMovingSection, ",");

                    cantMovingSection = String.Concat(cantMovingSection, section.Id);
                }
            }

            if (cantMovingSection != "")
                WriteLog(7, "", String.Concat("未採點Section : ", cantMovingSection));

            List<string> tempList;

            foreach (MapSection section in localData.TheMapInfo.AllSection.Values)
            {
                if (findSectionList.ContainsKey(section.Id))
                {
                    tempList = new List<string>();

                    for (int i = 0; i < section.NearbySection.Count; i++)
                    {
                        if (findSectionList.ContainsKey(section.NearbySection[i].Id))
                            tempList.Add(section.NearbySection[i].Id);
                    }

                    if (sectionConnectSectionList.ContainsKey(section.Id))
                        sectionConnectSectionList[section.Id] = tempList;
                    else
                        sectionConnectSectionList.Add(section.Id, tempList);
                }
            }
        }

        private SLAMTransfer GetTransferBySection(MapSection section)
        {
            SLAMTransfer temp = new SLAMTransfer();
            temp.Step1Offset = slamPosition[section.FromAddress.Id].Position;
            temp.Step4Offset = section.FromAddress.AGVPosition.Position;

            temp.Step3Mag = section.Distance / computeFunction.GetTwoPositionDistance(slamPosition[section.FromAddress.Id].Position, slamPosition[section.ToAddress.Id].Position);

            double mapAngle = computeFunction.ComputeAngle(section.FromAddress.AGVPosition.Position, section.ToAddress.AGVPosition.Position);
            MapPosition start = new MapPosition(slamPosition[section.FromAddress.Id].Position.X, -slamPosition[section.FromAddress.Id].Position.Y);
            MapPosition end = new MapPosition(slamPosition[section.ToAddress.Id].Position.X, -slamPosition[section.ToAddress.Id].Position.Y);

            double slamAngle = computeFunction.ComputeAngle(start, end);
            temp.ThetaOffset = mapAngle - slamAngle;

            // theta 要帶-/+?
            temp.Step2Sin = Math.Sin(-temp.ThetaOffset * Math.PI / 180);
            temp.Step2Cos = Math.Cos(-temp.ThetaOffset * Math.PI / 180);
            temp.ThetaOffset = (slamPosition[section.FromAddress.Id].Angle + slamPosition[section.ToAddress.Id].Angle) / 2;

            double slamStartAngle = slamPosition[section.FromAddress.Id].Angle - (section.FromVehicleAngle - section.FromAddress.AGVPosition.Angle);
            double slamEndAngle = slamPosition[section.ToAddress.Id].Angle - (section.ToVehicleAngle - section.ToAddress.AGVPosition.Angle);

            //temp.ThetaOffsetStart = computeFunction.GetCurrectAngle(slamPosition[section.FromAddress.Id].Angle - (section.FromVehicleAngle - section.FromAddress.AGVPosition.Angle));
            //temp.ThetaOffsetEnd = computeFunction.GetCurrectAngle(slamPosition[section.ToAddress.Id].Angle - (/*section.ToVehicleAngle -*/ section.ToAddress.AGVPosition.Angle));

            temp.ThetaOffsetStart = slamStartAngle + section.FromVehicleAngle;
            temp.ThetaOffsetEnd = slamEndAngle + section.ToVehicleAngle;

            //temp.ThetaOffsetStart = section.FromVehicleAngle + slamPosition[section.FromAddress.Id].Angle + (section.FromAddress.AGVPosition.Angle - section.FromVehicleAngle);
            //temp.ThetaOffsetEnd = section.ToVehicleAngle + slamPosition[section.ToAddress.Id].Angle + (section.ToAddress.AGVPosition.Angle - section.ToVehicleAngle);

            double angle = computeFunction.ComputeAngle(start, end);
            temp.SinTheta = Math.Sin(-angle * Math.PI / 180);
            temp.CosTheta = Math.Cos(-angle * Math.PI / 180);
            temp.Distance = computeFunction.GetTwoPositionDistance(start, end);

            return temp;
        }
        #endregion

        protected void SetSLAMOffset()
        {
        }

        private void ReadOffsetXML(string localPath)
        {
            string path = Path.Combine(new DirectoryInfo(localPath).Parent.FullName, "SlamOffset.txt");

            try
            {
                if (!File.Exists(path))
                {
                    WriteLog(7, "", String.Concat("SlamOffset.txt不存在, SlamOffset = 0, Path : ", path));
                    return;
                }

                string[] allRows = File.ReadAllLines(path);

                if (allRows.Length != 1)
                {
                    WriteLog(3, "", "SlamOffset.txt 格式不對(行數)");
                    return;
                }

                string[] data = Regex.Split(allRows[0], ",", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));

                if (data == null || data.Length != 3)
                    WriteLog(3, "", "SlamOffset.txt 格式不對(thetaOffset,極座標角度,極座標距離)");
                else
                {
                    double thetaOffset;
                    double polar_Theta;
                    double polar_Distance;

                    if (double.TryParse(data[0], out thetaOffset) && double.TryParse(data[1], out polar_Theta) && double.TryParse(data[2], out polar_Distance))
                    {
                        slamOffset = new SLAMOffseet(thetaOffset, polar_Theta, polar_Distance);

                        WriteLog(7, "", String.Concat("SlamOffset : thetaOffset = ", thetaOffset.ToString("0.000"), ", polar_Theta = ", polar_Theta.ToString("0.000"), ", polar_Distance = ", polar_Distance.ToString("0.00")));

                        if (thetaOffset == 0 && polar_Theta == 0 && polar_Distance == 0)
                            slamOffset = null;
                    }
                    else
                        WriteLog(3, "", "SlamOffset.txt 格式不對(有非浮點數)");
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        #region 讀取SlamAddress.csv.
        protected bool ReadSLAMAddress(string path)
        {
            if (path == null || path == "")
            {
                WriteLog(3, "", "SLAMAddress 路徑錯誤為null或空值.");
                return false;
            }
            else if (!File.Exists(path))
            {
                WriteLog(3, "", String.Concat("找不到 SLAMAddress.csv, path : ", path));
                return false;
            }

            try
            {
                readSlamPositionPath = path;
                writeSlamPositionPath = path.Replace(".csv", "_out.csv");

                string[] allRows = File.ReadAllLines(path);
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
                        if (dicHeaderIndexes.ContainsKey(keyword))
                            WriteLog(3, "", String.Concat("Title repeat : ", keyword));
                        else
                            dicHeaderIndexes.Add(keyword, i);
                    }
                }

                if (dicHeaderIndexes.ContainsKey("Address") && dicHeaderIndexes.ContainsKey("SLAM_X") &&
                    dicHeaderIndexes.ContainsKey("SLAM_Y") && dicHeaderIndexes.ContainsKey("SLAM_Theta"))
                {
                }
                else
                {
                    WriteLog(3, "", String.Concat("Title must be : Address,SLAM_X,SLAM_Y,SLAM_Theta"));
                    return false;
                }

                foreach (string addressID in localData.TheMapInfo.AllAddress.Keys)
                {
                    slamPosition.Add(addressID, null);
                }

                for (int i = 0; i < nRows; i++)
                {
                    string[] getThisRow = allRows[i].Split(',');
                    MapAGVPosition temp = new MapAGVPosition();

                    if (getThisRow.Length == 4)
                    {
                        temp.Position.X = double.Parse(getThisRow[dicHeaderIndexes["SLAM_X"]]);
                        temp.Position.Y = double.Parse(getThisRow[dicHeaderIndexes["SLAM_Y"]]);
                        temp.Angle = double.Parse(getThisRow[dicHeaderIndexes["SLAM_Theta"]]);

                        if (slamPosition.ContainsKey(getThisRow[dicHeaderIndexes["Address"]]))
                            slamPosition[getThisRow[dicHeaderIndexes["Address"]]] = temp;
                    }
                    else if (getThisRow.Length == 1)
                    {
                        if (slamPosition.ContainsKey(getThisRow[dicHeaderIndexes["Address"]]))
                            slamPosition[getThisRow[dicHeaderIndexes["Address"]]] = null;
                    }
                    else if (getThisRow.Length > 1)
                    {
                        WriteLog(5, "", String.Concat("Address : ", getThisRow[dicHeaderIndexes["Address"]], " 資料格式錯誤"));

                        if (slamPosition.ContainsKey(getThisRow[dicHeaderIndexes["Address"]]))
                            slamPosition[getThisRow[dicHeaderIndexes["Address"]]] = null;
                    }
                }

                ReadOffsetXML(path);
                SetFindSectionData();
                return true;
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }
        #endregion

        #region 判斷是否在Section內(以Slam資料判斷).
        virtual protected bool IsInSection(string sectionID)
        {
            if (!sectionSLAMTransferLit.ContainsKey(sectionID))
                return false;
            else if (localData.AutoManual == EnumAutoState.Auto ||
                     localData.MoveControlData.MoveCommand != null)
                return true;
            else
            {
                LocateAGVPosition slamNow = originAGVPosition;

                if (slamNow == null || slamNow.AGVPosition == null)
                    return false;

                MapPosition now = new MapPosition();

                now = computeFunction.GetTransferPosition(findSectionList[sectionID], slamNow.AGVPosition.Position);

                if (Math.Abs(now.Y) <= localData.MoveControlData.MoveControlConfig.Safety[EnumMoveControlSafetyType.OntimeReviseSectionDeviationLine].Range &&
                    (-localData.MoveControlData.CreateMoveCommandConfig.SafteyDistance[EnumCommandType.Move] <= now.X &&
                    now.X <= findSectionList[sectionID].Distance + localData.MoveControlData.CreateMoveCommandConfig.SafteyDistance[EnumCommandType.Move]))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        #endregion
        
        public bool SetPositionByAddressID(string addressID, double distanceRange, double angleRange, ref string errorMessage)
        {
            if (setPositioning)
            {
                errorMessage = "SetPosition中";
                return false;
            }
            else if (slamPosition.ContainsKey(addressID) && slamPosition[addressID] != null)
                return SetPositionAndWait(slamPosition[addressID], distanceRange, angleRange, ref errorMessage);
            else
            {
                errorMessage = String.Concat("AddressID : ", addressID, " 不存在或還未踩點");
                return false;
            }
        }

        public bool SetPositionByMapAGVPosition(MapAGVPosition position, double distanceRange, double angleRange, ref string errorMessage)
        {
            if (setPositioning)
            {
                errorMessage = "SetPosition中";
                return false;
            }
            else
                return SetPositionAndWait(position, distanceRange, angleRange, ref errorMessage);
        }

        virtual protected bool SetPositionAndWait(MapAGVPosition setPosition, double distanceRange, double angleRange, ref string errorMessage)
        {
            return false;
        }

        public LocateAGVPosition GetOriginAGVPosition
        {
            get
            {
                if (Status != EnumControlStatus.NotInitial && Status != EnumControlStatus.Initial)
                    //return originAGVPosition;
                    return offsetAGVPosition;
                else
                    return null;
            }
        }

        override public LocateAGVPosition GetLocateAGVPosition
        {
            get
            {
                if (Status == EnumControlStatus.Ready)
                    return nowAGVPosition;
                else
                    return null;
            }
        }

        #region 紀錄Slam座標.
        public bool SetSlamPositionAndReplace(string address, int averageCount)
        {
            if (!SetSlamPosition(address, averageCount))
                return false;
            else
            {
                changeSlamPosition = true;

                foreach (var newSlamData in setSlamPosition)
                {
                    if (slamPosition.ContainsKey(newSlamData.Key))
                        slamPosition[newSlamData.Key] = newSlamData.Value;
                }

                SetFindSectionData();

                changeSlamPosition = false;
            }

            return true;
        }

        public bool SetSlamPosition(string address, int averageCount)
        {
            double timeOutValue = averageCount * 1000;
            MapAGVPosition agvPosition = new MapAGVPosition();
            LocateAGVPosition temp;
            LocateAGVPosition lastMapAGVPosition = null;
            double angleDelta = 0;

            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Restart();

            for (int i = 0; i < averageCount;)
            {
                temp = offsetAGVPosition;

                if (temp != null && temp.AGVPosition != null && (lastMapAGVPosition == null || lastMapAGVPosition != temp))
                {
                    lastMapAGVPosition = temp;
                    agvPosition.Position.X += (temp.AGVPosition.Position.X / averageCount);
                    agvPosition.Position.Y += (temp.AGVPosition.Position.Y / averageCount);

                    if (i == 0)
                        agvPosition.Angle = temp.AGVPosition.Angle;

                    angleDelta += (computeFunction.GetCurrectAngle(temp.AGVPosition.Angle - agvPosition.Angle) / averageCount);
                    i++;
                }
                else if (timer.ElapsedMilliseconds > timeOutValue)
                {
                    return false;
                }
            }

            agvPosition.Angle = computeFunction.GetCurrectAngle(agvPosition.Angle + angleDelta);

            if (slamPosition.ContainsKey(address))
            {
                WriteLog(5, "", String.Concat("Address[", address, "] Slam 座標重新定義, 由 ",
                                computeFunction.GetMapAGVPositionStringWithAngle(slamPosition[address]), " 變更為 ",
                                computeFunction.GetMapAGVPositionStringWithAngle(agvPosition)));
            }
            else
                WriteLog(5, "", String.Concat("Address[", address, "] Slam 座標定義 ", computeFunction.GetMapAGVPositionStringWithAngle(agvPosition)));

            if (setSlamPosition.ContainsKey(address))
                setSlamPosition[address] = agvPosition;
            else
                setSlamPosition.Add(address, agvPosition);

            return true;
        }
        #endregion

        #region 寫檔SlamAddress.csv.
        public bool WirteSlamPosition_All()
        {
            try
            {
                string line = "";
                List<string> csvList = new List<string>();
                line = "Address,SLAM_X,SLAM_Y,SLAM_Theta";
                csvList.Add(line);

                foreach (var temp in slamPosition)
                {
                    if (setSlamPosition.ContainsKey(temp.Key))
                        line = String.Concat(temp.Key, ",", setSlamPosition[temp.Key].Position.X.ToString("0.000"), ",", setSlamPosition[temp.Key].Position.Y.ToString("0.000"), ",", setSlamPosition[temp.Key].Angle.ToString("0.000"));
                    else
                    {
                        if (temp.Value != null)
                            line = String.Concat(temp.Key, ",", temp.Value.Position.X.ToString("0.000"), ",", temp.Value.Position.Y.ToString("0.000"), ",", temp.Value.Angle.ToString("0.000"));
                        else
                            line = temp.Key;

                    }

                    csvList.Add(line);
                }

                foreach (var temp in setSlamPosition)
                {
                    if (!slamPosition.ContainsKey(temp.Key))
                    {
                        line = String.Concat(temp.Key, ",", temp.Value.Position.X.ToString("0.000"), ",", temp.Value.Position.Y.ToString("0.000"), ",", temp.Value.Angle.ToString("0.000"));
                        csvList.Add(line);
                    }
                }

                using (StreamWriter outputFile = new StreamWriter(writeSlamPositionPath))
                {
                    foreach (string t in csvList)
                        outputFile.WriteLine(t);
                }

                using (StreamWriter outputFile = new StreamWriter(readSlamPositionPath))
                {
                    foreach (string t in csvList)
                        outputFile.WriteLine(t);
                }

                return true;
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                return false;
            }
        }
        #endregion

        #region 內插計算出兩點之間的理想位置角度.
        private bool AddreesCIsInAddressAB(string addressA, string addressB, string addressC)
        {
            return computeFunction.ComputeAngle(localData.TheMapInfo.AllAddress[addressA].AGVPosition.Position, localData.TheMapInfo.AllAddress[addressB].AGVPosition.Position) ==
                   computeFunction.ComputeAngle(localData.TheMapInfo.AllAddress[addressC].AGVPosition.Position, localData.TheMapInfo.AllAddress[addressB].AGVPosition.Position) &&
                   computeFunction.ComputeAngle(localData.TheMapInfo.AllAddress[addressB].AGVPosition.Position, localData.TheMapInfo.AllAddress[addressA].AGVPosition.Position) ==
                   computeFunction.ComputeAngle(localData.TheMapInfo.AllAddress[addressC].AGVPosition.Position, localData.TheMapInfo.AllAddress[addressA].AGVPosition.Position);

        }

        private void 內插YO(string addressA, string addressB, string addressC)
        {
            MapAGVPosition yo = new MapAGVPosition();

            double abLength = computeFunction.GetDistanceFormTwoAGVPosition(localData.TheMapInfo.AllAddress[addressA].AGVPosition, localData.TheMapInfo.AllAddress[addressB].AGVPosition);
            double acLength = computeFunction.GetDistanceFormTwoAGVPosition(localData.TheMapInfo.AllAddress[addressA].AGVPosition, localData.TheMapInfo.AllAddress[addressC].AGVPosition);

            yo.Angle = computeFunction.GetCurrectAngle(slamPosition[addressA].Angle +
                                                       computeFunction.GetCurrectAngle(slamPosition[addressB].Angle - slamPosition[addressA].Angle) *
                                                       (acLength / abLength));

            yo.Position.X = slamPosition[addressA].Position.X +
                            (slamPosition[addressB].Position.X - slamPosition[addressA].Position.X) * (acLength / abLength);

            yo.Position.Y = slamPosition[addressA].Position.Y +
                            (slamPosition[addressB].Position.Y - slamPosition[addressA].Position.Y) * (acLength / abLength);


            changeSlamPosition = true;

            slamPosition[addressC] = yo;
            SetFindSectionData();
            changeSlamPosition = false;
        }

        public void 踹踹看內插法YO(string addressA, string addressB)
        {
            if (localData.TheMapInfo.AllAddress.ContainsKey(addressA) && localData.TheMapInfo.AllAddress.ContainsKey(addressB) &&
                localData.TheMapInfo.AllAddress[addressA].AGVPosition.Angle == localData.TheMapInfo.AllAddress[addressB].AGVPosition.Angle &&
                slamPosition.ContainsKey(addressA) && slamPosition[addressA] != null &&
                slamPosition.ContainsKey(addressB) && slamPosition[addressB] != null)
            {
                foreach (string addressC in localData.TheMapInfo.AllAddress.Keys)
                {
                    if (addressC != addressA && addressC != addressB &&
                        localData.TheMapInfo.AllAddress[addressA].AGVPosition.Angle == localData.TheMapInfo.AllAddress[addressC].AGVPosition.Angle &&
                        AddreesCIsInAddressAB(addressA, addressB, addressC))
                        內插YO(addressA, addressB, addressC);
                }
            }
        }
        #endregion

        #region 把Slam轉換成新的座標(slamOffset).
        protected void SLAMPositionOffset()
        {
            if (originAGVPosition == null)
                return;

            if (slamOffset == null || originAGVPosition.AGVPosition == null)
                offsetAGVPosition = originAGVPosition;
            else
            {
                LocateAGVPosition temp = new LocateAGVPosition(originAGVPosition);
                temp.AGVPosition.Angle = computeFunction.GetCurrectAngle(temp.AGVPosition.Angle + slamOffset.ThetaOffset);
                temp.AGVPosition.Position.X += Math.Cos((temp.AGVPosition.Angle + slamOffset.Polar_Theta) / 180 * Math.PI) * slamOffset.Polar_Distance;
                temp.AGVPosition.Position.Y += Math.Sin((temp.AGVPosition.Angle + slamOffset.Polar_Theta) / 180 * Math.PI) * slamOffset.Polar_Distance;
                offsetAGVPosition = temp;
            }
        }
        #endregion

        #region 把Slam座標轉換為原始座標(slamOffset).
        protected MapAGVPosition GetOrigionPositionBySlamPositionAndOffsetData(MapAGVPosition slamPosition)
        {
            if (slamOffset == null || slamPosition == null)
                return slamPosition;
            else
            {
                MapAGVPosition temp = new MapAGVPosition(slamPosition);
                temp.Position.X -= Math.Cos((temp.Angle + slamOffset.Polar_Theta) / 180 * Math.PI) * slamOffset.Polar_Distance;
                temp.Position.Y -= Math.Sin((temp.Angle + slamOffset.Polar_Theta) / 180 * Math.PI) * slamOffset.Polar_Distance;
                temp.Angle = computeFunction.GetCurrectAngle(temp.Angle - slamOffset.ThetaOffset);
                return temp;
            }
        }
        #endregion

        #region 轉換座標 slam -> cad.
        protected void TransferToMapPosition_BySLAMTransferData(SLAMTransfer transfer, string usingSection)
        {
            if (transfer == null)
                nowAGVPosition = null;
            else
            {
                LocateAGVPosition temp = new LocateAGVPosition(offsetAGVPosition);

                double x = offsetAGVPosition.AGVPosition.Position.X - transfer.Step1Offset.X;
                double y = offsetAGVPosition.AGVPosition.Position.Y - transfer.Step1Offset.Y;

                double newX = x * transfer.CosTheta + y * transfer.SinTheta;

                if (newX < 0)
                    newX = 0;
                else if (newX > transfer.Distance)
                    newX = transfer.Distance;

                // Theta顛倒.
                temp.AGVPosition.Angle = -temp.AGVPosition.Angle;

                temp.AGVPosition.Angle = computeFunction.GetCurrectAngle(temp.AGVPosition.Angle +
                    (transfer.ThetaOffsetStart + computeFunction.GetCurrectAngle(transfer.ThetaOffsetEnd - transfer.ThetaOffsetStart) * newX / transfer.Distance));

                // 上下顛倒.
                y = -y;

                // 旋轉.
                temp.AGVPosition.Position.X = x * transfer.Step2Cos + y * transfer.Step2Sin;
                temp.AGVPosition.Position.Y = -x * transfer.Step2Sin + y * transfer.Step2Cos;

                temp.AGVPosition.Position.X *= transfer.Step3Mag;
                temp.AGVPosition.Position.Y *= transfer.Step3Mag;

                temp.AGVPosition.Position.X += transfer.Step4Offset.X;
                temp.AGVPosition.Position.Y += transfer.Step4Offset.Y;
                temp.Tag = usingSection;
                nowAGVPosition = temp;
            }
        }

        protected void FindManualSection()
        {
            try
            {
                LocateAGVPosition slamNow = offsetAGVPosition;
                string tempManualSectionID = manualSection;

                if (slamNow == null)
                {
                    WriteLog(7, "", String.Concat("目前沒位置, 因此不更新"));
                    return;
                }

                MapPosition now = new MapPosition();

                if (sectionSLAMTransferLit.ContainsKey(tempManualSectionID))
                {
                    now = computeFunction.GetTransferPosition(findSectionList[tempManualSectionID], slamNow.AGVPosition.Position);

                    if (Math.Abs(now.Y) < sectionRange && (0 < now.X && now.X < findSectionList[tempManualSectionID].Distance))
                    {
                        //WriteLog(7, "", String.Concat("目前還在 : ", tempManualSectionID));
                        return;
                    }

                    for (int i = 0; i < sectionConnectSectionList[tempManualSectionID].Count; i++)
                    {
                        now = computeFunction.GetTransferPosition(findSectionList[sectionConnectSectionList[tempManualSectionID][i]], slamNow.AGVPosition.Position);

                        if (Math.Abs(now.Y) < sectionRange &&
                            (0 < now.X && now.X < findSectionList[sectionConnectSectionList[tempManualSectionID][i]].Distance))
                        {
                            manualSection = sectionConnectSectionList[tempManualSectionID][i];
                            //WriteLog(7, "", String.Concat("切換至 : ", sectionConnectSectionList[tempManualSectionID][i]));
                            return;
                        }
                    }
                }

                double min = -1;
                double tempMin;
                double minY = 0;

                foreach (SectionLine sectionLine in findSectionList.Values)
                {
                    now = computeFunction.GetTransferPosition(sectionLine, slamNow.AGVPosition.Position);

                    if (-sectionRange > now.X)
                        tempMin = Math.Abs(now.Y) - now.X;
                    else if (now.X > sectionLine.Distance + sectionRange)
                        tempMin = Math.Abs(now.Y) + (now.X - sectionLine.Distance);
                    else
                        tempMin = Math.Abs(now.Y);

                    if (min == -1 || tempMin < min)
                    {
                        tempManualSectionID = sectionLine.Section.Id;
                        min = tempMin;
                        minY = now.Y;
                    }
                }

                if (min != -1)
                {
                    manualSection = tempManualSectionID;
                    //WriteLog(7, "", String.Concat("切換至 : ", tempManualSectionID, " deltaY : ", minY.ToString("0.0")));
                }
                else
                    WriteLog(7, "", "Not Find in Section (min == -1)");
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        protected void TransferToMapPosition(double sectionRange)
        {
            this.sectionRange = sectionRange;

            VehicleLocation nowLocate = localData.Location;

            string nowSection = (nowLocate != null ? nowLocate.NowSection : "");

            if (IsInSection(nowSection))
            {
                manualSection = nowSection;
                TransferToMapPosition_BySLAMTransferData(sectionSLAMTransferLit[nowSection], nowSection);
            }
            else
            {
                string tempManualSectionID = manualSection;

                if ((DateTime.Now - lastSearchSectionTime).TotalMilliseconds > localData.MoveControlData.MoveControlConfig.TimeValueConfig.IntervalTimeList[EnumIntervalTimeType.ManualFindSectionInterval])
                {
                    if (searchSectionThread == null || !searchSectionThread.IsAlive)
                    {
                        lastSearchSectionTime = DateTime.Now;
                        searchSectionThread = new Thread(FindManualSection);
                        searchSectionThread.Start();
                    }
                }

                if (sectionSLAMTransferLit.ContainsKey(tempManualSectionID))
                    TransferToMapPosition_BySLAMTransferData(sectionSLAMTransferLit[tempManualSectionID], tempManualSectionID);
                else
                    TransferToMapPosition_BySLAMTransferData(null, "");
            }
        }
        #endregion
    }
}
