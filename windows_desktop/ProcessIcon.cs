using library;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using windows_desktop.Properties;

namespace windows_desktop
{
    class ProcessIcon : IDisposable
    {
        NotifyIcon ni;

        string s;

        public static IDisposable Start()
        {
            return new ProcessIcon();
        }

        ProcessIcon()
        {
            ni = new NotifyIcon();
            
            Display();
        }

        public void Display()
        {
            ni.MouseClick += new MouseEventHandler(ni_MouseClick);

            ni.Text = pParameters.AppName +  Program.p2pEndpoint.ToString(); 

            ni.Icon = Resources.A;

            ni.Visible = true;

            ni.ContextMenuStrip = new ContextMenus().Create();
        }

        public void Dispose()
        {
            ni.Dispose();
        }

        void ni_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                System.Diagnostics.Process.Start("http://localhost:" + Program.WebPort + "/" + pParameters.webHome + "/");
        }
    }
}
