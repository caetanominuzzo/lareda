using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace control_panel
{
    public class TabControl2 : TabControl
    {
        public override Color BackColor
        {
            get
            {
                return SystemColors.Window;
            }

            set
            {
                base.BackColor = value;
            }
        }
    }
}
