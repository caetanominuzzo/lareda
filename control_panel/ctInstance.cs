using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using CefSharp;
using CefSharp.WinForms;
using System.Threading;
using library;

namespace control_panel
{
  
    public partial class ctInstance : UserControl
    {
        public ChromiumWebBrowser chromeBrowser;

        public delegate void RemoveHandler(ctInstance instance);

        public event RemoveHandler OnRemoveHandler;

        public int Id = 0;

        public string ParentPeer = null;

        public string EndPoint = string.Empty;

        public string webPort = string.Empty;

        public byte[] Address;

        static string sourcePath = @"D:\lareda\windows_desktop\bin\Debug\";

        static string instancesDir = "Instances\\";

        static string[] subDirs = new string[] { "cache", "packets", "log", "json" };

        string basePath;

        public ctInstance() : this(0)
        {
            
            
        }

        BackgroundWorker newInstanceWorker = new BackgroundWorker();

        BackgroundWorker attachWorker = new BackgroundWorker();

        public ctInstance(int id, string parentPeer = null)
        {
            Id = id;

            ParentPeer = parentPeer;

            var port = Id.ToString();//.PadRight(, '0');

            EndPoint = "127.0.0.1:4" + port + "999";

            webPort = "3" + port + "999";


            basePath = instancesDir + Id.ToString();


            InitializeComponent();

            this.lblTitle.Text = EndPoint;


            attachWorker.DoWork += AttachWorker_DoWork;

            newInstanceWorker.DoWork += Worker_NewInstance;

            newInstanceWorker.RunWorkerAsync();

            var names = Enum.GetNames(typeof(Log.LogTypes));

            var values = Enum.GetValues(typeof(Log.LogTypes));

            for (var i = 0; i < names.Length; i++)
            {
                var name = names[i];

                var value = values.GetValue(i);

                CheckBox c = new CheckBox();

                c.Text = name;

                c.AutoSize = false;

                c.MinimumSize = new Size(flowLogItens.Width / 5, 28);

                c.Click += C_Click;

                c.Tag = value;

                c.Checked = ((Log.LogTypes)value & Log.typeFilter) != Log.LogTypes.None;

                flowLogItens.Controls.Add(c);
            }
        }

        void SetValues(object sender)
        {
            foreach (CheckBox c in flowLogItens.Controls)
            {
                if (c == sender)
                    continue;

                if (((Log.LogTypes)c.Tag & (Log.LogTypes)((Control)sender).Tag) != Log.LogTypes.None)
                    c.Checked = ((CheckBox)sender).Checked;

            }
        }

        void GetValue()
        {
            Log.typeFilter = Log.LogTypes.None;

            foreach (CheckBox c in flowLogItens.Controls)
            {
                if (c.Checked)
                    Log.typeFilter |= (Log.LogTypes)c.Tag;
            }
        }

        private void C_Click(object sender, EventArgs e)
        {

            SetValues(sender);

            GetValue();



        }



        private void Worker_NewInstance(object sender, DoWorkEventArgs e)
        {
            NewInstance();
        }

        public void NewInstance()
        {
           

            //if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);

                foreach (var dir in subDirs)
                    Directory.CreateDirectory(basePath + "\\" + dir);

                CreateLink();

                Copy();

                // webBrowser1.Url = new Uri("http://127.0.0.1:" + webPort + "/" + library.pParameters.webHome + "/");
            }

            Start();


            if (null != ParentPeer)
            {
                txtGetPeers.Invoke((MethodInvoker)(() => setGetPeers()));
            }

            InitializeChromium();
        }

        public void setGetPeers()
        {
            txtGetPeer.Text = ParentPeer;

            txtAddPeer_KeyPress(txtGetPeer, new KeyPressEventArgs('\r'));
        }

        public void InitializeChromium()
        {
            CefSettings settings = new CefSettings();
            // Initialize cef with the provided settings
            if(!Cef.IsInitialized)
                Cef.Initialize(settings);
            // Create a browser component
            chromeBrowser = new ChromiumWebBrowser("http://127.0.0.1:" + webPort + "/" + library.pParameters.webHome + "/");
            // Add it to the form and fill it to the form window.
            this.tabBrowser.Controls.Add(chromeBrowser);
            chromeBrowser.Dock = DockStyle.Fill;
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if(!process.HasExited)
                process.Kill();

            OnRemoveHandler?.Invoke(this);
        }

        void CreateLink()
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C mklink /d " + basePath + @"\cache\k5c0241K9ckEw3ruLh2ZTUppkFtTurVikZJTuMn8UX8_  ""..\..\..\..\..\..\home""";
            process.StartInfo = startInfo;
            process.Start();
        }

        void Copy()
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            process.StartInfo = startInfo;

            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";

            startInfo.Arguments = "/c copy \"" + sourcePath +"*.exe\" \".\\" + basePath + "\"";
            process.Start();
            process.WaitForExit();

            startInfo.Arguments = "/c copy \"" + sourcePath + "*.dll\" \".\\" + basePath + "\"";
            process.Start();
            process.WaitForExit();

            startInfo.Arguments = "/c copy \"" + sourcePath + "*.config\" \".\\" + basePath + "\"";

            process.Start();

            process.WaitForExit();

            var xml = File.ReadAllText(basePath + "\\la-red.exe.config");

            var r = new Regex("(\\<add\\ key=\"p2pEndpoint\"\\ value=\")([^\"]*?)(\"/\\>)");

            xml = r.Replace(xml, "<add key=\"p2pEndpoint\" value=\""+EndPoint+"\"/>");

            r = new Regex("(\\<add\\ key=\"webPort\"\\ value=\")([^\"]*?)(\"/\\>)");

            xml = r.Replace(xml, "<add key=\"webPort\" value=\"" + webPort + "\"/>");

            Address = new byte[library.pParameters.addressSize];

            for(var i = 0; i< library.pParameters.addressSize; i++)
                Address[i] = (byte)((Id +1)* 10);

            Address[0] = 200;
            Address[1] = 200;
            Address[2] = 200;

            r = new Regex("(\\<add\\ key=\"p2pAddress\"\\ value=\")([^\"]*?)(\"/\\>)");

            xml = r.Replace(xml, "<add key=\"p2pAddress\" value=\"" + library.Utils.ToBase64String(Address) + "\"/>");

            File.WriteAllText(basePath + "\\la-red.exe.config", xml);

            var files = Directory.GetFiles(basePath + "\\packets");

            foreach (var f in files)
                File.Delete(f);

            files = Directory.GetFiles(basePath, "*.bin");

            foreach (var f in files)
                File.Delete(f);

            //Directory.Delete(basePath + "\\packets");

        }

        System.Diagnostics.Process process;

        void Start()
        {
            process = new System.Diagnostics.Process();

            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();

            process.StartInfo = startInfo;

            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;

            startInfo.FileName = basePath + "\\la-red.exe";

            startInfo.Arguments = string.Empty;

           

            process.Start();

          
        }

     

        private void tabBrowser_Enter(object sender, EventArgs e)
        {
            //webBrowser1.Url = new Uri("http://127.0.0.1:" + webPort + "/" + library.pParameters.webHome + "/");
        }

        private void tabPeers_Enter(object sender, EventArgs e)
        {
        //    webBrowser1.Url = new Uri("http://127.0.0.1:" + webPort + "/" + library.pParameters.webHome + "/debug:GetPeers");
        }

        WebBrowser webGetPeers;

        WebBrowser webGetWelcomeKey;


        private void timer1_Tick(object sender, EventArgs e)
        {
            if (null == webGetPeers)
            {
                //txtLog.LoadFile(basePath + "\\logs\\log.txt", RichTextBoxStreamType.PlainText);

                webGetPeers = new WebBrowser();

                webGetPeers.AllowNavigation = true;

                webGetPeers.DocumentCompleted += Web_DocumentGetPeersCompleted;
            }

            webGetPeers.Navigate(new Uri("http://127.0.0.1:" + webPort + "/" + library.pParameters.webHome + "/debug:GetPeers"));

            if (null == webGetWelcomeKey)
            {
                webGetWelcomeKey = new WebBrowser();

                webGetWelcomeKey.AllowNavigation = true;

                webGetWelcomeKey.DocumentCompleted += Web_DocumentGetWelcomeKeyCompleted;

                if(null != ParentPeer)
                    webGetWelcomeKey.Navigate(new Uri("http://127.0.0.1:" + webPort + "/" + library.pParameters.webHome + "/debug:GetPeer:" + ParentPeer));
            }
            else
                webGetWelcomeKey.Navigate(new Uri("http://127.0.0.1:" + webPort + "/" + library.pParameters.webHome + "/debug:GetWelcomeKey"));

            try
            {
                
                //var log = File.ReadAllText(basePath + "\\logs\\log.txt");

                //if (txtLog.Text != log)
                //    txtLog.Text = log;
            }
            catch { }
        }

        private void Web_DocumentGetWelcomeKeyCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            var s = ((WebBrowser)sender).DocumentText;

            if (s != txtGetPeer.Text)
                txtGetPeer.Text = s;
        }

        private void Web_DocumentGetPeersCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            var s = ((WebBrowser)sender).DocumentText;

            if (s != txtGetPeers.Text)
                txtGetPeers.Text = s;
        }

        internal void txtAddPeer_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                var web = new WebBrowser();
                web.AllowNavigation = true;
                web.Navigate(new Uri("http://127.0.0.1:" + webPort + "/" + library.pParameters.webHome + "/debug:GetPeer:" + txtGetPeer.Text));
            }
        }

        private void AttachWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            EnvDTE80.DTE2 dte2;
            dte2 = (EnvDTE80.DTE2)System.Runtime.InteropServices.Marshal.GetActiveObject("VisualStudio.DTE.15.0");
            foreach (EnvDTE.Process p in dte2.Debugger.LocalProcesses)
            {
                if (p.ProcessID == process.Id)
                {
                    if (chAttach.Checked)
                        p.Attach();
                    else
                        p.Detach();
                }
            }
        }

        private void chAttach_CheckedChanged(object sender, EventArgs e)
        {
            if (!attachWorker.IsBusy)
                attachWorker.RunWorkerAsync();
            else
            {

                chAttach.CheckedChanged -= chAttach_CheckedChanged;

                chAttach.Checked = !chAttach.Checked;

                chAttach.CheckedChanged += chAttach_CheckedChanged;
            }


        }

        private void btnLogClear_Click(object sender, EventArgs e)
        {
            File.WriteAllText(basePath +"\\logs\\log.txt", string.Empty);
        }

        private void tabMain_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(tabMain.SelectedTab == tabMetapackets)
            {
                WebBrowser web;

                web = new WebBrowser();

                web.AllowNavigation = true;



                web.DocumentCompleted += delegate (object sender2, WebBrowserDocumentCompletedEventArgs e2)
                {
                    txtMetapacktes.Text = ((WebBrowser)sender2).DocumentText;
                };

                web.Navigate(new Uri("http://127.0.0.1:" + webPort + "/" + library.pParameters.webHome + "/debug:GetMetaPackets"));
            }
        }
    }
}

