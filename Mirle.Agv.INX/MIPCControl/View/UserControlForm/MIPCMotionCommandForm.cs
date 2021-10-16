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
using System.Threading;
using System.IO;

namespace Mirle.Agv.INX.View
{
    public partial class MIPCMotionCommandForm : UserControl
    {
        private LocalData localData = LocalData.Instance;
        private MIPCControlHandler mipcControl;
        private ComputeFunction computeFunction = ComputeFunction.Instance;

        public MIPCMotionCommandForm(MIPCControlHandler mipcControl)
        {
            this.mipcControl = mipcControl;
            InitializeComponent();
        }

        private void button_CommandStart_Click(object sender, EventArgs e)
        {
            //if (localData.MoveControlData.MotionControlData.MoveStatus != EnumAxisMoveStatus.Stop)
            //    return;

            button_CommandStart.Enabled = false;

            if (localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null &&
                !localData.MIPCData.Charging && localData.LoadUnloadData.LoadUnloadCommand == null)
            {
                double mapX;
                double mapY;
                double mapTheta;
                double lineVelocity;
                double lineAcc;
                double lineDec;
                double lineJerk;
                double thetaVelocity;
                double thetaAcc;
                double thetaDec;
                double thetaJerk;

                if (double.TryParse(tB_X.Text, out mapX) && double.TryParse(tB_Y.Text, out mapY) && double.TryParse(tB_Angle.Text, out mapTheta) &&
                    double.TryParse(tB_LineVelocity.Text, out lineVelocity) && double.TryParse(tB_LineAcc.Text, out lineAcc) &&
                     double.TryParse(tB_LineDec.Text, out lineDec) && double.TryParse(tB_LineJerk.Text, out lineJerk) &&
                     double.TryParse(tB_ThetaVelocity.Text, out thetaVelocity) && double.TryParse(tB_ThetaAcc.Text, out thetaAcc) &&
                     double.TryParse(tB_ThetaDec.Text, out thetaDec) && double.TryParse(tB_ThetaJerk.Text, out thetaJerk) &&
                     lineVelocity > 0 && lineAcc > 0 && lineDec > 0 && lineJerk > 0 &&
                     thetaVelocity > 0 && thetaAcc > 0 && thetaDec > 0 && thetaJerk > 0)
                {
                    MapAGVPosition agvPosition = new MapAGVPosition();
                    agvPosition.Position = new MapPosition(mapX, mapY);
                    agvPosition.Angle = mapTheta;

                    mipcControl.AGV_Move(agvPosition, lineVelocity, lineAcc, lineDec, lineJerk, thetaVelocity, thetaAcc, thetaDec, thetaJerk);
                }
                else
                    MessageBox.Show("有資料輸入錯誤");
            }
            else
            {
                MessageBox.Show("Auto 或 半自動移動中 無法使用");
            }

            button_CommandStart.Enabled = true;
        }

        public void UpdateData()
        {
            try
            {
                label_FeedbackNowValue.Text = computeFunction.GetLocateAGVPositionStringWithAngle(localData.MoveControlData.MotionControlData.EncoderAGVPosition);
                label_SLAMLocateValue.Text = computeFunction.GetLocateAGVPositionStringWithAngle(localData.MoveControlData.LocateControlData.LocateAGVPosition);

                if (localData.MoveControlData.MotionControlData.MoveStatus != EnumAxisMoveStatus.Stop)
                {
                    label_MoveStatusValue.Text = "Move";
                    label_MoveStatusValue.ForeColor = Color.Red;
                }
                else
                {
                    label_MoveStatusValue.Text = "Stop";
                    label_MoveStatusValue.ForeColor = Color.Green;
                }
            }
            catch
            {

            }
        }

        private void button_Stop_Click(object sender, EventArgs e)
        {
            double lineDec;
            double lineJerk;
            double thetaDec;
            double thetaJerk;

            if (double.TryParse(tB_LineDec.Text, out lineDec) && lineDec > 0 &&
                double.TryParse(tB_LineJerk.Text, out lineJerk) && lineJerk > 0 &&
                double.TryParse(tB_ThetaDec.Text, out thetaDec) && thetaDec > 0 &&
                double.TryParse(tB_ThetaJerk.Text, out thetaJerk) && thetaJerk > 0)
            {
            }
            else
            {
                lineDec = 200;
                lineJerk = 400;
                thetaDec = 10;
                thetaJerk = 20;
            }

            mipcControl.AGV_Stop(lineDec, lineJerk, thetaDec, thetaJerk);
        }

        private void button_ServoOn_Click(object sender, EventArgs e)
        {
            mipcControl.AGV_ServoOn();
        }

        private void button_ServoOff_Click(object sender, EventArgs e)
        {
            mipcControl.AGV_ServoOff();
        }

        private void button_SetConfig_Click(object sender, EventArgs e)
        {
            string readPath = @"D:\MecanumConfigs\MIPCControl\MIPCMotionConfig.900";

            string localPath = Path.Combine(Environment.CurrentDirectory, "MIPCMotionConfig.900");

            if (File.Exists(localPath))
            {
                if (mipcControl.SetConfig(localPath))
                {
                    try
                    {
                        File.Delete(localPath);
                    }
                    catch { }

                    MessageBox.Show("Debug讀檔成功");
                }
                else
                    MessageBox.Show("Debug讀檔失敗");
            }
            else
            {
                if (File.Exists(readPath) && mipcControl.SetConfig(readPath))
                {
                    try
                    {
                        File.Delete(readPath);
                    }
                    catch { }

                    MessageBox.Show("讀檔成功");
                }
                else
                    MessageBox.Show("讀檔失敗");
            }
        }

        private void button_WriteConfig_Click(object sender, EventArgs e)
        {
            string writePath = @"D:\MecanumConfigs\MIPCControl\MIPCMotionConfig.900";
            string writeBackUpPath = @"D:\MecanumConfigs\MIPCControl\MIPCMotionConfigBackup.900";

            mipcControl.WriteConfig("MotionSetting", writeBackUpPath);
            MessageBox.Show(mipcControl.WriteConfig("MotionSetting", writePath) ? "寫檔成功" : "寫檔失敗");
        }
    }
}