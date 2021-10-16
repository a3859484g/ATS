using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mirle.Agv.INX.Model;
using Mirle.Agv.INX.Controller;
using System.Threading;

namespace Mirle.Agv
{
    public partial class JogPitchLocateData : UserControl
    {
        LocateDriver driver;
        private int index = -1;
        private LocalData localData = LocalData.Instance;

        public event EventHandler ClickEvent;

        public JogPitchLocateData(LocateDriver driver)
        {
            this.driver = driver;
            InitializeComponent();
            label_DriverNameValue.Text = driver.DriverConfig.Device;

            if (((LocateDriver)driver).LocateType == EnumLocateType.SLAM)
                label_SpecialTurn.Text = "信心度 : ";
            else
            {
                switch (driver.DriverConfig.LocateDriverType)
                {
                    case EnumLocateDriverType.BarcodeMapSystem:
                        label_SpecialTurn.Text = "Barcode角度 : ";
                        break;
                    default:
                        button_ChangeType.Visible = false;
                        label_SpecialTurn.Text = "";
                        break;
                }
            }
        }

        private void object_Click(object sender, EventArgs e)
        {
            ClickEvnetFunction();
        }

        private void ClickEvnetFunction()
        {
            ClickEvent?.Invoke(this, null);
        }


        private string GetStringByTag(string tagString)
        {
            return localData.GetProfaceString(localData.Language, tagString);
        }

        private string GetStringByTag(EnumProfaceStringTag tag)
        {
            return localData.GetProfaceString(localData.Language, tag.ToString());
        }

        public void ChangeNameByLanguage()
        {
            label_DriverName.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.DriverName), " : ");
            label_DriverStatus.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.Status), " : ");
            label_DataType.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.DataType), " : ");

            if (((LocateDriver)driver).LocateType == EnumLocateType.SLAM)
                label_SpecialTurn.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.信心度), " : ");
            else
            {
                switch (driver.DriverConfig.LocateDriverType)
                {
                    case EnumLocateDriverType.BarcodeMapSystem:
                        label_SpecialTurn.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.Barcode角度), " : ");
                        break;
                    default:
                        label_SpecialTurn.Text = "";
                        break;
                }
            }

            label_MapX.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.Map_X), " : ");
            label_MapY.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.Map_Y), " : ");
            label_MapTheta.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.Map_Theta), " : ");
            label_Trigger.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.Trigger), " : ");
            button_ChangeType.Text = (index == -1) ? GetStringByTag(EnumProfaceStringTag.轉換後座標) : GetStringByTag(EnumProfaceStringTag.原始座標);
        }

        public void UpdateData(bool triggerCanUse)
        {
            if (driver == null)
                return;

            if (driver.PollingOnOff)
            {
                label_TriggerValue.Text = GetStringByTag(EnumProfaceStringTag.On);
                label_TriggerValue.ForeColor = Color.Green;
                button_TriggerOnOff.Text = GetStringByTag(EnumProfaceStringTag.關閉);
                driver.PollingOnOff = true;
            }
            else
            {
                label_TriggerValue.Text = GetStringByTag(EnumProfaceStringTag.Off);
                label_TriggerValue.ForeColor = Color.Red;
                button_TriggerOnOff.Text = GetStringByTag(EnumProfaceStringTag.開啟);
                driver.PollingOnOff = false;
            }

            EnumControlStatus status = driver.Status;
            label_DriverStatusValue.Text = GetStringByTag(status.ToString());

            switch (status)
            {
                case EnumControlStatus.Initial:
                case EnumControlStatus.Error:
                case EnumControlStatus.NotReady:
                    label_DriverStatusValue.ForeColor = Color.Red;
                    break;
                case EnumControlStatus.ResetAlarm:
                    label_DriverStatusValue.ForeColor = Color.Yellow;
                    break;
                case EnumControlStatus.Ready:
                    label_DriverStatusValue.ForeColor = Color.Green;
                    break;
                default:
                    label_DriverStatusValue.ForeColor = Color.DarkRed;
                    break;
            }

            button_TriggerOnOff.Enabled = triggerCanUse;

            LocateAGVPosition locateAGVPosition = null;

            if (index == -1)
                locateAGVPosition = driver.GetLocateAGVPosition;
            else
            {
                if (((LocateDriver)driver).LocateType == EnumLocateType.SLAM)
                    locateAGVPosition = ((LocateDriver_SLAM)driver).GetOriginAGVPosition;
                else
                {
                    switch (driver.DriverConfig.LocateDriverType)
                    {
                        case EnumLocateDriverType.BarcodeMapSystem:
                            locateAGVPosition = ((LocateDriver_BarcodeMapSystem)driver).GetLocateAGVPositionByIndex(index);
                            break;
                        default:
                            locateAGVPosition = driver.GetLocateAGVPosition;
                            break;
                    }
                }
            }

            if (locateAGVPosition != null)
            {
                if (((LocateDriver)driver).LocateType == EnumLocateType.SLAM)
                    label_SpecialTurnValue.ForeColor = Color.Green;

                label_SpecialTurnValue.Text = String.Concat(locateAGVPosition.Value.ToString("0"), " ", locateAGVPosition.Status);
                label_DataTypeValue.Text = locateAGVPosition.Type.ToString();

                if (locateAGVPosition.AGVPosition != null)
                {
                    label_MapXValue.Text = locateAGVPosition.AGVPosition.Position.X.ToString("0.0");
                    label_MapYValue.Text = locateAGVPosition.AGVPosition.Position.Y.ToString("0.0");
                    label_MapThetaValue.Text = locateAGVPosition.AGVPosition.Angle.ToString("0.0");
                }
                else
                {
                    label_MapXValue.Text = "";
                    label_MapYValue.Text = "";
                    label_MapThetaValue.Text = "";
                }
            }
            else
            {
                if (((LocateDriver)driver).LocateType == EnumLocateType.SLAM)
                {
                    label_SpecialTurnValue.ForeColor = Color.Red;
                    locateAGVPosition = ((LocateDriver_SLAM)driver).GetOriginAGVPosition;
                }

                if (locateAGVPosition != null)
                    label_SpecialTurnValue.Text = String.Concat(locateAGVPosition.Value.ToString("0"), " ", locateAGVPosition.Status);
                else
                    label_SpecialTurnValue.Text = "";

                label_DataTypeValue.Text = "";
                label_MapXValue.Text = "";
                label_MapYValue.Text = "";
                label_MapThetaValue.Text = "";
            }
        }

        private void button_TriggerOnOff_Click(object sender, EventArgs e)
        {
            ClickEvnetFunction();
            button_TriggerOnOff.Enabled = false;

            if (driver.PollingOnOff)
            {
                label_TriggerValue.Text = GetStringByTag(EnumProfaceStringTag.Off);
                label_TriggerValue.ForeColor = Color.Red;
                button_TriggerOnOff.Text = GetStringByTag(EnumProfaceStringTag.開啟);
                driver.PollingOnOff = false;
            }
            else
            {
                label_TriggerValue.Text = GetStringByTag(EnumProfaceStringTag.On);
                label_TriggerValue.ForeColor = Color.Green;
                button_TriggerOnOff.Text = GetStringByTag(EnumProfaceStringTag.關閉);
                driver.PollingOnOff = true;
            }

            button_TriggerOnOff.Enabled = true;
        }

        private void button_ChangeType_Click(object sender, EventArgs e)
        {
            ClickEvnetFunction();
            button_ChangeType.Enabled = false;

            if (driver.LocateType == EnumLocateType.SLAM)
            {
                if (index != 0)
                    index = 0;
                else
                    index = -1;

                button_ChangeType.Text = (index == -1) ? GetStringByTag(EnumProfaceStringTag.轉換後座標) : GetStringByTag(EnumProfaceStringTag.原始座標);
                button_ChangeType.BackColor = (index == -1) ? Color.Transparent : Color.Red;
                button_ChangeType.ForeColor = (index == -1) ? Color.Black : Color.White;
            }
            else if (driver.DriverConfig.LocateDriverType == EnumLocateDriverType.BarcodeMapSystem)
            {
                index++;

                if (index == ((LocateDriver_BarcodeMapSystem)driver).BarcodeReaderList.Count)
                    index = -1;

                button_ChangeType.Text = (index == -1) ? GetStringByTag(EnumProfaceStringTag.最後輸出) : index.ToString();
                button_ChangeType.BackColor = (index == -1) ? Color.Transparent : Color.Red;
            }

            button_ChangeType.Enabled = true;
        }
    }
}