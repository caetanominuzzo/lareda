using CefSharp;
using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace control_panel
{
 
    public partial class Form1 : Form
    {

        string firstPeer = string.Empty;

        public Form1()
        {
            InitializeComponent();

            KillAll();
        }

        internal static void KillAll()
        {
            using (PowerShell power = PowerShell.Create())
            {
                power.AddScript("Stop-Process -processname la-red");

                power.Invoke();
            }
        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            var proposedId = 0;

            while (flowPanel.Controls.Cast<ctInstance>().Any(x => x.Id == proposedId))
                proposedId++;


            var c = new ctInstance(proposedId, proposedId > 0 ? ((ctInstance)flowPanel.Controls[0]).txtGetPeer.Text : null);

            flowPanel.Controls.Add(c);

            flowPanel.Controls.SetChildIndex(c, proposedId);

            c.OnRemoveHandler += C_OnRemoveHandler;

            Form1_ResizeEnd(this, null);
        }

        private void C_OnRemoveHandler(ctInstance instance)
        {
            flowPanel.Controls.Remove(instance);
        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            foreach (ctInstance i in flowPanel.Controls)
            {
                i.Width = (flowPanel.Width / flowPanel.Controls.Count) - 6;

                i.Height = flowPanel.Height;
            }

        }
    }
}
