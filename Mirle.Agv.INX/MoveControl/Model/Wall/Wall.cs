using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class Wall
    {
        public string ID { get; set; }
        public MapPosition Start { get; set; }
        public MapPosition End { get; set; }
        public double Theta { get; set; }
        public double SinTheta { get; set; }
        public double CosTheta { get; set; }
        public double Distance { get; set; }
        public double WallLength { get; set; }

        public double TimeInterval { get; set; }
        public Stopwatch Timer { get; set; }

        public Wall(string id, MapPosition start, MapPosition end, double theta, double distance, double sleepTime)
        {
            ID = id;
            Distance = distance;
            WallLength = Math.Sqrt(Math.Pow(start.X - end.X, 2) + Math.Pow(start.Y - end.Y, 2));
            Start = start;
            End = end;
            Theta = theta;
            SinTheta = Math.Sin(theta * Math.PI / 180);
            CosTheta = Math.Cos(theta * Math.PI / 180);
            Timer = new Stopwatch();
            Timer.Restart();
            TimeInterval = sleepTime;
        }
    }
}
