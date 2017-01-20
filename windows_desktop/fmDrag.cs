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
    public partial class fmDrag : Form
    {
        static fmDrag form;

        static bool dragOnMe;

        static string DragId;

        static string UserAddress;

        public static void SetDrag(bool dragging, string dragId = null, string userAddress = null)
        {
            if (form == null)
                form = new fmDrag();

            DragId = dragId;

            UserAddress = userAddress;

            form.BeginInvoke(new Action(() =>
            {
                if (dragging)
                {
                    form.QuickShow();
                }

                else if (!dragOnMe)
                    form.Hide();
            }));
        }

        void SlowHide()
        {
            timerHide.Start();
        }

        void QuickShow()
        {
            timerHide.Stop();
            
            if (Location.Equals(new Point(0, 0)))
            {
                var point = MousePosition;

                point.Offset(10, 10);

                Location = point;
            }

            form.Show();
        }

        fmDrag()
        {
            InitializeComponent();

            Rectangle rect = Screen.GetWorkingArea(this);

            Location = new Point(0, 0);

            DragEnter += fmDrag_DragEnter;

            DragDrop += fmDrag_DragDrop;

            DragLeave += fmDrag_DragLeave;
        }

        void fmDrag_DragLeave(object sender, EventArgs e)
        {
            dragOnMe = false;

            SlowHide();
        }

        void fmDrag_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = (e.Data.GetFormats().Any(f => f == DataFormats.FileDrop)
       ? DragDropEffects.Copy
       : DragDropEffects.None);

                dragOnMe = true;

                QuickShow();

            }
        }

        void fmDrag_DragDrop(object sender, DragEventArgs e)
        {
            Hide();

            Point location = Location;

            Location = new Point(0, 0);

            dragOnMe = false;

            var dropFiles = (string[])e.Data.GetData(DataFormats.FileDrop);

            fmLoad.Show(dropFiles, DragId, UserAddress, location);
        }

        private void timerHide_Tick(object sender, EventArgs e)
        {
            Hide();

            timerHide.Stop();

            Location = new Point(0, 0);
        }
    }
}
