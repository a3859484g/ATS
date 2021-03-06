using Mirle.Agv.MiddlePackage.Umtc.Controller;
using Mirle.Agv.MiddlePackage.Umtc.Model;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace Mirle.Agv.MiddlePackage.Umtc.View
{
    public partial class LoginForm : Form
    {
        private UserAgent userAgent;
        private Vehicle Vehicle { get; set; } = Vehicle.Instance;
        //private MirleLogger mirleLogger = MirleLogger.Instance;
        private NLog.Logger _transferLogger = NLog.LogManager.GetLogger("Transfer");

        public LoginForm(UserAgent userAgent)
        {
            InitializeComponent();
            this.userAgent = userAgent;
            InitialBoxUserName();
        }

        private void InitialBoxUserName()
        {
            foreach (var userName in Enum.GetNames(typeof(EnumLoginLevel)))
            {
                if (userName != EnumLoginLevel.OneAboveAll.ToString())
                {
                    boxUserName.Items.Add(userName);
                }
            }
            boxUserName.SelectedIndex = (int)EnumLoginLevel.Op;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            try
            {
                Vehicle.LoginLevel = userAgent.GetLoginLevel(boxUserName.SelectedItem.ToString(), txtPassword.Text);
                if (Vehicle.LoginLevel != EnumLoginLevel.Op)
                {
                    this.Hide();
                }
            }
            catch (Exception ex)
            {
                LogException(GetType().Name + ":" + MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            Vehicle.LoginLevel = EnumLoginLevel.Op;
            this.Hide();
        }

        private void LogException(string classMethodName, string exMsg)
        {
            //mirleLogger.Log(new LogFormat("MainError", "5", classMethodName, Vehicle.AgvcConnectorConfig.ClientName, "CarrierID", exMsg));

            _transferLogger.Error($"[{Vehicle.SoftwareVersion}][{Vehicle.AgvcConnectorConfig.ClientName}][{classMethodName}][{exMsg}]");
        }

        private void btnHide_Click(object sender, EventArgs e)
        {
            this.Hide();
        }
    }
}
