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
    public partial class MoveControlConfig_Safety : UserControl
    {
        private LocalData localData = LocalData.Instance;
        private EnumMoveControlSafetyType type;
        private bool buttonChange = false;
        private bool textBoxChange = false;
        private bool canUpdate = false;

        public event EventHandler ClickEvent;

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

            if (localData.MoveControlData.MoveControlConfig.Safety[type].Enable)
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

        public bool Change
        {
            get
            {
                bool returnValue = buttonChange || textBoxChange;

                buttonChange = false;
                textBoxChange = false;

                return returnValue;
            }
        }

        public TextBox GetTextBox
        {
            get
            {
                return tB_Value;
            }
        }

        public MoveControlConfig_Safety(EnumMoveControlSafetyType type)
        {
            this.type = type;
            InitializeComponent();

            label_Name.Text = GetStringByTag(type.ToString());
            tB_Value.Text = localData.MoveControlData.MoveControlConfig.Safety[type].Range.ToString("0.0");
        }

        public void UpdateValue(bool canUpdate)
        {
            this.canUpdate = canUpdate;

            if (localData.MoveControlData.MoveControlConfig.Safety[type].Range.ToString("0.0") != tB_Value.Text)
            {
                if (canUpdate)
                {
                    double newValue;

                    if (localData.LoginLevel >= EnumLoginLevel.Admin && double.TryParse(tB_Value.Text, out newValue) && newValue >= 0)
                    {
                        newValue = Math.Round(newValue, 2);

                        localData.MoveControlData.MoveControlConfig.Safety[type].Range = newValue;
                        textBoxChange = true;
                    }
                }

                tB_Value.Text = localData.MoveControlData.MoveControlConfig.Safety[type].Range.ToString("0.0");
            }

            if (localData.MoveControlData.MoveControlConfig.Safety[type].Enable)
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
            Object_Click(sender, e);

            if (canUpdate)
            {
                localData.MoveControlData.MoveControlConfig.Safety[type].Enable = !localData.MoveControlData.MoveControlConfig.Safety[type].Enable;
                buttonChange = true;
            }

            button_ChangeButton.Enabled = true;
        }

        private void Object_Click(object sender, EventArgs e)
        {
            ClickEvent?.Invoke(sender, e);
        }
    }
}
