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
using Mirle.Agv.INX.Controller;

namespace Mirle.Agv.INX.View
{
    public partial class LoginForm : UserControl
    {
        private LocalData localData = LocalData.Instance;
        private KeyboardNumber keyboardNumber;
        private UserAgent userAgent;

        public LoginForm(UserAgent userAgent)
        {
            this.userAgent = userAgent;
            InitializeComponent();
            InitialUserControl();
        }

        private string GetStringByTag(EnumProfaceStringTag tag)
        {
            return localData.GetProfaceString(localData.Language, tag.ToString());
        }

        public void ChangeLanguage()
        {
            button_login.Text = GetStringByTag(EnumProfaceStringTag.Login);
            button_Logout.Text = GetStringByTag(EnumProfaceStringTag.Logout);
        }

        public void ShowLoginForm()
        {
            tB_Password.Text = "";
            this.Show();
            keyboardNumber.HideKeyboard();
        }
        
        public void HideLogForm()
        {
            this.Hide();
        }

        private void InitialUserControl()
        {
            keyboardNumber = new KeyboardNumber();
            keyboardNumber.SetPasswardMode();
            keyboardNumber.Location = new Point((this.Size.Width - keyboardNumber.Width) / 2,
                                                (this.Size.Height - keyboardNumber.Height) / 2);
            this.Controls.Add(keyboardNumber);

            keyboardNumber.HideKeyboard();

            cB_LoginLevel.Items.Add(EnumLoginLevel.Engineer.ToString());
            cB_LoginLevel.Items.Add(EnumLoginLevel.Admin.ToString());
            cB_LoginLevel.Items.Add(EnumLoginLevel.MirleAdmin.ToString());

            tB_Password.Click += new System.EventHandler(this.CallKeyboardNumber);
        }

        private void CallKeyboardNumber(object sender, EventArgs e)
        {
            keyboardNumber.SetTextBoxAndShow((TextBox)sender);
            keyboardNumber.BringToFront();
        }

        private void button_login_Click(object sender, EventArgs e)
        {
            if (userAgent.Login(cB_LoginLevel.Text, tB_Password.Text))
            {
                tB_Password.Text = "";
                MessageBox.Show(GetStringByTag(EnumProfaceStringTag.Login_Success));
                HideLogForm();
            }
            else
            {
                tB_Password.Text = "";
                MessageBox.Show(GetStringByTag(EnumProfaceStringTag.Login_Failed));
            }
        }

        private void button_Logout_Click(object sender, EventArgs e)
        {
            userAgent.Logout();
        }

        private void Hide_Click(object sender, EventArgs e)
        {
            keyboardNumber.HideKeyboard();
        }

        private void tB_Password_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
                button_login_Click(sender, null);
        }
    }
}
