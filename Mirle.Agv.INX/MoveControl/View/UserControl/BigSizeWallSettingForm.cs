using Mirle.Agv.INX.Controller;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mirle.Agv.INX.View
{
    public partial class BigSizeWallSettingForm : Form
    {
        private WallSetting wallSettingForm = null;

        public BigSizeWallSettingForm(WallSettingControl wallSetting)
        {
            InitializeComponent();

            wallSettingForm = new WallSetting(wallSetting);
            wallSettingForm.Location = new Point(0, 0);
            wallSettingForm.Size = new Size(1500, 800);
            this.Controls.Add(wallSettingForm);
        }

        public void ShowForm()
        {
            timer1.Enabled = true;
            this.Show();
        }

        private void button_Hide_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            this.Hide();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            wallSettingForm.Timer_Update();
        }
    }
}
