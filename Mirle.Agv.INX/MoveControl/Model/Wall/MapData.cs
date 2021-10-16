using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class MapData
    {
        public double MinX { get; set; }
        public double MaxX { get; set; }
        public double MinY { get; set; }
        public double MaxY { get; set; }

        private bool first = true;

        private double mapBorderLength;
        private double mapScale;

        public void UpdateMaxMin(MapPosition address)
        {
            if (first)
            {
                first = false;
                MinX = address.X;
                MaxX = address.X;
                MinY = address.Y;
                MaxY = address.Y;
            }
            else
            {
                if (address.X > MaxX)
                    MaxX = address.X;
                else if (address.X < MinX)
                    MinX = address.X;

                if (address.Y > MaxY)
                    MaxY = address.Y;
                else if (address.Y < MinY)
                    MinY = address.Y;
            }
        }

        public void SettingConfig(double mapBorderLength, double mapScale)
        {
            this.mapBorderLength = mapBorderLength;
            this.mapScale = mapScale;

            MaxX += mapBorderLength;
            MaxY += mapBorderLength;
            MinX -= mapBorderLength;
            MinY -= mapBorderLength;
        }

        public double TransferX(double x)
        {
            return (x - MinX) * mapScale;
        }

        public double TransferY(double y)
        {
            return (y - MinY) * mapScale;
        }

        public double AntiTransferX(double x)
        {
            if (mapScale == 0)
                return 0;
            else
                return x / mapScale + MinX;
        }

        public double AntiTransferY(double y)
        {
            if (mapScale == 0)
                return 0;
            else
                return y / mapScale + MinY;
        }
    }
}
