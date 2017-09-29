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

            // Downloads
            item = new ToolStripMenuItem();
            item.Text = "Downloads";
            item.Click += new System.EventHandler(Downloads_Click);
            menu.Items.Add(item);

            // Separator
            sep = new ToolStripSeparator();
            menu.Items.Add(sep);

            // Print
            item = new ToolStripMenuItem();
            item.Text = "Print";
            item.Click += new System.EventHandler(Print_Click);
            menu.Items.Add(item);

            // Separator
            sep = new ToolStripSeparator();
            menu.Items.Add(sep);

            // Log
            item = new ToolStripMenuItem();
            item.Text = "Log";
            item.Click += new System.EventHandler(Log_Click);
            menu.Items.Add(item);

#if DEBUG
            // Separator
            sep = new ToolStripSeparator();
            menu.Items.Add(sep);

            item = new ToolStripMenuItem();
            item.Text = "Debug";
            menu.Items.Add(item);

            var methods = typeof(Client).GetMethods();

            foreach(var m in methods)
            {
                if (m.GetParameters().Length > 1)
                    continue;

               var item2 = new ToolStripMenuItem();

                item2.Text = m.Name;

                item2.Click += new System.EventHandler(Debug_Click);

                item.DropDownItems.Add(item2);

            }

           
#endif


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

        void Downloads_Click(object sender, EventArgs e)
        {
            fmDownloads.Start();
        }

        void Debug_Click(object sender, EventArgs e)
        {
            var text = ((ToolStripMenuItem)sender).Text;

            System.Diagnostics.Process.Start("http://localhost:" + Program.WebPort + "/" + pParameters.webHome + "/debug:" + text);
        } 
        
        void Log_Click(object sender, EventArgs e)
        {
            fmLog p = new fmLog();

            p.Show();
        }
    }
}
