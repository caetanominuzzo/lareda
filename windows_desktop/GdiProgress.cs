using library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace windows_desktop
{
    class GdiProgress : Control
    {
        int[] Values;

        int[] Cursors;

        Label percent = null;

        public GdiProgress()
        {
            this.SetStyle(ControlStyles.DoubleBuffer, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.UserPaint, true);

            

        }

        public void Refresh(int[] values, int[] cursors)
        {
            if(null != values)
                Values = values;

            if (null == Values)
            {
                Values = new int[] { 0, 10, 30, 50, 70, 71, 95, 100 };

                Cursors = new int[] { 2, 3 };

                //  this.percent.Text = (Values.Max() / Values.Length).ToString() + "%";
            }

            Cursors = cursors;

            if(null == percent)
            {
                percent = new Label();

                this.Controls.Add(percent);

                percent.Dock = DockStyle.Fill;

                percent.BackColor = System.Drawing.Color.Transparent;

                percent.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            }

            if(cursors.Any( x=> x > 180))
            {

            }

            percent.Text = ((double)Values.Length*100 / Values.Max()).ToString("N0") + "%";

            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            int[] v = null;


            if (null == Values)
            {
                Values = new int[] { 0, 10, 30, 50, 70, 71, 95, 100 };

                Cursors = new int[] { 2, 3, 100 };

                //  this.percent.Text = (Values.Max() / Values.Length).ToString() + "%";
            }


            if (Values[0] == -1)
                return;

            lock (Values)
                v = Values;

            var c = pe.Graphics;

            var w = this.ClientSize.Width;

            var h = this.ClientSize.Height;

            var l = v.Max() + 1;

            var r = (double)w / (double)l;

            if(l > 100)
            {

            }

            for (var i = 0; i < v.Length; i ++)
            {
                c.FillRectangle(System.Drawing.Brushes.Green, new System.Drawing.Rectangle(Convert.ToInt32((v[i]-1) * r), 0, Convert.ToInt32(1 * r) + 1, h));
            }

            for (var i = 0; i < Cursors.Length; i++)
            {
                c.FillRectangle(System.Drawing.Brushes.Red, new System.Drawing.Rectangle(Convert.ToInt32((Cursors[i]) * r), 0, Convert.ToInt32(1 * r) + 1, h));

               
            }

         //   c.FillRectangle(System.Drawing.Brushes.White, new System.Drawing.Rectangle(240, 0, 7, h));
        }
    }
}
