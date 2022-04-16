using System.ComponentModel;
using Xylab.Management.Models;

namespace Xylab.Management.Services
{
    public class PosixUtilities : ISystemUtilities
    {
        public UserInformation FindUser(uint uid)
        {
            var passwd = Interop.Libc.getpwuid(uid);
            return passwd.HasValue ? UserInformation.From(passwd.Value) : null;
        }

        public UserInformation FindUser(string name)
        {
            var passwd = Interop.Libc.getpwnam(name);
            return passwd.HasValue ? UserInformation.From(passwd.Value) : null;
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
    }
}
