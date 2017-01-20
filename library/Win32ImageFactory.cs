using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace library
{
    static class Win32ImageFactory
    {
        static Cache<Factories> factories = new Cache<Factories>(1000 * 60 * 60);

        internal static Stream ExtractThumbnail(string filePath)
        {
            Size size = new Size(1024, 636);

            SIIGBF flags = SIIGBF.SIIGBF_RESIZETOFIT;

            if (filePath == null)
                throw new ArgumentNullException("filePath");

            IShellItemImageFactory factory = null;

            var extension = Path.GetExtension(filePath).ToUpper();

            var result = factories.FirstOrDefault(x => x.CachedValue.Extension.Equals(extension));

            if (result != null)
                factory = result.CachedValue.Factory;

            int hr = 0;

            if (factory == null)
            {
                hr = SHCreateItemFromParsingName(filePath, IntPtr.Zero, typeof(IShellItemImageFactory).GUID, out factory);

                if (hr != 0)
                    return null;
            }

            IntPtr hbmp;

            hr = factory.GetImage(size, flags, out hbmp);

            if (hr != 0)
                return null;

            var bmp = Bitmap.FromHbitmap(hbmp);

            MemoryStream ms = new MemoryStream();

            bmp.Save(ms, ImageFormat.Jpeg);

            ms.Seek(0, 0);

            return ms;
        }

        [Flags]
        public enum SIIGBF
        {
            SIIGBF_RESIZETOFIT = 0x00000000,
            SIIGBF_BIGGERSIZEOK = 0x00000001,
            SIIGBF_MEMORYONLY = 0x00000002,
            SIIGBF_ICONONLY = 0x00000004,
            SIIGBF_THUMBNAILONLY = 0x00000008,
            SIIGBF_INCACHEONLY = 0x00000010,
            SIIGBF_CROPTOSQUARE = 0x00000020,
            SIIGBF_WIDETHUMBNAILS = 0x00000040,
            SIIGBF_ICONBACKGROUND = 0x00000080,
            SIIGBF_SCALEUP = 0x00000100,
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHCreateItemFromParsingName(string path, IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IShellItemImageFactory factory);

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
        private interface IShellItemImageFactory
        {
            [PreserveSig]
            int GetImage(Size size, SIIGBF flags, out IntPtr phbm);
        }

        class Factories
        {
            internal string Extension;

            internal IShellItemImageFactory Factory;
        }
    }
}
