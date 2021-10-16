using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mirle.Agv.INX.Model;

namespace Mirle.Agv.INX.View
{
    public partial class MoveControlConfig_SensorBypass : UserControl
    {
        public event EventHandler ClickEvent;
        private LocalData localData = LocalData.Instance;
        private EnumSensorSafetyType type;
        private bool buttonChange = false;
        private bool canUdate = false;

        public bool Change
        {
            get
            {
                bool returnValue = buttonChange;
                buttonChange = false;

                return returnValue;
            }
        }

        private string GetStringByTag(string tagString)
        {
            return localData.GetProfaceString(localData.Language, tagString);
        }

        private string GetStringByTag(EnumProfaceStringTag tag)
        {
            return localData.GetProfaceString(localData.Language, tag.ToString());
        }

        public void ChangeLanguage()
        {
            label_Name.Text = GetStringByTag(type.ToString());
        }

        public MoveControlConfig_SensorBypass(EnumSensorSafetyType type)
        {
            this.type = type;
            InitializeComponent();
            label_Name.Text = GetStringByTag(type.ToString());
        }

        public void UpdateValue(bool canUdate)
        {
            this.canUdate = canUdate;

            if (localData.MoveControlData.MoveControlConfig.SensorByPass[type])
            {
                button_ChangeButton.Text = GetStringByTag(EnumProfaceStringTag.開啟中);
                button_ChangeButton.ForeColor = Color.Black;
                button_ChangeButton.BackColor = Color.Transparent;
            }
            else
            {
                button_ChangeButton.Text = GetStringByTag(EnumProfaceStringTag.關閉中);
                button_ChangeButton.ForeColor = Color.Black;
                button_ChangeButton.BackColor = Color.Red;
            }
        }

        private void button_ChangeButton_Click(object sender, EventArgs e)
        {
            button_ChangeButton.Enabled = false;

            object_Click(sender, e);

            if (canUdate)
            {
                localData.MoveControlData.MoveControlConfig.SensorByPass[type] = !localData.MoveControlData.MoveControlConfig.SensorByPass[type];
                buttonChange = true;
            }

            button_ChangeButton.Enabled = true;
        }

        private void object_Click(object sender, EventArgs e)
        {
            ClickEvent?.Invoke(sender, e);
        }
    }
}
