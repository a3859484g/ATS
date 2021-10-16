namespace Mirle.Agv.INX.View
{
    partial class MIPCMotionCommandForm
    {
        /// <summary> 
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置受控資源則為 true，否則為 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 元件設計工具產生的程式碼

        /// <summary> 
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器修改
        /// 這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
            this.gB_Command = new System.Windows.Forms.GroupBox();
            this.button_CommandStart = new System.Windows.Forms.Button();
            this.gB_Theta = new System.Windows.Forms.GroupBox();
            this.tB_ThetaJerk = new System.Windows.Forms.TextBox();
            this.label_ThetaJerk = new System.Windows.Forms.Label();
            this.tB_ThetaDec = new System.Windows.Forms.TextBox();
            this.label_ThetaDec = new System.Windows.Forms.Label();
            this.tB_ThetaAcc = new System.Windows.Forms.TextBox();
            this.label_ThetaAcc = new System.Windows.Forms.Label();
            this.tB_ThetaVelocity = new System.Windows.Forms.TextBox();
            this.label_ThetaVelocity = new System.Windows.Forms.Label();
            this.gB_Line = new System.Windows.Forms.GroupBox();
            this.tB_LineJerk = new System.Windows.Forms.TextBox();
            this.label_LineJerk = new System.Windows.Forms.Label();
            this.tB_LineDec = new System.Windows.Forms.TextBox();
            this.label_LineDec = new System.Windows.Forms.Label();
            this.tB_LineAcc = new System.Windows.Forms.TextBox();
            this.label_LineAcc = new System.Windows.Forms.Label();
            this.tB_LineVelocity = new System.Windows.Forms.TextBox();
            this.label_LineVelocity = new System.Windows.Forms.Label();
            this.tB_Angle = new System.Windows.Forms.TextBox();
            this.label_Angle = new System.Windows.Forms.Label();
            this.tB_Y = new System.Windows.Forms.TextBox();
            this.label_Y = new System.Windows.Forms.Label();
            this.tB_X = new System.Windows.Forms.TextBox();
            this.label_X = new System.Windows.Forms.Label();
            this.gB_FeedbackAGVPosition = new System.Windows.Forms.GroupBox();
            this.label_SLAMLocateValue = new System.Windows.Forms.Label();
            this.label_SLAMLocate = new System.Windows.Forms.Label();
            this.label_MoveStatusValue = new System.Windows.Forms.Label();
            this.label_MoveStatus = new System.Windows.Forms.Label();
            this.label_FeedbackNowValue = new System.Windows.Forms.Label();
            this.label_NowFeedback = new System.Windows.Forms.Label();
            this.gB_CycleRunTest = new System.Windows.Forms.GroupBox();
            this.button_WriteConfig = new System.Windows.Forms.Button();
            this.button_SetConfig = new System.Windows.Forms.Button();
            this.button_ServoOff = new System.Windows.Forms.Button();
            this.button_ServoOn = new System.Windows.Forms.Button();
            this.button_Stop = new System.Windows.Forms.Button();
            this.gB_Command.SuspendLayout();
            this.gB_Theta.SuspendLayout();
            this.gB_Line.SuspendLayout();
            this.gB_FeedbackAGVPosition.SuspendLayout();
            this.gB_CycleRunTest.SuspendLayout();
            this.SuspendLayout();
            // 
            // gB_Command
            // 
            this.gB_Command.Controls.Add(this.button_CommandStart);
            this.gB_Command.Controls.Add(this.gB_Theta);
            this.gB_Command.Controls.Add(this.gB_Line);
            this.gB_Command.Controls.Add(this.tB_Angle);
            this.gB_Command.Controls.Add(this.label_Angle);
            this.gB_Command.Controls.Add(this.tB_Y);
            this.gB_Command.Controls.Add(this.label_Y);
            this.gB_Command.Controls.Add(this.tB_X);
            this.gB_Command.Controls.Add(this.label_X);
            this.gB_Command.Font = new System.Drawing.Font("新細明體", 14F);
            this.gB_Command.Location = new System.Drawing.Point(1, 6);
            this.gB_Command.Name = "gB_Command";
            this.gB_Command.Size = new System.Drawing.Size(775, 198);
            this.gB_Command.TabIndex = 0;
            this.gB_Command.TabStop = false;
            this.gB_Command.Text = "Command";
            // 
            // button_CommandStart
            // 
            this.button_CommandStart.Location = new System.Drawing.Point(670, 22);
            this.button_CommandStart.Name = "button_CommandStart";
            this.button_CommandStart.Size = new System.Drawing.Size(99, 33);
            this.button_CommandStart.TabIndex = 17;
            this.button_CommandStart.Text = "執行";
            this.button_CommandStart.UseVisualStyleBackColor = true;
            this.button_CommandStart.Click += new System.EventHandler(this.button_CommandStart_Click);
            // 
            // gB_Theta
            // 
            this.gB_Theta.Controls.Add(this.tB_ThetaJerk);
            this.gB_Theta.Controls.Add(this.label_ThetaJerk);
            this.gB_Theta.Controls.Add(this.tB_ThetaDec);
            this.gB_Theta.Controls.Add(this.label_ThetaDec);
            this.gB_Theta.Controls.Add(this.tB_ThetaAcc);
            this.gB_Theta.Controls.Add(this.label_ThetaAcc);
            this.gB_Theta.Controls.Add(this.tB_ThetaVelocity);
            this.gB_Theta.Controls.Add(this.label_ThetaVelocity);
            this.gB_Theta.Location = new System.Drawing.Point(6, 123);
            this.gB_Theta.Name = "gB_Theta";
            this.gB_Theta.Size = new System.Drawing.Size(763, 68);
            this.gB_Theta.TabIndex = 16;
            this.gB_Theta.TabStop = false;
            this.gB_Theta.Text = "角";
            // 
            // tB_ThetaJerk
            // 
            this.tB_ThetaJerk.Location = new System.Drawing.Point(642, 23);
            this.tB_ThetaJerk.Name = "tB_ThetaJerk";
            this.tB_ThetaJerk.Size = new System.Drawing.Size(94, 30);
            this.tB_ThetaJerk.TabIndex = 15;
            this.tB_ThetaJerk.Text = "10";
            this.tB_ThetaJerk.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label_ThetaJerk
            // 
            this.label_ThetaJerk.AutoSize = true;
            this.label_ThetaJerk.Location = new System.Drawing.Point(578, 26);
            this.label_ThetaJerk.Name = "label_ThetaJerk";
            this.label_ThetaJerk.Size = new System.Drawing.Size(54, 19);
            this.label_ThetaJerk.TabIndex = 14;
            this.label_ThetaJerk.Text = "Jerk : ";
            // 
            // tB_ThetaDec
            // 
            this.tB_ThetaDec.Location = new System.Drawing.Point(448, 23);
            this.tB_ThetaDec.Name = "tB_ThetaDec";
            this.tB_ThetaDec.Size = new System.Drawing.Size(94, 30);
            this.tB_ThetaDec.TabIndex = 13;
            this.tB_ThetaDec.Text = "5";
            this.tB_ThetaDec.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label_ThetaDec
            // 
            this.label_ThetaDec.AutoSize = true;
            this.label_ThetaDec.Location = new System.Drawing.Point(384, 26);
            this.label_ThetaDec.Name = "label_ThetaDec";
            this.label_ThetaDec.Size = new System.Drawing.Size(53, 19);
            this.label_ThetaDec.TabIndex = 12;
            this.label_ThetaDec.Text = "Dec : ";
            // 
            // tB_ThetaAcc
            // 
            this.tB_ThetaAcc.Location = new System.Drawing.Point(263, 23);
            this.tB_ThetaAcc.Name = "tB_ThetaAcc";
            this.tB_ThetaAcc.Size = new System.Drawing.Size(94, 30);
            this.tB_ThetaAcc.TabIndex = 11;
            this.tB_ThetaAcc.Text = "5";
            this.tB_ThetaAcc.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label_ThetaAcc
            // 
            this.label_ThetaAcc.AutoSize = true;
            this.label_ThetaAcc.Location = new System.Drawing.Point(199, 26);
            this.label_ThetaAcc.Name = "label_ThetaAcc";
            this.label_ThetaAcc.Size = new System.Drawing.Size(53, 19);
            this.label_ThetaAcc.TabIndex = 10;
            this.label_ThetaAcc.Text = "Acc : ";
            // 
            // tB_ThetaVelocity
            // 
            this.tB_ThetaVelocity.Location = new System.Drawing.Point(80, 23);
            this.tB_ThetaVelocity.Name = "tB_ThetaVelocity";
            this.tB_ThetaVelocity.Size = new System.Drawing.Size(94, 30);
            this.tB_ThetaVelocity.TabIndex = 9;
            this.tB_ThetaVelocity.Text = "5";
            this.tB_ThetaVelocity.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label_ThetaVelocity
            // 
            this.label_ThetaVelocity.AutoSize = true;
            this.label_ThetaVelocity.Location = new System.Drawing.Point(16, 26);
            this.label_ThetaVelocity.Name = "label_ThetaVelocity";
            this.label_ThetaVelocity.Size = new System.Drawing.Size(50, 19);
            this.label_ThetaVelocity.TabIndex = 8;
            this.label_ThetaVelocity.Text = "Vel : ";
            // 
            // gB_Line
            // 
            this.gB_Line.Controls.Add(this.tB_LineJerk);
            this.gB_Line.Controls.Add(this.label_LineJerk);
            this.gB_Line.Controls.Add(this.tB_LineDec);
            this.gB_Line.Controls.Add(this.label_LineDec);
            this.gB_Line.Controls.Add(this.tB_LineAcc);
            this.gB_Line.Controls.Add(this.label_LineAcc);
            this.gB_Line.Controls.Add(this.tB_LineVelocity);
            this.gB_Line.Controls.Add(this.label_LineVelocity);
            this.gB_Line.Location = new System.Drawing.Point(6, 54);
            this.gB_Line.Name = "gB_Line";
            this.gB_Line.Size = new System.Drawing.Size(763, 69);
            this.gB_Line.TabIndex = 7;
            this.gB_Line.TabStop = false;
            this.gB_Line.Text = "線";
            // 
            // tB_LineJerk
            // 
            this.tB_LineJerk.Location = new System.Drawing.Point(642, 25);
            this.tB_LineJerk.Name = "tB_LineJerk";
            this.tB_LineJerk.Size = new System.Drawing.Size(94, 30);
            this.tB_LineJerk.TabIndex = 15;
            this.tB_LineJerk.Text = "200";
            this.tB_LineJerk.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label_LineJerk
            // 
            this.label_LineJerk.AutoSize = true;
            this.label_LineJerk.Location = new System.Drawing.Point(578, 28);
            this.label_LineJerk.Name = "label_LineJerk";
            this.label_LineJerk.Size = new System.Drawing.Size(49, 19);
            this.label_LineJerk.TabIndex = 14;
            this.label_LineJerk.Text = "Jerk :";
            // 
            // tB_LineDec
            // 
            this.tB_LineDec.Location = new System.Drawing.Point(448, 25);
            this.tB_LineDec.Name = "tB_LineDec";
            this.tB_LineDec.Size = new System.Drawing.Size(94, 30);
            this.tB_LineDec.TabIndex = 13;
            this.tB_LineDec.Text = "100";
            this.tB_LineDec.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label_LineDec
            // 
            this.label_LineDec.AutoSize = true;
            this.label_LineDec.Location = new System.Drawing.Point(384, 28);
            this.label_LineDec.Name = "label_LineDec";
            this.label_LineDec.Size = new System.Drawing.Size(48, 19);
            this.label_LineDec.TabIndex = 12;
            this.label_LineDec.Text = "Dec :";
            // 
            // tB_LineAcc
            // 
            this.tB_LineAcc.Location = new System.Drawing.Point(263, 25);
            this.tB_LineAcc.Name = "tB_LineAcc";
            this.tB_LineAcc.Size = new System.Drawing.Size(94, 30);
            this.tB_LineAcc.TabIndex = 11;
            this.tB_LineAcc.Text = "100";
            this.tB_LineAcc.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label_LineAcc
            // 
            this.label_LineAcc.AutoSize = true;
            this.label_LineAcc.Location = new System.Drawing.Point(199, 28);
            this.label_LineAcc.Name = "label_LineAcc";
            this.label_LineAcc.Size = new System.Drawing.Size(48, 19);
            this.label_LineAcc.TabIndex = 10;
            this.label_LineAcc.Text = "Acc :";
            // 
            // tB_LineVelocity
            // 
            this.tB_LineVelocity.Location = new System.Drawing.Point(80, 25);
            this.tB_LineVelocity.Name = "tB_LineVelocity";
            this.tB_LineVelocity.Size = new System.Drawing.Size(94, 30);
            this.tB_LineVelocity.TabIndex = 9;
            this.tB_LineVelocity.Text = "100";
            this.tB_LineVelocity.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label_LineVelocity
            // 
            this.label_LineVelocity.AutoSize = true;
            this.label_LineVelocity.Location = new System.Drawing.Point(16, 28);
            this.label_LineVelocity.Name = "label_LineVelocity";
            this.label_LineVelocity.Size = new System.Drawing.Size(50, 19);
            this.label_LineVelocity.TabIndex = 8;
            this.label_LineVelocity.Text = "Vel : ";
            // 
            // tB_Angle
            // 
            this.tB_Angle.Location = new System.Drawing.Point(557, 22);
            this.tB_Angle.Name = "tB_Angle";
            this.tB_Angle.Size = new System.Drawing.Size(87, 30);
            this.tB_Angle.TabIndex = 6;
            this.tB_Angle.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label_Angle
            // 
            this.label_Angle.AutoSize = true;
            this.label_Angle.Location = new System.Drawing.Point(464, 25);
            this.label_Angle.Name = "label_Angle";
            this.label_Angle.Size = new System.Drawing.Size(68, 19);
            this.label_Angle.TabIndex = 5;
            this.label_Angle.Text = "Angle : ";
            // 
            // tB_Y
            // 
            this.tB_Y.Location = new System.Drawing.Point(301, 22);
            this.tB_Y.Name = "tB_Y";
            this.tB_Y.Size = new System.Drawing.Size(136, 30);
            this.tB_Y.TabIndex = 4;
            this.tB_Y.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label_Y
            // 
            this.label_Y.AutoSize = true;
            this.label_Y.Location = new System.Drawing.Point(237, 25);
            this.label_Y.Name = "label_Y";
            this.label_Y.Size = new System.Drawing.Size(37, 19);
            this.label_Y.TabIndex = 3;
            this.label_Y.Text = "Y : ";
            // 
            // tB_X
            // 
            this.tB_X.Location = new System.Drawing.Point(82, 22);
            this.tB_X.Name = "tB_X";
            this.tB_X.Size = new System.Drawing.Size(136, 30);
            this.tB_X.TabIndex = 2;
            this.tB_X.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label_X
            // 
            this.label_X.AutoSize = true;
            this.label_X.Location = new System.Drawing.Point(18, 25);
            this.label_X.Name = "label_X";
            this.label_X.Size = new System.Drawing.Size(37, 19);
            this.label_X.TabIndex = 0;
            this.label_X.Text = "X : ";
            // 
            // gB_FeedbackAGVPosition
            // 
            this.gB_FeedbackAGVPosition.Controls.Add(this.label_SLAMLocateValue);
            this.gB_FeedbackAGVPosition.Controls.Add(this.label_SLAMLocate);
            this.gB_FeedbackAGVPosition.Controls.Add(this.label_MoveStatusValue);
            this.gB_FeedbackAGVPosition.Controls.Add(this.label_MoveStatus);
            this.gB_FeedbackAGVPosition.Controls.Add(this.label_FeedbackNowValue);
            this.gB_FeedbackAGVPosition.Controls.Add(this.label_NowFeedback);
            this.gB_FeedbackAGVPosition.Font = new System.Drawing.Font("新細明體", 14F);
            this.gB_FeedbackAGVPosition.Location = new System.Drawing.Point(0, 203);
            this.gB_FeedbackAGVPosition.Name = "gB_FeedbackAGVPosition";
            this.gB_FeedbackAGVPosition.Size = new System.Drawing.Size(775, 77);
            this.gB_FeedbackAGVPosition.TabIndex = 1;
            this.gB_FeedbackAGVPosition.TabStop = false;
            this.gB_FeedbackAGVPosition.Text = "Feedback";
            // 
            // label_SLAMLocateValue
            // 
            this.label_SLAMLocateValue.AutoSize = true;
            this.label_SLAMLocateValue.Location = new System.Drawing.Point(346, 32);
            this.label_SLAMLocateValue.Name = "label_SLAMLocateValue";
            this.label_SLAMLocateValue.Size = new System.Drawing.Size(79, 19);
            this.label_SLAMLocateValue.TabIndex = 23;
            this.label_SLAMLocateValue.Text = "(---,---), 0";
            // 
            // label_SLAMLocate
            // 
            this.label_SLAMLocate.AutoSize = true;
            this.label_SLAMLocate.Location = new System.Drawing.Point(275, 32);
            this.label_SLAMLocate.Name = "label_SLAMLocate";
            this.label_SLAMLocate.Size = new System.Drawing.Size(74, 19);
            this.label_SLAMLocate.TabIndex = 22;
            this.label_SLAMLocate.Text = "SLAM : ";
            // 
            // label_MoveStatusValue
            // 
            this.label_MoveStatusValue.AutoSize = true;
            this.label_MoveStatusValue.Location = new System.Drawing.Point(712, 32);
            this.label_MoveStatusValue.Name = "label_MoveStatusValue";
            this.label_MoveStatusValue.Size = new System.Drawing.Size(42, 19);
            this.label_MoveStatusValue.TabIndex = 21;
            this.label_MoveStatusValue.Text = "Stop";
            // 
            // label_MoveStatus
            // 
            this.label_MoveStatus.AutoSize = true;
            this.label_MoveStatus.Location = new System.Drawing.Point(584, 32);
            this.label_MoveStatus.Name = "label_MoveStatus";
            this.label_MoveStatus.Size = new System.Drawing.Size(110, 19);
            this.label_MoveStatus.TabIndex = 20;
            this.label_MoveStatus.Text = "MoveStatus : ";
            // 
            // label_FeedbackNowValue
            // 
            this.label_FeedbackNowValue.AutoSize = true;
            this.label_FeedbackNowValue.Location = new System.Drawing.Point(78, 32);
            this.label_FeedbackNowValue.Name = "label_FeedbackNowValue";
            this.label_FeedbackNowValue.Size = new System.Drawing.Size(79, 19);
            this.label_FeedbackNowValue.TabIndex = 19;
            this.label_FeedbackNowValue.Text = "(---,---), 0";
            // 
            // label_NowFeedback
            // 
            this.label_NowFeedback.AutoSize = true;
            this.label_NowFeedback.Location = new System.Drawing.Point(7, 32);
            this.label_NowFeedback.Name = "label_NowFeedback";
            this.label_NowFeedback.Size = new System.Drawing.Size(59, 19);
            this.label_NowFeedback.TabIndex = 18;
            this.label_NowFeedback.Text = "Now : ";
            // 
            // gB_CycleRunTest
            // 
            this.gB_CycleRunTest.Controls.Add(this.button_WriteConfig);
            this.gB_CycleRunTest.Controls.Add(this.button_SetConfig);
            this.gB_CycleRunTest.Controls.Add(this.button_ServoOff);
            this.gB_CycleRunTest.Controls.Add(this.button_ServoOn);
            this.gB_CycleRunTest.Controls.Add(this.button_Stop);
            this.gB_CycleRunTest.Font = new System.Drawing.Font("新細明體", 16F);
            this.gB_CycleRunTest.Location = new System.Drawing.Point(3, 286);
            this.gB_CycleRunTest.Name = "gB_CycleRunTest";
            this.gB_CycleRunTest.Size = new System.Drawing.Size(775, 237);
            this.gB_CycleRunTest.TabIndex = 2;
            this.gB_CycleRunTest.TabStop = false;
            this.gB_CycleRunTest.Text = "CycleRunTest";
            // 
            // button_WriteConfig
            // 
            this.button_WriteConfig.Location = new System.Drawing.Point(315, 75);
            this.button_WriteConfig.Name = "button_WriteConfig";
            this.button_WriteConfig.Size = new System.Drawing.Size(151, 61);
            this.button_WriteConfig.TabIndex = 36;
            this.button_WriteConfig.Text = "匯出";
            this.button_WriteConfig.UseVisualStyleBackColor = true;
            this.button_WriteConfig.Click += new System.EventHandler(this.button_WriteConfig_Click);
            // 
            // button_SetConfig
            // 
            this.button_SetConfig.Location = new System.Drawing.Point(53, 75);
            this.button_SetConfig.Name = "button_SetConfig";
            this.button_SetConfig.Size = new System.Drawing.Size(151, 61);
            this.button_SetConfig.TabIndex = 35;
            this.button_SetConfig.Text = "匯入";
            this.button_SetConfig.UseVisualStyleBackColor = true;
            this.button_SetConfig.Click += new System.EventHandler(this.button_SetConfig_Click);
            // 
            // button_ServoOff
            // 
            this.button_ServoOff.Font = new System.Drawing.Font("新細明體", 12F);
            this.button_ServoOff.Location = new System.Drawing.Point(599, 99);
            this.button_ServoOff.Name = "button_ServoOff";
            this.button_ServoOff.Size = new System.Drawing.Size(152, 52);
            this.button_ServoOff.TabIndex = 34;
            this.button_ServoOff.Text = "ServoOff";
            this.button_ServoOff.UseVisualStyleBackColor = true;
            this.button_ServoOff.Click += new System.EventHandler(this.button_ServoOff_Click);
            // 
            // button_ServoOn
            // 
            this.button_ServoOn.Font = new System.Drawing.Font("新細明體", 12F);
            this.button_ServoOn.Location = new System.Drawing.Point(599, 25);
            this.button_ServoOn.Name = "button_ServoOn";
            this.button_ServoOn.Size = new System.Drawing.Size(152, 52);
            this.button_ServoOn.TabIndex = 33;
            this.button_ServoOn.Text = "ServoOn";
            this.button_ServoOn.UseVisualStyleBackColor = true;
            this.button_ServoOn.Click += new System.EventHandler(this.button_ServoOn_Click);
            // 
            // button_Stop
            // 
            this.button_Stop.Location = new System.Drawing.Point(599, 171);
            this.button_Stop.Name = "button_Stop";
            this.button_Stop.Size = new System.Drawing.Size(152, 52);
            this.button_Stop.TabIndex = 28;
            this.button_Stop.Text = "Stop";
            this.button_Stop.UseVisualStyleBackColor = true;
            this.button_Stop.Click += new System.EventHandler(this.button_Stop_Click);
            // 
            // MIPCMotionCommandForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.gB_CycleRunTest);
            this.Controls.Add(this.gB_FeedbackAGVPosition);
            this.Controls.Add(this.gB_Command);
            this.Name = "MIPCMotionCommandForm";
            this.Size = new System.Drawing.Size(780, 550);
            this.gB_Command.ResumeLayout(false);
            this.gB_Command.PerformLayout();
            this.gB_Theta.ResumeLayout(false);
            this.gB_Theta.PerformLayout();
            this.gB_Line.ResumeLayout(false);
            this.gB_Line.PerformLayout();
            this.gB_FeedbackAGVPosition.ResumeLayout(false);
            this.gB_FeedbackAGVPosition.PerformLayout();
            this.gB_CycleRunTest.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox gB_Command;
        private System.Windows.Forms.GroupBox gB_Line;
        private System.Windows.Forms.TextBox tB_Angle;
        private System.Windows.Forms.Label label_Angle;
        private System.Windows.Forms.TextBox tB_Y;
        private System.Windows.Forms.Label label_Y;
        private System.Windows.Forms.TextBox tB_X;
        private System.Windows.Forms.Label label_X;
        private System.Windows.Forms.Button button_CommandStart;
        private System.Windows.Forms.GroupBox gB_Theta;
        private System.Windows.Forms.TextBox tB_ThetaJerk;
        private System.Windows.Forms.Label label_ThetaJerk;
        private System.Windows.Forms.TextBox tB_ThetaDec;
        private System.Windows.Forms.Label label_ThetaDec;
        private System.Windows.Forms.TextBox tB_ThetaAcc;
        private System.Windows.Forms.Label label_ThetaAcc;
        private System.Windows.Forms.TextBox tB_ThetaVelocity;
        private System.Windows.Forms.Label label_ThetaVelocity;
        private System.Windows.Forms.TextBox tB_LineJerk;
        private System.Windows.Forms.Label label_LineJerk;
        private System.Windows.Forms.TextBox tB_LineDec;
        private System.Windows.Forms.Label label_LineDec;
        private System.Windows.Forms.TextBox tB_LineAcc;
        private System.Windows.Forms.Label label_LineAcc;
        private System.Windows.Forms.TextBox tB_LineVelocity;
        private System.Windows.Forms.Label label_LineVelocity;
        private System.Windows.Forms.GroupBox gB_FeedbackAGVPosition;
        private System.Windows.Forms.Label label_FeedbackNowValue;
        private System.Windows.Forms.Label label_NowFeedback;
        private System.Windows.Forms.GroupBox gB_CycleRunTest;
        private System.Windows.Forms.Button button_Stop;
        private System.Windows.Forms.Label label_MoveStatusValue;
        private System.Windows.Forms.Label label_MoveStatus;
        private System.Windows.Forms.Label label_SLAMLocateValue;
        private System.Windows.Forms.Label label_SLAMLocate;
        private System.Windows.Forms.Button button_ServoOff;
        private System.Windows.Forms.Button button_ServoOn;
        private System.Windows.Forms.Button button_WriteConfig;
        private System.Windows.Forms.Button button_SetConfig;
    }
}
