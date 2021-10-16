using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class Vector3
    {
        public float X { get; set; } = 0;
        public float Y { get; set; } = 0;
        public float Z { get; set; } = 0;

        public Vector3()
        {
        }

        public Vector3(byte[] dataArray, int index)
        {
            if (index + 4 < dataArray.Length)
                X = BitConverter.ToSingle(dataArray, index);

            if (index + 8 < dataArray.Length)
                Y = BitConverter.ToSingle(dataArray, index + 4);

            if (index + 12 < dataArray.Length)
                Z = BitConverter.ToSingle(dataArray, index + 8);
        }
    }
}
