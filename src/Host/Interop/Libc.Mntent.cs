using System;
using System.Runtime.InteropServices;

namespace JetHub.Interop
{
    public partial class Libc
    {
        public struct mntent_t
        {
            public string mnt_fsname;   /* Device or server for filesystem.  */
            public string mnt_dir;      /* Directory mounted on.  */
            public string mnt_type;     /* Type of filesystem: ufs, nfs, etc.  */
            public string mnt_opts;     /* Comma-separated options for fs.  */
            public int mnt_freq;        /* Dump frequency (in days).  */
            public int mnt_passno;      /* Pass number for `fsck'.  */
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct mntent_rt
        {
            public IntPtr mnt_fsname;
            public IntPtr mnt_dir;
            public IntPtr mnt_type;
            public IntPtr mnt_opts;
            public int mnt_freq;
            public int mnt_passno;
        }

        [DllImport(libc_so_6)]
        public static extern int endmntent(IntPtr stream);

        [DllImport(libc_so_6, CharSet = CharSet.Ansi)]
        public static extern IntPtr setmntent(string file, string mode);

        [DllImport(libc_so_6)]
        private static extern unsafe IntPtr getmntent_r(IntPtr stream, out mntent_rt result, byte* buffer, int bufsize);

        public static unsafe mntent_t? getmntent(IntPtr stream)
        {
            const int bufsiz = 1024;
            byte* buf = stackalloc byte[bufsiz];

            if (getmntent_r(stream, out mntent_rt mnt, buf, bufsiz) != IntPtr.Zero)
            {
                return new mntent_t
                {
                    mnt_passno = mnt.mnt_passno,
                    mnt_freq = mnt.mnt_freq,
                    mnt_fsname = Marshal.PtrToStringAnsi(mnt.mnt_fsname),
                    mnt_type = Marshal.PtrToStringAnsi(mnt.mnt_type),
                    mnt_dir = Marshal.PtrToStringAnsi(mnt.mnt_dir),
                    mnt_opts = Marshal.PtrToStringAnsi(mnt.mnt_opts),
                };
            }
            else
            {
                return null;
            }
        }
    }
}
