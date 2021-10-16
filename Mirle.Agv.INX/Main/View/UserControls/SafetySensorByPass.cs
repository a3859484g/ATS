using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mirle.Agv.INX.Control;
using Mirle.Agv.INX.Model;

namespace Mirle.Agv.INX.View
{
    public partial class SafetySensorByPass : UserControl
    {
        private LocalData localData = LocalData.Instance;
        private SafetySensor sensor;

        private string normalString = "Normal";
        private string byPassString = "ByPass";

        public SafetySensorByPass(SafetySensor sensor)
        {
            this.sensor = sensor;
            InitializeComponent();

            label_DeviceName.Text = sensor.Config.Device;
        }

        public void UpdateInfo()
        {
            if (sensor.ByPassAlarm == 1)
            {
                button_AlarmByPass.Text = byPassString;
                button_AlarmByPass.ForeColor = Color.Black;
                button_AlarmByPass.BackColor = Color.Red;
            }
            else
            {
                button_AlarmByPass.Text = normalString;
                button_AlarmByPass.ForeColor = Color.Green;
                button_AlarmByPass.BackColor = Color.Transparent;
            }

            if (sensor.ByPassStatus == 1)
            {
                button_SafetyByPass.Text = byPassString;
                button_SafetyByPass.ForeColor = Color.Black;
                button_SafetyByPass.BackColor = Color.Red;
            }
            else
            {
                button_SafetyByPass.Text = normalString;
                button_SafetyByPass.ForeColor = Color.Green;
                button_SafetyByPass.BackColor = Color.Transparent;
            }
        }

        private void button_AlarmByPass_Click(object sender, EventArgs e)
        {
            if (localData.LoginLevel == EnumLoginLevel.Admin)
            {
                if (sensor.ByPassAlarm == 1)
                    sensor.ByPassAlarm = 0;
                else
                    sensor.ByPassAlarm = 1;
            }
        }

        private void button_SafetyByPass_Click(object sender, EventArgs e)
        {
            if (localData.LoginLevel >= EnumLoginLevel.Engineer)
            {
                if (sensor.ByPassStatus == 1)
                    sensor.ByPassStatus = 0;
                else
                    sensor.ByPassStatus = 1;
            }
        }
    }
}
