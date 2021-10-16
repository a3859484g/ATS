using System;

namespace Mirle.Agv.MiddlePackage.Umtc.Model
{
    public class AutoStateArgs : EventArgs
    {
        public EnumAutoState AutoState { get; set; } = EnumAutoState.None;
    }
}
