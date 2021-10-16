using Mirle.Agv.INX.Configs;
using Mirle.Agv.INX.Control;
using Mirle.Agv.INX.Model.Configs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirle.Agv.INX.Model
{
    public class MIPCControlData
    {
        public bool StartProcessReceiveData { get; set; } = false;

        public bool U動力電 { get; set; } = true;

        public MIPCConfig Config { get; set; }
        public float[] MIPCTestArray { get; set; } = new float[30];

        public Dictionary<string, MIPCData> AllDataByMIPCTagName { get; set; }
        public Dictionary<string, MIPCData> AllDataByIPCTagName { get; set; }
        public bool Start
        {
            get
            {
                return StartButton_Front || StartButton_Back;
            }
        }

        public bool StartButton_Front { get; set; } = false;
        public bool StartButton_Back { get; set; } = false;

        public bool ButtonPause
        {
            get
            {
                if (ButtonPause_Front || ButtonPause_Back)
                    return true;
                else
                    return false;
            }
        }

        public bool ButtonPause_Front { get; set; } = false;
        public bool ButtonPause_Back { get; set; } = false;

        public bool BreakRelease
        {
            get
            {
                return BreakRelease_Front || BreakRelease_Back;
            }
        }

        public bool BreakRelease_Front { get; set; } = false;
        public bool BreakRelease_Back { get; set; } = false;

        public float GetDataByMIPCTagName(string tagName)
        {
            if (AllDataByMIPCTagName.ContainsKey(tagName) && AllDataByMIPCTagName[tagName].Object != null)
                return (float)AllDataByMIPCTagName[tagName].Object;
            else
                return -1;
        }

        public float GetDataByIPCTagName(string tagName)
        {
            if (AllDataByIPCTagName.ContainsKey(tagName) && AllDataByIPCTagName[tagName].Object != null)
                return (float)AllDataByIPCTagName[tagName].Object;
            else
                return -1;
        }

        public bool CanLeftCharging { get; set; } = false;
        public bool CanRightCharging { get; set; } = false;
        public PIOFlow LeftChargingPIO { get; set; } = null;
        public PIOFlow RightChargingPIO { get; set; } = null;
        public bool Charging
        {
            get
            {
                if (CanLeftCharging && ((PIOFlow_Charging)LeftChargingPIO).Charging)
                    return true;
                else if (CanRightCharging && ((PIOFlow_Charging)RightChargingPIO).Charging)
                    return true;
                else
                    return false;
            }
        }

        public Dictionary<EnumSafetyLevel, int> AllByPassLevel { get; set; } = new Dictionary<EnumSafetyLevel, int>();

        public EnumSafetyLevel safetySensorStatus { get; set; } = EnumSafetyLevel.Normal;      // 原始資料,不包含Delay內容.
        private Stopwatch areaSensorTimer = new Stopwatch();                                                // Delay用Timer.
        private bool safetySensorDelaying = false;

        private EnumSafetyLevel safetySensorStopType = EnumSafetyLevel.Normal;

        public EnumSafetyLevel SafetySensorStatus                                                      // 移動控制取得資料.
        {
            get
            {
                if (safetySensorDelaying)
                {
                    if (areaSensorTimer.ElapsedMilliseconds > LocalData.Instance.MoveControlData.MoveControlConfig.TimeValueConfig.DelayTimeList[EnumDelayTimeType.SafetySensorStartDelayTime])
                    {
                        safetySensorDelaying = false;
                        areaSensorTimer.Stop();
                        return safetySensorStatus;
                    }
                    else
                        return safetySensorStopType;
                }
                else
                    return safetySensorStatus;
            }

            set
            {
                if (value != safetySensorStatus)
                {
                    if ((int)value >= (int)EnumSafetyLevel.SlowStop)
                    {   // 變成Stop.
                        safetySensorDelaying = false;
                        areaSensorTimer.Stop();
                    }
                    else if ((int)safetySensorStatus >= (int)EnumSafetyLevel.SlowStop && (int)value < (int)EnumSafetyLevel.SlowStop)
                    {
                        safetySensorStopType = safetySensorStatus;
                        areaSensorTimer.Restart();
                        safetySensorDelaying = true;
                    }

                    safetySensorStatus = value;
                }
            }
        }

        public EnumMovingDirection MoveControlDirection { get; set; } = EnumMovingDirection.None;

        public EnumMovingDirection AreaSensorDirection { get; set; } = EnumMovingDirection.None;

        public bool 方向Bypass { get; set; } = false;

        public List<string> ChangeAreaSensorDirectionTagList { get; set; } = new List<string>();
        public List<float> ChangeAreaSensorDirectionValueList { get; set; } = new List<float>();

        public bool BypassFront { get; set; } = false;
        public bool BypassBack { get; set; } = false;
        public bool BypassLeft { get; set; } = false;
        public bool BypassRight { get; set; } = false;

        public bool BypassIO { get; set; } = false;

        public EnumBuzzerType MoveControlBuzzerType { get; set; } = EnumBuzzerType.None;
        public EnumDirectionLight MoveControlDirectionLight { get; set; } = EnumDirectionLight.None;

        public EnumBuzzerType BuzzerType
        {
            get
            {
                if (LocalData.Instance.MoveControlData.MotionControlData.JoystickMode || LocalData.Instance.MoveControlData.SpecialFlow)
                    return EnumBuzzerType.Moving;
                else if (LocalData.Instance.LoadUnloadData.LoadUnloadCommand != null)
                    return EnumBuzzerType.LoadUnload;
                else if (LocalData.Instance.MIPCData.Charging)
                    return EnumBuzzerType.Charging;
                else
                {
                    if (HasAlarm && !BuzzOff && !LocalData.Instance.LoadUnloadData.Homing)
                        return EnumBuzzerType.Alarm;
                    else
                        return MoveControlBuzzerType;
                }
            }
        }

        public bool HasAlarm { get; set; } = false;
        public bool HasWarn { get; set; } = false;
        public bool BuzzOff { get; set; } = false;

        public EnumDirectionLight DirectionLight
        {
            get
            {
                return MoveControlDirectionLight;
            }
        }

        public bool NeedSendHeartbeat { get; set; } = true;
        public bool MotionAlarm { get; set; } = false;

        public string ChargingMessage { get; set; } = "";

        private int chargingMessageMaxLength = 10000;
        public int LocalChargingCount = 1;

        public string AddChargingMessage
        {
            set
            {
                ChargingMessage = String.Concat("第", LocalChargingCount.ToString(), "次充電 ", value, "\r\n", ChargingMessage);

                if (ChargingMessage.Length > chargingMessageMaxLength)
                    ChargingMessage = ChargingMessage.Substring(0, chargingMessageMaxLength);
            }
        }

    }
}
