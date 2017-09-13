using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using library;
using System.IO;

namespace windows_desktop
{
    public partial class ctDownload : UserControl
    {
        float blocks_count = 114f;

        public ctDownload()
        {
            InitializeComponent();

            lblFilename.Text = "temP";

            for (var i = 0; i < blocks_count; i++)
            {
                var bp = new Button();

                bp.Text = string.Empty;

                bp.Width = this.ClientSize.Width / 40;

                bp.Height = bp.Width;

                bp.BackColor = Color.FromArgb(11, 11, 11);

                bp.Margin = new Padding(0, 0, 1, 1);

                bp.FlatStyle = FlatStyle.Flat;

                bp.FlatAppearance.BorderSize = 0;

                packetsFlow.Controls.Add(bp);
            }

            var arrives = new int[] { 1, 2,3,4,5,6,7,8,9,10,11,12,13, 40,41,42,43,44,45,46,47,48,49,50,51,52, 100 };

            var cursors = new int[] { 0, 49, 98 };

            RefresFile(arrives, cursors);
        }

        public string Filename;

        public ctDownload(string filename, int[] arrives, int[] cursors)
        {
            InitializeComponent();

            Filename = filename;

            lblFilename.Text = Path.GetFileName(Filename);
        }

        public void RefresFile(int[] arrives, int[] cursors)
        {
            if (packetsFlow.Controls.Count == 0)
            {
                for (var i = 0; i < blocks_count; i++)
                {
                    var bp = new Button();

                    bp.Text = string.Empty;

                    bp.Width = this.ClientSize.Width / 40;

                    bp.Height = bp.Width;

                    bp.BackColor = Color.FromArgb(11, 11, 11);

                    bp.Margin = new Padding(0, 0, 1, 1);

                    bp.FlatStyle = FlatStyle.Flat;

                    bp.FlatAppearance.BorderSize = 0;

                    packetsFlow.Controls.Add(bp);
                }
            }

            

            if (null != arrives)
            {
              //  return;

                foreach (Button c in packetsFlow.Controls)
                    c.BackColor = Color.FromArgb(33, 33, 33);

                var max = arrives.Max();

                var ratio = blocks_count / max;

                PaintIt(arrives, blocks_count, ratio, Color.FromArgb(102, 102, 102));

               // max = cursors.Max();

                ratio = (blocks_count) / max;

                PaintIt(cursors, blocks_count, ratio, Color.Red, true);
            }

            //this.progress.Refresh(arrives, cursors);
        }

        private void PaintIt(int[] packets, float last, float ratio, Color color, bool onlyLastPacket = false)
        {
            foreach (var index in packets)
            {
                var i = index;

                if (!onlyLastPacket)
                    i--;

                var k = i + 1;

                var j = Convert.ToInt32(i * ratio);

                Log.Add(Log.LogTypes.Stream, Log.LogOperations.Paint, new { color, i, j, last, ratio, packets, Filename });

                try
                {
                    if (!onlyLastPacket)
                    {
                        packetsFlow.Controls[j].BackColor = color;

                        while (j++ < Convert.ToInt32(k * ratio) && j < last)
                            packetsFlow.Controls[j].BackColor = color;
                    }
                    else
                    {
                        packetsFlow.Controls[j].BackColor = color;

                        while (j++ < Convert.ToInt32(k * ratio) && j < last) 
                        //if (j < packetsFlow.Controls.Count)
                            packetsFlow.Controls[j].BackColor = color;

                        while(j-- > 0 && (packetsFlow.Controls[j].BackColor == Color.FromArgb(102, 102, 102) || packetsFlow.Controls[j].BackColor == Color.Red))
                            packetsFlow.Controls[j].BackColor = color;
                    }
                }
                catch (Exception e)
                {

                }
            }
        }
    }
}
