namespace Mirle.Agv.INX.View
{
    partial class PIOHistoryForm
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
            this.panel = new System.Windows.Forms.Panel();
            this.label_CommandIDValue = new System.Windows.Forms.Label();
            this.label_CommandID = new System.Windows.Forms.Label();
            this.label_Action = new System.Windows.Forms.Label();
            this.label_AutoManual = new System.Windows.Forms.Label();
            this.label_PIOResult = new System.Windows.Forms.Label();
            this.label_CSTID = new System.Windows.Forms.Label();
            this.label_AddressID = new System.Windows.Forms.Label();
            this.label_AddressIDValue = new System.Windows.Forms.Label();
            this.label_ErrorCodeValue = new System.Windows.Forms.Label();
            this.label_CommandResult = new System.Windows.Forms.Label();
            this.label_CommandResultValue = new System.Windows.Forms.Label();
            this.label_EndTime = new System.Windows.Forms.Label();
            this.label_EndTimeValue = new System.Windows.Forms.Label();
            this.label_StartTime = new System.Windows.Forms.Label();
            this.label_StartTimeValue = new System.Windows.Forms.Label();
            this.label_AlignmentData = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // panel
            // 
            this.panel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel.Location = new System.Drawing.Point(0, 203);
            this.panel.Name = "panel";
            this.panel.Size = new System.Drawing.Size(700, 295);
            this.panel.TabIndex = 1;
            this.panel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panel_MouseDown);
            this.panel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.panel_MouseMove);
            this.panel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.panel_MouseUp);
            // 
            // label_CommandIDValue
            // 
            this.label_CommandIDValue.Font = new System.Drawing.Font("標楷體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_CommandIDValue.Location = new System.Drawing.Point(200, 10);
            this.label_CommandIDValue.Name = "label_CommandIDValue";
            this.label_CommandIDValue.Size = new System.Drawing.Size(140, 30);
            this.label_CommandIDValue.TabIndex = 2;
            this.label_CommandIDValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label_CommandID
            // 
            this.label_CommandID.Font = new System.Drawing.Font("標楷體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_CommandID.Location = new System.Drawing.Point(30, 10);
            this.label_CommandID.Name = "label_CommandID";
            this.label_CommandID.Size = new System.Drawing.Size(140, 30);
            this.label_CommandID.TabIndex = 2;
            this.label_CommandID.Text = "Command ID :";
            this.label_CommandID.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label_Action
            // 
            this.label_Action.Font = new System.Drawing.Font("標楷體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_Action.Location = new System.Drawing.Point(369, 10);
            this.label_Action.Name = "label_Action";
            this.label_Action.Size = new System.Drawing.Size(140, 30);
            this.label_Action.TabIndex = 3;
            this.label_Action.Text = "Load/Unload";
            this.label_Action.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label_AutoManual
            // 
            this.label_AutoManual.Font = new System.Drawing.Font("標楷體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_AutoManual.Location = new System.Drawing.Point(534, 10);
            this.label_AutoManual.Name = "label_AutoManual";
            this.label_AutoManual.Size = new System.Drawing.Size(140, 30);
            this.label_AutoManual.TabIndex = 4;
            this.label_AutoManual.Text = "Auto/Manual";
            this.label_AutoManual.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label_PIOResult
            // 
            this.label_PIOResult.Font = new System.Drawing.Font("標楷體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_PIOResult.Location = new System.Drawing.Point(369, 50);
            this.label_PIOResult.Name = "label_PIOResult";
            this.label_PIOResult.Size = new System.Drawing.Size(140, 30);
            this.label_PIOResult.TabIndex = 7;
            this.label_PIOResult.Text = "PIO Result";
            this.label_PIOResult.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label_CSTID
            // 
            this.label_CSTID.Font = new System.Drawing.Font("標楷體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_CSTID.Location = new System.Drawing.Point(534, 50);
            this.label_CSTID.Name = "label_CSTID";
            this.label_CSTID.Size = new System.Drawing.Size(140, 30);
            this.label_CSTID.TabIndex = 8;
            this.label_CSTID.Text = "ReadOK/Fail";
            this.label_CSTID.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label_AddressID
            // 
            this.label_AddressID.Font = new System.Drawing.Font("標楷體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_AddressID.Location = new System.Drawing.Point(30, 50);
            this.label_AddressID.Name = "label_AddressID";
            this.label_AddressID.Size = new System.Drawing.Size(140, 30);
            this.label_AddressID.TabIndex = 5;
            this.label_AddressID.Text = "Address ID :";
            this.label_AddressID.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label_AddressIDValue
            // 
            this.label_AddressIDValue.Font = new System.Drawing.Font("標楷體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_AddressIDValue.Location = new System.Drawing.Point(200, 50);
            this.label_AddressIDValue.Name = "label_AddressIDValue";
            this.label_AddressIDValue.Size = new System.Drawing.Size(140, 30);
            this.label_AddressIDValue.TabIndex = 6;
            this.label_AddressIDValue.Text = "~~";
            this.label_AddressIDValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label_ErrorCodeValue
            // 
            this.label_ErrorCodeValue.Font = new System.Drawing.Font("標楷體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_ErrorCodeValue.Location = new System.Drawing.Point(373, 90);
            this.label_ErrorCodeValue.Name = "label_ErrorCodeValue";
            this.label_ErrorCodeValue.Size = new System.Drawing.Size(301, 30);
            this.label_ErrorCodeValue.TabIndex = 12;
            this.label_ErrorCodeValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label_CommandResult
            // 
            this.label_CommandResult.Font = new System.Drawing.Font("標楷體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_CommandResult.Location = new System.Drawing.Point(30, 90);
            this.label_CommandResult.Name = "label_CommandResult";
            this.label_CommandResult.Size = new System.Drawing.Size(140, 30);
            this.label_CommandResult.TabIndex = 9;
            this.label_CommandResult.Text = "Result : ";
            this.label_CommandResult.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label_CommandResultValue
            // 
            this.label_CommandResultValue.Font = new System.Drawing.Font("標楷體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_CommandResultValue.Location = new System.Drawing.Point(200, 90);
            this.label_CommandResultValue.Name = "label_CommandResultValue";
            this.label_CommandResultValue.Size = new System.Drawing.Size(140, 30);
            this.label_CommandResultValue.TabIndex = 10;
            this.label_CommandResultValue.Text = "~~~";
            this.label_CommandResultValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label_EndTime
            // 
            this.label_EndTime.Font = new System.Drawing.Font("標楷體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_EndTime.Location = new System.Drawing.Point(369, 130);
            this.label_EndTime.Name = "label_EndTime";
            this.label_EndTime.Size = new System.Drawing.Size(140, 30);
            this.label_EndTime.TabIndex = 15;
            this.label_EndTime.Text = "End Time :";
            this.label_EndTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label_EndTimeValue
            // 
            this.label_EndTimeValue.Font = new System.Drawing.Font("標楷體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_EndTimeValue.Location = new System.Drawing.Point(534, 130);
            this.label_EndTimeValue.Name = "label_EndTimeValue";
            this.label_EndTimeValue.Size = new System.Drawing.Size(140, 30);
            this.label_EndTimeValue.TabIndex = 16;
            this.label_EndTimeValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label_StartTime
            // 
            this.label_StartTime.Font = new System.Drawing.Font("標楷體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_StartTime.Location = new System.Drawing.Point(30, 130);
            this.label_StartTime.Name = "label_StartTime";
            this.label_StartTime.Size = new System.Drawing.Size(140, 30);
            this.label_StartTime.TabIndex = 13;
            this.label_StartTime.Text = "Start Time : ";
            this.label_StartTime.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label_StartTimeValue
            // 
            this.label_StartTimeValue.Font = new System.Drawing.Font("標楷體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_StartTimeValue.Location = new System.Drawing.Point(200, 130);
            this.label_StartTimeValue.Name = "label_StartTimeValue";
            this.label_StartTimeValue.Size = new System.Drawing.Size(140, 30);
            this.label_StartTimeValue.TabIndex = 14;
            this.label_StartTimeValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label_AlignmentData
            // 
            this.label_AlignmentData.Font = new System.Drawing.Font("標楷體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_AlignmentData.Location = new System.Drawing.Point(30, 170);
            this.label_AlignmentData.Name = "label_AlignmentData";
            this.label_AlignmentData.Size = new System.Drawing.Size(644, 30);
            this.label_AlignmentData.TabIndex = 17;
            this.label_AlignmentData.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // PIOHistoryForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label_AlignmentData);
            this.Controls.Add(this.label_EndTime);
            this.Controls.Add(this.label_EndTimeValue);
            this.Controls.Add(this.label_StartTime);
            this.Controls.Add(this.label_StartTimeValue);
            this.Controls.Add(this.label_ErrorCodeValue);
            this.Controls.Add(this.label_CommandResult);
            this.Controls.Add(this.label_CommandResultValue);
            this.Controls.Add(this.label_PIOResult);
            this.Controls.Add(this.label_CSTID);
            this.Controls.Add(this.label_AddressID);
            this.Controls.Add(this.label_AddressIDValue);
            this.Controls.Add(this.label_Action);
            this.Controls.Add(this.label_AutoManual);
            this.Controls.Add(this.label_CommandID);
            this.Controls.Add(this.label_CommandIDValue);
            this.Controls.Add(this.panel);
            this.Name = "PIOHistoryForm";
            this.Size = new System.Drawing.Size(700, 500);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel panel;
        private System.Windows.Forms.Label label_CommandIDValue;
        private System.Windows.Forms.Label label_CommandID;
        private System.Windows.Forms.Label label_Action;
        private System.Windows.Forms.Label label_AutoManual;
        private System.Windows.Forms.Label label_PIOResult;
        private System.Windows.Forms.Label label_CSTID;
        private System.Windows.Forms.Label label_AddressID;
        private System.Windows.Forms.Label label_AddressIDValue;
        private System.Windows.Forms.Label label_ErrorCodeValue;
        private System.Windows.Forms.Label label_CommandResult;
        private System.Windows.Forms.Label label_CommandResultValue;
        private System.Windows.Forms.Label label_EndTime;
        private System.Windows.Forms.Label label_EndTimeValue;
        private System.Windows.Forms.Label label_StartTime;
        private System.Windows.Forms.Label label_StartTimeValue;
        private System.Windows.Forms.Label label_AlignmentData;
    }
}
