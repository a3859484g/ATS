using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.mirle.aka.sc.ProtocolFormat.agvMessage ;
using Mirle.Agv.MiddlePackage.Umtc.Tools;
using Newtonsoft.Json;

namespace Mirle.Agv.MiddlePackage.Umtc.Model
{
    public class MovingGuide
    {
        private List<string> _GuideSectionIds = new List<string>(); //0408liu 補漏掉的134
        private List<string> _GuideSectionIdsForReserveReport = new List<string>();

        public object LockGuideSectionIdsForReserveReport = new object(); // 0412 ChiaWei 新增用來Assign和List Remove用.

        public List<string> GuideSectionIds
        {
            get
            {
                return _GuideSectionIds;
            }

            set
            {
                _GuideSectionIds = value;
            }
        }
        public List<string> GuideSectionIdsForReserveReport
        {
            get
            {
                return _GuideSectionIdsForReserveReport;
            }

            set
            {
            }
        }
        public List<string> GuideAddressIds { get; set; } = new List<string>();
        public string FromAddressId { get; set; } = "";
        public string ToAddressId { get; set; } = "";
        public uint GuideDistance { get; set; } = 0;
        public VhStopSingle ReserveStop { get; set; } = VhStopSingle.Off;
        public List<MapSection> MovingSections { get; set; } = new List<MapSection>();
        public int MovingSectionsIndex { get; set; } = 0;
        public ushort SeqNum { get; set; }
        public EnumMoveComplete MoveComplete { get; set; } = EnumMoveComplete.Fail;
        public bool IsAvoidMove { get; set; } = false;
        public bool IsAvoidComplete { get; set; } = false;
        public bool IsOverrideMove { get; set; } = false;

        public MovingGuide() { }

        public MovingGuide(MovingGuide movingGuide)
        {
            this.GuideSectionIds = movingGuide.GuideSectionIds;
            this.GuideAddressIds = movingGuide.GuideAddressIds;
            this.FromAddressId = movingGuide.FromAddressId;
            this.ToAddressId = movingGuide.ToAddressId;
            this.GuideDistance = movingGuide.GuideDistance;
            this.ReserveStop = movingGuide.ReserveStop;
            this.IsAvoidComplete = movingGuide.IsAvoidComplete;
            this.MovingSections = movingGuide.MovingSections;
            this.MovingSectionsIndex = movingGuide.MovingSectionsIndex;
            this.SeqNum = movingGuide.SeqNum;
            this.IsAvoidMove = movingGuide.IsAvoidMove;
        }

        //public string GetJsonInfo()
        //{
        //    return $"[GuideSectionIds={GuideSectionIds.GetJsonInfo()}]\r\n" +
        //           $"[GuideAddressIds={GuideAddressIds.GetJsonInfo()}]\r\n" +
        //           $"[FromAddressId={FromAddressId}]\r\n" +
        //           $"[ToAddressId={ToAddressId}]\r\n" +
        //           $"[ReserveStop={ReserveStop}]\r\n" +
        //           $"[MovingSections={MovingSections.Count}]\r\n" +
        //           $"[SeqNum={SeqNum}]\r\n" +
        //           $"[CommandId={CommandId}]\r\n" +
        //           $"[MoveComplete ={MoveComplete}]\r\n" +
        //           $"[IsAvoidComplete ={IsAvoidComplete}]\r\n" +
        //           $"[IsAvoidMove ={IsAvoidMove}]\r\n" +
        //           $"[IsOverrideMove ={IsOverrideMove}]\r\n";
        //}
    }
}
