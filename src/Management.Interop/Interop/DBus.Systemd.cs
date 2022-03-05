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

    [Dictionary]
    class ManagerProperties
    {
        private string _Version = default (string);
        public string Version
        {
            get
            {
                return _Version;
            }

            set
            {
                _Version = (value);
            }
        }

        private string _Features = default (string);
        public string Features
        {
            get
            {
                return _Features;
            }

            set
            {
                _Features = (value);
            }
        }

        private string _Virtualization = default (string);
        public string Virtualization
        {
            get
            {
                return _Virtualization;
            }

            set
            {
                _Virtualization = (value);
            }
        }

        private string _Architecture = default (string);
        public string Architecture
        {
            get
            {
                return _Architecture;
            }

            set
            {
                _Architecture = (value);
            }
        }

        private string _Tainted = default (string);
        public string Tainted
        {
            get
            {
                return _Tainted;
            }

            set
            {
                _Tainted = (value);
            }
        }

        private ulong _FirmwareTimestamp = default (ulong);
        public ulong FirmwareTimestamp
        {
            get
            {
                return _FirmwareTimestamp;
            }

            set
            {
                _FirmwareTimestamp = (value);
            }
        }

        private ulong _FirmwareTimestampMonotonic = default (ulong);
        public ulong FirmwareTimestampMonotonic
        {
            get
            {
                return _FirmwareTimestampMonotonic;
            }

            set
            {
                _FirmwareTimestampMonotonic = (value);
            }
        }

        private ulong _LoaderTimestamp = default (ulong);
        public ulong LoaderTimestamp
        {
            get
            {
                return _LoaderTimestamp;
            }

            set
            {
                _LoaderTimestamp = (value);
            }
        }

        private ulong _LoaderTimestampMonotonic = default (ulong);
        public ulong LoaderTimestampMonotonic
        {
            get
            {
                return _LoaderTimestampMonotonic;
            }

            set
            {
                _LoaderTimestampMonotonic = (value);
            }
        }

        private ulong _KernelTimestamp = default (ulong);
        public ulong KernelTimestamp
        {
            get
            {
                return _KernelTimestamp;
            }

            set
            {
                _KernelTimestamp = (value);
            }
        }

        private ulong _KernelTimestampMonotonic = default (ulong);
        public ulong KernelTimestampMonotonic
        {
            get
            {
                return _KernelTimestampMonotonic;
            }

            set
            {
                _KernelTimestampMonotonic = (value);
            }
        }

        private ulong _InitRDTimestamp = default (ulong);
        public ulong InitRDTimestamp
        {
            get
            {
                return _InitRDTimestamp;
            }

            set
            {
                _InitRDTimestamp = (value);
            }
        }

        private ulong _InitRDTimestampMonotonic = default (ulong);
        public ulong InitRDTimestampMonotonic
        {
            get
            {
                return _InitRDTimestampMonotonic;
            }

            set
            {
                _InitRDTimestampMonotonic = (value);
            }
        }

        private ulong _UserspaceTimestamp = default (ulong);
        public ulong UserspaceTimestamp
        {
            get
            {
                return _UserspaceTimestamp;
            }

            set
            {
                _UserspaceTimestamp = (value);
            }
        }

        private ulong _UserspaceTimestampMonotonic = default (ulong);
        public ulong UserspaceTimestampMonotonic
        {
            get
            {
                return _UserspaceTimestampMonotonic;
            }

            set
            {
                _UserspaceTimestampMonotonic = (value);
            }
        }

        private ulong _FinishTimestamp = default (ulong);
        public ulong FinishTimestamp
        {
            get
            {
                return _FinishTimestamp;
            }

            set
            {
                _FinishTimestamp = (value);
            }
        }

        private ulong _FinishTimestampMonotonic = default (ulong);
        public ulong FinishTimestampMonotonic
        {
            get
            {
                return _FinishTimestampMonotonic;
            }

            set
            {
                _FinishTimestampMonotonic = (value);
            }
        }

        private ulong _SecurityStartTimestamp = default (ulong);
        public ulong SecurityStartTimestamp
        {
            get
            {
                return _SecurityStartTimestamp;
            }

            set
            {
                _SecurityStartTimestamp = (value);
            }
        }

        private ulong _SecurityStartTimestampMonotonic = default (ulong);
        public ulong SecurityStartTimestampMonotonic
        {
            get
            {
                return _SecurityStartTimestampMonotonic;
            }

            set
            {
                _SecurityStartTimestampMonotonic = (value);
            }
        }

        private ulong _SecurityFinishTimestamp = default (ulong);
        public ulong SecurityFinishTimestamp
        {
            get
            {
                return _SecurityFinishTimestamp;
            }

            set
            {
                _SecurityFinishTimestamp = (value);
            }
        }

        private ulong _SecurityFinishTimestampMonotonic = default (ulong);
        public ulong SecurityFinishTimestampMonotonic
        {
            get
            {
                return _SecurityFinishTimestampMonotonic;
            }

            set
            {
                _SecurityFinishTimestampMonotonic = (value);
            }
        }

        private ulong _GeneratorsStartTimestamp = default (ulong);
        public ulong GeneratorsStartTimestamp
        {
            get
            {
                return _GeneratorsStartTimestamp;
            }

            set
            {
                _GeneratorsStartTimestamp = (value);
            }
        }

        private ulong _GeneratorsStartTimestampMonotonic = default (ulong);
        public ulong GeneratorsStartTimestampMonotonic
        {
            get
            {
                return _GeneratorsStartTimestampMonotonic;
            }

            set
            {
                _GeneratorsStartTimestampMonotonic = (value);
            }
        }

        private ulong _GeneratorsFinishTimestamp = default (ulong);
        public ulong GeneratorsFinishTimestamp
        {
            get
            {
                return _GeneratorsFinishTimestamp;
            }

            set
            {
                _GeneratorsFinishTimestamp = (value);
            }
        }

        private ulong _GeneratorsFinishTimestampMonotonic = default (ulong);
        public ulong GeneratorsFinishTimestampMonotonic
        {
            get
            {
                return _GeneratorsFinishTimestampMonotonic;
            }

            set
            {
                _GeneratorsFinishTimestampMonotonic = (value);
            }
        }

        private ulong _UnitsLoadStartTimestamp = default (ulong);
        public ulong UnitsLoadStartTimestamp
        {
            get
            {
                return _UnitsLoadStartTimestamp;
            }

            set
            {
                _UnitsLoadStartTimestamp = (value);
            }
        }

        private ulong _UnitsLoadStartTimestampMonotonic = default (ulong);
        public ulong UnitsLoadStartTimestampMonotonic
        {
            get
            {
                return _UnitsLoadStartTimestampMonotonic;
            }

            set
            {
                _UnitsLoadStartTimestampMonotonic = (value);
            }
        }

        private ulong _UnitsLoadFinishTimestamp = default (ulong);
        public ulong UnitsLoadFinishTimestamp
        {
            get
            {
                return _UnitsLoadFinishTimestamp;
            }

            set
            {
                _UnitsLoadFinishTimestamp = (value);
            }
        }

        private ulong _UnitsLoadFinishTimestampMonotonic = default (ulong);
        public ulong UnitsLoadFinishTimestampMonotonic
        {
            get
            {
                return _UnitsLoadFinishTimestampMonotonic;
            }

            set
            {
                _UnitsLoadFinishTimestampMonotonic = (value);
            }
        }

        private string _LogLevel = default (string);
        public string LogLevel
        {
            get
            {
                return _LogLevel;
            }

            set
            {
                _LogLevel = (value);
            }
        }

        private string _LogTarget = default (string);
        public string LogTarget
        {
            get
            {
                return _LogTarget;
            }

            set
            {
                _LogTarget = (value);
            }
        }

        private uint _NNames = default (uint);
        public uint NNames
        {
            get
            {
                return _NNames;
            }

            set
            {
                _NNames = (value);
            }
        }

        private uint _NFailedUnits = default (uint);
        public uint NFailedUnits
        {
            get
            {
                return _NFailedUnits;
            }

            set
            {
                _NFailedUnits = (value);
            }
        }

        private uint _NJobs = default (uint);
        public uint NJobs
        {
            get
            {
                return _NJobs;
            }

            set
            {
                _NJobs = (value);
            }
        }

        private uint _NInstalledJobs = default (uint);
        public uint NInstalledJobs
        {
            get
            {
                return _NInstalledJobs;
            }

            set
            {
                _NInstalledJobs = (value);
            }
        }

        private uint _NFailedJobs = default (uint);
        public uint NFailedJobs
        {
            get
            {
                return _NFailedJobs;
            }

            set
            {
                _NFailedJobs = (value);
            }
        }

        private double _Progress = default (double);
        public double Progress
        {
            get
            {
                return _Progress;
            }

            set
            {
                _Progress = (value);
            }
        }

        private string[] _Environment = default (string[]);
        public string[] Environment
        {
            get
            {
                return _Environment;
            }

            set
            {
                _Environment = (value);
            }
        }

        private bool _ConfirmSpawn = default (bool);
        public bool ConfirmSpawn
        {
            get
            {
                return _ConfirmSpawn;
            }

            set
            {
                _ConfirmSpawn = (value);
            }
        }

        private bool _ShowStatus = default (bool);
        public bool ShowStatus
        {
            get
            {
                return _ShowStatus;
            }

            set
            {
                _ShowStatus = (value);
            }
        }

        private string[] _UnitPath = default (string[]);
        public string[] UnitPath
        {
            get
            {
                return _UnitPath;
            }

            set
            {
                _UnitPath = (value);
            }
        }

        private string _DefaultStandardOutput = default (string);
        public string DefaultStandardOutput
        {
            get
            {
                return _DefaultStandardOutput;
            }

            set
            {
                _DefaultStandardOutput = (value);
            }
        }

        private string _DefaultStandardError = default (string);
        public string DefaultStandardError
        {
            get
            {
                return _DefaultStandardError;
            }

            set
            {
                _DefaultStandardError = (value);
            }
        }

        private ulong _RuntimeWatchdogUSec = default (ulong);
        public ulong RuntimeWatchdogUSec
        {
            get
            {
                return _RuntimeWatchdogUSec;
            }

            set
            {
                _RuntimeWatchdogUSec = (value);
            }
        }

        private ulong _ShutdownWatchdogUSec = default (ulong);
        public ulong ShutdownWatchdogUSec
        {
            get
            {
                return _ShutdownWatchdogUSec;
            }

            set
            {
                _ShutdownWatchdogUSec = (value);
            }
        }

        private string _ControlGroup = default (string);
        public string ControlGroup
        {
            get
            {
                return _ControlGroup;
            }

            set
            {
                _ControlGroup = (value);
            }
        }

        private string _SystemState = default (string);
        public string SystemState
        {
            get
            {
                return _SystemState;
            }

            set
            {
                _SystemState = (value);
            }
        }

        private byte _ExitCode = default (byte);
        public byte ExitCode
        {
            get
            {
                return _ExitCode;
            }

            set
            {
                _ExitCode = (value);
            }
        }

        private ulong _DefaultTimerAccuracyUSec = default (ulong);
        public ulong DefaultTimerAccuracyUSec
        {
            get
            {
                return _DefaultTimerAccuracyUSec;
            }

            set
            {
                _DefaultTimerAccuracyUSec = (value);
            }
        }

        private ulong _DefaultTimeoutStartUSec = default (ulong);
        public ulong DefaultTimeoutStartUSec
        {
            get
            {
                return _DefaultTimeoutStartUSec;
            }

            set
            {
                _DefaultTimeoutStartUSec = (value);
            }
        }

        private ulong _DefaultTimeoutStopUSec = default (ulong);
        public ulong DefaultTimeoutStopUSec
        {
            get
            {
                return _DefaultTimeoutStopUSec;
            }

            set
            {
                _DefaultTimeoutStopUSec = (value);
            }
        }

        private ulong _DefaultRestartUSec = default (ulong);
        public ulong DefaultRestartUSec
        {
            get
            {
                return _DefaultRestartUSec;
            }

            set
            {
                _DefaultRestartUSec = (value);
            }
        }

        private ulong _DefaultStartLimitIntervalSec = default (ulong);
        public ulong DefaultStartLimitIntervalSec
        {
            get
            {
                return _DefaultStartLimitIntervalSec;
            }

            set
            {
                _DefaultStartLimitIntervalSec = (value);
            }
        }

        private uint _DefaultStartLimitBurst = default (uint);
        public uint DefaultStartLimitBurst
        {
            get
            {
                return _DefaultStartLimitBurst;
            }

            set
            {
                _DefaultStartLimitBurst = (value);
            }
        }

        private bool _DefaultCPUAccounting = default (bool);
        public bool DefaultCPUAccounting
        {
            get
            {
                return _DefaultCPUAccounting;
            }

            set
            {
                _DefaultCPUAccounting = (value);
            }
        }

        private bool _DefaultBlockIOAccounting = default (bool);
        public bool DefaultBlockIOAccounting
        {
            get
            {
                return _DefaultBlockIOAccounting;
            }

            set
            {
                _DefaultBlockIOAccounting = (value);
            }
        }

        private bool _DefaultMemoryAccounting = default (bool);
        public bool DefaultMemoryAccounting
        {
            get
            {
                return _DefaultMemoryAccounting;
            }

            set
            {
                _DefaultMemoryAccounting = (value);
            }
        }

        private bool _DefaultTasksAccounting = default (bool);
        public bool DefaultTasksAccounting
        {
            get
            {
                return _DefaultTasksAccounting;
            }

            set
            {
                _DefaultTasksAccounting = (value);
            }
        }

        private ulong _DefaultLimitCPU = default (ulong);
        public ulong DefaultLimitCPU
        {
            get
            {
                return _DefaultLimitCPU;
            }

            set
            {
                _DefaultLimitCPU = (value);
            }
        }

        private ulong _DefaultLimitCPUSoft = default (ulong);
        public ulong DefaultLimitCPUSoft
        {
            get
            {
                return _DefaultLimitCPUSoft;
            }

            set
            {
                _DefaultLimitCPUSoft = (value);
            }
        }

        private ulong _DefaultLimitFSIZE = default (ulong);
        public ulong DefaultLimitFSIZE
        {
            get
            {
                return _DefaultLimitFSIZE;
            }

            set
            {
                _DefaultLimitFSIZE = (value);
            }
        }

        private ulong _DefaultLimitFSIZESoft = default (ulong);
        public ulong DefaultLimitFSIZESoft
        {
            get
            {
                return _DefaultLimitFSIZESoft;
            }

            set
            {
                _DefaultLimitFSIZESoft = (value);
            }
        }

        private ulong _DefaultLimitDATA = default (ulong);
        public ulong DefaultLimitDATA
        {
            get
            {
                return _DefaultLimitDATA;
            }

            set
            {
                _DefaultLimitDATA = (value);
            }
        }

        private ulong _DefaultLimitDATASoft = default (ulong);
        public ulong DefaultLimitDATASoft
        {
            get
            {
                return _DefaultLimitDATASoft;
            }

            set
            {
                _DefaultLimitDATASoft = (value);
            }
        }

        private ulong _DefaultLimitSTACK = default (ulong);
        public ulong DefaultLimitSTACK
        {
            get
            {
                return _DefaultLimitSTACK;
            }

            set
            {
                _DefaultLimitSTACK = (value);
            }
        }

        private ulong _DefaultLimitSTACKSoft = default (ulong);
        public ulong DefaultLimitSTACKSoft
        {
            get
            {
                return _DefaultLimitSTACKSoft;
            }

            set
            {
                _DefaultLimitSTACKSoft = (value);
            }
        }

        private ulong _DefaultLimitCORE = default (ulong);
        public ulong DefaultLimitCORE
        {
            get
            {
                return _DefaultLimitCORE;
            }

            set
            {
                _DefaultLimitCORE = (value);
            }
        }

        private ulong _DefaultLimitCORESoft = default (ulong);
        public ulong DefaultLimitCORESoft
        {
            get
            {
                return _DefaultLimitCORESoft;
            }

            set
            {
                _DefaultLimitCORESoft = (value);
            }
        }

        private ulong _DefaultLimitRSS = default (ulong);
        public ulong DefaultLimitRSS
        {
            get
            {
                return _DefaultLimitRSS;
            }

            set
            {
                _DefaultLimitRSS = (value);
            }
        }

        private ulong _DefaultLimitRSSSoft = default (ulong);
        public ulong DefaultLimitRSSSoft
        {
            get
            {
                return _DefaultLimitRSSSoft;
            }

            set
            {
                _DefaultLimitRSSSoft = (value);
            }
        }

        private ulong _DefaultLimitNOFILE = default (ulong);
        public ulong DefaultLimitNOFILE
        {
            get
            {
                return _DefaultLimitNOFILE;
            }

            set
            {
                _DefaultLimitNOFILE = (value);
            }
        }

        private ulong _DefaultLimitNOFILESoft = default (ulong);
        public ulong DefaultLimitNOFILESoft
        {
            get
            {
                return _DefaultLimitNOFILESoft;
            }

            set
            {
                _DefaultLimitNOFILESoft = (value);
            }
        }

        private ulong _DefaultLimitAS = default (ulong);
        public ulong DefaultLimitAS
        {
            get
            {
                return _DefaultLimitAS;
            }

            set
            {
                _DefaultLimitAS = (value);
            }
        }

        private ulong _DefaultLimitASSoft = default (ulong);
        public ulong DefaultLimitASSoft
        {
            get
            {
                return _DefaultLimitASSoft;
            }

            set
            {
                _DefaultLimitASSoft = (value);
            }
        }

        private ulong _DefaultLimitNPROC = default (ulong);
        public ulong DefaultLimitNPROC
        {
            get
            {
                return _DefaultLimitNPROC;
            }

            set
            {
                _DefaultLimitNPROC = (value);
            }
        }

        private ulong _DefaultLimitNPROCSoft = default (ulong);
        public ulong DefaultLimitNPROCSoft
        {
            get
            {
                return _DefaultLimitNPROCSoft;
            }

            set
            {
                _DefaultLimitNPROCSoft = (value);
            }
        }

        private ulong _DefaultLimitMEMLOCK = default (ulong);
        public ulong DefaultLimitMEMLOCK
        {
            get
            {
                return _DefaultLimitMEMLOCK;
            }

            set
            {
                _DefaultLimitMEMLOCK = (value);
            }
        }

        private ulong _DefaultLimitMEMLOCKSoft = default (ulong);
        public ulong DefaultLimitMEMLOCKSoft
        {
            get
            {
                return _DefaultLimitMEMLOCKSoft;
            }

            set
            {
                _DefaultLimitMEMLOCKSoft = (value);
            }
        }

        private ulong _DefaultLimitLOCKS = default (ulong);
        public ulong DefaultLimitLOCKS
        {
            get
            {
                return _DefaultLimitLOCKS;
            }

            set
            {
                _DefaultLimitLOCKS = (value);
            }
        }

        private ulong _DefaultLimitLOCKSSoft = default (ulong);
        public ulong DefaultLimitLOCKSSoft
        {
            get
            {
                return _DefaultLimitLOCKSSoft;
            }

            set
            {
                _DefaultLimitLOCKSSoft = (value);
            }
        }

        private ulong _DefaultLimitSIGPENDING = default (ulong);
        public ulong DefaultLimitSIGPENDING
        {
            get
            {
                return _DefaultLimitSIGPENDING;
            }

            set
            {
                _DefaultLimitSIGPENDING = (value);
            }
        }

        private ulong _DefaultLimitSIGPENDINGSoft = default (ulong);
        public ulong DefaultLimitSIGPENDINGSoft
        {
            get
            {
                return _DefaultLimitSIGPENDINGSoft;
            }

            set
            {
                _DefaultLimitSIGPENDINGSoft = (value);
            }
        }

        private ulong _DefaultLimitMSGQUEUE = default (ulong);
        public ulong DefaultLimitMSGQUEUE
        {
            get
            {
                return _DefaultLimitMSGQUEUE;
            }

            set
            {
                _DefaultLimitMSGQUEUE = (value);
            }
        }

        private ulong _DefaultLimitMSGQUEUESoft = default (ulong);
        public ulong DefaultLimitMSGQUEUESoft
        {
            get
            {
                return _DefaultLimitMSGQUEUESoft;
            }

            set
            {
                _DefaultLimitMSGQUEUESoft = (value);
            }
        }

        private ulong _DefaultLimitNICE = default (ulong);
        public ulong DefaultLimitNICE
        {
            get
            {
                return _DefaultLimitNICE;
            }

            set
            {
                _DefaultLimitNICE = (value);
            }
        }

        private ulong _DefaultLimitNICESoft = default (ulong);
        public ulong DefaultLimitNICESoft
        {
            get
            {
                return _DefaultLimitNICESoft;
            }

            set
            {
                _DefaultLimitNICESoft = (value);
            }
        }

        private ulong _DefaultLimitRTPRIO = default (ulong);
        public ulong DefaultLimitRTPRIO
        {
            get
            {
                return _DefaultLimitRTPRIO;
            }

            set
            {
                _DefaultLimitRTPRIO = (value);
            }
        }

        private ulong _DefaultLimitRTPRIOSoft = default (ulong);
        public ulong DefaultLimitRTPRIOSoft
        {
            get
            {
                return _DefaultLimitRTPRIOSoft;
            }

            set
            {
                _DefaultLimitRTPRIOSoft = (value);
            }
        }

        private ulong _DefaultLimitRTTIME = default (ulong);
        public ulong DefaultLimitRTTIME
        {
            get
            {
                return _DefaultLimitRTTIME;
            }

            set
            {
                _DefaultLimitRTTIME = (value);
            }
        }

        private ulong _DefaultLimitRTTIMESoft = default (ulong);
        public ulong DefaultLimitRTTIMESoft
        {
            get
            {
                return _DefaultLimitRTTIMESoft;
            }

            set
            {
                _DefaultLimitRTTIMESoft = (value);
            }
        }

        private ulong _DefaultTasksMax = default (ulong);
        public ulong DefaultTasksMax
        {
            get
            {
                return _DefaultTasksMax;
            }

            set
            {
                _DefaultTasksMax = (value);
            }
        }

        private ulong _TimerSlackNSec = default (ulong);
        public ulong TimerSlackNSec
        {
            get
            {
                return _TimerSlackNSec;
            }

            set
            {
                _TimerSlackNSec = (value);
            }
        }
    }
}