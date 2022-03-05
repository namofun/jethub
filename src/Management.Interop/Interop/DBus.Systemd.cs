using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Tmds.DBus;

// dotnet tool install Tmds.DBus.Tool -g
// dotnet dbus codegen --bus system --service org.freedesktop.systemd1 --interface org.freedesktop.systemd1.Manager
namespace Xylab.Management.Interop.DBus
{
    internal static class Systemd
    {
        public const string Service = "org.freedesktop.systemd1";
        public const string RootPath = "/org/freedesktop/systemd1";
    }

    internal static class LoadState
    {
        public const string Stub = "stub";
        public const string Loaded = "loaded";
        public const string NotFound = "not-found";
        public const string Error = "error";
        public const string Merged = "merged";
        public const string Masked = "masked";
    }

    internal static class ActiveState
    {
        public const string Active = "active";
        public const string Reloading = "reloading";
        public const string Inactive = "inactive";
        public const string Failed = "failed";
        public const string Activating = "activating";
        public const string Deactivating = "deactivating";
    }

    internal static class UnitFileState
    {
        public const string Enabled = "enabled";
        public const string EnabledRuntime = "enabled-runtime";
        public const string Masked = "masked";
        public const string MaskedRuntime = "masked-runtime";
        public const string Static = "static";
        public const string Disabled = "disabled";
        public const string Invalid = "invalid";
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class Unit
    {
        public string UnitName;
        public string Description;
        public string LoadState;
        public string ActiveState;
        public string SubState;
        public string FollowUnit;
        public ObjectPath UnitObjectPath;
        public uint JobId;
        public string JobType;
        public ObjectPath JobObjectPath;
    }

    [DBusInterface("org.freedesktop.systemd1.Manager")]
    internal interface IManager : IDBusObject
    {
        Task<Unit[]> ListUnitsAsync();
        Task<Unit[]> ListUnitsFilteredAsync(string[] states);
        Task<Unit[]> ListUnitsByPatternsAsync(string[] states, string[] patterns);
        Task<Unit[]> ListUnitsByNamesAsync(string[] units);
    }
}