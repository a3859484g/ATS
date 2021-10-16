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
    public partial class PIOTimeAndValueUserControl : UserControl
    {
        private int inputCount = 0;
        private int outputCount = 0;
        private Label timeLabel;

        private List<Label> inputLabelList = new List<Label>();
        private List<Label> outputLabelList = new List<Label>();

        public event EventHandler<Point> OjbectMouseDown;
        public event EventHandler<Point> OjbectMouseMove;
        public event EventHandler OjbectMouseUp;

        public PIOTimeAndValueUserControl()
        {
            InitializeComponent();
        }

        public void InitialLabel(Size timeSize, int width, int heigh, List<int> inputStartX, List<int> outputStartX)
        {
            inputCount = inputStartX.Count;
            outputCount = outputStartX.Count;

            #region Time.
            timeLabel = new Label();
            timeLabel.Font = new System.Drawing.Font("標楷體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            timeLabel.Size = timeSize;
            timeLabel.TextAlign = ContentAlignment.MiddleCenter;
            timeLabel.BorderStyle = BorderStyle.FixedSingle;
            timeLabel.Location = new Point(0, 0);

            timeLabel.MouseDown += Object_MouseDown;
            timeLabel.MouseUp += Object_MouseUp;
            timeLabel.MouseMove += Object_MouseMove;

            this.Controls.Add(timeLabel);
            #endregion

            Label tempLabel;

            #region Input.
            for (int i = 0; i < inputCount; i++)
            {
                tempLabel = new Label();
                tempLabel.Size = new Size(width + 1, heigh + 1);
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.Location = new Point(inputStartX[i], 0);

                tempLabel.MouseDown += Object_MouseDown;
                tempLabel.MouseUp += Object_MouseUp;
                tempLabel.MouseMove += Object_MouseMove;

                this.Controls.Add(tempLabel);
                inputLabelList.Add(tempLabel);
            }
            #endregion

            #region Output.
            for (int i = 0; i < outputCount; i++)
            {
                tempLabel = new Label();
                tempLabel.Size = new Size(width + 1, heigh + 1);
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.Location = new Point(outputStartX[i], 0);

                tempLabel.MouseDown += Object_MouseDown;
                tempLabel.MouseUp += Object_MouseUp;
                tempLabel.MouseMove += Object_MouseMove;

                this.Controls.Add(tempLabel);
                outputLabelList.Add(tempLabel);
            }
            #endregion

            this.MouseDown += Object_MouseDown;
            this.MouseUp += Object_MouseUp;
            this.MouseMove += Object_MouseMove;
            this.Size = new Size(outputStartX[outputCount - 1] + width + 1, heigh + 1);
        }

        public void SetData(PIODataAndTime data, DateTime startTime)
        {
            timeLabel.Text = ((data.Time - startTime).TotalMilliseconds / 1000).ToString("0.00");

            uint tempUint = data.Input;

            for (int i = 0; i < inputCount; i++, tempUint = tempUint >> 1)
                inputLabelList[inputCount - i - 1].BackColor = (tempUint & 1) != 0 ? Color.Green : Color.Transparent;

            tempUint = data.Output;

            for (int i = 0; i < outputCount; i++, tempUint = tempUint >> 1)
                outputLabelList[outputCount - i - 1].BackColor = (tempUint & 1) != 0 ? Color.Green : Color.Transparent;
        }

        private void Object_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                OjbectMouseMove?.Invoke(sender, new Point(e.Location.X + this.Location.X, e.Location.Y + this.Location.Y));
        }

        private void Object_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                OjbectMouseUp?.Invoke(OjbectMouseUp, null);
        }

        private void Object_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                OjbectMouseDown?.Invoke(sender, new Point(e.Location.X + this.Location.X, e.Location.Y + this.Location.Y));
        }
    }
}
