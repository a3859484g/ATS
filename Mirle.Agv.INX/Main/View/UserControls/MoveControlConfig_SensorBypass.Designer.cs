namespace Mirle.Agv.INX.View
{
    partial class MoveControlConfig_SensorBypass
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
            this.button_ChangeButton = new System.Windows.Forms.Button();
            this.label_Name = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // button_ChangeButton
            // 
            this.button_ChangeButton.Font = new System.Drawing.Font("標楷體", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.button_ChangeButton.Location = new System.Drawing.Point(201, 0);
            this.button_ChangeButton.Name = "button_ChangeButton";
            this.button_ChangeButton.Size = new System.Drawing.Size(100, 40);
            this.button_ChangeButton.TabIndex = 3;
            this.button_ChangeButton.Text = "開啟中";
            this.button_ChangeButton.UseVisualStyleBackColor = true;
            this.button_ChangeButton.Click += new System.EventHandler(this.button_ChangeButton_Click);
            // 
            // label_Name
            // 
            this.label_Name.Font = new System.Drawing.Font("標楷體", 14F);
            this.label_Name.Location = new System.Drawing.Point(0, 0);
            this.label_Name.Name = "label_Name";
            this.label_Name.Size = new System.Drawing.Size(200, 40);
            this.label_Name.TabIndex = 2;
            this.label_Name.Text = "Title";
            this.label_Name.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label_Name.Click += new System.EventHandler(this.object_Click);
            // 
            // MoveControlConfig_SensorBypass
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.button_ChangeButton);
            this.Controls.Add(this.label_Name);
            this.Name = "MoveControlConfig_SensorBypass";
            this.Size = new System.Drawing.Size(301, 40);
            this.Click += new System.EventHandler(this.object_Click);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button_ChangeButton;
        private System.Windows.Forms.Label label_Name;
    }
}
