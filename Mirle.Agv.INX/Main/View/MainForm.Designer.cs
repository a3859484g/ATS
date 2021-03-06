namespace Mirle.Agv.INX.View
{
    partial class MainForm
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
            this.menuStrip_MainForm = new System.Windows.Forms.MenuStrip();
            this.系統ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.關閉ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.模式ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MIPC = new System.Windows.Forms.ToolStripMenuItem();
            this.MoveControl = new System.Windows.Forms.ToolStripMenuItem();
            this.人機畫面ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.牆壁設定ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.panel_Map = new System.Windows.Forms.Panel();
            this.pB_Map = new System.Windows.Forms.PictureBox();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.MovingAddresList = new System.Windows.Forms.ListBox();
            this.button_SendToMoveControlDebug = new System.Windows.Forms.Button();
            this.button_ClearAddressList = new System.Windows.Forms.Button();
            this.button_Alarm = new System.Windows.Forms.Button();
            this.button_BuzzOff = new System.Windows.Forms.Button();
            this.button_AutoManual = new System.Windows.Forms.Button();
            this.label_Alarm = new System.Windows.Forms.Label();
            this.label_Warn = new System.Windows.Forms.Label();
            this.pB_Warn = new System.Windows.Forms.PictureBox();
            this.pB_Alarm = new System.Windows.Forms.PictureBox();
            this.button_SetPosition = new System.Windows.Forms.Button();
            this.label_LocateStatus = new System.Windows.Forms.Label();
            this.pB_reservestatus = new System.Windows.Forms.PictureBox();
            this.label_TestString = new System.Windows.Forms.Label();
            this.menuStrip_MainForm.SuspendLayout();
            this.panel_Map.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pB_Map)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pB_Warn)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pB_Alarm)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pB_reservestatus)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip_MainForm
            // 
            this.menuStrip_MainForm.Font = new System.Drawing.Font("Microsoft JhengHei UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.menuStrip_MainForm.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip_MainForm.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.系統ToolStripMenuItem,
            this.模式ToolStripMenuItem});
            this.menuStrip_MainForm.Location = new System.Drawing.Point(0, 0);
            this.menuStrip_MainForm.Name = "menuStrip_MainForm";
            this.menuStrip_MainForm.Padding = new System.Windows.Forms.Padding(8, 2, 0, 2);
            this.menuStrip_MainForm.Size = new System.Drawing.Size(1300, 33);
            this.menuStrip_MainForm.TabIndex = 1;
            this.menuStrip_MainForm.Text = "menuStrip1";
            // 
            // 系統ToolStripMenuItem
            // 
            this.系統ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.關閉ToolStripMenuItem});
            this.系統ToolStripMenuItem.Name = "系統ToolStripMenuItem";
            this.系統ToolStripMenuItem.Size = new System.Drawing.Size(64, 29);
            this.系統ToolStripMenuItem.Text = "系統";
            // 
            // 關閉ToolStripMenuItem
            // 
            this.關閉ToolStripMenuItem.Name = "關閉ToolStripMenuItem";
            this.關閉ToolStripMenuItem.Size = new System.Drawing.Size(130, 30);
            this.關閉ToolStripMenuItem.Text = "關閉";
            this.關閉ToolStripMenuItem.Click += new System.EventHandler(this.關閉ToolStripMenuItem_Click);
            // 
            // 模式ToolStripMenuItem
            // 
            this.模式ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MIPC,
            this.MoveControl,
            this.人機畫面ToolStripMenuItem,
            this.牆壁設定ToolStripMenuItem});
            this.模式ToolStripMenuItem.Name = "模式ToolStripMenuItem";
            this.模式ToolStripMenuItem.Size = new System.Drawing.Size(64, 29);
            this.模式ToolStripMenuItem.Text = "模式";
            // 
            // MIPC
            // 
            this.MIPC.Name = "MIPC";
            this.MIPC.Size = new System.Drawing.Size(213, 30);
            this.MIPC.Text = "MIPC";
            this.MIPC.Click += new System.EventHandler(this.MIPC_Click);
            // 
            // MoveControl
            // 
            this.MoveControl.Name = "MoveControl";
            this.MoveControl.Size = new System.Drawing.Size(213, 30);
            this.MoveControl.Text = "MoveControl";
            this.MoveControl.Click += new System.EventHandler(this.MoveControl_Click);
            // 
            // 人機畫面ToolStripMenuItem
            // 
            this.人機畫面ToolStripMenuItem.Name = "人機畫面ToolStripMenuItem";
            this.人機畫面ToolStripMenuItem.Size = new System.Drawing.Size(213, 30);
            this.人機畫面ToolStripMenuItem.Text = "人機畫面";
            this.人機畫面ToolStripMenuItem.Click += new System.EventHandler(this.人機畫面ToolStripMenuItem_Click);
            // 
            // 牆壁設定ToolStripMenuItem
            // 
            this.牆壁設定ToolStripMenuItem.Name = "牆壁設定ToolStripMenuItem";
            this.牆壁設定ToolStripMenuItem.Size = new System.Drawing.Size(213, 30);
            this.牆壁設定ToolStripMenuItem.Text = "牆壁設定";
            this.牆壁設定ToolStripMenuItem.Click += new System.EventHandler(this.牆壁設定ToolStripMenuItem_Click);
            // 
            // panel_Map
            // 
            this.panel_Map.Controls.Add(this.pB_Map);
            this.panel_Map.Location = new System.Drawing.Point(4, 88);
            this.panel_Map.Margin = new System.Windows.Forms.Padding(4);
            this.panel_Map.Name = "panel_Map";
            this.panel_Map.Size = new System.Drawing.Size(940, 499);
            this.panel_Map.TabIndex = 2;
            // 
            // pB_Map
            // 
            this.pB_Map.Location = new System.Drawing.Point(0, 0);
            this.pB_Map.Margin = new System.Windows.Forms.Padding(4);
            this.pB_Map.Name = "pB_Map";
            this.pB_Map.Size = new System.Drawing.Size(297, 88);
            this.pB_Map.TabIndex = 0;
            this.pB_Map.TabStop = false;
            // 
            // timer
            // 
            this.timer.Enabled = true;
            this.timer.Interval = 200;
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // MovingAddresList
            // 
            this.MovingAddresList.Font = new System.Drawing.Font("新細明體", 12F);
            this.MovingAddresList.FormattingEnabled = true;
            this.MovingAddresList.ItemHeight = 20;
            this.MovingAddresList.Location = new System.Drawing.Point(948, 88);
            this.MovingAddresList.Margin = new System.Windows.Forms.Padding(4);
            this.MovingAddresList.Name = "MovingAddresList";
            this.MovingAddresList.ScrollAlwaysVisible = true;
            this.MovingAddresList.Size = new System.Drawing.Size(149, 384);
            this.MovingAddresList.TabIndex = 62;
            // 
            // button_SendToMoveControlDebug
            // 
            this.button_SendToMoveControlDebug.Font = new System.Drawing.Font("新細明體", 14F);
            this.button_SendToMoveControlDebug.Location = new System.Drawing.Point(948, 480);
            this.button_SendToMoveControlDebug.Margin = new System.Windows.Forms.Padding(4);
            this.button_SendToMoveControlDebug.Name = "button_SendToMoveControlDebug";
            this.button_SendToMoveControlDebug.Size = new System.Drawing.Size(149, 38);
            this.button_SendToMoveControlDebug.TabIndex = 63;
            this.button_SendToMoveControlDebug.Text = "SendList";
            this.button_SendToMoveControlDebug.UseVisualStyleBackColor = true;
            this.button_SendToMoveControlDebug.Click += new System.EventHandler(this.button_SendToMoveControlDebug_Click);
            // 
            // button_ClearAddressList
            // 
            this.button_ClearAddressList.Font = new System.Drawing.Font("新細明體", 14F);
            this.button_ClearAddressList.Location = new System.Drawing.Point(948, 525);
            this.button_ClearAddressList.Margin = new System.Windows.Forms.Padding(4);
            this.button_ClearAddressList.Name = "button_ClearAddressList";
            this.button_ClearAddressList.Size = new System.Drawing.Size(149, 38);
            this.button_ClearAddressList.TabIndex = 64;
            this.button_ClearAddressList.Text = "Clear";
            this.button_ClearAddressList.UseVisualStyleBackColor = true;
            this.button_ClearAddressList.Click += new System.EventHandler(this.button_ClearAddressList_Click);
            // 
            // button_Alarm
            // 
            this.button_Alarm.Font = new System.Drawing.Font("Times New Roman", 14F);
            this.button_Alarm.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.button_Alarm.Location = new System.Drawing.Point(757, 15);
            this.button_Alarm.Margin = new System.Windows.Forms.Padding(4);
            this.button_Alarm.Name = "button_Alarm";
            this.button_Alarm.Size = new System.Drawing.Size(167, 65);
            this.button_Alarm.TabIndex = 68;
            this.button_Alarm.Text = "Reset Alarm";
            this.button_Alarm.UseVisualStyleBackColor = true;
            this.button_Alarm.Click += new System.EventHandler(this.button_Alarm_Click);
            // 
            // button_BuzzOff
            // 
            this.button_BuzzOff.Font = new System.Drawing.Font("Times New Roman", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_BuzzOff.Location = new System.Drawing.Point(932, 15);
            this.button_BuzzOff.Margin = new System.Windows.Forms.Padding(4);
            this.button_BuzzOff.Name = "button_BuzzOff";
            this.button_BuzzOff.Size = new System.Drawing.Size(167, 65);
            this.button_BuzzOff.TabIndex = 69;
            this.button_BuzzOff.Text = "Buzz Off";
            this.button_BuzzOff.UseVisualStyleBackColor = true;
            this.button_BuzzOff.Click += new System.EventHandler(this.button_BuzzOff_Click);
            // 
            // button_AutoManual
            // 
            this.button_AutoManual.BackColor = System.Drawing.Color.Red;
            this.button_AutoManual.Font = new System.Drawing.Font("Times New Roman", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_AutoManual.Location = new System.Drawing.Point(1107, 15);
            this.button_AutoManual.Margin = new System.Windows.Forms.Padding(4);
            this.button_AutoManual.Name = "button_AutoManual";
            this.button_AutoManual.Size = new System.Drawing.Size(188, 90);
            this.button_AutoManual.TabIndex = 77;
            this.button_AutoManual.Text = "Manual";
            this.button_AutoManual.UseVisualStyleBackColor = false;
            this.button_AutoManual.Click += new System.EventHandler(this.btnAutoManual_Click);
            // 
            // label_Alarm
            // 
            this.label_Alarm.AutoSize = true;
            this.label_Alarm.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Alarm.Location = new System.Drawing.Point(593, 46);
            this.label_Alarm.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_Alarm.Name = "label_Alarm";
            this.label_Alarm.Size = new System.Drawing.Size(72, 27);
            this.label_Alarm.TabIndex = 80;
            this.label_Alarm.Text = "Alarm";
            // 
            // label_Warn
            // 
            this.label_Warn.AutoSize = true;
            this.label_Warn.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Warn.Location = new System.Drawing.Point(459, 46);
            this.label_Warn.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_Warn.Name = "label_Warn";
            this.label_Warn.Size = new System.Drawing.Size(64, 27);
            this.label_Warn.TabIndex = 82;
            this.label_Warn.Text = "Warn";
            // 
            // pB_Warn
            // 
            this.pB_Warn.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pB_Warn.Location = new System.Drawing.Point(527, 39);
            this.pB_Warn.Margin = new System.Windows.Forms.Padding(4);
            this.pB_Warn.Name = "pB_Warn";
            this.pB_Warn.Size = new System.Drawing.Size(65, 37);
            this.pB_Warn.TabIndex = 81;
            this.pB_Warn.TabStop = false;
            // 
            // pB_Alarm
            // 
            this.pB_Alarm.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pB_Alarm.Location = new System.Drawing.Point(671, 39);
            this.pB_Alarm.Margin = new System.Windows.Forms.Padding(4);
            this.pB_Alarm.Name = "pB_Alarm";
            this.pB_Alarm.Size = new System.Drawing.Size(65, 37);
            this.pB_Alarm.TabIndex = 79;
            this.pB_Alarm.TabStop = false;
            // 
            // button_SetPosition
            // 
            this.button_SetPosition.Font = new System.Drawing.Font("新細明體", 14F);
            this.button_SetPosition.Location = new System.Drawing.Point(948, 612);
            this.button_SetPosition.Margin = new System.Windows.Forms.Padding(4);
            this.button_SetPosition.Name = "button_SetPosition";
            this.button_SetPosition.Size = new System.Drawing.Size(149, 38);
            this.button_SetPosition.TabIndex = 83;
            this.button_SetPosition.Text = "SetPosition";
            this.button_SetPosition.UseVisualStyleBackColor = true;
            this.button_SetPosition.Click += new System.EventHandler(this.button_SetPosition_Click);
            // 
            // label_LocateStatus
            // 
            this.label_LocateStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label_LocateStatus.Font = new System.Drawing.Font("新細明體", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_LocateStatus.ForeColor = System.Drawing.Color.Red;
            this.label_LocateStatus.Location = new System.Drawing.Point(949, 571);
            this.label_LocateStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_LocateStatus.Name = "label_LocateStatus";
            this.label_LocateStatus.Size = new System.Drawing.Size(147, 32);
            this.label_LocateStatus.TabIndex = 84;
            this.label_LocateStatus.Text = "定位NG";
            this.label_LocateStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pB_reservestatus
            // 
            this.pB_reservestatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pB_reservestatus.Location = new System.Drawing.Point(1535, 280);
            this.pB_reservestatus.Margin = new System.Windows.Forms.Padding(5);
            this.pB_reservestatus.Name = "pB_reservestatus";
            this.pB_reservestatus.Size = new System.Drawing.Size(86, 46);
            this.pB_reservestatus.TabIndex = 85;
            this.pB_reservestatus.TabStop = false;
            // 
            // label_TestString
            // 
            this.label_TestString.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label_TestString.Font = new System.Drawing.Font("新細明體", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_TestString.ForeColor = System.Drawing.Color.Red;
            this.label_TestString.Location = new System.Drawing.Point(777, 612);
            this.label_TestString.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_TestString.Name = "label_TestString";
            this.label_TestString.Size = new System.Drawing.Size(147, 32);
            this.label_TestString.TabIndex = 86;
            this.label_TestString.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1300, 702);
            this.ControlBox = false;
            this.Controls.Add(this.label_TestString);
            this.Controls.Add(this.pB_reservestatus);
            this.Controls.Add(this.label_Warn);
            this.Controls.Add(this.pB_Warn);
            this.Controls.Add(this.label_Alarm);
            this.Controls.Add(this.pB_Alarm);
            this.Controls.Add(this.button_AutoManual);
            this.Controls.Add(this.panel_Map);
            this.Controls.Add(this.MovingAddresList);
            this.Controls.Add(this.button_SendToMoveControlDebug);
            this.Controls.Add(this.button_ClearAddressList);
            this.Controls.Add(this.button_BuzzOff);
            this.Controls.Add(this.button_Alarm);
            this.Controls.Add(this.menuStrip_MainForm);
            this.Controls.Add(this.label_LocateStatus);
            this.Controls.Add(this.button_SetPosition);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MainForm";
            this.Text = "MainForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.menuStrip_MainForm.ResumeLayout(false);
            this.menuStrip_MainForm.PerformLayout();
            this.panel_Map.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pB_Map)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pB_Warn)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pB_Alarm)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pB_reservestatus)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip_MainForm;
        private System.Windows.Forms.ToolStripMenuItem 系統ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 關閉ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 模式ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem MoveControl;
        private System.Windows.Forms.Panel panel_Map;
        private System.Windows.Forms.PictureBox pB_Map;
        private System.Windows.Forms.Timer timer;
        private System.Windows.Forms.ListBox MovingAddresList;
        private System.Windows.Forms.Button button_SendToMoveControlDebug;
        private System.Windows.Forms.Button button_ClearAddressList;
        private System.Windows.Forms.Button button_Alarm;
        private System.Windows.Forms.Button button_BuzzOff;
        private System.Windows.Forms.Button button_AutoManual;
        private System.Windows.Forms.PictureBox pB_Alarm;
        private System.Windows.Forms.Label label_Alarm;
        private System.Windows.Forms.Label label_Warn;
        private System.Windows.Forms.PictureBox pB_Warn;
        private System.Windows.Forms.ToolStripMenuItem MIPC;
        private System.Windows.Forms.Button button_SetPosition;
        private System.Windows.Forms.Label label_LocateStatus;
        private System.Windows.Forms.ToolStripMenuItem 人機畫面ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 牆壁設定ToolStripMenuItem;
        private System.Windows.Forms.PictureBox pB_reservestatus;
        private System.Windows.Forms.Label label_TestString;
    }
}