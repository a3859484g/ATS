using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;

namespace Mirle.Agv.INX.Controller.Tools
{
    [Serializable]
    public class Logger
    {
        public static readonly long MB = 1024 * 1024;
        public static readonly int LOG_FORMAT_LENGTH = 19;

        private LogType logType;

        private string directoryFullPath = "Empty";

        private FileStream fileStream;
        private StreamWriter streamWriter;
        private Encoding encodingType = Encoding.UTF8;  // 設定編碼格式字元編碼/解碼 類別

        private string logFileFullPath = "";
        private long lngLogMaxSize = 0;

        private DateTime dtTimeOfOverdueFileCheck = DateTime.Now.AddHours(-2);

        private ConcurrentQueue<string> queInputLogData;
        private ConcurrentQueue<string> queOutputLogData;
        private Thread thdDataSave;

        private DateTime writingFileDate = DateTime.Now;

        public Logger(LogType aLogType)
        {
            try
            {
                logType = aLogType;

                queInputLogData = new ConcurrentQueue<string>();
                queOutputLogData = new ConcurrentQueue<string>();

                thdDataSave = new Thread(ThreadBufferDataSave);
                thdDataSave.IsBackground = true;
                thdDataSave.Start();

                lngLogMaxSize = logType.LogMaxSize * MB;
                MakeSurePathExist();
            }
            catch (Exception ex)
            {
                ExceptionLog("Logger", ex.StackTrace);
            }
        }

        private void MakeSurePathExist()
        {
            MakeSurePathValid(logType.LogFileName);
            MakeSurePathValid(logType.DirName);
            
            if (logType.FileExtension == ".csv")
                directoryFullPath = Path.Combine(Environment.CurrentDirectory, "CSV", logType.DirName);
            else
                directoryFullPath = Path.Combine(Environment.CurrentDirectory, "Log", logType.DirName);
            
            //directoryFullPath = Path.Combine(Environment.CurrentDirectory, "Log", logType.DirName); // 舊寫法.

            var saveFullName = string.Concat(logType.LogFileName, logType.FileExtension); // 存檔名稱
            logFileFullPath = Path.Combine(directoryFullPath, saveFullName);        // 要被開啟處理的檔案

            if (!Directory.Exists(directoryFullPath))
            {
                Directory.CreateDirectory(directoryFullPath);
            }

            if (!File.Exists(logFileFullPath))
            {
                CreateNewFileWithFirstLine();
            }
            else
            {
                // 新加讀取最後修改時間是否沒帶日期的是否換檔案, 不確定是否會GG.(測試過看起來OK,但是有點抖抖的)
                try
                {
                    FileInfo file = new FileInfo(logFileFullPath);

                    if (file.LastWriteTime.DayOfYear != DateTime.Now.DayOfYear ||
                        file.LastWriteTime.Year != DateTime.Now.Year)
                    {
                        writingFileDate = DateTime.Now;

                        var dateTime = String.Concat(file.LastWriteTime.ToString("yyyy-MM-dd"), "_23-59-59");
                        var copyName = string.Concat(logType.LogFileName, "_", dateTime, logType.FileExtension);

                        copyName = Path.Combine(directoryFullPath, copyName);

                        for (int i = 1; File.Exists(copyName); i++)
                        {
                            copyName = string.Concat(logType.LogFileName, "_", dateTime, "-", i.ToString(), logType.FileExtension);
                            copyName = Path.Combine(directoryFullPath, copyName);
                        }

                        File.Copy(logFileFullPath, copyName);

                        CreateNewFileWithFirstLine();
                    }
                }
                catch
                {

                }
            }

            fileStream = new FileStream(logFileFullPath, FileMode.Append, FileAccess.Write, FileShare.Read);
            streamWriter = new StreamWriter(fileStream, encodingType);
        }

        private void CreateNewFileWithFirstLine()
        {
            using (FileStream stream = File.Create(logFileFullPath))
            {
                using (StreamWriter sw = new StreamWriter(stream, encodingType))
                {
                    if (!string.IsNullOrWhiteSpace(logType.FirstLineString))
                    {
                        sw.WriteLine(logType.FirstLineString);
                        sw.Flush();
                    }
                }
            }
        }

        public void ExceptionLog(string aFunctionName, string aMessage)
        {
            try
            {
                using (FileStream stream = new FileStream("LoggerException.txt", FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    using (StreamWriter sw = new StreamWriter(stream, encodingType))
                    {
                        sw.WriteLine(string.Concat(DateTime.Now.ToString("yyyy-MM-dd HH-mm:ss.fff"), "\t", aFunctionName, "\t", aMessage));
                        sw.Flush();
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        #region Enqueue

        public void Log(LogFormat logFormat)
        {
            try
            {
                LogToQueue(logFormat.GetString());
            }
            catch (Exception ex)
            {
                ExceptionLog("SaveLogFile", string.Concat(logFormat.Message, ex.StackTrace));
            }
        }

        public void LogString(string aMessage)
        {
            try
            {
                LogToQueue(aMessage);
            }
            catch (Exception ex)
            {
                ExceptionLog("SavePureLog", string.Concat(aMessage, ex.StackTrace));
            }
        }

        public void LogString(string classMethodName, string aMessage)
        {
            try
            {
                LogToQueue(string.Concat(DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss.fff"), ",\t", classMethodName, ",\t", aMessage));
            }
            catch (Exception ex)
            {
                ExceptionLog("SavePureLog", string.Concat(aMessage, ex.StackTrace));
            }
        }

        private void LogToQueue(string aMessage)
        {
            try
            {
                if (logType.LogEnable)
                {
                    aMessage.Replace(Environment.NewLine, logType.LineSeparateToken);
                    queInputLogData.Enqueue(aMessage);
                }
            }
            catch (Exception ex)
            {
                ExceptionLog("SaveLogFile", aMessage + ex.StackTrace);
            }
        }

        #endregion

        #region Dequeue

        private void ThreadBufferDataSave()
        {
            while (true)
            {
                try
                {
                    CheckChangeDay();
                    CheckOverdueDate();

                    queOutputLogData = queInputLogData;
                    queInputLogData = new ConcurrentQueue<string>();
                    bool isWrittenToFile = false;

                    while (!queOutputLogData.IsEmpty)
                    {
                        CheckFileSize();

                        if (queOutputLogData.TryDequeue(out string msg))
                        {
                            WriteLog(msg);
                            isWrittenToFile = true;
                        }
                    }

                    if (isWrittenToFile)
                    {
                        SpinWait.SpinUntil(() => false, logType.DequeueWithoutEmptyInterval);
                    }
                    else
                    {
                        SpinWait.SpinUntil(() => false, logType.DequeueInterval);
                    }

                }
                catch (Exception ex)
                {
                    ExceptionLog("ThreadDataSave", ex.StackTrace);
                    SpinWait.SpinUntil(() => false, logType.DequeueInterval);
                }
            }
        }

        private void WriteLog(string aMessage)
        {
            streamWriter.WriteLine(aMessage);
            streamWriter.Flush();
        }

        private void CheckChangeDay()
        {
            if (DateTime.Now.Day != writingFileDate.Day)
            {
                writingFileDate = DateTime.Now;

                var dateTime = String.Concat(writingFileDate.AddDays(-1).ToString("yyyy-MM-dd"), "_23-59-59");
                var copyName = string.Concat(logType.LogFileName, "_", dateTime, logType.FileExtension);

                copyName = Path.Combine(directoryFullPath, copyName);

                for (int i = 1; File.Exists(copyName); i++)
                {
                    copyName = string.Concat(logType.LogFileName, "_", dateTime, "-", i.ToString(), logType.FileExtension);
                    copyName = Path.Combine(directoryFullPath, copyName);
                }

                File.Copy(logFileFullPath, copyName);

                streamWriter.Close();
                fileStream.Close();

                fileStream = new FileStream(logFileFullPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                streamWriter = new StreamWriter(fileStream, encodingType);

                if (!string.IsNullOrWhiteSpace(logType.FirstLineString))
                    WriteLog(logType.FirstLineString);
            }
        }

        private void CheckFileSize()
        {
            var fileSize = new FileInfo(logFileFullPath).Length;

            if (fileSize > lngLogMaxSize)
            {
                // 超過限制的大小，換檔再刪除
                SpinWait.SpinUntil(() => false, 1000); // 避免產生同時間的檔案
                var dateTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var copyName = string.Concat(logType.LogFileName, "_", dateTime, logType.FileExtension);
                copyName = Path.Combine(directoryFullPath, copyName);


                File.Copy(logFileFullPath, copyName);

                // 清除檔案內容                
                //FileStream stream = new FileStream(logFileFullPath, FileMode.Create);
                //stream.Close();

                streamWriter.Close();
                fileStream.Close();

                fileStream = new FileStream(logFileFullPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                streamWriter = new StreamWriter(fileStream, encodingType);

                if (!string.IsNullOrWhiteSpace(logType.FirstLineString))
                {
                    WriteLog(logType.FirstLineString);
                }
            }
        }

        private void CheckOverdueFile()
        {
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(directoryFullPath);
                FileInfo[] allFiles = dirInfo.GetFiles();

                foreach (FileInfo fileInfo in allFiles)
                {
                    string fileName = fileInfo.Name;
                    int startPos = fileName.IndexOf("_", 0);
                    int endPos = fileName.IndexOf(logType.FileExtension, 0);
                    if (startPos != 0 && endPos != 0 && (startPos + 19) < endPos)
                    {
                        string fileDateTime = fileName.Substring(startPos + 1, 19);
                        if (fileDateTime.Length == LOG_FORMAT_LENGTH)
                        {
                            DateTime fileDate = DateTime.ParseExact(fileDateTime, "yyyy-MM-dd_HH-mm-ss", null);

                            if (DayDiff(fileDate, DateTime.Now) > logType.FileKeepDay)
                            {
                                string sFilePath = Path.Combine(directoryFullPath, fileName);
                                if (File.Exists(sFilePath))
                                    File.Delete(sFilePath);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionLog("CheckOverdueFile", ex.StackTrace);
            }
        }

        private void CheckOverdueDate()
        {
            if (logType.DelOverdueFile)
            {
                if (DateTime.Compare(DateTime.Now, dtTimeOfOverdueFileCheck.AddHours(1)) > 0)
                {
                    CheckOverdueFile();
                    dtTimeOfOverdueFileCheck = DateTime.Now;
                }
            }
        }

        //Replace VB Datediff function
        private int DayDiff(DateTime startDate, DateTime endDate)
        {
            TimeSpan TS = new TimeSpan(endDate.Ticks - startDate.Ticks);
            return Convert.ToInt32(TS.TotalDays);
        }

        #endregion

        public string GetLogTypeName()
        {
            return logType.Name;
        }

        public void MakeSurePathValid(string path)
        {

            char[] errorChar = new char[] { ',', '>', '<', '-', '!', '~' };

            // 判斷是否傳入值為空
            if (string.IsNullOrWhiteSpace(path))
            {
                path = "Empty";
            }

            foreach (char badChar in Path.GetInvalidPathChars())
            {
                if (path.IndexOf(badChar) > -1)
                    path = "HasInvalidCharInPath";
            }

            foreach (char badChar in errorChar)
            {
                if (path.IndexOf(badChar) > -1)
                    // MessageBox.Show("名稱中有不合法的字元")
                    path = "HasErrorCharInPath";
            }
        }
    }
}
