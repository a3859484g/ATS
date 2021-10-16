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
    public class SafetySensor_Tim781 : SafetySensor
    {
        public override void Initial(MIPCControlHandler mipcControl, AlarmHandler alarmHandler, SafetySensorData config)
        {
            this.alarmHandler = alarmHandler;
            this.mipcControl = mipcControl;
            Config = config;
            device = MethodInfo.GetCurrentMethod().ReflectedType.Name;
        }

        public override bool ChangeMovingDirection(EnumMovingDirection newDirection)
        {
            bool returnResult = true;

            if (Config.Type == EnumSafetySensorType.AreaSensor && newDirection != movingDirection)
            {
                if (Config.AreaSensorChangeDircetionInputIOList.ContainsKey(newDirection))
                {
                    if (mipcControl.SendMIPCDataByMIPCTagName(Config.MIPCTagNameOutput, Config.AreaSensorChangeDircetionInputIOList[newDirection]))
                    {
                        movingDirection = newDirection;
                    }
                    else
                    {
                        WriteLog(7, "", String.Concat("Device : ", Config.Device, " 切換 ", newDirection.ToString(), " 失敗"));
                        returnResult = false;
                    }
                }
                else
                    WriteLog(3, "", String.Concat("Device : ", Config.Device, " Config 中並未定義移動方向 : ", newDirection.ToString(), " 的切換方式"));
            }

            return returnResult;
        }

        public override void ChangeMovingDirection_AddList(EnumMovingDirection newDirection)
        {
            if (Config.Type == EnumSafetySensorType.AreaSensor && newDirection != movingDirection && Config.AreaSensorChangeDircetionInputIOList.ContainsKey(newDirection))
            {
                for (int i = 0; i < Config.MIPCTagNameOutput.Count; i++)
                {
                    localData.MIPCData.ChangeAreaSensorDirectionTagList.Add(Config.MIPCTagNameOutput[i]);
                    localData.MIPCData.ChangeAreaSensorDirectionValueList.Add(Config.AreaSensorChangeDircetionInputIOList[newDirection][i]);
                }
            }
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
                    WriteLog(7, "", String.Concat(Config.Device, " : ", ((EnumSafetyLevel)i).ToString(), " Change to ", (newBit != 0 ? "On" : "Off")));

                    if (newBit != 0)
                    {
                        switch ((EnumSafetyLevel)i)
                        {
                            case EnumSafetyLevel.Alarm:
                                SetAlarmCode(EnumMIPCControlErrorCode.AreaSensorAlarm);
                                break;
                            case EnumSafetyLevel.EMO:
                                SetAlarmCode(EnumMIPCControlErrorCode.AreaSensor觸發);
                                break;
                            case EnumSafetyLevel.IPCEMO:
                                SetAlarmCode(EnumMIPCControlErrorCode.AreaSensor觸發);
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        ResetAlarmCode(EnumMIPCControlErrorCode.AreaSensor觸發);
                        ResetAlarmCode(EnumMIPCControlErrorCode.AreaSensorAlarm);
                    }
                }
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
