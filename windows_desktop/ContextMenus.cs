using library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace windows_desktop
{
    class ContextMenus
    {
        public ContextMenuStrip Create()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem item;
            ToolStripSeparator sep;

            // Print
            item = new ToolStripMenuItem();
            item.Text = "Print";
            item.Click += new System.EventHandler(Print_Click);
            menu.Items.Add(item);

            // Separator
            sep = new ToolStripSeparator();
            menu.Items.Add(sep);

            // Exit
            item = new ToolStripMenuItem();
            item.Text = "Exit";
            item.Click += new System.EventHandler(Exit_Click);
            menu.Items.Add(item);

            return menu;
        }

        void Exit_Click(object sender, EventArgs e)
        {
            Client.Close();

            Application.Exit();
        }

        void Print_Click(object sender, EventArgs e)
        {
            Print p = new Print();

            p.Show();
        }
    }
}
