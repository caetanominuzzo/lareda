namespace sim2
{
    partial class Form1
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series3 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series4 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series5 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series6 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend2 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series7 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.btnAddPeers = new System.Windows.Forms.Button();
            this.lblTotalPeers = new System.Windows.Forms.Label();
            this.txtAddPeers = new System.Windows.Forms.TextBox();
            this.btNClearPeers = new System.Windows.Forms.Button();
            this.txtProcessPeers = new System.Windows.Forms.TextBox();
            this.btNProcess = new System.Windows.Forms.Button();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnSeries = new System.Windows.Forms.Button();
            this.btnOptimize = new System.Windows.Forms.Button();
            this.chkTorus = new System.Windows.Forms.CheckBox();
            this.chkChain = new System.Windows.Forms.CheckedListBox();
            this.chkSelected = new System.Windows.Forms.CheckedListBox();
            this.button1 = new System.Windows.Forms.Button();
            this.chart2 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.picDHT = new System.Windows.Forms.PictureBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.picGeo = new System.Windows.Forms.PictureBox();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.txtLinks = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picDHT)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picGeo)).BeginInit();
            this.SuspendLayout();
            // 
            // btnAddPeers
            // 
            this.btnAddPeers.Location = new System.Drawing.Point(15, 55);
            this.btnAddPeers.Name = "btnAddPeers";
            this.btnAddPeers.Size = new System.Drawing.Size(82, 23);
            this.btnAddPeers.TabIndex = 0;
            this.btnAddPeers.Text = "AddPeers";
            this.btnAddPeers.UseVisualStyleBackColor = true;
            this.btnAddPeers.Click += new System.EventHandler(this.btnAddPeers_Click);
            // 
            // lblTotalPeers
            // 
            this.lblTotalPeers.AutoSize = true;
            this.lblTotalPeers.Location = new System.Drawing.Point(16, 13);
            this.lblTotalPeers.Name = "lblTotalPeers";
            this.lblTotalPeers.Size = new System.Drawing.Size(73, 13);
            this.lblTotalPeers.TabIndex = 1;
            this.lblTotalPeers.Text = "Total Peers: 0";
            // 
            // txtAddPeers
            // 
            this.txtAddPeers.Location = new System.Drawing.Point(15, 29);
            this.txtAddPeers.Name = "txtAddPeers";
            this.txtAddPeers.Size = new System.Drawing.Size(100, 20);
            this.txtAddPeers.TabIndex = 2;
            this.txtAddPeers.Text = "1000";
            // 
            // btNClearPeers
            // 
            this.btNClearPeers.Location = new System.Drawing.Point(14, 84);
            this.btNClearPeers.Name = "btNClearPeers";
            this.btNClearPeers.Size = new System.Drawing.Size(83, 23);
            this.btNClearPeers.TabIndex = 3;
            this.btNClearPeers.Text = "Clear";
            this.btNClearPeers.UseVisualStyleBackColor = true;
            this.btNClearPeers.Click += new System.EventHandler(this.btNClearPeers_Click);
            // 
            // txtProcessPeers
            // 
            this.txtProcessPeers.Location = new System.Drawing.Point(316, 29);
            this.txtProcessPeers.Name = "txtProcessPeers";
            this.txtProcessPeers.Size = new System.Drawing.Size(100, 20);
            this.txtProcessPeers.TabIndex = 6;
            this.txtProcessPeers.Text = "100";
            this.txtProcessPeers.TextChanged += new System.EventHandler(this.txtProcessPeers_TextChanged);
            // 
            // btNProcess
            // 
            this.btNProcess.Location = new System.Drawing.Point(316, 55);
            this.btNProcess.Name = "btNProcess";
            this.btNProcess.Size = new System.Drawing.Size(82, 23);
            this.btNProcess.TabIndex = 4;
            this.btNProcess.Text = "Process Peers";
            this.btNProcess.UseVisualStyleBackColor = true;
            this.btNProcess.Click += new System.EventHandler(this.btNProcess_Click);
            // 
            // chart1
            // 
            chartArea1.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea1);
            this.chart1.Dock = System.Windows.Forms.DockStyle.Fill;
            legend1.Name = "Legend1";
            this.chart1.Legends.Add(legend1);
            this.chart1.Location = new System.Drawing.Point(3, 3);
            this.chart1.Name = "chart1";
            series1.ChartArea = "ChartArea1";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series1.Legend = "Legend1";
            series1.Name = "NetAvgDistance";
            series2.ChartArea = "ChartArea1";
            series2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series2.Legend = "Legend1";
            series2.Name = "GEO NetAvgDistance";
            series3.ChartArea = "ChartArea1";
            series3.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series3.Legend = "Legend1";
            series3.Name = "QTD Peers";
            series3.YAxisType = System.Windows.Forms.DataVisualization.Charting.AxisType.Secondary;
            series4.ChartArea = "ChartArea1";
            series4.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series4.Legend = "Legend1";
            series4.Name = "Jumps";
            series5.ChartArea = "ChartArea1";
            series5.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series5.Legend = "Legend1";
            series5.Name = "Return Ratio";
            series6.ChartArea = "ChartArea1";
            series6.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series6.Legend = "Legend1";
            series6.Name = "Latency";
            this.chart1.Series.Add(series1);
            this.chart1.Series.Add(series2);
            this.chart1.Series.Add(series3);
            this.chart1.Series.Add(series4);
            this.chart1.Series.Add(series5);
            this.chart1.Series.Add(series6);
            this.chart1.Size = new System.Drawing.Size(1128, 387);
            this.chart1.TabIndex = 7;
            this.chart1.Text = "chart1";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.txtLinks);
            this.panel1.Controls.Add(this.btnSeries);
            this.panel1.Controls.Add(this.btnOptimize);
            this.panel1.Controls.Add(this.chkTorus);
            this.panel1.Controls.Add(this.chkChain);
            this.panel1.Controls.Add(this.chkSelected);
            this.panel1.Controls.Add(this.button1);
            this.panel1.Controls.Add(this.lblTotalPeers);
            this.panel1.Controls.Add(this.btnAddPeers);
            this.panel1.Controls.Add(this.txtProcessPeers);
            this.panel1.Controls.Add(this.txtAddPeers);
            this.panel1.Controls.Add(this.btNProcess);
            this.panel1.Controls.Add(this.btNClearPeers);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1142, 123);
            this.panel1.TabIndex = 8;
            this.panel1.DoubleClick += new System.EventHandler(this.panel1_DoubleClick);
            // 
            // btnSeries
            // 
            this.btnSeries.Location = new System.Drawing.Point(405, 55);
            this.btnSeries.Name = "btnSeries";
            this.btnSeries.Size = new System.Drawing.Size(75, 23);
            this.btnSeries.TabIndex = 12;
            this.btnSeries.Text = "Series";
            this.btnSeries.UseVisualStyleBackColor = true;
            this.btnSeries.Click += new System.EventHandler(this.btnSeries_Click);
            // 
            // btnOptimize
            // 
            this.btnOptimize.Location = new System.Drawing.Point(316, 84);
            this.btnOptimize.Name = "btnOptimize";
            this.btnOptimize.Size = new System.Drawing.Size(75, 23);
            this.btnOptimize.TabIndex = 11;
            this.btnOptimize.Text = "Optimize";
            this.btnOptimize.UseVisualStyleBackColor = true;
            this.btnOptimize.Click += new System.EventHandler(this.btnOptimize_Click);
            // 
            // chkTorus
            // 
            this.chkTorus.AutoSize = true;
            this.chkTorus.Location = new System.Drawing.Point(754, 88);
            this.chkTorus.Name = "chkTorus";
            this.chkTorus.Size = new System.Drawing.Size(64, 17);
            this.chkTorus.TabIndex = 10;
            this.chkTorus.Text = "Toroidal";
            this.chkTorus.UseVisualStyleBackColor = true;
            this.chkTorus.CheckedChanged += new System.EventHandler(this.chkTorus_CheckedChanged);
            // 
            // chkChain
            // 
            this.chkChain.FormattingEnabled = true;
            this.chkChain.Location = new System.Drawing.Point(627, 12);
            this.chkChain.Name = "chkChain";
            this.chkChain.Size = new System.Drawing.Size(120, 94);
            this.chkChain.TabIndex = 9;
            this.chkChain.SelectedValueChanged += new System.EventHandler(this.chkTorus_CheckedChanged);
            // 
            // chkSelected
            // 
            this.chkSelected.FormattingEnabled = true;
            this.chkSelected.Location = new System.Drawing.Point(501, 12);
            this.chkSelected.Name = "chkSelected";
            this.chkSelected.Size = new System.Drawing.Size(120, 94);
            this.chkSelected.TabIndex = 8;
            this.chkSelected.SelectedValueChanged += new System.EventHandler(this.chkTorus_CheckedChanged);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(811, 13);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 7;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // chart2
            // 
            chartArea2.Name = "ChartArea1";
            this.chart2.ChartAreas.Add(chartArea2);
            this.chart2.Dock = System.Windows.Forms.DockStyle.Bottom;
            legend2.Name = "Legend1";
            this.chart2.Legends.Add(legend2);
            this.chart2.Location = new System.Drawing.Point(0, 542);
            this.chart2.Name = "chart2";
            this.chart2.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.Excel;
            series7.ChartArea = "ChartArea1";
            series7.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series7.Legend = "Legend1";
            series7.Name = "Total Peers";
            this.chart2.Series.Add(series7);
            this.chart2.Size = new System.Drawing.Size(1142, 25);
            this.chart2.TabIndex = 9;
            this.chart2.Text = "chart2";
            // 
            // picDHT
            // 
            this.picDHT.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picDHT.Location = new System.Drawing.Point(0, 0);
            this.picDHT.Name = "picDHT";
            this.picDHT.Size = new System.Drawing.Size(376, 387);
            this.picDHT.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picDHT.TabIndex = 10;
            this.picDHT.TabStop = false;
            this.picDHT.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBox1_Paint);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 123);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1142, 419);
            this.tabControl1.TabIndex = 11;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.chart1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(1134, 393);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.splitContainer1);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(1134, 393);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 3);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.picDHT);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.picGeo);
            this.splitContainer1.Size = new System.Drawing.Size(1128, 387);
            this.splitContainer1.SplitterDistance = 376;
            this.splitContainer1.TabIndex = 13;
            // 
            // picGeo
            // 
            this.picGeo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picGeo.Location = new System.Drawing.Point(0, 0);
            this.picGeo.Name = "picGeo";
            this.picGeo.Size = new System.Drawing.Size(748, 387);
            this.picGeo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picGeo.TabIndex = 12;
            this.picGeo.TabStop = false;
            // 
            // tabPage3
            // 
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(1134, 393);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "tabPage3";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // txtLinks
            // 
            this.txtLinks.Location = new System.Drawing.Point(122, 29);
            this.txtLinks.Name = "txtLinks";
            this.txtLinks.Size = new System.Drawing.Size(100, 20);
            this.txtLinks.TabIndex = 13;
            this.txtLinks.Text = "10";
            this.txtLinks.TextChanged += new System.EventHandler(this.txtLinks_TextChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1142, 567);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.chart2);
            this.Controls.Add(this.panel1);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picDHT)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picGeo)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnAddPeers;
        private System.Windows.Forms.Label lblTotalPeers;
        private System.Windows.Forms.TextBox txtAddPeers;
        private System.Windows.Forms.Button btNClearPeers;
        private System.Windows.Forms.TextBox txtProcessPeers;
        private System.Windows.Forms.Button btNProcess;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart2;
        private System.Windows.Forms.PictureBox picDHT;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.PictureBox picGeo;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.CheckedListBox chkChain;
        private System.Windows.Forms.CheckedListBox chkSelected;
        private System.Windows.Forms.CheckBox chkTorus;
        private System.Windows.Forms.Button btnOptimize;
        private System.Windows.Forms.Button btnSeries;
        private System.Windows.Forms.TextBox txtLinks;
    }
}

