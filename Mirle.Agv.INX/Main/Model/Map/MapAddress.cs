using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    [Serializable]
    public class MapAddress
    {
        public string Id { get; set; } = "FakeAddress";
        public bool Enalbe { get; set; } = true;

        public MapAGVPosition AGVPosition { get; set; } = new MapAGVPosition();

        public string AddressName { get; set; } = "";

        public bool CanSpin { get; set; } = false;
        public string InsideSectionId { get; set; } = "";

        public EnumStageDirection LoadUnloadDirection { get; set; } = EnumStageDirection.None;
        public int StageNumber { get; set; } = 0;
        public bool NeedPIO { get; set; } = true;
        public string RFPIODeviceID { get; set; } = "";
        public string RFPIOChannelID { get; set; } = "";
        public EnumStageDirection ChargingDirection { get; set; } = EnumStageDirection.None;

        public MapSection InsideSection { get; set; } = null;
        public List<MapSection> NearbySection { get; set; } = new List<MapSection>();
    }

}

