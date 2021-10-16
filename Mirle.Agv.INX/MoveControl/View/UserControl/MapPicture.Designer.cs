namespace Mirle.Agv.INX.View
{
    partial class MapPicture
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
            this.pB_Picture = new System.Windows.Forms.PictureBox();
            this.panel1 = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.pB_Picture)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pB_Picture
            // 
            this.pB_Picture.Location = new System.Drawing.Point(0, -27);
            this.pB_Picture.Name = "pB_Picture";
            this.pB_Picture.Size = new System.Drawing.Size(500, 400);
            this.pB_Picture.TabIndex = 0;
            this.pB_Picture.TabStop = false;
            this.pB_Picture.MouseDown += new System.Windows.Forms.MouseEventHandler(this.LocateCheck_MouseDown);
            this.pB_Picture.MouseUp += new System.Windows.Forms.MouseEventHandler(this.LocateCheck_MouseUp);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.pB_Picture);
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(700, 500);
            this.panel1.TabIndex = 2;
            // 
            // MapPicture
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel1);
            this.Name = "MapPicture";
            this.Size = new System.Drawing.Size(700, 500);
            ((System.ComponentModel.ISupportInitialize)(this.pB_Picture)).EndInit();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pB_Picture;
        private System.Windows.Forms.Panel panel1;
    }
}
