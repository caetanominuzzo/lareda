using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace windows_desktop
{
    public partial class UIHelper : Form
    {
        public UIHelper()
        {
            InitializeComponent();
        }

        static SaveFileDialog dialog = new SaveFileDialog();

        internal string SaveAs(string filename)
        {
            lock (dialog)
            {
                //if(InvokeRequired)
                    this.Invoke((MethodInvoker)delegate
                    {

                        dialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                        dialog.FilterIndex = 2;
                        dialog.RestoreDirectory = true;
                        dialog.FileName = filename;

                        if (dialog.ShowDialog() == DialogResult.OK)
                            filename = dialog.FileName;
                        else
                            filename = string.Empty;
                    });

                return filename;
            }
        }
    }
}
