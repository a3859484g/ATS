using Mirle.Agv.INX.Control;
using Mirle.Agv.INX.Controller;
using Mirle.Agv.INX.Controller.Tools;
using Mirle.Agv.INX.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace Mirle.Agv.INX.View
{
    public partial class ProFaceForm : Form
    {
        private LocalData localData = LocalData.Instance;
        private ComputeFunction computeFunction = ComputeFunction.Instance;
        private ProgramVersion versionForm = null;

        private object lastSender = null;

        private KeyboardNumber keyboardNumber;

        private MainForm mainForm;
        private MainFlowHandler mainFlow;
        private XmlHandler xmlHandler = new XmlHandler();

        private LoginForm loginForm = null;
        private PIOHistoryForm pioHistoryForm = null;

        public ProFaceForm(MainFlowHandler mainFlow, MainForm mainForm)
        {
            InitialAutoCycleRunConfig();

            this.mainFlow = mainFlow;
            this.mainForm = mainForm;
            InitializeComponent();

            Initial_Proface();
            Initial_Main();
            Initial_Move();
            Initial_Fork();
            Initial_Charging();
            Initial_IO();
            Initial_Alarm();
            Initial_Parameter();

            InitialLanguage();
            InitialByAGVType();
        }

        #region Language.
        private Dictionary<EnumLanguage, Label> languageLabel = new Dictionary<EnumLanguage, Label>();

        private string GetStringByTag(string tagString)
        {
            return localData.GetProfaceString(localData.Language, tagString);
        }

        private string GetStringByTag(EnumProfaceStringTag tag)
        {
            return localData.GetProfaceString(localData.Language, tag.ToString());
        }

        private void InitialLanguage()
        {
            Label tempLabel;

            int width = 100;
            int heigh = 40;

            int startX = 688;
            int startY = 395;

            foreach (EnumLanguage language in localData.AllLanguageProfaceString.Keys)
            {
                if (language != EnumLanguage.None)
                {
                    tempLabel = new Label();
                    tempLabel.Text = language.ToString();
                    tempLabel.AutoSize = false;
                    tempLabel.Font = new System.Drawing.Font("標楷體", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
                    tempLabel.Size = new Size(width, heigh);
                    tempLabel.Location = new Point(startX, startY);
                    tempLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                    tempLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
                    tempLabel.Click += LanguageLabelChange_Click;
                    tP_Main.Controls.Add(tempLabel);
                    languageLabel.Add(language, tempLabel);

                    startX -= (width - 1);
                }
            }
        }

        private void LanguageLabelChange_Click(object sender, EventArgs e)
        {
            HideAll();

            foreach (EnumLanguage language in localData.AllLanguageProfaceString.Keys)
            {
                if (languageLabel.ContainsKey(language) &&
                    languageLabel[language] == (Label)sender)
                {
                    ChangeLanguage(language);
                    break;
                }
            }
        }

        private void ChangeLanguage(EnumLanguage language)
        {
            if (localData.Language != language)
            {
                if (languageLabel.ContainsKey(localData.Language))
                {
                    languageLabel[localData.Language].ForeColor = string_Normal;
                    languageLabel[localData.Language].BackColor = backColor_Normal;
                }

                localData.Language = language;

                if (languageLabel.ContainsKey(localData.Language))
                {
                    languageLabel[localData.Language].ForeColor = string_Select;
                    languageLabel[localData.Language].BackColor = backColor_Select;
                }

                #region 共用.
                label_Warn.Text = GetStringByTag(EnumProfaceStringTag.Warn);
                label_Alarm.Text = GetStringByTag(EnumProfaceStringTag.Alarm);

                button_Main.Text = GetStringByTag(EnumProfaceStringTag.主畫面);
                button_Move.Text = GetStringByTag(EnumProfaceStringTag.走行相關);
                button_Fork.Text = GetStringByTag(EnumProfaceStringTag.取放相關);
                button_Charging.Text = GetStringByTag(EnumProfaceStringTag.充電資訊);
                button_IO.Text = GetStringByTag(EnumProfaceStringTag.IO監控);
                button_Parameter.Text = GetStringByTag(EnumProfaceStringTag.參數設定);
                button_Alarm.Text = GetStringByTag(EnumProfaceStringTag.異常資訊);

                loginForm.ChangeLanguage();
                #endregion

                #region Main.
                label_Main_ProgramVersion.Text = GetStringByTag(EnumProfaceStringTag.程式版本資訊);
                label_Main_LocalIP.Text = GetStringByTag(EnumProfaceStringTag.Local_IP);
                label_Main_MiddlerCommand.Text = GetStringByTag(EnumProfaceStringTag.MiddlerCommand);

                label_Main_MoveCommand.Text = GetStringByTag(EnumProfaceStringTag.MoveCommand);
                label_Main_AreaSensorDirection.Text = GetStringByTag(EnumProfaceStringTag.AreaSensorDirection);
                label_Main_Charging.Text = GetStringByTag(EnumProfaceStringTag.Charging);

                label_Main_ForkCommand.Text = GetStringByTag(EnumProfaceStringTag.LoadUnload);
                label_Main_Loading.Text = GetStringByTag(EnumProfaceStringTag.Loading);
                label_Main_CassetteID.Text = GetStringByTag(EnumProfaceStringTag.Cassette_ID);

                button_Main_Hide.Text = GetStringByTag(EnumProfaceStringTag.Hide);
                button_Main_IPCPowerOff.Text = GetStringByTag(EnumProfaceStringTag.IPC關機);
                versionForm.ChangeLanguage();
                #endregion

                #region Move.

                #region Move-Select.
                button_Move_Jog.Text = GetStringByTag(EnumProfaceStringTag.JogPitch);
                button_Move_Map.Text = GetStringByTag(EnumProfaceStringTag.圖資顯示);
                button_Move_SetAddressPosition.Text = GetStringByTag(EnumProfaceStringTag.Slam位置設定);
                button_Move_SensorData.Text = GetStringByTag(EnumProfaceStringTag.狀態監控);
                button_Move_AxisData.Text = GetStringByTag(EnumProfaceStringTag.各軸資訊);
                button_Move_LocateControl.Text = GetStringByTag(EnumProfaceStringTag.定位裝置);
                button_Move_CommandRecord.Text = GetStringByTag(EnumProfaceStringTag.走行命令紀錄);
                button_Move_SLAMSetPosition.Text = GetStringByTag(EnumProfaceStringTag.SLAM定位走行FromTo);
                #endregion

                #region Move-Jog.
                label_Move_Jog_LineVelocity.Text = GetStringByTag(EnumProfaceStringTag.線速度);
                label_Move_Jog_LineAcc.Text = GetStringByTag(EnumProfaceStringTag.線加速度);
                label_Move_Jog_LineDec.Text = GetStringByTag(EnumProfaceStringTag.線減速度);

                label_Move_Jog_ThetaVelocity.Text = GetStringByTag(EnumProfaceStringTag.角速度);
                label_Move_Jog_ThetaAcc.Text = GetStringByTag(EnumProfaceStringTag.角加速度);
                label_Move_Jog_ThetaDec.Text = GetStringByTag(EnumProfaceStringTag.角減速度);

                label_Move_Jog_Joystick.Text = GetStringByTag(EnumProfaceStringTag.搖桿操作);
                button_Move_Jog_JoystickEnable.Text = GetStringByTag(EnumProfaceStringTag.開啟);
                button_Move_Jog_JoystickDisable.Text = GetStringByTag(EnumProfaceStringTag.關閉);

                label_Move_Jog_AllServoOn.Text = GetStringByTag(EnumProfaceStringTag.AllServoOn);
                label_Move_Jog_AllServoOff.Text = GetStringByTag(EnumProfaceStringTag.AllServoOff);
                label_Move_Jog_RealPosition.Text = GetStringByTag(EnumProfaceStringTag.Real);

                button_Move_Jog_Set.Text = GetStringByTag(EnumProfaceStringTag.設定);
                button_Move_Jog_Clear.Text = GetStringByTag(EnumProfaceStringTag.清除);

                label_Move_Jog_SectionDeviation.Text = GetStringByTag(EnumProfaceStringTag.軌道偏差);
                label_Move_Jog_SectionTheta.Text = GetStringByTag(EnumProfaceStringTag.角度偏差);

                label_Move_Jog_SpecialFlowStatus.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.狀態), " : ");
                button_Move_Jog_ReviseByTarget.Text = GetStringByTag(EnumProfaceStringTag.自動站點補正);
                button_Move_Jog_MoveToSectionCenter.Text = GetStringByTag(EnumProfaceStringTag.移動至路中央);
                button_Move_Jog_SpecialFlowStop.Text = GetStringByTag(EnumProfaceStringTag.Stop);
                #endregion

                #region Move-Map.
                label_Move_Map_Action_Move.Text = GetStringByTag(EnumProfaceStringTag.Move);
                label_Move_Map_Action_Load.Text = GetStringByTag(EnumProfaceStringTag.Load);
                label_Move_Map_Action_Unload.Text = GetStringByTag(EnumProfaceStringTag.Unload);
                label_Move_Map_Action_LoadUnload.Text = GetStringByTag(EnumProfaceStringTag.LoadUnload);
                button_Move_Map_FunctionStart.Text = GetStringByTag(EnumProfaceStringTag.Start);
                button_Move_Map_FunctionStop.Text = GetStringByTag(EnumProfaceStringTag.Stop);

                button_Move_Map_SearchAddressIDList.Text = GetStringByTag(EnumProfaceStringTag.搜尋站點);
                button_Move_Map_SetSlamPositionByAddressID.Text = GetStringByTag(EnumProfaceStringTag.紀錄位置);
                button_Move_Map_AutoMove.Text = GetStringByTag(EnumProfaceStringTag.自動Target補正);
                button_Move_Map_AutoMoveStop.Text = GetStringByTag(EnumProfaceStringTag.Stop);
                label_Move_Map_AGVAngle.Text = GetStringByTag(EnumProfaceStringTag.車頭角度);
                label_Move_Map_TargetAddressID.Text = GetStringByTag(EnumProfaceStringTag.指定Address);
                label_Move_Map_SearchAngleRange.Text = GetStringByTag(EnumProfaceStringTag.搜尋角度);
                label_Move_Map_SearchDistanceRange.Text = GetStringByTag(EnumProfaceStringTag.搜尋範圍);
                label_Move_Map_AutoMoveStatus.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.狀態), " : ");
                #endregion

                #region Move-AxisData.
                label_Move_AxisData_AxisID.Text = GetStringByTag(EnumProfaceStringTag.軸代號.ToString());
                label_Move_AxisData_AxisName.Text = GetStringByTag(EnumProfaceStringTag.軸名稱.ToString());
                label_Move_AxisData_Encoder.Text = GetStringByTag(EnumProfaceStringTag.Encoder.ToString());
                label_Move_AxisData_RPM.Text = GetStringByTag(EnumProfaceStringTag.RPM.ToString());
                label_Move_AxisData_ServoOnOff.Text = GetStringByTag(EnumProfaceStringTag.ServoOnOff.ToString());
                label_Move_AxisData_EC.Text = GetStringByTag(EnumProfaceStringTag.EC.ToString());
                label_Move_AxisData_MF.Text = GetStringByTag(EnumProfaceStringTag.MF.ToString());
                label_Move_AxisData_V.Text = GetStringByTag(EnumProfaceStringTag.V.ToString());
                label_Move_AxisData_QA.Text = GetStringByTag(EnumProfaceStringTag.Q軸電流.ToString());
                #endregion

                #region Move-DataInfo.
                sensorData_Address.ReName(GetStringByTag(EnumProfaceStringTag.Address));
                sensorData_Section.ReName(GetStringByTag(EnumProfaceStringTag.Section));
                sensorData_Distance.ReName(GetStringByTag(EnumProfaceStringTag.Distance));
                sensorData_Real.ReName(GetStringByTag(EnumProfaceStringTag.Real));
                sensorData_MIPCAGVPosition.ReName(GetStringByTag(EnumProfaceStringTag.MIPC));
                sensorData_CommandID.ReName(GetStringByTag(EnumProfaceStringTag.CommandID));
                sensorData_LocationAGVPosition.ReName(GetStringByTag(EnumProfaceStringTag.Locate));
                sensorData_CommandStartTime.ReName(GetStringByTag(EnumProfaceStringTag.StartTime));
                sensorData_CommandStstus.ReName(GetStringByTag(EnumProfaceStringTag.CmdStatus));
                sensorData_MoveStatus.ReName(GetStringByTag(EnumProfaceStringTag.MoveStatus));
                sensorData_CommandEncoder.ReName(GetStringByTag(EnumProfaceStringTag.CmdEncoder));
                sensorData_Velocity.ReName(GetStringByTag(EnumProfaceStringTag.Velocity));
                #endregion

                #region Move-LocateDriver.
                for (int i = 0; i < tC_LocateDriverList.TabPages.Count; i++)
                    jogPitchLocateDataList[i].ChangeNameByLanguage();
                #endregion

                #region Move-Record.
                label_Move_Record_CommandID.Text = GetStringByTag(EnumProfaceStringTag.CommandID);
                label_Move_Record_StartTime.Text = GetStringByTag(EnumProfaceStringTag.StartTime);
                label_Move_Record_EndTime.Text = GetStringByTag(EnumProfaceStringTag.EndTime);
                label_Move_Record_Result.Text = GetStringByTag(EnumProfaceStringTag.Result);
                #endregion

                #region Move-SetSlamPosition.
                label_Move_SetSlamPosition_SelectAddress.Text = GetStringByTag(EnumProfaceStringTag.Address);
                label_FromTo_TargetAddressID.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.AddressID), " :");
                radioButton_FormTo_Normal.Text = GetStringByTag(EnumProfaceStringTag.一般點位);
                radioButton_FromTo_ChargerStation.Text = GetStringByTag(EnumProfaceStringTag.充電站);
                radioButton_FromTo_Port.Text = GetStringByTag(EnumProfaceStringTag.取放站);
                label_Move_SetSlamPosition_SlamInfo.Text = GetStringByTag(EnumProfaceStringTag.SlamInfo);
                button_Move_SetSlamPosition_AgreeSlamData.Text = GetStringByTag(EnumProfaceStringTag.認可目前位置);
                button_Move_SetSlamPosition_SetPosition.Text = GetStringByTag(EnumProfaceStringTag.SetPosition);
                #endregion

                #region Move-LocalCycleRun.
                label_Move_SetSlamPosition_LocalCycleRun.Text = GetStringByTag(EnumProfaceStringTag.LocalCycleRun);
                #endregion

                #endregion

                #region Fork.

                #region Fork-Select.
                button_Fork_Jog.Text = GetStringByTag(EnumProfaceStringTag.手臂吋動);
                button_Fork_Home.Text = GetStringByTag(EnumProfaceStringTag.原點復歸);
                button_Fork_Command.Text = GetStringByTag(EnumProfaceStringTag.手臂半自動);
                button_Fork_Alignment.Text = GetStringByTag(EnumProfaceStringTag.補正測試);
                button_Fork_CommandRecord.Text = GetStringByTag(EnumProfaceStringTag.命令紀錄);
                button_Fork_PIO.Text = GetStringByTag(EnumProfaceStringTag.PIO監控);
                button_Fork_AxisData.Text = GetStringByTag(EnumProfaceStringTag.各軸資訊);
                button_Fork_HomeAndStageSetting.Text = GetStringByTag(EnumProfaceStringTag.Home設定_站點設定);

                #endregion

                #region Fork-Jog.
                button_LoadUnloadJogHigh.Text = GetStringByTag(EnumProfaceStringTag.Fork_Jog_快);
                button_LoadUnloadJogNormal.Text = GetStringByTag(EnumProfaceStringTag.Fork_Jog_中);
                button_LoadUnloadJogLow.Text = GetStringByTag(EnumProfaceStringTag.Fork_Jog_慢);
                gB_LoadUnload.Text = GetStringByTag(EnumProfaceStringTag.Velocity);
                button_LoadUnloadJogStop.Text = GetStringByTag(EnumProfaceStringTag.ForkJogStop);
                button_LoadUnloadJogByPass.Text = GetStringByTag(EnumProfaceStringTag.Fork_Jog_強制Bypass);

                int sensorCount;

                if (mainFlow.LoadUnloadControl.LoadUnload != null)
                {
                    for (int i = 0; i < forkAxisList.Count && i < mainFlow.LoadUnloadControl.LoadUnload.AxisList.Count; i++)
                    {
                        forkAxisList[i].Text = GetStringByTag(mainFlow.LoadUnloadControl.LoadUnload.AxisList[i]);
                        posButtonList[i].Text = GetStringByTag(mainFlow.LoadUnloadControl.LoadUnload.AxisPosName[i]);
                        nagButtonList[i].Text = GetStringByTag(mainFlow.LoadUnloadControl.LoadUnload.AxisNagName[i]);



                        sensorCount = mainFlow.LoadUnloadControl.LoadUnload.AxisSensorList[mainFlow.LoadUnloadControl.LoadUnload.AxisList[i]].Count;

                        for (int j = 0; j < sensorCount; j++)
                        {
                            if (axisSensorList.ContainsKey(mainFlow.LoadUnloadControl.LoadUnload.AxisList[i]) &&
                                j < axisSensorList[mainFlow.LoadUnloadControl.LoadUnload.AxisList[i]].Count)
                                axisSensorList[mainFlow.LoadUnloadControl.LoadUnload.AxisList[i]][j].Text = GetStringByTag(mainFlow.LoadUnloadControl.LoadUnload.AxisSensorList[mainFlow.LoadUnloadControl.LoadUnload.AxisList[i]][j]);
                        }
                    }
                }
                #endregion

                #region Fork-Home.
                label_Fork_HomeTitle.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.回Home條件), " :");
                label_Fork_HomeTitleMessage.Text = GetStringByTag(EnumProfaceStringTag.回Home條件_內容);

                label_Fork_Home_Homging.Text = GetStringByTag(EnumProfaceStringTag.回Home流程中);
                label_Fork_Home_HomeStatus.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.Fork_Home), " :");

                button_Fork_Home_Home.Text = GetStringByTag(EnumProfaceStringTag.Home);
                button_Fork_Home_Home_Initial.Text = GetStringByTag(EnumProfaceStringTag.Home_Initial);
                button_Fork_Home_Stop.Text = GetStringByTag(EnumProfaceStringTag.Stop);
                #endregion

                #region Fork-Command.
                label_Fork_Command_Type.Text = GetStringByTag(EnumProfaceStringTag.命令);
                button_Fork_Command_Type_Load.Text = GetStringByTag(EnumProfaceStringTag.Load);
                button_Fork_Command_Type_Unload.Text = GetStringByTag(EnumProfaceStringTag.Unload);

                label_Fork_Command_Direction.Text = GetStringByTag(EnumProfaceStringTag.方向);
                button_Fork_Command_Direction_Left.Text = GetStringByTag(EnumProfaceStringTag.Left);
                button_Fork_Command_Direction_Right.Text = GetStringByTag(EnumProfaceStringTag.Right);

                label_Fork_Command_StageNumber.Text = GetStringByTag(EnumProfaceStringTag.StageNumber);
                label_Fork_Command_Speed.Text = GetStringByTag(EnumProfaceStringTag.速度Percentage);

                label_Fork_Command_PIO.Text = GetStringByTag(EnumProfaceStringTag.PIO);
                button_Fork_Command_PIO_Use.Text = GetStringByTag(EnumProfaceStringTag.使用);
                button_Fork_Command_PIO_NotUse.Text = GetStringByTag(EnumProfaceStringTag.不使用);

                label_Fork_Command_BreakenMode.Text = GetStringByTag(EnumProfaceStringTag.分解模式);
                button_Fork_Command_BreakenMode_NotUse.Text = GetStringByTag(EnumProfaceStringTag.關閉);
                button_Fork_Command_BreakenMode_Use.Text = GetStringByTag(EnumProfaceStringTag.開啟);

                label_Fork_Command_AlignmentValue.Text = GetStringByTag(EnumProfaceStringTag.啟用補正);
                button_Fork_Command_AlignmentValue_Use.Text = GetStringByTag(EnumProfaceStringTag.啟用);
                button_Fork_Command_AlignmentValue_NotUse.Text = GetStringByTag(EnumProfaceStringTag.不啟用);

                label_Fork_Command_CstInAGVDirection.Text = GetStringByTag(EnumProfaceStringTag.儲位);
                button_Fork_Command_CstInAGV_Left.Text = GetStringByTag(EnumProfaceStringTag.Left);
                button_Fork_Command_CstInAGV_Right.Text = GetStringByTag(EnumProfaceStringTag.Right);

                button_Fork_Command_Start.Text = GetStringByTag(EnumProfaceStringTag.Start);
                button_Fork_Command_StartByAddressID.Text = GetStringByTag(EnumProfaceStringTag.Start_By_NowAddress);
                button_Fork_Command_Stop.Text = GetStringByTag(EnumProfaceStringTag.Stop);

                button_Fork_Command_Pause.Text = GetStringByTag(EnumProfaceStringTag.Pause);
                button_Fork_Command_Continue.Text = GetStringByTag(EnumProfaceStringTag.Continue);

                button_Fork_Command_GoNext.Text = GetStringByTag(EnumProfaceStringTag.下一步);
                button_Fork_Command_Back.Text = GetStringByTag(EnumProfaceStringTag.上一步);

                label_Fork_Command_CommandStatus.Text = GetStringByTag(EnumProfaceStringTag.分解模式);
                label_Fork_Command_ForkHome.Text = GetStringByTag(EnumProfaceStringTag.ForkHome);
                label_Fork_Command_Loading.Text = GetStringByTag(EnumProfaceStringTag.Loading);
                label_Fork_Command_DoubleStoregeL.Text = GetStringByTag(EnumProfaceStringTag.二重格檢知_L);
                label_Fork_Command_DoubleStoregeR.Text = GetStringByTag(EnumProfaceStringTag.二重格檢知_R);

                label_Fork_Command_Alignment_P.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.Alignment_P), " :");
                label_Fork_Command_Alignment_Y.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.Alignment_Y), " :");
                label_Fork_Command_Alignment_Theta.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.Alignment_Theta), " :");
                label_Fork_Command_Alignment_Z.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.Alignment_Z), " :");
                #endregion

                #region Fork-Alignment.
                button_Fork_Alignment_LeftCheck.Text = GetStringByTag(EnumProfaceStringTag.左側);
                button_Fork_Alignment_RightCheck.Text = GetStringByTag(EnumProfaceStringTag.右側);
                button_ForkAlignment_Alignment_AddressCheck.Text = GetStringByTag(EnumProfaceStringTag.使用圖資偵測);

                label_Fork_Alignment_StageNumber.Text = GetStringByTag(EnumProfaceStringTag.StageNumber);

                label_Fork_Alignment_LaserF.Text = GetStringByTag(EnumProfaceStringTag.Laser_Front);
                label_Fork_Alignment_LaserB.Text = GetStringByTag(EnumProfaceStringTag.Laser_Back);

                label_Fork_Alignment_BarcodeTitle.Text = GetStringByTag(EnumProfaceStringTag.Barcode資料);
                label_Fork_Alignment_Barcode_ID.Text = GetStringByTag(EnumProfaceStringTag.Barcode_ID);
                label_Fork_Alignment_Barcode_X.Text = GetStringByTag(EnumProfaceStringTag.Barcode_X);
                label_Fork_Alignment_Barcode_Y.Text = GetStringByTag(EnumProfaceStringTag.Barcode_Y);

                label_Fork_Alignment_AlignmentTitle.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.AlignmentValue), " :");
                label_Fork_Alignment_Alignment_P.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.Alignment_P), " :");
                label_Fork_Alignment_Alignment_Y.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.Alignment_Y), " :");
                label_Fork_Alignment_Alignment_Theta.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.Alignment_Theta), " :");
                label_Fork_Alignment_Alignment_Z.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.Alignment_Z), " :");
                #endregion

                #region Fork-Record.
                #endregion

                #region Fork-PIO
                label_Fork_PIO_NotSendTR_REQ.Text = GetStringByTag(EnumProfaceStringTag.NotSendTR_REQ);
                label_Fork_PIO_NotSendBUSY.Text = GetStringByTag(EnumProfaceStringTag.NotSendBUSY);
                label_Fork_PIO_NotForkBusyAction.Text = GetStringByTag(EnumProfaceStringTag.NotForkBusyAction);
                label_Fork_PIO_NotSendCOMPT.Text = GetStringByTag(EnumProfaceStringTag.NotSendCOMPT);
                label_Fork_PIO_NotSendAllOff.Text = GetStringByTag(EnumProfaceStringTag.NotSendAllOff);
                #endregion

                #region Fork-AxisData.
                if (mainFlow.LoadUnloadControl.LoadUnload != null)
                {
                    for (int i = 0; i < allForkAxisName.Count && i < mainFlow.LoadUnloadControl.LoadUnload.FeedbackAxisList.Count; i++)
                        allForkAxisName[i].Text = GetStringByTag(mainFlow.LoadUnloadControl.LoadUnload.FeedbackAxisList[i]);
                }

                label_Fork_AxisData_Name.Text = GetStringByTag(EnumProfaceStringTag.軸名稱);
                label_Fork_AxisData_Encoder.Text = GetStringByTag(EnumProfaceStringTag.Encoder);
                label_Fork_AxisData_Velocity.Text = GetStringByTag(EnumProfaceStringTag.Velocity);
                label_Fork_AxisData_ServoOnOff.Text = GetStringByTag(EnumProfaceStringTag.ServoOnOff);
                label_Fork_AxisData_Stop.Text = GetStringByTag(EnumProfaceStringTag.軸狀態);
                label_Fork_AxisData_EC.Text = GetStringByTag(EnumProfaceStringTag.EC);
                label_Fork_AxisData_MF.Text = GetStringByTag(EnumProfaceStringTag.MF);
                label_Fork_AxisData_V.Text = GetStringByTag(EnumProfaceStringTag.V);
                label_Fork_AxisData_QA.Text = GetStringByTag(EnumProfaceStringTag.Q軸電流);
                #endregion

                #region Fork-HomeSetting.
                switch (localData.MainFlowConfig.AGVType)
                {
                    case EnumAGVType.UMTC:
                        #region Fork-HomeSetting(UMTC).
                        #endregion
                        break;
                }

                #endregion

                #endregion

                #region Charging.

                #region Charging-Select.
                button_Charging_BatteryInfo.Text = GetStringByTag(EnumProfaceStringTag.電池資訊);
                button_Charging_Flow.Text = GetStringByTag(EnumProfaceStringTag.手自動充電);
                button_Charging_PIO.Text = GetStringByTag(EnumProfaceStringTag.充電PIO監控);
                button_Charging_Record.Text = GetStringByTag(EnumProfaceStringTag.充電紀錄);
                #endregion

                #region Charging-BatteryInfo.
                #endregion

                #region Charging-Command.
                label_Charging_Command_Chaging.Text = GetStringByTag(EnumProfaceStringTag.Charging充電中); ;

                button_Charging_Command_LeftAuto.Text = GetStringByTag(EnumProfaceStringTag.左充電); ;
                button_Charging_Command_RightAuto.Text = GetStringByTag(EnumProfaceStringTag.右充電); ;
                button_Charging_Command_LeftChargingSafety.Text = GetStringByTag(EnumProfaceStringTag.電磁接觸器); ;
                button_Charging_Command_RightChargingSafety.Text = GetStringByTag(EnumProfaceStringTag.電磁接觸器); ;

                label_Charging_Command_ConfirmSensorL.Text = GetStringByTag(EnumProfaceStringTag.對位Sensor_L); ;
                label_Charging_Command_ConfirmSensorR.Text = GetStringByTag(EnumProfaceStringTag.對位Sensor_R); ;

                label_Charging_Command_BatteryV.Text = GetStringByTag(EnumProfaceStringTag.電池電壓); ;
                label_Charging_Command_BatteryA.Text = GetStringByTag(EnumProfaceStringTag.電池安培); ;
                label_Charging_Command_BatteryTemp.Text = GetStringByTag(EnumProfaceStringTag.電池溫度); ;
                label_Charging_Command_MeterV.Text = GetStringByTag(EnumProfaceStringTag.電表電壓); ;
                label_Charging_Command_MeterA.Text = GetStringByTag(EnumProfaceStringTag.電表安培); ;
                #endregion

                #region Charging-PIO.
                #endregion

                #region Charging-Record.
                #endregion

                #endregion

                #region IO.
                button_IO_SensorSafey.Text = GetStringByTag(EnumProfaceStringTag.安全元件訊號);
                button_IO_IOTest.Text = GetStringByTag(EnumProfaceStringTag.IO_測試);

                safetySensorTitle.SetLabelByStringList(new List<string>()
                {
                    GetStringByTag(EnumProfaceStringTag.DeviceName),
                    GetStringByTag(EnumProfaceStringTag.Alarm),
                    GetStringByTag(EnumProfaceStringTag.Warn),
                    GetStringByTag(EnumProfaceStringTag.EMO),
                    GetStringByTag(EnumProfaceStringTag.IPCEMO),
                    GetStringByTag(EnumProfaceStringTag.EMS),
                    GetStringByTag(EnumProfaceStringTag.SlowStop),
                    GetStringByTag(EnumProfaceStringTag.LowSpeed_Low),
                    GetStringByTag(EnumProfaceStringTag.LowSpeed_High),
                    GetStringByTag(EnumProfaceStringTag.Normal)
                });
                safetySensorStatus.SetLabelByStringList(new List<string>() { GetStringByTag(EnumProfaceStringTag.Status) });

                label_IO_IOTest_Input.Text = GetStringByTag(EnumProfaceStringTag.Input);
                label_IO_IOTest_Output.Text = GetStringByTag(EnumProfaceStringTag.Output);
                label_IO_IOTest_StopSendIO.Text = GetStringByTag(EnumProfaceStringTag.取消自動輸出Output);

                if (lpmsIndex != -1)
                    lpmsDataView.ChangeLanguage();
                #endregion

                #region Alarm.
                button_Alarm_ShowAlarm.Text = GetStringByTag(EnumProfaceStringTag.現有異常.ToString());
                button_Alarm_ShowAlarmHistory.Text = GetStringByTag(EnumProfaceStringTag.異常紀錄.ToString());

                label_Alarm_NoPower.Text = GetStringByTag(EnumProfaceStringTag.無動力電中_請按Reset_重新送電.ToString());

                button_Alarm_ResetAlarm.Text = GetStringByTag(EnumProfaceStringTag.清除異常.ToString());
                button_Alarm_BuzzOff.Text = GetStringByTag(EnumProfaceStringTag.Buzz_Off.ToString());
                #endregion

                #region Parameter.
                for (int i = 0; i < pioTimeoutLabelList.Count; i++)
                    pioTimeoutLabelList[i].Text = GetStringByTag(EnumProfaceStringTag.秒);
                button_Parameter_SensorSafety.Text = GetStringByTag(EnumProfaceStringTag.安全偵測設定);
                button_Parameter_BatteryConfig.Text = GetStringByTag(EnumProfaceStringTag.電池保護設定);
                button_Parameter_PIOTimeout.Text = GetStringByTag(EnumProfaceStringTag.PIO_timeout設定);
                button_Parameter_MoveControlConfig.Text = GetStringByTag(EnumProfaceStringTag.移動控制參數);
                button_Parameter_MainFlowConfig.Text = GetStringByTag(EnumProfaceStringTag.其他設定);

                label_Parameter_SafetySensor_DeviceName.Text = GetStringByTag(EnumProfaceStringTag.Name);
                label_Parameter_SafetySensor_BypassAlarm.Text = GetStringByTag(EnumProfaceStringTag.ByPassAlarm);
                label_Parameter_SafetySensor_BypassSafety.Text = GetStringByTag(EnumProfaceStringTag.ByPassSafety);
                button_ParameterSafetySensorReset.Text = GetStringByTag(EnumProfaceStringTag.Reset);

                label_Parameter_BatteryConfig_FullChargingSOC.Text = GetStringByTag(EnumProfaceStringTag.滿充SOC);
                label_Parameter_BatteryConfig_LowBatterySOC.Text = GetStringByTag(EnumProfaceStringTag.低水位SOC);
                label_Parameter_BatteryConfig_LowBatteryShutDownSOC.Text = GetStringByTag(EnumProfaceStringTag.斷電SOC);

                label_Parameter_BatteryConfig_FullChargingV.Text = GetStringByTag(EnumProfaceStringTag.滿充電壓);
                label_Parameter_BatteryConfig_LowBatteryV.Text = GetStringByTag(EnumProfaceStringTag.低水位電壓);
                label_Parameter_BatteryConfig_LowBatteryShutDownV.Text = GetStringByTag(EnumProfaceStringTag.斷電電壓);

                label_Parameter_BatteryConfig_ChargingMaxA.Text = GetStringByTag(EnumProfaceStringTag.充電最大安培);
                label_Parameter_BatteryConfig_ChargingMaxTemp.Text = GetStringByTag(EnumProfaceStringTag.充電最高溫度);

                label_Parameter_BatteryConfig_SendAlarmDelayTime.Text = GetStringByTag(EnumProfaceStringTag.警報延遲時間);
                label_Parameter_BatteryConfig_ShutDownDelayTime.Text = GetStringByTag(EnumProfaceStringTag.斷電延遲時間);

                label_Parameter_BatteryConfig_TempWarningValue.Text = GetStringByTag(EnumProfaceStringTag.溫度警告閥值);
                label_Parameter_BatteryConfig_ShutDownTemp.Text = GetStringByTag(EnumProfaceStringTag.溫度斷電閥值);

                label_Parameter_MainFlowConfig_SavePowerMode.Text = GetStringByTag(EnumProfaceStringTag.省電模式);
                label_Parameter_MainFlowConfig_ZUpMode.Text = GetStringByTag(EnumProfaceStringTag.Z軸上位Home);
                label_Parameter_MainFlowConfig_CheckPassLine.Text = GetStringByTag(EnumProfaceStringTag.檢查上定位);
                label_Parameter_MainFlowConfig_IdleNotLogCSV.Text = GetStringByTag(EnumProfaceStringTag.Idle不紀錄CSV);

                foreach (EnumMoveControlSafetyType type in (EnumMoveControlSafetyType[])Enum.GetValues(typeof(EnumMoveControlSafetyType)))
                {
                    if (allMoveControl_Safety.ContainsKey(type))
                        allMoveControl_Safety[type].ChangeLanguage();
                }

                foreach (EnumSensorSafetyType type in (EnumSensorSafetyType[])Enum.GetValues(typeof(EnumSensorSafetyType)))
                {
                    if (allMoveControl_SensorBypass.ContainsKey(type))
                        allMoveControl_SensorBypass[type].ChangeLanguage();
                }
                #endregion
            }
        }
        #endregion

        #region InitialByAGVType.
        private void InitialByAGVType()
        {
            switch (localData.MainFlowConfig.AGVType)
            {
                case EnumAGVType.AGC:
                    InitialByAGVType_AGC();
                    break;
                case EnumAGVType.UMTC:
                    InitialByAGVType_UMTC();
                    break;
                case EnumAGVType.PTI:
                    InitialByAGVType_PTI();
                    break;
                case EnumAGVType.ATS:
                    InitalByAGVType_ATS();
                    break;
                default:
                    break;
            }
        }

        private void InitialByAGVType_AGC()
        {
            ChangeLanguage(EnumLanguage.繁體中文);
            button_Fork_HomeAndStageSetting.Visible = false;
            label_Main_LoadingValue_Left.Visible = false;
            label_Main_LoadingValue_Right.Visible = false;
            label_Main_CassetteIDValue_Left.Visible = false;
            label_Main_CassetteIDValue_Right.Visible = false;
        }

        private void InitialByAGVType_UMTC()
        {
            ChangeLanguage(EnumLanguage.繁體中文);


            button_LoadUnload_HomeAndStageSetting_SaveZ.Visible = true;
            button_Move_Map_PanelVisableChange.Visible = true;
            panel_Move_Map_MoveByTarget.Visible = true;
            label_Main_LoadingValue_Left.Visible = false;
            label_Main_LoadingValue_Right.Visible = false;
            label_Main_CassetteIDValue_Left.Visible = false;
            label_Main_CassetteIDValue_Right.Visible = false;
        }

        private void InitialByAGVType_PTI()
        {
            ChangeLanguage(EnumLanguage.繁體中文);

            button_Fork_HomeAndStageSetting.Visible = false;
            label_Main_LoadingValue_Left.Visible = false;
            label_Main_LoadingValue_Right.Visible = false;
            label_Main_CassetteIDValue_Left.Visible = false;
            label_Main_CassetteIDValue_Right.Visible = false;
        }

        private void InitalByAGVType_ATS()
        {
            ChangeLanguage(EnumLanguage.簡體中文);
            button_Fork_HomeAndStageSetting.Visible = false;

            button_Fork_Command_CstInAGV_Left.BackColor = Color.Green;

            panel_LoadUnload_Command_CstInAGVDirection.Visible = true;
            panel_LoadUnload_Command_Speed.Visible = false;
            panel_LoadUnload_Command_CstInAGVDirection.Location = panel_LoadUnload_Command_Speed.Location;
            label_Main_LoadingValue.Visible = false;
            label_Main_CassetteIDValue.Visible = false;
        }
        #endregion

        #region Set Label/Button ForeColor & BackColor.
        private Color string_Normal = Color.Black;
        private Color string_Select = Color.White;

        private Color backColor_Normal = Color.Transparent;
        private Color backColor_Select = Color.Green;
        private Color backColor_Warning = Color.Red;
        private Color backColor_Alarm = Color.DarkRed;

        private void SetLabelInAlarm(Label label, bool onOff)
        {
            if (onOff)
            {
                label.ForeColor = string_Normal;
                label.BackColor = backColor_Alarm;
            }
            else
            {
                label.ForeColor = string_Normal;
                label.BackColor = backColor_Normal;
            }
        }

        private void SetLabelInWarning(Label label, bool onOff)
        {
            if (onOff)
            {
                label.ForeColor = string_Normal;
                label.BackColor = backColor_Warning;
            }
            else
            {
                label.ForeColor = string_Normal;
                label.BackColor = backColor_Normal;
            }
        }

        private void SetLabelInSelected(Label label, bool onOff)
        {
            if (onOff)
            {
                label.ForeColor = string_Select;
                label.BackColor = backColor_Select;
            }
            else
            {
                label.ForeColor = string_Normal;
                label.BackColor = backColor_Normal;
            }
        }

        private void SetButtonInAlarm(Button button, bool onOff)
        {
            if (onOff)
            {
                button.ForeColor = string_Normal;
                button.BackColor = backColor_Alarm;
            }
            else
            {
                button.ForeColor = string_Normal;
                button.BackColor = backColor_Normal;
            }
        }

        private void SetButtonInWarning(Button button, bool onOff)
        {
            if (onOff)
            {
                button.ForeColor = string_Normal;
                button.BackColor = backColor_Warning;
            }
            else
            {
                button.ForeColor = string_Normal;
                button.BackColor = backColor_Normal;
            }
        }

        private void SetButtonInSelected(Button button, bool onOff)
        {
            if (onOff)
            {
                button.ForeColor = string_Select;
                button.BackColor = backColor_Select;
            }
            else
            {
                button.ForeColor = string_Normal;
                button.BackColor = backColor_Normal;
            }
        }

        private void OpenAlarmHistory()
        {
            try
            {         
            string EXCUTEPATH = Path.Combine(Environment.CurrentDirectory, "HelloWorld.exe"); //
                Process[] app = Process.GetProcessesByName("HelloWorld");
                if (app.Length > 0)
                {
                    foreach (Process ps in app)
                    {
                        ps.Kill();
                        if (app.Length != 0)
                            System.Diagnostics.Process.Start(EXCUTEPATH);
                        //MessageBox.Show("請關閉已經啟動的程式後再進行嘗試");
                    }                               
                }
                else
                {
                    System.Diagnostics.Process.Start(EXCUTEPATH);
                }

            }
            catch(Exception ex)
            {
                mainFlow.WriteLog(2, "", String.Concat("AlarmHistory外部開啟失敗 或 找不到該程式", ex.ToString()));
                MessageBox.Show("");
            }
        }

        private void SetLabelTextAndColor(Label label, EnumVehicleSafetyAction type)
        {
            label.Text = GetStringByTag(type.ToString());

            switch (type)
            {
                case EnumVehicleSafetyAction.Normal:
                    label.ForeColor = Color.Green;
                    break;
                case EnumVehicleSafetyAction.LowSpeed_High:
                    label.ForeColor = Color.Yellow;
                    break;
                case EnumVehicleSafetyAction.LowSpeed_Low:
                    label.ForeColor = Color.OrangeRed;
                    break;
                case EnumVehicleSafetyAction.SlowStop:
                    label.ForeColor = Color.Red;
                    break;
            }
        }

        private void SetLabelTextAndColor(Label label, EnumSafetyLevel type)
        {
            label.Text = type.ToString();

            switch (type)
            {
                case EnumSafetyLevel.Alarm:
                case EnumSafetyLevel.EMO:
                case EnumSafetyLevel.IPCEMO:
                    label.ForeColor = Color.DarkRed;
                    break;
                case EnumSafetyLevel.EMS:
                case EnumSafetyLevel.SlowStop:
                    label.ForeColor = Color.Red;

                    break;
                case EnumSafetyLevel.LowSpeed_Low:
                    label.ForeColor = Color.OrangeRed;

                    break;
                case EnumSafetyLevel.LowSpeed_High:
                    label.ForeColor = Color.Yellow;
                    break;
                default:
                    label.ForeColor = Color.Green;
                    break;
            }
        }
        #endregion

        #region Proface.
        private void Initial_Proface()
        {
            #region Login User Control Form.
            loginForm = new LoginForm(mainFlow.UserLoginout);
            loginForm.Location = new Point((this.Size.Width - loginForm.Size.Width) / 2,
                                           (this.Size.Height - loginForm.Size.Height) / 2);
            this.Controls.Add(loginForm);
            loginForm.HideLogForm();
            #endregion

            #region 數字鍵盤 User Control Form.
            keyboardNumber = new KeyboardNumber();
            keyboardNumber.Location = new Point(280, 50);
            this.Controls.Add(keyboardNumber);
            keyboardNumber.BringToFront();
            keyboardNumber.Visible = false;
            #endregion

            lastSender = button_Main;
            SetButtonInSelected((Button)lastSender, true);
        }

        public void ShowForm()
        {
            this.Show();
            timer.Enabled = true;
            this.BringToFront();
        }

        private void HideAll()
        {
            keyboardNumber.Hide();
            versionForm.HideVersion();
            loginForm.HideLogForm();

            if (pioHistoryForm != null)
                pioHistoryForm.Hide();
        }

        private void Hide_Click(object sender, EventArgs e)
        {
            HideAll();
        }

        private void CallKeyboardNumber(object sender, EventArgs e)
        {
            keyboardNumber.SetTextBoxAndShow((TextBox)sender);
            keyboardNumber.BringToFront();
        }

        private bool ChageTabControlByPageIndex(int pageIndex)
        {
            HideAll();

            if (pageIndex < tC_Info.TabCount)
            {
                tC_Info.SelectedIndex = pageIndex;
                return true;
            }
            else
                return false;
        }

        private void ShowLoginForm(object sender, EventArgs e)
        {
            HideAll();
            loginForm.ShowLoginForm();
            loginForm.BringToFront();
        }

        private void pB_Warn_Click(object sender, EventArgs e)
        {
            if (localData.MIPCData.HasWarn)
                button_Alarm_Click((object)button_Alarm, e);
            else
                HideAll();
        }

        private void pB_Alarm_Click(object sender, EventArgs e)
        {
            if (localData.MIPCData.HasAlarm)
                button_Alarm_Click((object)button_Alarm, e);
            else
                HideAll();
        }

        private void button_Main_Click(object sender, EventArgs e)
        {
            if (ChageTabControlByPageIndex((int)EnumProfacePageIndex.Main))
            {
                SetButtonInSelected((Button)lastSender, false);
                lastSender = sender;
                SetButtonInSelected((Button)lastSender, true);
            }
        }

        private void button_MoveControl_Click(object sender, EventArgs e)
        {
            if (ChageTabControlByPageIndex((int)EnumProfacePageIndex.Move_Select))
            {
                SetButtonInSelected((Button)lastSender, false);
                lastSender = sender;
                SetButtonInSelected((Button)lastSender, true);
            }
        }

        private void button_LoadUnload_Click(object sender, EventArgs e)
        {
            if (ChageTabControlByPageIndex((int)EnumProfacePageIndex.Fork_Select))
            {
                SetButtonInSelected((Button)lastSender, false);
                lastSender = sender;
                SetButtonInSelected((Button)lastSender, true);
            }
        }

        private void button_Charging_Click(object sender, EventArgs e)
        {
            if (ChageTabControlByPageIndex((int)EnumProfacePageIndex.Charging_Select))
            {
                SetButtonInSelected((Button)lastSender, false);
                lastSender = sender;
                SetButtonInSelected((Button)lastSender, true);
            }
        }

        private void button_IO_Click(object sender, EventArgs e)
        {
            if (ChageTabControlByPageIndex((int)EnumProfacePageIndex.IO))
            {
                SetButtonInSelected((Button)lastSender, false);
                lastSender = sender;
                SetButtonInSelected((Button)lastSender, true);
            }
        }

        private void button_Alarm_Click(object sender, EventArgs e)
        {
            if (ChageTabControlByPageIndex((int)EnumProfacePageIndex.Alarm))
            {
                SetButtonInSelected((Button)lastSender, false);
                lastSender = sender;
                SetButtonInSelected((Button)lastSender, true);
            }
        }

        private void button_Parameter_Click(object sender, EventArgs e)
        {
            if (ChageTabControlByPageIndex((int)EnumProfacePageIndex.Parameter))
            {
                SetButtonInSelected((Button)lastSender, false);
                lastSender = sender;
                SetButtonInSelected((Button)lastSender, true);
            }
        }

        private void Update_Proface()
        {
            if (!loginForm.Visible)
                mainFlow.UserLoginout.UpdateLoginTime();

            localData.AGVCOnline = mainFlow.MiddlerContorlHandler != null && mainFlow.MiddlerContorlHandler.agvcConnector != null && mainFlow.MiddlerContorlHandler.agvcConnector.IsConnected();

            pB_Alarm.BackColor = (localData.MIPCData.HasAlarm ? Color.Red : Color.Transparent);
            pB_Warn.BackColor = (localData.MIPCData.HasWarn ? Color.Yellow : Color.Transparent);

            label_SOC.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.SOC.ToString()), " : ",
                                                            localData.BatteryInfo.Battery_SOC.ToString("0"), "% ",
                                           GetStringByTag(EnumProfaceStringTag.電壓.ToString()), " : ",
                                                            localData.BatteryInfo.Battery_V.ToString("0.0"), "V");

            label_LoginLevel.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.LoginLevel.ToString()), " : ",
                                                  GetStringByTag(localData.LoginLevel.ToString()));

            label_AutoManual.Text = localData.AutoManual.ToString();

            if (localData.AutoManual == EnumAutoState.Auto)
            {
                label_AutoManual.BackColor = Color.Green;
                label_AutoManual.ForeColor = Color.White;
            }
            else
            {
                label_AutoManual.BackColor = Color.Red;
                label_AutoManual.ForeColor = Color.Black;
            }
        }
        #endregion

        #region Main.
        private void Initial_Main()
        {
            try
            {
                if (mainFlow.MiddlerContorlHandler != null)
                {
                    label_Main_AGVLocalName.Text = mainFlow.MiddlerContorlHandler.Vehicle.AgvcConnectorConfig.ClientName;
                    label_Main_LocalIPValue.Text = mainFlow.MiddlerContorlHandler.Vehicle.AgvcConnectorConfig.LocalIp;
                }
            }
            catch { }

            #region 程式版本User Control Form.
            versionForm = new ProgramVersion();
            tP_Main.Controls.Add(versionForm);
            versionForm.Location = new Point((tP_Main.Size.Width - versionForm.Size.Width) / 2,
                                             (tP_Main.Size.Height - versionForm.Size.Height) / 2);
            versionForm.HideVersion();
            #endregion
        }

        private void label_Main_ProgramVersion_Click(object sender, EventArgs e)
        {
            HideAll();
            versionForm.ShowVersion();
            versionForm.BringToFront();
        }

        private void button_Main_Hid_Click(object sender, EventArgs e)
        {
            if (mainFlow.ActionCanUse(EnumUserAction.Main_Hide))
                this.Hide();
        }

        private Stopwatch shutDownTimer = new Stopwatch();
        private bool shutDownTimeStart = false;
        private double shutDownTime = 1000;
        private int shutDownButtonCount = 0;
        private int shutDownNumber = 3;

        private void button_Main_IPCPowerOff_Click(object sender, EventArgs e)
        {
            if (localData.AutoManual == EnumAutoState.Manual &&
                localData.MoveControlData.MoveCommand == null &&
                localData.LoadUnloadData.LoadUnloadCommand == null)
            {
                if (shutDownTimeStart && shutDownTimer.ElapsedMilliseconds < shutDownTime)
                {
                    shutDownButtonCount++;
                    if (shutDownButtonCount >= shutDownNumber)
                    {
                        try
                        {
                            Process.Start("Shutdown.exe", " -s -t 0");
                        }
                        catch (Exception ex)
                        {
                            mainFlow.WriteLog(3, "", String.Concat("Shutdown Exception : ", ex.ToString()));
                        }
                    }
                }
                else
                {
                    shutDownTimer.Restart();
                    shutDownTimeStart = true;
                    shutDownButtonCount = 1;
                }
            }
        }

        private bool mainFormMouseDown = false;

        private Point lastMianFormPoint;

        private double mainFormMovingDistance = 0;
        private double mainFormHideMovingDistance = 2000;

        private void tP_Main_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                mainFormMovingDistance = 0;
                lastMianFormPoint = e.Location;
                mainFormMouseDown = true;
            }
        }

        private void tP_Main_MouseUp(object sender, MouseEventArgs e)
        {
            mainFormMouseDown = false;
        }

        private void tP_Main_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Right && mainFormMouseDown)
                {
                    mainFormMovingDistance += Math.Sqrt(Math.Pow(e.Location.X - lastMianFormPoint.X, 2) + Math.Pow(e.Location.Y - lastMianFormPoint.Y, 2));
                    lastMianFormPoint = e.Location;

                    if (mainFormMovingDistance >= mainFormHideMovingDistance)
                    {
                        mainFormMovingDistance = 0;
                        this.Hide();
                    }
                }
            }
            catch { }
        }

        private void Update_Main()
        {
            button_Main_Hide.Enabled = localData.SimulateMode || localData.LoginLevel == EnumLoginLevel.MirleAdmin;

            #region Middler Data.
            if (mainFlow.MiddlerContorlHandler == null)
            {
                label_Main_MiddlerStatus.Text = GetStringByTag(EnumProfaceStringTag.Middler.ToString());
                label_Main_MiddlerStatusValue.Text = GetStringByTag(EnumProfaceStringTag.InitialFail.ToString());
                label_Main_MiddlerStatusValue.ForeColor = string_Normal;
                label_Main_MiddlerStatusValue.BackColor = backColor_Warning;
            }
            else
            {
                string middlerCommandID =
                label_Main_MiddlerCommandValue.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.ID.ToString()),
                                                               " = ", mainFlow.MiddlerContorlHandler.Vehicle.TransferCommand.CommandId,
                                                               " / ", GetStringByTag(EnumProfaceStringTag.Step.ToString()),
                                                               " = ", mainFlow.MiddlerContorlHandler.Vehicle.TransferCommand.TransferStep.ToString());

                label_Main_MiddlerStatus.Text = GetStringByTag(EnumProfaceStringTag.AGVC.ToString());

                if (localData.AGVCOnline)
                {
                    label_Main_MiddlerStatusValue.Text = GetStringByTag(EnumProfaceStringTag.Online.ToString());
                    label_Main_MiddlerStatusValue.ForeColor = string_Select;
                    label_Main_MiddlerStatusValue.BackColor = backColor_Select;
                }
                else
                {
                    label_Main_MiddlerStatusValue.Text = GetStringByTag(EnumProfaceStringTag.Offline.ToString());
                    label_Main_MiddlerStatusValue.ForeColor = string_Normal;
                    label_Main_MiddlerStatusValue.BackColor = backColor_Warning;
                }
            }
            #endregion

            label_Main_DateTime.Text = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            LoadUnloadCommandData loadUnloadCommand = localData.LoadUnloadData.LoadUnloadCommand;

            if (loadUnloadCommand == null)
                label_Main_ProgramVersionValue.Text = GetStringByTag(EnumProfaceStringTag.無命令.ToString());
            else
                label_Main_ProgramVersionValue.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.命令開始時間.ToString()),
                                                                 " : ", loadUnloadCommand.CommandStartTime.ToString("HH:mm:ss"));

            switch (localData.MainFlowConfig.AGVType)
            {
                case EnumAGVType.ATS:
                    if (localData.LoadUnloadData.Loading_Left)
                    {
                        label_Main_LoadingValue_Left.Text = GetStringByTag(EnumProfaceStringTag.有貨.ToString());
                        label_Main_CassetteIDValue_Left.Text = localData.LoadUnloadData.CstID_Left;
                    }
                    else
                    {
                        label_Main_LoadingValue_Left.Text = GetStringByTag(EnumProfaceStringTag.無貨.ToString());
                        label_Main_CassetteIDValue_Left.Text = "";
                    }

                    if (localData.LoadUnloadData.Loading_Right)
                    {
                        label_Main_LoadingValue_Right.Text = GetStringByTag(EnumProfaceStringTag.有貨.ToString());
                        label_Main_CassetteIDValue_Right.Text = localData.LoadUnloadData.CstID_Right;
                    }
                    else
                    {
                        label_Main_LoadingValue_Right.Text = GetStringByTag(EnumProfaceStringTag.無貨.ToString());
                        label_Main_CassetteIDValue_Right.Text = "";
                    }
                    break;
                default:
            if (localData.LoadUnloadData.Loading)
            {
                label_Main_LoadingValue.Text = GetStringByTag(EnumProfaceStringTag.有貨.ToString());
                label_Main_CassetteIDValue.Text = localData.LoadUnloadData.CstID;
            }
            else
            {
                label_Main_LoadingValue.Text = GetStringByTag(EnumProfaceStringTag.無貨.ToString());
                label_Main_CassetteIDValue.Text = "";
            }
                    break;
            }

            MoveCommandData moveCommand = localData.MoveControlData.MoveCommand;

            if (moveCommand == null)
                label_Main_MoveCommandValue.Text = GetStringByTag(EnumProfaceStringTag.無命令.ToString());
            else
                label_Main_MoveCommandValue.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.命令開始時間.ToString()),
                                                            " : ", moveCommand.StartTime.ToString("HH:mm:ss"));

            label_Main_AreaSensorDirectionValue.Text = GetStringByTag(localData.MIPCData.AreaSensorDirection.ToString());
            label_Main_ChargingValue.Text = (localData.MIPCData.Charging ? GetStringByTag(EnumProfaceStringTag.充電中.ToString()) : "");
        }
        #endregion

        #region Move-Select.
        private bool inMapPage_ByMapButton = true;

        private void Initial_Move()
        {
            Initial_Move_Jog();
            Initial_Move_Map();
            Initial_Move_AxisData();
            Initial_Move_DataInfo();
            Initial_Move_LocateDriver();
            Initial_Move_CommandRecord();
            Initial_Move_SetSlamPosition();
        }

        private void button_MoveControl_Jog_Click(object sender, EventArgs e)
        {
            ChageTabControlByPageIndex((int)EnumProfacePageIndex.Move_Jog);
        }

        private void button_MoveControl_Map_Click(object sender, EventArgs e)
        {
            inMapPage_ByMapButton = true;
            ChageTabControlByPageIndex((int)EnumProfacePageIndex.Move_Map);
        }

        private void button_Move_SetAddressPosition_Click(object sender, EventArgs e)
        {
            inMapPage_ByMapButton = false;
            ChageTabControlByPageIndex((int)EnumProfacePageIndex.Move_Map);
        }

        private void button_MoveControl_SensorData_Click(object sender, EventArgs e)
        {
            ChageTabControlByPageIndex((int)EnumProfacePageIndex.Move_DataInfo);
        }

        private void button_MoveControl_AxisData_Click(object sender, EventArgs e)
        {
            ChageTabControlByPageIndex((int)EnumProfacePageIndex.Move_AxisData);
        }

        private void button_MoveControl_LocateControl_Click(object sender, EventArgs e)
        {
            ChageTabControlByPageIndex((int)EnumProfacePageIndex.Move_LocateDriver);
        }

        private void button_MoveControl_CommandRecord_Click(object sender, EventArgs e)
        {
            ChageTabControlByPageIndex((int)EnumProfacePageIndex.Move_CommandRecord);
        }

        private void button_MoveControl_SLAMSetPosition_Click(object sender, EventArgs e)
        {
            ChageTabControlByPageIndex((int)EnumProfacePageIndex.Move_SetSlamPosition);
        }

        private void Update_Move_Select()
        {
            button_Move_SetAddressPosition.Enabled = localData.LoginLevel == EnumLoginLevel.MirleAdmin;
        }
        #endregion

        #region Move-Jog.
        private void Initial_Move_Jog()
        {
        }

        private void button_Move_Jog_JoystickEnable_Click(object sender, EventArgs e)
        {
            HideAll();

            if (mainFlow.ActionCanUse(EnumUserAction.Move_Jog))
                mainFlow.MipcControl.JogjoystickOnOff(true);
        }

        private void button_Move_Jog_JoystickDisable_Click(object sender, EventArgs e)
        {
            HideAll();

            if (mainFlow.ActionCanUse(EnumUserAction.Move_Jog))
                mainFlow.MipcControl.JogjoystickOnOff(false);
        }

        private void button_JogPitch_Set_Click(object sender, EventArgs e)
        {
            button_Move_Jog_Set.Enabled = false;

            #region 取得 線/角 速度/加速度/減速度.
            HideAll();
            double lineVelocity;
            double lineAcc;
            double lineDec;

            double thetaVelocity;
            double thetaAcc;
            double thetaDec;

            if (!double.TryParse(tB_Move_Jog_LineVelocity.Text, out lineVelocity) || lineVelocity <= 0)
                lineVelocity = -1;

            if (!double.TryParse(tB_Move_Jog_LineAcc.Text, out lineAcc) || lineAcc <= 0)
                lineAcc = -1;

            if (!double.TryParse(tB_Move_Jog_LineDec.Text, out lineDec) || lineDec <= 0)
                lineDec = -1;

            if (!double.TryParse(tB_Move_Jog_ThetaVelocity.Text, out thetaVelocity) || thetaVelocity <= 0)
                thetaVelocity = -1;

            if (!double.TryParse(tB_Move_Jog_ThetaAcc.Text, out thetaAcc) || thetaAcc <= 0)
                thetaAcc = -1;

            if (!double.TryParse(tB_Move_Jog_ThetaDec.Text, out thetaDec) || thetaDec <= 0)
                thetaDec = -1;
            #endregion

            mainFlow.MipcControl.WriteJogjoystickData(lineVelocity, lineAcc, lineDec, thetaVelocity, thetaAcc, thetaDec);

            button_Move_Jog_Set.Enabled = true;
        }

        private void button_JogPitch_Clear_Click(object sender, EventArgs e)
        {
            HideAll();

            tB_Move_Jog_LineVelocity.Text = "";
            tB_Move_Jog_LineAcc.Text = "";
            tB_Move_Jog_LineDec.Text = "";
            tB_Move_Jog_ThetaVelocity.Text = "";
            tB_Move_Jog_ThetaAcc.Text = "";
            tB_Move_Jog_ThetaDec.Text = "";
        }

        private void button_MoveJog_ReviseByTarget_Click(object sender, EventArgs e)
        {
            HideAll();

            if (mainFlow.ActionCanUse(EnumUserAction.Move_SpecialFlow_ReviseByTargetOrLocateData))
            {
                VehicleLocation now = localData.Location;
                MapSection section;

                if (now != null && localData.TheMapInfo.AllSection.ContainsKey(now.NowSection))
                {
                    section = localData.TheMapInfo.AllSection[now.NowSection];

                    if ((section.FromAddress.ChargingDirection != EnumStageDirection.None || section.FromAddress.LoadUnloadDirection != EnumStageDirection.None) &&
                        computeFunction.CheckTwoMapAGVPositionDistanceAndAngle(localData.Real, section.FromAddress.AGVPosition, 100, 3))
                        mainFlow.MoveControl.SpecailFlow_ReviseByTargetOrLocateData(section.FromAddress.Id);
                    else if ((section.ToAddress.ChargingDirection != EnumStageDirection.None || section.ToAddress.LoadUnloadDirection != EnumStageDirection.None) &&
                              computeFunction.CheckTwoMapAGVPositionDistanceAndAngle(localData.Real, section.ToAddress.AGVPosition, 100, 3))
                        mainFlow.MoveControl.SpecailFlow_ReviseByTargetOrLocateData(section.ToAddress.Id);
                }
            }
        }

        private void button_Move_Jog_MoveToSectionCenter_Click(object sender, EventArgs e)
        {
            HideAll();

            if (mainFlow.ActionCanUse(EnumUserAction.Move_SpecialFlow_ToSectionCenter))
                mainFlow.MoveControl.SpecailFlow_MoveToSectionCenter();
        }

        private void button_MoveControlMoveToAddressStatusStop_Click(object sender, EventArgs e)
        {
            HideAll();
            mainFlow.MoveControl.SpecialFlowStop = true;
        }

        private void Update_Move_Jog()
        {
            #region 設定 加速度/減速度.
            if (mainFlow.ActionCanUse(EnumUserAction.Move_Jog_SettingAccDec))
            {
                tB_Move_Jog_LineAcc.Enabled = true;
                tB_Move_Jog_LineDec.Enabled = true;
                tB_Move_Jog_ThetaAcc.Enabled = true;
                tB_Move_Jog_ThetaDec.Enabled = true;
            }
            else
            {
                tB_Move_Jog_LineAcc.Enabled = false;
                tB_Move_Jog_LineDec.Enabled = false;
                tB_Move_Jog_ThetaAcc.Enabled = false;
                tB_Move_Jog_ThetaDec.Enabled = false;
            }
            #endregion

            #region 特殊流程.
            if (localData.MoveControlData.SpecialFlow)
            {
                label_Move_Jog_SpecialFlowStatusValue.Text = GetStringByTag(EnumProfaceStringTag.移動中.ToString());
                button_Move_Jog_SpecialFlowStop.Enabled = true;
            }
            else
            {
                label_Move_Jog_SpecialFlowStatusValue.Text = GetStringByTag(EnumProfaceStringTag.停止.ToString());
                button_Move_Jog_SpecialFlowStop.Enabled = false;
            }

            button_Move_Jog_ReviseByTarget.Enabled = mainFlow.ActionCanUse(EnumUserAction.Move_SpecialFlow_ReviseByTargetOrLocateData);
            button_Move_Jog_MoveToSectionCenter.Enabled = mainFlow.ActionCanUse(EnumUserAction.Move_SpecialFlow_ToSectionCenter);
            #endregion

            #region 是否可以使用搖桿.
            label_Move_Jog_CantUsingJoyReson.Visible = !mainFlow.ActionCanUse(EnumUserAction.Move_Jog);

            if (localData.AutoManual != EnumAutoState.Manual)
                label_Move_Jog_CantUsingJoyReson.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.無法使用搖桿.ToString()), " : ", GetStringByTag(EnumProfaceStringTag.Auto中.ToString()));
            else if (localData.MoveControlData.MoveCommand != null)
                label_Move_Jog_CantUsingJoyReson.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.無法使用搖桿.ToString()), " : ", GetStringByTag(EnumProfaceStringTag.移動命令中.ToString()));
            else if (localData.LoadUnloadData.LoadUnloadCommand != null)
                label_Move_Jog_CantUsingJoyReson.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.無法使用搖桿.ToString()), " : ", GetStringByTag(EnumProfaceStringTag.取放命令中.ToString()));
            else if (localData.MoveControlData.MoveControlConfig.SensorByPass[EnumSensorSafetyType.ChargingCheck] && localData.MIPCData.Charging)
                label_Move_Jog_CantUsingJoyReson.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.無法使用搖桿.ToString()), " : ", GetStringByTag(EnumProfaceStringTag.充電中.ToString()));
            else if (localData.MoveControlData.SpecialFlow)
                label_Move_Jog_CantUsingJoyReson.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.無法使用搖桿.ToString()), " : ", GetStringByTag(EnumProfaceStringTag.特殊走行流程中.ToString()));
            else if (localData.MoveControlData.MoveControlConfig.SensorByPass[EnumSensorSafetyType.ForkHomeCheck] && !localData.LoadUnloadData.Ready)
                label_Move_Jog_CantUsingJoyReson.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.無法使用搖桿.ToString()), " : ", GetStringByTag(EnumProfaceStringTag.Fork_Not_Home.ToString()));

            if (localData.MoveControlData.MotionControlData.JoystickMode)
            {
                label_Move_Jog_JoystickValue.Text = GetStringByTag(EnumProfaceStringTag.開啟中.ToString());
                label_Move_Jog_JoystickValue.ForeColor = backColor_Select;
            }
            else
            {
                label_Move_Jog_JoystickValue.Text = GetStringByTag(EnumProfaceStringTag.關閉中.ToString());
                label_Move_Jog_JoystickValue.ForeColor = backColor_Warning;
            }
            #endregion

            #region Servo on/off 顯示.
            pB_Move_Jog_AllServoOn.BackColor = (mainFlow.MoveControl.MotionControl.AllServoOn ? backColor_Select : Color.Transparent);
            pB_Move_Jog_AllServoOff.BackColor = (mainFlow.MoveControl.MotionControl.AllServoOff ? backColor_Warning : Color.Transparent);
            #endregion

            #region 線/角 速度/加速度/減速度 顯示.
            label_Move_Jog_LineVelocityValue.Text = localData.MoveControlData.MotionControlData.JoystickLineAxisData.Velocity.ToString("0");
            label_Move_Jog_LineAccValue.Text = localData.MoveControlData.MotionControlData.JoystickLineAxisData.Acceleration.ToString("0");
            label_Move_Jog_LineDecValue.Text = localData.MoveControlData.MotionControlData.JoystickLineAxisData.Deceleration.ToString("0");

            label_Move_Jog_ThetaVelocityValue.Text = localData.MoveControlData.MotionControlData.JoystickThetaAxisData.Velocity.ToString("0");
            label_Move_Jog_ThetaAccValue.Text = localData.MoveControlData.MotionControlData.JoystickThetaAxisData.Acceleration.ToString("0");
            label_Move_Jog_ThetaDecValue.Text = localData.MoveControlData.MotionControlData.JoystickThetaAxisData.Deceleration.ToString("0");
            #endregion

            #region 軌道偏差/角度偏差 顯示.
            ThetaSectionDeviation temp = localData.MoveControlData.ThetaSectionDeviation;

            if (temp != null)
            {
                label_Move_Jog_SectionDeviationValue.Text = temp.SectionDeviation.ToString("0.0");
                label_Move_Jog_SectionThetaValue.Text = temp.Theta.ToString("0.0");

                if (Math.Abs(temp.SectionDeviation) <= localData.MoveControlData.MoveControlConfig.Safety[EnumMoveControlSafetyType.OntimeReviseSectionDeviationLine].Range / 2)
                    label_Move_Jog_SectionDeviationValue.ForeColor = Color.Green;
                else
                    label_Move_Jog_SectionDeviationValue.ForeColor = Color.Red;

                if (Math.Abs(temp.Theta) <= localData.MoveControlData.MoveControlConfig.Safety[EnumMoveControlSafetyType.OntimeReviseTheta].Range / 2)
                    label_Move_Jog_SectionThetaValue.ForeColor = Color.Green;
                else
                    label_Move_Jog_SectionThetaValue.ForeColor = Color.Red;
            }
            else
            {
                label_Move_Jog_SectionDeviationValue.Text = "---";
                label_Move_Jog_SectionDeviationValue.ForeColor = Color.Red;
                label_Move_Jog_SectionThetaValue.Text = "---";
                label_Move_Jog_SectionThetaValue.ForeColor = Color.Red;
            }

            if (localData.MoveControlData.MoveControlCanAuto)
            {
                label_Move_Jog_CanAuto.Text = GetStringByTag(EnumProfaceStringTag.可以Auto);
                label_Move_Jog_CanAuto.ForeColor = Color.Green;
                label_Move_Jog_CantAutoReason.Text = "";
            }
            else
            {
                label_Move_Jog_CanAuto.Text = GetStringByTag(EnumProfaceStringTag.不能Auto);
                label_Move_Jog_CanAuto.ForeColor = Color.Red;
                label_Move_Jog_CantAutoReason.Text = GetStringByTag(localData.MoveControlData.MoveControlCantAutoReason);
            }

            label_Move_Jog_RealPositionValue.Text = computeFunction.GetMapAGVPositionStringWithAngle(localData.Real);
            #endregion
        }
        #endregion

        #region Move-Map.
        private PictureBox load;
        private PictureBox unload;
        private PictureBox moveP;

        private PictureBox agv_User;


        private MapAddress loadAddress = null;
        private MapAddress unloadAddress = null;
        private MapAddress moveA = null;

        private PictureBox agv_MirleAdmin;

        private void Initial_Move_Map()
        {
            panel_Move_Map.AutoScroll = true;

            pB_Move_Map.Size = mainForm.GetMapSize;
            pB_Move_Map.Image = mainForm.GetMapImage;

            InitialLoadUnloadPictureBox();
            pB_Move_Map.MouseDown += pB_MoveControlMap_MouseDown;
            pB_Move_Map.MouseUp += pB_MoveControlMap_MouseUp;
            pB_Move_Map.MouseMove += pB_MoveControlMap_MouseMove;
            PictureBox temp;

            #region 讓ChargingStation 和 Port 在上面.
            foreach (AddressPicture picture in mainForm.GetAllAddressPicture.Values)
            {
                if (localData.TheMapInfo.AllAddress[picture.AddressID].ChargingDirection == EnumStageDirection.None &&
                    localData.TheMapInfo.AllAddress[picture.AddressID].LoadUnloadDirection == EnumStageDirection.None)
                {
                }
                else
                {
                    temp = new PictureBox();
                    temp.Size = picture.Size;
                    temp.Image = picture.Bitmap_Address;
                    temp.Location = picture.Location;
                    temp.BackColor = Color.White;
                    pB_Move_Map.Controls.Add(temp);
                    allAddressPicture.Add(picture.AddressID, temp);
                    temp.MouseClick += Temp_MouseClick;
                }
            }

            foreach (AddressPicture picture in mainForm.GetAllAddressPicture.Values)
            {
                if (localData.TheMapInfo.AllAddress[picture.AddressID].ChargingDirection == EnumStageDirection.None &&
                    localData.TheMapInfo.AllAddress[picture.AddressID].LoadUnloadDirection == EnumStageDirection.None)
                {
                    temp = new PictureBox();
                    temp.Size = picture.Size;
                    temp.Image = picture.Bitmap_Address;
                    temp.Location = picture.Location;
                    temp.BackColor = Color.White;
                    pB_Move_Map.Controls.Add(temp);
                    allAddressPicture.Add(picture.AddressID, temp);
                    temp.MouseClick += Temp_MouseClick;
                }
            }
            #endregion
        }

        private void HidePictureBox(PictureBox pB)
        {
            pB.Location = new Point(-pB.Size.Width, -pB.Size.Height);
        }

        private void InitialLoadUnloadPictureBox()
        {
            #region AGV-MirleAdmin.
            agv_MirleAdmin = new PictureBox();
            agv_MirleAdmin.Size = new Size(9, 9);
            agv_MirleAdmin.BackColor = Color.Green;
            pB_Move_Map.Controls.Add(agv_MirleAdmin);
            agv_MirleAdmin.MouseClick += Agv_MouseClick;

            HidePictureBox(agv_MirleAdmin);
            agv_MirleAdmin.Location = new Point(-agv_MirleAdmin.Size.Width, -agv_MirleAdmin.Size.Height);
            #endregion

            #region Load-Picture.
            load = new PictureBox();
            load.Size = new Size(20, 20);
            Bitmap loadBitmap = new Bitmap(20, 20);
            Graphics loadGraphics = Graphics.FromImage(loadBitmap);
            loadGraphics.Clear(Color.Red);

            Font font = new Font("微軟正黑體", 12, System.Drawing.FontStyle.Bold);
            SizeF s = loadGraphics.MeasureString("L", font);

            float x = 10 - s.Width / 2;
            float y = 10 - s.Height / 2;

            loadGraphics.DrawString("L", font, Brushes.Black, new PointF(x, y));
            load.Image = loadBitmap;

            load.BorderStyle = BorderStyle.FixedSingle;
            pB_Move_Map.Controls.Add(load);
            HidePictureBox(load);
            #endregion

            #region Unload-Picture.
            unload = new PictureBox();
            unload.Size = new Size(20, 20);
            Bitmap unloadBitmap = new Bitmap(20, 20);
            Graphics unloadGraphics = Graphics.FromImage(unloadBitmap);
            unloadGraphics.Clear(Color.Red);

            font = new Font("微軟正黑體", 12, System.Drawing.FontStyle.Bold);
            s = unloadGraphics.MeasureString("U", font);

            x = 10 - s.Width / 2;
            y = 10 - s.Height / 2;

            unloadGraphics.DrawString("U", font, Brushes.Black, new PointF(x, y));
            unload.Image = unloadBitmap;

            unload.BorderStyle = BorderStyle.FixedSingle;
            pB_Move_Map.Controls.Add(unload);
            HidePictureBox(unload);
            #endregion

            #region Move-Picture.
            moveP = new PictureBox();
            moveP.Size = new Size(20, 20);
            Bitmap moveBitmap = new Bitmap(20, 20);
            Graphics moveGraphics = Graphics.FromImage(moveBitmap);
            moveGraphics.Clear(Color.Red);

            font = new Font("微軟正黑體", 12, System.Drawing.FontStyle.Bold);
            s = moveGraphics.MeasureString("M", font);

            x = 10 - s.Width / 2;
            y = 10 - s.Height / 2;

            moveGraphics.DrawString("M", font, Brushes.Black, new PointF(x, y));
            moveP.Image = moveBitmap;

            moveP.BorderStyle = BorderStyle.FixedSingle;
            pB_Move_Map.Controls.Add(moveP);
            HidePictureBox(moveP);
            #endregion

            #region AGV-User.
            agv_User = new PictureBox();
            agv_User.Size = Mirle.Agv.INX.Properties.Resources.VehHasNoCarrier.Size;
            agv_User.Image = Mirle.Agv.INX.Properties.Resources.VehHasNoCarrier;
            pB_Move_Map.Controls.Add(agv_User);
            agv_User.MouseClick += Agv_MouseClick;
            HidePictureBox(agv_User);
            #endregion

            agvcActionToLabel.Add(EnumAGVCActionType.Move, label_Move_Map_Action_Move);
            agvcActionToLabel.Add(EnumAGVCActionType.Load, label_Move_Map_Action_Load);
            agvcActionToLabel.Add(EnumAGVCActionType.Unload, label_Move_Map_Action_Unload);
            agvcActionToLabel.Add(EnumAGVCActionType.LoadUnload, label_Move_Map_Action_LoadUnload);
            agvcAction = EnumAGVCActionType.Move;
            SetLabelInSelected(agvcActionToLabel[agvcAction], true);
        }

        private void Temp_MouseClick(object sender, MouseEventArgs e)
        {
            Point newLocate = new Point(e.X + ((PictureBox)sender).Location.X,
                                        e.Y + ((PictureBox)sender).Location.Y);

            MapMouseClickEvent(newLocate);
        }

        #region Map-命令.
        private Dictionary<EnumAGVCActionType, Label> agvcActionToLabel = new Dictionary<EnumAGVCActionType, Label>();
        private EnumAGVCActionType agvcAction = EnumAGVCActionType.Move;

        private void button_MoveMap_LocalFunction_Click(object sender, EventArgs e)
        {
            HideAll();
            panel_Move_Map_Function.Visible = true;
        }

        private void button_MoveMap_FunctionHide_Click(object sender, EventArgs e)
        {
            HideAll();
            ClearLoadUnloadTag();
            panel_Move_Map_Function.Visible = false;
        }

        private void ChangeAGVCFunction(EnumAGVCActionType newAction)
        {
            HideAll();

            if (agvcAction != newAction)
            {
                ClearLoadUnloadTag();
                SetLabelInSelected(agvcActionToLabel[agvcAction], false);
                agvcAction = newAction;
                SetLabelInSelected(agvcActionToLabel[agvcAction], true);
            }
        }

        private void label_Move_Map_Action_Move_Click(object sender, EventArgs e)
        {
            ChangeAGVCFunction(EnumAGVCActionType.Move);
        }

        private void label_Move_Map_Action_Load_Click(object sender, EventArgs e)
        {
            ChangeAGVCFunction(EnumAGVCActionType.Load);
        }

        private void label_Move_Map_Action_Unload_Click(object sender, EventArgs e)
        {
            ChangeAGVCFunction(EnumAGVCActionType.Unload);
        }

        private void label_Move_Map_Action_LoadUnload_Click(object sender, EventArgs e)
        {
            ChangeAGVCFunction(EnumAGVCActionType.LoadUnload);
        }

        private void SetMove(string addressID)
        {
            if (localData.TheMapInfo.AllAddress.ContainsKey(addressID))
            {
                moveA = localData.TheMapInfo.AllAddress[addressID];
                moveP.Location = new Point(
                    (int)(mainForm.GetDrawMapData.TransferX(localData.TheMapInfo.AllAddress[addressID].AGVPosition.Position.X) - moveP.Size.Width / 2),
                    (int)(mainForm.GetDrawMapData.TransferY(localData.TheMapInfo.AllAddress[addressID].AGVPosition.Position.Y) - moveP.Size.Height / 2));
            }
        }

        private void ResetMove()
        {
            moveA = null;
            moveP.Location = new Point(0, 0);
            HidePictureBox(moveP);
        }

        private void SetLoad(string addressID)
        {
            if (localData.TheMapInfo.IsPort(addressID))
            {
                loadAddress = localData.TheMapInfo.AllAddress[addressID];
                load.Location = new Point(
                    (int)(mainForm.GetDrawMapData.TransferX(localData.TheMapInfo.AllAddress[addressID].AGVPosition.Position.X) - load.Size.Width / 2),
                    (int)(mainForm.GetDrawMapData.TransferY(localData.TheMapInfo.AllAddress[addressID].AGVPosition.Position.Y) - load.Size.Height / 2));
            }
        }

        private void ResetLoad()
        {
            loadAddress = null;
            load.Location = new Point(0, 0);
            HidePictureBox(load);
        }

        private void SetUnload(string addressID)
        {
            if (localData.TheMapInfo.IsPort(addressID))
            {
                unloadAddress = localData.TheMapInfo.AllAddress[addressID];
                unload.Location = new Point(
                    (int)(mainForm.GetDrawMapData.TransferX(localData.TheMapInfo.AllAddress[addressID].AGVPosition.Position.X) - unload.Size.Width / 2),
                    (int)(mainForm.GetDrawMapData.TransferY(localData.TheMapInfo.AllAddress[addressID].AGVPosition.Position.Y) - unload.Size.Height / 2));
            }
        }

        private void ResetUnload()
        {
            unloadAddress = null;
            unload.Location = new Point(0, 0);
            HidePictureBox(unload);
        }

        private Thread agvcFunctionThread = null;

        private EnumAGVCActionType runAction = EnumAGVCActionType.Move;
        private string runActionAddress1 = "";
        private string runActionAddress2 = "";

        #region 類AGVC命令執行.
        private void AGVCFunctionActionThread()
        {
            try
            {
                if (localData.MIPCData.Charging)
                {
                    mainFlow.MipcControl.StopCharging();

                    Stopwatch waitStopChargingTimer = new Stopwatch();
                    waitStopChargingTimer.Restart();

                    while (localData.MIPCData.Charging)
                    {
                        if (waitStopChargingTimer.ElapsedMilliseconds > 60 * 1000)
                            return;
                        else if (mainFlow.LocalTestCommandStop)
                            return;

                        Thread.Sleep(1000);
                    }
                }

                if (!mainFlow.MoveByAddressID_ManualTest(runActionAddress1))
                    return;

                switch (runAction)
                {
                    case EnumAGVCActionType.Move:
                        if (localData.TheMapInfo.AllAddress[runActionAddress1].ChargingDirection != EnumStageDirection.None &&
                           localData.BatteryInfo.Battery_SOC < autoCycleRunNeedChargingSOC_Move)
                            mainFlow.MipcControl.StartCharging(localData.TheMapInfo.AllAddress[runActionAddress1].ChargingDirection);
                        return;
                    case EnumAGVCActionType.Load:
                        mainFlow.ForByAddressIDAndAction_ManualTest(runActionAddress1, EnumLoadUnload.Load);
                        return;
                    case EnumAGVCActionType.Unload:
                        mainFlow.ForByAddressIDAndAction_ManualTest(runActionAddress1, EnumLoadUnload.Unload);
                        return;
                    case EnumAGVCActionType.LoadUnload:
                        if (!mainFlow.ForByAddressIDAndAction_ManualTest(runActionAddress1, EnumLoadUnload.Load))
                            return;
                        break;
                }

                if (!mainFlow.MoveByAddressID_ManualTest(runActionAddress2))
                    return;

                mainFlow.ForByAddressIDAndAction_ManualTest(runActionAddress2, EnumLoadUnload.Unload);
            }
            catch
            {
                mainFlow.MoveControl.VehicleStop();
            }
        }
        #endregion

        #region 類AGVC 命令 設定/確認可以執行.
        private void button_Move_Map_FunctionStart_Click(object sender, EventArgs e)
        {
            HideAll();

            if (mainFlow.ActionCanUse(EnumUserAction.Move_LocalCommand))
            {
                if (agvcFunctionThread == null || !agvcFunctionThread.IsAlive)
                {
                    bool dataOK = false;
                    runAction = agvcAction;

                    switch (agvcAction)
                    {
                        case EnumAGVCActionType.Move:
                            if (moveA != null)
                            {
                                runActionAddress1 = moveA.Id;
                                dataOK = true;
                            }

                            break;
                        case EnumAGVCActionType.Load:
                            if (loadAddress != null && !localData.LoadUnloadData.Loading)
                            {
                                runActionAddress1 = loadAddress.Id;
                                dataOK = true;
                            }

                            break;
                        case EnumAGVCActionType.Unload:
                            if (unloadAddress != null && localData.LoadUnloadData.Loading)
                            {
                                runActionAddress1 = unloadAddress.Id;
                                dataOK = true;
                            }
                            break;
                        case EnumAGVCActionType.LoadUnload:
                            if (loadAddress != null && unloadAddress != null && !localData.LoadUnloadData.Loading)
                            {
                                runActionAddress1 = loadAddress.Id;
                                runActionAddress2 = unloadAddress.Id;
                                dataOK = true;
                            }
                            break;
                    }

                    if (dataOK)
                    {
                        mainFlow.LocalTestCommandStop = false;
                        agvcFunctionThread = new Thread(AGVCFunctionActionThread);
                        agvcFunctionThread.Start();
                    }
                }
            }
        }
        #endregion

        private void button_Move_Map_FunctionStop_Click(object sender, EventArgs e)
        {
            HideAll();

            if (localData.AutoManual == EnumAutoState.Manual && agvcFunctionThread != null && agvcFunctionThread.IsAlive)
                mainFlow.LocalTestCommandStop = true;
        }
        #endregion

        #region Map-踩點.
        private void button_Move_Map_PanelVisableChange_Click(object sender, EventArgs e)
        {
            HideAll();
            panel_Move_Map_MoveByTarget.Visible = !panel_Move_Map_MoveByTarget.Visible;
        }

        private void button_Move_Map_SearchAddressIDList_Click(object sender, EventArgs e)
        {
            HideAll();

            if (!mainFlow.ActionCanUse(EnumUserAction.Move_SetSlamAddressPosition))
                return;

            button_Move_Map_SearchAddressIDList.Enabled = false;

            tB_JogPitch_SetSlamPositionAddressValue.Text = "";
            double searchDistance = 0;
            double searchAngle = 0;

            if (!double.TryParse(tB_Move_Map_SearchDistanceRange.Text, out searchDistance) || searchDistance < 0)
                searchDistance = 200;

            if (!double.TryParse(tBl_Move_Map_SearchAngleRange.Text, out searchAngle) || searchAngle < 0)
                searchAngle = 5;

            MapAGVPosition now = localData.Real;

            cB_Move_Map_AddressIDList.Items.Clear();

            if (now == null)
            {
                button_Move_Map_SearchAddressIDList.Enabled = true;
                return;
            }

            foreach (MapAddress address in localData.TheMapInfo.AllAddress.Values)
            {
                if (Math.Abs(computeFunction.GetCurrectAngle(address.AGVPosition.Angle - now.Angle)) < searchAngle &&
                    computeFunction.GetDistanceFormTwoAGVPosition(address.AGVPosition, now) < searchDistance)
                {
                    cB_Move_Map_AddressIDList.Items.Add(address.Id);
                }
            }

            if (cB_Move_Map_AddressIDList.Items.Count > 0)
                cB_Move_Map_AddressIDList.SelectedIndex = 0;

            string cBString = cB_Move_Map_AddressIDList.Text;

            tB_JogPitch_SetSlamPositionAddressValue.Text = localData.TheMapInfo.AllAddress.ContainsKey(cBString) ? cBString : "";

            button_Move_Map_SearchAddressIDList.Enabled = true;
        }

        private void cB_Move_Map_AddressIDList_SelectedIndexChanged(object sender, EventArgs e)
        {
            string cBString = cB_Move_Map_AddressIDList.Text;
            tB_JogPitch_SetSlamPositionAddressValue.Text = localData.TheMapInfo.AllAddress.ContainsKey(cBString) ? cBString : "";
        }

        private void button_Move_Map_SetSlamPositionByAddressID_Click(object sender, EventArgs e)
        {
            HideAll();

            if (!mainFlow.ActionCanUse(EnumUserAction.Move_SetSlamAddressPosition))
                return;

            button_Move_Map_SetSlamPositionByAddressID.Enabled = false;

            string targetAddressID = tB_JogPitch_SetSlamPositionAddressValue.Text;

            if (localData.TheMapInfo.AllAddress.ContainsKey(targetAddressID))
            {
                tB_JogPitch_SetSlamPositionAddressValue.Text = "";

                cB_Move_Map_AddressIDList.Items.Clear();
                cB_Move_Map_AddressIDList.Text = "";
                mainFlow.MoveControl.LocateControl.SetSlamPositionAndReplace(targetAddressID, 10);
                mainFlow.MoveControl.LocateControl.WriteSlamPositionAll();
            }

            button_Move_Map_SetSlamPositionByAddressID.Enabled = true;
        }

        private void button_Move_Map_AutoMove_Click(object sender, EventArgs e)
        {
            HideAll();

            string targetAddressID = tB_JogPitch_SetSlamPositionAddressValue.Text;

            if (mainFlow.ActionCanUse(EnumUserAction.Move_SpecialFlow_ReviseByTarget))
                mainFlow.MoveControl.SpecailFlow_ReviseByTarget(targetAddressID);
        }

        private void button_Move_Map_AutoMoveStop_Click(object sender, EventArgs e)
        {
            HideAll();
            mainFlow.MoveControl.SpecialFlowStop = true;
        }
        #endregion

        #region Map-移動畫面.
        private Point moveMapLocateStart = new Point(0, 0);
        private bool mouseDown = false;

        private void pB_MoveControlMap_MouseDown(object sender, MouseEventArgs e)
        {
            HideAll();

            if (e.Button == MouseButtons.Left)
            {
                moveMapLocateStart = new Point(e.Location.X, e.Location.Y);
                mouseDown = true;
            }

            MapMouseClickEvent(e.Location);
        }

        private void pB_MoveControlMap_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                mouseDown = false;
        }

        private void pB_MoveControlMap_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown && e.Button == MouseButtons.Left)
            {
                int deltaX = e.Location.X - moveMapLocateStart.X;
                int deltaY = e.Location.Y - moveMapLocateStart.Y;

                panel_Move_Map.AutoScrollPosition =
                    new Point(-panel_Move_Map.AutoScrollPosition.X - deltaX,
                              -panel_Move_Map.AutoScrollPosition.Y - deltaY);
            }
        }
        #endregion

        #region Map-選Address資料.
        private void MapMouseClickEvent(Point location)
        {
            #region Manual狀態下, 去搜尋可能選取的Address.
            if (localData.AutoManual == EnumAutoState.Manual)
            {
                double searchDistance;
                double searchTheta;

                if (!double.TryParse(tB_Move_Map_SearchDistanceRange.Text, out searchDistance) || searchDistance < 0)
                    searchDistance = 200 * 2;
                else
                    searchDistance *= 2;

                if (!double.TryParse(tBl_Move_Map_SearchAngleRange.Text, out searchTheta) || searchTheta < 0)
                    searchTheta = 5;

                MapPosition aTransfer = new MapPosition(mainForm.GetDrawMapData.ATransferX(location.X),
                                                        mainForm.GetDrawMapData.ATransferY(location.Y));

                MapAddress targetAddress = null;

                double tempDistance = -1;
                double minDistance = -1;

                bool nowAddressIsChargOrPort;
                bool minAddressIsChargOrPort;

                bool nowAngleOK;
                bool minAngleOK;

                bool minInRange;
                bool tempInRange;

                MapAGVPosition now = localData.Real;

                foreach (MapAddress address in localData.TheMapInfo.AllAddress.Values)
                {
                    tempDistance = computeFunction.GetDistanceFormTwoPosition(address.AGVPosition.Position, aTransfer);

                    if (tempDistance <= searchDistance)
                    {
                        if (targetAddress == null)
                        {
                            minDistance = tempDistance;
                            targetAddress = address;
                        }
                        else
                        {
                            if (localData.LoginLevel < EnumLoginLevel.MirleAdmin || now == null)
                            {
                                #region 不是最高權限狀態 (目的是From to 或 SetPosition >> Port ChargerStation優先.
                                minAddressIsChargOrPort = (targetAddress.LoadUnloadDirection != EnumStageDirection.None || targetAddress.ChargingDirection != EnumStageDirection.None);
                                nowAddressIsChargOrPort = (address.LoadUnloadDirection != EnumStageDirection.None || address.ChargingDirection != EnumStageDirection.None);

                                if ((minAddressIsChargOrPort && nowAddressIsChargOrPort) ||
                                    (!minAddressIsChargOrPort && nowAddressIsChargOrPort))
                                {
                                    if (tempDistance < minDistance)
                                    {
                                        minDistance = tempDistance;
                                        targetAddress = address;
                                    }
                                }
                                else
                                {
                                    if (nowAddressIsChargOrPort)
                                    {
                                        minDistance = tempDistance;
                                        targetAddress = address;
                                    }
                                }
                                #endregion
                            }
                            else if (now != null)
                            {
                                #region 最高權限.
                                minAngleOK = computeFunction.GetCurrectAngle(now.Angle - targetAddress.AGVPosition.Angle) <= searchTheta;
                                nowAngleOK = computeFunction.GetCurrectAngle(now.Angle - address.AGVPosition.Angle) <= searchTheta;

                                minInRange = computeFunction.GetDistanceFormTwoAGVPosition(now, targetAddress.AGVPosition) <= searchDistance;
                                tempInRange = computeFunction.GetDistanceFormTwoAGVPosition(now, address.AGVPosition) <= searchDistance;

                                if (!minInRange && !tempInRange)
                                {
                                    if (tempDistance < minDistance)
                                    {
                                        minDistance = tempDistance;
                                        targetAddress = address;
                                    }
                                }
                                else if (minInRange && tempInRange)
                                {
                                    if ((minAngleOK && nowAngleOK) ||
                                        (!minAngleOK && !nowAngleOK))
                                    {
                                        if (tempDistance < minDistance)
                                        {
                                            minDistance = tempDistance;
                                            targetAddress = address;
                                        }
                                    }
                                    else
                                    {
                                        if (nowAngleOK)
                                        {
                                            minDistance = tempDistance;
                                            targetAddress = address;
                                        }
                                    }
                                }
                                else if (tempInRange)
                                {
                                    minDistance = tempDistance;
                                    targetAddress = address;
                                }
                                #endregion
                            }
                        }
                    }
                }

                if (targetAddress != null)
                {
                    if (allAddressPicture.ContainsKey(lastChangeColorAddress))
                        allAddressPicture[lastChangeColorAddress].BackColor = Color.White;

                    tB_JogPitch_SetSlamPositionAddressValue.Text = targetAddress.Id;
                    tB_FromToTargetAddressID.Text = targetAddress.Id;

                    if (localData.TheMapInfo.AllAddress.ContainsKey(targetAddress.Id))
                        allAddressPicture[targetAddress.Id].BackColor = Color.Black;

                    lastChangeColorAddress = targetAddress.Id;

                    if (inMapPage_ByMapButton)
                    {
                        switch (agvcAction)
                        {
                            case EnumAGVCActionType.Load:
                                SetLoad(targetAddress.Id);
                                break;
                            case EnumAGVCActionType.Unload:
                                SetUnload(targetAddress.Id);
                                break;
                            case EnumAGVCActionType.LoadUnload:
                                if (loadAddress != null && unloadAddress != null)
                                    ClearLoadUnloadTag();

                                if (loadAddress == null)
                                    SetLoad(targetAddress.Id);
                                else if (unloadAddress == null)
                                    SetUnload(targetAddress.Id);

                                break;
                            default:
                                SetMove(targetAddress.Id);
                                break;
                        }
                    }
                }
            }
            #endregion
        }

        private Dictionary<string, PictureBox> allAddressPicture = new Dictionary<string, PictureBox>();
        private string lastChangeColorAddress = "";

        private void Agv_MouseClick(object sender, MouseEventArgs e)
        {
            Point newLocate = new Point(e.X + ((PictureBox)sender).Location.X,
                                        e.Y + ((PictureBox)sender).Location.Y);

            MapMouseClickEvent(newLocate);
        }

        private void ClearLoadUnloadTag()
        {
            ResetLoad();
            ResetUnload();
            ResetMove();
        }
        #endregion

        private string lastSetSlamPositionAddressID = "";

        private void CheckSetSlamPositionAddress()
        {
            string tBString = tB_JogPitch_SetSlamPositionAddressValue.Text;

            if (cB_Move_Map_AddressIDList.Text != "" && tBString != cB_Move_Map_AddressIDList.Text)
            {
                cB_Move_Map_AddressIDList.Items.Clear();
                cB_Move_Map_AddressIDList.Text = "";
            }

            if (tBString != "")
            {
                if (lastSetSlamPositionAddressID != tBString)
                {
                    lastSetSlamPositionAddressID = tBString;

                    if (localData.TheMapInfo.AllAddress.ContainsKey(lastSetSlamPositionAddressID))
                    {
                        if (allAddressPicture.ContainsKey(lastChangeColorAddress))
                            allAddressPicture[lastChangeColorAddress].BackColor = Color.White;

                        tB_FromToTargetAddressID.Text = lastSetSlamPositionAddressID;

                        if (localData.TheMapInfo.AllAddress.ContainsKey(lastSetSlamPositionAddressID))
                            allAddressPicture[lastSetSlamPositionAddressID].BackColor = Color.Black;

                        lastChangeColorAddress = lastSetSlamPositionAddressID;

                        label_Move_Map_AGVAngleValue.Text = localData.TheMapInfo.AllAddress[lastSetSlamPositionAddressID].AGVPosition.Angle.ToString("0");
                    }
                    else
                        label_Move_Map_AGVAngleValue.Text = "";
                }
            }
        }

        private void UpdateAGV(MapAGVPosition now)
        {
            if (now == null)
            {
                HidePictureBox(agv_MirleAdmin);
                HidePictureBox(agv_User);
            }
            else
            {
                if (localData.LoginLevel == EnumLoginLevel.MirleAdmin)
                {
                    agv_MirleAdmin.Location = new Point((int)(mainForm.GetDrawMapData.TransferX(now.Position.X) - agv_MirleAdmin.Width / 2),
                                                        (int)(mainForm.GetDrawMapData.TransferY(now.Position.Y) - agv_MirleAdmin.Height / 2));
                    HidePictureBox(agv_User);
                }
                else
                {
                    agv_User.Location = new Point((int)(mainForm.GetDrawMapData.TransferX(now.Position.X) - agv_User.Width / 2),
                                                  (int)(mainForm.GetDrawMapData.TransferY(now.Position.Y) - agv_User.Height / 2));
                    HidePictureBox(agv_MirleAdmin);
                }
            }
        }

        private void Update_Move_Map()
        {
            pB_Move_Map.Image = mainForm.GetMapImage;

            if (inMapPage_ByMapButton)
            {
                panel_Move_Map_SettingAddress.Visible = false;
                panel_Move_Map_Function.Visible = true;
                panel_Move_Map.Size = new Size(668, 480);

                if (mainFlow.ActionCanUse(EnumUserAction.Move_LocalCommand))
                {
                    if (agvcFunctionThread == null || !agvcFunctionThread.IsAlive)
                    {
                        button_Move_Map_FunctionStart.Enabled = true;
                        button_Move_Map_FunctionStop.Enabled = false;
                    }
                    else
                    {
                        button_Move_Map_FunctionStart.Enabled = false;
                        button_Move_Map_FunctionStop.Enabled = true;
                    }
                }
                else
                {
                    button_Move_Map_FunctionStart.Enabled = false;
                    button_Move_Map_FunctionStop.Enabled = false;
                }
            }
            else
            {
                if (mainFlow.ActionCanUse(EnumUserAction.Move_SpecialFlow_ReviseByTarget))
                    button_Move_Map_AutoMove.Enabled = true;

                button_Move_Map_AutoMoveStop.Enabled = localData.MoveControlData.SpecialFlow;

                ClearLoadUnloadTag();

                //panel_Move_Map_SettingAddress.Enabled = mainFlow.ActionCanUse(EnumUserAction.Move_SetSlamAddressPosition);
                button_Move_Map_SetSlamPositionByAddressID.Enabled = mainFlow.ActionCanUse(EnumUserAction.Move_SetSlamAddressPosition);

                panel_Move_Map_SettingAddress.Visible = true;
                panel_Move_Map_Function.Visible = false;
                panel_Move_Map.Size = new Size(794, 388);
                label_Move_Map_AutoMoveStatusValue.Text = localData.MoveControlData.SpecialFlow ? GetStringByTag(EnumProfaceStringTag.移動中) : GetStringByTag(EnumProfaceStringTag.停止);
                CheckSetSlamPositionAddress();
            }

            UpdateAGV(localData.Real);
        }
        #endregion

        #region Move-AxisData.
        private List<EnumDefaultAxisName> move_AxisList = new List<EnumDefaultAxisName>() { EnumDefaultAxisName.XFL, EnumDefaultAxisName.XFR, EnumDefaultAxisName.XRL, EnumDefaultAxisName.XRR };
        private Dictionary<EnumDefaultAxisName, Label> move_AllAxisID = new Dictionary<EnumDefaultAxisName, Label>();
        private Dictionary<EnumDefaultAxisName, Label> move_AllAxisName = new Dictionary<EnumDefaultAxisName, Label>();
        private Dictionary<EnumDefaultAxisName, Label> move_AllAxisEncoder = new Dictionary<EnumDefaultAxisName, Label>();
        private Dictionary<EnumDefaultAxisName, Label> move_AllAxisRPM = new Dictionary<EnumDefaultAxisName, Label>();
        private Dictionary<EnumDefaultAxisName, Label> move_AllAxisServoOnOff = new Dictionary<EnumDefaultAxisName, Label>();
        private Dictionary<EnumDefaultAxisName, Label> move_AllAxisEC = new Dictionary<EnumDefaultAxisName, Label>();
        private Dictionary<EnumDefaultAxisName, Label> move_AllAxisMF = new Dictionary<EnumDefaultAxisName, Label>();
        private Dictionary<EnumDefaultAxisName, Label> move_AllAxisV = new Dictionary<EnumDefaultAxisName, Label>();
        private Dictionary<EnumDefaultAxisName, Label> move_AllAxisQA = new Dictionary<EnumDefaultAxisName, Label>();

        private void Initial_Move_AxisData()
        {
            int startX;
            int startY;

            Label tempLabel;

            for (int i = 0; i < move_AxisList.Count; i++)
            {
                startY = label_Move_AxisData_AxisID.Location.Y + (i + 1) * (label_Move_AxisData_AxisID.Size.Height - 1);

                #region AxisID.
                startX = label_Move_AxisData_AxisID.Location.X;
                tempLabel = new Label();

                tempLabel.AutoSize = false;
                tempLabel.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tempLabel.Location = new System.Drawing.Point(startX, startY);
                tempLabel.Name = String.Concat(move_AxisList[i], "_AxisID");
                tempLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.Size = label_Move_AxisData_AxisID.Size;
                tempLabel.Text = move_AxisList[i].ToString();
                tP_Move_AxisData.Controls.Add(tempLabel);
                move_AllAxisID.Add(move_AxisList[i], tempLabel);
                tempLabel.Click += Hide_Click;
                #endregion

                #region AxisName.
                startX = label_Move_AxisData_AxisName.Location.X;
                tempLabel = new Label();

                tempLabel.AutoSize = false;
                tempLabel.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tempLabel.Location = new System.Drawing.Point(startX, startY);
                tempLabel.Name = String.Concat(move_AxisList[i], "_AxisName");
                tempLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.Size = label_Move_AxisData_AxisName.Size;
                tempLabel.Text = ((EnumDefaultAxisNameChinese)(int)move_AxisList[i]).ToString();
                tP_Move_AxisData.Controls.Add(tempLabel);
                move_AllAxisName.Add(move_AxisList[i], tempLabel);
                tempLabel.Click += Hide_Click;
                #endregion

                #region Encoder.
                startX = label_Move_AxisData_Encoder.Location.X;
                tempLabel = new Label();

                tempLabel.AutoSize = false;
                tempLabel.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tempLabel.Location = new System.Drawing.Point(startX, startY);
                tempLabel.Name = String.Concat(move_AxisList[i], "_Encoder");
                tempLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.Size = label_Move_AxisData_Encoder.Size;
                tempLabel.Text = "";
                tP_Move_AxisData.Controls.Add(tempLabel);
                move_AllAxisEncoder.Add(move_AxisList[i], tempLabel);
                tempLabel.Click += Hide_Click;
                #endregion

                #region RPM
                startX = label_Move_AxisData_RPM.Location.X;
                tempLabel = new Label();

                tempLabel.AutoSize = false;
                tempLabel.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tempLabel.Location = new System.Drawing.Point(startX, startY);
                tempLabel.Name = String.Concat(move_AxisList[i], "_RPM");
                tempLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.Size = label_Move_AxisData_RPM.Size;
                tempLabel.Text = "";
                tP_Move_AxisData.Controls.Add(tempLabel);
                move_AllAxisRPM.Add(move_AxisList[i], tempLabel);
                tempLabel.Click += Hide_Click;
                #endregion

                #region ServoOnOff.
                startX = label_Move_AxisData_ServoOnOff.Location.X;
                tempLabel = new Label();

                tempLabel.AutoSize = false;
                tempLabel.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tempLabel.Location = new System.Drawing.Point(startX, startY);
                tempLabel.Name = String.Concat(move_AxisList[i], "_ServoOnOff");
                tempLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.Size = label_Move_AxisData_ServoOnOff.Size;
                tempLabel.Text = "";
                tP_Move_AxisData.Controls.Add(tempLabel);
                move_AllAxisServoOnOff.Add(move_AxisList[i], tempLabel);
                tempLabel.Click += Hide_Click;
                #endregion

                #region EC.
                startX = label_Move_AxisData_EC.Location.X;
                tempLabel = new Label();

                tempLabel.AutoSize = false;
                tempLabel.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tempLabel.Location = new System.Drawing.Point(startX, startY);
                tempLabel.Name = String.Concat(move_AxisList[i], "_EC");
                tempLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.Size = label_Move_AxisData_EC.Size;
                tempLabel.Text = "";
                tP_Move_AxisData.Controls.Add(tempLabel);
                move_AllAxisEC.Add(move_AxisList[i], tempLabel);
                tempLabel.Click += Hide_Click;
                #endregion

                #region MF.
                startX = label_Move_AxisData_MF.Location.X;
                tempLabel = new Label();

                tempLabel.AutoSize = false;
                tempLabel.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tempLabel.Location = new System.Drawing.Point(startX, startY);
                tempLabel.Name = String.Concat(move_AxisList[i], "_MF");
                tempLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.Size = label_Move_AxisData_MF.Size;
                tempLabel.Text = "";
                tP_Move_AxisData.Controls.Add(tempLabel);
                move_AllAxisMF.Add(move_AxisList[i], tempLabel);
                tempLabel.Click += Hide_Click;
                #endregion

                #region V.
                startX = label_Move_AxisData_V.Location.X;
                tempLabel = new Label();

                tempLabel.AutoSize = false;
                tempLabel.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tempLabel.Location = new System.Drawing.Point(startX, startY);
                tempLabel.Name = String.Concat(move_AxisList[i], "_V");
                tempLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.Size = label_Move_AxisData_V.Size;
                tempLabel.Text = "";
                tP_Move_AxisData.Controls.Add(tempLabel);
                move_AllAxisV.Add(move_AxisList[i], tempLabel);
                tempLabel.Click += Hide_Click;
                #endregion

                #region QA.
                startX = label_Move_AxisData_QA.Location.X;
                tempLabel = new Label();

                tempLabel.AutoSize = false;
                tempLabel.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tempLabel.Location = new System.Drawing.Point(startX, startY);
                tempLabel.Name = String.Concat(move_AxisList[i], "_QA");
                tempLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.Size = label_Move_AxisData_QA.Size;
                tempLabel.Text = "";
                tP_Move_AxisData.Controls.Add(tempLabel);
                move_AllAxisQA.Add(move_AxisList[i], tempLabel);
                tempLabel.Click += Hide_Click;
                #endregion
            }
        }

        private void Update_Move_AxisData()
        {
            if (localData.MoveControlData.MotionControlData.AllAxisFeedbackData != null)
            {
                AxisFeedbackData temp;

                for (int i = 0; i < move_AxisList.Count; i++)
                {
                    if (localData.MoveControlData.MotionControlData.AllAxisFeedbackData.ContainsKey(move_AxisList[i]))
                    {
                        temp = localData.MoveControlData.MotionControlData.AllAxisFeedbackData[move_AxisList[i]];

                        if (temp != null)
                        {
                            move_AllAxisEncoder[move_AxisList[i]].Text = temp.Position.ToString("0");
                            move_AllAxisRPM[move_AxisList[i]].Text = temp.Velocity.ToString("0.0");
                            move_AllAxisServoOnOff[move_AxisList[i]].Text = GetStringByTag(temp.AxisServoOnOff.ToString());
                            move_AllAxisEC[move_AxisList[i]].Text = temp.EC.ToString();
                            move_AllAxisMF[move_AxisList[i]].Text = temp.MF.ToString();
                            move_AllAxisV[move_AxisList[i]].Text = temp.V.ToString("0.0");
                            move_AllAxisQA[move_AxisList[i]].Text = temp.QA.ToString("0.000");
                        }
                    }
                }
            }
        }
        #endregion

        #region Move-DataInfo.
        private LabelNameAndValue sensorData_Address;
        private LabelNameAndValue sensorData_Section;
        private LabelNameAndValue sensorData_Distance;

        private LabelNameAndValue sensorData_Real;
        private LabelNameAndValue sensorData_LocationAGVPosition;
        private LabelNameAndValue sensorData_MIPCAGVPosition;

        private LabelNameAndValue sensorData_CommandID;
        private LabelNameAndValue sensorData_CommandStartTime;
        private LabelNameAndValue sensorData_CommandStstus;

        private LabelNameAndValue sensorData_MoveStatus;
        private LabelNameAndValue sensorData_CommandEncoder;
        private LabelNameAndValue sensorData_Velocity;

        private void Initial_Move_DataInfo()
        {
            #region MoveControlSensorData.
            sensorData_Address = new LabelNameAndValue(GetStringByTag(EnumProfaceStringTag.Address));
            sensorData_Address.Location = new Point(30, 20);
            tP_Move_DataInfo.Controls.Add(sensorData_Address);
            sensorData_Address.ClickEvent += Hide_Click;

            sensorData_Section = new LabelNameAndValue(GetStringByTag(EnumProfaceStringTag.Section));
            sensorData_Section.Location = new Point(280, 20);
            tP_Move_DataInfo.Controls.Add(sensorData_Section);
            sensorData_Section.ClickEvent += Hide_Click;

            sensorData_Distance = new LabelNameAndValue(GetStringByTag(EnumProfaceStringTag.Distance));
            sensorData_Distance.Location = new Point(530, 20);
            tP_Move_DataInfo.Controls.Add(sensorData_Distance);
            sensorData_Distance.ClickEvent += Hide_Click;

            sensorData_Real = new LabelNameAndValue(GetStringByTag(EnumProfaceStringTag.Real));
            sensorData_Real.Location = new Point(30, 70);
            sensorData_Real.SetLabelValueBigerr(100);
            tP_Move_DataInfo.Controls.Add(sensorData_Real);
            sensorData_Real.ClickEvent += Hide_Click;

            sensorData_MIPCAGVPosition = new LabelNameAndValue(GetStringByTag(EnumProfaceStringTag.MIPC));
            sensorData_MIPCAGVPosition.Location = new Point(400, 70);
            sensorData_MIPCAGVPosition.SetLabelValueBigerr(100);
            tP_Move_DataInfo.Controls.Add(sensorData_MIPCAGVPosition);
            sensorData_MIPCAGVPosition.ClickEvent += Hide_Click;

            sensorData_CommandID = new LabelNameAndValue(GetStringByTag(EnumProfaceStringTag.CommandID), false, 12);
            sensorData_CommandID.Location = new Point(30, 120);
            sensorData_CommandID.SetLabelValueBigerr(100);
            tP_Move_DataInfo.Controls.Add(sensorData_CommandID);
            sensorData_CommandID.ClickEvent += Hide_Click;

            sensorData_LocationAGVPosition = new LabelNameAndValue(GetStringByTag(EnumProfaceStringTag.Locate));
            sensorData_LocationAGVPosition.Location = new Point(400, 120);
            sensorData_LocationAGVPosition.SetLabelValueBigerr(100);
            tP_Move_DataInfo.Controls.Add(sensorData_LocationAGVPosition);
            sensorData_LocationAGVPosition.ClickEvent += Hide_Click;

            sensorData_CommandStartTime = new LabelNameAndValue(GetStringByTag(EnumProfaceStringTag.StartTime));
            sensorData_CommandStartTime.Location = new Point(280, 170);
            tP_Move_DataInfo.Controls.Add(sensorData_CommandStartTime);
            sensorData_CommandStartTime.ClickEvent += Hide_Click;

            sensorData_CommandStstus = new LabelNameAndValue(GetStringByTag(EnumProfaceStringTag.CmdStatus), false, 12);
            sensorData_CommandStstus.Location = new Point(30, 170);
            tP_Move_DataInfo.Controls.Add(sensorData_CommandStstus);
            sensorData_CommandStstus.ClickEvent += Hide_Click;

            sensorData_MoveStatus = new LabelNameAndValue(GetStringByTag(EnumProfaceStringTag.MoveStatus), false, 12);
            sensorData_MoveStatus.Location = new Point(30, 220);
            tP_Move_DataInfo.Controls.Add(sensorData_MoveStatus);
            sensorData_MoveStatus.ClickEvent += Hide_Click;

            sensorData_CommandEncoder = new LabelNameAndValue(GetStringByTag(EnumProfaceStringTag.CmdEncoder), false, 12);
            sensorData_CommandEncoder.Location = new Point(280, 220);
            tP_Move_DataInfo.Controls.Add(sensorData_CommandEncoder);
            sensorData_CommandEncoder.ClickEvent += Hide_Click;

            sensorData_Velocity = new LabelNameAndValue(GetStringByTag(EnumProfaceStringTag.Velocity));
            sensorData_Velocity.Location = new Point(530, 220);
            tP_Move_DataInfo.Controls.Add(sensorData_Velocity);
            sensorData_Velocity.ClickEvent += Hide_Click;
            #endregion
        }

        private void Update_Move_DataInfo()
        {
            VehicleLocation location = localData.Location;

            if (localData != null)
            {
                sensorData_Address.SetValueAndColor(location.LastAddress, (location.InAddress ? 100 : 0));
                sensorData_Section.SetValueAndColor(location.NowSection);
                sensorData_Distance.SetValueAndColor(location.DistanceFormSectionHead.ToString("0"));
            }
            else
            {
                sensorData_Address.SetValueAndColor("");
                sensorData_Section.SetValueAndColor("");
                sensorData_Distance.SetValueAndColor("");
            }

            sensorData_Real.SetValueAndColor(computeFunction.GetMapAGVPositionStringWithAngle(localData.Real));
            sensorData_LocationAGVPosition.SetValueAndColor(computeFunction.GetLocateAGVPositionStringWithAngle(localData.MoveControlData.LocateControlData.LocateAGVPosition));
            sensorData_MIPCAGVPosition.SetValueAndColor(computeFunction.GetLocateAGVPositionStringWithAngle(localData.MoveControlData.MotionControlData.EncoderAGVPosition));

            MoveCommandData command = localData.MoveControlData.MoveCommand;

            if (command != null)
            {
                sensorData_CommandID.SetValueAndColor(command.CommandID);
                sensorData_CommandStartTime.SetValueAndColor(command.StartTime.ToString("HH:mm:ss"));
                sensorData_CommandStstus.SetValueAndColor(command.CommandStatus.ToString());
                sensorData_MoveStatus.SetValueAndColor(command.MoveStatus.ToString(), (int)command.MoveStatus);
                sensorData_CommandEncoder.SetValueAndColor(command.CommandEncoder.ToString("0"));
                SetLabelTextAndColor(label_Move_DataInfo_SensorSstatusValue, command.SensorStatus);
                SetLabelTextAndColor(label_Move_DataInfo_ReserveValue, (command.ReserveStop ? EnumVehicleSafetyAction.SlowStop : EnumVehicleSafetyAction.Normal));
                SetLabelTextAndColor(label_Move_DataInfo_PauseValue, command.AGVPause);
            }
            else
            {
                sensorData_CommandID.SetValueAndColor("");
                sensorData_CommandStartTime.SetValueAndColor("");
                sensorData_CommandStstus.SetValueAndColor("");
                sensorData_MoveStatus.SetValueAndColor("");
                sensorData_CommandEncoder.SetValueAndColor("");
                label_Move_DataInfo_SensorSstatusValue.Text = "";
                label_Move_DataInfo_ReserveValue.Text = "";
                label_Move_DataInfo_PauseValue.Text = "";
            }

            SetLabelTextAndColor(label_Move_DataInfo_SafetySensorInfo, localData.MIPCData.SafetySensorStatus);
            SetLabelTextAndColor(label_Move_DataInfo_LocalPauseValue, localData.MoveControlData.SensorStatus.LocalPause);

            sensorData_Velocity.SetValueAndColor(String.Concat(localData.MoveControlData.MotionControlData.LineVelocity.ToString("0"), "/",
                                                                     localData.MoveControlData.MotionControlData.SimulateLineVelocity.ToString("0")));
        }
        #endregion

        #region Move-LocateDriver.
        private List<JogPitchLocateData> jogPitchLocateDataList = new List<JogPitchLocateData>();

        private void Initial_Move_LocateDriver()
        {
            #region LocateDriver.
            TabPage tempTabPage;
            JogPitchLocateData tempJogPitchLocateData;

            for (int i = 0; i < mainFlow.MoveControl.LocateControl.LocateControlDriverList.Count; i++)
            {
                tempJogPitchLocateData = new JogPitchLocateData(mainFlow.MoveControl.LocateControl.LocateControlDriverList[i]);
                tempJogPitchLocateData.Size = new Size(800, 400);

                tempTabPage = new TabPage();
                tempTabPage.Text = mainFlow.MoveControl.LocateControl.LocateControlDriverList[i].DriverConfig.Device;
                tempJogPitchLocateData.ClickEvent += Hide_Click;
                tempTabPage.Controls.Add(tempJogPitchLocateData);

                tC_LocateDriverList.TabPages.Add(tempTabPage);
                jogPitchLocateDataList.Add(tempJogPitchLocateData);
            }
            #endregion
        }

        private void Update_Move_LocateDriver()
        {
            int index = tC_LocateDriverList.SelectedIndex;

            if (tC_LocateDriverList.TabPages.Count > 0 && index < tC_LocateDriverList.TabPages.Count)
            {
                jogPitchLocateDataList[index].UpdateData(mainFlow.ActionCanUse(EnumUserAction.Move_LocateDriver_TriggerChange));
            }
        }
        #endregion

        #region Move-CommandRecord.
        private void Initial_Move_CommandRecord()
        {

        }

        private void listBox_MoveCommandRecordString_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //int index = listBox_Move_Record_String.IndexFromPoint(e.Location);

            //if (index >= 0)
            //{
            //    lock (localData.MoveControlData.LockMoveCommandRecordObject)
            //    {
            //        if (index < localData.MoveControlData.MoveCommandRecordList.Count)
            //        {
            //            MessageBox.Show(localData.MoveControlData.MoveCommandRecordList[index].CommandID);
            //        }
            //    }
            //}
        }

        private string lastCommandID = "";

        private void Update_Move_CommandRecord()
        {
            lock (localData.MoveControlData.LockMoveCommandRecordObject)
            {
                if (lastCommandID != localData.MoveControlData.LastCommandID)
                {
                    listBox_Move_Record_String.Items.Clear();

                    for (int i = 0; i < localData.MoveControlData.MoveCommandRecordList.Count; i++)
                        listBox_Move_Record_String.Items.Add(localData.MoveControlData.MoveCommandRecordList[i].LogString);

                    lastCommandID = localData.MoveControlData.LastCommandID;
                }
            }
        }
        #endregion

        #region Move-SetSlamPosition.
        private void Initial_Move_SetSlamPosition()
        {
            #region From-to.

            foreach (MapAddress address in localData.TheMapInfo.AllAddress.Values)
            {
                if (address.LoadUnloadDirection != EnumStageDirection.None)
                    cB_FromTo_Port.Items.Add(address.Id);

                if (address.ChargingDirection != EnumStageDirection.None)
                    cB_FromTo_ChargerStation.Items.Add(address.Id);

                if (address.LoadUnloadDirection == EnumStageDirection.None &&
                    address.ChargingDirection == EnumStageDirection.None)
                    cB_FromTo_Normal.Items.Add(address.Id);
            }

            cB_FromTo_Normal.DropDownHeight = 200;
            cB_FromTo_ChargerStation.DropDownHeight = 200;
            cB_FromTo_Port.DropDownHeight = 200;
            #endregion
        }

        private void button_Move_SetSlamPosition_AgreeSlamData_Click(object sender, EventArgs e)
        {
            HideAll();

            if (mainFlow.ActionCanUse(EnumUserAction.Move_ForceSetSlamDataOK))
                localData.MoveControlData.LocateControlData.SlamLocateOK = true;
        }

        private void button_Move_SetSlamPosition_Click(object sender, EventArgs e)
        {
            HideAll();

            if (mainFlow.ActionCanUse(EnumUserAction.Move_SetPosition) && mainFlow.ActionCanUse(EnumUserAction.Move_SpecialFlow_ReviseByTarget))
                mainFlow.MoveControl.ReviseAndSetPositionByAddressID(tB_FromToTargetAddressID.Text);
        }

        private void cB_FromTo_SelectedIndexChanged(object sender, EventArgs e)
        {
            HideAll();

            try
            {
                tB_FromToTargetAddressID.Text = ((ComboBox)sender).Text;
            }
            catch { }
        }

        private void radioButton_FormTo_Tag_CheckedChanged(object sender, EventArgs e)
        {
            HideAll();
            if (sender == radioButton_FormTo_Normal)
            {
                cB_FromTo_Normal.BringToFront();
                tB_FromToTargetAddressID.Text = cB_FromTo_Normal.Text;
            }
            else if (sender == radioButton_FromTo_ChargerStation)
            {
                cB_FromTo_ChargerStation.BringToFront();
                tB_FromToTargetAddressID.Text = cB_FromTo_ChargerStation.Text;
            }
            else if (sender == radioButton_FromTo_Port)
            {
                cB_FromTo_Port.BringToFront();
                tB_FromToTargetAddressID.Text = cB_FromTo_Port.Text;
            }
        }

        #region LocalCycleRun.
        private bool localCycleRunT0Test = false;
        private Thread autoCycleRunThread = null;

        private List<string> autoCycleRunAddressList_Move = new List<string>();
        private string autoCycleRunChargingAddressID_Move = "";
        private double autoCycleRunNeedChargingSOC_Move = 30;
        private bool autoCycleRunReadOK_Move = false;

        private void button_LocalCycleRun_Click(object sender, EventArgs e)
        {
            if (autoCycleRunThread != null && autoCycleRunThread.IsAlive)
            {
                if (localData.LoadUnloadData.LoadUnloadCommand != null)
                {
                    mainFlow.LocalTestCommandStop = true;
                }
                else if (localData.MIPCData.Charging)
                {
                    mainFlow.LocalTestCommandStop = true;
                }
                else
                {
                    if (localData.MoveControlData.MoveCommand != null)
                    {
                        mainFlow.LocalTestCommandStop = true;
                        mainFlow.MoveControl.VehicleStop();
                    }
                }
            }
            else
            {
                if (autoCycleRunReadOK_Move && localData.AutoManual == EnumAutoState.Manual &&
                    localData.LoginLevel >= EnumLoginLevel.Admin &&
                    localData.LoadUnloadData.LoadUnloadCommand == null &&
                    localData.MoveControlData.MoveCommand == null)
                {
                    mainFlow.LocalTestCommandStop = false;
                    autoCycleRunThread = new Thread(AutoCycleRun_Move);
                    autoCycleRunThread.Start();
                }
            }
        }

        private void InitialAutoCycleRunConfig()
        {
            string cycleRun_MovePath = Path.Combine(Environment.CurrentDirectory, "AutoCycleRun_Move.YO");

            try
            {
                string[] allRows = File.ReadAllLines(cycleRun_MovePath);

                if (allRows[0] == "CycleAddressIDList")
                {
                    string[] addressList = Regex.Split(allRows[1], "[: / ,]+", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));

                    for (int i = 0; i < addressList.Length; i++)
                        autoCycleRunAddressList_Move.Add(addressList[i]);
                }

                if (allRows[2] == "ChargerID")
                    autoCycleRunChargingAddressID_Move = allRows[3];

                if (allRows[4] == "NeedChargingSOC")
                    autoCycleRunNeedChargingSOC_Move = double.Parse(allRows[5]);

                autoCycleRunReadOK_Move = true;
            }
            catch { }
        }

        private void AutoCycleRun_Move()
        {
            try
            {
                string nextID = "";
                Random randon = new Random();

                bool autoCharging = localData.TheMapInfo.AllAddress.ContainsKey(autoCycleRunChargingAddressID_Move) &&
                                    localData.TheMapInfo.AllAddress[autoCycleRunChargingAddressID_Move].ChargingDirection != EnumStageDirection.None;

                bool endNeedCharging = false;

                if (localData.MIPCData.Charging)
                    mainFlow.MipcControl.StopCharging();

                while (localData.MoveControlData.MoveCommand != null ||
                       localData.MIPCData.Charging)
                {
                    if (mainFlow.LocalTestCommandStop)
                        return;

                    Thread.Sleep(100);
                }

                if (localData.MoveControlData.ErrorBit)
                    return;

                while (!mainFlow.LocalTestCommandStop)
                {
                    if (autoCharging && localData.BatteryInfo.Battery_SOC < autoCycleRunNeedChargingSOC_Move &&
                        computeFunction.GetDistanceFormTwoAGVPosition(localData.Real, localData.TheMapInfo.AllAddress[autoCycleRunChargingAddressID_Move].AGVPosition) > 200)
                    {
                        nextID = autoCycleRunChargingAddressID_Move;
                        endNeedCharging = true;
                    }
                    else
                    {
                        while (true)
                        {
                            nextID = autoCycleRunAddressList_Move[randon.Next(0, autoCycleRunAddressList_Move.Count)];

                            if (mainFlow.LocalTestCommandStop)
                                return;

                            if (computeFunction.GetDistanceFormTwoAGVPosition(localData.Real, localData.TheMapInfo.AllAddress[nextID].AGVPosition) > 200)
                                break;
                        }

                        endNeedCharging = false;
                    }

                    if (!mainFlow.MoveByAddressID_ManualTest(nextID))
                        return;

                    if (localCycleRunT0Test && localData.TheMapInfo.AllAddress[nextID].LoadUnloadDirection != EnumStageDirection.None)
                    {
                        if (localData.LoadUnloadData.Loading)
                        {
                            if (!mainFlow.ForByAddressIDAndAction_ManualTest(nextID, (localData.LoadUnloadData.Loading ? EnumLoadUnload.Unload : EnumLoadUnload.Load)))
                                return;

                            if (!localData.LoadUnloadData.Loading && !mainFlow.ForByAddressIDAndAction_ManualTest(nextID, (localData.LoadUnloadData.Loading ? EnumLoadUnload.Unload : EnumLoadUnload.Load)))
                                return;

                            if (!localData.LoadUnloadData.Loading)
                                return;
                        }
                        else
                        {
                            if (!mainFlow.ForByAddressIDAndAction_ManualTest(nextID, (localData.LoadUnloadData.Loading ? EnumLoadUnload.Unload : EnumLoadUnload.Load)))
                                return;
                        }

                        mainFlow.ResetAlarm();
                    }

                    if (endNeedCharging)
                    {
                        mainFlow.MipcControl.StartCharging(localData.TheMapInfo.AllAddress[nextID].ChargingDirection);

                        while (localData.MIPCData.Charging)
                        {
                            if (mainFlow.LocalTestCommandStop)
                                return;

                            Thread.Sleep(500);
                        }
                    }
                }
            }
            catch { }
        }
        #endregion

        private void Update_Move_SetSlamPosition()
        {
            button_Move_SetSlamPosition_AgreeSlamData.Enabled = localData.LoginLevel == EnumLoginLevel.MirleAdmin;
            button_LocalCycleRun.Text = (autoCycleRunThread != null && autoCycleRunThread.IsAlive ? "Stop" : "LocalCycleRun");

            switch (localData.MoveControlData.ReviseAndSetPositionStatus)
            {
                case EnumProfaceStringTag.定位OK:
                    label_LocateStatus.ForeColor = Color.Green;
                    break;
                case EnumProfaceStringTag.定位NG:
                    label_LocateStatus.ForeColor = Color.Red;
                    break;
                default:
                    label_LocateStatus.ForeColor = Color.Blue;
                    break;
            }

            label_LocateStatus.Text = GetStringByTag(localData.MoveControlData.ReviseAndSetPositionStatus);

            if (localData.MoveControlData.ReviseAndSetPositionResult != "")
            {
                label_Move_SetPosition_Result.Text = localData.MoveControlData.ReviseAndSetPositionResult;
                label_Move_SetPosition_Data.Text = localData.MoveControlData.ReviseAndSetPositionData;

                label_Move_SetPosition_Result.Visible = true;
                label_Move_SetPosition_Data.Visible = true;
            }
            else
            {
                label_Move_SetPosition_Result.Visible = false;
                label_Move_SetPosition_Data.Visible = false;
            }
        }
        #endregion

        #region Fork-Select.
        private void Initial_Fork()
        {
            if (mainFlow.LoadUnloadControl.LoadUnload != null)
            {
                Initial_Fork_Jog();
                Initial_Fork_Home();
                Initial_Fork_Command();
                Initial_Fork_Alignment();
                Initial_Fork_CommandRecord();
                Initial_Fork_PIO();
                Initial_Fork_AxisData();
                Initial_Fork_HomeSetting();
            }
        }

        private void button_LoadUnload_JogPitch_Click(object sender, EventArgs e)
        {
            ChageTabControlByPageIndex((int)EnumProfacePageIndex.Fork_Jog);
        }

        private void button_LoadUnload_Home_Click(object sender, EventArgs e)
        {
            ChageTabControlByPageIndex((int)EnumProfacePageIndex.Fork_Home);
        }

        private void button_LoadUnload_ManualCommand_Click(object sender, EventArgs e)
        {
            ChageTabControlByPageIndex((int)EnumProfacePageIndex.Fork_Command);
        }

        private void button_LoadUnload_AlignmentCheck_Click(object sender, EventArgs e)
        {
            ChageTabControlByPageIndex((int)EnumProfacePageIndex.Fork_Alignment);
        }

        private void button_LoadUnload_CommandRecord_Click(object sender, EventArgs e)
        {
            SetForkCommandHistory();
            ChageTabControlByPageIndex((int)EnumProfacePageIndex.Fork_CommandRecord);
        }

        private void button_LoadUnload_PIO_Click(object sender, EventArgs e)
        {
            ChageTabControlByPageIndex((int)EnumProfacePageIndex.Fork_PIO);
        }

        private void button_LoadUnload_AxisData_Click(object sender, EventArgs e)
        {
            ChageTabControlByPageIndex((int)EnumProfacePageIndex.Fork_AxisData);
        }

        private void button_LoadUnload_HomeAndStageSetting_Click(object sender, EventArgs e)
        {
            switch (localData.MainFlowConfig.AGVType)
            {
                case EnumAGVType.UMTC:
                    ChageTabControlByPageIndex((int)EnumProfacePageIndex.Fork_HomeSetting_UMTC);
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Fork-Jog.
        private List<Label> forkAxisList = new List<Label>();
        private List<Button> posButtonList = new List<Button>();
        private Dictionary<object, int> allPosButtonToIndex = new Dictionary<object, int>();

        private List<Button> nagButtonList = new List<Button>();
        private Dictionary<object, int> allNagButtonToIndex = new Dictionary<object, int>();

        private Dictionary<string, List<Label>> axisSensorList = new Dictionary<string, List<Label>>();

        private Button loadUnloadChangeColorButton = null;

        private void Initial_Fork_Jog()
        {
            #region Jog.
            Label tempLabel;
            Button tempButton;

            List<Label> tempLabelList;

            int buttonWidth = 100;
            int buttonHeigh = 70;

            int xInitial = 10;

            int yInitial = 30;
            int yInterval = 90;

            int sensorWidth = 80;
            int sensorHeigh = 50;

            int deltaX;
            int sensorCount = 0;

            for (int i = 0; i < mainFlow.LoadUnloadControl.LoadUnload.AxisList.Count; i++)
            {
                tempLabel = new Label();
                tempLabel.AutoSize = false;
                tempLabel.Size = new Size(buttonWidth, buttonHeigh);
                tempLabel.Font = new System.Drawing.Font("Times New Roman", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tempLabel.Text = mainFlow.LoadUnloadControl.LoadUnload.AxisList[i];

                tempLabel.Location = new Point(xInitial, i * yInterval + yInitial);
                tempLabel.TextAlign = ContentAlignment.MiddleLeft;
                forkAxisList.Add(tempLabel);
                tP_LoadUnload_Jog.Controls.Add(tempLabel);

                tempButton = new Button();
                tempButton.Size = new Size(buttonWidth, buttonHeigh);
                tempButton.Font = new System.Drawing.Font("Times New Roman", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tempButton.Text = mainFlow.LoadUnloadControl.LoadUnload.AxisPosName[i];

                tempButton.Location = new Point(xInitial + (buttonWidth + xInitial), i * yInterval + yInitial);
                tempButton.TextAlign = ContentAlignment.MiddleCenter;
                posButtonList.Add(tempButton);
                allPosButtonToIndex.Add((object)tempButton, i);

                tempButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.button_LoadUnloadJogStart_MouseDown);
                tempButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.button_LoadUnloadJogStart_MouseUp);
                tP_LoadUnload_Jog.Controls.Add(tempButton);

                tempButton = new Button();
                tempButton.Size = new Size(buttonWidth, buttonHeigh);
                tempButton.Font = new System.Drawing.Font("Times New Roman", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tempButton.Text = mainFlow.LoadUnloadControl.LoadUnload.AxisNagName[i];

                tempButton.Location = new Point(xInitial + (buttonWidth + xInitial) * 2, i * yInterval + yInitial);
                tempButton.TextAlign = ContentAlignment.MiddleCenter;
                nagButtonList.Add(tempButton);
                allNagButtonToIndex.Add((object)tempButton, i);

                tempButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.button_LoadUnloadJogStart_MouseDown);
                tempButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.button_LoadUnloadJogStart_MouseUp);
                tP_LoadUnload_Jog.Controls.Add(tempButton);

                sensorCount = mainFlow.LoadUnloadControl.LoadUnload.AxisSensorList[mainFlow.LoadUnloadControl.LoadUnload.AxisList[i]].Count;
                deltaX = ((tP_LoadUnload_Jog.Size.Width - (xInitial + (buttonWidth + xInitial) * 3)) - sensorCount * sensorWidth) / (sensorCount + 1);

                tempLabelList = new List<Label>();

                if (deltaX > 0)
                {
                    for (int j = 0; j < sensorCount; j++)
                    {
                        tempLabel = new Label();
                        tempLabel.AutoSize = false;
                        tempLabel.Size = new Size(sensorWidth, sensorHeigh);
                        tempLabel.Font = new System.Drawing.Font("Times New Roman", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                        tempLabel.Text = mainFlow.LoadUnloadControl.LoadUnload.AxisSensorList[mainFlow.LoadUnloadControl.LoadUnload.AxisList[i]][j];

                        tempLabel.Location = new Point((xInitial + (buttonWidth + xInitial) * 3) + deltaX / 2 + (deltaX + sensorHeigh) * j, i * yInterval + yInitial + (buttonHeigh - sensorHeigh) / 2);
                        tempLabel.TextAlign = ContentAlignment.MiddleCenter;
                        tempLabel.BorderStyle = BorderStyle.FixedSingle;
                        tP_LoadUnload_Jog.Controls.Add(tempLabel);
                        tempLabelList.Add(tempLabel);
                    }
                }
                else
                {
                    int newCount = sensorCount;

                    if (newCount % 2 != 0)
                        newCount++;

                    deltaX = ((tP_LoadUnload_Jog.Size.Width - (xInitial + (buttonWidth + xInitial) * 3)) - (newCount / 2) * sensorWidth) / ((newCount / 2) + 1);

                    for (int j = 0; j < sensorCount; j++)
                    {
                        tempLabel = new Label();
                        tempLabel.AutoSize = false;
                        tempLabel.Font = new System.Drawing.Font("Times New Roman", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                        tempLabel.Text = mainFlow.LoadUnloadControl.LoadUnload.AxisSensorList[mainFlow.LoadUnloadControl.LoadUnload.AxisList[i]][j];
                        tempLabel.TextAlign = ContentAlignment.MiddleCenter;
                        tempLabel.BorderStyle = BorderStyle.FixedSingle;
                        tempLabel.Size = new Size(sensorWidth, sensorHeigh / 2);

                        if (j < newCount / 2)
                            tempLabel.Location = new Point((xInitial + (buttonWidth + xInitial) * 3) + deltaX / 2 + (deltaX + sensorHeigh) * j, i * yInterval + yInitial + (buttonHeigh - sensorHeigh) / 2 - 5);
                        else
                            tempLabel.Location = new Point((xInitial + (buttonWidth + xInitial) * 3) + deltaX / 2 + (deltaX + sensorHeigh) * (j - newCount / 2), i * yInterval + yInitial + (buttonHeigh - sensorHeigh) / 2 + sensorHeigh / 2 + 5);

                        tP_LoadUnload_Jog.Controls.Add(tempLabel);
                        tempLabelList.Add(tempLabel);
                    }
                }

                axisSensorList.Add(mainFlow.LoadUnloadControl.LoadUnload.AxisList[i], tempLabelList);
            }
            #endregion
        }

        private void button_LoadUnloadJogHigh_Click(object sender, EventArgs e)
        {
            if (mainFlow.LoadUnloadControl.LoadUnload != null)
                mainFlow.LoadUnloadControl.LoadUnload.JogSpeed = EnumLoadUnloadJogSpeed.High;
        }

        private void button_LoadUnloadJogNormal_Click(object sender, EventArgs e)
        {
            if (mainFlow.LoadUnloadControl.LoadUnload != null)
                mainFlow.LoadUnloadControl.LoadUnload.JogSpeed = EnumLoadUnloadJogSpeed.Normal;
        }

        private void button_LoadUnloadJogLow_Click(object sender, EventArgs e)
        {
            if (mainFlow.LoadUnloadControl.LoadUnload != null)
                mainFlow.LoadUnloadControl.LoadUnload.JogSpeed = EnumLoadUnloadJogSpeed.Low;
        }

        private void button_LoadUnloadJogByPass_Click(object sender, EventArgs e)
        {
            if (mainFlow.LoadUnloadControl.LoadUnload != null)
                mainFlow.LoadUnloadControl.LoadUnload.JogByPass = !mainFlow.LoadUnloadControl.LoadUnload.JogByPass;
        }

        private void button_LoadUnloadJogStart_MouseDown(object sender, MouseEventArgs e)
        {
            if (mainFlow.LoadUnloadControl.LoadUnload == null)
                return;

            if (e.Button == MouseButtons.Left)
            {
                if (allPosButtonToIndex.ContainsKey(sender))
                {
                    loadUnloadChangeColorButton = (Button)sender;
                    loadUnloadChangeColorButton.BackColor = Color.Green;
                    mainFlow.LoadUnloadControl.LoadUnload.Jog(allPosButtonToIndex[sender], true);
                }
                else if (allNagButtonToIndex.ContainsKey(sender))
                {
                    loadUnloadChangeColorButton = (Button)sender;
                    loadUnloadChangeColorButton.BackColor = Color.Green;
                    mainFlow.LoadUnloadControl.LoadUnload.Jog(allNagButtonToIndex[sender], false);
                }
            }
        }

        private void button_LoadUnloadJogStart_MouseUp(object sender, MouseEventArgs e)
        {
            if (mainFlow.LoadUnloadControl.LoadUnload != null)
            {
                mainFlow.LoadUnloadControl.LoadUnload.JogStop();

                if (loadUnloadChangeColorButton != null)
                    loadUnloadChangeColorButton.BackColor = Color.Transparent;
            }
        }

        private void button_LoadUnloadJogStop_MouseDown(object sender, MouseEventArgs e)
        {
            if (mainFlow.LoadUnloadControl.LoadUnload != null && localData.AutoManual == EnumAutoState.Manual &&
                localData.LoadUnloadData.LoadUnloadCommand == null)
                mainFlow.LoadUnloadControl.LoadUnload.JogStop();

            ((Button)sender).BackColor = Color.Green;
        }

        private void button_LoadUnloadJogStop_MouseUp(object sender, MouseEventArgs e)
        {
            ((Button)sender).BackColor = Color.Transparent;
        }

        private void Update_Fork_Jog()
        {
            if (mainFlow.LoadUnloadControl.LoadUnload == null)
                return;

            label14.Text = mainFlow.LoadUnloadControl.LoadUnload.GetDeltaZ.ToString("0.0");
            button_LoadUnloadJogByPass.BackColor = (mainFlow.LoadUnloadControl.LoadUnload.JogByPass ? Color.Red : Color.Transparent);

            switch ((mainFlow.LoadUnloadControl.LoadUnload.JogByPass ? EnumLoadUnloadJogSpeed.Low : mainFlow.LoadUnloadControl.LoadUnload.JogSpeed))
            {
                case EnumLoadUnloadJogSpeed.High:
                    SetButtonInSelected(button_LoadUnloadJogHigh, true);
                    SetButtonInSelected(button_LoadUnloadJogNormal, false);
                    SetButtonInSelected(button_LoadUnloadJogLow, false);
                    break;
                case EnumLoadUnloadJogSpeed.Normal:
                    SetButtonInSelected(button_LoadUnloadJogHigh, false);
                    SetButtonInSelected(button_LoadUnloadJogNormal, true);
                    SetButtonInSelected(button_LoadUnloadJogLow, false);
                    break;
                case EnumLoadUnloadJogSpeed.Low:
                    SetButtonInSelected(button_LoadUnloadJogHigh, false);
                    SetButtonInSelected(button_LoadUnloadJogNormal, false);
                    SetButtonInSelected(button_LoadUnloadJogLow, true);
                    break;
                default:
                    break;
            }

            for (int i = 0; i < mainFlow.LoadUnloadControl.LoadUnload.AxisList.Count; i++)
            {
                for (int j = 0; j < mainFlow.LoadUnloadControl.LoadUnload.AxisSensorList[mainFlow.LoadUnloadControl.LoadUnload.AxisList[i]].Count; j++)
                    SetLabelInSelected(axisSensorList[mainFlow.LoadUnloadControl.LoadUnload.AxisList[i]][j], mainFlow.LoadUnloadControl.LoadUnload.AxisSensorDataList[mainFlow.LoadUnloadControl.LoadUnload.AxisList[i]][j].data);

                if (mainFlow.LoadUnloadControl.LoadUnload.AxisCanJog[i])
                {
                    posButtonList[i].Enabled = true;
                    nagButtonList[i].Enabled = true;
                }
                else
                {
                    posButtonList[i].Enabled = false;
                    nagButtonList[i].Enabled = false;
                }
            }

        }
        #endregion

        #region Fork-Home.
        private void Initial_Fork_Home()
        {
            label_Fork_HomeTitleMessage.Text = mainFlow.LoadUnloadControl.LoadUnload.HomeText;
        }

        private void Button_LoadUnloadHome_Home_Click(object sender, EventArgs e)
        {
            HideAll();

            if (mainFlow.ActionCanUse(EnumUserAction.Fork_Home))
                mainFlow.LoadUnloadControl.LoadUnload.Home();
        }

        private void Button_LoadUnloadHome_HomeInitial_Click(object sender, EventArgs e)
        {
            HideAll();

            if (mainFlow.ActionCanUse(EnumUserAction.Fork_Home))
                mainFlow.LoadUnloadControl.LoadUnload.Home_Initial();
        }

        private void Button_LoadUnloadHome_StopHome_Click(object sender, EventArgs e)
        {
            HideAll();

            if (mainFlow.ActionCanUse(EnumUserAction.Fork_Home))
                localData.LoadUnloadData.HomeStop = true;
        }

        private void Update_Fork_Home()
        {
            label_Fork_Home_Homging.Visible = localData.LoadUnloadData.Homing;
            button_Fork_Home_Home.Enabled = !localData.LoadUnloadData.Homing;
            button_Fork_Home_Home_Initial.Enabled = !localData.LoadUnloadData.Homing;
            label_Fork_Home_HomeStatusValue.BackColor = (localData.LoadUnloadData.ForkHome ? Color.Green : Color.Transparent);
        }
        #endregion

        #region Fork-Command.
        private EnumLoadUnload loadUnloadCommandType = EnumLoadUnload.Load;
        private EnumStageDirection loadUnloadDirection = EnumStageDirection.None;
        private EnumCstInAGVLocate cstInAGVLocate = EnumCstInAGVLocate.None;

        private bool loadUnloadUsingPIO = true;
        private bool loadUnloadbreakenMode = false;
        private bool loadUnloadNotUsingAlignmentValue = true;

        private void Initial_Fork_Command()
        {
            if (!mainFlow.LoadUnloadControl.LoadUnload.CanPause)
            {
                button_Fork_Command_Pause.Visible = false;
                button_Fork_Command_Continue.Visible = false;
            }

            SetButtonInSelected(button_Fork_Command_Type_Load, true);

            if (mainFlow.LoadUnloadControl.LoadUnload.CanLeftLoadUnload &&
                mainFlow.LoadUnloadControl.LoadUnload.CanRightLoadUnload)
            {
                loadUnloadDirection = EnumStageDirection.Left;
                SetButtonInSelected(button_Fork_Command_Direction_Left, true);
            }
            else if (mainFlow.LoadUnloadControl.LoadUnload.CanLeftLoadUnload)
            {
                loadUnloadDirection = EnumStageDirection.Left;
                button_Fork_Command_Direction_Right.Enabled = false;
                SetButtonInSelected(button_Fork_Command_Direction_Left, true);
            }
            else if (mainFlow.LoadUnloadControl.LoadUnload.CanRightLoadUnload)
            {
                loadUnloadDirection = EnumStageDirection.Right;
                button_Fork_Command_Direction_Left.Enabled = false;
                SetButtonInSelected(button_Fork_Command_Direction_Right, true);
            }
            else
            {
                loadUnloadDirection = EnumStageDirection.None;
                button_Fork_Command_Direction_Left.Enabled = false;
                button_Fork_Command_Direction_Right.Enabled = false;
            }

            SetButtonInSelected(button_Fork_Command_PIO_Use, true);
            SetButtonInSelected(button_Fork_Command_BreakenMode_NotUse, true);
            SetButtonInSelected(button_Fork_Command_AlignmentValue_Use, true);

            if (!mainFlow.LoadUnloadControl.LoadUnload.DoubleStoregeL)
            {
                label_Fork_Command_DoubleStoregeL.Visible = false;
                label_Fork_Command_DoubleStoregeLValue.Visible = false;
            }

            if (!mainFlow.LoadUnloadControl.LoadUnload.DoubleStoregeR)
            {
                label_Fork_Command_DoubleStoregeR.Visible = false;
                label_Fork_Command_DoubleStoregeRValue.Visible = false;
            }
        }

        #region 選項切換.
        private void Button_LoadUnload_Command_Type_Load_Click(object sender, EventArgs e)
        {
            HideAll();
            loadUnloadCommandType = EnumLoadUnload.Load;
            SetButtonInSelected(button_Fork_Command_Type_Load, true);
            SetButtonInSelected(button_Fork_Command_Type_Unload, false);
        }

        private void Button_LoadUnload_Command_Type_Unload_Click(object sender, EventArgs e)
        {
            HideAll();
            loadUnloadCommandType = EnumLoadUnload.Unload;
            SetButtonInSelected(button_Fork_Command_Type_Load, false);
            SetButtonInSelected(button_Fork_Command_Type_Unload, true);
        }

        private void Button_LoadUnload_Command_NeedPIO_Yes_Click(object sender, EventArgs e)
        {
            HideAll();
            loadUnloadUsingPIO = true;
            SetButtonInSelected(button_Fork_Command_PIO_Use, true);
            SetButtonInSelected(button_Fork_Command_PIO_NotUse, false);
        }

        private void Button_LoadUnload_Command_NeedPIO_No_Click(object sender, EventArgs e)
        {
            HideAll();
            loadUnloadUsingPIO = false;
            SetButtonInSelected(button_Fork_Command_PIO_Use, false);
            SetButtonInSelected(button_Fork_Command_PIO_NotUse, true);
        }

        private void Button_LoadUnload_Command_BreakenMode_No_Click(object sender, EventArgs e)
        {
            HideAll();
            loadUnloadbreakenMode = false;
            SetButtonInSelected(button_Fork_Command_BreakenMode_NotUse, true);
            SetButtonInSelected(button_Fork_Command_BreakenMode_Use, false);
        }

        private void Button_LoadUnload_Command_BreakenMode_Yes_Click(object sender, EventArgs e)
        {
            HideAll();
            loadUnloadbreakenMode = true;
            SetButtonInSelected(button_Fork_Command_BreakenMode_NotUse, false);
            SetButtonInSelected(button_Fork_Command_BreakenMode_Use, true);
        }

        private void Button_LoadUnload_Command_Left_Click(object sender, EventArgs e)
        {
            HideAll();
            loadUnloadDirection = EnumStageDirection.Left;
            SetButtonInSelected(button_Fork_Command_Direction_Left, true);
            SetButtonInSelected(button_Fork_Command_Direction_Right, false);
        }

        private void Button_LoadUnload_Command_Right_Click(object sender, EventArgs e)
        {
            HideAll();
            loadUnloadDirection = EnumStageDirection.Right;
            SetButtonInSelected(button_Fork_Command_Direction_Left, false);
            SetButtonInSelected(button_Fork_Command_Direction_Right, true);
        }

        private void Button_LoadUnload_Command_AlignmentValue_Yes_Click(object sender, EventArgs e)
        {
            HideAll();

            loadUnloadNotUsingAlignmentValue = true;
            SetButtonInSelected(button_Fork_Command_AlignmentValue_Use, true);
            SetButtonInSelected(button_Fork_Command_AlignmentValue_NotUse, false);
        }

        private void Button_LoadUnload_Command_AlignmentValue_No_Click(object sender, EventArgs e)
        {
            HideAll();

            loadUnloadNotUsingAlignmentValue = false;
            SetButtonInSelected(button_Fork_Command_AlignmentValue_Use, false);
            SetButtonInSelected(button_Fork_Command_AlignmentValue_NotUse, true);
        }

        private void Button_LoadUnload_Command_CstInAGV_Left_Click(object sender, EventArgs e)
        {
            HideAll();

            cstInAGVLocate = EnumCstInAGVLocate.Left;
            SetButtonInSelected(button_Fork_Command_CstInAGV_Left, true);
            SetButtonInSelected(button_Fork_Command_CstInAGV_Right, false);
        }

        private void Button_LoadUnload_Command_CstInAGV_Right_Click(object sender, EventArgs e)
        {
            HideAll();

            cstInAGVLocate = EnumCstInAGVLocate.Right;
            SetButtonInSelected(button_Fork_Command_CstInAGV_Left, false);
            SetButtonInSelected(button_Fork_Command_CstInAGV_Right, true);
        }
        #endregion

        #region Command.
        private void Button_LoadUnload_Command_Start_Click(object sender, EventArgs e)
        {
            HideAll();

            try
            {
                int stageNumber = 0;

                int speed = 100;

                switch (localData.MainFlowConfig.AGVType)
                {
                    case EnumAGVType.AGC:
                    case EnumAGVType.ATS:
                        speed = 100;
                        break;
                    default:
                        if (!Int32.TryParse(tB_LoadUnload_Command_Speed.Text, out speed))
                            speed = -1;
                        else if (speed > 100)
                            speed = -1;

                        break;
                }

                if (localData.AutoManual == EnumAutoState.Manual && speed > 0 && speed <= 100 &&
                    Int32.TryParse(tB_LoadUnload_Command_StageNumber.Text, out stageNumber) && stageNumber >= 0)
                {
                    MapAGVPosition now = localData.Real;

                    mainFlow.LoadUnloadControl.LoadUnloadRequest(loadUnloadCommandType, loadUnloadDirection, loadUnloadDirection, cstInAGVLocate,
                        localData.Location.LastAddress,
                        stageNumber, speed, loadUnloadUsingPIO, loadUnloadbreakenMode, "", loadUnloadNotUsingAlignmentValue);
                }
            }
            catch { }
        }

        private void Button_LoadUnload_Command_StartByAddressID_Click(object sender, EventArgs e)
        {
            HideAll();

            try
            {
                if (localData.AutoManual == EnumAutoState.Manual)
                {
                    MapAGVPosition now = localData.Real;

                    string addressID = "";

                    if (now != null)
                    {
                        foreach (MapAddress address in localData.TheMapInfo.AllAddress.Values)
                        {
                            if (address.LoadUnloadDirection != EnumStageDirection.None)
                            {
                                if (computeFunction.GetDistanceFormTwoAGVPosition(address.AGVPosition, now) < 100)
                                {
                                    addressID = address.Id;
                                    break;
                                }
                            }
                        }
                    }

                    if (localData.TheMapInfo.AllAddress.ContainsKey(addressID) && localData.TheMapInfo.AllAddress[addressID].LoadUnloadDirection != EnumStageDirection.None)
                    {
                        mainFlow.LoadUnloadControl.LoadUnloadRequest((localData.LoadUnloadData.Loading ? EnumLoadUnload.Unload : EnumLoadUnload.Load),
                            localData.TheMapInfo.AllAddress[addressID].LoadUnloadDirection,
                            localData.TheMapInfo.AllAddress[addressID].LoadUnloadDirection,
                            cstInAGVLocate,
                            addressID,
                            localData.TheMapInfo.AllAddress[addressID].StageNumber, 100,
                            loadUnloadUsingPIO, loadUnloadbreakenMode, "", loadUnloadNotUsingAlignmentValue);
                    }
                }
            }
            catch { }
        }

        private void Button_LoadUnload_Command_Stop_Click(object sender, EventArgs e)
        {
            HideAll();

            if (localData.AutoManual == EnumAutoState.Manual)
                mainFlow.LoadUnloadControl.StopCommandRequest();
        }

        private void Button_LoadUnload_Command_Pause_Click(object sender, EventArgs e)
        {
            HideAll();

            if (localData.AutoManual == EnumAutoState.Manual)
                mainFlow.LoadUnloadControl.LoadUnloadPause();
        }

        private void Button_LoadUnload_Command_Continue_Click(object sender, EventArgs e)
        {
            HideAll();

            if (localData.AutoManual == EnumAutoState.Manual)
                mainFlow.LoadUnloadControl.LoadUnloadContinue();
        }

        private void Button_LoadUnload_Command_GoNest_Click(object sender, EventArgs e)
        {
            HideAll();

            if (localData.AutoManual == EnumAutoState.Manual)
                mainFlow.LoadUnloadControl.LoadUnloadGoNext();
        }

        private void Button_LoadUnload_Command_Back_Click(object sender, EventArgs e)
        {
            HideAll();

            if (localData.AutoManual == EnumAutoState.Manual)
                mainFlow.LoadUnloadControl.LoadUnloadGoBack();
        }
        #endregion

        private void Update_Fork_Command()
        {
            tP_LoadUnload_ManualCommand.Enabled = (localData.AutoManual == EnumAutoState.Manual);

            AlignmentValueData temp = localData.LoadUnloadData.AlignmentValue;

            if (temp != null && temp.AlignmentVlaue)
            {
                label_Fork_Command_Alignment_PValue.Text = temp.P.ToString("0.00");
                label_Fork_Command_Alignment_ThetaValue.Text = temp.Theta.ToString("0.00");
                label_Fork_Command_Alignment_YValue.Text = temp.Y.ToString("0.00");
                label_Fork_Command_Alignment_ZValue.Text = temp.Z.ToString("0.00");
            }
            else
            {
                label_Fork_Command_Alignment_PValue.Text = "";
                label_Fork_Command_Alignment_ThetaValue.Text = "";
                label_Fork_Command_Alignment_YValue.Text = "";
                label_Fork_Command_Alignment_ZValue.Text = "";
            }

            LoadUnloadCommandData tempCommand = localData.LoadUnloadData.LoadUnloadCommand;

            if (tempCommand == null)
                label_Fork_Command_CommandStatusValue.Text = "";
            else
                label_Fork_Command_CommandStatusValue.Text = tempCommand.StepString;

            label_Fork_Command_ForkHomeValue.Text = GetStringByTag(localData.LoadUnloadData.ForkHome ? EnumProfaceStringTag.ForkHome : EnumProfaceStringTag.Fork_Not_Home);
            label_Fork_Command_LoadingValue.Text = GetStringByTag(localData.LoadUnloadData.Loading ? EnumProfaceStringTag.有貨 : EnumProfaceStringTag.無貨);


            label_Fork_Command_DoubleStoregeLValue.Text = GetStringByTag(localData.LoadUnloadData.DoubleStoregeL ? EnumProfaceStringTag.有貨 : EnumProfaceStringTag.無貨);
            label_Fork_Command_DoubleStoregeRValue.Text = GetStringByTag(localData.LoadUnloadData.DoubleStoregeR ? EnumProfaceStringTag.有貨 : EnumProfaceStringTag.無貨);
        }
        #endregion

        #region Fork-Alignment.
        private Thread checkAlignmentValueThread = null;
        private EnumStageDirection checkAlignmentValueDirection = EnumStageDirection.None;

        private bool usingAddressGetAlignmentValue = false;

        private string checkAlignmentAddressID = "";

        private void Initial_Fork_Alignment()
        {
            #region AlignmentCheck.
            if (!mainFlow.LoadUnloadControl.LoadUnload.CanLeftLoadUnload)
            {
                button_Fork_Alignment_LeftCheck.Visible = false;
                button_Fork_HomeAndStageSetting_Alignment_ResetZeroLeft.Visible = false;
            }

            if (!mainFlow.LoadUnloadControl.LoadUnload.CanRightLoadUnload)
            {
                button_Fork_Alignment_RightCheck.Visible = false;
                button_Fork_HomeAndStageSetting_Alignment_ResetZeroRight.Visible = false;
            }
            #endregion
        }


        private void button_Fork_Alignment_LeftCheck_Click(object sender, EventArgs e)
        {
            HideAll();

            if (mainFlow.ActionCanUse(EnumUserAction.Fork_GetAlignmentValue))
            {
                button_Fork_Alignment_LeftCheck.Enabled = false;
                button_Fork_Alignment_RightCheck.Enabled = false;
                button_ForkAlignment_Alignment_AddressCheck.Enabled = false;

                HideAll();
                checkAlignmentValueDirection = EnumStageDirection.Left;

                checkAlignmentValueThread = new Thread(CheckAlignmentValueThread);
                checkAlignmentValueThread.Start();
            }
        }

        private void button_Fork_Alignment_RightCheck_Click(object sender, EventArgs e)
        {
            HideAll();

            if (mainFlow.ActionCanUse(EnumUserAction.Fork_GetAlignmentValue))
            {
                button_Fork_Alignment_LeftCheck.Enabled = false;
                button_Fork_Alignment_RightCheck.Enabled = false;
                button_ForkAlignment_Alignment_AddressCheck.Enabled = false;

                HideAll();
                checkAlignmentValueDirection = EnumStageDirection.Right;

                checkAlignmentValueThread = new Thread(CheckAlignmentValueThread);
                checkAlignmentValueThread.Start();
            }
        }

        private void button_ForkAlignment_Alignment_AddressCheck_Click(object sender, EventArgs e)
        {
            HideAll();

            VehicleLocation location = localData.Location;

            if (mainFlow.ActionCanUse(EnumUserAction.Fork_GetAlignmentValue) &&
                location != null && localData.TheMapInfo.IsPortOrChargingStation(location.LastAddress))
            {
                button_Fork_Alignment_LeftCheck.Enabled = false;
                button_Fork_Alignment_RightCheck.Enabled = false;
                button_ForkAlignment_Alignment_AddressCheck.Enabled = false;

                checkAlignmentAddressID = location.LastAddress;
                usingAddressGetAlignmentValue = true;
                checkAlignmentValueThread = new Thread(CheckAlignmentValueThread);
                checkAlignmentValueThread.Start();
            }
        }

        private void CheckAlignmentValueThread()
        {
            try
            {
                if (usingAddressGetAlignmentValue)
                {
                    usingAddressGetAlignmentValue = false;
                    mainFlow.LoadUnloadControl.LoadUnload.CheckAlingmentValueByAddressID(checkAlignmentAddressID);
                }
                else
                    mainFlow.LoadUnloadControl.LoadUnload.CheckAlingmentValue(checkAlignmentValueDirection, Int32.Parse(tB_Fork_Alignment_StageNumber.Text));
            }
            catch { }
        }

        private void Update_Fork_Alignment()
        {
            if (checkAlignmentValueThread == null || !checkAlignmentValueThread.IsAlive)
            {
                button_Fork_Alignment_LeftCheck.Enabled = true;
                button_Fork_Alignment_RightCheck.Enabled = true;
                button_ForkAlignment_Alignment_AddressCheck.Enabled = true;
            }

            AlignmentValueData temp = localData.LoadUnloadData.AlignmentValue;

            if (temp == null)
            {
                label_Fork_Alignment_LaserFValue.Text = "";
                label_Fork_Alignment_LaserBValue.Text = "";

                label_Fork_Alignment_Barcode_IDValue.Text = "";
                label_Fork_Alignment_Barcode_XValue.Text = "";
                label_Fork_Alignment_Barcode_YValue.Text = "";

                label_Fork_Alignment_Alignment_PValue.Text = "";
                label_Fork_Alignment_Alignment_YValue.Text = "";
                label_Fork_Alignment_Alignment_ThetaValue.Text = "";
                label_Fork_Alignment_Alignment_ZValue.Text = "";
            }
            else
            {
                if (temp.LaserF != 0)
                    label_Fork_Alignment_LaserFValue.Text = temp.LaserF.ToString("0.00");
                else
                    label_Fork_Alignment_LaserFValue.Text = "";

                if (temp.LaserB != 0)
                    label_Fork_Alignment_LaserBValue.Text = temp.LaserB.ToString("0.00");
                else
                    label_Fork_Alignment_LaserBValue.Text = "";

                if (temp.BarcodeNumber != "")
                {
                    label_Fork_Alignment_Barcode_IDValue.Text = temp.BarcodeNumber;
                    label_Fork_Alignment_Barcode_XValue.Text = temp.BarcodePosition.X.ToString("0.00");
                    label_Fork_Alignment_Barcode_YValue.Text = temp.BarcodePosition.Y.ToString("0.00");
                }
                else
                {
                    label_Fork_Alignment_Barcode_IDValue.Text = "";
                    label_Fork_Alignment_Barcode_XValue.Text = "";
                    label_Fork_Alignment_Barcode_YValue.Text = "";
                }

                if (temp.AlignmentVlaue)
                {
                    label_Fork_Alignment_Alignment_PValue.Text = temp.P.ToString("0.0");
                    label_Fork_Alignment_Alignment_YValue.Text = temp.Y.ToString("0.0");
                    label_Fork_Alignment_Alignment_ThetaValue.Text = temp.Theta.ToString("0.0");
                    label_Fork_Alignment_Alignment_ZValue.Text = temp.Z.ToString("0.0");
                }
                else
                {
                    label_Fork_Alignment_Alignment_PValue.Text = "";
                    label_Fork_Alignment_Alignment_YValue.Text = "";
                    label_Fork_Alignment_Alignment_ThetaValue.Text = "";
                    label_Fork_Alignment_Alignment_ZValue.Text = "";
                }
            }
        }
        #endregion

        #region Fork-CommandRecord.
        private List<LoadUnloadCommandData> forkCommandHistory = new List<LoadUnloadCommandData>();

        private void Initial_Fork_CommandRecord()
        {
            if (mainFlow.LoadUnloadControl.LoadUnload.LeftPIO != null)
                pioHistoryForm = new PIOHistoryForm(mainFlow.LoadUnloadControl.LoadUnload.LeftPIO.PIOInputNameList, mainFlow.LoadUnloadControl.LoadUnload.LeftPIO.PIOOutputNameList);
            else if (mainFlow.LoadUnloadControl.LoadUnload.RightPIO != null)
                pioHistoryForm = new PIOHistoryForm(mainFlow.LoadUnloadControl.LoadUnload.RightPIO.PIOInputNameList, mainFlow.LoadUnloadControl.LoadUnload.RightPIO.PIOOutputNameList);

            pioHistoryForm.Location = new Point((this.Size.Width - pioHistoryForm.Size.Width) / 2,
                                                (this.Size.Height - pioHistoryForm.Size.Height) / 2);
            this.Controls.Add(pioHistoryForm);
            pioHistoryForm.Hide();
        }

        private void ShowPIOHistoryForm()
        {
            if (pioHistoryForm != null)
            {
                pioHistoryForm.Show();
                pioHistoryForm.BringToFront();
            }
        }

        private void SetPIOHisotryAndShow(int index)
        {
            if (pioHistoryForm != null)
            {
                pioHistoryForm.SetCommandResult(forkCommandHistory[index]);
                ShowPIOHistoryForm();
            }
        }

        private void SetForkCommandHistory()
        {
            forkCommandHistory = localData.LoadUnloadData.CommandHistory;

            listBox_Fork_Record_Data.Items.Clear();

            for (int i = 0; i < forkCommandHistory.Count; i++)
            {
                listBox_Fork_Record_Data.Items.Add(
                    String.Concat(forkCommandHistory[i].CommandStartTime.ToString("HH:mm:ss"), "\t\t",
                                  GetStringByTag(forkCommandHistory[i].Action.ToString()), "\t\t",
                                  forkCommandHistory[i].CommandResult.ToString(), "\t\t",
                                  forkCommandHistory[i].CommandID)

                    );
            }
        }

        private void listBox_Fork_Record_Data_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int index = listBox_Fork_Record_Data.IndexFromPoint(e.Location);

            if (index >= 0)
            {
                if (index < forkCommandHistory.Count)
                    SetPIOHisotryAndShow(index);
            }
        }

        private void Update_Fork_CommandRecord()
        {

        }
        #endregion

        #region Fork-PIO.
        private List<PIOForm> loadUnloadPIOList = new List<PIOForm>();

        private void Initial_Fork_PIO()
        {
            #region PIO.
            if (mainFlow.LoadUnloadControl.LoadUnload.LeftPIO != null)
            {
                TabPage leftTabPage = new TabPage();
                leftTabPage.Text = "Left";
                leftTabPage.Size = new Size(768, 339);

                PIOForm leftLoadUnloadPIOForm = new PIOForm(mainFlow.MipcControl, mainFlow.LoadUnloadControl.LoadUnload.LeftPIO);
                leftLoadUnloadPIOForm.Location = new Point(0, 0);

                leftTabPage.Controls.Add(leftLoadUnloadPIOForm);

                loadUnloadPIOList.Add(leftLoadUnloadPIOForm);
                tC_LoadUnloadPIO.TabPages.Add(leftTabPage);
            }

            if (mainFlow.LoadUnloadControl.LoadUnload.RightPIO != null)
            {
                TabPage rightTagPage = new TabPage();
                rightTagPage.Text = "Right";
                rightTagPage.Size = new Size(768, 339);

                PIOForm rightLoadUnloadPIOForm = new PIOForm(mainFlow.MipcControl, mainFlow.LoadUnloadControl.LoadUnload.RightPIO);
                rightLoadUnloadPIOForm.Location = new Point(0, 0);

                rightTagPage.Controls.Add(rightLoadUnloadPIOForm);

                loadUnloadPIOList.Add(rightLoadUnloadPIOForm);
                tC_LoadUnloadPIO.TabPages.Add(rightTagPage);
            }
            #endregion
        }

        private void label_Fork_PIO_NotSendTR_REQ_Click(object sender, EventArgs e)
        {
            HideAll();

            if (mainFlow.ActionCanUse(EnumUserAction.Fork_PIOTest))
                mainFlow.LoadUnloadControl.NotSendTR_REQ = !mainFlow.LoadUnloadControl.NotSendTR_REQ;
        }

        private void label_Fork_PIO_NotSendBUSY_Click(object sender, EventArgs e)
        {
            HideAll();

            if (mainFlow.ActionCanUse(EnumUserAction.Fork_PIOTest))
                mainFlow.LoadUnloadControl.NotSendBUSY = !mainFlow.LoadUnloadControl.NotSendBUSY;
        }

        private void label_Fork_PIO_NotForkBusyAction_Click(object sender, EventArgs e)
        {
            HideAll();

            if (mainFlow.ActionCanUse(EnumUserAction.Fork_PIOTest))
                mainFlow.LoadUnloadControl.NotForkBusyAction = !mainFlow.LoadUnloadControl.NotForkBusyAction;
        }

        private void label_Fork_PIO_NotSendCOMPT_Click(object sender, EventArgs e)
        {
            HideAll();

            if (mainFlow.ActionCanUse(EnumUserAction.Fork_PIOTest))
                mainFlow.LoadUnloadControl.NotSendCOMPT = !mainFlow.LoadUnloadControl.NotSendCOMPT;
        }

        private void label_Fork_PIO_NotSendAllOff_Click(object sender, EventArgs e)
        {
            HideAll();

            if (mainFlow.ActionCanUse(EnumUserAction.Fork_PIOTest))
                mainFlow.LoadUnloadControl.NotSendAllOff = !mainFlow.LoadUnloadControl.NotSendAllOff;
        }

        private void Update_Fork_PIO()
        {
            SetLabelInAlarm(label_Fork_PIO_NotSendTR_REQ, mainFlow.LoadUnloadControl.NotSendTR_REQ);
            SetLabelInAlarm(label_Fork_PIO_NotSendBUSY, mainFlow.LoadUnloadControl.NotSendBUSY);
            SetLabelInAlarm(label_Fork_PIO_NotSendCOMPT, mainFlow.LoadUnloadControl.NotSendCOMPT);
            SetLabelInAlarm(label_Fork_PIO_NotForkBusyAction, mainFlow.LoadUnloadControl.NotForkBusyAction);
            SetLabelInAlarm(label_Fork_PIO_NotSendAllOff, mainFlow.LoadUnloadControl.NotSendAllOff);

            if (tC_LoadUnloadPIO.TabPages.Count > 0 && tC_LoadUnloadPIO.SelectedIndex >= 0 && tC_LoadUnloadPIO.SelectedIndex < tC_LoadUnloadPIO.TabPages.Count)
            {
                loadUnloadPIOList[tC_LoadUnloadPIO.SelectedIndex].UpdatePIOStatus();
            }
        }
        #endregion

        #region Fork-AxisData.
        private List<Label> allForkAxisName = new List<Label>();
        private List<Label> allForkAxisEncoder = new List<Label>();
        private List<Label> allForkAxisVelocity = new List<Label>();
        private List<Label> allForkAxisSeervoOnOff = new List<Label>();
        private List<Label> allForkAxisStop = new List<Label>();
        private List<Label> allForkAxisEC = new List<Label>();
        private List<Label> allForkAxisMF = new List<Label>();
        private List<Label> allForkAxisV = new List<Label>();
        private List<Label> allForkAxisQA = new List<Label>();
        private List<string> allForkFeedbackAxisList = new List<string>();

        private void Initial_Fork_AxisData()
        {
            #region AxisData.
            int startX;
            int startY;

            string id = "";
            Label tempLabel;

            for (int i = 0; i < mainFlow.LoadUnloadControl.LoadUnload.FeedbackAxisList.Count; i++)
            {
                id = mainFlow.LoadUnloadControl.LoadUnload.FeedbackAxisList[i];

                startY = label_Fork_AxisData_Name.Location.Y + (allForkFeedbackAxisList.Count + 1) * (label_Fork_AxisData_Name.Size.Height - 1);
                allForkFeedbackAxisList.Add(id);
                #region AxisID.
                startX = label_Fork_AxisData_Name.Location.X;
                tempLabel = new Label();
                tempLabel.AutoSize = false;
                tempLabel.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tempLabel.Location = new System.Drawing.Point(startX, startY);
                tempLabel.Name = String.Concat(id, "_AxisName");
                tempLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                tempLabel.Text = id;
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.Size = label_Fork_AxisData_Name.Size;
                tempLabel.Click += Hide_Click;
                tP_LoadUnload_AxisData.Controls.Add(tempLabel);
                allForkAxisName.Add(tempLabel);
                #endregion

                #region Encoder.
                startX = label_Fork_AxisData_Encoder.Location.X;
                tempLabel = new Label();
                tempLabel.AutoSize = false;
                tempLabel.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tempLabel.Location = new System.Drawing.Point(startX, startY);
                tempLabel.Name = String.Concat(id, "_Encoder");
                tempLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.Size = label_Fork_AxisData_Encoder.Size;
                tempLabel.Click += Hide_Click;
                tP_LoadUnload_AxisData.Controls.Add(tempLabel);
                allForkAxisEncoder.Add(tempLabel);
                #endregion

                #region Velocity.
                startX = label_Fork_AxisData_Velocity.Location.X;
                tempLabel = new Label();
                tempLabel.AutoSize = false;
                tempLabel.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tempLabel.Location = new System.Drawing.Point(startX, startY);
                tempLabel.Name = String.Concat(id, "_Velocity");
                tempLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.Size = label_Fork_AxisData_Velocity.Size;
                tempLabel.Click += Hide_Click;
                tP_LoadUnload_AxisData.Controls.Add(tempLabel);
                allForkAxisVelocity.Add(tempLabel);
                #endregion

                #region ServoOnOff.
                startX = label_Fork_AxisData_ServoOnOff.Location.X;
                tempLabel = new Label();
                tempLabel.AutoSize = false;
                tempLabel.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tempLabel.Location = new System.Drawing.Point(startX, startY);
                tempLabel.Name = String.Concat(id, "_ServoOnOff");
                tempLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.Size = label_Fork_AxisData_ServoOnOff.Size;
                tempLabel.Click += Hide_Click;
                tP_LoadUnload_AxisData.Controls.Add(tempLabel);
                allForkAxisSeervoOnOff.Add(tempLabel);
                #endregion

                #region Stop.
                startX = label_Fork_AxisData_Stop.Location.X;
                tempLabel = new Label();
                tempLabel.AutoSize = false;
                tempLabel.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tempLabel.Location = new System.Drawing.Point(startX, startY);
                tempLabel.Name = String.Concat(id, "_Stop");
                tempLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.Size = label_Fork_AxisData_Stop.Size;
                tempLabel.Click += Hide_Click;
                tP_LoadUnload_AxisData.Controls.Add(tempLabel);
                allForkAxisStop.Add(tempLabel);
                #endregion

                #region EC.
                startX = label_Fork_AxisData_EC.Location.X;
                tempLabel = new Label();
                tempLabel.AutoSize = false;
                tempLabel.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tempLabel.Location = new System.Drawing.Point(startX, startY);
                tempLabel.Name = String.Concat(id, "_EC");
                tempLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.Size = label_Fork_AxisData_EC.Size;
                tempLabel.Click += Hide_Click;
                tP_LoadUnload_AxisData.Controls.Add(tempLabel);
                allForkAxisEC.Add(tempLabel);
                #endregion

                #region MF.
                startX = label_Fork_AxisData_MF.Location.X;
                tempLabel = new Label();
                tempLabel.AutoSize = false;
                tempLabel.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tempLabel.Location = new System.Drawing.Point(startX, startY);
                tempLabel.Name = String.Concat(id, "_MF");
                tempLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.Size = label_Fork_AxisData_MF.Size;
                tempLabel.Click += Hide_Click;
                tP_LoadUnload_AxisData.Controls.Add(tempLabel);
                allForkAxisMF.Add(tempLabel);
                #endregion

                #region V.
                startX = label_Fork_AxisData_V.Location.X;
                tempLabel = new Label();
                tempLabel.AutoSize = false;
                tempLabel.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tempLabel.Location = new System.Drawing.Point(startX, startY);
                tempLabel.Name = String.Concat(id, "_V");
                tempLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.Size = label_Fork_AxisData_V.Size;
                tempLabel.Click += Hide_Click;
                tP_LoadUnload_AxisData.Controls.Add(tempLabel);
                allForkAxisV.Add(tempLabel);
                #endregion

                #region QA.
                startX = label_Fork_AxisData_QA.Location.X;
                tempLabel = new Label();
                tempLabel.AutoSize = false;
                tempLabel.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tempLabel.Location = new System.Drawing.Point(startX, startY);
                tempLabel.Name = String.Concat(id, "_QA");
                tempLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.Size = label_Fork_AxisData_QA.Size;
                tempLabel.Click += Hide_Click;
                tP_LoadUnload_AxisData.Controls.Add(tempLabel);
                allForkAxisQA.Add(tempLabel);
                #endregion
            }
            #endregion
        }

        private void Update_Fork_AxisData()
        {
            AxisFeedbackData temp;

            for (int i = 0; i < allForkFeedbackAxisList.Count; i++)
            {
                if (localData.LoadUnloadData.CVFeedbackData.ContainsKey(allForkFeedbackAxisList[i]))
                {
                    temp = localData.LoadUnloadData.CVFeedbackData[allForkFeedbackAxisList[i]];

                    if (temp != null)
                    {
                        allForkAxisEncoder[i].Text = temp.Position.ToString("0.0");
                        allForkAxisVelocity[i].Text = temp.Velocity.ToString("0.0");
                        allForkAxisSeervoOnOff[i].Text = temp.AxisServoOnOff.ToString();
                        allForkAxisStop[i].Text = temp.AxisMoveStaus.ToString();
                        allForkAxisEC[i].Text = temp.EC.ToString("0");
                        allForkAxisMF[i].Text = temp.MF.ToString("0");
                        allForkAxisQA[i].Text = temp.QA.ToString("0.000");
                        allForkAxisV[i].Text = temp.V.ToString("0.000");
                    }
                    else
                    {
                        allForkAxisEncoder[i].Text = "";
                        allForkAxisVelocity[i].Text = "";
                        allForkAxisSeervoOnOff[i].Text = "";
                        allForkAxisStop[i].Text = "";
                        allForkAxisEC[i].Text = "";
                        allForkAxisMF[i].Text = "";
                        allForkAxisQA[i].Text = "";
                        allForkAxisV[i].Text = temp.QA.ToString("0.000");
                    }
                }
            }
        }
        #endregion

        #region Fork-HomeSetting.
        private void Initial_Fork_HomeSetting()
        {
            #region ForkHomeSetting.W
            lastForkHomeAndStageSetting = button_LoadUnload_HomeAndStageSetting_HomeSetting;

            SetButtonInSelected(lastForkHomeAndStageSetting, true);
            #endregion
        }

        private Button lastForkHomeAndStageSetting = null;

        private void button_LoadUnload_HomeAndStageSetting_HomeSetting_Click(object sender, EventArgs e)
        {
            HideAll();
            SetButtonInSelected(lastForkHomeAndStageSetting, false);
            tC_ForkHomeAndStageSetting.SelectedIndex = 0;
            lastForkHomeAndStageSetting = button_LoadUnload_HomeAndStageSetting_HomeSetting;
            SetButtonInSelected(lastForkHomeAndStageSetting, true);
        }

        private void button_LoadUnload_HomeAndStageSetting_SaveZ_Click(object sender, EventArgs e)
        {
            HideAll();
            SetButtonInSelected(lastForkHomeAndStageSetting, false);
            tC_ForkHomeAndStageSetting.SelectedIndex = 1;
            lastForkHomeAndStageSetting = button_LoadUnload_HomeAndStageSetting_SaveZ;
            SetButtonInSelected(lastForkHomeAndStageSetting, true);
        }

        private void button_Fork_HomeAndStageSetting_SearchHomeSensorOffset_Click(object sender, EventArgs e)
        {
            if (mainFlow.LoadUnloadControl.LoadUnload != null)
                mainFlow.LoadUnloadControl.LoadUnload.FindHomeSensorOffsetByEncoderInHome();
        }

        private void button_Fork_HomeAndStageSetting_HomeStop_Click(object sender, EventArgs e)
        {
            if (localData.LoadUnloadData.Homing)
                localData.LoadUnloadData.HomeStop = true;
            else if (localData.LoadUnloadData.LoadUnloadCommand == null)
                mainFlow.LoadUnloadControl.LoadUnload.JogStop();
        }

        private void button_SetAlignmentDeviceToZeroGoNext_Click(object sender, EventArgs e)
        {
            localData.LoadUnloadData.ResetAlignmentDeviceToZeroGoNext = true;
        }

        private void button_Fork_HomeAndStageSetting_Alignment_ResetZeroLeft_Click(object sender, EventArgs e)
        {
            if (mainFlow.LoadUnloadControl.LoadUnload != null && localData.LoginLevel >= EnumLoginLevel.Admin &&
                localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null &&
                localData.LoadUnloadData.LoadUnloadCommand == null && !localData.MoveControlData.MotionControlData.JoystickMode)
            {
                mainFlow.LoadUnloadControl.LoadUnload.SetAlignmentDeviceToZero(EnumStageDirection.Left);
            }
        }

        private void button_Fork_HomeAndStageSetting_Alignment_ResetZeroRight_Click(object sender, EventArgs e)
        {
            if (mainFlow.LoadUnloadControl.LoadUnload != null && localData.LoginLevel >= EnumLoginLevel.Admin &&
                localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null &&
                localData.LoadUnloadData.LoadUnloadCommand == null && !localData.MoveControlData.MotionControlData.JoystickMode)
            {
                mainFlow.LoadUnloadControl.LoadUnload.SetAlignmentDeviceToZero(EnumStageDirection.Right);
            }
        }

        private void button_SaveZ_ChangeMode_Click(object sender, EventArgs e)
        {
            if (localData.LoadUnloadData.平衡Z軸)
                localData.LoadUnloadData.平衡Z軸 = false;
            else
            {
                if (localData.LoginLevel >= EnumLoginLevel.Admin &&
                    localData.AutoManual == EnumAutoState.Manual &&
                    localData.MoveControlData.MoveCommand == null &&
                    localData.LoadUnloadData.LoadUnloadCommand == null &&
                    !localData.MoveControlData.MotionControlData.JoystickMode &&
                    !localData.LoadUnloadData.Homing)
                {
                    localData.LoadUnloadData.平衡Z軸 = true;

                    mainFlow.MipcControl.SendMIPCDataByMIPCTagName(
                        new List<string>()
                        {   String.Concat(EnumLoadUnloadAxisName.Z軸.ToString(), "_", EnumLoadUnloadAxisCommandType.ServoOn),
                        String.Concat(EnumLoadUnloadAxisName.Z軸_Slave.ToString(), "_", EnumLoadUnloadAxisCommandType.ServoOn)
                        },
                        new List<float>()
                        { (float)EnumMIPCServoOnOffValue.ServoOn,(float)EnumMIPCServoOnOffValue.ServoOff });
                }
            }
        }

        private void button_SaveZ_ServoOnZAndServoOffZSlave_Click(object sender, EventArgs e)
        {
            if (localData.LoadUnloadData.平衡Z軸)
            {
                mainFlow.MipcControl.SendMIPCDataByMIPCTagName(
                    new List<string>()
                    {   String.Concat(EnumLoadUnloadAxisName.Z軸.ToString(), "_", EnumLoadUnloadAxisCommandType.ServoOn),
                        String.Concat(EnumLoadUnloadAxisName.Z軸_Slave.ToString(), "_", EnumLoadUnloadAxisCommandType.ServoOn)
                    },
                    new List<float>()
                    { (float)EnumMIPCServoOnOffValue.ServoOn,(float)EnumMIPCServoOnOffValue.ServoOff });
            }
        }

        private void button_ResetZ軸歪掉_Click(object sender, EventArgs e)
        {
            if (localData.LoadUnloadData.平衡Z軸)
            {
                localData.LoadUnloadData.z軸EncoderHome = false;
                mainFlow.MipcControl.SendMIPCDataByIPCTagName(new List<EnumMecanumIPCdefaultTag>() { EnumMecanumIPCdefaultTag.Z軸Encoder歪掉 }, new List<float>() { 0 });
            }
        }

        private void button_SaveZ_MouseUp(object sender, MouseEventArgs e)
        {
            if (localData.LoadUnloadData.平衡Z軸)
                mainFlow.LoadUnloadControl.LoadUnload.JogStop();
        }

        private void button_SaveZ_Up_MouseDown(object sender, MouseEventArgs e)
        {
            if (localData.LoadUnloadData.平衡Z軸)
            {
                if (e.Button == MouseButtons.Left)
                    mainFlow.LoadUnloadControl.LoadUnload.Jog_NoSafety(EnumLoadUnloadAxisName.Z軸.ToString(), true);
            }
        }

        private void button_SaveZ_Down_MouseDown(object sender, MouseEventArgs e)
        {
            if (localData.LoadUnloadData.平衡Z軸)
            {
                if (e.Button == MouseButtons.Left)
                    mainFlow.LoadUnloadControl.LoadUnload.Jog_NoSafety(EnumLoadUnloadAxisName.Z軸.ToString(), false);
            }
        }

        private void button_SaveZ_相對移動_Click(object sender, EventArgs e)
        {
            if (localData.LoadUnloadData.平衡Z軸)
            {
                double deltaZ;

                if (double.TryParse(tB_SaveZ_MoveDistance.Text, out deltaZ) && deltaZ != 0)
                    mainFlow.LoadUnloadControl.LoadUnload.Jog_相對_NoSafety(EnumLoadUnloadAxisName.Z軸.ToString(), deltaZ);
            }
        }

        private void button_SaveZ_Stop_Click(object sender, EventArgs e)
        {
            if (localData.LoadUnloadData.平衡Z軸)
                mainFlow.LoadUnloadControl.LoadUnload.JogStop();
        }

        private void Update_Fork_HomeSetting()
        {

            switch (tC_ForkHomeAndStageSetting.SelectedIndex)
            {
                case 0:
                    #region
                    if (localData.LoginLevel >= EnumLoginLevel.Admin &&
                        localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null &&
                        localData.LoadUnloadData.LoadUnloadCommand == null && !localData.MoveControlData.MotionControlData.JoystickMode &&
                        !localData.LoadUnloadData.Homing)
                    {
                        button_Fork_HomeAndStageSetting_Alignment_ResetZeroLeft.Enabled = true;
                        button_Fork_HomeAndStageSetting_Alignment_ResetZeroRight.Enabled = true;
                    }
                    else
                    {
                        button_Fork_HomeAndStageSetting_Alignment_ResetZeroLeft.Enabled = false;
                        button_Fork_HomeAndStageSetting_Alignment_ResetZeroRight.Enabled = false;
                    }


                    button_Fork_HomeAndStageSetting_HomeStop.Enabled = localData.LoadUnloadData.Homing;
                    label_Fork_HomeAndStageSetting_SearchHomeOffset.Visible = localData.LoadUnloadData.Homing && !localData.LoadUnloadData.ResetZero;

                    label_SetAlignmentDeviceToZeroMessage.Text = localData.LoadUnloadData.ResetAlignmentDeviceToZeroMessage;

                    if (localData.LoadUnloadData.Homing && localData.LoadUnloadData.ResetZero)
                        button_SetAlignmentDeviceToZeroGoNext.Enabled = localData.LoadUnloadData.ResetAlignmentDeviceToZeroCanNextStep &&
                                                                        !localData.LoadUnloadData.ResetAlignmentDeviceToZeroGoNext;
                    else
                        button_SetAlignmentDeviceToZeroGoNext.Enabled = false;

                    button_Fork_HomeAndStageSetting_SearchHomeSensorOffset.Enabled =
                        localData.LoadUnloadData.LoadUnloadCommand == null &&
                        !localData.MoveControlData.MotionControlData.JoystickMode &&
                        !localData.LoadUnloadData.Homing && localData.LoginLevel == EnumLoginLevel.MirleAdmin;
                    #endregion
                    break;
                case 1:
                    #region SaveZ.
                    if (localData.LoadUnloadData.平衡Z軸)
                    {
                        button_SaveZ_ServoOnZAndServoOffZSlave.Enabled = true;
                        button_ResetZ軸歪掉.Enabled = true;
                        button_SaveZ_Up.Enabled = true;
                        button_SaveZ_Down.Enabled = true;
                        button_SaveZ_相對移動.Enabled = true;
                        button_SaveZ_Stop.Enabled = true;
                    }
                    else
                    {
                        button_SaveZ_ServoOnZAndServoOffZSlave.Enabled = false;
                        button_ResetZ軸歪掉.Enabled = false;
                        button_SaveZ_Up.Enabled = false;
                        button_SaveZ_Down.Enabled = false;
                        button_SaveZ_相對移動.Enabled = false;
                        button_SaveZ_Stop.Enabled = false;
                    }

                    SetButtonInWarning(button_SaveZ_ChangeMode, localData.LoadUnloadData.平衡Z軸);

                    AxisFeedbackData temp = null;
                    if (localData.LoadUnloadData.CVFeedbackData.ContainsKey(EnumLoadUnloadAxisName.Z軸.ToString()))
                    {
                        temp = localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸.ToString()];

                        if (temp != null)
                        {
                            label_SaveZ_Z_Encoder.Text = temp.Position.ToString("0.0");
                            label_SaveZ_Z_Velocity.Text = temp.Velocity.ToString("0.00");
                            label_SaveZ_Z_ServoOnOff.Text = temp.AxisServoOnOff.ToString();

                            if (temp.AxisMoveStaus == EnumAxisMoveStatus.Stop)
                                label_SaveZ_Z_MoveStatus.ForeColor = Color.Black;
                            else
                                label_SaveZ_Z_MoveStatus.ForeColor = Color.Red;

                            label_SaveZ_Z_MoveStatus.Text = temp.AxisMoveStaus.ToString();
                        }
                    }

                    if (localData.LoadUnloadData.CVFeedbackData.ContainsKey(EnumLoadUnloadAxisName.Z軸_Slave.ToString()))
                    {
                        temp = localData.LoadUnloadData.CVFeedbackData[EnumLoadUnloadAxisName.Z軸_Slave.ToString()];

                        if (temp != null)
                        {
                            label_SaveZ_ZSlave_Encoder.Text = temp.Position.ToString("0.0");
                            label_SaveZ_ZSlave_Velocity.Text = temp.Velocity.ToString("0.0");
                            label_SaveZ_ZSlave_ServoOnOff.Text = temp.AxisServoOnOff.ToString();

                            if (temp.AxisMoveStaus == EnumAxisMoveStatus.Stop)
                                label_SaveZ_ZSlave_MoveStatus.ForeColor = Color.Black;
                            else
                                label_SaveZ_ZSlave_MoveStatus.ForeColor = Color.Red;

                            label_SaveZ_ZSlave_MoveStatus.Text = temp.AxisMoveStaus.ToString();
                        }
                    }
                    #endregion
                    break;
                default:
                    break;

            }
        }
        #endregion

        #region Charging-Select.
        private void Initial_Charging()
        {
            Initial_Charging_BatteryInfo();
            Initial_Charging_Command();
            Initial_Charging_PIO();
            Initial_Charging_Record();
        }

        private void button_Charging_BatteryInfo_Click(object sender, EventArgs e)
        {
            ChageTabControlByPageIndex((int)EnumProfacePageIndex.Charging_BatteryInfo);
        }

        private void button_Charging_Flow_Click(object sender, EventArgs e)
        {
            ChageTabControlByPageIndex((int)EnumProfacePageIndex.Charging_Command);
        }

        private void button_Charging_PIO_Click(object sender, EventArgs e)
        {
            ChageTabControlByPageIndex((int)EnumProfacePageIndex.Charging_PIO);
        }

        private void button_Charging_Record_Click(object sender, EventArgs e)
        {
            ChageTabControlByPageIndex((int)EnumProfacePageIndex.Charging_Record);
        }

        private void Update_Charging_Select()
        {
        }
        #endregion

        #region Carging-BatteryInfo.
        private List<string> batteryInfoTagList = new List<string>();
        private List<LabelNameAndValue> batteryInfoDataList = new List<LabelNameAndValue>();

        private string batteryClassificationTag = "Battery";

        private void Initial_Charging_BatteryInfo()
        {
            if (mainFlow.MipcControl.AllDataByClassification.ContainsKey(batteryClassificationTag))
            {
                for (int i = 0; i < mainFlow.MipcControl.AllDataByClassification[batteryClassificationTag].Count; i++)
                    batteryInfoTagList.Add(mainFlow.MipcControl.AllDataByClassification[batteryClassificationTag][i].DataName);
            }

            panel_Battery_Info.AutoScroll = true;

            Size nameSize = new Size(450, 30);
            Size valueSize = new Size(150, 30);

            int deltaY = 29;

            int startY = 10;
            int startX = 50;
            LabelNameAndValue temp;

            for (int i = 0; i < batteryInfoTagList.Count; i++)
            {
                temp = new LabelNameAndValue(batteryInfoTagList[i], false, 16);
                temp.ReSize(nameSize, valueSize);

                temp.Location = new Point(startX, startY);

                temp.ClickEvent += Hide_Click;
                temp.MouseUpEvent += Temp_MouseUpEvent;
                temp.MouseDownEvent += Temp_MouseDownEvent;
                temp.MouseMoveEvent += Temp_MouseMoveEvent;

                panel_Battery_Info.Controls.Add(temp);
                batteryInfoDataList.Add(temp);

                startY += deltaY;
            }
        }
        #region Info 畫面移動.
        private void Temp_MouseDownEvent(object sender, Point e)
        {
            batteryInfo_MouseDown(e);
        }

        private void Temp_MouseMoveEvent(object sender, Point e)
        {
            batteryInfo_MouseMove(e);
        }

        private void Temp_MouseUpEvent(object sender, EventArgs e)
        {
            batteryInfo_MouseUp();
        }

        private void panel_Battery_Info_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                batteryInfo_MouseDown(e.Location);
        }

        private void panel_Battery_Info_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                batteryInfo_MouseMove(e.Location);
        }

        private void panel_Battery_Info_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                batteryInfo_MouseUp();
        }

        private void batteryInfo_MouseDown(Point locate)
        {
            batteryInfoMoveLocateStart = new Point(locate.X, locate.Y);
            batteryInfoMouseDown = true;
        }

        private Point batteryInfoMoveLocateStart = new Point(0, 0);
        private bool batteryInfoMouseDown = false;
        private bool batteryInfoMoving = false;

        private void batteryInfo_MouseMove(Point locate)
        {
            if (!batteryInfoMoving && batteryInfoMouseDown)
            {
                batteryInfoMoving = true;
                int deltaX = locate.X - batteryInfoMoveLocateStart.X;
                int deltaY = locate.Y - batteryInfoMoveLocateStart.Y;

                batteryInfoMoveLocateStart = locate;

                panel_Battery_Info.AutoScrollPosition = new Point(-panel_Battery_Info.AutoScrollPosition.X - deltaX,
                                                                  -panel_Battery_Info.AutoScrollPosition.Y - deltaY);
                batteryInfoMoving = false;
            }
        }

        private void batteryInfo_MouseUp()
        {
            batteryInfoMouseDown = false;
        }
        #endregion

        private void Update_Charging_BatteryInfo()
        {
            for (int i = 0; i < batteryInfoTagList.Count; i++)
                batteryInfoDataList[i].SetValueAndColor(localData.MIPCData.GetDataByMIPCTagName(batteryInfoTagList[i]).ToString("0.0"));
        }
        #endregion

        #region Charging-Command.
        private void Initial_Charging_Command()
        {
            if (!localData.MIPCData.CanLeftCharging)
            {
                label_Charging_Command_ConfirmSensorL.Visible = false;
                label_Charging_Command_ConfirmSensorLValue.Visible = false;
                button_Charging_Command_LeftAuto.Visible = false;
                button_Charging_Command_LeftChargingSafety.Visible = false;
            }

            if (!localData.MIPCData.CanRightCharging)
            {
                label_Charging_Command_ConfirmSensorR.Visible = false;
                label_Charging_Command_ConfirmSensorRValue.Visible = false;
                button_Charging_Command_RightAuto.Visible = false;
                button_Charging_Command_RightChargingSafety.Visible = false;
            }
        }

        private void button_Charging_Flow_LeftAuto_Click(object sender, EventArgs e)
        {
            if (localData.AutoManual == EnumAutoState.Manual)
            {
                if (localData.MIPCData.CanLeftCharging)
                {
                    if (((PIOFlow_Charging)localData.MIPCData.LeftChargingPIO).ChargingStep != EnumChargingStatus.Idle)
                    {
                        localData.MIPCData.LeftChargingPIO.StopPIO();
                    }
                    else
                    {
                        localData.MIPCData.LeftChargingPIO.StartPIO();
                    }
                }
            }
        }

        private void button_Charging_Flow_LeftChargingSafety_Click(object sender, EventArgs e)
        {
            if (mainFlow.ActionCanUse(EnumUserAction.Charging_ChargingTest))
            {
                if (localData.MIPCData.CanLeftCharging)
                    ((PIOFlow_Charging)localData.MIPCData.LeftChargingPIO).ChargingSafetyOnOff(!((PIOFlow_Charging)localData.MIPCData.LeftChargingPIO).GetChargingSafetyOnOff);
            }
        }

        private void button_Charging_Flow_RightAuto_Click(object sender, EventArgs e)
        {
            if (localData.AutoManual == EnumAutoState.Manual)
            {
                if (localData.MIPCData.CanRightCharging)
                {
                    if (((PIOFlow_Charging)localData.MIPCData.RightChargingPIO).ChargingStep != EnumChargingStatus.Idle)
                    {
                        localData.MIPCData.RightChargingPIO.StopPIO();
                    }
                    else
                    {
                        localData.MIPCData.RightChargingPIO.StartPIO();
                    }
                }
            }
        }

        private void button_Charging_Flow_RightChargingSafety_Click(object sender, EventArgs e)
        {
            if (mainFlow.ActionCanUse(EnumUserAction.Charging_ChargingTest))
            {
                if (localData.MIPCData.CanRightCharging)
                    ((PIOFlow_Charging)localData.MIPCData.RightChargingPIO).ChargingSafetyOnOff(!((PIOFlow_Charging)localData.MIPCData.RightChargingPIO).GetChargingSafetyOnOff);
            }
        }

        private void Update_Charging_Command()
        {
            label_Charging_Command_Chaging.Visible = localData.MIPCData.Charging;

            if (localData.MIPCData.CanLeftCharging)
            {
                label_Charging_Command_ConfirmSensorLValue.BackColor = (((PIOFlow_Charging)localData.MIPCData.LeftChargingPIO).GetConfirmSensorOnOff) ? Color.Green : Color.Transparent;
                button_Charging_Command_LeftAuto.BackColor = (((PIOFlow_Charging)localData.MIPCData.LeftChargingPIO).ChargingStep != EnumChargingStatus.Idle) ? Color.Red : Color.Transparent;
                button_Charging_Command_LeftChargingSafety.BackColor = (((PIOFlow_Charging)localData.MIPCData.LeftChargingPIO).ManualChargingSafetyOn) ? Color.Red : Color.Transparent;
            }

            if (localData.MIPCData.CanRightCharging)
            {
                label_Charging_Command_ConfirmSensorRValue.BackColor = (((PIOFlow_Charging)localData.MIPCData.RightChargingPIO).GetConfirmSensorOnOff) ? Color.Green : Color.Transparent;
                button_Charging_Command_RightAuto.BackColor = (((PIOFlow_Charging)localData.MIPCData.RightChargingPIO).ChargingStep != EnumChargingStatus.Idle) ? Color.Red : Color.Transparent;
                button_Charging_Command_RightChargingSafety.BackColor = (((PIOFlow_Charging)localData.MIPCData.RightChargingPIO).ManualChargingSafetyOn) ? Color.Red : Color.Transparent;
            }

            label_Charging_Command_BatteryVValue.Text = localData.BatteryInfo.Battery_V.ToString("0.0");
            label_Charging_Command_BatteryAValue.Text = localData.BatteryInfo.Battery_A.ToString("0.0");
            label_Charging_Command_MeterVValue.Text = localData.BatteryInfo.Meter_V.ToString("0.0");
            label_Charging_Command_MeterAValue.Text = localData.BatteryInfo.Meter_A.ToString("0.0");

            if (localData.BatteryInfo.Battery_溫度1 > 0 ||
                localData.BatteryInfo.Battery_溫度2 > 0)
            {
                if (localData.BatteryInfo.Battery_溫度1 > localData.BatteryInfo.Battery_溫度2)
                    label_Charging_Command_BatteryTempValue.Text = localData.BatteryInfo.Battery_溫度1.ToString("0.0");
                else
                    label_Charging_Command_BatteryTempValue.Text = localData.BatteryInfo.Battery_溫度2.ToString("0.0");
            }
            else
                label_Charging_Command_BatteryTempValue.Text = "";
        }
        #endregion

        #region Charging-PIO.
        private List<PIOForm> chargingFormPIOList = new List<PIOForm>();

        private void Initial_Charging_PIO()
        {
            if (localData.MIPCData.CanLeftCharging)
            {
                TabPage leftTabPage = new TabPage();
                leftTabPage.Text = "Left";
                leftTabPage.Size = new Size(768, 339);

                PIOForm leftLoadUnloadPIOForm = new PIOForm(mainFlow.MipcControl, localData.MIPCData.LeftChargingPIO);
                leftLoadUnloadPIOForm.Location = new Point(0, 0);

                leftTabPage.Controls.Add(leftLoadUnloadPIOForm);

                chargingFormPIOList.Add(leftLoadUnloadPIOForm);
                tC_ChargingPIO.TabPages.Add(leftTabPage);
            }

            if (localData.MIPCData.CanRightCharging)
            {
                TabPage rightTagPage = new TabPage();
                rightTagPage.Text = "right";
                rightTagPage.Size = new Size(768, 339);

                PIOForm rightLoadUnloadPIOForm = new PIOForm(mainFlow.MipcControl, localData.MIPCData.RightChargingPIO);
                rightLoadUnloadPIOForm.Location = new Point(0, 0);

                rightTagPage.Controls.Add(rightLoadUnloadPIOForm);

                chargingFormPIOList.Add(rightLoadUnloadPIOForm);
                tC_ChargingPIO.TabPages.Add(rightTagPage);
            }

        }

        private void Update_Charging_PIO()
        {
            if (tC_ChargingPIO.TabPages.Count > 0 && tC_ChargingPIO.SelectedIndex >= 0 && tC_ChargingPIO.SelectedIndex < tC_ChargingPIO.TabPages.Count)
                chargingFormPIOList[tC_ChargingPIO.SelectedIndex].UpdatePIOStatus();
        }
        #endregion

        #region Charging-CommandRecord.
        private void Initial_Charging_Record()
        {

        }

        private void Update_Charging_Record()
        {

        }
        #endregion

        #region IO.
        private LabelList safetySensorTitle;
        private LabelList safetySensorStatus;
        private List<LabelList> allSafetySensor = new List<LabelList>();

        private List<Label> inputLabelList = new List<Label>();
        private List<Label> outputLabelList = new List<Label>();

        private LPMSDataView lpmsDataView = null;
        private int lpmsIndex = -1;

        private Button ioLastSelectButton = null;

        private void Initial_IO()
        {
            safetySensorTitle = new LabelList();
            safetySensorTitle.Location = new Point(2, 10);
            tP_IO_SafetySensor.Controls.Add(safetySensorTitle);
            safetySensorTitle.SetLabelByStringList(new List<string>()
            {
                "DeviceName", EnumSafetyLevel.Alarm.ToString(),
                EnumSafetyLevel.Warn.ToString(), EnumSafetyLevel.EMO.ToString(),
                EnumSafetyLevel.IPCEMO.ToString(), EnumSafetyLevel.EMS.ToString(),
                "緩停", "降速(低)",
                "降速(高)", EnumSafetyLevel.Normal.ToString()
            });

            safetySensorStatus = new LabelList();
            safetySensorStatus.Location = new Point(2, 44);
            tP_IO_SafetySensor.Controls.Add(safetySensorStatus);
            safetySensorStatus.SetLabelByStringList(new List<string>() { GetStringByTag(EnumProfaceStringTag.Status) });

            panel_SafetySensorList.AutoScroll = true;

            LabelList temp;

            for (int i = 0; i < mainFlow.MipcControl.SafetySensorControl.AllSafetySensor.Count; i++)
            {
                temp = new LabelList();
                temp.Location = new Point(1, i * 34);
                panel_SafetySensorList.Controls.Add(temp);
                temp.SetLabelByStringList(new List<string>() { mainFlow.MipcControl.SafetySensorControl.AllSafetySensor[i].Config.Device });
                allSafetySensor.Add(temp);
            }

            foreach (EnumMovingDirection item in (EnumMovingDirection[])Enum.GetValues(typeof(EnumMovingDirection)))
            {
                cB_IO_MovingAreaSensorChange.Items.Add(item.ToString());
            }

            int initialY = 10;

            Label tempLabel;
            int tempY = initialY;

            panel_IO_IO.AutoScroll = true;

            if (mainFlow.MipcControl.AllDataByClassification.ContainsKey("DI"))
            {
                for (int i = 0; i < mainFlow.MipcControl.AllDataByClassification["DI"].Count; i++)
                {
                    tempLabel = new Label();
                    tempLabel.AutoSize = false;
                    tempLabel.BorderStyle = BorderStyle.FixedSingle;
                    tempLabel.TextAlign = ContentAlignment.MiddleLeft;
                    tempLabel.Size = new Size(350, 60);
                    tempLabel.Text = mainFlow.MipcControl.AllDataByClassification["DI"][i].DataName;
                    tempLabel.Font = new System.Drawing.Font("標楷體", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

                    tempLabel.Location = new Point(20, tempY);

                    tempLabel.MouseDown += ioLabel_MouseDown;
                    tempLabel.MouseUp += ioPanel_MouseUp;
                    tempLabel.MouseMove += ioLabel_MouseMove;

                    tempY += 60;
                    panel_IO_IO.Controls.Add(tempLabel);
                    inputLabelList.Add(tempLabel);
                }
            }

            tempY = initialY;

            if (mainFlow.MipcControl.AllDataByClassification.ContainsKey("DO"))
            {
                for (int i = 0; i < mainFlow.MipcControl.AllDataByClassification["DO"].Count; i++)
                {
                    tempLabel = new Label();
                    tempLabel.AutoSize = false;
                    tempLabel.BorderStyle = BorderStyle.FixedSingle;
                    tempLabel.TextAlign = ContentAlignment.MiddleLeft;
                    tempLabel.Size = new Size(350, 60);
                    tempLabel.Text = mainFlow.MipcControl.AllDataByClassification["DO"][i].DataName;
                    tempLabel.Font = new System.Drawing.Font("標楷體", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

                    tempLabel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.outputIOLabel_MouseDown);
                    tempLabel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.outputIOLabel_MouseUp);
                    tempLabel.MouseMove += ioLabel_MouseMove;
                    tempLabel.Location = new Point(390, tempY);

                    tempY += 60;
                    panel_IO_IO.Controls.Add(tempLabel);
                    outputLabelList.Add(tempLabel);
                }
            }

            if (mainFlow.LPMS != null)
            {
                lpmsIndex = tC_IO.TabPages.Count;

                TabPage newTabPage = new TabPage();
                newTabPage.Text = "LPMS";
                newTabPage.Size = tC_IO.TabPages[0].Size;
                lpmsDataView = new LPMSDataView(mainFlow.LPMS);
                lpmsDataView.Size = new Size(780, 400);
                lpmsDataView.Location = new Point(0, 0);
                newTabPage.Controls.Add(lpmsDataView);
                tC_IO.TabPages.Add(newTabPage);
                newTabPage.Click += Hide_Click;
                lpmsDataView.ClickEvent += Hide_Click;

                SetIOButtonName(lpmsIndex, "LPMS");
            }

            ioLastSelectButton = button_IO_SensorSafey;
            SetButtonInSelected(ioLastSelectButton, true);
        }

        private void SetIOButtonName(int index, string name)
        {
            if (index == 2)
            {
                button_IO_F3.Text = name;
            }
            else if (index == 3)
            {
                button_IO_F4.Text = name;
            }

            if (tC_IO.TabPages.Count > 2)
                button_IO_F3.Visible = true;

            if (tC_IO.TabPages.Count > 3)
                button_IO_F4.Visible = true;
        }

        #region IO-頁面切換.
        private void button_IO_SensorSafey_Click(object sender, EventArgs e)
        {
            HideAll();
            SetButtonInSelected(ioLastSelectButton, false);
            ioLastSelectButton = button_IO_SensorSafey;
            tC_IO.SelectedIndex = 0;
            SetButtonInSelected(ioLastSelectButton, true);
        }

        private void button_IO_IOTest_Click(object sender, EventArgs e)
        {
            HideAll();
            SetButtonInSelected(ioLastSelectButton, false);
            ioLastSelectButton = button_IO_IOTest;
            tC_IO.SelectedIndex = 1;
            SetButtonInSelected(ioLastSelectButton, true);
        }

        private void button_IO_F3_Click(object sender, EventArgs e)
        {
            HideAll();
            if (tC_IO.TabPages.Count > 2)
            {
                SetButtonInSelected(ioLastSelectButton, false);
                ioLastSelectButton = button_IO_F3;
                tC_IO.SelectedIndex = 2;
                SetButtonInSelected(ioLastSelectButton, true);
            }
        }

        private void button_IO_F4_Click(object sender, EventArgs e)
        {
            HideAll();
            if (tC_IO.TabPages.Count > 3)
            {
                SetButtonInSelected(ioLastSelectButton, false);
                ioLastSelectButton = button_IO_F4;
                tC_IO.SelectedIndex = 3;
                SetButtonInSelected(ioLastSelectButton, true);
            }
        }
        #endregion

        private void outputIOLabel_MouseDown(object sender, MouseEventArgs e)
        {
            HideAll();

            try
            {
                if (localData.MIPCData.BypassIO)
                    mainFlow.MipcControl.SendMIPCDataByMIPCTagName(new List<string>() { ((Label)sender).Text }, new List<float>() { 1 });
            }
            catch { }

            ioLabel_MouseDown(sender, e);
        }

        private void outputIOLabel_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                if (localData.MIPCData.BypassIO)
                    mainFlow.MipcControl.SendMIPCDataByMIPCTagName(new List<string>() { ((Label)sender).Text }, new List<float>() { 0 });
            }
            catch { }

            ioPanel_MouseUp(sender, e);
        }

        private void cB_IO_MovingAreaSensorChange_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (mainFlow.ActionCanUse(EnumUserAction.IO_ChangeAreaSensorDirection))
            {
                EnumMovingDirection newDirection = EnumMovingDirection.None;

                if (Enum.TryParse(cB_IO_MovingAreaSensorChange.Text, out newDirection))
                    localData.MIPCData.MoveControlDirection = newDirection;
            }
        }

        private void label_IO_IOTest_StopSendIO_Click(object sender, EventArgs e)
        {
            HideAll();

            if (localData.MIPCData.BypassIO)
                localData.MIPCData.BypassIO = false;
            else
            {
                if (mainFlow.ActionCanUse(EnumUserAction.IO_IOTest))
                    localData.MIPCData.BypassIO = true;
            }
        }

        #region IO.畫面移動.
        private void ioLabel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                io_MouseDown(new Point(e.Location.X + ((Label)sender).Location.X, e.Location.Y + ((Label)sender).Location.Y));
        }

        private void ioLabel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                io_MouseMove(new Point(e.Location.X + ((Label)sender).Location.X, e.Location.Y + ((Label)sender).Location.Y));
        }

        private void ioPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                io_MouseDown(e.Location);
        }

        private void ioPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                io_MouseUp();
        }

        private void ioPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                io_MouseMove(e.Location);
        }

        private void io_MouseDown(Point locate)
        {
            ioMoveLocateStart = new Point(locate.X, locate.Y);
            ioMouseDown = true;
        }

        private Point ioMoveLocateStart = new Point(0, 0);
        private bool ioMouseDown = false;
        private bool ioMoving = false;

        private void io_MouseMove(Point locate)
        {
            if (!ioMoving && ioMouseDown)
            {
                ioMoving = true;
                int deltaX = locate.X - ioMoveLocateStart.X;
                int deltaY = locate.Y - ioMoveLocateStart.Y;

                ioMoveLocateStart = locate;

                panel_IO_IO.AutoScrollPosition =
                    new Point(-panel_IO_IO.AutoScrollPosition.X - deltaX,
                              -panel_IO_IO.AutoScrollPosition.Y - deltaY);
                ioMoving = false;
            }
        }

        private void io_MouseUp()
        {
            ioMouseDown = false;
        }
        #endregion

        private void Update_IO()
        {
            cB_IO_MovingAreaSensorChange.Visible = mainFlow.ActionCanUse(EnumUserAction.IO_ChangeAreaSensorDirection);

            switch (tC_IO.SelectedIndex)
            {
                case 0:
                    safetySensorStatus.SetLabelBackColorByUint(mainFlow.MipcControl.SafetySensorControl.AllStatus);

                    for (int i = 0; i < mainFlow.MipcControl.SafetySensorControl.AllSafetySensor.Count; i++)
                        allSafetySensor[i].SetLabelBackColorByUint(mainFlow.MipcControl.SafetySensorControl.AllSafetySensor[i].Status);
                    break;

                case 1:
                    label_IO_IOTest_StopSendIO.Enabled = mainFlow.ActionCanUse(EnumUserAction.IO_IOTest);

                    SetLabelInWarning(label_IO_IOTest_StopSendIO, localData.MIPCData.BypassIO);

                    if (mainFlow.MipcControl.AllDataByClassification.ContainsKey("DI"))
                    {
                        for (int i = 0; i < mainFlow.MipcControl.AllDataByClassification["DI"].Count; i++)
                            SetLabelInSelected(inputLabelList[i], localData.MIPCData.GetDataByMIPCTagName(mainFlow.MipcControl.AllDataByClassification["DI"][i].DataName) != 0);
                    }

                    if (mainFlow.MipcControl.AllDataByClassification.ContainsKey("DO"))
                    {
                        for (int i = 0; i < mainFlow.MipcControl.AllDataByClassification["DO"].Count; i++)
                            SetLabelInSelected(outputLabelList[i], localData.MIPCData.GetDataByMIPCTagName(mainFlow.MipcControl.AllDataByClassification["DO"][i].DataName) != 0);
                    }

                    break;
                default:
                    if (lpmsIndex > 0 && lpmsIndex == tC_IO.SelectedIndex)
                        lpmsDataView.UpdateView();
                    break;
            }
        }
        #endregion

        #region Alarm.
        private bool showAlarmNow = true;

        private void Initial_Alarm()
        {
            SetButtonInSelected(button_Alarm_ShowAlarm, true);
        }

        private void button_Alarm_ShowAlarm_Click(object sender, EventArgs e)
        {
            HideAll();
            SetButtonInSelected(button_Alarm_ShowAlarm, true);
            SetButtonInSelected(button_Alarm_ShowAlarmHistory, false);
            showAlarmNow = true;
        }

        private void button_Alarm_ShowAlarmHistory_Click(object sender, EventArgs e)
        {
            HideAll();
            SetButtonInSelected(button_Alarm_ShowAlarm, false);
            SetButtonInSelected(button_Alarm_ShowAlarmHistory, true);
            OpenAlarmHistory();
            showAlarmNow = false;
        }

        private void button_Alarm_ResetAlarm_Click(object sender, EventArgs e)
        {
            HideAll();
            mainFlow.ResetAlarm();
        }

        private void button_Alarm_BuzzOff_Click(object sender, EventArgs e)
        {
            HideAll();
            localData.MIPCData.BuzzOff = true;
        }

        private void Update_Alarm()
        {
            label_Alarm_NoPower.Visible = !localData.MIPCData.U動力電;
            tbx_Alarm.Text = (showAlarmNow ? mainFlow.AlarmHandler.NowAlarm : mainFlow.AlarmHandler.AlarmHistory);
        }
        #endregion

        #region Parameter.
        private List<SafetySensorByPass> allBypassSensorList = new List<SafetySensorByPass>();
        private List<TextBox> pioTimeoutList = new List<TextBox>();
        private List<Label> pioTimeoutLabelList = new List<Label>();

        private Button lastParamterButton = null;
        private Dictionary<EnumMoveControlSafetyType, MoveControlConfig_Safety> allMoveControl_Safety = new Dictionary<EnumMoveControlSafetyType, MoveControlConfig_Safety>();
        private Dictionary<EnumSensorSafetyType, MoveControlConfig_SensorBypass> allMoveControl_SensorBypass = new Dictionary<EnumSensorSafetyType, MoveControlConfig_SensorBypass>();

        private void Initial_Parameter()
        {
            panel_BypassSafetySensor.AutoScroll = true;

            SafetySensorByPass temp;
            Label tempLabel = null;
            TextBox tempTextBox = null;
            MoveControlConfig_Safety tempSafety;
            MoveControlConfig_SensorBypass tempSensorBypass;

            #region Sensor.
            for (int i = 0; i < mainFlow.MipcControl.SafetySensorControl.AllSafetySensor.Count; i++)
            {
                temp = new SafetySensorByPass(mainFlow.MipcControl.SafetySensorControl.AllSafetySensor[i]);
                temp.Location = new Point(10, i * 40);
                panel_BypassSafetySensor.Controls.Add(temp);
                allBypassSensorList.Add(temp);
            }
            #endregion

            #region BatteryConfig.
            parameter_BatteryConfig_HighBattery_SOC.Text = localData.BatteryConfig.HighBattery_SOC.ToString("0.0");
            parameter_BatteryConfig_HighBattery_V.Text = localData.BatteryConfig.HighBattery_Voltage.ToString("0.0");
            parameter_BatteryConfig_LowBattery_SOC.Text = localData.BatteryConfig.LowBattery_SOC.ToString("0.0");
            parameter_BatteryConfig_LowBattery_V.Text = localData.BatteryConfig.LowBattery_Voltage.ToString("0.0");
            parameter_BatteryConfig_ShowDownBattery_SOC.Text = localData.BatteryConfig.ShutDownBattery_SOC.ToString("0.0");
            parameter_BatteryConfig_ShowDownBattery_V.Text = localData.BatteryConfig.ShutDownBattery_Voltage.ToString("0.0");
            parameter_BatteryConfig_ChargingMaxA.Text = localData.BatteryConfig.ChargingMaxCurrent.ToString("0.0");
            parameter_BatteryConfig_ChargingMaxTemp.Text = localData.BatteryConfig.ChargingMaxTemperature.ToString("0.0");
            parameter_BatteryConfig_WarningTemp.Text = localData.BatteryConfig.Battery_WarningTemperature.ToString("0.0");
            parameter_BatteryConfig_ShowDownTemp.Text = localData.BatteryConfig.Battery_ShowDownTemperature.ToString("0.0");
            parameter_BatteryConfig_AlarmDelayTime.Text = localData.BatteryConfig.AlarmDelayTime.ToString("0.0");
            parameter_BatteryConfig_ShutDownDelayTime.Text = localData.BatteryConfig.ShutDownDelayTime.ToString("0.0");
            #endregion

            int y = 10;

            #region PIOTimeoutSetting.
            for (int i = 0; i < localData.LoadUnloadData.PIOTimeoutList.Count; i++)
            {
                tempLabel = new Label();
                tempLabel.AutoSize = false;
                //tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.TextAlign = ContentAlignment.MiddleCenter;
                tempLabel.Size = new Size(350, 40);
                tempLabel.Text = localData.LoadUnloadData.PIOTimeoutTageList[i].ToString();
                tempLabel.Font = new System.Drawing.Font("標楷體", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

                tempLabel.Click += Hide_Click;
                tempLabel.Location = new Point(10, y);
                this.tP_Parameter_PIOTimeoutSetting.Controls.Add(tempLabel);


                tempTextBox = new TextBox();
                tempTextBox.Size = new Size(200, 40);
                tempTextBox.TextAlign = HorizontalAlignment.Center;
                tempTextBox.Font = new System.Drawing.Font("Times New Roman", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tempTextBox.Text = (localData.LoadUnloadData.PIOTimeoutList[i] / 1000).ToString("0.0");

                tempTextBox.Click += CallKeyboardNumber;
                tempTextBox.Location = new Point(400, y);
                this.tP_Parameter_PIOTimeoutSetting.Controls.Add(tempTextBox);
                pioTimeoutList.Add(tempTextBox);

                tempLabel = new Label();
                tempLabel.AutoSize = false;
                tempLabel.TextAlign = ContentAlignment.MiddleCenter;
                tempLabel.Size = new Size(40, 40);
                tempLabel.Text = "秒";
                tempLabel.Font = new System.Drawing.Font("標楷體", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

                tempLabel.Click += Hide_Click;
                tempLabel.Location = new Point(610, y);
                pioTimeoutLabelList.Add(tempLabel);
                this.tP_Parameter_PIOTimeoutSetting.Controls.Add(tempLabel);

                this.tP_Parameter_PIOTimeoutSetting.Controls.Add(tempLabel);
                y += 50;
            }
            #endregion

            #region MoveControlConfig.
            bool showConfig;

            y = 10;
            foreach (EnumMoveControlSafetyType type in localData.MoveControlData.MoveControlConfig.Safety.Keys)
            {
                tempSafety = new MoveControlConfig_Safety(type);
                tempSafety.Location = new Point(10, y); ;
                tP_Parameter_MoveControlConfig.Controls.Add(tempSafety);
                tempSafety.ClickEvent += Hide_Click;
                tempSafety.GetTextBox.Click += CallKeyboardNumber;
                allMoveControl_Safety.Add(type, tempSafety);
                y += 60;
            }

            y = 10;
            foreach (EnumSensorSafetyType type in localData.MoveControlData.MoveControlConfig.SensorByPass.Keys)
            {
                tempSensorBypass = new MoveControlConfig_SensorBypass(type);

                tempSensorBypass.Location = new Point(450, y); ;
                tP_Parameter_MoveControlConfig.Controls.Add(tempSensorBypass);
                tempSensorBypass.ClickEvent += Hide_Click;
                allMoveControl_SensorBypass.Add(type, tempSensorBypass);
                y += 60;
            }
            #endregion

            lastParamterButton = button_Parameter_SensorSafety;
            SetButtonInSelected(lastParamterButton, true);
        }

        private void button_Parameter_SensorSafety_Click(object sender, EventArgs e)
        {
            HideAll();
            SetButtonInSelected(lastParamterButton, false);
            lastParamterButton = button_Parameter_SensorSafety;
            SetButtonInSelected(lastParamterButton, true);
            tC_Parameter.SelectedIndex = 0;
        }

        private void button_Parameter_BatteryConfig_Click(object sender, EventArgs e)
        {
            HideAll();
            SetButtonInSelected(lastParamterButton, false);
            lastParamterButton = button_Parameter_BatteryConfig;
            SetButtonInSelected(lastParamterButton, true);
            tC_Parameter.SelectedIndex = 1;
        }

        private void button_Parameter_PIOTimeout_Click(object sender, EventArgs e)
        {
            HideAll();
            SetButtonInSelected(lastParamterButton, false);
            lastParamterButton = button_Parameter_PIOTimeout;
            SetButtonInSelected(lastParamterButton, true);
            tC_Parameter.SelectedIndex = 2;
        }

        private void button_Parameter_MoveControlConfig_Click(object sender, EventArgs e)
        {
            HideAll();
            SetButtonInSelected(lastParamterButton, false);
            lastParamterButton = button_Parameter_MoveControlConfig;
            SetButtonInSelected(lastParamterButton, true);
            tC_Parameter.SelectedIndex = 3;
        }

        private void button_Parameter_MainFlowConfig_Click(object sender, EventArgs e)
        {
            HideAll();
            SetButtonInSelected(lastParamterButton, false);
            lastParamterButton = button_Parameter_MainFlowConfig;
            SetButtonInSelected(lastParamterButton, true);
            tC_Parameter.SelectedIndex = 4;
        }


        private void button_Parameter_MainFlowConfig_SavePowerMode_Click(object sender, EventArgs e)
        {
            HideAll();

            if (mainFlow.ActionCanUse(EnumUserAction.Parameter_MainConfig))
            {
                if (localData.MainFlowConfig.PowerSavingMode)
                    localData.MainFlowConfig.PowerSavingMode = false;
                else
                    localData.MainFlowConfig.PowerSavingMode = true;

                mainFlow.WriteMainFlowConfig();
            }
        }

        private void button_Parameter_MainFlowConfig_ZUpMode_Click(object sender, EventArgs e)
        {
            HideAll();

            if (mainFlow.ActionCanUse(EnumUserAction.Parameter_MainConfig))
            {
                if (localData.MainFlowConfig.HomeInUpOrDownPosition)
                    localData.MainFlowConfig.HomeInUpOrDownPosition = false;
                else
                    localData.MainFlowConfig.HomeInUpOrDownPosition = true;

                mainFlow.WriteMainFlowConfig();
            }
        }

        private void button_Parameter_MainFlowConfig_CheckPassLine_Click(object sender, EventArgs e)
        {
            HideAll();

            if (mainFlow.ActionCanUse(EnumUserAction.Parameter_MainConfig))
            {
                if (localData.MainFlowConfig.CheckPassLineSensor)
                    localData.MainFlowConfig.CheckPassLineSensor = false;
                else
                    localData.MainFlowConfig.CheckPassLineSensor = true;

                mainFlow.WriteMainFlowConfig();
            }
        }

        private void button_Parameter_MainFlowConfig_IdleNotLogCSV_Click(object sender, EventArgs e)
        {
            HideAll();

            if (mainFlow.ActionCanUse(EnumUserAction.Parameter_MainConfig))
            {
                if (localData.MainFlowConfig.IdleNotRecordCSV)
                    localData.MainFlowConfig.IdleNotRecordCSV = false;
                else
                    localData.MainFlowConfig.IdleNotRecordCSV = true;

                mainFlow.WriteMainFlowConfig();
            }
        }

        private void button_ParameterSafetySensorReset_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < mainFlow.MipcControl.SafetySensorControl.AllSafetySensor.Count; i++)
            {
                mainFlow.MipcControl.SafetySensorControl.AllSafetySensor[i].ByPassAlarm = 0;
                mainFlow.MipcControl.SafetySensorControl.AllSafetySensor[i].ByPassStatus = 0;
            }
        }

        private void Update_Parameter()
        {
            switch (tC_Parameter.SelectedIndex)
            {
                case 0:
                    for (int i = 0; i < mainFlow.MipcControl.SafetySensorControl.AllSafetySensor.Count; i++)
                        allBypassSensorList[i].UpdateInfo();
                    break;
                case 1:
                    bool Need_Write_BatteryConfig = false;

                    #region 確認是否有修改參數.
                    double highValue_SOC = Convert.ToDouble(parameter_BatteryConfig_HighBattery_SOC.Text);
                    double lowValue_SOC = Convert.ToDouble(parameter_BatteryConfig_LowBattery_SOC.Text);
                    double shutdownValue_SOC = Convert.ToDouble(parameter_BatteryConfig_ShowDownBattery_SOC.Text);
                    double highValue_V = Convert.ToDouble(parameter_BatteryConfig_HighBattery_V.Text);
                    double lowValue_V = Convert.ToDouble(parameter_BatteryConfig_LowBattery_V.Text);
                    double shutdown_V = Convert.ToDouble(parameter_BatteryConfig_ShowDownBattery_V.Text);
                    double MaxA = Convert.ToDouble(parameter_BatteryConfig_ChargingMaxA.Text);
                    double chargingMaxTemp = Convert.ToDouble(parameter_BatteryConfig_ChargingMaxTemp.Text);
                    double WarningTemp = Convert.ToDouble(parameter_BatteryConfig_WarningTemp.Text);
                    double ShutdownTemp = Convert.ToDouble(parameter_BatteryConfig_ShowDownTemp.Text);
                    double AlarmDelay = Convert.ToDouble(parameter_BatteryConfig_AlarmDelayTime.Text);
                    double ShutDownDelay = Convert.ToDouble(parameter_BatteryConfig_ShutDownDelayTime.Text);

                    if (parameter_BatteryConfig_HighBattery_SOC.Text != localData.BatteryConfig.HighBattery_SOC.ToString("0.0"))
                    {
                        if (highValue_SOC > lowValue_SOC && highValue_SOC > shutdownValue_SOC && 0 < highValue_SOC && highValue_SOC < 102)
                        {
                            localData.BatteryConfig.HighBattery_SOC = highValue_SOC;
                            Need_Write_BatteryConfig = true;
                        }

                        parameter_BatteryConfig_HighBattery_SOC.Text = localData.BatteryConfig.HighBattery_SOC.ToString("0.0");
                    }
                    if (parameter_BatteryConfig_LowBattery_SOC.Text != localData.BatteryConfig.LowBattery_SOC.ToString("0.0"))
                    {
                        if (highValue_SOC > lowValue_SOC && lowValue_SOC > shutdownValue_SOC && 0 < lowValue_SOC)
                        {
                            localData.BatteryConfig.LowBattery_SOC = lowValue_SOC;
                            Need_Write_BatteryConfig = true;
                        }

                        parameter_BatteryConfig_LowBattery_SOC.Text = localData.BatteryConfig.LowBattery_SOC.ToString("0.0");
                    }
                    if (parameter_BatteryConfig_ShowDownBattery_SOC.Text != localData.BatteryConfig.ShutDownBattery_SOC.ToString("0.0"))
                    {
                        if (shutdownValue_SOC < highValue_SOC && shutdownValue_SOC < lowValue_SOC && shutdownValue_SOC >= 0)
                        {
                            localData.BatteryConfig.ShutDownBattery_SOC = shutdownValue_SOC;
                            mainFlow.MipcControl.SendMIPCDataByIPCTagName(new List<EnumMecanumIPCdefaultTag>() { EnumMecanumIPCdefaultTag.ShutDown_SOC },
                                new List<float>() { (float)((localData.BatteryConfig.ShutDownBattery_SOC - 3) >= 0 ? localData.BatteryConfig.ShutDownBattery_SOC - 3 : 0) });
                            Need_Write_BatteryConfig = true;
                        }

                        parameter_BatteryConfig_ShowDownBattery_SOC.Text = localData.BatteryConfig.ShutDownBattery_SOC.ToString("0.0");
                    }
                    if (parameter_BatteryConfig_HighBattery_V.Text != localData.BatteryConfig.HighBattery_Voltage.ToString("0.0"))
                    {
                        if (highValue_V > lowValue_V && highValue_V > shutdown_V && 0 < highValue_V && highValue_V < 60)
                        {
                            localData.BatteryConfig.HighBattery_Voltage = highValue_V;
                            Need_Write_BatteryConfig = true;
                        }

                        parameter_BatteryConfig_HighBattery_V.Text = localData.BatteryConfig.HighBattery_Voltage.ToString("0.0");
                    }
                    if (parameter_BatteryConfig_LowBattery_V.Text != localData.BatteryConfig.LowBattery_Voltage.ToString("0.0"))
                    {
                        if (highValue_V > lowValue_V && lowValue_V > shutdown_V && lowValue_V > 0)
                        {
                            localData.BatteryConfig.LowBattery_Voltage = lowValue_V;
                            Need_Write_BatteryConfig = true;
                        }

                        parameter_BatteryConfig_LowBattery_V.Text = localData.BatteryConfig.LowBattery_Voltage.ToString("0.0");
                    }
                    if (parameter_BatteryConfig_ShowDownBattery_V.Text != localData.BatteryConfig.ShutDownBattery_Voltage.ToString("0.0"))
                    {
                        if (highValue_V > shutdown_V && lowValue_V > shutdown_V && shutdown_V >= 0)
                        {
                            localData.BatteryConfig.ShutDownBattery_Voltage = shutdown_V;
                            mainFlow.MipcControl.SendMIPCDataByIPCTagName(new List<EnumMecanumIPCdefaultTag>() { EnumMecanumIPCdefaultTag.ShutDown_V },
                                new List<float>() { (float)((localData.BatteryConfig.ShutDownBattery_SOC - 0.5) >= 0 ? localData.BatteryConfig.ShutDownBattery_SOC - 0.5 : 0) });
                            Need_Write_BatteryConfig = true;
                        }

                        parameter_BatteryConfig_ShowDownBattery_V.Text = localData.BatteryConfig.ShutDownBattery_Voltage.ToString("0.0");
                    }
                    if (parameter_BatteryConfig_ChargingMaxA.Text != localData.BatteryConfig.ChargingMaxCurrent.ToString("0.0"))
                    {
                        if (0 < MaxA)
                        {
                            localData.BatteryConfig.ChargingMaxCurrent = MaxA;
                            Need_Write_BatteryConfig = true;
                        }

                        parameter_BatteryConfig_ChargingMaxA.Text = localData.BatteryConfig.ChargingMaxCurrent.ToString("0.0");
                    }
                    if (parameter_BatteryConfig_ChargingMaxTemp.Text != localData.BatteryConfig.ChargingMaxTemperature.ToString("0.0"))
                    {
                        if (0 < chargingMaxTemp)
                        {
                            localData.BatteryConfig.ChargingMaxTemperature = chargingMaxTemp;
                            Need_Write_BatteryConfig = true;
                        }

                        parameter_BatteryConfig_ChargingMaxTemp.Text = localData.BatteryConfig.ChargingMaxTemperature.ToString("0.0");
                    }
                    if (parameter_BatteryConfig_WarningTemp.Text != localData.BatteryConfig.Battery_WarningTemperature.ToString("0.0"))
                    {
                        if (ShutdownTemp > WarningTemp && WarningTemp > 0)
                        {
                            localData.BatteryConfig.Battery_WarningTemperature = WarningTemp;
                            Need_Write_BatteryConfig = true;
                        }

                        parameter_BatteryConfig_WarningTemp.Text = localData.BatteryConfig.Battery_WarningTemperature.ToString("0.0");
                    }
                    if (parameter_BatteryConfig_ShowDownTemp.Text != localData.BatteryConfig.Battery_ShowDownTemperature.ToString("0.0"))
                    {
                        if (ShutdownTemp > WarningTemp && ShutdownTemp > 0)
                        {
                            localData.BatteryConfig.Battery_ShowDownTemperature = ShutdownTemp;
                            Need_Write_BatteryConfig = true;
                        }

                        parameter_BatteryConfig_ShowDownTemp.Text = localData.BatteryConfig.Battery_ShowDownTemperature.ToString("0.0");
                    }
                    if (parameter_BatteryConfig_AlarmDelayTime.Text != localData.BatteryConfig.AlarmDelayTime.ToString("0.0"))
                    {
                        if (AlarmDelay > 0)
                        {
                            localData.BatteryConfig.AlarmDelayTime = AlarmDelay;
                            Need_Write_BatteryConfig = true;
                        }

                        parameter_BatteryConfig_AlarmDelayTime.Text = localData.BatteryConfig.AlarmDelayTime.ToString("0.0");
                    }
                    if (parameter_BatteryConfig_ShutDownDelayTime.Text != localData.BatteryConfig.ShutDownDelayTime.ToString("0.0"))
                    {
                        if (ShutDownDelay > 0)
                        {
                            localData.BatteryConfig.ShutDownDelayTime = ShutDownDelay;
                            Need_Write_BatteryConfig = true;
                        }

                        parameter_BatteryConfig_ShutDownDelayTime.Text = localData.BatteryConfig.ShutDownDelayTime.ToString("0.0");
                    }
                    #endregion

                    if (Need_Write_BatteryConfig)
                        mainFlow.WriteBatteryConfig();
                    break;
                case 2:
                    #region 確認數值是否需要修改.
                    bool needWritePIOCSV = false;
                    double tempPIOValue;

                    for (int i = 0; i < pioTimeoutList.Count; i++)
                    {
                        if ((localData.LoadUnloadData.PIOTimeoutList[i] / 1000).ToString("0.0") != pioTimeoutList[i].Text)
                        {
                            if (double.TryParse(pioTimeoutList[i].Text, out tempPIOValue) &&
                                tempPIOValue >= 0)
                            {
                                tempPIOValue = Math.Round(tempPIOValue, 2);
                                localData.LoadUnloadData.PIOTimeoutList[i] = tempPIOValue * 1000;
                                needWritePIOCSV = true;
                            }

                            pioTimeoutList[i].Text = (localData.LoadUnloadData.PIOTimeoutList[i] / 1000).ToString("0.0");
                        }
                    }

                    if (needWritePIOCSV)
                        mainFlow.LoadUnloadControl.LoadUnload.WritePIOTimeoutCSV();
                    #endregion

                    break;
                case 3:
                    #region MoveControlConfig.
                    bool needWriteMoveControlSensorConfig = false;

                    foreach (EnumMoveControlSafetyType type in (EnumMoveControlSafetyType[])Enum.GetValues(typeof(EnumMoveControlSafetyType)))
                    {
                        if (allMoveControl_Safety.ContainsKey(type))
                        {
                            allMoveControl_Safety[type].UpdateValue(mainFlow.ActionCanUse(EnumUserAction.Parameter_MoveConfig));

                            if (allMoveControl_Safety[type].Change)
                                needWriteMoveControlSensorConfig = true;
                        }
                    }

                    foreach (EnumSensorSafetyType type in (EnumSensorSafetyType[])Enum.GetValues(typeof(EnumSensorSafetyType)))
                    {
                        if (allMoveControl_SensorBypass.ContainsKey(type))
                        {
                            allMoveControl_SensorBypass[type].UpdateValue(mainFlow.ActionCanUse(EnumUserAction.Parameter_MoveConfig));

                            if (allMoveControl_SensorBypass[type].Change)
                                needWriteMoveControlSensorConfig = true;
                        }
                    }

                    if (needWriteMoveControlSensorConfig)
                    {
                    }
                    #endregion
                    break;
                case 4:
                    #region MainFlowConfig.
                    if (localData.MainFlowConfig.PowerSavingMode)
                    {
                        button_Parameter_MainFlowConfig_SavePowerMode.Text = "啟用中";
                        button_Parameter_MainFlowConfig_SavePowerMode.ForeColor = Color.DarkRed;
                    }
                    else
                    {
                        button_Parameter_MainFlowConfig_SavePowerMode.Text = "關閉中";
                        button_Parameter_MainFlowConfig_SavePowerMode.ForeColor = Color.Black;
                    }

                    if (localData.MainFlowConfig.HomeInUpOrDownPosition)
                    {
                        button_Parameter_MainFlowConfig_ZUpMode.Text = "啟用中";
                        button_Parameter_MainFlowConfig_ZUpMode.ForeColor = Color.DarkRed;
                    }
                    else
                    {
                        button_Parameter_MainFlowConfig_ZUpMode.Text = "關閉中";
                        button_Parameter_MainFlowConfig_ZUpMode.ForeColor = Color.Black;
                    }

                    if (localData.MainFlowConfig.CheckPassLineSensor)
                    {
                        button_Parameter_MainFlowConfig_CheckPassLine.Text = "啟用中";
                        button_Parameter_MainFlowConfig_CheckPassLine.ForeColor = Color.DarkRed;
                    }
                    else
                    {
                        button_Parameter_MainFlowConfig_CheckPassLine.Text = "關閉中";
                        button_Parameter_MainFlowConfig_CheckPassLine.ForeColor = Color.Black;
                    }

                    if (localData.MainFlowConfig.IdleNotRecordCSV)
                    {
                        button_Parameter_MainFlowConfig_IdleNotLogCSV.Text = "啟用中";
                        button_Parameter_MainFlowConfig_IdleNotLogCSV.ForeColor = Color.DarkRed;
                    }
                    else
                    {
                        button_Parameter_MainFlowConfig_IdleNotLogCSV.Text = "關閉中";
                        button_Parameter_MainFlowConfig_IdleNotLogCSV.ForeColor = Color.Black;
                    }
                    #endregion
                    break;
                default:
                    break;
            }
        }
        #endregion

        private void timer_Tick(object sender, EventArgs e)
        {
            try
            {
                Update_Proface();
                tC_Info.TabPages[tC_Info.SelectedIndex].Enabled = mainFlow.TabPageEnable((EnumProfacePageIndex)tC_Info.SelectedIndex);

                switch (tC_Info.SelectedIndex)
                {
                    #region Main.
                    case (int)EnumProfacePageIndex.Main:
                        Update_Main();
                        break;
                    #endregion

                    #region MoveControl.
                    case (int)EnumProfacePageIndex.Move_Select:
                        Update_Move_Select();
                        break;
                    case (int)EnumProfacePageIndex.Move_Jog:
                        Update_Move_Jog();
                        break;
                    case (int)EnumProfacePageIndex.Move_Map:
                        Update_Move_Map();
                        break;
                    case (int)EnumProfacePageIndex.Move_DataInfo:
                        Update_Move_DataInfo();
                        break;
                    case (int)EnumProfacePageIndex.Move_AxisData:
                        Update_Move_AxisData();
                        break;
                    case (int)EnumProfacePageIndex.Move_LocateDriver:
                        Update_Move_LocateDriver();
                        break;
                    case (int)EnumProfacePageIndex.Move_CommandRecord:
                        Update_Move_CommandRecord();
                        break;
                    case (int)EnumProfacePageIndex.Move_SetSlamPosition:
                        Update_Move_SetSlamPosition();
                        break;
                    #endregion

                    #region LoadUnload.
                    case (int)EnumProfacePageIndex.Fork_Jog:
                        Update_Fork_Jog();
                        break;
                    case (int)EnumProfacePageIndex.Fork_Home:
                        Update_Fork_Home();
                        break;
                    case (int)EnumProfacePageIndex.Fork_Command:
                        Update_Fork_Command();
                        break;
                    case (int)EnumProfacePageIndex.Fork_Alignment:
                        Update_Fork_Alignment();
                        break;
                    case (int)EnumProfacePageIndex.Fork_PIO:
                        Update_Fork_PIO();
                        break;
                    case (int)EnumProfacePageIndex.Fork_AxisData:
                        Update_Fork_AxisData();
                        break;
                    case (int)EnumProfacePageIndex.Fork_CommandRecord:
                        Update_Fork_CommandRecord();
                        break;
                    case (int)EnumProfacePageIndex.Fork_HomeSetting_UMTC:
                        Update_Fork_HomeSetting();
                        break;
                    #endregion

                    #region Charging.
                    case (int)EnumProfacePageIndex.Charging_Select:
                        Update_Charging_Select();
                        break;
                    case (int)EnumProfacePageIndex.Charging_BatteryInfo:
                        Update_Charging_BatteryInfo();
                        break;
                    case (int)EnumProfacePageIndex.Charging_Command:
                        Update_Charging_Command();
                        break;
                    case (int)EnumProfacePageIndex.Charging_PIO:
                        Update_Charging_PIO();
                        break;
                    case (int)EnumProfacePageIndex.Charging_Record:
                        tB_ChargingRecord.Text = localData.MIPCData.ChargingMessage;
                        break;

                    #endregion
                    #region IO.
                    case (int)EnumProfacePageIndex.IO:
                        Update_IO();
                        break;
                    #endregion

                    #region Alarm.
                    case (int)EnumProfacePageIndex.Alarm:
                        Update_Alarm();
                        break;
                    #endregion

                    #region Parameter.
                    case (int)EnumProfacePageIndex.Parameter:
                        Update_Parameter();
                        break;
                    #endregion

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                if (lastException != ex.ToString())
                {
                    mainFlow.WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
                    lastException = ex.ToString();
                }
            }
        }

        private string lastException = "";
    }
}