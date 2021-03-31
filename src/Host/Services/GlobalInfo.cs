using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace JetHub.Services
{
    public interface IGlobalInfo
    {
        string KernelVersion { get; }

        string BootCmdline { get; }
        
        string VfsRoot { get; }
        
        List<(string CpuName, int Core, int Processor)> CpuInfo { get; }
    }
    
    public class FakeGlobalInfo : IGlobalInfo
    {
        public string KernelVersion { get; } =
            "Linux version 4.15.0-112-generic " +
            "(buildd@lcy01-amd64-027) " +
            "(gcc version 7.5.0 (Ubuntu 7.5.0-3ubuntu1~18.04)) " +
            "#113-Ubuntu SMP Thu Jul 9 23:41:39 UTC 2020";

        public string BootCmdline { get; } =
            "BOOT_IMAGE=/boot/vmlinuz-4.15.0-112-generic " +
            "root=UUID=00000000-0000-0000-0000-000000000000 " +
            "ro " +
            "vga=792 " +
            "console=tty0 " +
            "console=ttyS0,115200n8 " +
            "net.ifnames=0 " +
            "noibrs " +
            "quiet " +
            "splash " +
            "vt.handoff=1";

        public string VfsRoot { get; }

        public List<(string CpuName, int Core, int Processor)> CpuInfo { get; }
            = new List<(string CpuName, int Core, int Processor)>
            {
                ("Intel(R) Xeon(R) CPU E5-2682 v4 @ 2.50GHz", 2, 4),
            };

        public FakeGlobalInfo()
        {
            var playground = Path.Combine(Environment.CurrentDirectory, "playground");
            if (!Directory.Exists(playground)) Directory.CreateDirectory(playground);
            VfsRoot = playground;
        }
    }

    public class OneShotGlobalInfo : IGlobalInfo, IHostedService
    {
        public string KernelVersion { get; private set; } = "Unknown";

        public string BootCmdline { get; private set; } = "Unknown";

        public string VfsRoot { get; } = "/opt/domjudge/judgehost/judgings";

        public List<(string CpuName, int Core, int Processor)> CpuInfo { get; private set; } = new List<(string, int, int)>();

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            KernelVersion = await File.ReadAllTextAsync("/proc/version", cancellationToken);
            BootCmdline = await File.ReadAllTextAsync("/proc/cmdline", cancellationToken);
            
            /*
processor	: 3
vendor_id	: GenuineIntel
cpu family	: 6
model		: 158
model name	: Intel(R) Core(TM) i5-7500 CPU @ 3.40GHz
stepping	: 9
microcode	: 0xde
cpu MHz		: 3645.152
cache size	: 6144 KB
physical id	: 0
siblings	: 4
core id		: 3
cpu cores	: 4
apicid		: 6
initial apicid	: 6
fpu		: yes
fpu_exception	: yes
cpuid level	: 22
wp		: yes
flags		: fpu vme de pse tsc msr pae mce cx8 apic sep mtrr pge mca cmov pat pse36 clflush dts acpi mmx fxsr sse sse2 ss ht tm pbe syscall nx pdpe1gb rdtscp lm constant_tsc art arch_perfmon pebs bts rep_good nopl xtopology nonstop_tsc cpuid aperfmperf pni pclmulqdq dtes64 monitor ds_cpl vmx smx est tm2 ssse3 sdbg fma cx16 xtpr pdcm pcid sse4_1 sse4_2 x2apic movbe popcnt tsc_deadline_timer aes xsave avx f16c rdrand lahf_lm abm 3dnowprefetch cpuid_fault epb invpcid_single pti ssbd ibrs ibpb stibp tpr_shadow vnmi flexpriority ept vpid ept_ad fsgsbase tsc_adjust bmi1 hle avx2 smep bmi2 erms invpcid rtm mpx rdseed adx smap clflushopt intel_pt xsaveopt xsavec xgetbv1 xsaves dtherm ida arat pln pts hwp hwp_notify hwp_act_window hwp_epp md_clear flush_l1d
bugs		: cpu_meltdown spectre_v1 spectre_v2 spec_store_bypass l1tf mds swapgs taa itlb_multihit srbds
bogomips	: 6799.81
clflush size	: 64
cache_alignment	: 64
address sizes	: 39 bits physical, 48 bits virtual
power management:
             */

            var cpuinfo = await File.ReadAllTextAsync("/proc/cpuinfo", cancellationToken);
            var processors = cpuinfo.Trim().Split("\n\n");
            var processor1 = new Dictionary<string, (string, Dictionary<string, int>)>();
            foreach (var processor in processors)
            {
                var things = processor.Trim()
                    .Split('\n')
                    .Select(a => a.Split(new[] {':'}, 2))
                    .ToDictionary(k => k[0].Trim(), v => v[1].Trim());

                var modelName = things["model name"];
                var physicalId = things["physical id"];
                var coreId = things["core id"];
                if (!processor1.TryGetValue(physicalId, out var kvp))
                {
                    kvp = (modelName, new Dictionary<string, int>());
                    processor1.Add(physicalId, kvp);
                }

                kvp.Item2[coreId] = kvp.Item2.GetValueOrDefault(coreId) + 1;
            }

            var finalCpuInfo = new List<(string, int, int)>();
            foreach (var (modelName, coreToProc) in processor1.Values)
            {
                finalCpuInfo.Add((modelName, coreToProc.Count, coreToProc.Values.Sum()));
            }

            CpuInfo = finalCpuInfo;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}