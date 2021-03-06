using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.MiddlePackage.Umtc.Model
{

    public class MapPosition
    {
        public double X { get; set; }
        public double Y { get; set; }

        public MapPosition(double x, double y)
        {
            X = x;
            Y = y;
        }

        public MapPosition() : this(double.MinValue, double.MinValue)
        {

        }

        public int MyDistance(MapPosition targetPosition)
        {
            var diffX = Math.Abs(targetPosition.X - X);
            var diffY = Math.Abs(targetPosition.Y - Y);
            return (int)Math.Sqrt(diffX * diffX + diffY * diffY);
        }
    }
}
