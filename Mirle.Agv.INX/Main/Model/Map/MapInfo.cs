using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    [Serializable]
    public class MapInfo
    {
        public Dictionary<string, MapAddress> AllAddress = new Dictionary<string, MapAddress>();
        public Dictionary<string, MapSection> AllSection = new Dictionary<string, MapSection>();

        public bool IsPort(string addressID)
        {
            return AllAddress.ContainsKey(addressID) && AllAddress[addressID].LoadUnloadDirection != EnumStageDirection.None;
        }

        public bool IsChargingStation(string addressID)
        {
            return AllAddress.ContainsKey(addressID) && AllAddress[addressID].ChargingDirection != EnumStageDirection.None;
        }

        public bool IsPortOrChargingStation(string addressID)
        {
            return AllAddress.ContainsKey(addressID) && (AllAddress[addressID].LoadUnloadDirection != EnumStageDirection.None || AllAddress[addressID].ChargingDirection != EnumStageDirection.None);
        }
    }
}
