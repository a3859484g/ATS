namespace Mirle.Agv.INX.View
{
    partial class SafetySensorByPass
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
            this.label_DeviceName = new System.Windows.Forms.Label();
            this.button_AlarmByPass = new System.Windows.Forms.Button();
            this.button_SafetyByPass = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label_DeviceName
            // 
            this.label_DeviceName.Font = new System.Drawing.Font("標楷體", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_DeviceName.Location = new System.Drawing.Point(9, 4);
            this.label_DeviceName.Name = "label_DeviceName";
            this.label_DeviceName.Size = new System.Drawing.Size(211, 30);
            this.label_DeviceName.TabIndex = 5;
            this.label_DeviceName.Text = "Name";
            this.label_DeviceName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // button_AlarmByPass
            // 
            this.button_AlarmByPass.Font = new System.Drawing.Font("標楷體", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.button_AlarmByPass.Location = new System.Drawing.Point(252, 3);
            this.button_AlarmByPass.Name = "button_AlarmByPass";
            this.button_AlarmByPass.Size = new System.Drawing.Size(154, 33);
            this.button_AlarmByPass.TabIndex = 9;
            this.button_AlarmByPass.Text = "Normal";
            this.button_AlarmByPass.UseVisualStyleBackColor = true;
            this.button_AlarmByPass.Click += new System.EventHandler(this.button_AlarmByPass_Click);
            // 
            // button_SafetyByPass
            // 
            this.button_SafetyByPass.Font = new System.Drawing.Font("標楷體", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.button_SafetyByPass.Location = new System.Drawing.Point(450, 3);
            this.button_SafetyByPass.Name = "button_SafetyByPass";
            this.button_SafetyByPass.Size = new System.Drawing.Size(154, 33);
            this.button_SafetyByPass.TabIndex = 10;
            this.button_SafetyByPass.Text = "Normal";
            this.button_SafetyByPass.UseVisualStyleBackColor = true;
            this.button_SafetyByPass.Click += new System.EventHandler(this.button_SafetyByPass_Click);
            // 
            // SafetySensorByPass
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.button_SafetyByPass);
            this.Controls.Add(this.button_AlarmByPass);
            this.Controls.Add(this.label_DeviceName);
            this.Name = "SafetySensorByPass";
            this.Size = new System.Drawing.Size(620, 40);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Label label_DeviceName;
        private System.Windows.Forms.Button button_AlarmByPass;
        private System.Windows.Forms.Button button_SafetyByPass;
    }
}
