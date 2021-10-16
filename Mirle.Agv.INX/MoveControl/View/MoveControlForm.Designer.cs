namespace Mirle.Agv.INX.View
{
    partial class MoveControlForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tC_MoveControl = new System.Windows.Forms.TabControl();
            this.tP_CreateMoveCommand = new System.Windows.Forms.TabPage();
            this.panel_AdminAdressIDAdd = new System.Windows.Forms.Panel();
            this.button_AdmiinAddressIDAdd = new System.Windows.Forms.Button();
            this.tB_AdmiinAddressID = new System.Windows.Forms.TextBox();
            this.button_DebugModeSend = new System.Windows.Forms.Button();
            this.btnClearMoveCmdInfo = new System.Windows.Forms.Button();
            this.button_CreateCommandList_Stop = new System.Windows.Forms.Button();
            this.panel_CreateCommand_Hide = new System.Windows.Forms.Panel();
            this.button_AutoCycleRun = new System.Windows.Forms.Button();
            this.label_CreateCommand_Angle = new System.Windows.Forms.Label();
            this.label_CreateCommand_Y = new System.Windows.Forms.Label();
            this.label_CreateCommand_X = new System.Windows.Forms.Label();
            this.button_SetRealPosition = new System.Windows.Forms.Button();
            this.tB_CreateCommand_Angle = new System.Windows.Forms.TextBox();
            this.tB_CreateCommand_Y = new System.Windows.Forms.TextBox();
            this.tB_CreateCommand_X = new System.Windows.Forms.TextBox();
            this.gB_SpeedSelect = new System.Windows.Forms.GroupBox();
            this.rB_SecrtionSpeed = new System.Windows.Forms.RadioButton();
            this.rB_SettingSpeed = new System.Windows.Forms.RadioButton();
            this.button_CheckAddress = new System.Windows.Forms.Button();
            this.label_AddressList = new System.Windows.Forms.Label();
            this.AddressList = new System.Windows.Forms.ListBox();
            this.label_AddressFromMainForm = new System.Windows.Forms.Label();
            this.MainForm_AddressList = new System.Windows.Forms.ListBox();
            this.tP_MoveCommand = new System.Windows.Forms.TabPage();
            this.label_LoopTime = new System.Windows.Forms.Label();
            this.button_ResetAlarm = new System.Windows.Forms.Button();
            this.button_SendList = new System.Windows.Forms.Button();
            this.button_StopMove = new System.Windows.Forms.Button();
            this.label_StopReasonValue = new System.Windows.Forms.Label();
            this.label_StopReason = new System.Windows.Forms.Label();
            this.label_MoveCommandID = new System.Windows.Forms.Label();
            this.label_MoveCommandIDLabel = new System.Windows.Forms.Label();
            this.cB_GetAllReserve = new System.Windows.Forms.CheckBox();
            this.label_ReserveList = new System.Windows.Forms.Label();
            this.ReserveList = new System.Windows.Forms.ListBox();
            this.CommandList = new System.Windows.Forms.ListBox();
            this.tP_MoveControlCommand = new System.Windows.Forms.TabPage();
            this.button_YO = new System.Windows.Forms.Button();
            this.tB_AddressB = new System.Windows.Forms.TextBox();
            this.tB_AddressA = new System.Windows.Forms.TextBox();
            this.tbxLogView = new System.Windows.Forms.TextBox();
            this.button_Hide = new System.Windows.Forms.Button();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.tC_MoveControl.SuspendLayout();
            this.tP_CreateMoveCommand.SuspendLayout();
            this.panel_AdminAdressIDAdd.SuspendLayout();
            this.panel_CreateCommand_Hide.SuspendLayout();
            this.gB_SpeedSelect.SuspendLayout();
            this.tP_MoveCommand.SuspendLayout();
            this.tP_MoveControlCommand.SuspendLayout();
            this.SuspendLayout();
            // 
            // tC_MoveControl
            // 
            this.tC_MoveControl.Controls.Add(this.tP_CreateMoveCommand);
            this.tC_MoveControl.Controls.Add(this.tP_MoveCommand);
            this.tC_MoveControl.Controls.Add(this.tP_MoveControlCommand);
            this.tC_MoveControl.Font = new System.Drawing.Font("新細明體", 16F);
            this.tC_MoveControl.Location = new System.Drawing.Point(0, 0);
            this.tC_MoveControl.Name = "tC_MoveControl";
            this.tC_MoveControl.SelectedIndex = 0;
            this.tC_MoveControl.Size = new System.Drawing.Size(780, 408);
            this.tC_MoveControl.TabIndex = 0;
            this.tC_MoveControl.SelectedIndexChanged += new System.EventHandler(this.tC_MoveControl_SelectedIndexChanged);
            // 
            // tP_CreateMoveCommand
            // 
            this.tP_CreateMoveCommand.Controls.Add(this.panel_AdminAdressIDAdd);
            this.tP_CreateMoveCommand.Controls.Add(this.button_DebugModeSend);
            this.tP_CreateMoveCommand.Controls.Add(this.btnClearMoveCmdInfo);
            this.tP_CreateMoveCommand.Controls.Add(this.button_CreateCommandList_Stop);
            this.tP_CreateMoveCommand.Controls.Add(this.panel_CreateCommand_Hide);
            this.tP_CreateMoveCommand.Controls.Add(this.gB_SpeedSelect);
            this.tP_CreateMoveCommand.Controls.Add(this.button_CheckAddress);
            this.tP_CreateMoveCommand.Controls.Add(this.label_AddressList);
            this.tP_CreateMoveCommand.Controls.Add(this.AddressList);
            this.tP_CreateMoveCommand.Controls.Add(this.label_AddressFromMainForm);
            this.tP_CreateMoveCommand.Controls.Add(this.MainForm_AddressList);
            this.tP_CreateMoveCommand.Font = new System.Drawing.Font("新細明體", 8F);
            this.tP_CreateMoveCommand.Location = new System.Drawing.Point(4, 31);
            this.tP_CreateMoveCommand.Name = "tP_CreateMoveCommand";
            this.tP_CreateMoveCommand.Padding = new System.Windows.Forms.Padding(3);
            this.tP_CreateMoveCommand.Size = new System.Drawing.Size(772, 373);
            this.tP_CreateMoveCommand.TabIndex = 1;
            this.tP_CreateMoveCommand.Text = "產生命令";
            this.tP_CreateMoveCommand.UseVisualStyleBackColor = true;
            // 
            // panel_AdminAdressIDAdd
            // 
            this.panel_AdminAdressIDAdd.Controls.Add(this.button_AdmiinAddressIDAdd);
            this.panel_AdminAdressIDAdd.Controls.Add(this.tB_AdmiinAddressID);
            this.panel_AdminAdressIDAdd.Location = new System.Drawing.Point(259, 124);
            this.panel_AdminAdressIDAdd.Name = "panel_AdminAdressIDAdd";
            this.panel_AdminAdressIDAdd.Size = new System.Drawing.Size(225, 51);
            this.panel_AdminAdressIDAdd.TabIndex = 122;
            // 
            // button_AdmiinAddressIDAdd
            // 
            this.button_AdmiinAddressIDAdd.Font = new System.Drawing.Font("新細明體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.button_AdmiinAddressIDAdd.Location = new System.Drawing.Point(128, 12);
            this.button_AdmiinAddressIDAdd.Name = "button_AdmiinAddressIDAdd";
            this.button_AdmiinAddressIDAdd.Size = new System.Drawing.Size(68, 31);
            this.button_AdmiinAddressIDAdd.TabIndex = 123;
            this.button_AdmiinAddressIDAdd.Text = "Add";
            this.button_AdmiinAddressIDAdd.UseVisualStyleBackColor = true;
            this.button_AdmiinAddressIDAdd.Click += new System.EventHandler(this.button_AdmiinAddressIDAdd_Click);
            // 
            // tB_AdmiinAddressID
            // 
            this.tB_AdmiinAddressID.Font = new System.Drawing.Font("新細明體", 12F);
            this.tB_AdmiinAddressID.Location = new System.Drawing.Point(21, 12);
            this.tB_AdmiinAddressID.Name = "tB_AdmiinAddressID";
            this.tB_AdmiinAddressID.Size = new System.Drawing.Size(88, 27);
            this.tB_AdmiinAddressID.TabIndex = 0;
            // 
            // button_DebugModeSend
            // 
            this.button_DebugModeSend.Font = new System.Drawing.Font("新細明體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.button_DebugModeSend.Location = new System.Drawing.Point(629, 327);
            this.button_DebugModeSend.Name = "button_DebugModeSend";
            this.button_DebugModeSend.Size = new System.Drawing.Size(140, 40);
            this.button_DebugModeSend.TabIndex = 121;
            this.button_DebugModeSend.Text = "產生移動命令";
            this.button_DebugModeSend.UseVisualStyleBackColor = true;
            this.button_DebugModeSend.Click += new System.EventHandler(this.button_DebugModeSend_Click);
            // 
            // btnClearMoveCmdInfo
            // 
            this.btnClearMoveCmdInfo.Font = new System.Drawing.Font("新細明體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.btnClearMoveCmdInfo.Location = new System.Drawing.Point(265, 329);
            this.btnClearMoveCmdInfo.Name = "btnClearMoveCmdInfo";
            this.btnClearMoveCmdInfo.Size = new System.Drawing.Size(127, 40);
            this.btnClearMoveCmdInfo.TabIndex = 120;
            this.btnClearMoveCmdInfo.Text = "Clear";
            this.btnClearMoveCmdInfo.UseVisualStyleBackColor = true;
            this.btnClearMoveCmdInfo.Click += new System.EventHandler(this.btnClearMoveCmdInfo_Click);
            // 
            // button_CreateCommandList_Stop
            // 
            this.button_CreateCommandList_Stop.Font = new System.Drawing.Font("新細明體", 16F);
            this.button_CreateCommandList_Stop.Location = new System.Drawing.Point(529, 327);
            this.button_CreateCommandList_Stop.Name = "button_CreateCommandList_Stop";
            this.button_CreateCommandList_Stop.Size = new System.Drawing.Size(82, 41);
            this.button_CreateCommandList_Stop.TabIndex = 116;
            this.button_CreateCommandList_Stop.Text = "Stop";
            this.button_CreateCommandList_Stop.UseVisualStyleBackColor = true;
            this.button_CreateCommandList_Stop.Click += new System.EventHandler(this.button_CreateCommandList_Stop_Click);
            // 
            // panel_CreateCommand_Hide
            // 
            this.panel_CreateCommand_Hide.Controls.Add(this.button_AutoCycleRun);
            this.panel_CreateCommand_Hide.Controls.Add(this.label_CreateCommand_Angle);
            this.panel_CreateCommand_Hide.Controls.Add(this.label_CreateCommand_Y);
            this.panel_CreateCommand_Hide.Controls.Add(this.label_CreateCommand_X);
            this.panel_CreateCommand_Hide.Controls.Add(this.button_SetRealPosition);
            this.panel_CreateCommand_Hide.Controls.Add(this.tB_CreateCommand_Angle);
            this.panel_CreateCommand_Hide.Controls.Add(this.tB_CreateCommand_Y);
            this.panel_CreateCommand_Hide.Controls.Add(this.tB_CreateCommand_X);
            this.panel_CreateCommand_Hide.Location = new System.Drawing.Point(265, 187);
            this.panel_CreateCommand_Hide.Name = "panel_CreateCommand_Hide";
            this.panel_CreateCommand_Hide.Size = new System.Drawing.Size(258, 115);
            this.panel_CreateCommand_Hide.TabIndex = 118;
            // 
            // button_AutoCycleRun
            // 
            this.button_AutoCycleRun.Location = new System.Drawing.Point(8, 24);
            this.button_AutoCycleRun.Name = "button_AutoCycleRun";
            this.button_AutoCycleRun.Size = new System.Drawing.Size(132, 29);
            this.button_AutoCycleRun.TabIndex = 112;
            this.button_AutoCycleRun.Text = "AutoCycleRun";
            this.button_AutoCycleRun.UseVisualStyleBackColor = true;
            this.button_AutoCycleRun.Click += new System.EventHandler(this.button_AutoCycleRun_Click);
            // 
            // label_CreateCommand_Angle
            // 
            this.label_CreateCommand_Angle.AutoSize = true;
            this.label_CreateCommand_Angle.Location = new System.Drawing.Point(182, 55);
            this.label_CreateCommand_Angle.Name = "label_CreateCommand_Angle";
            this.label_CreateCommand_Angle.Size = new System.Drawing.Size(30, 11);
            this.label_CreateCommand_Angle.TabIndex = 106;
            this.label_CreateCommand_Angle.Text = "Angle";
            // 
            // label_CreateCommand_Y
            // 
            this.label_CreateCommand_Y.AutoSize = true;
            this.label_CreateCommand_Y.Location = new System.Drawing.Point(118, 55);
            this.label_CreateCommand_Y.Name = "label_CreateCommand_Y";
            this.label_CreateCommand_Y.Size = new System.Drawing.Size(12, 11);
            this.label_CreateCommand_Y.TabIndex = 105;
            this.label_CreateCommand_Y.Text = "Y";
            // 
            // label_CreateCommand_X
            // 
            this.label_CreateCommand_X.AutoSize = true;
            this.label_CreateCommand_X.Location = new System.Drawing.Point(36, 56);
            this.label_CreateCommand_X.Name = "label_CreateCommand_X";
            this.label_CreateCommand_X.Size = new System.Drawing.Size(12, 11);
            this.label_CreateCommand_X.TabIndex = 104;
            this.label_CreateCommand_X.Text = "X";
            // 
            // button_SetRealPosition
            // 
            this.button_SetRealPosition.Font = new System.Drawing.Font("新細明體", 12F);
            this.button_SetRealPosition.Location = new System.Drawing.Point(168, 22);
            this.button_SetRealPosition.Name = "button_SetRealPosition";
            this.button_SetRealPosition.Size = new System.Drawing.Size(87, 32);
            this.button_SetRealPosition.TabIndex = 103;
            this.button_SetRealPosition.Text = "設定位置";
            this.button_SetRealPosition.UseVisualStyleBackColor = true;
            this.button_SetRealPosition.Click += new System.EventHandler(this.button_SetRealPosition_Click);
            // 
            // tB_CreateCommand_Angle
            // 
            this.tB_CreateCommand_Angle.Location = new System.Drawing.Point(172, 70);
            this.tB_CreateCommand_Angle.Name = "tB_CreateCommand_Angle";
            this.tB_CreateCommand_Angle.Size = new System.Drawing.Size(83, 20);
            this.tB_CreateCommand_Angle.TabIndex = 102;
            // 
            // tB_CreateCommand_Y
            // 
            this.tB_CreateCommand_Y.Location = new System.Drawing.Point(90, 70);
            this.tB_CreateCommand_Y.Name = "tB_CreateCommand_Y";
            this.tB_CreateCommand_Y.Size = new System.Drawing.Size(83, 20);
            this.tB_CreateCommand_Y.TabIndex = 101;
            // 
            // tB_CreateCommand_X
            // 
            this.tB_CreateCommand_X.Location = new System.Drawing.Point(8, 70);
            this.tB_CreateCommand_X.Name = "tB_CreateCommand_X";
            this.tB_CreateCommand_X.Size = new System.Drawing.Size(83, 20);
            this.tB_CreateCommand_X.TabIndex = 75;
            // 
            // gB_SpeedSelect
            // 
            this.gB_SpeedSelect.Controls.Add(this.rB_SecrtionSpeed);
            this.gB_SpeedSelect.Controls.Add(this.rB_SettingSpeed);
            this.gB_SpeedSelect.Font = new System.Drawing.Font("新細明體", 16F);
            this.gB_SpeedSelect.Location = new System.Drawing.Point(259, 7);
            this.gB_SpeedSelect.Name = "gB_SpeedSelect";
            this.gB_SpeedSelect.Size = new System.Drawing.Size(167, 103);
            this.gB_SpeedSelect.TabIndex = 74;
            this.gB_SpeedSelect.TabStop = false;
            this.gB_SpeedSelect.Text = "速度選擇";
            // 
            // rB_SecrtionSpeed
            // 
            this.rB_SecrtionSpeed.AutoSize = true;
            this.rB_SecrtionSpeed.Location = new System.Drawing.Point(21, 64);
            this.rB_SecrtionSpeed.Name = "rB_SecrtionSpeed";
            this.rB_SecrtionSpeed.Size = new System.Drawing.Size(133, 26);
            this.rB_SecrtionSpeed.TabIndex = 76;
            this.rB_SecrtionSpeed.TabStop = true;
            this.rB_SecrtionSpeed.Text = "Section速度";
            this.rB_SecrtionSpeed.UseVisualStyleBackColor = true;
            // 
            // rB_SettingSpeed
            // 
            this.rB_SettingSpeed.AutoSize = true;
            this.rB_SettingSpeed.Location = new System.Drawing.Point(21, 32);
            this.rB_SettingSpeed.Name = "rB_SettingSpeed";
            this.rB_SettingSpeed.Size = new System.Drawing.Size(116, 26);
            this.rB_SettingSpeed.TabIndex = 75;
            this.rB_SettingSpeed.TabStop = true;
            this.rB_SettingSpeed.Text = "設定速度";
            this.rB_SettingSpeed.UseVisualStyleBackColor = true;
            // 
            // button_CheckAddress
            // 
            this.button_CheckAddress.Font = new System.Drawing.Font("新細明體", 16F);
            this.button_CheckAddress.Location = new System.Drawing.Point(396, 328);
            this.button_CheckAddress.Name = "button_CheckAddress";
            this.button_CheckAddress.Size = new System.Drawing.Size(127, 40);
            this.button_CheckAddress.TabIndex = 71;
            this.button_CheckAddress.Text = "補Address";
            this.button_CheckAddress.UseVisualStyleBackColor = true;
            this.button_CheckAddress.Click += new System.EventHandler(this.button_CheckAddress_Click);
            // 
            // label_AddressList
            // 
            this.label_AddressList.AutoSize = true;
            this.label_AddressList.Font = new System.Drawing.Font("新細明體", 12F);
            this.label_AddressList.Location = new System.Drawing.Point(136, 8);
            this.label_AddressList.Name = "label_AddressList";
            this.label_AddressList.Size = new System.Drawing.Size(86, 16);
            this.label_AddressList.TabIndex = 70;
            this.label_AddressList.Text = "Address List";
            // 
            // AddressList
            // 
            this.AddressList.Font = new System.Drawing.Font("新細明體", 12F);
            this.AddressList.FormattingEnabled = true;
            this.AddressList.ItemHeight = 16;
            this.AddressList.Location = new System.Drawing.Point(134, 28);
            this.AddressList.Name = "AddressList";
            this.AddressList.ScrollAlwaysVisible = true;
            this.AddressList.Size = new System.Drawing.Size(120, 340);
            this.AddressList.TabIndex = 69;
            // 
            // label_AddressFromMainForm
            // 
            this.label_AddressFromMainForm.AutoSize = true;
            this.label_AddressFromMainForm.Font = new System.Drawing.Font("新細明體", 8F);
            this.label_AddressFromMainForm.Location = new System.Drawing.Point(5, 12);
            this.label_AddressFromMainForm.Name = "label_AddressFromMainForm";
            this.label_AddressFromMainForm.Size = new System.Drawing.Size(104, 11);
            this.label_AddressFromMainForm.TabIndex = 68;
            this.label_AddressFromMainForm.Text = "MainForm Address List";
            // 
            // MainForm_AddressList
            // 
            this.MainForm_AddressList.Font = new System.Drawing.Font("新細明體", 12F);
            this.MainForm_AddressList.FormattingEnabled = true;
            this.MainForm_AddressList.ItemHeight = 16;
            this.MainForm_AddressList.Location = new System.Drawing.Point(8, 28);
            this.MainForm_AddressList.Name = "MainForm_AddressList";
            this.MainForm_AddressList.ScrollAlwaysVisible = true;
            this.MainForm_AddressList.Size = new System.Drawing.Size(120, 340);
            this.MainForm_AddressList.TabIndex = 67;
            // 
            // tP_MoveCommand
            // 
            this.tP_MoveCommand.Controls.Add(this.label_LoopTime);
            this.tP_MoveCommand.Controls.Add(this.button_ResetAlarm);
            this.tP_MoveCommand.Controls.Add(this.button_SendList);
            this.tP_MoveCommand.Controls.Add(this.button_StopMove);
            this.tP_MoveCommand.Controls.Add(this.label_StopReasonValue);
            this.tP_MoveCommand.Controls.Add(this.label_StopReason);
            this.tP_MoveCommand.Controls.Add(this.label_MoveCommandID);
            this.tP_MoveCommand.Controls.Add(this.label_MoveCommandIDLabel);
            this.tP_MoveCommand.Controls.Add(this.cB_GetAllReserve);
            this.tP_MoveCommand.Controls.Add(this.label_ReserveList);
            this.tP_MoveCommand.Controls.Add(this.ReserveList);
            this.tP_MoveCommand.Controls.Add(this.CommandList);
            this.tP_MoveCommand.Font = new System.Drawing.Font("新細明體", 9F);
            this.tP_MoveCommand.Location = new System.Drawing.Point(4, 31);
            this.tP_MoveCommand.Name = "tP_MoveCommand";
            this.tP_MoveCommand.Padding = new System.Windows.Forms.Padding(3);
            this.tP_MoveCommand.Size = new System.Drawing.Size(772, 373);
            this.tP_MoveCommand.TabIndex = 0;
            this.tP_MoveCommand.Text = "移動資料";
            this.tP_MoveCommand.UseVisualStyleBackColor = true;
            // 
            // label_LoopTime
            // 
            this.label_LoopTime.AutoSize = true;
            this.label_LoopTime.Font = new System.Drawing.Font("新細明體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_LoopTime.ForeColor = System.Drawing.Color.Red;
            this.label_LoopTime.Location = new System.Drawing.Point(712, 307);
            this.label_LoopTime.Name = "label_LoopTime";
            this.label_LoopTime.Size = new System.Drawing.Size(56, 19);
            this.label_LoopTime.TabIndex = 157;
            this.label_LoopTime.Text = "XXms";
            // 
            // button_ResetAlarm
            // 
            this.button_ResetAlarm.Location = new System.Drawing.Point(558, 335);
            this.button_ResetAlarm.Name = "button_ResetAlarm";
            this.button_ResetAlarm.Size = new System.Drawing.Size(89, 34);
            this.button_ResetAlarm.TabIndex = 156;
            this.button_ResetAlarm.Text = "ResetAlarm";
            this.button_ResetAlarm.UseVisualStyleBackColor = true;
            this.button_ResetAlarm.Click += new System.EventHandler(this.button_ResetAlarm_Click);
            // 
            // button_SendList
            // 
            this.button_SendList.Location = new System.Drawing.Point(648, 335);
            this.button_SendList.Name = "button_SendList";
            this.button_SendList.Size = new System.Drawing.Size(120, 34);
            this.button_SendList.TabIndex = 154;
            this.button_SendList.Text = "執行移動命令";
            this.button_SendList.UseVisualStyleBackColor = true;
            this.button_SendList.Click += new System.EventHandler(this.button_SendList_Click);
            // 
            // button_StopMove
            // 
            this.button_StopMove.Location = new System.Drawing.Point(470, 335);
            this.button_StopMove.Name = "button_StopMove";
            this.button_StopMove.Size = new System.Drawing.Size(86, 34);
            this.button_StopMove.TabIndex = 155;
            this.button_StopMove.Text = "Stop";
            this.button_StopMove.UseVisualStyleBackColor = true;
            this.button_StopMove.Click += new System.EventHandler(this.button_StopMove_Click);
            // 
            // label_StopReasonValue
            // 
            this.label_StopReasonValue.AutoSize = true;
            this.label_StopReasonValue.Font = new System.Drawing.Font("新細明體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_StopReasonValue.ForeColor = System.Drawing.Color.Red;
            this.label_StopReasonValue.Location = new System.Drawing.Point(424, 5);
            this.label_StopReasonValue.Name = "label_StopReasonValue";
            this.label_StopReasonValue.Size = new System.Drawing.Size(110, 19);
            this.label_StopReasonValue.TabIndex = 153;
            this.label_StopReasonValue.Text = "ErrorMessage";
            // 
            // label_StopReason
            // 
            this.label_StopReason.AutoSize = true;
            this.label_StopReason.Font = new System.Drawing.Font("新細明體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_StopReason.Location = new System.Drawing.Point(308, 5);
            this.label_StopReason.Name = "label_StopReason";
            this.label_StopReason.Size = new System.Drawing.Size(110, 19);
            this.label_StopReason.TabIndex = 152;
            this.label_StopReason.Text = "Stop Reason :";
            // 
            // label_MoveCommandID
            // 
            this.label_MoveCommandID.AutoSize = true;
            this.label_MoveCommandID.Font = new System.Drawing.Font("新細明體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_MoveCommandID.Location = new System.Drawing.Point(178, 5);
            this.label_MoveCommandID.Name = "label_MoveCommandID";
            this.label_MoveCommandID.Size = new System.Drawing.Size(57, 19);
            this.label_MoveCommandID.TabIndex = 151;
            this.label_MoveCommandID.Text = "Empty";
            // 
            // label_MoveCommandIDLabel
            // 
            this.label_MoveCommandIDLabel.AutoSize = true;
            this.label_MoveCommandIDLabel.Font = new System.Drawing.Font("新細明體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_MoveCommandIDLabel.Location = new System.Drawing.Point(7, 7);
            this.label_MoveCommandIDLabel.Name = "label_MoveCommandIDLabel";
            this.label_MoveCommandIDLabel.Size = new System.Drawing.Size(165, 19);
            this.label_MoveCommandIDLabel.TabIndex = 150;
            this.label_MoveCommandIDLabel.Text = "Move Command ID :";
            // 
            // cB_GetAllReserve
            // 
            this.cB_GetAllReserve.AutoSize = true;
            this.cB_GetAllReserve.Checked = true;
            this.cB_GetAllReserve.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cB_GetAllReserve.Font = new System.Drawing.Font("新細明體", 12F);
            this.cB_GetAllReserve.Location = new System.Drawing.Point(660, 4);
            this.cB_GetAllReserve.Name = "cB_GetAllReserve";
            this.cB_GetAllReserve.Size = new System.Drawing.Size(107, 20);
            this.cB_GetAllReserve.TabIndex = 149;
            this.cB_GetAllReserve.Text = "取得所有點";
            this.cB_GetAllReserve.UseVisualStyleBackColor = true;
            // 
            // label_ReserveList
            // 
            this.label_ReserveList.AutoSize = true;
            this.label_ReserveList.Font = new System.Drawing.Font("新細明體", 10F);
            this.label_ReserveList.Location = new System.Drawing.Point(571, 7);
            this.label_ReserveList.Name = "label_ReserveList";
            this.label_ReserveList.Size = new System.Drawing.Size(83, 14);
            this.label_ReserveList.TabIndex = 148;
            this.label_ReserveList.Text = "Reserve List :";
            // 
            // ReserveList
            // 
            this.ReserveList.Font = new System.Drawing.Font("新細明體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.ReserveList.FormattingEnabled = true;
            this.ReserveList.HorizontalScrollbar = true;
            this.ReserveList.ItemHeight = 16;
            this.ReserveList.Location = new System.Drawing.Point(595, 27);
            this.ReserveList.Name = "ReserveList";
            this.ReserveList.ScrollAlwaysVisible = true;
            this.ReserveList.Size = new System.Drawing.Size(172, 276);
            this.ReserveList.TabIndex = 146;
            this.ReserveList.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.ReserveList_MouseDoubleClick);
            // 
            // CommandList
            // 
            this.CommandList.Font = new System.Drawing.Font("新細明體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.CommandList.FormattingEnabled = true;
            this.CommandList.HorizontalScrollbar = true;
            this.CommandList.ItemHeight = 16;
            this.CommandList.Location = new System.Drawing.Point(3, 27);
            this.CommandList.Name = "CommandList";
            this.CommandList.ScrollAlwaysVisible = true;
            this.CommandList.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.CommandList.Size = new System.Drawing.Size(586, 276);
            this.CommandList.TabIndex = 145;
            // 
            // tP_MoveControlCommand
            // 
            this.tP_MoveControlCommand.Controls.Add(this.button_YO);
            this.tP_MoveControlCommand.Controls.Add(this.tB_AddressB);
            this.tP_MoveControlCommand.Controls.Add(this.tB_AddressA);
            this.tP_MoveControlCommand.Location = new System.Drawing.Point(4, 31);
            this.tP_MoveControlCommand.Name = "tP_MoveControlCommand";
            this.tP_MoveControlCommand.Size = new System.Drawing.Size(772, 373);
            this.tP_MoveControlCommand.TabIndex = 2;
            this.tP_MoveControlCommand.Text = "指定命令";
            this.tP_MoveControlCommand.UseVisualStyleBackColor = true;
            // 
            // button_YO
            // 
            this.button_YO.Location = new System.Drawing.Point(283, 102);
            this.button_YO.Name = "button_YO";
            this.button_YO.Size = new System.Drawing.Size(152, 54);
            this.button_YO.TabIndex = 2;
            this.button_YO.Text = "button1";
            this.button_YO.UseVisualStyleBackColor = true;
            this.button_YO.Click += new System.EventHandler(this.button_YO_Click);
            // 
            // tB_AddressB
            // 
            this.tB_AddressB.Location = new System.Drawing.Point(98, 143);
            this.tB_AddressB.Name = "tB_AddressB";
            this.tB_AddressB.Size = new System.Drawing.Size(153, 33);
            this.tB_AddressB.TabIndex = 1;
            // 
            // tB_AddressA
            // 
            this.tB_AddressA.Location = new System.Drawing.Point(98, 77);
            this.tB_AddressA.Name = "tB_AddressA";
            this.tB_AddressA.Size = new System.Drawing.Size(153, 33);
            this.tB_AddressA.TabIndex = 0;
            // 
            // tbxLogView
            // 
            this.tbxLogView.Font = new System.Drawing.Font("新細明體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.tbxLogView.Location = new System.Drawing.Point(4, 410);
            this.tbxLogView.MaxLength = 65550;
            this.tbxLogView.Multiline = true;
            this.tbxLogView.Name = "tbxLogView";
            this.tbxLogView.ReadOnly = true;
            this.tbxLogView.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbxLogView.Size = new System.Drawing.Size(776, 151);
            this.tbxLogView.TabIndex = 137;
            // 
            // button_Hide
            // 
            this.button_Hide.Font = new System.Drawing.Font("標楷體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.button_Hide.Location = new System.Drawing.Point(749, -1);
            this.button_Hide.Name = "button_Hide";
            this.button_Hide.Size = new System.Drawing.Size(36, 23);
            this.button_Hide.TabIndex = 136;
            this.button_Hide.Text = "X";
            this.button_Hide.UseVisualStyleBackColor = true;
            this.button_Hide.Click += new System.EventHandler(this.button_Hide_Click);
            // 
            // timer
            // 
            this.timer.Interval = 200;
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // MoveControlForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.ControlBox = false;
            this.Controls.Add(this.tbxLogView);
            this.Controls.Add(this.button_Hide);
            this.Controls.Add(this.tC_MoveControl);
            this.Font = new System.Drawing.Font("新細明體", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "MoveControlForm";
            this.Text = "MoveControlForm";
            this.tC_MoveControl.ResumeLayout(false);
            this.tP_CreateMoveCommand.ResumeLayout(false);
            this.tP_CreateMoveCommand.PerformLayout();
            this.panel_AdminAdressIDAdd.ResumeLayout(false);
            this.panel_AdminAdressIDAdd.PerformLayout();
            this.panel_CreateCommand_Hide.ResumeLayout(false);
            this.panel_CreateCommand_Hide.PerformLayout();
            this.gB_SpeedSelect.ResumeLayout(false);
            this.gB_SpeedSelect.PerformLayout();
            this.tP_MoveCommand.ResumeLayout(false);
            this.tP_MoveCommand.PerformLayout();
            this.tP_MoveControlCommand.ResumeLayout(false);
            this.tP_MoveControlCommand.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl tC_MoveControl;
        private System.Windows.Forms.TabPage tP_CreateMoveCommand;
        private System.Windows.Forms.TabPage tP_MoveCommand;
        private System.Windows.Forms.Button button_Hide;
        private System.Windows.Forms.Timer timer;
        private System.Windows.Forms.Label label_AddressList;
        private System.Windows.Forms.Label label_AddressFromMainForm;
        private System.Windows.Forms.ListBox MainForm_AddressList;
        private System.Windows.Forms.TabPage tP_MoveControlCommand;
        private System.Windows.Forms.TextBox tbxLogView;
        private System.Windows.Forms.Button button_CheckAddress;
        private System.Windows.Forms.GroupBox gB_SpeedSelect;
        private System.Windows.Forms.ListBox AddressList;
        private System.Windows.Forms.Button button_DebugModeSend;
        private System.Windows.Forms.Button btnClearMoveCmdInfo;
        private System.Windows.Forms.Button button_CreateCommandList_Stop;
        private System.Windows.Forms.Panel panel_CreateCommand_Hide;
        private System.Windows.Forms.Button button_AutoCycleRun;
        private System.Windows.Forms.Label label_CreateCommand_Angle;
        private System.Windows.Forms.Label label_CreateCommand_Y;
        private System.Windows.Forms.Label label_CreateCommand_X;
        private System.Windows.Forms.Button button_SetRealPosition;
        private System.Windows.Forms.TextBox tB_CreateCommand_Angle;
        private System.Windows.Forms.TextBox tB_CreateCommand_Y;
        private System.Windows.Forms.TextBox tB_CreateCommand_X;
        private System.Windows.Forms.Label label_LoopTime;
        private System.Windows.Forms.Button button_ResetAlarm;
        private System.Windows.Forms.Button button_SendList;
        private System.Windows.Forms.Button button_StopMove;
        private System.Windows.Forms.Label label_StopReasonValue;
        private System.Windows.Forms.Label label_StopReason;
        private System.Windows.Forms.Label label_MoveCommandID;
        private System.Windows.Forms.Label label_MoveCommandIDLabel;
        private System.Windows.Forms.CheckBox cB_GetAllReserve;
        private System.Windows.Forms.Label label_ReserveList;
        private System.Windows.Forms.ListBox ReserveList;
        private System.Windows.Forms.ListBox CommandList;
        private System.Windows.Forms.RadioButton rB_SecrtionSpeed;
        private System.Windows.Forms.RadioButton rB_SettingSpeed;
        private System.Windows.Forms.Panel panel_AdminAdressIDAdd;
        private System.Windows.Forms.Button button_AdmiinAddressIDAdd;
        private System.Windows.Forms.TextBox tB_AdmiinAddressID;
        private System.Windows.Forms.TextBox tB_AddressB;
        private System.Windows.Forms.TextBox tB_AddressA;
        private System.Windows.Forms.Button button_YO;
    }
}