using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mirle.Agv.INX.Controller;
using Mirle.Agv.INX.Model;

namespace Mirle.Agv.INX.View
{
    public partial class LPMSDataView : UserControl
    {
        private LPMS lpms = null;
        private LocalData localData = LocalData.Instance;

        public event EventHandler ClickEvent;

        public LPMSDataView(LPMS lpms)
        {
            this.lpms = lpms;
            InitializeComponent();
        }

        private string GetStringByTag(EnumProfaceStringTag tag)
        {
            return localData.GetProfaceString(localData.Language, tag.ToString());
        }

        public void ChangeLanguage()
        {
            label_Gyroscope.Text = GetStringByTag(EnumProfaceStringTag.Gyroscope);
            label_Acceleromete.Text = GetStringByTag(EnumProfaceStringTag.Acceleromete);
            label_Magnetometer.Text = GetStringByTag(EnumProfaceStringTag.Magnetometer);
            label_Orientation.Text = GetStringByTag(EnumProfaceStringTag.Orientation);
            label_EulerAngle.Text = GetStringByTag(EnumProfaceStringTag.EulerAngle);
            label_LinearAccelerationa.Text = GetStringByTag(EnumProfaceStringTag.LinearAccelerationa);
        }

        private void SetEmpty()
        {
            label_GyroscopeX.Text = "";
            label_GyroscopeY.Text = "";
            label_GyroscopeZ.Text = "";
            label_AccelerometeX.Text = "";
            label_AccelerometeY.Text = "";
            label_AccelerometeZ.Text = "";
            label_MagnetometerX.Text = "";
            label_MagnetometerY.Text = "";
            label_MagnetometerZ.Text = "";
            label_OrientationX.Text = "";
            label_OrientationY.Text = "";
            label_OrientationZ.Text = "";
            label_OrientationW.Text = "";
            label_EulerAngleX.Text = "";
            label_EulerAngleY.Text = "";
            label_EulerAngleZ.Text = "";
            label_LinearAccelerationaX.Text = "";
            label_LinearAccelerationaY.Text = "";
            label_LinearAccelerationaZ.Text = "";
        }

        public void UpdateView()
        {
            if (lpms != null)
            {
                LPMSData lpmsData = lpms.LPMSData;

                if (lpmsData != null)
                {
                    label_GyroscopeX.Text = lpmsData.Gyroscope.X.ToString("0.000");
                    label_GyroscopeY.Text = lpmsData.Gyroscope.Y.ToString("0.000");
                    label_GyroscopeZ.Text = lpmsData.Gyroscope.Z.ToString("0.000");

                    label_AccelerometeX.Text = lpmsData.Acceleromete.X.ToString("0.000");
                    label_AccelerometeY.Text = lpmsData.Acceleromete.Y.ToString("0.000");
                    label_AccelerometeZ.Text = lpmsData.Acceleromete.Z.ToString("0.000");

                    label_MagnetometerX.Text = lpmsData.Magnetometer.X.ToString("0.000");
                    label_MagnetometerY.Text = lpmsData.Magnetometer.Y.ToString("0.000");
                    label_MagnetometerZ.Text = lpmsData.Magnetometer.Z.ToString("0.000");

                    label_OrientationX.Text = lpmsData.Orientation.X.ToString("0.000");
                    label_OrientationY.Text = lpmsData.Orientation.Y.ToString("0.000");
                    label_OrientationZ.Text = lpmsData.Orientation.Z.ToString("0.000");
                    label_OrientationW.Text = lpmsData.Orientation.W.ToString("0.000");

                    label_EulerAngleX.Text = lpmsData.EulerAngle.X.ToString("0.000");
                    label_EulerAngleY.Text = lpmsData.EulerAngle.Y.ToString("0.000");
                    label_EulerAngleZ.Text = lpmsData.EulerAngle.Z.ToString("0.000");

                    label_LinearAccelerationaX.Text = lpmsData.LinearAccelerationa.X.ToString("0.000");
                    label_LinearAccelerationaY.Text = lpmsData.LinearAccelerationa.Y.ToString("0.000");
                    label_LinearAccelerationaZ.Text = lpmsData.LinearAccelerationa.Z.ToString("0.000");
                }
                else
                    SetEmpty();
            }
            else
                SetEmpty();
        }

        private void label_Click(object sender, EventArgs e)
        {
            ClickEvent?.Invoke(this, null);
        }
    }
}
