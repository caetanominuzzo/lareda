using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using windows_desktop;

namespace windows_desktop_installer
{
    class Program
    {

        static string getfilename(Assembly a)
        {
            string codeBase = a.CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            return Uri.UnescapeDataString(uri.Path);
        }

        static List<string> ff = new List<string>();

        static List<long> ss = new List<long>();

        static void add(string name)
        {
            var s = name + ".dll";

            if (File.Exists(s))
            {
                var i = File.ReadAllBytes(s);

                ff.Add(s);

                ss.Add(i.Length);

                bw.Write(i);
            }
        }

        static string outputFilename = "setup.bin";

        static BinaryWriter bw = new BinaryWriter(new FileStream(outputFilename, FileMode.Create));

        static void build_and_wait()
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\msbuild ../../../windows_desktop_installer_header/windows_desktop_installer_header.vcxproj /p:configuration=Release"
                }
            };
            process.Start();
            process.WaitForExit();
        }

        static void Main(string[] args)
        {
            build_and_wait();

            var original_size = new FileInfo("../../../windows_desktop_installer_header/Release/windows_desktop_installer_header.exe").Length;

            ff.Add("");

            ss.Add(original_size);

            var a = typeof(windows_desktop.fmDrag).Assembly;
            
            var s = getfilename(a);

            var i = File.ReadAllBytes(s);

            bw.Write(i);


            ff.Add(Path.GetFileName(s).ToLower());

            ss.Add(i.Length);


            foreach (AssemblyName r in a.GetReferencedAssemblies())
            {
                add(r.Name);
                
            }

            bw.Close();

            var files = string.Join("\", \"", ff);

            var sizes = string.Join(", ", ss);

            var output = string.Format(@"#define load_sizes int sizes [] = {{{0}}};{2}#define load_files char* files [] = {{""{1}""}};", sizes, files, Environment.NewLine);

            File.WriteAllText("../../../windows_desktop_installer_header/files.h", output);

            build_and_wait();

            bw = new BinaryWriter(new FileStream("setup.exe", FileMode.Create));

            var o = File.ReadAllBytes("../../../windows_desktop_installer_header/Release/windows_desktop_installer_header.exe");

            bw.Write(o);

            o = File.ReadAllBytes(outputFilename);

            bw.Write(o);

            bw.Close();

            //Process.Start("cmd.exe", "/k copy /b \"../../../windows_desktop_installer_header/Release/windows_desktop_installer_header.exe\"  setup.exe"); //+ setup.bin

        }
    }
}
