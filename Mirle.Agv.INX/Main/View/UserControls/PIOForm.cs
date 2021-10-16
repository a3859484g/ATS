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
using Mirle.Agv.INX.Controller;
using Mirle.Agv.INX.Model;

namespace Mirle.Agv.INX.View
{
    public partial class PIOForm : UserControl
    {
        private LocalData localData = LocalData.Instance;
        private PIOFlow pio;
        private MIPCControlHandler mipcControl;

        private List<string> inputList = new List<string>();
        private List<string> outputList = new List<string>();

        private List<string> inputNameList = new List<string>();
        private List<string> outputNameList = new List<string>();

        public PIOForm(MIPCControlHandler mipcControl, PIOFlow pio)
        {
            this.mipcControl = mipcControl;
            this.pio = pio;
            inputList = pio.PIOInputTagList;
            outputList = pio.PIOOutputTagList;

            inputNameList = pio.PIOInputNameList;
            outputNameList = pio.PIOOutputNameList;

            InitializeComponent();
            InitialStatus();
        }

        private List<PictureBox> inputStatusList = new List<PictureBox>();
        private List<PictureBox> outputStatusList = new List<PictureBox>();

        private int defaultWidth = 85;
        private int defaultHeigh = 40;

        private int defaultStatusWidth = 85;
        private int defaultStatusHeigh = 40;

        private void InitialStatus()
        {
            int deltaX = (this.Size.Width - defaultWidth * inputList.Count) / (inputList.Count + 1);

            Label tempLabel;
            PictureBox tempPicture;

            for (int i = 0; i < inputList.Count; i++)
            {
                tempLabel = new Label();
                tempLabel.AutoSize = false;
                tempLabel.TextAlign = ContentAlignment.MiddleCenter;
                tempLabel.Size = new Size(defaultWidth, defaultHeigh);
                tempLabel.Location = new Point((deltaX + defaultWidth) * i + deltaX, 30);
                tempLabel.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

                tempLabel.Text = inputNameList[i];

                this.Controls.Add(tempLabel);

                tempPicture = new PictureBox();
                tempPicture.Size = new Size(defaultStatusWidth, defaultStatusHeigh);
                tempPicture.Location = new Point(tempLabel.Location.X + tempLabel.Size.Width / 2 - defaultStatusWidth / 2, 80);
                tempPicture.BorderStyle = BorderStyle.FixedSingle;

                inputStatusList.Add(tempPicture);
                this.Controls.Add(tempPicture);
            }

            deltaX = (this.Size.Width - defaultWidth * outputList.Count) / (outputList.Count + 1);

            for (int i = 0; i < outputList.Count; i++)
            {
                tempLabel = new Label();
                tempLabel.AutoSize = false;
                tempLabel.TextAlign = ContentAlignment.MiddleCenter;
                tempLabel.Size = new Size(defaultWidth, defaultHeigh);
                tempLabel.Location = new Point((deltaX + defaultWidth) * i + deltaX, 200);
                tempLabel.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

                tempLabel.Text = outputNameList[i];
                this.Controls.Add(tempLabel);

                tempPicture = new PictureBox();
                tempPicture.Text = "";
                tempPicture.Name = outputList[i];
                tempPicture.Size = new Size(defaultStatusWidth, defaultStatusHeigh);
                tempPicture.BackColor = Color.Transparent;
                tempPicture.BorderStyle = BorderStyle.FixedSingle;
                tempPicture.Location = new Point((deltaX + defaultWidth) * i + deltaX, 250);
                tempPicture.MouseDown += new System.Windows.Forms.MouseEventHandler(this.outputPicture_MouseDown);
                tempPicture.MouseUp += new System.Windows.Forms.MouseEventHandler(this.outputPicture_MouseUp);

                outputStatusList.Add(tempPicture);
                this.Controls.Add(tempPicture);
            }
        }

        public void UpdatePIOStatus()
        {
            for (int i = 0; i < inputList.Count; i++)
                inputStatusList[i].BackColor = pio.GetPIOStatueByTag(inputList[i]) ? Color.Green : Color.Transparent;

            for (int i = 0; i < outputList.Count; i++)
                outputStatusList[i].BackColor = pio.GetPIOStatueByTag(outputList[i]) ? Color.Green : Color.Transparent;
        }

        private void outputPicture_MouseDown(object sender, MouseEventArgs e)
        {
            pio.PIOTestOnOff(((PictureBox)sender).Name, true);
        }

        private void outputPicture_MouseUp(object sender, MouseEventArgs e)
        {
            pio.PIOTestOnOff(((PictureBox)sender).Name, false);
        }
    }
}
