using System;
using System.Runtime.InteropServices;

namespace Xylab.Management.Interop
{
    internal partial class Libc
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct passwd_r
        {
            public IntPtr pw_name;
            public IntPtr pw_passwd;
            public uint pw_uid;
            public uint pw_gid;
            public IntPtr pw_gecos;
            public IntPtr pw_dir;
            public IntPtr pw_shell;
        }

        public struct passwd_t
        {
            public string pw_name;
            public string pw_passwd;
            public uint pw_uid;
            public uint pw_gid;
            public string pw_gecos;
            public string pw_dir;
            public string pw_shell;
        }

        [DllImport(libc_so_6)]
        private static unsafe extern int getpwuid_r(uint uid, out passwd_r pwd, byte* buffer, ulong bufsize, out IntPtr result);

        public static unsafe passwd_t? getpwuid(uint uid)
        {
            byte* buffer = stackalloc byte[1024];
            int retVal = getpwuid_r(uid, out passwd_r pwd, buffer, 1024, out IntPtr ptr);
            if (retVal != 0 || ptr == IntPtr.Zero) return null;

            return new passwd_t
            {
                pw_name = Marshal.PtrToStringAnsi(pwd.pw_name),
                pw_passwd = Marshal.PtrToStringAnsi(pwd.pw_passwd),
                pw_uid = pwd.pw_uid,
                pw_gid = pwd.pw_gid,
                pw_gecos = Marshal.PtrToStringAnsi(pwd.pw_gecos),
                pw_dir = Marshal.PtrToStringAnsi(pwd.pw_dir),
                pw_shell = Marshal.PtrToStringAnsi(pwd.pw_shell),
            };
        }
    }
}
