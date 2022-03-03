using JetHub.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace JetHub.UnitTests
{
    [TestClass]
    public class DpkgStatus
    {
        [TestMethod]
        public void ParseDpkgStatus()
        {
            var list = LinuxSystem.ParseDpkgStatus(File.ReadAllLines("./Content/dpkg.status"));

            Assert.AreEqual(5, list.Count);
            Assert.AreEqual("coreutils", list[2].Name);
            Assert.AreEqual("all", list[1].Architect);
            Assert.AreEqual("amd64", list[0].Architect);
            Assert.AreEqual("2:3.3.16-1ubuntu2.3", list[3].Version);
            Assert.AreEqual("4:9.3.0-1ubuntu2", list[4].Version);
        }
    }
}