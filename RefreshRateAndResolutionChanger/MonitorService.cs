using RefreshRateWpfApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RefreshRateAndResolutionChanger
{
    public  class MonitorService
    {

        public static bool RefreshMonitorsLists()
        {
            //monitorInfoHandlesList.Clear();
            MonitorState.MonitorInfoHandlesList.Clear();
            MonitorState.MonitorInfoHandlesList.AddRange(WinApiWrapper.GetMonitorInfoHandlesList());


            List<MonitorInfo> monitorsNewList = MonitorsName.GetMonitors();

            bool monitorsChanged = !MonitorState.MonitorInfoNamesList.Select(a => a.IdName).SequenceEqual(monitorsNewList.Select(a => a.IdName));

            if (monitorsChanged)
            {
                MonitorState.MonitorInfoNamesList = monitorsNewList;
            }
            return monitorsChanged;
        }




        public static (string resolutionInfo, string displayInfo) ApplyTestSettings(RefreshDataModel resSettings, out RefreshDataModel originalSettings)
        {
            originalSettings = WinApiWrapper.GetActualResolutionAndRefresRateFromMonitor(resSettings.MonitorDisplay);

            WinApiWrapper.SetResolutionAndFrequerency(resSettings);

            var currentInfo = WinApiWrapper.GetActualResolutionAndRefresRateFromMonitor(resSettings.MonitorDisplay).FullNameWithMonitorDisplayAndName;
            var splitInfo = currentInfo.Split('@');

            string info1 = splitInfo.Length > 1 ? $"{splitInfo[0].Trim()} @ {splitInfo[1].Trim()}" : "Unknown";
            string info2 = splitInfo.Length > 2 ? $"Display: {splitInfo[2].Trim().LastOrDefault()}" : "Display: N/A";

            return (info1, info2);
        }



        public static (double hOffset, double vOffset) CalctulatePopupPosition(RefreshDataModel resSettings, double popupWidth, double popupHeight)
        {

            // znajdź DISPLAY

            var targetDisplay = MonitorState.MonitorInfoNamesList.FirstOrDefault(m => m.IdName == resSettings.MonitorIdName)?.DisplayName;

            if (targetDisplay == null)
            {
                return (0, 0);
            }

            var target = MonitorState.MonitorInfoHandlesList.FirstOrDefault(m => m.SzDevice == targetDisplay);
            if (target == null)
            {
                return (0, 0);
            }

            IntPtr hmonitor = target.Handle;

            //var rectMon = target.info.rcMonitor;

            var width = target.Right - target.Left;
            var height = target.Bottom - target.Top;

            (uint dpiX, uint dpiY) = WinApiWrapper.GetDpiForMonitor(hmonitor);

            var dpiScaleX = dpiX / 96.0;
            var dpiScaleY = dpiY / 96.0;


            var screenWidth = width / dpiScaleX;
            var screenHeight = height / dpiScaleY;
                        

            var scaledWidth = popupWidth / dpiScaleX;
            var scaledHeight = popupHeight / dpiScaleY;

            var offsetX = (screenWidth - popupWidth) / 2;
            var offsetY = (screenHeight - popupHeight) / 2;


            var horizontalOffset = target.Left / dpiScaleX + offsetX; 
            var verticalOffset = target.Top / dpiScaleY + offsetY;

            return (horizontalOffset, verticalOffset);
        }
    }
}
