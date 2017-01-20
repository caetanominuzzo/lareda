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
    public partial class fmLoad : Form
    {
        string DragId;

        string UserAddress;

        public static void Show(string[] files, string dragId, string UserAddress, Point location)
        {
            fmLoad form = new fmLoad();
            //using (fmLoad form = new fmLoad())
            {
                form.Text = string.Join(", ", files);

                form.DragId = dragId;

                form.UserAddress = UserAddress;

                form.Show();

                form.Location = location;

                form.Process(files);
            }
        }

        List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();

        public fmLoad()
        {
            InitializeComponent();

            Rectangle rect = Screen.GetWorkingArea(this);
        }

        string[] discovered = null;

        int tDiscovered = 0;

        int progress;

        void Process(string[] dropFiles)
        {
            var user = Utils.AddressFromBase64String(UserAddress);

            ThreadPool.QueueUserWorkItem(ThreadDiscover, dropFiles);

            Client.OnFileUpload += Client_OnFileUpload;

            Client.Upload(dropFiles, user);

           // Client.OnFileUpload -= Client_OnFileUpload;
        }

        void ThreadDiscover(object o)
        {
            discovered = Discover((string[])o);

            tDiscovered = discovered.Length;
        }

        void Client_OnFileUpload(string filename, string base64Address)
        {
            if (discovered != null && discovered.Any(x => filename.Equals(x, StringComparison.OrdinalIgnoreCase)))
            {
                progress++;

                SetProgress(((100.0 / tDiscovered) * progress));

                Application.DoEvents();
            }
        }

        delegate void SetProgressCallback(double value);

        private void SetProgress(double value)
        {
            if (this.lblProgress.InvokeRequired)
            {
                SetProgressCallback d = new SetProgressCallback(SetProgress);

                this.Invoke(d, new object[] { value });
            }
            else
            {
                this.lblProgress.Text = value.ToString("n0") + " %";

                if (value == 100)
                    Close();
            }
        }

        string[] Discover(string[] dropFiles)
        {
            var result = new List<string>();

            foreach (var dropFile in dropFiles)
            {
                if(!Directory.Exists(dropFile))
                    result.Add(dropFile);

                if (Directory.Exists(dropFile))
                {
                    //result.AddRange(Directory.GetDirectories(dropFile, "*", SearchOption.AllDirectories));

                    result.AddRange(Directory.GetFiles(dropFile, "*", SearchOption.AllDirectories));
                }
            }

            return result.ToArray();                 
        }


        

        private static Dictionary<int, string> WindowsFields;

        private static Dictionary<int, string> GetFileHeaders()
        {
            if (WindowsFields == null)
            {
                WindowsFields = new Dictionary<int, string>();

                WindowsFields.Add(0, Utils.ToBase64String(Encoding.Unicode.GetBytes((("Name")))));

                //WindowsFields.Add(12, Convert.ToBase64String(Encoding.Unicode.GetBytes((" Kind "))));
                //WindowsFields.Add(13, Convert.ToBase64String(Encoding.Unicode.GetBytes((" Date taken "))));
                //WindowsFields.Add(14, Convert.ToBase64String(Encoding.Unicode.GetBytes((" Contributing artists "))));
                //WindowsFields.Add(15, Convert.ToBase64String(Encoding.Unicode.GetBytes((" Album "))));
                //WindowsFields.Add(16, Convert.ToBase64String(Encoding.Unicode.GetBytes((" Year "))));
                //WindowsFields.Add(17, Convert.ToBase64String(Encoding.Unicode.GetBytes((" Genre "))));
                //WindowsFields.Add(18, Convert.ToBase64String(Encoding.Unicode.GetBytes((" Conductors "))));
                //WindowsFields.Add(19, Convert.ToBase64String(Encoding.Unicode.GetBytes((" Tags "))));
                //WindowsFields.Add(21, Convert.ToBase64String(Encoding.Unicode.GetBytes((" Authors "))));
                //WindowsFields.Add(22, Convert.ToBase64String(Encoding.Unicode.GetBytes((" Title "))));
                //WindowsFields.Add(23, Convert.ToBase64String(Encoding.Unicode.GetBytes((" Subject "))));
                //WindowsFields.Add(24, Convert.ToBase64String(Encoding.Unicode.GetBytes((" Categories "))));
                //WindowsFields.Add(25, Convert.ToBase64String(Encoding.Unicode.GetBytes((" Comments "))));
                //WindowsFields.Add(27, Convert.ToBase64String(Encoding.Unicode.GetBytes((" # "))));
                //WindowsFields.Add(28, Convert.ToBase64String(Encoding.Unicode.GetBytes((" Length "))));
                //WindowsFields.Add(29, Convert.ToBase64String(Encoding.Unicode.GetBytes((" Bit rate "))));
                //WindowsFields.Add(31, Convert.ToBase64String(Encoding.Unicode.GetBytes((" Camera model "))));
                //WindowsFields.Add(32, Convert.ToBase64String(Encoding.Unicode.GetBytes((" Dimensions "))));
                //WindowsFields.Add(33, Convert.ToBase64String(Encoding.Unicode.GetBytes((" Camera maker "))));
                //WindowsFields.Add(34, Convert.ToBase64String(Encoding.Unicode.GetBytes((" Company "))));
                //WindowsFields.Add(35, Convert.ToBase64String(Encoding.Unicode.GetBytes((" File description "))));
                //WindowsFields.Add(36, Convert.ToBase64String(Encoding.Unicode.GetBytes((" Program name "))));
                //WindowsFields.Add(37, Convert.ToBase64String(Encoding.Unicode.GetBytes((" Duration "))));
                //WindowsFields.Add(40, Convert.ToBase64String(Encoding.Unicode.GetBytes((" Location "))));
                //WindowsFields.Add(163, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Bit depth   "))));
                //WindowsFields.Add(164, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Horizontal resolution   "))));
                //WindowsFields.Add(165, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Width   "))));
                //WindowsFields.Add(166, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Vertical resolution   "))));
                //WindowsFields.Add(167, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Height   "))));
                //WindowsFields.Add(188, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Language   "))));
                //WindowsFields.Add(195, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Date released   "))));
                //WindowsFields.Add(196, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Encoded by   "))));
                //WindowsFields.Add(197, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Producers   "))));
                //WindowsFields.Add(198, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Publisher   "))));
                //WindowsFields.Add(201, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Writers   "))));
                //WindowsFields.Add(220, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Album artist   "))));
                //WindowsFields.Add(221, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Album ID   "))));
                //WindowsFields.Add(222, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Beats-per-minute   "))));
                //WindowsFields.Add(223, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Composers   "))));
                //WindowsFields.Add(224, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Initial key   "))));
                //WindowsFields.Add(225, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Part of a compilation   "))));
                //WindowsFields.Add(226, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Mood   "))));
                //WindowsFields.Add(227, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Part of set   "))));
                //WindowsFields.Add(228, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Period   "))));
                //WindowsFields.Add(229, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Color   "))));
                //WindowsFields.Add(230, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Parental rating   "))));
                //WindowsFields.Add(231, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Parental rating reason   "))));
                //WindowsFields.Add(235, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Exposure bias   "))));
                //WindowsFields.Add(236, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Exposure program   "))));
                //WindowsFields.Add(237, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Exposure time   "))));
                //WindowsFields.Add(238, Convert.ToBase64String(Encoding.Unicode.GetBytes(("F-stop   "))));
                //WindowsFields.Add(239, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Flash mode   "))));
                //WindowsFields.Add(240, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Focal length   "))));
                //WindowsFields.Add(241, Convert.ToBase64String(Encoding.Unicode.GetBytes(("35mm focal length   "))));
                //WindowsFields.Add(242, Convert.ToBase64String(Encoding.Unicode.GetBytes(("ISO speed   "))));
                //WindowsFields.Add(243, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Lens maker   "))));
                //WindowsFields.Add(244, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Lens model   "))));
                //WindowsFields.Add(245, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Light source   "))));
                //WindowsFields.Add(246, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Max aperture   "))));
                //WindowsFields.Add(247, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Metering mode   "))));
                //WindowsFields.Add(248, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Orientation   "))));
                //WindowsFields.Add(249, Convert.ToBase64String(Encoding.Unicode.GetBytes(("People   "))));
                //WindowsFields.Add(250, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Program mode   "))));
                //WindowsFields.Add(251, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Saturation   "))));
                //WindowsFields.Add(252, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Subject distance   "))));
                //WindowsFields.Add(253, Convert.ToBase64String(Encoding.Unicode.GetBytes(("White balance   "))));
                //WindowsFields.Add(254, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Priority   "))));
                //WindowsFields.Add(255, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Project   "))));
                //WindowsFields.Add(256, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Channel number   "))));
                //WindowsFields.Add(257, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Episode name   "))));
                //WindowsFields.Add(261, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Broadcast date   "))));
                //WindowsFields.Add(262, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Program description   "))));
                //WindowsFields.Add(263, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Recording time   "))));
                //WindowsFields.Add(266, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Summary   "))));
                //WindowsFields.Add(273, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Product name   "))));
                //WindowsFields.Add(274, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Product version   "))));
                //WindowsFields.Add(283, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Video compression   "))));
                //WindowsFields.Add(284, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Directors   "))));
                //WindowsFields.Add(286, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Frame height   "))));
                //WindowsFields.Add(287, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Frame rate   "))));
                //WindowsFields.Add(288, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Frame width   "))));
                //WindowsFields.Add(289, Convert.ToBase64String(Encoding.Unicode.GetBytes(("Total bitrate   "))));
            }


            return WindowsFields;
        }
    }
}

