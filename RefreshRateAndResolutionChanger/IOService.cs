using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RefreshRateAndResolutionChanger
{
    internal class IOService
    {
        public static void SetRunAtStartup(bool runStartup)
        {
            using (Microsoft.Win32.RegistryKey reg = Microsoft.Win32.Registry.CurrentUser
                .OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {

                if (reg == null) return;
                var key2 = reg.GetValue("RefreshRateApp");
                if (runStartup)
                {
                    reg.SetValue("RefreshRateApp", Process.GetCurrentProcess().MainModule.FileName);
                }
                else
                {
                    var key = reg.GetValue("RefreshRateApp");
                    if (key == null) return;

                    reg.DeleteValue("RefreshRateApp");

                }
                reg.Close();
            }

        }

        void SaveToFile()
        {
            // Implement saving to file logic here
        }   
    }
}
