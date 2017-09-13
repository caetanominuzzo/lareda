namespace windows_desktop
{
    partial class ctDownload
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblFilename = new System.Windows.Forms.TextBox();
            this.packetsFlow = new System.Windows.Forms.FlowLayoutPanel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // lblFilename
            // 
            this.lblFilename.BackColor = System.Drawing.Color.Black;
            this.lblFilename.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lblFilename.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblFilename.Font = new System.Drawing.Font("Segoe UI", 9.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFilename.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.lblFilename.Location = new System.Drawing.Point(0, 0);
            this.lblFilename.Name = "lblFilename";
            this.lblFilename.ReadOnly = true;
            this.lblFilename.Size = new System.Drawing.Size(270, 17);
            this.lblFilename.TabIndex = 3;
            this.lblFilename.Text = "ds fasdf asdf asdfasdf";
            // 
            // packetsFlow
            // 
            this.packetsFlow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.packetsFlow.Location = new System.Drawing.Point(0, 17);
            this.packetsFlow.Name = "packetsFlow";
            this.packetsFlow.Size = new System.Drawing.Size(270, 37);
            this.packetsFlow.TabIndex = 4;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(3, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(56, 50);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // ctDownload
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.packetsFlow);
            this.Controls.Add(this.lblFilename);
            this.Controls.Add(this.pictureBox1);
            this.Name = "ctDownload";
            this.Size = new System.Drawing.Size(270, 54);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox lblFilename;
        private System.Windows.Forms.FlowLayoutPanel packetsFlow;
    }
}
