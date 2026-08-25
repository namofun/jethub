namespace Xylab.Management.Interop;

using System.Runtime.InteropServices;

internal partial class Libc
{
    [DllImport(libc_so_6)]
    public static unsafe extern uint umask(uint cmask);

    [DllImport(libc_so_6, CharSet = CharSet.Ansi)]
    public static unsafe extern int chmod(string path, uint mode);
}
