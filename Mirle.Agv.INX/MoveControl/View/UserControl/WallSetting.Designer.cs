namespace Mirle.Agv.INX.View
{
    partial class WallSetting
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
            this.button_ShowAll = new System.Windows.Forms.Button();
            this.button_HideAll = new System.Windows.Forms.Button();
            this.cB_WallList = new System.Windows.Forms.ComboBox();
            this.button_OnlyShowThisWall = new System.Windows.Forms.Button();
            this.button_ShowThisWallData = new System.Windows.Forms.Button();
            this.label_ID = new System.Windows.Forms.Label();
            this.button_DeleteThisWall = new System.Windows.Forms.Button();
            this.button_AddNewWall = new System.Windows.Forms.Button();
            this.label_StartPosition = new System.Windows.Forms.Label();
            this.tB_ID = new System.Windows.Forms.TextBox();
            this.tB_StartX = new System.Windows.Forms.TextBox();
            this.tB_StartY = new System.Windows.Forms.TextBox();
            this.tB_EndY = new System.Windows.Forms.TextBox();
            this.tB_EndX = new System.Windows.Forms.TextBox();
            this.label_EndPosition = new System.Windows.Forms.Label();
            this.tB_Distance = new System.Windows.Forms.TextBox();
            this.label_Distance = new System.Windows.Forms.Label();
            this.button_Send = new System.Windows.Forms.Button();
            this.button_WriteCSV = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button_ShowAll
            // 
            this.button_ShowAll.Font = new System.Drawing.Font("標楷體", 12F);
            this.button_ShowAll.Location = new System.Drawing.Point(720, 9);
            this.button_ShowAll.Name = "button_ShowAll";
            this.button_ShowAll.Size = new System.Drawing.Size(92, 34);
            this.button_ShowAll.TabIndex = 0;
            this.button_ShowAll.Text = "全部顯示";
            this.button_ShowAll.UseVisualStyleBackColor = true;
            this.button_ShowAll.Click += new System.EventHandler(this.button_ShowAll_Click);
            // 
            // button_HideAll
            // 
            this.button_HideAll.Font = new System.Drawing.Font("標楷體", 12F);
            this.button_HideAll.Location = new System.Drawing.Point(830, 9);
            this.button_HideAll.Name = "button_HideAll";
            this.button_HideAll.Size = new System.Drawing.Size(92, 34);
            this.button_HideAll.TabIndex = 1;
            this.button_HideAll.Text = "全部隱藏";
            this.button_HideAll.UseVisualStyleBackColor = true;
            this.button_HideAll.Click += new System.EventHandler(this.button_HideAll_Click);
            // 
            // cB_WallList
            // 
            this.cB_WallList.Font = new System.Drawing.Font("新細明體", 16F);
            this.cB_WallList.FormattingEnabled = true;
            this.cB_WallList.Location = new System.Drawing.Point(720, 55);
            this.cB_WallList.Name = "cB_WallList";
            this.cB_WallList.Size = new System.Drawing.Size(202, 29);
            this.cB_WallList.TabIndex = 2;
            // 
            // button_OnlyShowThisWall
            // 
            this.button_OnlyShowThisWall.Font = new System.Drawing.Font("標楷體", 10F);
            this.button_OnlyShowThisWall.Location = new System.Drawing.Point(830, 97);
            this.button_OnlyShowThisWall.Name = "button_OnlyShowThisWall";
            this.button_OnlyShowThisWall.Size = new System.Drawing.Size(92, 34);
            this.button_OnlyShowThisWall.TabIndex = 4;
            this.button_OnlyShowThisWall.Text = "只顯示此牆";
            this.button_OnlyShowThisWall.UseVisualStyleBackColor = true;
            this.button_OnlyShowThisWall.Click += new System.EventHandler(this.button_OnlyShowThisWall_Click);
            // 
            // button_ShowThisWallData
            // 
            this.button_ShowThisWallData.Font = new System.Drawing.Font("標楷體", 12F);
            this.button_ShowThisWallData.Location = new System.Drawing.Point(720, 97);
            this.button_ShowThisWallData.Name = "button_ShowThisWallData";
            this.button_ShowThisWallData.Size = new System.Drawing.Size(92, 34);
            this.button_ShowThisWallData.TabIndex = 3;
            this.button_ShowThisWallData.Text = "顯示資料";
            this.button_ShowThisWallData.UseVisualStyleBackColor = true;
            this.button_ShowThisWallData.Click += new System.EventHandler(this.button_ShowThisWallData_Click);
            // 
            // label_ID
            // 
            this.label_ID.AutoSize = true;
            this.label_ID.Font = new System.Drawing.Font("新細明體", 16F);
            this.label_ID.Location = new System.Drawing.Point(716, 185);
            this.label_ID.Name = "label_ID";
            this.label_ID.Size = new System.Drawing.Size(44, 22);
            this.label_ID.TabIndex = 5;
            this.label_ID.Text = "ID :";
            // 
            // button_DeleteThisWall
            // 
            this.button_DeleteThisWall.Font = new System.Drawing.Font("標楷體", 12F);
            this.button_DeleteThisWall.Location = new System.Drawing.Point(830, 136);
            this.button_DeleteThisWall.Name = "button_DeleteThisWall";
            this.button_DeleteThisWall.Size = new System.Drawing.Size(92, 34);
            this.button_DeleteThisWall.TabIndex = 8;
            this.button_DeleteThisWall.Text = "刪除此牆";
            this.button_DeleteThisWall.UseVisualStyleBackColor = true;
            this.button_DeleteThisWall.Click += new System.EventHandler(this.button_DeleteThisWall_Click);
            // 
            // button_AddNewWall
            // 
            this.button_AddNewWall.Font = new System.Drawing.Font("標楷體", 12F);
            this.button_AddNewWall.Location = new System.Drawing.Point(720, 136);
            this.button_AddNewWall.Name = "button_AddNewWall";
            this.button_AddNewWall.Size = new System.Drawing.Size(92, 34);
            this.button_AddNewWall.TabIndex = 7;
            this.button_AddNewWall.Text = "新增牆壁";
            this.button_AddNewWall.UseVisualStyleBackColor = true;
            this.button_AddNewWall.Click += new System.EventHandler(this.button_AddNewWall_Click);
            // 
            // label_StartPosition
            // 
            this.label_StartPosition.AutoSize = true;
            this.label_StartPosition.Font = new System.Drawing.Font("新細明體", 16F);
            this.label_StartPosition.Location = new System.Drawing.Point(716, 222);
            this.label_StartPosition.Name = "label_StartPosition";
            this.label_StartPosition.Size = new System.Drawing.Size(61, 22);
            this.label_StartPosition.TabIndex = 9;
            this.label_StartPosition.Text = "Start :";
            // 
            // tB_ID
            // 
            this.tB_ID.Font = new System.Drawing.Font("新細明體", 16F);
            this.tB_ID.Location = new System.Drawing.Point(809, 182);
            this.tB_ID.Name = "tB_ID";
            this.tB_ID.Size = new System.Drawing.Size(113, 33);
            this.tB_ID.TabIndex = 10;
            // 
            // tB_StartX
            // 
            this.tB_StartX.Font = new System.Drawing.Font("新細明體", 16F);
            this.tB_StartX.Location = new System.Drawing.Point(720, 247);
            this.tB_StartX.Name = "tB_StartX";
            this.tB_StartX.Size = new System.Drawing.Size(92, 33);
            this.tB_StartX.TabIndex = 11;
            // 
            // tB_StartY
            // 
            this.tB_StartY.Font = new System.Drawing.Font("新細明體", 16F);
            this.tB_StartY.Location = new System.Drawing.Point(830, 247);
            this.tB_StartY.Name = "tB_StartY";
            this.tB_StartY.Size = new System.Drawing.Size(92, 33);
            this.tB_StartY.TabIndex = 12;
            // 
            // tB_EndY
            // 
            this.tB_EndY.Font = new System.Drawing.Font("新細明體", 16F);
            this.tB_EndY.Location = new System.Drawing.Point(830, 319);
            this.tB_EndY.Name = "tB_EndY";
            this.tB_EndY.Size = new System.Drawing.Size(92, 33);
            this.tB_EndY.TabIndex = 15;
            // 
            // tB_EndX
            // 
            this.tB_EndX.Font = new System.Drawing.Font("新細明體", 16F);
            this.tB_EndX.Location = new System.Drawing.Point(720, 319);
            this.tB_EndX.Name = "tB_EndX";
            this.tB_EndX.Size = new System.Drawing.Size(92, 33);
            this.tB_EndX.TabIndex = 14;
            // 
            // label_EndPosition
            // 
            this.label_EndPosition.AutoSize = true;
            this.label_EndPosition.Font = new System.Drawing.Font("新細明體", 16F);
            this.label_EndPosition.Location = new System.Drawing.Point(716, 293);
            this.label_EndPosition.Name = "label_EndPosition";
            this.label_EndPosition.Size = new System.Drawing.Size(55, 22);
            this.label_EndPosition.TabIndex = 13;
            this.label_EndPosition.Text = "End :";
            // 
            // tB_Distance
            // 
            this.tB_Distance.Font = new System.Drawing.Font("新細明體", 16F);
            this.tB_Distance.Location = new System.Drawing.Point(720, 394);
            this.tB_Distance.Name = "tB_Distance";
            this.tB_Distance.Size = new System.Drawing.Size(202, 33);
            this.tB_Distance.TabIndex = 17;
            // 
            // label_Distance
            // 
            this.label_Distance.AutoSize = true;
            this.label_Distance.Font = new System.Drawing.Font("新細明體", 14F);
            this.label_Distance.Location = new System.Drawing.Point(716, 366);
            this.label_Distance.Name = "label_Distance";
            this.label_Distance.Size = new System.Drawing.Size(203, 19);
            this.label_Distance.TabIndex = 16;
            this.label_Distance.Text = "Distacne(有效範圍)mm : ";
            // 
            // button_Send
            // 
            this.button_Send.Font = new System.Drawing.Font("標楷體", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.button_Send.Location = new System.Drawing.Point(720, 437);
            this.button_Send.Name = "button_Send";
            this.button_Send.Size = new System.Drawing.Size(202, 34);
            this.button_Send.TabIndex = 18;
            this.button_Send.Text = "確定";
            this.button_Send.UseVisualStyleBackColor = true;
            this.button_Send.Click += new System.EventHandler(this.button_Send_Click);
            // 
            // button_WriteCSV
            // 
            this.button_WriteCSV.Font = new System.Drawing.Font("標楷體", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.button_WriteCSV.Location = new System.Drawing.Point(720, 478);
            this.button_WriteCSV.Name = "button_WriteCSV";
            this.button_WriteCSV.Size = new System.Drawing.Size(202, 34);
            this.button_WriteCSV.TabIndex = 19;
            this.button_WriteCSV.Text = "存進設定檔";
            this.button_WriteCSV.UseVisualStyleBackColor = true;
            this.button_WriteCSV.Click += new System.EventHandler(this.button_WriteCSV_Click);
            // 
            // WallSetting
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.button_WriteCSV);
            this.Controls.Add(this.button_Send);
            this.Controls.Add(this.tB_Distance);
            this.Controls.Add(this.label_Distance);
            this.Controls.Add(this.tB_EndY);
            this.Controls.Add(this.tB_EndX);
            this.Controls.Add(this.label_EndPosition);
            this.Controls.Add(this.tB_StartY);
            this.Controls.Add(this.tB_StartX);
            this.Controls.Add(this.tB_ID);
            this.Controls.Add(this.label_StartPosition);
            this.Controls.Add(this.button_DeleteThisWall);
            this.Controls.Add(this.button_AddNewWall);
            this.Controls.Add(this.label_ID);
            this.Controls.Add(this.button_OnlyShowThisWall);
            this.Controls.Add(this.button_ShowThisWallData);
            this.Controls.Add(this.cB_WallList);
            this.Controls.Add(this.button_HideAll);
            this.Controls.Add(this.button_ShowAll);
            this.Name = "WallSetting";
            this.Size = new System.Drawing.Size(930, 520);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button_ShowAll;
        private System.Windows.Forms.Button button_HideAll;
        private System.Windows.Forms.ComboBox cB_WallList;
        private System.Windows.Forms.Button button_OnlyShowThisWall;
        private System.Windows.Forms.Button button_ShowThisWallData;
        private System.Windows.Forms.Label label_ID;
        private System.Windows.Forms.Button button_DeleteThisWall;
        private System.Windows.Forms.Button button_AddNewWall;
        private System.Windows.Forms.Label label_StartPosition;
        private System.Windows.Forms.TextBox tB_ID;
        private System.Windows.Forms.TextBox tB_StartX;
        private System.Windows.Forms.TextBox tB_StartY;
        private System.Windows.Forms.TextBox tB_EndY;
        private System.Windows.Forms.TextBox tB_EndX;
        private System.Windows.Forms.Label label_EndPosition;
        private System.Windows.Forms.TextBox tB_Distance;
        private System.Windows.Forms.Label label_Distance;
        private System.Windows.Forms.Button button_Send;
        private System.Windows.Forms.Button button_WriteCSV;
    }
}
