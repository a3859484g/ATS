namespace Mirle.Agv.MiddlePackage.Umtc.Model.TransferSteps
{
    public class MoveCmdInfo : TransferStep
    {
        public MapAddress EndAddress { get; set; } = new MapAddress();

        public MoveCmdInfo(MapAddress endAddress, string cmdId) : base(cmdId)
        {
            type = EnumTransferStepType.Move;
            this.EndAddress = endAddress;
        }
    }

    public class MoveToChargerCmdInfo : MoveCmdInfo
    {
        public MoveToChargerCmdInfo(MapAddress endAddress, string cmdId) : base(endAddress, cmdId)
        {
            type = EnumTransferStepType.MoveToCharger;
        }
    }
}
