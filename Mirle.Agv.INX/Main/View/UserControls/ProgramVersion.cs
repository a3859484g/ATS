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
    public partial class ProgramVersion : UserControl
    {
        private LocalData localData = LocalData.Instance;

        public ProgramVersion()
        {
            InitializeComponent();
        }

        private string GetStringByTag(EnumProfaceStringTag tag)
        {
            return localData.GetProfaceString(localData.Language, tag.ToString());
        }

        public void ChangeLanguage()
        {
            label_LocalVersionTitle.Text = GetStringByTag(EnumProfaceStringTag.Local版本);
            label_MIPCVersionTitle.Text = GetStringByTag(EnumProfaceStringTag.MIPC通訊板本);
            label_MotionVersionTitle.Text = GetStringByTag(EnumProfaceStringTag.Motion版本);
            label_MiddlerVersionTitle.Text = GetStringByTag(EnumProfaceStringTag.Middler版本);
        }

        public void ShowVersion()
        {
            label_LocalVersion.Text = (localData.LocalVersion == 0 ? "" : localData.LocalVersion.ToString("0.0"));

            label_MIPCVersion.Text = (localData.MIPCVersion == 0 ? "" : localData.MIPCVersion.ToString("0.0"));
            label_MIPCVersion.ForeColor = (Math.Round((double)localData.MIPCVersion, 1) < Math.Round((double)localData.MIPCData_MIPCVersion, 1) ? Color.Red : Color.Black);

            label_MotionVersion.Text = (localData.MotionVersion == 0 ? "" : localData.MotionVersion.ToString("0.0"));
            label_MotionVersion.ForeColor = (Math.Round((double)localData.MotionVersion, 1) < Math.Round((double)localData.MIPCData_MotionVersion, 1) ? Color.Red : Color.Black);

            label_MiddlerVersion.Text = (localData.MiddlerVersion == 0 ? "" : localData.MiddlerVersion.ToString("0.0"));

            this.Visible = true;
        }

        public void HideVersion()
        {
            this.Visible = false;
        }

    }
}
