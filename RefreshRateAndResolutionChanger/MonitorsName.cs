using RefreshRateWpfApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RefreshRateAndResolutionChanger
{
    public class MonitorInfo
    {
        public string DisplayName;   // \\.\DISPLAY1
        public string IdName;  // np. Dell U2720Q
    }
    internal class MonitorsName
    {
        public static List<MonitorInfo> GetMonitors()
        {
            var result = new List<MonitorInfo>();

            var wmiNames = GetWmiMonitorNames();

            uint devNum = 0;
            WinApiWrapper.DISPLAY_DEVICE d = new WinApiWrapper.DISPLAY_DEVICE();
            d.cb = Marshal.SizeOf(d);

            while (WinApiWrapper.EnumDisplayDevices(null, devNum, ref d, 0))
            {
                if ((d.StateFlags & WinApiWrapper.DISPLAY_DEVICE_ACTIVE) != 0)
                {
                    WinApiWrapper.DISPLAY_DEVICE monitor = new WinApiWrapper.DISPLAY_DEVICE();
                    monitor.cb = Marshal.SizeOf(monitor);

                    uint devNum2 = 0;
                    while (WinApiWrapper.EnumDisplayDevices(d.DeviceName, devNum2, ref monitor, 0))
                    {
                        string deviceId = monitor.DeviceID;

                        //string friendly = "Unknown";
                        string friendly = deviceId.Split('\\')[1];

                        foreach (var wmi in wmiNames)
                        {
                            if (deviceId.Contains(wmi.Key))
                            {
                                friendly = wmi.Value;
                                break;
                            }
                        }

                        result.Add(new MonitorInfo
                        {
                            DisplayName = d.DeviceName,
                            IdName = friendly
                        });
                        devNum2++;
                        monitor = new WinApiWrapper.DISPLAY_DEVICE();
                        monitor.cb = Marshal.SizeOf(monitor);

                    }
                }

                devNum++;
                d = new WinApiWrapper.DISPLAY_DEVICE();
                d.cb = Marshal.SizeOf(d);
            }

            return result;
        }

        private static Dictionary<string, string> GetWmiMonitorNames()
        {
            var dict = new Dictionary<string, string>();

            using (var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM WmiMonitorID"))
            using (var results = searcher.Get())
            {

                foreach (ManagementObject m in results)
                {
                    using (m)
                    {
                        string instanceName = m["InstanceName"].ToString();

                        string name = Decode(m["UserFriendlyName"] as ushort[]);
                        string manufacturer = Decode(m["ManufacturerName"] as ushort[]);

                        string key = instanceName.Split('\\')[1]; // identyfikator sprzętu

                        dict[key] = $"{manufacturer} {name}";
                    }
                }
            }

            return dict;
        }

        private static string Decode(ushort[] data)
        {
            if (data == null) return "";

            var sb = new StringBuilder();
            foreach (var c in data)
            {
                if (c == 0) break;
                sb.Append((char)c);
            }
            return sb.ToString();
        }

        // WinAPI


    }
}
