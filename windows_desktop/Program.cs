using library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.IO;
using System.Reflection;
using windows_desktop.Properties;
using System.Diagnostics;
using System.Security.Permissions;
using System.Security.Principal;
using System.Net;
using log4net;
using log4net.Repository.Hierarchy;
using log4net.Layout;
using log4net.Appender;
using log4net.Core;
using System.Text.RegularExpressions;

namespace windows_desktop
{
    public static class Program
    {
        public static int WebPort;

        public static IPEndPoint p2pEndpoint;

        public static byte[] p2pAddress;

        internal static UIHelper UIHelper;

         
        static void GenerateSimpleNames()
        {
            var s = string.Empty;

            for (var i = 1; i < 20000; i++)
                s += Utils.ToSimpleName(Utils.GetAddress()) + Environment.NewLine;

            File.WriteAllText("endereços.txt", s);

            return;
        }

        enum startArguments
        {
            none = 0,
            netsh = 1
        }

        static void log4netConfig()
        {
            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();

            PatternLayout patternLayout = new PatternLayout();
            patternLayout.ConversionPattern = "%date [%thread] %message%newline";
            patternLayout.ActivateOptions();

            FileAppender roller = new FileAppender();
            roller.AppendToFile = true;
            roller.File = @"logs\log.txt";
            roller.Layout = patternLayout;
            roller.ActivateOptions();
            hierarchy.Root.AddAppender(roller);

            //MemoryAppender memory = new MemoryAppender();
            //memory.ActivateOptions();
            //hierarchy.Root.AddAppender(memory);

            hierarchy.Root.Level = Level.Debug;

            hierarchy.Configured = true;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //GenerateSimpleNames()

            Utils.Base64ReplaceRegexPlus = new Regex(@"\+");

            Utils.Base64ReplaceRegexSlash = new Regex(@"/");

            Configure(); 

          
            //log4netConfig();
            log4net.Config.XmlConfigurator.Configure();

            Log.Add(Log.LogTypes.Application, Log.LogOperations.Start, AppDomain.CurrentDomain.BaseDirectory);


            if (Netsh(args))
                return;

#if !DEBUG
                       // if(Install())
                       //     return;
#endif

            

            

            Application.EnableVisualStyles();

            Application.SetCompatibleTextRenderingDefault(false);


            using (ProcessIcon.Start())
            using (Client.Start(p2pAddress, p2pEndpoint))
            using (WebServer.Start())
            using (UIHelper = new UIHelper())
            {
                WebServer.OnDragging += WebServer_OnDragging;

                fmDrag.SetDrag(false);

                //ThreadPool.QueueUserWorkItem(new WaitCallback(Client.BootStrap));
                Client.BootStrap(null);

                Client.StartThreads();

                //Client.GetInstaller();

                int i = 0;

                //while(true)
                //{
                //    Peer p = new Peer();
                //    p.Address = Utils.GetAddress();
                //    p.EndPoint = new System.Net.IPEndPoint(IPAddress.Loopback, Utils.GetAvaiablePort());
                //    Peers.AddPeer(p);
                //   // Thread.Sleep(1);
                //    i++;
                //    if(i > 1200)
                //        break;
                //}

                Application.Run();   
            }
        }

        public static int GetThreads(bool w = true)
        {
            var iA = 0;
            var iW = 0;

            ThreadPool.GetAvailableThreads(out iW, out iA);

            Log.Write(iW.ToString() + "\t" + iA.ToString());

            return w ? iW : iA;
        }

        public static void Configure()
        {
            if (Directory.GetCurrentDirectory() != AppDomain.CurrentDomain.BaseDirectory)
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            Log.Add(Log.LogTypes.Application, Log.LogOperations.Configure, AppDomain.CurrentDomain.BaseDirectory);

            pParameters.webCache = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, pParameters.webCache);

            if (!Directory.Exists(pParameters.webCache))
                Directory.CreateDirectory(pParameters.webCache);

            #region app.config

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            if (config.AppSettings.Settings["p2pEndpoint"] == null ||
                string.IsNullOrWhiteSpace(config.AppSettings.Settings["p2pEndpoint"].Value))
            {
                var port = Utils.GetAvaiablePort();

                p2pEndpoint = new IPEndPoint(IPAddress.Loopback, port);

                config.AppSettings.Settings.Add("p2pEndpoint", p2pEndpoint.ToString());
            }
            else
                p2pEndpoint = Addresses.CreateIPEndPoint(config.AppSettings.Settings["p2pEndpoint"].Value);

            if (config.AppSettings.Settings["p2pAddress"] == null ||
                string.IsNullOrWhiteSpace(config.AppSettings.Settings["p2pAddress"].Value))
            {
                p2pAddress = Utils.GetAddress();
                config.AppSettings.Settings.Remove("p2pAddress");

                config.AppSettings.Settings.Add("p2pAddress", Utils.ToBase64String(p2pAddress));
            }
            else
            {
                p2pAddress = Utils.AddressFromBase64String(config.AppSettings.Settings["p2pAddress"].Value);
            }

            if (config.AppSettings.Settings["webPort"] == null ||
                string.IsNullOrWhiteSpace(config.AppSettings.Settings["webPort"].Value))
            {
                WebPort = Utils.GetAvaiablePort();
                config.AppSettings.Settings.Add("webPort", WebPort.ToString());
            }
            else
            {
                WebPort = int.Parse(config.AppSettings.Settings["webPort"].Value);
            }

            config.Save(ConfigurationSaveMode.Modified);

            #endregion
        }

        static void WebServer_OnDragging(bool dragging, string dragId, string userAddress)
        {
            fmDrag.SetDrag(dragging, dragId, userAddress);
        }

        static bool Install()
        {
            Log.Add(Log.LogTypes.Application, Log.LogOperations.Install, AppDomain.CurrentDomain.BaseDirectory);

            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            appdata = Path.Combine(appdata, Application.CompanyName, Application.ProductName, Application.ProductVersion);

            var target = Path.Combine(appdata, pParameters.AppName + ".exe");

            if (AppDomain.CurrentDomain.BaseDirectory != appdata + "\\"
                && !AppDomain.CurrentDomain.FriendlyName.Contains("vshost")
                && !AppDomain.CurrentDomain.BaseDirectory.Contains("debug"))
            {
                try
                {
                   // RunAsAdministrator(AppDomain.CurrentDomain.FriendlyName);

                    Directory.CreateDirectory(appdata);

                    ReadTail(AppDomain.CurrentDomain.FriendlyName);

                    ProcessStartInfo p = new ProcessStartInfo(target);

                    p.WorkingDirectory = appdata;

                    System.Diagnostics.Process.Start(p);

                    CreateShortcut(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), pParameters.AppName),
                        target);

                    CreateShortcut(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), pParameters.AppName),
                        target);

                    return true;
                }
                catch (MissingTailException e)
                {
                    Log.Add(Log.LogTypes.Application, Log.LogOperations.Exception, AppDomain.CurrentDomain.BaseDirectory, e);

                    throw e;                         
                }
            }

            return false;
        }

        internal static int VerifyTailItem(byte[] data)
        {
            var dataSize = BitConverter.ToInt32(data, 0);

            var length = BitConverter.GetBytes(dataSize);

            var hash = Utils.ComputeHash(data.Take(4).ToArray(), 0, 4); //4 for int32

            if (Addresses.Equals(hash, data.Skip(4).Take(16).ToArray()))
                return dataSize;

            throw new MissingTailException();

            return -1;
        }

        static void ReadTail(string filename)
        {
            using (var f = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                var tailItemSize = 20; //int32 + hash256

                var tailTailSize = ReadTailItemSize(tailItemSize, f);

                var appTailSize = ReadTailItemSize(tailTailSize, f);

                var peersTailSize = ReadTailItemSize(0, f);

                var libraryTail = ReadTailItemSize(0, f);

                var GraphVizWrapperTail = ReadTailItemSize(0, f);

                var log4netTail = ReadTailItemSize(0, f);

                var newtonsoftTail = ReadTailItemSize(0, f);

                var taglibTail = ReadTailItemSize(0, f);

                f.Seek(0, SeekOrigin.Begin);

                WriteTailAssembly(f, appTailSize, pParameters.AppName + ".exe");

                WriteTailAssembly(f, libraryTail, "library.dll");

                WriteTailAssembly(f, newtonsoftTail, "Newtonsoft.Json.dll");

                WriteTailAssembly(f, taglibTail, "taglib-sharp.dll");



///#define load_files char* files [] = {"", "la-red.exe", "library.dll", "GraphVizWrapper.dll", "log4net.dll", "Newtonsoft.Json.dll", "NIdenticon.dll", "la-red.exe.config"};

            }
        }

        private static void WriteTailAssembly(FileStream f, int appTailSize, string filename)
        {
            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            appdata = Path.Combine(appdata, Application.CompanyName, Application.ProductName, Application.ProductVersion);

            filename = Path.Combine(appdata, filename);

            using (var target = new FileStream(filename, FileMode.Create))
            {
                var buffer = new byte[10 * 1024];

                var totalRead = 0;

                while (totalRead < appTailSize)
                {
                    var read = f.Read(buffer, 0, Math.Min(buffer.Length, appTailSize - totalRead));

                    totalRead += read;

                    target.Write(buffer, 0, read);
                }
            }
        }

        private static int ReadTailItemSize(int offset, FileStream f)
        {
            var tailItemSize = 20; //int32 + hash256

            if (offset != 0)
                f.Seek(-offset, SeekOrigin.End);

            var buffer = new byte[tailItemSize];

            if (f.Read(buffer, 0, tailItemSize) != tailItemSize)
                throw new MissingTailException();

            return VerifyTailItem(buffer);
        }

        private static void CreateShortcut(string shortcutLocation, string target)
        {
            using (StreamWriter writer = new StreamWriter(shortcutLocation + ".url"))
            {
                writer.WriteLine("[InternetShortcut]");
                writer.WriteLine("URL=file:///" + target);
                writer.WriteLine("IconIndex=0");
                string icon = target.Replace('\\', '/');
                writer.WriteLine("IconFile=" + target);
                writer.Flush();
            }
        }

        internal static bool IsElevated()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();

            WindowsPrincipal principal = new WindowsPrincipal(identity);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// Try to run an executable with elevated privilegies.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        internal static bool RunAsAdministrator(string path, string arguments = null)
        {

            var psi = new ProcessStartInfo();

            psi.FileName = path;

            if (!string.IsNullOrEmpty(arguments))
                psi.Arguments = arguments;

            psi.Verb = "runas";

            var process = new Process();

            process.StartInfo = psi;

            process.Start();

            process.WaitForExit();

            return true;
        }

        /// <summary>
        /// Requires elevated privilegies.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static bool Netsh(string[] args)
        {
            if (!args.Any())
                return false;

            if (args[0] != "NETSH")
                return false;

            Log.Add(Log.LogTypes.Application, Log.LogOperations.Configure, WebPort);

            Process p = new Process();

            ProcessStartInfo psi = new ProcessStartInfo("netsh", "http add urlacl url=http://+:" + WebPort + "/ user=" + Environment.UserDomainName + "\\" + Environment.UserName);

            p.StartInfo = psi;

            p.Start();

            p.WaitForExit();

            return true;
        }

        class MissingTailException : Exception
        {

        }
    }
}
