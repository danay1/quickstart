// QuickStart — значки программ и ярлыков через оболочку Windows.
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace QuickStart
{
    static class IconUtil
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SHGetFileInfo(string path, uint attributes, ref SHFILEINFO info, uint infoSize, uint flags);

        [DllImport("user32.dll")]
        static extern bool DestroyIcon(IntPtr icon);

        const uint SHGFI_ICON = 0x100;
        const uint SHGFI_LARGEICON = 0x0;

        public static Image Get(AppEntry app)
        {
            try
            {
                string path = app.ExpandedPath;
                if (string.IsNullOrWhiteSpace(path)) return Fallback();
                if (!File.Exists(path) && !Directory.Exists(path)) return Fallback();

                var info = new SHFILEINFO();
                IntPtr ok = SHGetFileInfo(path, 0, ref info, (uint)Marshal.SizeOf(typeof(SHFILEINFO)), SHGFI_ICON | SHGFI_LARGEICON);
                if (ok == IntPtr.Zero || info.hIcon == IntPtr.Zero) return Fallback();
                try
                {
                    using (var icon = Icon.FromHandle(info.hIcon))
                        return icon.ToBitmap();
                }
                finally
                {
                    DestroyIcon(info.hIcon);
                }
            }
            catch { return Fallback(); }
        }

        static Image Fallback() => (Image)SystemIcons.Application.ToBitmap();
    }
}
