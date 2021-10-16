using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mirle.Agv.INX.View
{
    public partial class LabelNameAndValue : UserControl
    {
        public event EventHandler ClickEvent;
        public event EventHandler<Point> MouseDownEvent;
        public event EventHandler<Point> MouseMoveEvent;
        public event EventHandler MouseUpEvent;

        public LabelNameAndValue(string name, bool hideBorderStyle = false, float labelSize = 14)
        {
            InitializeComponent();

            this.label_Name.Font = new System.Drawing.Font("新細明體", labelSize);

            label_Name.Text = String.Concat(name, " : ");

            if (label_Name.Size.Width > label_Value.Location.X)
            {
                this.Size = new Size(this.Size.Width + (label_Name.Size.Width - label_Value.Location.X), this.Size.Height);
                label_Value.Location = new Point(label_Name.Size.Width, label_Value.Location.Y);
            }

            if (hideBorderStyle)
            {
                label_Value.BorderStyle = BorderStyle.None;
                label_Value.AutoSize = true;
                label_Value.Location = new Point(label_Value.Location.X, label_Name.Location.Y);
            }

            this.Click += LabelNameAndValue_Click;
            label_Name.Click += Label_Name_Click;
            label_Value.Click += Label_Value_Click;

            label_Name.MouseDown += Label_MouseDown;
            label_Name.MouseMove += Label_MouseMove;
            label_Name.MouseUp += Label_MouseUp;

            label_Value.MouseDown += Label_MouseDown;
            label_Value.MouseMove += Label_MouseMove;
            label_Value.MouseUp += Label_MouseUp;
        }

        private void Label_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                MouseUpEvent?.Invoke(sender, null);
        }

        private void Label_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                MouseMoveEvent?.Invoke(sender, new Point(((Label)sender).Location.X + e.Location.X,
                                                         ((Label)sender).Location.Y + e.Location.Y));
        }

        private void Label_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                MouseDownEvent.Invoke(sender, new Point(((Label)sender).Location.X + e.Location.X,
                                                        ((Label)sender).Location.Y + e.Location.Y));
        }

        public void ReSize(Size sizeName, Size sizeValue)
        {
            this.Size = new Size(sizeName.Width + sizeValue.Width, sizeName.Height);
            label_Name.AutoSize = false;
            label_Name.TextAlign = ContentAlignment.MiddleLeft;
            label_Name.Size = sizeName;
            label_Value.Size = sizeValue;
            label_Value.Location = new Point(sizeName.Width, 0);
        }

        private void Label_Value_Click(object sender, EventArgs e)
        {
            ClickEventFunction();
        }

        private void Label_Name_Click(object sender, EventArgs e)
        {
            ClickEventFunction();
        }

        private void LabelNameAndValue_Click(object sender, EventArgs e)
        {
            ClickEventFunction();
        }

        private void ClickEventFunction()
        {
            ClickEvent?.Invoke(this, null);
        }

        public void ReName(string name)
        {
            label_Name.Text = String.Concat(name, " : ");

            if (label_Name.Size.Width > label_Value.Location.X)
            {
                this.Size = new Size(this.Size.Width + (label_Name.Size.Width - label_Value.Location.X), this.Size.Height);
                label_Value.Location = new Point(label_Name.Size.Width, label_Value.Location.Y);
            }
        }

        public void SetLabelValueBigerr(int extraLength)
        {
            label_Value.Size = new Size(label_Value.Size.Width + extraLength, label_Value.Size.Height);

            this.Size = new Size(this.Size.Width + extraLength, this.Size.Height);
        }

        public void SetValueAndColor(string value, int enumInt = 0)
        {
            label_Value.Text = value;

            if (label_Value.Location.X + label_Value.Size.Width > this.Size.Width)
                this.Size = new Size(label_Value.Location.X + label_Value.Size.Width, this.Size.Height);

            if (enumInt < 100)
                label_Value.ForeColor = Color.Black;
            else if (enumInt < 200)
                label_Value.ForeColor = Color.Green;
            else if (enumInt < 300)
                label_Value.ForeColor = Color.Yellow;
            else if (enumInt < 400)
                label_Value.ForeColor = Color.Red;
            else
                label_Value.ForeColor = Color.DarkRed;
        }

        public void SetReady(bool ready)
        {
            if (ready)
            {
                label_Value.Text = "Ready";
                label_Value.ForeColor = Color.Green;
            }
            else
            {
                label_Value.Text = "NotReady";
                label_Value.ForeColor = Color.Red;
            }
        }

        public void SetError(bool error)
        {
            if (error)
            {
                label_Value.Text = "Error";
                label_Value.ForeColor = Color.Red;
            }
            else
            {
                label_Value.Text = "Normal";
                label_Value.ForeColor = Color.Green;
            }
        }
    }
}
