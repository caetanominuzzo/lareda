using library;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using windows_desktop.Properties;
using System.Runtime.InteropServices;

namespace windows_desktop
{
    public partial class fmDownloads : Form
    {
        static fmDownloads form;

        public static void Start()
        {
            if (form == null)
                form = new fmDownloads();

            form.BeginInvoke(new Action(() =>
            {
                    form.QuickShow();

                    Client.OnFileDownload += Client_OnFileDownload;

                    WebServer.OnFileWrite += WebServer_OnFileWrite;
            }));

            
        }

        private static void WebServer_OnFileWrite(string filename, int[] cursors)
        {
            form.BeginInvoke(new Action(() =>
            {
                foreach (var control in form.lst.Controls)
                {
                    if (((ctDownload)control).Filename == filename)
                    {
                        ((ctDownload)control).RefresFile(null, cursors);

                        break;
                    }
                }
            }));


        }

        private static void Client_OnFileDownload(byte[] address, string filename, string speficFilena, int[] arrives, int[] cursors)
        {
            var queue = p2pFile.Queue.queue;

            form.BeginInvoke(new Action(() =>
            {
                var found = false;

                foreach (var control in form.lst.Controls)
                {
                    if (((ctDownload)control).Filename == filename)
                    {
                        ((ctDownload)control).RefresFile(arrives, cursors);

                        found = true;

                        break;
                    }
                }

                if (found)
                    return;

                var c = new ctDownload(filename, arrives, cursors);

                c.Dock = DockStyle.Top;

                form.lst.Controls.Add(c);

                c.BringToFront();

                c.Width = form.Width;

            }));
           


           
        }

        void SlowHide()
        {
            timerHide.Start();
        }

        void QuickShow()
        {
            timerHide.Start();
            
         //   if (Location.Equals(new Point(0, 0)))
            {
                var point = MousePosition;

                point.Offset(-this.Width + 1, -this.Height+1);

                Location = point;
            }

            form.Show();
        }

        fmDownloads()
        {
            InitializeComponent();

            Rectangle rect = Screen.GetWorkingArea(this);

            Location = new Point(0, 0);

        }

        void fmDrag_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = (e.Data.GetFormats().Any(f => f == DataFormats.FileDrop)
       ? DragDropEffects.Copy
       : DragDropEffects.None);

                QuickShow();

            }
        }

        void fmDrag_DragDrop(object sender, DragEventArgs e)
        {
            Hide();

            Point location = Location;

            Location = new Point(0, 0);

            var dropFiles = (string[])e.Data.GetData(DataFormats.FileDrop);

        }

        private void timerHide_Tick(object sender, EventArgs e)
        {
            Point pos = Control.MousePosition;

            bool inForm = pos.X >= Left && pos.Y >= Top && pos.X < Right && pos.Y < Bottom;

            if (!inForm)
            {
               // Hide();

               // timerHide.Stop();

               // Location = new Point(0, 0);

               // return;
            }


            

            //lst.Controls.Clear();


        }

        private void fmDownloads_MouseLeave(object sender, EventArgs e)
        {
       //     Hide();
        }

        private void picClose_Click(object sender, EventArgs e)
        {
            Hide();
        }
    }
}
