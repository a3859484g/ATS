using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mirle.Agv.INX.Properties;
using Mirle.Agv.INX.Model;

namespace Mirle.Agv.INX.View
{
    public partial class UcVehicleImage : UserControl
    {
        private float deltaX;
        private float deltaY;
        private DrawMapData drawMapData;

        public UcVehicleImage(DrawMapData drawMapData)
        {
            InitializeComponent();
            picVeh.Image = Resources.VehHasNoCarrier;
            deltaX = this.Size.Width / 2;
            deltaY = this.Size.Height / 2;
            this.drawMapData = drawMapData;
        }

        public PictureBox GetAGVPictureBox
        {
            get
            {
                return picVeh;
            }
        }

        public void UpdateLocate(MapAGVPosition agvPosition)
        {
            if (agvPosition == null)
                this.Visible = false;
            else
            {
                this.Visible = true;
                this.Location = new Point((int)(drawMapData.TransferX(agvPosition.Position.X) - deltaX), (int)(drawMapData.TransferY(agvPosition.Position.Y) - deltaX));
            }
        }
    }
}
