using GraphVizWrapper;
using GraphVizWrapper.Commands;
using GraphVizWrapper.Queries;
using library;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace windows_desktop
{
    public partial class Print : Form
    {
        static string lastPrint = string.Empty;

        static GraphGeneration wrapper;

        public Print()
        {
            InitializeComponent();

            var getStartProcessQuery = new GetStartProcessQuery();
            var getProcessStartInfoQuery = new GetProcessStartInfoQuery();
            var registerLayoutPluginCommand = new RegisterLayoutPluginCommand(getProcessStartInfoQuery, getStartProcessQuery);

            wrapper = new GraphGeneration(getStartProcessQuery,
                                  getProcessStartInfoQuery,
                                  registerLayoutPluginCommand);
        }

        private void Print_Load(object sender, EventArgs e)
        {
            if (!File.Exists("prints.txt"))
                File.CreateText("prints.txt").Close();

            var print = File.ReadAllText("prints.txt");

            var prints = print.Split('|');

            foreach(var p in prints)
            {
                var pp = p.Split('*');

                if(pp.Length == 2)

                listView1.Items.Add(pp[1], pp[0], 0);
            }

            lastPrint = Client.Print();
            
            textBox1.Text = DateTime.Now.ToString("dd HH:mm");

            Genarate(lastPrint);

            this.Close();
        }

        static Process process = null;

        public static void Genarate(string print)
        {
            var graph = "digraph{compound=true;" + print + "}";

            byte[] output = wrapper.GenerateGraph(graph, Enums.GraphReturnType.Svg);

            if (output != null && output.Length > 0)
            {
                var file = "demo.svg";

                var txtFile = "demo.gv";

                var path = @"..\\..\\..\\home\\jquery.graphviz.svg-master\\";

                path = Path.GetFullPath( path);

                if (!Directory.Exists(Path.GetDirectoryName(path)))
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                File.WriteAllBytes(path + file, output);
                    
                File.WriteAllText(path + txtFile, graph);

                if (process != null)
                    process.Close();

                process = System.Diagnostics.Process.Start("http://localhost:46005/k5c0241K9ckEw3ruLh2ZTUppkFtTurVikZJTuMn8UX8_/jquery.graphviz.svg-master/demo.html");

                //var psi = new ProcessStartInfo();

                //psi.FileName = @"chrome image.png";

                //var process = new Process();

                //process.StartInfo = psi;

                //process.Start();

                //pictureBox1.Image = Image.FromStream(new MemoryStream(output));
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            lastPrint = Client.Print();

            textBox1.Text = DateTime.Now.ToString("dd HH:mm");

            Genarate(lastPrint);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            File.AppendAllText("prints.txt", "|" + textBox1.Text + "*" + lastPrint + '\n');

            listView1.Items.Add(lastPrint, textBox1.Text, 0);
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            if(listView1.SelectedItems.Count == 1)
                lastPrint = listView1.SelectedItems[0].Name;
            else
            {
                var prints = new List<string>();

                int width = 0;

                var colors = new string[] { "black", "red", "blue", "green" };

                foreach (var i in listView1.SelectedItems)
                {
                    var ii = ((ListViewItem)i);

                    var ss = ii.Name.Split('\n');

                    foreach(var s in ss)
                    {
                        var sss = s.Split(';');

                        var ssss = sss[0];

                        if (!prints.Any(x => x.StartsWith(ssss)))
                            prints.Add(s.Replace(";\r", "[color=" + colors[width] + "];"));
                    }

                    width++;
                }

                lastPrint = string.Join(Environment.NewLine, prints);
            }



            Genarate(lastPrint);
        }

        private void listView1_KeyPress(object sender, KeyPressEventArgs e)
        {
        }

        private void listView1_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Delete)
            {
                if (listView1.SelectedItems.Count == 0)
                    return;

                listView1.Items.Remove(listView1.SelectedItems[0]);

                var s = string.Empty;

                foreach(var i in listView1.Items)
                {
                    var ii =((ListViewItem)i);

                    s += ii.Text + "*" + ii.Name + "|";
                }

                File.WriteAllText("prints.txt", s);
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            var s = lastPrint;

            lastPrint = textBox2.Text + lastPrint;

            Genarate(lastPrint);

            lastPrint = s;
        }


    }
}
