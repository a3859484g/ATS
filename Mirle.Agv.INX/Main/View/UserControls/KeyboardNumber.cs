using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace Mirle.Agv.INX.View
{
    public partial class KeyboardNumber : UserControl
    {
        public KeyboardNumber()
        {
            InitializeComponent();
        }

        private string showValue;

        private double newValue;

        private bool flag = true;

        TextBox target = null;

        private bool passward = false;

        public void SetTextBoxAndShow(TextBox target)
        {
            this.target = target;
            Reset();
            this.Visible = true;
        }

        public void SetPasswardMode()
        {
            passward = true;
        }

        public void HideKeyboard()
        {
            this.Hide();
        }

        private void Reset()
        {
            showValue = "";
            flag = true;
            ShowValue(2);
        }

        private object lockObject = new object();

        // 0 : add, 1 : delete, 2 : Reset
        private void ShowValue(int typeValue = 0)
        {
            if (passward)
            {
                if (typeValue == 0)
                    label_View.Text = String.Concat(label_View.Text, "*");
                else if (typeValue == 1)
                {
                    if (label_View.Text.Length != 0)
                        label_View.Text = label_View.Text.Substring(0, label_View.Text.Length - 1);
                }
                else
                    label_View.Text = "";
            }
            else
            {
                if (flag)
                    label_View.Text = showValue;
                else
                    label_View.Text = String.Concat("-", showValue);
            }
        }

        private void ProcessButtonEvent(string value)
        {
            string newString;

            if (passward)
                showValue = String.Concat(showValue, value);
            else
            {
                if (showValue == "0" && value != ".")
                    newString = value;
                else
                    newString = String.Concat(showValue, value);

                if (double.TryParse(newString, out newValue))
                    showValue = newString;
            }

            ShowValue();
        }

        private void button0_Click(object sender, EventArgs e)
        {
            lock (lockObject)
                ProcessButtonEvent("0");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            lock (lockObject)
                ProcessButtonEvent("1");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            lock (lockObject)
                ProcessButtonEvent("2");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            lock (lockObject)
                ProcessButtonEvent("3");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            lock (lockObject)
                ProcessButtonEvent("4");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            lock (lockObject)
                ProcessButtonEvent("5");
        }

        private void button6_Click(object sender, EventArgs e)
        {
            lock (lockObject)
                ProcessButtonEvent("6");
        }

        private void button7_Click(object sender, EventArgs e)
        {
            lock (lockObject)
                ProcessButtonEvent("7");
        }

        private void button8_Click(object sender, EventArgs e)
        {
            lock (lockObject)
                ProcessButtonEvent("8");
        }

        private void button9_Click(object sender, EventArgs e)
        {
            lock (lockObject)
                ProcessButtonEvent("9");
        }

        private void buttonDot_Click(object sender, EventArgs e)
        {
            lock (lockObject)
                ProcessButtonEvent(".");
        }

        private void buttonNag_Click(object sender, EventArgs e)
        {
            lock (lockObject)
            {
                if (showValue == "")
                    flag = false;

                ShowValue();
            }
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            lock (lockObject)
            {
                if (showValue.Length > 0)
                    showValue = showValue.Substring(0, showValue.Length - 1);
                else
                    flag = true;

                ShowValue(1);
            }
        }

        private void buttonEnter_Click(object sender, EventArgs e)
        {
            lock (lockObject)
            {
                if (!flag)
                    showValue = String.Concat("-", showValue);

                if (passward)
                {
                    if (target != null)
                        target.Text = showValue;
                }
                else
                {
                    if (double.TryParse(showValue, out newValue))
                    {
                        if (target != null)
                            target.Text = showValue;
                    }
                }

                this.Hide();
            }
        }
    }
}
