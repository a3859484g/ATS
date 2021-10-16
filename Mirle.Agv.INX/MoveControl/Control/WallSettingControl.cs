using Mirle.Agv.INX.Controller.Tools;
using Mirle.Agv.INX.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Mirle.Agv.INX.Controller
{
    /// <summary>
    ///  simulate stop distance >> using Move.Dec, Move.Jerk
    /// </summary>

    public class WallSettingControl
    {
        private LocalData localData = LocalData.Instance;
        private ComputeFunction computeFunction = ComputeFunction.Instance;

        public Dictionary<string, Wall> AllWall { get; set; } = new Dictionary<string, Wall>();

        private AlarmHandler alarmHandler;
        private LoggerAgent loggerAgent = LoggerAgent.Instance;
        private string device = MethodInfo.GetCurrentMethod().ReflectedType.Name;

        private string normalLogName;

        private Thread wallThread;
        public WallSettingConfig Config { get; set; }

        private Dictionary<string, uint> inWallData = new Dictionary<string, uint>();
        private uint lastDisable = 0;

        private bool close = false;

        private bool wallDataChangeAction = false;
        private bool waitAction = false;

        private string csvPath = "";
        private string cavPath_Backup = "";

        public WallSettingControl(AlarmHandler alarmHandler, string normalLogName)
        {
            this.alarmHandler = alarmHandler;
            this.normalLogName = normalLogName;

            if (ReadXML())
            {
                wallThread = new Thread(WallThread);
                wallThread.Start();
            }
            else
                WriteLog(3, "", "ReadXMLFail, 因此不開Thread!");
        }

        public void Close()
        {
            close = true;
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

        private bool ReadWallCSV(string path, ref string errorMessage)
        {
            /// ID,StartX,StartY,EndX,EndY,Distance.
            try
            {
                if (!File.Exists(path))
                {
                    errorMessage = String.Concat("找不到", path);
                    return false;
                }

                string[] allRows = File.ReadAllLines(path);

                if (allRows.Length < 1)
                {
                    errorMessage = "檔案空白,無Title";
                    return false;
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

                if (dicHeaderIndexes.ContainsKey("ID") &&
                    dicHeaderIndexes.ContainsKey("Start_X") &&
                    dicHeaderIndexes.ContainsKey("Start_Y") &&
                    dicHeaderIndexes.ContainsKey("End_X") &&
                    dicHeaderIndexes.ContainsKey("End_Y") &&
                    dicHeaderIndexes.ContainsKey("Distance"))
                {
                }
                else
                {
                    errorMessage = "Title錯誤";
                    return false;
                }

                string id;
                MapPosition start;
                MapPosition end;
                double distance;
                double angle;
                Wall wall;

                for (int i = 0; i < nRows; i++)
                {
                    string[] getThisRow = allRows[i].Split(',');
                    start = new MapPosition();
                    end = new MapPosition();

                    id = getThisRow[dicHeaderIndexes["ID"]];
                    start.X = double.Parse(getThisRow[dicHeaderIndexes["Start_X"]]);
                    start.Y = double.Parse(getThisRow[dicHeaderIndexes["Start_Y"]]);

                    end.X = double.Parse(getThisRow[dicHeaderIndexes["End_X"]]);
                    end.Y = double.Parse(getThisRow[dicHeaderIndexes["End_Y"]]);

                    distance = double.Parse(getThisRow[dicHeaderIndexes["Distance"]]);
                    angle = computeFunction.ComputeAngle(start, end);

                    wall = new Wall(id, start, end, angle, distance, Config.SleepTime);

                    if (AllWall.ContainsKey(wall.ID))
                        WriteLog(5, "", String.Concat("line ", (i + 2).ToString(), " ID 重複!"));
                    else
                        AllWall.Add(wall.ID, wall);
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = String.Concat("Exception : ", ex.ToString());
                return false;
            }
        }

        private void WriteCSV(string backupPath)
        {
            try
            {
                List<string> writeData = new List<string>();
                writeData.Add("ID,Start_X,Start_Y,End_X,End_Y,Distance");

                foreach (Wall wall in AllWall.Values)
                {
                    writeData.Add(String.Concat(wall.ID, ",", wall.Start.X.ToString("0"), ",", wall.Start.Y.ToString("0"), ",",
                                                wall.End.X.ToString("0"), ",", wall.End.Y.ToString("0"), ",", wall.Distance.ToString("0")));
                }

                using (StreamWriter outputFile = new StreamWriter(backupPath))
                {
                    for (int i = 0; i < writeData.Count; i++)
                        outputFile.WriteLine(writeData[i]);
                }
            }
            catch (Exception ex)
            {
                WriteLog(5, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        public void WriteCSV()
        {
            WriteCSV(csvPath);
        }

        private bool ReadXML()
        {
            string path = @"D:\MecanumConfigs\MoveControl\WallSettingConfig.xml";

            csvPath = Path.Combine(localData.MapConfig.FileDirectory, "Wall.csv");
            cavPath_Backup = Path.Combine(localData.MapConfig.FileDirectory, "Wall_backup.csv");
            string errorMessage = "";

            Config = new WallSettingConfig();

            try
            {
                XmlDocument doc = new XmlDocument();

                if (!File.Exists(path))
                {
                    WriteLog(1, "", String.Concat("找不到", path));
                }
                else
                {
                    doc.Load(path);
                    XmlElement rootNode = doc.DocumentElement;

                    foreach (XmlNode item in rootNode.ChildNodes)
                    {
                        switch (item.Name)
                        {
                            case "MaxInterval":
                                Config.MaxInterval = double.Parse(item.InnerText);
                                break;
                            case "MinInterval":
                                Config.MinInterval = double.Parse(item.InnerText);
                                break;
                            case "InWallInterval":
                                Config.InWallInterval = double.Parse(item.InnerText);
                                break;
                            case "SleepTime":
                                Config.SleepTime = Int32.Parse(item.InnerText);
                                break;
                            case "MapScale":
                                Config.MapScale = double.Parse(item.InnerText);
                                break;
                            case "MapBorderLength":
                                Config.MapBorderLength = double.Parse(item.InnerText);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }

            if (ReadWallCSV(csvPath, ref errorMessage))
            {
                WriteCSV(cavPath_Backup);
            }
            else
            {
                WriteLog(5, "", String.Concat("牆壁檔 - ", csvPath, " - 讀取失敗, errorMessge : ", errorMessage));

                bool result = ReadWallCSV(cavPath_Backup, ref errorMessage);

                WriteLog(5, "", String.Concat("嘗試讀取backup 檔案 - ", cavPath_Backup, " - ", (result ? "成功" : String.Concat("失敗, errorMessage : ", errorMessage))));
            }

            return true;
        }

        private void ReflashDisableData()
        {
            uint newUint = 0;

            foreach (uint temp in inWallData.Values)
            {
                newUint = newUint | temp;
            }

            if (newUint != lastDisable)
            {
                if ((lastDisable >> 3) != (newUint >> 3))
                {
                    WriteLog(7, "", String.Concat("Wall Disable Change(fornt) : ", (newUint >> 3) == 1 ? "disable" : "enable"));

                    localData.MIPCData.BypassFront = ((newUint >> 3) == 1);
                }

                if (((lastDisable >> 2) & 1) != ((newUint >> 2) & 1))
                {
                    WriteLog(7, "", String.Concat("Wall Disable Change(back) : ", ((newUint >> 2) & 1) == 1 ? "disable" : "enable"));
                    localData.MIPCData.BypassBack = (((newUint >> 2) & 1) == 1);
                }

                if (((lastDisable >> 1) & 1) != ((newUint >> 1) & 1))
                {
                    WriteLog(7, "", String.Concat("Wall Disable Change(left) : ", ((newUint >> 1) & 1) == 1 ? "disable" : "enable"));
                    localData.MIPCData.BypassLeft = (((newUint >> 1) & 1) == 1);
                }

                if ((lastDisable & 1) != (newUint & 1))
                {
                    WriteLog(7, "", String.Concat("Wall Disable Change(right) : ", ((newUint & 1) == 1 ? "disable" : "enable")));
                    localData.MIPCData.BypassRight = ((newUint & 1) == 1);
                }


                lastDisable = newUint;
            }
        }

        private void CheckWall(Wall wall, MapAGVPosition real)
        {
            double x = real.Position.X - wall.Start.X;
            double y = real.Position.Y - wall.Start.Y;

            double newX = x * wall.CosTheta + y * wall.SinTheta;
            double newY = -x * wall.SinTheta + y * wall.CosTheta;

            if (Math.Abs(newY) <= wall.Distance && newX >= 0 && newX <= wall.WallLength)
            {
                uint disableUint = 0;
                // 在牆壁內
                //double theta = computeFunction.GetCurrectAngle(real.Angle - wall.Theta);

                #region 方法2.
                MapPosition agvPositionOffset;
                double offsetY;
                double mathAngle = real.Angle * Math.PI / 180;

                agvPositionOffset = new MapPosition(real.Position.X + Math.Cos(mathAngle) * wall.Distance, real.Position.Y + Math.Sin(mathAngle) * wall.Distance);

                offsetY = -(agvPositionOffset.X - wall.Start.X) * wall.SinTheta + (agvPositionOffset.Y - wall.Start.Y) * wall.CosTheta;

                if (offsetY * newY < 0)
                    disableUint += 1;

                disableUint = disableUint << 1;

                agvPositionOffset = new MapPosition(real.Position.X + Math.Cos(mathAngle + Math.PI) * wall.Distance, real.Position.Y + Math.Sin(mathAngle + Math.PI) * wall.Distance);

                offsetY = -(agvPositionOffset.X - wall.Start.X) * wall.SinTheta + (agvPositionOffset.Y - wall.Start.Y) * wall.CosTheta;

                if (offsetY * newY < 0)
                    disableUint += 1;

                disableUint = disableUint << 1;

                agvPositionOffset = new MapPosition(real.Position.X + Math.Cos(mathAngle - Math.PI / 2) * wall.Distance, real.Position.Y + Math.Sin(mathAngle - Math.PI / 2) * wall.Distance);

                offsetY = -(agvPositionOffset.X - wall.Start.X) * wall.SinTheta + (agvPositionOffset.Y - wall.Start.Y) * wall.CosTheta;

                if (offsetY * newY < 0)
                    disableUint += 1;

                disableUint = disableUint << 1;

                agvPositionOffset = new MapPosition(real.Position.X + Math.Cos(mathAngle + Math.PI / 2) * wall.Distance, real.Position.Y + Math.Sin(mathAngle + Math.PI / 2) * wall.Distance);

                offsetY = -(agvPositionOffset.X - wall.Start.X) * wall.SinTheta + (agvPositionOffset.Y - wall.Start.Y) * wall.CosTheta;

                if (offsetY * newY < 0)
                    disableUint += 1;
                #endregion

                if (inWallData.ContainsKey(wall.ID))
                {
                    if (inWallData[wall.ID] != disableUint)
                    {
                        inWallData[wall.ID] = disableUint;
                        ReflashDisableData();
                    }
                }
                else
                {
                    if (wall.ID == "wall-0")
                        WriteLog(3, "", String.Concat("wall-0 in : ", disableUint.ToString()));

                    WriteLog(7, "", String.Concat("Wall in : ", wall.ID));
                    inWallData[wall.ID] = disableUint;
                    ReflashDisableData();
                }

                wall.TimeInterval = Config.InWallInterval;
            }
            else
            {
                if (inWallData.ContainsKey(wall.ID))
                {
                    WriteLog(7, "", String.Concat("Wall out : ", wall.ID));
                    inWallData.Remove(wall.ID);
                    ReflashDisableData();
                }

                double deltaY = Math.Abs(newY) - wall.Distance;

                if (deltaY < 0)
                    deltaY = 0;

                double deltaX = 0;

                if (newX < 0)
                    deltaX = -newX;
                else if (newX > wall.WallLength)
                    deltaX = newX - wall.WallLength;

                double time = Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2)) / localData.MoveControlData.CreateMoveCommandConfig.Move.Velocity * 1000;

                if (time < Config.MinInterval)
                    time = Config.MinInterval;
                else if (time > Config.MaxInterval)
                    time = Config.MaxInterval;

                wall.TimeInterval = time;
            }

            wall.Timer.Restart();
        }

        private void WallThread()
        {
            try
            {
                Stopwatch timer = new Stopwatch();
                MapAGVPosition now;

                while (!close)
                {
                    if (wallDataChangeAction)
                    {
                        waitAction = true;
                    }
                    else
                    {
                        now = localData.Real;

                        if (now != null)
                        {
                            foreach (Wall wall in AllWall.Values)
                            {
                                if (wall.Timer.ElapsedMilliseconds > wall.TimeInterval)
                                    CheckWall(wall, now);

                                Thread.Sleep(Config.SleepTimeMin);
                            }
                        }
                    }

                    timer.Restart();

                    while (timer.ElapsedMilliseconds < Config.SleepTime)
                        Thread.Sleep(Config.SleepTimeMin);
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                localData.MIPCData.BypassFront = false;
                localData.MIPCData.BypassBack = false;
                localData.MIPCData.BypassLeft = false;
                localData.MIPCData.BypassRight = false;
            }
        }

        public void AddNewWall(Wall wall)
        {
            waitAction = false;
            wallDataChangeAction = true;

            while (!waitAction)
                Thread.Sleep(Config.SleepTimeMin);

            AllWall.Add(wall.ID, wall);
            wallDataChangeAction = false;
        }

        public void ChangeWallData(Wall wall)
        {
            waitAction = false;
            wallDataChangeAction = true;

            while (!waitAction)
                Thread.Sleep(5);

            AllWall[wall.ID] = wall;
            wallDataChangeAction = false;
        }

        public void DeleteThisWall(string wallID)
        {
            waitAction = false;
            wallDataChangeAction = true;

            while (!waitAction)
                Thread.Sleep(5);

            AllWall.Remove(wallID);

            if (inWallData.ContainsKey(wallID))
            {
                inWallData.Remove(wallID);
                ReflashDisableData();
            }

            wallDataChangeAction = false;
        }
    }
}
