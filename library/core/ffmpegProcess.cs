using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Threading;

namespace library
{

    internal class ffmpegProcess
    {
        static ManualResetEvent finish = new ManualResetEvent(false);

        static string log = string.Empty;

        internal static void ExecuteAsync(string arguments)
        {
            var process = new Process();

            try
            {
                log = string.Empty;

                ProcessStartInfo info = new ProcessStartInfo(ConfigurationManager.AppSettings["ffmpeg:ExeLocation"],
                    arguments);

                info.CreateNoWindow = false;
                info.UseShellExecute = false;
                info.RedirectStandardError = true;
                info.RedirectStandardOutput = true;
               

                process.StartInfo = info;

                process.EnableRaisingEvents = true;

                process.ErrorDataReceived += new DataReceivedEventHandler(process_ErrorDataReceived);
                process.Exited += new EventHandler(process_Exited);

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                finish.Reset();

                finish.WaitOne();
            }
            finally
            {
                if (process != null) process.Dispose();
            }
        }

        static void process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            log += e.Data + Environment.NewLine;
        }

        static void process_Exited(object sender, EventArgs e)
        {
            finish.Set();
        }
    }

}
