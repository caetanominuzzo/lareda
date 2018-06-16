namespace control_panel
{
    partial class ctInstance
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
            this.components = new System.ComponentModel.Container();
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tabPeers = new System.Windows.Forms.TabPage();
            this.txtGetPeer = new System.Windows.Forms.TextBox();
            this.txtGetPeers = new System.Windows.Forms.TextBox();
            this.tabBrowser = new System.Windows.Forms.TabPage();
            this.tabLog = new System.Windows.Forms.TabPage();
            this.tabControlLog = new System.Windows.Forms.TabControl();
            this.tabLogData = new System.Windows.Forms.TabPage();
            this.txtLog = new System.Windows.Forms.RichTextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnLogClear = new System.Windows.Forms.Button();
            this.tabLogFilter = new System.Windows.Forms.TabPage();
            this.flowLogItens = new System.Windows.Forms.FlowLayoutPanel();
            this.btnRemove = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.chAttach = new System.Windows.Forms.CheckBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.tabMetapackets = new System.Windows.Forms.TabPage();
            this.txtMetapacktes = new System.Windows.Forms.RichTextBox();
            this.tabMain.SuspendLayout();
            this.tabPeers.SuspendLayout();
            this.tabLog.SuspendLayout();
            this.tabControlLog.SuspendLayout();
            this.tabLogData.SuspendLayout();
            this.panel1.SuspendLayout();
            this.tabLogFilter.SuspendLayout();
            this.panel2.SuspendLayout();
            this.tabMetapackets.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabMain
            // 
            this.tabMain.Controls.Add(this.tabPeers);
            this.tabMain.Controls.Add(this.tabBrowser);
            this.tabMain.Controls.Add(this.tabLog);
            this.tabMain.Controls.Add(this.tabMetapackets);
            this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMain.Location = new System.Drawing.Point(0, 29);
            this.tabMain.Margin = new System.Windows.Forms.Padding(0);
            this.tabMain.Name = "tabMain";
            this.tabMain.Padding = new System.Drawing.Point(0, 0);
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(573, 251);
            this.tabMain.TabIndex = 1;
            this.tabMain.SelectedIndexChanged += new System.EventHandler(this.tabMain_SelectedIndexChanged);
            // 
            // tabPeers
            // 
            this.tabPeers.Controls.Add(this.txtGetPeer);
            this.tabPeers.Controls.Add(this.txtGetPeers);
            this.tabPeers.Location = new System.Drawing.Point(4, 22);
            this.tabPeers.Name = "tabPeers";
            this.tabPeers.Padding = new System.Windows.Forms.Padding(3);
            this.tabPeers.Size = new System.Drawing.Size(565, 225);
            this.tabPeers.TabIndex = 0;
            this.tabPeers.Text = "Peers";
            this.tabPeers.UseVisualStyleBackColor = true;
            this.tabPeers.Enter += new System.EventHandler(this.tabPeers_Enter);
            // 
            // txtGetPeer
            // 
            this.txtGetPeer.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtGetPeer.Location = new System.Drawing.Point(3, 129);
            this.txtGetPeer.Name = "txtGetPeer";
            this.txtGetPeer.Size = new System.Drawing.Size(559, 20);
            this.txtGetPeer.TabIndex = 2;
            this.txtGetPeer.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtAddPeer_KeyPress);
            // 
            // txtGetPeers
            // 
            this.txtGetPeers.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtGetPeers.Location = new System.Drawing.Point(3, 3);
            this.txtGetPeers.Multiline = true;
            this.txtGetPeers.Name = "txtGetPeers";
            this.txtGetPeers.Size = new System.Drawing.Size(559, 126);
            this.txtGetPeers.TabIndex = 3;
            // 
            // tabBrowser
            // 
            this.tabBrowser.Location = new System.Drawing.Point(4, 22);
            this.tabBrowser.Name = "tabBrowser";
            this.tabBrowser.Padding = new System.Windows.Forms.Padding(3);
            this.tabBrowser.Size = new System.Drawing.Size(565, 225);
            this.tabBrowser.TabIndex = 1;
            this.tabBrowser.Text = "Browser";
            this.tabBrowser.UseVisualStyleBackColor = true;
            this.tabBrowser.Enter += new System.EventHandler(this.tabBrowser_Enter);
            // 
            // tabLog
            // 
            this.tabLog.Controls.Add(this.tabControlLog);
            this.tabLog.Location = new System.Drawing.Point(4, 22);
            this.tabLog.Name = "tabLog";
            this.tabLog.Padding = new System.Windows.Forms.Padding(3);
            this.tabLog.Size = new System.Drawing.Size(565, 225);
            this.tabLog.TabIndex = 2;
            this.tabLog.Text = "Log";
            this.tabLog.UseVisualStyleBackColor = true;
            // 
            // tabControlLog
            // 
            this.tabControlLog.Controls.Add(this.tabLogData);
            this.tabControlLog.Controls.Add(this.tabLogFilter);
            this.tabControlLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlLog.Location = new System.Drawing.Point(3, 3);
            this.tabControlLog.Name = "tabControlLog";
            this.tabControlLog.SelectedIndex = 0;
            this.tabControlLog.Size = new System.Drawing.Size(559, 219);
            this.tabControlLog.TabIndex = 6;
            // 
            // tabLogData
            // 
            this.tabLogData.Controls.Add(this.txtLog);
            this.tabLogData.Controls.Add(this.panel1);
            this.tabLogData.Location = new System.Drawing.Point(4, 22);
            this.tabLogData.Name = "tabLogData";
            this.tabLogData.Padding = new System.Windows.Forms.Padding(3);
            this.tabLogData.Size = new System.Drawing.Size(551, 193);
            this.tabLogData.TabIndex = 0;
            this.tabLogData.Text = "Data";
            this.tabLogData.UseVisualStyleBackColor = true;
            // 
            // txtLog
            // 
            this.txtLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Location = new System.Drawing.Point(3, 31);
            this.txtLog.Name = "txtLog";
            this.txtLog.Size = new System.Drawing.Size(545, 159);
            this.txtLog.TabIndex = 8;
            this.txtLog.Text = "";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnLogClear);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(545, 28);
            this.panel1.TabIndex = 6;
            // 
            // btnLogClear
            // 
            this.btnLogClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLogClear.Location = new System.Drawing.Point(470, 3);
            this.btnLogClear.Name = "btnLogClear";
            this.btnLogClear.Size = new System.Drawing.Size(75, 23);
            this.btnLogClear.TabIndex = 3;
            this.btnLogClear.Text = "Clear";
            this.btnLogClear.UseVisualStyleBackColor = true;
            this.btnLogClear.Click += new System.EventHandler(this.btnLogClear_Click);
            // 
            // tabLogFilter
            // 
            this.tabLogFilter.Controls.Add(this.flowLogItens);
            this.tabLogFilter.Location = new System.Drawing.Point(4, 22);
            this.tabLogFilter.Name = "tabLogFilter";
            this.tabLogFilter.Padding = new System.Windows.Forms.Padding(3);
            this.tabLogFilter.Size = new System.Drawing.Size(551, 193);
            this.tabLogFilter.TabIndex = 1;
            this.tabLogFilter.Text = "Filter";
            this.tabLogFilter.UseVisualStyleBackColor = true;
            // 
            // flowLogItens
            // 
            this.flowLogItens.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLogItens.Location = new System.Drawing.Point(3, 3);
            this.flowLogItens.Name = "flowLogItens";
            this.flowLogItens.Size = new System.Drawing.Size(545, 187);
            this.flowLogItens.TabIndex = 0;
            // 
            // btnRemove
            // 
            this.btnRemove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRemove.Location = new System.Drawing.Point(495, 3);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(75, 23);
            this.btnRemove.TabIndex = 2;
            this.btnRemove.Text = "Remove";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 5000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // chAttach
            // 
            this.chAttach.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chAttach.AutoSize = true;
            this.chAttach.Location = new System.Drawing.Point(432, 7);
            this.chAttach.Name = "chAttach";
            this.chAttach.Size = new System.Drawing.Size(57, 17);
            this.chAttach.TabIndex = 3;
            this.chAttach.Text = "Attach";
            this.chAttach.UseVisualStyleBackColor = true;
            this.chAttach.CheckedChanged += new System.EventHandler(this.chAttach_CheckedChanged);
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.SystemColors.ControlDark;
            this.panel2.Controls.Add(this.lblTitle);
            this.panel2.Controls.Add(this.chAttach);
            this.panel2.Controls.Add(this.btnRemove);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(573, 29);
            this.panel2.TabIndex = 4;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Padding = new System.Windows.Forms.Padding(8);
            this.lblTitle.Size = new System.Drawing.Size(65, 29);
            this.lblTitle.TabIndex = 4;
            this.lblTitle.Text = "Endpoint";
            // 
            // tabMetapackets
            // 
            this.tabMetapackets.Controls.Add(this.txtMetapacktes);
            this.tabMetapackets.Location = new System.Drawing.Point(4, 22);
            this.tabMetapackets.Name = "tabMetapackets";
            this.tabMetapackets.Padding = new System.Windows.Forms.Padding(3);
            this.tabMetapackets.Size = new System.Drawing.Size(565, 225);
            this.tabMetapackets.TabIndex = 3;
            this.tabMetapackets.Text = "Metapackets";
            this.tabMetapackets.UseVisualStyleBackColor = true;
            // 
            // txtMetapacktes
            // 
            this.txtMetapacktes.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtMetapacktes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtMetapacktes.Location = new System.Drawing.Point(3, 3);
            this.txtMetapacktes.Name = "txtMetapacktes";
            this.txtMetapacktes.Size = new System.Drawing.Size(559, 219);
            this.txtMetapacktes.TabIndex = 9;
            this.txtMetapacktes.Text = "";
            // 
            // ctInstance
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabMain);
            this.Controls.Add(this.panel2);
            this.Name = "ctInstance";
            this.Size = new System.Drawing.Size(573, 280);
            this.tabMain.ResumeLayout(false);
            this.tabPeers.ResumeLayout(false);
            this.tabPeers.PerformLayout();
            this.tabLog.ResumeLayout(false);
            this.tabControlLog.ResumeLayout(false);
            this.tabLogData.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.tabLogFilter.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.tabMetapackets.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabBrowser;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.TabPage tabPeers;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.TabPage tabLog;
        private System.Windows.Forms.TextBox txtGetPeers;
        private System.Windows.Forms.CheckBox chAttach;
        private System.Windows.Forms.TabControl tabControlLog;
        private System.Windows.Forms.TabPage tabLogData;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnLogClear;
        private System.Windows.Forms.TabPage tabLogFilter;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.FlowLayoutPanel flowLogItens;
        internal System.Windows.Forms.TextBox txtGetPeer;
        private System.Windows.Forms.RichTextBox txtLog;
        private System.Windows.Forms.TabPage tabMetapackets;
        private System.Windows.Forms.RichTextBox txtMetapacktes;
    }
}
