using System;
using System.IO;

namespace JetHub.Services
{
    public interface IGlobalInfo
    {
        string VfsRoot { get; }

        string SecretFile { get; }
    }

    public class FakeGlobalInfo : IGlobalInfo
    {
        public string VfsRoot { get; }

        public string SecretFile { get; }

        public FakeGlobalInfo()
        {
            var playground = Path.Combine(Environment.CurrentDirectory, "playground");
            if (!Directory.Exists(playground)) Directory.CreateDirectory(playground);
            VfsRoot = playground;
            SecretFile = Path.Combine(VfsRoot, "a.txt");
        }
    }

    public class OneShotGlobalInfo : IGlobalInfo
    {
        public string VfsRoot { get; } = "/opt/domjudge/judgehost/judgings";

        public string SecretFile { get; } = "/opt/domjudge/judgehost/etc/restapi.secret";
    }
}
