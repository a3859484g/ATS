namespace Mirle.Agv.INX.View
{
    partial class LoginForm
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
            this.cB_LoginLevel = new System.Windows.Forms.ComboBox();
            this.tB_Password = new System.Windows.Forms.TextBox();
            this.button_login = new System.Windows.Forms.Button();
            this.button_Logout = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // cB_LoginLevel
            // 
            this.cB_LoginLevel.Font = new System.Drawing.Font("標楷體", 30F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.cB_LoginLevel.FormattingEnabled = true;
            this.cB_LoginLevel.Location = new System.Drawing.Point(61, 53);
            this.cB_LoginLevel.Name = "cB_LoginLevel";
            this.cB_LoginLevel.Size = new System.Drawing.Size(292, 48);
            this.cB_LoginLevel.TabIndex = 0;
            this.cB_LoginLevel.Click += new System.EventHandler(this.Hide_Click);
            // 
            // tB_Password
            // 
            this.tB_Password.Font = new System.Drawing.Font("標楷體", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.tB_Password.Location = new System.Drawing.Point(61, 146);
            this.tB_Password.Name = "tB_Password";
            this.tB_Password.PasswordChar = '*';
            this.tB_Password.Size = new System.Drawing.Size(292, 49);
            this.tB_Password.TabIndex = 1;
            this.tB_Password.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.tB_Password.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tB_Password_KeyPress);
            // 
            // button_login
            // 
            this.button_login.Font = new System.Drawing.Font("標楷體", 24F);
            this.button_login.Location = new System.Drawing.Point(61, 248);
            this.button_login.Name = "button_login";
            this.button_login.Size = new System.Drawing.Size(128, 68);
            this.button_login.TabIndex = 2;
            this.button_login.Text = "Login";
            this.button_login.UseVisualStyleBackColor = true;
            this.button_login.Click += new System.EventHandler(this.button_login_Click);
            // 
            // button_Logout
            // 
            this.button_Logout.Font = new System.Drawing.Font("標楷體", 24F);
            this.button_Logout.Location = new System.Drawing.Point(225, 248);
            this.button_Logout.Name = "button_Logout";
            this.button_Logout.Size = new System.Drawing.Size(128, 68);
            this.button_Logout.TabIndex = 3;
            this.button_Logout.Text = "Logout";
            this.button_Logout.UseVisualStyleBackColor = true;
            this.button_Logout.Click += new System.EventHandler(this.button_Logout_Click);
            // 
            // LoginForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.button_Logout);
            this.Controls.Add(this.button_login);
            this.Controls.Add(this.tB_Password);
            this.Controls.Add(this.cB_LoginLevel);
            this.Name = "LoginForm";
            this.Size = new System.Drawing.Size(422, 374);
            this.Click += new System.EventHandler(this.Hide_Click);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cB_LoginLevel;
        private System.Windows.Forms.TextBox tB_Password;
        private System.Windows.Forms.Button button_login;
        private System.Windows.Forms.Button button_Logout;
    }
}
