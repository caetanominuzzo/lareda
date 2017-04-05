using library;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace windows_desktop
{
    public partial class fmLog : Form
    {
        public fmLog()
        {
            InitializeComponent();
        }

        private void fmLog_Load(object sender, EventArgs e)
        {
            Log.OnLog += Log_OnLog;

            var names = Enum.GetNames(typeof(Log.LogTypes));

            var values = Enum.GetValues(typeof(Log.LogTypes));

            for (var i = 0; i < names.Length; i++)
            {
                var name = names[i];

                var value = values.GetValue(i);

                CheckBox c = new CheckBox();

                c.Text = name;

                c.AutoSize = true;

                c.MinimumSize = new Size(200, 28);

                c.Click += C_Click;

                c.Tag = value;

                c.Checked = ((Log.LogTypes)value & Log.filter) != Log.LogTypes.None;

                flowLogTypes.Controls.Add(c);
            }

        }

        private void Log_OnLog(Log.LogItem item)
        {
            Refresh(item);
        
        }

        delegate void RefreshCallback(Log.LogItem item);

        private void Refresh(Log.LogItem item)
        {
            if (this.textBox1.InvokeRequired)
            {
                RefreshCallback d = new RefreshCallback(Refresh);

                this.Invoke(d, new object[] { item });
            }
            else
            {
                var s = Newtonsoft.Json.JsonConvert.SerializeObject(item.Data);

                if (textBox2.Text.Length == 0 || s.Contains(textBox2.Text))
                {
                    //int caretPos = textBox1.Text.Length;
                    textBox1.AppendText(string.Concat(item.DateTime.ToString("HH:mm:ss.fff"), "\t", item.Type, "\t", s, Environment.NewLine));

                    

                    //textBox1.Select(caretPos, 0);
                 //   textBox1.ScrollToLine(textBox1.GetLineIndexFromCharacterIndex(caretPos));
                }
            }
        }

        void SetValues(object sender)
        {
            foreach (CheckBox c in flowLogTypes.Controls)
            {
                if (c == sender)
                    continue;

                if (((Log.LogTypes)c.Tag & (Log.LogTypes)((Control)sender).Tag) != Log.LogTypes.None)
                    c.Checked = ((CheckBox)sender).Checked;

            }
        }

        void GetValue()
        {
            Log.filter = Log.LogTypes.None;

            foreach (CheckBox c in flowLogTypes.Controls)
            {
                if (c.Checked)
                    Log.filter |= (Log.LogTypes)c.Tag;
            }
        }

        private void C_Click(object sender, EventArgs e)
        {

            SetValues(sender);

            GetValue();



        }

        private void panel1_DoubleClick(object sender, EventArgs e)
        {
            if (panel1.Height > 40)
                panel1.Height = 40;
            else
                panel1.Height = 400;
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (Convert.ToInt32(e.KeyChar) == 13)
            {
                textBox1.Clear();

                foreach(var i in Log.Items)
                {
                    var s = Newtonsoft.Json.JsonConvert.SerializeObject(i.Data);

                    if (s.Contains(textBox2.Text))
                        this.textBox1.AppendText(string.Concat(i.DateTime.ToString("HH:mm:ss.fff"), "\t", i.Type, "\t", s, Environment.NewLine));
                    
                }
            }
        }
    }
}
