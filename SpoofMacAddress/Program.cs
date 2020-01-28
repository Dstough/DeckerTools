using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;

namespace SpoofMacAddress
{
    class Program
    {
        static string baseReg = @"SYSTEM\CurrentControlSet\Control\Class\{4D36E972-E325-11CE-BFC1-08002bE10318}\";

        static void Main(string[] args)
        {
        }

        public static bool SetMAC(string nicid, string newmac)
        {
            var ret = false;

            using (var bkey = GetBaseKey())
            using (var key = bkey.OpenSubKey(baseReg + nicid))
            {
                if (key != null)
                {
                    key.SetValue("NetworkAddress", newmac, RegistryValueKind.String);

                    var mos = new ManagementObjectSearcher(new SelectQuery("SELECT * FROM Win32_NetworkAdapter WHERE Index = " + nicid));

                    foreach (var o in mos.Get().OfType<ManagementObject>())
                    {
                        o.InvokeMethod("Disable", null);
                        o.InvokeMethod("Enable", null);
                        ret = true;
                    }
                }
            }

            return ret;
        }

        public static IEnumerable<string> GetNicIds()
        {
            using var bkey = GetBaseKey();
            using var key = bkey.OpenSubKey(baseReg);

            if (key == null)
                yield break;

            foreach (string name in key.GetSubKeyNames().Where(n => n != "Properties"))
                using (RegistryKey sub = key.OpenSubKey(name))
                    if (sub != null)
                    {
                        var busType = sub.GetValue("BusType");
                        var busStr = busType != null ? busType.ToString() : string.Empty;

                        if (busStr != string.Empty)
                            yield return name;
                    }
        }

        public static RegistryKey GetBaseKey() => RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,InternalCheckIsWow64() ? RegistryView.Registry64 : RegistryView.Registry32);
        
        public static bool InternalCheckIsWow64()
        {
            if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) || Environment.OSVersion.Version.Major >= 6)
                using (var p = Process.GetCurrentProcess())
                {
                    bool retVal;

                    if (!IsWow64Process(p.Handle, out retVal))
                        return false;

                    return retVal;
                }
            else
                return false;
        }

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool wow64Process);
    }
}
