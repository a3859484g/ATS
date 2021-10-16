using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Mirle.Agv.INX.Model;
using Mirle.Agv.INX.Model.Configs;
using Mirle.Agv.INX.Controller.Tools;
using System.Collections.Concurrent;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Globalization;

namespace Mirle.Agv.INX.Controller
{
    [Serializable]
    public class AlarmHandler
    {
        private LocalData localData = LocalData.Instance;
        public Dictionary<int, Alarm> AlarmCodeTable { get; set; } = new Dictionary<int, Alarm>();

        private Dictionary<int, Alarm> nowAlarm = new Dictionary<int, Alarm>();
        private Dictionary<int, Alarm> nowWarning = new Dictionary<int, Alarm>();

        private List<Alarm> nowAlarmAndWarning = new List<Alarm>();
        private List<Alarm> alarmAndWanringHistory = new List<Alarm>();

        private object lockObject = new object();
        private object lockObject_Middler = new object();

        private bool logChange = false;

        private string alarmFileName = "AlarmCode.csv";
        private LoggerAgent loggerAgent = LoggerAgent.Instance;

        private string alarmHistoryLogName = "AlarmHistory";
        private string device = MethodInfo.GetCurrentMethod().ReflectedType.Name;

        public string NowAlarm { get; set; } = "";
        public string AlarmHistory { get; set; } = "";
        private int maxAlarmHistoryCount = 30;

        public event EventHandler<int> SendMiddlerAlarm;

        private Thread updateLogMessage = null;

        private bool checkTime = true;
        private Stopwatch checkTimeTimer = new Stopwatch();

        private string alarmFileID = "Id";
        private string alarmFileAlarmText = "AlarmText";
        private string alarmFileLevel = "Level";
        private string alarmFileDescription = "Description";

        private Dictionary<EnumLanguage, Dictionary<int, string>> languageAndAlarmIDToString = new Dictionary<EnumLanguage, Dictionary<int, string>>();
        private List<EnumLanguage> languageList = new List<EnumLanguage>();

        public AlarmHandler()
        {
            LoadAlarmFile();
            AddAlarmCodeByEnumTag();
            WriteAlarmCodeByTable();
            SetAlarmLog(String.Concat("───── IPC 程式啟動, LocalVersion = ", localData.LocalVersion.ToString("0.0"), ", AGV Type = ", localData.MainFlowConfig.AGVType.ToString(), " ─────"));
            GetTodayAlarmHistory(); //Fetch current date alarmhistory log and extract data with UI(tbx_Alarm) format ,finally add alarmAndWanringHistory list.
            checkTimeTimer.Restart();
            updateLogMessage = new Thread(UpdateLogMessage);
            updateLogMessage.Start();
        }

        public void CloseAlarmHandler()
        {
            close = true;
        }

        private string GetAlarmStringByAlarmCode(int alarmID)
        {
            if (languageAndAlarmIDToString.ContainsKey(localData.Language) && languageAndAlarmIDToString[localData.Language].ContainsKey(alarmID))
                return languageAndAlarmIDToString[localData.Language][alarmID];
            else if (AlarmCodeTable.ContainsKey(alarmID))
                return AlarmCodeTable[alarmID].AlarmText;
            else
                return String.Concat("Know AlarmCode ID = ", alarmID.ToString());
        }

        private bool close = false;

        private EnumLanguage lastLanguage = EnumLanguage.None;

        private void UpdateLogMessage()
        {
            Stopwatch timer = new Stopwatch();

            while (!close)
            {
                if (checkTime)
                {
                    if (checkTimeTimer.ElapsedMilliseconds > 3 * 60 * 1000 || localData.AutoManual == EnumAutoState.Auto)
                    {
                        checkTimeTimer.Stop();
                        checkTime = false;
                    }
                }

                if (logChange || lastLanguage != localData.Language)
                {
                    lastLanguage = localData.Language;
                    string nowLogMessage = "";
                    string historyLogMessage = "";

                    lock (lockObject)
                    {
                        for (int i = 0; i < nowAlarmAndWarning.Count; i++)
                        {
                            nowLogMessage = String.Concat(nowAlarmAndWarning[i].SetTime.ToString("MM/dd HH:mm:ss"), " [",
                                                          nowAlarmAndWarning[i].Level.ToString(), "][",
                                                          nowAlarmAndWarning[i].Id.ToString("0"), "] ",
                                                          GetAlarmStringByAlarmCode(nowAlarmAndWarning[i].Id), "\r\n", nowLogMessage);
                        }

                        for (int i = 0; i < alarmAndWanringHistory.Count; i++)
                        {
                            historyLogMessage = String.Concat(alarmAndWanringHistory[i].SetTime.ToString("MM/dd HH:mm:ss"), " [",
                                                              alarmAndWanringHistory[i].Level.ToString(), "][",
                                                              alarmAndWanringHistory[i].Id.ToString("0"), "] ",
                                                              GetAlarmStringByAlarmCode(alarmAndWanringHistory[i].Id), "\r\n", historyLogMessage);
                        }

                        logChange = false;
                    }

                    NowAlarm = nowLogMessage;
                    AlarmHistory = historyLogMessage;
                }

                timer.Restart();

                while (timer.ElapsedMilliseconds < 500)
                    Thread.Sleep(1);
            }

            SetAlarmLog("───── IPC 程式關閉 ─────");
            Thread.Sleep(1000);
        }

        private void SetAlarmLog(string message)
        {
            loggerAgent.LogString(alarmHistoryLogName, message);
        }

        public void WriteLog(int logLevel, string carrierId, string message, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            LogFormat logFormat = new LogFormat(alarmHistoryLogName, logLevel.ToString(), memberName, device, carrierId, message);
            loggerAgent.Log(logFormat.Category, logFormat);
        }

        private void WriteSetAlarmCodeLog(Alarm alarm)
        {
            VehicleLocation now = localData.Location;
            string nowAddressID = (now != null && localData.TheMapInfo.AllAddress.ContainsKey(now.LastAddress) ? now.LastAddress : "Know");

            SetAlarmLog(String.Concat(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                                      " [", alarm.Level.ToString(), "]",
                                      "[SetAlarm]",
                                      "[", alarm.Id.ToString("0"), "]",
                                      "[", localData.AutoManual.ToString(), "]",
                                      alarm.AlarmText,
                                      " { ", nowAddressID, ", ", ComputeFunction.Instance.GetMapAGVPositionStringWithAngle(localData.Real, "0"), " }"));
        }

        private void WriteResetAlarmCodeLog(Alarm alarm)
        {
            VehicleLocation now = localData.Location;
            string nowAddressID = (now != null && localData.TheMapInfo.AllAddress.ContainsKey(now.LastAddress) ? now.LastAddress : "Know");

            SetAlarmLog(String.Concat(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                                   " [", alarm.Level.ToString(), "]",
                                   "[ResetAlarm]",
                                   "[", alarm.Id.ToString("0"), "]",
                                   "[", localData.AutoManual.ToString(), "]",
                                   alarm.AlarmText,
                                   " { ", nowAddressID, ", ", ComputeFunction.Instance.GetMapAGVPositionStringWithAngle(localData.Real, "0"), " }"));
        }

        #region Add AlarmCode By Enum.
        private void AddAlarmCodeByEnumTag()
        {
            Alarm newAlarm;

            #region MoveControl.
            foreach (EnumMoveCommandControlErrorCode item in (EnumMoveCommandControlErrorCode[])Enum.GetValues(typeof(EnumMoveCommandControlErrorCode)))
            {
                if ((int)item != 0)
                {
                    if (!AlarmCodeTable.ContainsKey((int)item))
                    {
                        newAlarm = new Alarm();

                        if (item == EnumMoveCommandControlErrorCode.End_SecondCorrectionTimeout ||
                            item == EnumMoveCommandControlErrorCode.End_ServoOffTimeout ||
                            item == EnumMoveCommandControlErrorCode.LocateDriver_BarcodeMapSystemTriggerException ||
                            item == EnumMoveCommandControlErrorCode.LocateDriver_BarcodeMapSystem回傳資料格式錯誤 ||
                            item == EnumMoveCommandControlErrorCode.LocateDriver_SLAM_精度迷航 ||
                            item == EnumMoveCommandControlErrorCode.LocateDriver_SLAM_資料格式錯誤 ||
                            item == EnumMoveCommandControlErrorCode.命令分解失敗 ||
                            item == EnumMoveCommandControlErrorCode.拒絕移動命令_充電中 ||
                            item == EnumMoveCommandControlErrorCode.拒絕移動命令_資料格式錯誤 ||
                            item == EnumMoveCommandControlErrorCode.拒絕移動命令_移動命令中 ||
                            item == EnumMoveCommandControlErrorCode.拒絕移動命令_MoveControlNotReady)
                            newAlarm.Level = EnumAlarmLevel.Warn;
                        else
                            newAlarm.Level = EnumAlarmLevel.Alarm;

                        newAlarm.Id = (int)item;
                        newAlarm.AlarmText = item.ToString();
                        newAlarm.Description = item.ToString();

                        AlarmCodeTable.Add(newAlarm.Id, newAlarm);
                    }
                }
            }
            #endregion

            #region MIPC.
            foreach (EnumMIPCControlErrorCode item in (EnumMIPCControlErrorCode[])Enum.GetValues(typeof(EnumMIPCControlErrorCode)))
            {
                if ((int)item != 0)
                {
                    if (!AlarmCodeTable.ContainsKey((int)item))
                    {
                        newAlarm = new Alarm();

                        if (item == EnumMIPCControlErrorCode.BatteryWarningTemp ||
                            item == EnumMIPCControlErrorCode.ByPass聲音燈號IO自動傳送 ||
                            item == EnumMIPCControlErrorCode.LowBattery_SOC ||
                            item == EnumMIPCControlErrorCode.LowBattery_V ||
                            item == EnumMIPCControlErrorCode.MIPC回傳資料異常 ||
                            item == EnumMIPCControlErrorCode.MIPC通訊異常 ||
                            item == EnumMIPCControlErrorCode.SensorSafety_AlarmByPass ||
                            item == EnumMIPCControlErrorCode.SensorSafety_SafetyByPass ||
                            item == EnumMIPCControlErrorCode.解剎車中 ||
                            item == EnumMIPCControlErrorCode.SensorSafety_停止訊號Timeout)
                            newAlarm.Level = EnumAlarmLevel.Warn;
                        else
                            newAlarm.Level = EnumAlarmLevel.Alarm;

                        newAlarm.Id = (int)item;
                        newAlarm.AlarmText = item.ToString();
                        newAlarm.Description = item.ToString();

                        AlarmCodeTable.Add(newAlarm.Id, newAlarm);
                    }
                }
            }
            #endregion

            #region LoadUnload.
            foreach (EnumLoadUnloadControlErrorCode item in (EnumLoadUnloadControlErrorCode[])Enum.GetValues(typeof(EnumLoadUnloadControlErrorCode)))
            {
                if ((int)item != 0)
                {
                    if (!AlarmCodeTable.ContainsKey((int)item))
                    {
                        newAlarm = new Alarm();

                        if (item == EnumLoadUnloadControlErrorCode.AlignmentNG ||
                            item == EnumLoadUnloadControlErrorCode.AlignmentValueNG ||
                            item == EnumLoadUnloadControlErrorCode.ChargerStationFullCharging ||
                            item == EnumLoadUnloadControlErrorCode.ChargerStationAlarm ||
                            item == EnumLoadUnloadControlErrorCode.ChargerStationNotService ||
                            item == EnumLoadUnloadControlErrorCode.ChargerStationWaitChargingTimeout ||
                            item == EnumLoadUnloadControlErrorCode.ChargerStationWaitOPTimeout ||
                            item == EnumLoadUnloadControlErrorCode.ChargerStationWarning ||
                            item == EnumLoadUnloadControlErrorCode.ChargingConfrimSensorNotOn ||
                            item == EnumLoadUnloadControlErrorCode.ChargingTimeout ||
                            item == EnumLoadUnloadControlErrorCode.ChargingWith_OPOff ||
                            item == EnumLoadUnloadControlErrorCode.CVSensor異常 ||
                            item == EnumLoadUnloadControlErrorCode.T0_Timeout ||
                            item == EnumLoadUnloadControlErrorCode.T1_Timeout ||
                            item == EnumLoadUnloadControlErrorCode.T2_Timeout ||
                            item == EnumLoadUnloadControlErrorCode.T3_Timeout ||
                            item == EnumLoadUnloadControlErrorCode.T5_Timeout ||
                            item == EnumLoadUnloadControlErrorCode.T6_Timeout ||
                            item == EnumLoadUnloadControlErrorCode.取放命令與EQRequest不相符 ||
                            item == EnumLoadUnloadControlErrorCode.取放貨中CV檢知異常 ||
                            item == EnumLoadUnloadControlErrorCode.取放貨中EQPIOOff ||
                            item == EnumLoadUnloadControlErrorCode.取貨_LoadingOn異常 ||
                            item == EnumLoadUnloadControlErrorCode.啟用PIOTimeout測試 ||
                            item == EnumLoadUnloadControlErrorCode.回Home失敗_CST回CV_Timeout ||
                            item == EnumLoadUnloadControlErrorCode.回Home失敗_CVSensor狀態異常 ||
                            item == EnumLoadUnloadControlErrorCode.回Home失敗_Exception ||
                            item == EnumLoadUnloadControlErrorCode.回Home失敗_HomeSensor未on ||
                            item == EnumLoadUnloadControlErrorCode.回Home失敗_極限Sensor未On ||
                            item == EnumLoadUnloadControlErrorCode.回Home失敗_MIPC指令失敗 ||
                            item == EnumLoadUnloadControlErrorCode.回Home失敗_MovingTimeout ||
                            item == EnumLoadUnloadControlErrorCode.回Home失敗_P軸不在Home ||
                            item == EnumLoadUnloadControlErrorCode.回Home失敗_ServoOn_Timeout ||
                            item == EnumLoadUnloadControlErrorCode.回Home失敗_Theta軸不再Home ||
                            item == EnumLoadUnloadControlErrorCode.回Home失敗_Z軸不在上定位 ||
                            item == EnumLoadUnloadControlErrorCode.回Home失敗_人員觸發停止 ||
                            item == EnumLoadUnloadControlErrorCode.放貨_LoadingOff異常 ||
                            item == EnumLoadUnloadControlErrorCode.TA1_Timeout ||
                            item == EnumLoadUnloadControlErrorCode.TA2_Timeout ||
                            item == EnumLoadUnloadControlErrorCode.TA3_Timeout ||
                            item == EnumLoadUnloadControlErrorCode.TP1_Timeout ||
                            item == EnumLoadUnloadControlErrorCode.TP2_Timeout ||
                            item == EnumLoadUnloadControlErrorCode.TP5_Timeout ||
                            item == EnumLoadUnloadControlErrorCode.Port站ES或HO_AVBLNotOn)
                            newAlarm.Level = EnumAlarmLevel.Warn;
                        else
                            newAlarm.Level = EnumAlarmLevel.Alarm;

                        newAlarm.Id = (int)item;
                        newAlarm.AlarmText = item.ToString();
                        newAlarm.Description = item.ToString();

                        AlarmCodeTable.Add(newAlarm.Id, newAlarm);
                    }
                }
            }
            #endregion

            #region Middler.
            foreach (MiddlePackage.Umtc.EnumMiddlerAlarmCode item in (MiddlePackage.Umtc.EnumMiddlerAlarmCode[])Enum.GetValues(typeof(MiddlePackage.Umtc.EnumMiddlerAlarmCode)))
            {
                if ((int)item != 0)
                {
                    if (!AlarmCodeTable.ContainsKey((int)item))
                    {
                        newAlarm = new Alarm();
                        newAlarm.Level = EnumAlarmLevel.Alarm;
                        newAlarm.Id = (int)item;
                        newAlarm.AlarmText = item.ToString();
                        newAlarm.Description = item.ToString();
                        AlarmCodeTable.Add(newAlarm.Id, newAlarm);
                    }
                }
            }
            #endregion
        }

        private void WriteAlarmCodeByTable()
        {
            List<string> output = new List<string>();
            string tempString;
            tempString = String.Concat(alarmFileID, ",", alarmFileAlarmText, ",", alarmFileLevel, ",", alarmFileDescription);

            for (int i = 0; i < languageList.Count; i++)
                tempString = String.Concat(tempString, ",", languageList[i].ToString());

            output.Add(tempString);

            try
            {

                foreach (Alarm alarm in AlarmCodeTable.Values)
                {
                    tempString = String.Concat(alarm.Id.ToString(), ",", alarm.AlarmText, ",", alarm.Level.ToString(), ",", alarm.Description);

                    for (int i = 0; i < languageList.Count; i++)
                    {
                        if (languageAndAlarmIDToString.ContainsKey(languageList[i]) &&
                            languageAndAlarmIDToString[languageList[i]].ContainsKey(alarm.Id))
                            tempString = String.Concat(tempString, ",", languageAndAlarmIDToString[languageList[i]][alarm.Id]);
                        else
                            tempString = String.Concat(tempString, ",", "Not Define");
                    }

                    output.Add(tempString);
                }

                using (StreamWriter outputFile = new StreamWriter(@"D:\AlarmCode.csv"))
                {
                    for (int i = 0; i < output.Count; i++)
                        outputFile.WriteLine(output[i]);
                }
            }
            catch
            {
            }
        }
        #endregion

        #region Read AlarmCode.csv.
        private void LoadAlarmFile()
        {
            try
            {
                string alarmFullPath = Path.Combine(Environment.CurrentDirectory, alarmFileName);

                if (!File.Exists(alarmFullPath))
                {
                    WriteLog(3, "", String.Concat("找不到AlarmCode.csv, path : ", alarmFullPath));
                    return;
                }

                Dictionary<string, int> dicAlarmIndexes = new Dictionary<string, int>();
                AlarmCodeTable.Clear();

                string[] allRows = File.ReadAllLines(alarmFullPath, Encoding.UTF8);

                if (allRows == null || allRows.Length < 2)
                {
                    WriteLog(3, "", "There are no alarms in file");
                    return;
                }

                string[] titleRow = allRows[0].Split(',');
                allRows = allRows.Skip(1).ToArray();

                int nRows = allRows.Length;
                int nColumns = titleRow.Length;

                //Id, AlarmText, PlcAddress, PlcBitNumber, Level, Description
                for (int i = 0; i < nColumns; i++)
                {
                    var keyword = titleRow[i].Trim();

                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        if (!dicAlarmIndexes.ContainsKey(keyword))
                            dicAlarmIndexes.Add(keyword, i);
                    }
                }
                EnumLanguage tempLanguage;

                foreach (string key in dicAlarmIndexes.Keys)
                {
                    if (Enum.TryParse(key, out tempLanguage))
                    {
                        if (!languageAndAlarmIDToString.ContainsKey(tempLanguage))
                        {
                            languageAndAlarmIDToString.Add(tempLanguage, new Dictionary<int, string>());
                            languageList.Add(tempLanguage);
                        }
                    }
                }

                for (int i = 0; i < nRows; i++)
                {
                    string[] getThisRow = LoadAlarmFile_SplitCsvLine(allRows[i]);
                    Alarm oneRow = new Alarm();
                    oneRow.Id = int.Parse(getThisRow[dicAlarmIndexes[alarmFileID]]);
                    oneRow.AlarmText = getThisRow[dicAlarmIndexes[alarmFileAlarmText]];
                    oneRow.Level = EnumAlarmLevelParse(getThisRow[dicAlarmIndexes[alarmFileLevel]]);
                    oneRow.Description = getThisRow[dicAlarmIndexes[alarmFileDescription]];

                    for (int j = 0; j < languageList.Count; j++)
                    {
                        if (dicAlarmIndexes[languageList[j].ToString()] < getThisRow.Length)
                            languageAndAlarmIDToString[languageList[j]].Add(oneRow.Id, getThisRow[dicAlarmIndexes[languageList[j].ToString()]]);
                    }

                    if (AlarmCodeTable.ContainsKey(oneRow.Id))
                        WriteLog(3, "", String.Concat("Alarm code : ", oneRow.Id.ToString(), "repeat"));
                    else
                    {
                        AlarmCodeTable.Add(oneRow.Id, oneRow);
                        localData.AllAlarmBit.Add(oneRow.Id, new AlarmCodeAndSetOrReset());
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                WriteLog(3, "", String.Concat("Exception : ", ex.StackTrace));
            }
        }

        private string[] LoadAlarmFile_SplitCsvLine(string strLine)
        {
            string[] result = new string[6];

            if (strLine.Contains('"'))
            {
                string[] temp = strLine.Split(',');
                int resultIndex = 0;
                bool quoteFlag = false;
                for (int i = 0; i < temp.Length; i++)
                {
                    if (temp[i].Contains('"') && quoteFlag == false)
                    {
                        quoteFlag = true;
                        result[resultIndex] = temp[i].Substring(1);
                    }
                    else if (temp[i].Contains('"') && quoteFlag == true)
                    {
                        result[resultIndex] = result[resultIndex] + "," + temp[i].Substring(0, temp[i].Length - 2);
                        quoteFlag = false;
                        resultIndex++;
                    }
                    else if (!temp[i].Contains('"') && quoteFlag == false)
                    {
                        result[resultIndex] = temp[i];
                        resultIndex++;
                    }
                    else if (!temp[i].Contains('"') && quoteFlag == true)
                    {
                        result[resultIndex] = result[resultIndex] + "," + temp[i];
                    }
                }
            }
            else
            {
                result = strLine.Split(',');
            }

            return result;

        }

        private EnumAlarmLevel EnumAlarmLevelParse(string v)
        {
            try
            {
                v = v.Trim();

                return (EnumAlarmLevel)Enum.Parse(typeof(EnumAlarmLevel), v);
            }
            catch (Exception ex)
            {
                loggerAgent.Log("Error", new LogFormat("Error", "5", GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, "Device", "CarrierID"
                  , ex.StackTrace));
                return EnumAlarmLevel.Warn;
            }
        }
        #endregion

        public void SetAlarmCode(int alarmCodeID, bool localAlarm = true)
        {
            try
            {
                DateTime timeStamp = DateTime.Now;

                Alarm alarm = new Alarm();
                alarm.Id = alarmCodeID;

                if (AlarmCodeTable.ContainsKey(alarmCodeID))
                {
                    alarm.AlarmText = AlarmCodeTable[alarmCodeID].AlarmText;
                    alarm.Level = AlarmCodeTable[alarmCodeID].Level;
                    alarm.Description = AlarmCodeTable[alarmCodeID].Description;
                }

                //Alarm alarm = AlarmCodeTable.ContainsKey(alarmCodeID) ? AlarmCodeTable[alarmCodeID] : new Alarm { Id = alarmCodeID };
                alarm.SetTime = timeStamp;

                bool sendMiddlerAlarm = false;

                lock (lockObject)
                {
                    if (!nowAlarm.ContainsKey(alarmCodeID) &&
                        !nowWarning.ContainsKey(alarmCodeID))
                    {
                        WriteSetAlarmCodeLog(alarm);
                        logChange = true;

                        switch (alarm.Level)
                        {
                            case EnumAlarmLevel.Alarm:
                                nowAlarm.Add(alarmCodeID, alarm);
                                localData.MIPCData.BuzzOff = false;
                                localData.MIPCData.HasAlarm = true;
                                break;
                            case EnumAlarmLevel.Warn:
                                nowWarning.Add(alarmCodeID, alarm);
                                localData.MIPCData.HasWarn = true;
                                break;
                        }

                        nowAlarmAndWarning.Add(alarm);
                        alarmAndWanringHistory.Add(alarm);

                        if (alarmAndWanringHistory.Count > maxAlarmHistoryCount)
                            alarmAndWanringHistory.RemoveAt(0);

                        sendMiddlerAlarm = localAlarm;
                    }
                }

                if (sendMiddlerAlarm)
                {
                    Task.Factory.StartNew(() =>
                    {
                        SendMiddlerAlarm?.Invoke(this, alarmCodeID);
                    });
                }
            }
            catch (Exception ex)
            {
                WriteLog(5, "", "Exception : ", ex.ToString());
                WriteLog(5, "", "Exception : ", ex.StackTrace);
            }
        }

        public void ResetAlarmCode(int alarmCodeID)
        {
            try
            {
                lock (lockObject)
                {
                    if (nowAlarm.ContainsKey(alarmCodeID))
                    {
                        WriteResetAlarmCodeLog(nowAlarm[alarmCodeID]);
                        nowAlarm[alarmCodeID].ResetTime = DateTime.Now;
                        nowAlarmAndWarning.Remove(nowAlarm[alarmCodeID]);

                        if (checkTime)
                            alarmAndWanringHistory.Remove(nowAlarm[alarmCodeID]);

                        nowAlarm.Remove(alarmCodeID);
                        logChange = true;

                        if (nowAlarm.Count == 0)
                            localData.MIPCData.HasAlarm = false;

                    }
                    else if (nowWarning.ContainsKey(alarmCodeID))
                    {
                        WriteResetAlarmCodeLog(nowWarning[alarmCodeID]);
                        nowWarning[alarmCodeID].ResetTime = DateTime.Now;
                        nowAlarmAndWarning.Remove(nowWarning[alarmCodeID]);

                        if (checkTime)
                            alarmAndWanringHistory.Remove(nowWarning[alarmCodeID]);

                        nowWarning.Remove(alarmCodeID);
                        logChange = true;

                        if (nowWarning.Count == 0)
                            localData.MIPCData.HasWarn = false;
                    }

                }
            }
            catch (Exception ex)
            {
                WriteLog(5, "", "Exception : ", ex.ToString());
                WriteLog(5, "", "Exception : ", ex.StackTrace);
            }
        }

        private List<int> middlerAlarmAndWarning = new List<int>();

        private Dictionary<int, int> allMiddlerAlarmAndWarning = new Dictionary<int, int>();

        public void SetAlarmCodeByMiddler(int alarmCodeID)
        {
            lock (lockObject_Middler)
            {
                try
                {
                    if (!allMiddlerAlarmAndWarning.ContainsKey(alarmCodeID))
                        allMiddlerAlarmAndWarning.Add(alarmCodeID, 0);

                    if (!middlerAlarmAndWarning.Contains(alarmCodeID))
                    {
                        SetAlarmCode(alarmCodeID, false);
                        middlerAlarmAndWarning.Add(alarmCodeID);
                    }
                }
                catch (Exception ex)
                {
                    WriteLog(5, "", "Exception : ", ex.ToString());
                    WriteLog(5, "", "Exception : ", ex.StackTrace);
                }
            }
        }

        public void ResetAllMiddlerAlarmCode()
        {
            lock (lockObject_Middler)
            {
                try
                {
                    for (int i = 0; i < middlerAlarmAndWarning.Count; i++)
                        ResetAlarmCode(middlerAlarmAndWarning[i]);

                    middlerAlarmAndWarning = new List<int>();
                }
                catch (Exception ex)
                {
                    WriteLog(5, "", "Exception : ", ex.ToString());
                    WriteLog(5, "", "Exception : ", ex.StackTrace);
                }
            }
        }

        public void SendMiddlerOldLocalAlarm()
        {
            List<int> sendMiddlerOldAlarmCode = new List<int>();

            lock (lockObject)
            {
                lock (lockObject_Middler)
                {
                    for (int i = 0; i < nowAlarmAndWarning.Count; i++)
                    {
                        if (!allMiddlerAlarmAndWarning.ContainsKey(nowAlarmAndWarning[i].Id))
                            sendMiddlerOldAlarmCode.Add(nowAlarmAndWarning[i].Id);
                    }
                }
            }

            for (int i = 0; i < sendMiddlerOldAlarmCode.Count; i++)
                SendMiddlerAlarm?.Invoke(this, sendMiddlerOldAlarmCode[i]);
        }
        private void GetTodayAlarmHistory()//get TodayAlarmHistory and put in UI(tbx_Alarm.text) show
        {
            lock (lockObject)
            {
                string KEYWORD = "[SetAlarm]";
                string alarm_history_log_path = Path.Combine(Environment.CurrentDirectory, "Log\\" + alarmHistoryLogName + "\\AlarmHistory.log");
                string temp_file = Path.Combine(Environment.CurrentDirectory, "Log\\" + alarmHistoryLogName + "\\AlarmHistory_temp.log");
                File.Copy(alarm_history_log_path, temp_file, true);

                // check file(path) Exists or not;
                if (!File.Exists(alarm_history_log_path) && !File.Exists(temp_file))
                {
                    WriteLog(2, "", "not find current date AlarmHistory.log file");
                    return;
                }
                try
                {
                    using (StreamReader sr = new StreamReader(temp_file, Encoding.UTF8))
                    {
                        string content = sr.ReadToEnd();
                        char[] delimiter_newline = { '\n' };
                        char[] delimiter_chars = { ' ', ']', '[' };
                        string[] sentence = content.Split(delimiter_newline);
                        foreach (var words in sentence)
                        {
                            if (words.Contains(KEYWORD))
                            {
                                //System.Console.WriteLine($"<{words}>");
                                //todayAlarmHistory += word;
                                string[] word = words.Split(delimiter_chars);
                                if (word.Length > 7)
                                {
                                    Alarm alarm = new Alarm();
                                    alarm.Id = Int32.Parse(word[7]); //歷史資料的alarm code

                                    if (AlarmCodeTable.ContainsKey(alarm.Id))
                                    {
                                        alarm.AlarmText = AlarmCodeTable[alarm.Id].AlarmText;
                                        alarm.Level = AlarmCodeTable[alarm.Id].Level;
                                        alarm.Description = AlarmCodeTable[alarm.Id].Description;
                                    }
                                    //word[0] = 歷史資料的yyyy/MM/dd   word[1]=歷史資料的 HH:mm:ss.fff
                                    alarm.SetTime = DateTime.ParseExact(word[0].ToString() + " " + word[1].ToString(), "yyyy/MM/dd HH:mm:ss.fff", CultureInfo.InvariantCulture);//要抓file history 時間                            
                                    alarmAndWanringHistory.Add(alarm);//放進 alarmAndWanringHistory list

                                    if (alarmAndWanringHistory.Count > maxAlarmHistoryCount)
                                        alarmAndWanringHistory.RemoveAt(0);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteLog(2, "", "Exception : ", ex.ToString());
                    WriteLog(2, "", "Exception : ", ex.StackTrace);
                }

                //把資料取完後並刪除temp資料
                File.Delete(temp_file);
            }
            return;
        }
    }
}
