
using System;

namespace Mirle.Agv.INX.Model
{
    [Serializable]
    public class VehicleLocation
    {
        public string NowSection { get; set; } = "";
        public double DistanceFormSectionHead { get; set; } = 0;
        public double RealDistanceFormSectionHead { get; set; } = 0;

        public string LastAddress { get; set; } = "";
        public bool InAddress { get; set; } = false;

        public bool InSection { get; set; } = true;
        //public MapAGVPosition Fake { get; set; } = null;
    }
}