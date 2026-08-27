namespace Xylab.Management.Services;

using System.ComponentModel;
using System.Runtime.Versioning;
using Xylab.Management.Models;

[SupportedOSPlatform("linux")]
public class PosixUtilities : ISystemUtilities
{
    public UserInformation FindUser(uint uid)
    {
        var passwd = Interop.Libc.getpwuid(uid);
        return passwd.HasValue ? PasswdToUserInformation(passwd.Value) : null;
    }

    public UserInformation FindUser(string name)
    {
        var passwd = Interop.Libc.getpwnam(name);
        return passwd.HasValue ? PasswdToUserInformation(passwd.Value) : null;
    }

    public uint GetUserId()
    {
        return Interop.Libc.getuid();
    }

    public uint GetEffectiveUserId()
    {
        return Interop.Libc.geteuid();
    }

    public uint GetGroupId()
    {
        return Interop.Libc.getgid();
    }

    public uint GetEffectiveGroupId()
    {
        return Interop.Libc.getegid();
    }

    public void ChangeMode(string filePath, uint mode)
    {
        if (Interop.Libc.chmod(filePath, mode) == -1)
        {
            throw new Win32Exception();
        }
    }

    public bool TryChangeMode(string filePath, uint mode)
    {
        if (Interop.Libc.chmod(filePath, mode) == -1)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public void SetUmask(uint cmask, out uint originalCmask)
    {
        originalCmask = Interop.Libc.umask(cmask);
    }

    private static UserInformation PasswdToUserInformation(in Interop.Libc.passwd_t passwd)
    {
        return new UserInformation
        {
            Shell = passwd.pw_shell,
            Comment = passwd.pw_gecos,
            HomeDirectory = passwd.pw_dir,
            GroupId = passwd.pw_gid,
            UserId = passwd.pw_uid,
            UserName = passwd.pw_name,
        };
    }
}
