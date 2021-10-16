using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class LPMSData
    {
        public DateTime Time { get; set; } = DateTime.Now;
        public float Timestamp { get; set; } = 0;
        public Vector3 Gyroscope { get; set; } = new Vector3();
        public Vector3 Acceleromete { get; set; } = new Vector3();
        public Vector3 Magnetometer { get; set; } = new Vector3();
        public Vector4 Orientation { get; set; } = new Vector4();
        public Vector3 EulerAngle { get; set; } = new Vector3();
        public Vector3 LinearAccelerationa { get; set; } = new Vector3();
    }
}
