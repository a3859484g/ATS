using Mirle.Agv.INX.Controller;
using Mirle.Agv.INX.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Control
{
    public class SafetySensor_Bumper : SafetySensor
    {
        public override void Initial(MIPCControlHandler mipcControl, AlarmHandler alarmHandler, SafetySensorData config)
        {
            this.alarmHandler = alarmHandler;
            this.mipcControl = mipcControl;
            Config = config;
            device = MethodInfo.GetCurrentMethod().ReflectedType.Name;
        }

        public override void UpdateStatus()
        {
            uint newStatus = Status;

            foreach (var temp in Config.MIPCInputTagNameToBit)
            {
                if (localData.MIPCData.GetDataByMIPCTagName(temp.Key) == -1)
                    return;
                else if ((localData.MIPCData.GetDataByMIPCTagName(temp.Key) == 0 && Config.MIPCInputTagNameToAB[temp.Key]) ||
                         (localData.MIPCData.GetDataByMIPCTagName(temp.Key) != 0 && !Config.MIPCInputTagNameToAB[temp.Key]))
                    newStatus = newStatus & (maxStatusValue - (uint)(1 << temp.Value));
                else
                    newStatus = newStatus | ((uint)(1 << temp.Value));
            }

            foreach (EnumSafetyLevel level in localData.MIPCData.AllByPassLevel.Keys)
            {
                newStatus = newStatus & (maxStatusValue - (uint)(1 << (int)level));
            }

            if (ByPassAlarm == 1)
                newStatus = newStatus & (1 << (int)EnumSafetyLevel.Alarm);

            if (ByPassStatus == 1)
                newStatus = newStatus & 0b111000000;

            uint oldBit;
            uint newBit;

            for (int i = safetyLevelCount - 1; i >= 0; i--)
            {
                oldBit = (uint)(Status & (1 << i));
                newBit = (uint)(newStatus & (1 << i));

                if (oldBit != newBit)
                {
                    WriteLog(7, "", String.Concat(((EnumSafetyLevel)i).ToString(), " Change to ", (newBit != 0 ? "On" : "Off")));

                    if (newBit != 0)
                    {
                        switch ((EnumSafetyLevel)i)
                        {
                            case EnumSafetyLevel.EMO:
                                SetAlarmCode(EnumMIPCControlErrorCode.Bumper觸發);
                                break;
                            case EnumSafetyLevel.IPCEMO:
                                SetAlarmCode(EnumMIPCControlErrorCode.Bumper觸發);
                                break;
                            default:
                                SetAlarmCode(EnumMIPCControlErrorCode.Bumper觸發);
                                break;
                        }
                    }
                    else
                        ResetAlarmCode(EnumMIPCControlErrorCode.Bumper觸發);
                }
            }

            switch (localData.MainFlowConfig.AGVType)
            {
                case EnumAGVType.AGC:
                    if (localData.MoveControlData.MotionControlData.JoystickMode && newStatus != 0)
                    {
                        if (!mipcControl.SetMIPCReady(false))
                            WriteLog(1, "", "發送斷電失敗");
                    }

                    break;
                case EnumAGVType.UMTC:
                    //if (newStatus != 0)
                    //{
                    //    if (!mipcControl.SetMIPCReady(false))
                    //        WriteLog(1, "", "發送斷電失敗");
                    //}
                    break;
                default:
                    break;
            }

            if ((newStatus & (1 << ((int)EnumSafetyLevel.IPCEMO))) != 0)
            {
                if (!mipcControl.SetMIPCReady(false))
                    WriteLog(1, "", "發送斷電失敗");
            }

            Status = newStatus;
        }
    }
}
