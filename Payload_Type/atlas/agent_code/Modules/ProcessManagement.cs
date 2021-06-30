using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Atlas {
    class ProcessManagement {
        // Most of this code is directly from SharSploit: https://github.com/cobbr/SharpSploit
        public class ProcessList
        {
            private struct PROCESS_BASIC_INFORMATION
            {
                private IntPtr ExitStatus;
                private IntPtr PebBaseAddress;
                private IntPtr AffinityMask;
                private IntPtr BasePriority;
                private UIntPtr UniqueProcessId;
                public int InheritedFromUniqueProcessId;

                private int Size
                {
                    get { return (int)Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION)); }
                }
            }

            private enum PROCESSINFOCLASS : int
            {
                ProcessBasicInformation = 0, // 0, q: PROCESS_BASIC_INFORMATION, PROCESS_EXTENDED_BASIC_INFORMATION
                ProcessQuotaLimits, // qs: QUOTA_LIMITS, QUOTA_LIMITS_EX
                ProcessIoCounters, // q: IO_COUNTERS
                ProcessVmCounters, // q: VM_COUNTERS, VM_COUNTERS_EX
                ProcessTimes, // q: KERNEL_USER_TIMES
                ProcessBasePriority, // s: KPRIORITY
                ProcessRaisePriority, // s: ULONG
                ProcessDebugPort, // q: HANDLE
                ProcessExceptionPort, // s: HANDLE
                ProcessAccessToken, // s: PROCESS_ACCESS_TOKEN
                ProcessLdtInformation, // 10
                ProcessLdtSize,
                ProcessDefaultHardErrorMode, // qs: ULONG
                ProcessIoPortHandlers, // (kernel-mode only)
                ProcessPooledUsageAndLimits, // q: POOLED_USAGE_AND_LIMITS
                ProcessWorkingSetWatch, // q: PROCESS_WS_WATCH_INFORMATION[]; s: void
                ProcessUserModeIOPL,
                ProcessEnableAlignmentFaultFixup, // s: BOOLEAN
                ProcessPriorityClass, // qs: PROCESS_PRIORITY_CLASS
                ProcessWx86Information,
                ProcessHandleCount, // 20, q: ULONG, PROCESS_HANDLE_INFORMATION
                ProcessAffinityMask, // s: KAFFINITY
                ProcessPriorityBoost, // qs: ULONG
                ProcessDeviceMap, // qs: PROCESS_DEVICEMAP_INFORMATION, PROCESS_DEVICEMAP_INFORMATION_EX
                ProcessSessionInformation, // q: PROCESS_SESSION_INFORMATION
                ProcessForegroundInformation, // s: PROCESS_FOREGROUND_BACKGROUND
                ProcessWow64Information, // q: ULONG_PTR
                ProcessImageFileName, // q: UNICODE_STRING
                ProcessLUIDDeviceMapsEnabled, // q: ULONG
                ProcessBreakOnTermination, // qs: ULONG
                ProcessDebugObjectHandle, // 30, q: HANDLE
                ProcessDebugFlags, // qs: ULONG
                ProcessHandleTracing, // q: PROCESS_HANDLE_TRACING_QUERY; s: size 0 disables, otherwise enables
                ProcessIoPriority, // qs: ULONG
                ProcessExecuteFlags, // qs: ULONG
                ProcessResourceManagement,
                ProcessCookie, // q: ULONG
                ProcessImageInformation, // q: SECTION_IMAGE_INFORMATION
                ProcessCycleTime, // q: PROCESS_CYCLE_TIME_INFORMATION
                ProcessPagePriority, // q: ULONG
                ProcessInstrumentationCallback, // 40
                ProcessThreadStackAllocation, // s: PROCESS_STACK_ALLOCATION_INFORMATION, PROCESS_STACK_ALLOCATION_INFORMATION_EX
                ProcessWorkingSetWatchEx, // q: PROCESS_WS_WATCH_INFORMATION_EX[]
                ProcessImageFileNameWin32, // q: UNICODE_STRING
                ProcessImageFileMapping, // q: HANDLE (input)
                ProcessAffinityUpdateMode, // qs: PROCESS_AFFINITY_UPDATE_MODE
                ProcessMemoryAllocationMode, // qs: PROCESS_MEMORY_ALLOCATION_MODE
                ProcessGroupInformation, // q: USHORT[]
                ProcessTokenVirtualizationEnabled, // s: ULONG
                ProcessConsoleHostProcess, // q: ULONG_PTR
                ProcessWindowInformation, // 50, q: PROCESS_WINDOW_INFORMATION
                ProcessHandleInformation, // q: PROCESS_HANDLE_SNAPSHOT_INFORMATION // since WIN8
                ProcessMitigationPolicy, // s: PROCESS_MITIGATION_POLICY_INFORMATION
                ProcessDynamicFunctionTableInformation,
                ProcessHandleCheckingMode,
                ProcessKeepAliveCount, // q: PROCESS_KEEPALIVE_COUNT_INFORMATION
                ProcessRevokeFileHandles, // s: PROCESS_REVOKE_FILE_HANDLES_INFORMATION
                MaxProcessInfoClass
            };

            [DllImport("ntdll.dll", SetLastError = true)]
            private static extern int NtQueryInformationProcess(IntPtr hProcess, PROCESSINFOCLASS pic, IntPtr pi, int cb, out int pSize);

            public static int GetParentProcess(IntPtr Handle)
            {
                int returnLength;
                var basicProcessInformation = new PROCESS_BASIC_INFORMATION();
                IntPtr pProcInfo = Marshal.AllocHGlobal(Marshal.SizeOf(basicProcessInformation));
                Marshal.StructureToPtr(basicProcessInformation, pProcInfo, true);
                NtQueryInformationProcess(Handle, PROCESSINFOCLASS.ProcessBasicInformation, pProcInfo, Marshal.SizeOf(basicProcessInformation), out returnLength);
                basicProcessInformation = (PROCESS_BASIC_INFORMATION)Marshal.PtrToStructure(pProcInfo, typeof(PROCESS_BASIC_INFORMATION));

                return basicProcessInformation.InheritedFromUniqueProcessId;
            }

            [DllImport("kernel32.dll")]
            private static extern Boolean OpenProcessToken(IntPtr hProcess, UInt32 dwDesiredAccess, out IntPtr hToken);

            private static string GetProcessOwner(Process Process)
            {
                try
                {
                    IntPtr handle;
                    OpenProcessToken(Process.Handle, 8, out handle);
                    using (var winIdentity = new System.Security.Principal.WindowsIdentity(handle))
                    {
                        return winIdentity.Name;
                    }
                }
                catch (InvalidOperationException)
                {
                    return string.Empty;
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    return string.Empty;
                }
            }

            private struct SYSTEM_INFO
            {
                public ushort wProcessorArchitecture;
                private ushort wReserved;
                private uint dwPageSize;
                private IntPtr lpMinimumApplicationAddress;
                private IntPtr lpMaximumApplicationAddress;
                private UIntPtr dwActiveProcessorMask;
                private uint dwNumberOfProcessors;
                private uint dwProcessorType;
                private uint dwAllocationGranularity;
                private ushort wProcessorLevel;
                private ushort wProcessorRevision;
            };

            private enum Platform
            {
                x86,
                x64,
                IA64,
                Unknown
            }

            [DllImport("kernel32.dll")]
            private static extern void GetNativeSystemInfo(ref SYSTEM_INFO lpSystemInfo);

            private static Platform GetArchitecture()
            {
                const ushort PROCESSOR_ARCHITECTURE_INTEL = 0;
                const ushort PROCESSOR_ARCHITECTURE_IA64 = 6;
                const ushort PROCESSOR_ARCHITECTURE_AMD64 = 9;

                var sysInfo = new SYSTEM_INFO();
                GetNativeSystemInfo(ref sysInfo);

                switch (sysInfo.wProcessorArchitecture)
                {
                    case PROCESSOR_ARCHITECTURE_AMD64:
                        return Platform.x64;
                    case PROCESSOR_ARCHITECTURE_INTEL:
                        return Platform.x86;
                    case PROCESSOR_ARCHITECTURE_IA64:
                        return Platform.IA64;
                    default:
                        return Platform.Unknown;
                }
            }

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool IsWow64Process(IntPtr hProcess, out bool Wow64Process);

            private static bool IsWow64(Process Process)
            {
                try
                {
                    bool isWow64;
                    IsWow64Process(Process.Handle, out isWow64);
                    return isWow64;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    return false;
                }
            }

            private static int GetParentProcess(Process Process)
            {
                try
                {
                    return GetParentProcess(Process.Handle);
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    return 0;
                }
            }

            public static Messages.ProcessList GetProcessList()
            {
                Messages.ProcessList process_list = new Messages.ProcessList { };
                try
                {
                    var processorArchitecture = GetArchitecture();
                    Process[] processes = Process.GetProcesses().OrderBy(P => P.Id).ToArray();
                    foreach (Process process in processes)
                    {
                        Messages.Process process_info = new Messages.Process
                        {
                            process_id = process.Id,
                            parent_process_id = GetParentProcess(process),
                            name = process.ProcessName,
                            bin_path = string.Empty,
                            user = GetProcessOwner(process)
                        };

                        if (process_info.parent_process_id != 0)
                        {
                            try
                            {
                                process_info.bin_path = process.MainModule.FileName;
                            }
                            catch (System.ComponentModel.Win32Exception) { }
                        }

                        if (processorArchitecture == Platform.x64)
                        {
                            process_info.architecture = "x64";
                        }
                        else
                        {
                            process_info.architecture = "x86";
                        }
                        process_list.process_list.Add(process_info);
                    }
                    return process_list;
                }
                catch
                {
                    return process_list;
                }
            }
        }
    }
}