using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Mirle.Agv.INX.Controller.Tools;
using Mirle.Agv.INX.Model;
using System.Diagnostics;
using System.Reflection;

namespace Mirle.Agv.INX.Controller
{
    public class UserAgent
    {
        private LocalData localData = LocalData.Instance;
        private Stopwatch loginTimer = new Stopwatch();
        private LoggerAgent loggerAgent = LoggerAgent.Instance;
        private string device = MethodInfo.GetCurrentMethod().ReflectedType.Name;
        private string normalLogName = "MainFlow";

        private double autoLogoutTime = 30 * 60 * 1000;

        private EnumLoginLevel logoutLevel = EnumLoginLevel.Engineer;

        public UserAgent()
        {
            LoginLevelChange(logoutLevel);
        }

        private void WriteLog(int logLevel, string carrierId, string message, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            LogFormat logFormat = new LogFormat(normalLogName, logLevel.ToString(), "", "", "", message);

            loggerAgent.Log(logFormat.Category, logFormat);

            if (logLevel <= localData.ErrorLevel)
            {
                logFormat = new LogFormat(localData.ErrorLogName, logLevel.ToString(), memberName, device, carrierId, message);
                loggerAgent.Log(logFormat.Category, logFormat);
            }
        }

        private void LoginLevelChange(EnumLoginLevel newLevel)
        {
            if (newLevel > logoutLevel)
                loginTimer.Restart();

            localData.LoginLevel = newLevel;
        }

        public void UpdateLoginTime()
        {
            if (localData.LoginLevel > logoutLevel && loginTimer.ElapsedMilliseconds > autoLogoutTime)
            {
                Logout();
                loginTimer.Stop();
            }
        }

        public bool Login(string account, string password)
        {
            try
            {
                if (String.Compare(account, EnumLoginLevel.Engineer.ToString(), true) == 0)
                {
                    //LoginLevelChange(EnumLoginLevel.Engineer);
                    //return true;
                }
                else if (String.Compare(account, EnumLoginLevel.Admin.ToString(), true) == 0)
                {
                    if (password == "22099478")
                    {
                        LoginLevelChange(EnumLoginLevel.Admin);
                        WriteLog(7, "", String.Concat("LoginLevel切換至", account, " 成功"));
                        return true;
                    }
                }
                else if (String.Compare(account, EnumLoginLevel.MirleAdmin.ToString(), true) == 0)
                {
                    if (password == DateTime.Now.ToString("HHmm"))
                    {
                        LoginLevelChange(EnumLoginLevel.MirleAdmin);
                        WriteLog(7, "", String.Concat("LoginLevel切換至", account, " 成功"));
                        return true;
                    }
                }

                WriteLog(7, "", String.Concat("LoginLevel切換至", account, " 失敗"));
                return false;
            }
            catch (Exception ex)
            {
                WriteLog(7, "", String.Concat("LoginLevel切換至", account, " 失敗, Exception : ", ex.ToString()));
                return false;
            }
        }

        public void Logout()
        {
            WriteLog(7, "", String.Concat("LoginLevel 從 ", localData.LoginLevel.ToString(), " 切換至 ", logoutLevel));
            localData.LoginLevel = logoutLevel;
            //localData.LoginLevel = EnumLoginLevel.User;
        }
    }
}
