using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class LocateAGVPosition
    {
        public uint Count { get; set; } = 0;
        public MapAGVPosition AGVPosition { get; set; } = new MapAGVPosition();
        public DateTime GetDataTime { get; set; }
        public double ScanTime { get; set; } = 0;
        public double Value { get; set; } = 0;
        public EnumAGVPositionType Type { get; set; }
        public string Device { get; set; }
        public int Order { get; set; }
        public string Tag { get; set; } = "";
        public string Status { get; set; } = "";

        public LocateAGVPosition(LocateAGVPosition old)
        {
            Status = old.Status;
            Tag = old.Tag;
            Order = old.Order;
            Device = old.Device;
            Count = old.Count;
            Value = old.Value;
            Type = old.Type;
            ScanTime = old.ScanTime;
            GetDataTime = old.GetDataTime;
            AGVPosition = new MapAGVPosition(old.AGVPosition);
        }

        public LocateAGVPosition(MapPosition position, double theta, double value, double scanTime, DateTime getDataTime, uint count, EnumAGVPositionType type, string device, int oreder)
        {
            if (position != null)
                AGVPosition = new MapAGVPosition(position, theta);
            else
                AGVPosition = null;

            Value = value;
            ScanTime = scanTime;
            GetDataTime = getDataTime;
            Count = count;
            Type = type;
            Device = device;
            Order = oreder;
        }

        public LocateAGVPosition(MapAGVPosition agvPosition, double value, double scanTime, DateTime getDataTime, uint count, EnumAGVPositionType type, string device, int oreder)
        {
            AGVPosition = agvPosition;
            Value = value;
            ScanTime = scanTime;
            GetDataTime = getDataTime;
            Count = count;
            Type = type;
            Device = device;
            Order = oreder;
        }

        public LocateAGVPosition()
        {
        }
    }
}
