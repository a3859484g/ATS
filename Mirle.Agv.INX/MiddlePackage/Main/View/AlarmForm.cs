using Mirle.Agv.MiddlePackage.Umtc.Controller;
using System;
using System.Threading;
using System.Windows.Forms;

namespace Mirle.Agv.MiddlePackage.Umtc.View
{
    public partial class AlarmForm : Form
    {
        private MainFlowHandler mainFlowHandler;

        public AlarmForm(MainFlowHandler mainFlowHandler)
        {
            InitializeComponent();
            this.mainFlowHandler = mainFlowHandler;
        }

        private void btnAlarmReset_Click(object sender, EventArgs e)
        {
            btnAlarmReset.Enabled = false;
            mainFlowHandler.AlarmHandler.ResetAllAlarmsFromAgvm();
            Thread.Sleep(500);
            btnAlarmReset.Enabled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.SendToBack();
            this.Hide();
        }

        private void timeUpdateUI_Tick(object sender, EventArgs e)
        {
            tbxHappendingAlarms.Text = mainFlowHandler.AlarmHandler.GetAlarmLogMsg();
            tbxHistoryAlarms.Text = mainFlowHandler.AlarmHandler.GetAlarmHistoryLogMsg();
        }
    }
}
