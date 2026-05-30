using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using RefreshRateAndResolutionChanger;
using RefreshRateAndResolutionChanger.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace RefreshRateWpfApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private DispatcherTimer timer;
        private int _testTime = 8;

        public int TestTime
        {
            get => _testTime;
            set
            {
                _testTime = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TestTime)));
                DirtySetting = true;
            }
        }

        private bool _dirtySetting;
        public bool DirtySetting
        {
            get { return _dirtySetting; }
            set
            {
                _dirtySetting = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DirtySetting)));
            }
        }

        private bool _runStartup;
        public bool RunStartup
        {
            get { return _runStartup; }
            set
            {
                _runStartup = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RunStartup)));
                SetRunSatartup();
            }
        }

        private bool _runAsMinimalized;

        public bool RunAsMinimalized
        {
            get { return _runAsMinimalized; }
            set
            {
                _runAsMinimalized = value;
                DirtySetting = true;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RunAsMinimalized)));
            }
        }


        private bool _allResolutionMode = true;

        public bool AllResolutionMode
        {
            get { return _allResolutionMode; }
            set
            {
                _allResolutionMode = value;
                DirtySetting = true;
                SetActalRefreshRateAndHeaderLabel();
                SetPossibleRefreshRateList(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AllResolutionMode)));
            }
        }

        private bool _isMoreThenOneMonitor;
        public bool IsMoreThenOneMonitor //set by SetMonitorsList method in timer tick
        {

            get { return _isMoreThenOneMonitor; }
            set
            {
                if (value == _isMoreThenOneMonitor) return;
                _isMoreThenOneMonitor = value;
                RefreshTryList();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMoreThenOneMonitor)));
            }
        }

        void SetRunSatartup()
        {
            Microsoft.Win32.RegistryKey reg = Microsoft.Win32.Registry.CurrentUser
                .OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (reg == null) return;
            var key2 = reg.GetValue("RefreshRateApp");
            if (_runStartup)
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

        void SetWindowHeight()
        {
            var actualSetting = GetActualResolutionAndRefresRate();
            if (actualSetting.Height > 850) return;

            this.Height -= 100;
        }

        public MainWindow()
        {
            InitializeComponent();
            SetMonitorsList();
            SetWindowHeight();
            //this.timer = new DispatcherTimer();
            //this.timer.Interval = TimeSpan.FromMilliseconds(1000);
            //this.timer.Tick += OnTimerTick;
            //this.timer.Start();


            LoadFromFilePosiibleRefreshrateList();
            Refresh_RefreshText();
            SetTrayFromPossibleRefreshList();

            //tbi.Icon = SystemIcons.Error;
            //var aaa = new Bitmap("f:\\ikona.bmp");
            //IntPtr hicon = aaa.GetHicon();
            //tbi.Icon = System.Drawing.Icon.FromHandle(hicon);
            //tbi.Icon = new Icon(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "ikona2.ico"));

            tbi.Icon = Resource1.ikona2;
            tbi.Visibility = Visibility.Visible;

            //DestroyIcon(newIcon.Handle);

            if (RunAsMinimalized) Hide();
            SetRunSatartup();

            RadioButtinAcualResMode.IsChecked = !RadioButtinAllResMode.IsChecked;
            DirtySetting = false;

            monitorLastInfo = GetMONITORINFOEXW();

            SystemEvents.DisplaySettingsChanged += (s, e) =>
            {

                SetMonitorsList();
                //Console.WriteLine("Monitory się zmieniły!");

                OnTimerTick(s, e);
            };
        }
        List<MonitorInfo> _monitorInfoNamesList = new List<MonitorInfo>();
        private void OnLocationOrSizeChanged(object sender, EventArgs e)
        {

            var actualMonitor = GetMONITORINFOEXW();
            if (monitorLastInfo.szDevice == actualMonitor.szDevice)
            {
                return;
            }
            monitorLastInfo = actualMonitor;
            OnTimerTick(sender, e);
        }


        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        private unsafe void OnTimerTick(object sender, object e)
        {

            var actualSetting = GetActualResolutionAndRefresRate();

            if (actualSetting.FullName.Split('@')[0] == this.textBlockActualRefreshRate.Text.Split('@')[0]
                && actualSetting.MonitorIdName == ((RefreshDataModel)header.Tag).MonitorIdName)
            {
                SetLabelRefreshRateAndHeader(actualSetting);
            }
            else
            {
                SetPossibleRefreshRateList(AllResolutionMode);
                SetLabelRefreshRateAndHeader(actualSetting);
            }
        }

        [DllImport("user32.dll")]
        static extern bool EnumDisplayMonitors(
                IntPtr hdc,
                IntPtr lprcClip,
                MonitorEnumProc lpfnEnum,
                IntPtr dwData);

        delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);


        MONITORINFOEXW monitorLastInfo;
        MONITORINFOEXW GetMONITORINFOEXW()
        {
            // 1. Get the window handle ("HWND" in Win32 parlance)
            WindowInteropHelper helper = new WindowInteropHelper(this);
            IntPtr hwnd = helper.Handle;

            // 2. Get a monitor handle ("HMONITOR") for the window. 
            //    If the window is straddling more than one monitor, Windows will pick the "best" one.
            IntPtr hmonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (hmonitor == IntPtr.Zero)
            {
                throw new Exception("MonitorFromWindow returned NULL ☹");
            }

            // 3. Get more information about the monitor.
            var monitorInfo = new MONITORINFOEXW();
            monitorInfo.cbSize = (uint)Marshal.SizeOf<MONITORINFOEXW>();

            bool bResult = GetMonitorInfoW(hmonitor, ref monitorInfo);
            if (!bResult)
            {
                throw new Exception("GetMonitorInfoW returned FALSE ☹");
            }

            // 4. Get the current display settings for that monitor, which includes the resolution and refresh rate.
            //devMode = new DEVMODEW();
            //devMode.dmSize = (ushort)Marshal.SizeOf<DEVMODEW>();

            return monitorInfo;
        }


        private RefreshDataModel GetActualResolutionAndRefresRate()
        {
            var monitorInfo = GetMONITORINFOEXW();

            bool bResult = EnumDisplaySettingsW(monitorInfo.szDevice, ENUM_CURRENT_SETTINGS, out var devMode);
            if (!bResult)
            {
                throw new Exception("EnumDisplaySettingsW returned FALSE ☹");
            }

            var currentSetting = new RefreshDataModel
            {
                RefreshRate = devMode.dmDisplayFrequency,
                Height = devMode.dmPelsHeight,
                Width = devMode.dmPelsWidth,
                MonitorDisplay = monitorInfo.szDevice,
                MonitorIdName = _monitorInfoNamesList.FirstOrDefault(a => a.DisplayName == monitorInfo.szDevice).IdName
            };

            // Done!
            return currentSetting;
        }

        private RefreshDataModel GetActualResolutionAndRefresRateFromMonitor(string display)
        {

            bool bResult = EnumDisplaySettingsW(display, ENUM_CURRENT_SETTINGS, out var devMode);
            if (!bResult)
            {
                throw new Exception("EnumDisplaySettingsW returned FALSE ☹");
            }

            var currentSetting = new RefreshDataModel
            {
                RefreshRate = devMode.dmDisplayFrequency,
                Height = devMode.dmPelsHeight,
                Width = devMode.dmPelsWidth,
                MonitorDisplay = display,
                MonitorIdName = _monitorInfoNamesList.FirstOrDefault(a => a.DisplayName == display).IdName
            };

            // Done!
            return currentSetting;
        }


        private void SetPossibleRefreshRateList(bool allResolution = false)
        {


            var actualMonSet = GetActualResolutionAndRefresRate();
            uint i = 0;
            //PosiibleRefreshrateList.Clear();

            List<RefreshDataModel> temPList = new List<RefreshDataModel>();

            string actualResolitionandRefresh = string.Format("{0} x {1} @ {2}Hz",
                            actualMonSet.Width, actualMonSet.Height, actualMonSet.RefreshRate);


            while (EnumDisplaySettingsW(actualMonSet.MonitorDisplay, i++, out var devMode))
            {

                if (allResolution || (actualMonSet.Width == devMode.dmPelsWidth && actualMonSet.Height == devMode.dmPelsHeight))
                {
                    var t = new RefreshDataModel
                    {
                        RefreshRate = devMode.dmDisplayFrequency
                    ,
                        Height = devMode.dmPelsHeight,
                        Width = devMode.dmPelsWidth,
                        MonitorDisplay = actualMonSet.MonitorDisplay,
                        MonitorIdName = actualMonSet.MonitorIdName
                    };

                    //probably to remove this if statement (this cause lost choosed item when resolution changes)
                    //if (actualResolitionandRefresh.Split('@')[0] == this.textBlockActualRefreshRate.Text.Split('@')[0])
                    //{

                    //var item = PosiibleRefreshrateList.Where(a => a.RefreshRate == devMode.dmDisplayFrequency
                    //&& a.Width == devMode.dmPelsWidth && a.Height == devMode.dmPelsHeight && a.Monitor == monitorInfo.szDevice).FirstOrDefault();
                    //if (item != null && item.Choosed)
                    //{
                    //    t.Choosed = true;
                    //}
                    //}

                    temPList.Add(t);
                }
            }

            var newList = new List<RefreshDataModel>();

            var eualityComparer = new RefreshDataModelEqualityComparer();
            var chosen = PossibleRefreshrateList.Where(a => a.Choosed).ToList();

            //-------
            //newList.AddRange(temPList);                        
            //newList = newList.Distinct(eualityComparer).ToList();

            //newList.RemoveAll(a=> chosen.Any(b=> eualityComparer.Equals(a, b) ) );
            //newList.AddRange(chosen);
            //-----

            newList = chosen
                .Concat(temPList)
                .Distinct(eualityComparer)
                .ToList();


            PossibleRefreshrateList.Clear();
            newList.Sort(new RefreshDataModelComparer());
            newList.Reverse();
            newList.ForEach(a => PossibleRefreshrateList.Add(a));

            //temPList = temPList.GroupBy(a => a.RefreshRate).Select(a => a.First()).ToList();
            //temPList.Sort(new RefreshDataModelComparer());
            //PosiibleRefreshrateList.Clear();
            //temPList.ForEach(a => PosiibleRefreshrateList.Add(a));

        }

        ObservableCollection<RefreshDataModel> _posiibleRefreshrateList = new ObservableCollection<RefreshDataModel>();

        public ObservableCollection<RefreshDataModel> PossibleRefreshrateList => _posiibleRefreshrateList;


        List<(IntPtr handle, MONITORINFOEXW info)> monitorInfoHandlesList = new List<(IntPtr handle, MONITORINFOEXW info)>();
        public List<string> MonitorsIdNameListString => _monitorInfoNamesList.Select(a => a.IdName).ToList();

        public bool IsDuplicationMode => _monitorInfoNamesList.GroupBy(a => a.DisplayName).Any(a => a.Count() > 1);
        private void SetMonitorsList()
        {
            List<MonitorInfo> monitorsNewList = MonitorsName.GetMonitors();

            monitorInfoHandlesList.Clear();

            bool Callback(IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr data2)
            {
                var info = new MONITORINFOEXW();
                info.cbSize = (uint)Marshal.SizeOf<MONITORINFOEXW>();

                GetMonitorInfoW(hMonitor, ref info);

                monitorInfoHandlesList.Add((hMonitor, info));
                return true;
            }


            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, Callback, IntPtr.Zero);

            IsMoreThenOneMonitor = monitorsNewList.Count > 1;



            if (!_monitorInfoNamesList.Select(a=>a.IdName).SequenceEqual(monitorsNewList.Select(a=>a.IdName)))
            //if (monitorInfoHandlesList.Count != monitorsOldCount)
            {
                _monitorInfoNamesList = monitorsNewList;

                SetProperDisplayNumberInPossibleRefreshrateList();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDuplicationMode)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MonitorsIdNameListString)));
            }
        }

        private void SetProperDisplayNumberInPossibleRefreshrateList()
        {
            foreach (var item in PossibleRefreshrateList)
            {
                var monitor = _monitorInfoNamesList.FirstOrDefault(a => a.IdName == item.MonitorIdName);
                if (monitor != null)
                {
                    item.MonitorDisplay = monitor.DisplayName;
                }
            }
        }
        private int SetResolutionAndFrequerency(RefreshDataModel settingsToSet)
        {

            // var (width, height, refresh, monitor) = GetResAndFreqAndMonitorFromString(settingsToSet);

            bool bResult = EnumDisplaySettingsW(settingsToSet.MonitorDisplay, ENUM_CURRENT_SETTINGS, out var devMode);
            if (!bResult)
            {
                throw new Exception("EnumDisplaySettingsW returned FALSE ☹");
            }

            devMode.dmPelsWidth = settingsToSet.Width;
            devMode.dmPelsHeight = settingsToSet.Height;

            //devMode.dmBitsPerPel = (uint)32;
            devMode.dmDisplayFrequency = settingsToSet.RefreshRate;
            //devMode.dmFields = 0x00400000;
            devMode.dmFields = 0x00080000 | 0x00100000 | 0x00400000;
            //ChangeDisplaySettingsW(ref devMode, 0);
            //ChangeDisplaySettingsW(ref devMode, 0);
            var res = ChangeDisplaySettingsExW(settingsToSet.MonitorDisplay, ref devMode, IntPtr.Zero, 0, IntPtr.Zero);

            // szDevice id string eg "\\\\.\\DISPLAY2" for secod monitor

            // Done!
            return res;
        }




        void OnClose(object sender, CancelEventArgs args)
        {
            tbi.Dispose();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {

            switch (this.WindowState)
            {
                case WindowState.Minimized:
                    Hide();
                    //timer.Stop();
                    break;
                default:
                    //timer.Start();
                    break;
            }
        }

        private void tbi_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            Refresh_RefreshText();
            Show();
        }

        private void tbi_TrayRightMouseDown(object sender, RoutedEventArgs e)
        {
            Refresh_RefreshText();
        }

        private void Button_Click_Refresh(object sender, RoutedEventArgs e)
        {
            //_posiibleRefreshrateList.Clear();
            Refresh_RefreshText();
        }

        private void Button_Click_Save(object sender, RoutedEventArgs e)
        {
            SaveAction();
        }

        void SaveAction()
        {
            SetTrayFromPossibleRefreshList();
            SaveToFile();
            DirtySetting = false;
        }
        void SetTrayFromPossibleRefreshList()
        {
            SetTryItemsFromCollection(PossibleRefreshrateList);
        }

        void RefreshTryList()
        {

            var list = new List<RefreshDataModel>();

            foreach (var item in ContextMenu.Items)
            {
                if (item is MenuItem menuitem && menuitem?.Tag != null)
                {
                    list.Add((RefreshDataModel)((MenuItem)item).Tag);

                }
            }

            SetTryItemsFromCollection(list);
        }

        void SetTryItemsFromCollection(ICollection<RefreshDataModel> list)
        {
            while (ContextMenu.Items.Count > 1)
            {
                ContextMenu.Items.RemoveAt(ContextMenu.Items.Count - 1);
            }

            foreach (var item in list)
            {
                if (item.Choosed)
                {

                    //if (!MonitorInfoListString.Contains(item.MonitorDisplay))
                    //{
                    //    continue;
                    //}

                    if (!_monitorInfoNamesList.Select(a => a.IdName).Contains(item.MonitorIdName))
                    {
                        continue;
                    }

                    var menuItem = new MenuItem();
                    menuItem.Header = item.FullName;

                    //var monitorsList = list.Select(a => a.DisplayNumber).Distinct().ToList();
                    //var isDsplayedMonitorNumber = IsMoreThenOneMonitor || monitorsList.Count > 1 ? true : int.Parse(monitorsList.First()) > 1 ? true : false;

                    menuItem.Header = item.FullName; //item.FullName
                    menuItem.Header += IsMoreThenOneMonitor ? " Disp.: " + item.DisplayNumber : string.Empty;
                    menuItem.Tag = item;
                    menuItem.Click += (a, b) =>
                    {
                        //SetFrequerency(uint.Parse(((MenuItem)a).Tag.ToString()));
                        SetResolutionAndFrequerency((RefreshDataModel)((MenuItem)a).Tag);

                    };
                    ContextMenu.Items.Add(menuItem);

                }
            }
        }
        void SaveToFile()
        {
            List<string> listToSave = new List<string> { RunStartup.ToString(), RunAsMinimalized.ToString(), AllResolutionMode.ToString(), TestTime.ToString(), "<RES>" };

            //listToSave.Add(this.textBlockActualRefreshRate.Text.Split('@')[0]);

            PossibleRefreshrateList.Where(a => a.Choosed).Select(a => a.FullNameWithMonitorDisplayAndName.ToString()).ToList().ForEach(a => listToSave.Add(a));

            // using (var file = new StreamWriter("RefreshRate.cfg"))
            var path = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "RefreshRate.cfg");
            using (var file = new StreamWriter(path))
            {
                listToSave.ForEach(a => file.WriteLine(a));
            }
        }

        void LoadFromFilePosiibleRefreshrateList()
        {
            try
            {
                var path = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "RefreshRate.cfg");

                // using (var file = new StreamReader("RefreshRate.cfg"))
                using (var file = new StreamReader(path))
                {
                    var line = file.ReadLine();
                    RunStartup = bool.Parse(line);

                    line = file.ReadLine();
                    RunAsMinimalized = bool.Parse(line);

                    line = file.ReadLine();
                    AllResolutionMode = bool.Parse(line);

                    line = file.ReadLine();
                    TestTime = int.Parse(line);

                    while (line != "<RES>")
                    {
                        line = file.ReadLine();
                        if (line == null)
                            return;
                    }
                    //line = file.ReadLine();
                    //if (line == GetActualResolutionAndRefresRategString().Split('@')[0])
                    PossibleRefreshrateList.Clear();

                    while (!file.EndOfStream)
                    {
                        line = file.ReadLine();
                        var (Width, Height, Refresh, MonitorDisplay, MonitorName) = GetResAndFreqAndMonitorFromString(line);

                        PossibleRefreshrateList.Add(new RefreshDataModel
                        {
                            RefreshRate = Refresh,
                            Height = Height,
                            Width = Width,
                            MonitorDisplay = MonitorDisplay,
                            MonitorIdName = MonitorName,
                            Choosed = true
                        });
                    }
                    SetProperDisplayNumberInPossibleRefreshrateList();
                }
            }
            catch (Exception ex)
            {

            }
        }

        private async void Button_Click_TestAsync(object sender, RoutedEventArgs e)
        {
            var resSettings = (RefreshDataModel)((Button)sender).DataContext;
            var actualRefreshAndResolution = GetActualResolutionAndRefresRateFromMonitor(resSettings.MonitorDisplay);


            SetResolutionAndFrequerency(resSettings);
            var splitInfo = GetActualResolutionAndRefresRateFromMonitor(resSettings.MonitorDisplay)
                .FullNameWithMonitorDisplayAndName.Split('@');

            var infoString1 = splitInfo[0].Trim() + " @ " + splitInfo[1].Trim();
            var infoString2 = "Display: " + splitInfo[2].Trim().Last();


            //SetLabelRefreshRateAndHeader(infoString);

            StackPanelAll.IsEnabled = false;

            //////////////////////

            SetMonitorsList();

            // znajdź DISPLAY

            var targetDisplay = _monitorInfoNamesList.FirstOrDefault(m => m.IdName == resSettings.MonitorIdName).DisplayName;

            var target = monitorInfoHandlesList.First(m => m.info.szDevice == targetDisplay);

            IntPtr hmonitor = target.handle;

            var rectMon = target.info.rcMonitor;

            var width = rectMon.right - rectMon.left;
            var height = rectMon.bottom - rectMon.top;

            uint dpiX, dpiY;
            GetDpiForMonitor(hmonitor, MonitorDpiType.MDT_EFFECTIVE_DPI, out dpiX, out dpiY);

            var dpiScaleX = dpiX / 96.0;
            var dpiScaleY = dpiY / 96.0;

            /////////////////
            // get dpi
            //var dpiScaleX = VisualTreeHelper.GetDpi(this).DpiScaleX;
            //var dpiScaleY = VisualTreeHelper.GetDpi(this).DpiScaleY;

            // get screen size

            var screenWidth = width / dpiScaleX;
            var screenHeight = height / dpiScaleY;

            //var screenWidth = SystemParameters.PrimaryScreenWidth / dpiScaleX;
            //var screenHeight = SystemParameters.PrimaryScreenHeight / dpiScaleY;


            var popupWidth = Popup.Width;
            var popupHeight = Popup.Height;

            var scaledWidth = popupWidth / dpiScaleX;
            var scaledHeight = popupHeight / dpiScaleY;

            var offsetX = (screenWidth - popupWidth) / 2;
            var offsetY = (screenHeight - popupHeight) / 2;


            Popup.Placement = System.Windows.Controls.Primitives.PlacementMode.Absolute;
            //Popup.HorizontalOffset = offsetX;
            //Popup.VerticalOffset = offsetY;
            Popup.HorizontalOffset = rectMon.left / dpiScaleX + offsetX;
            Popup.VerticalOffset = rectMon.top / dpiScaleY + offsetY;

            //////////////////////////////////


            Popup.IsOpen = true;
            Popup_Label1.Content = "Test: " + infoString1;
            Popup_Label2.Content = infoString2;

            await Task.Run(async () =>
            {

                for (int i = TestTime; i > 0; i--)
                {

                    this.Dispatcher.Invoke(() => { Popup_Label_Counter.Content = i; });
                    await Task.Delay(1000);
                }

            });

            StackPanelAll.IsEnabled = true;

            SetResolutionAndFrequerency(actualRefreshAndResolution);

            SetLabelRefreshRateAndHeader(GetActualResolutionAndRefresRate());
            Popup.IsOpen = false;
        }

        void Refresh_RefreshText()
        {
            var actualSetting = GetActualResolutionAndRefresRate();
            SetLabelRefreshRateAndHeader(actualSetting);
            SetPossibleRefreshRateList(AllResolutionMode);
        }

        void SetLabelRefreshRateAndHeader(RefreshDataModel data)
        {
            this.textBlockActualRefreshRate.Text = data.FullName;
            header.Header = this.textBlockActualRefreshRate.Text;
            header.Header += IsMoreThenOneMonitor ? " Disp.: " + data.DisplayNumber : string.Empty;
            header.Tag = data;
            //header.IsEnabled = false;
        }

        void SetActalRefreshRateAndHeaderLabel()
        {
            var RefreshRate = GetActualResolutionAndRefresRate();

            SetLabelRefreshRateAndHeader(RefreshRate);
        }

        //eg. 1920 x 1600 @ 60Hz"

        private (uint Width, uint Height, uint Refresh, string MonitorDisplay, string MonitorName) GetResAndFreqAndMonitorFromString(string data)
        {

            var split1 = data.Split('@');

            var monitorD = split1[2].Trim();
            var monitorN = split1[3].Trim();
            var refS = split1[1].Substring(0, split1[1].Length - 3).Trim();

            var refU = uint.Parse(refS);

            var heightU = uint.Parse(split1[0].Split('x')[1].Trim());
            var widthU = uint.Parse(split1[0].Split('x')[0].Trim());


            return (widthU, heightU, refU, monitorD, monitorN);

        }

        private void AppSave_Click(object sender, RoutedEventArgs e)
        {
            SaveAction();
        }

        private void AppExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AppHide_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }


        private void CheckBoxInListView_Click(object sender, RoutedEventArgs e)
        {
            DirtySetting = true;
        }

        private void AppAbout_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AboutWindow();

            // Display the dialog box and read the response
            bool? result = dialog.ShowDialog();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        //---------------------------------------------------

        // MonitorFromWindow
        private const uint MONITOR_DEFAULTTONEAREST = 2;

        [DllImport("user32.dll", SetLastError = false)]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int ChangeDisplaySettingsExW(
            string lpszDeviceName,
            ref DEVMODEW lpDevMode,
            IntPtr hwnd,
            int dwflags,
            IntPtr lParam
        );

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.I4)]
        private static extern int ChangeDisplaySettingsW(
            [In, Out]
        ref DEVMODEW lpDevMode,
            [param: MarshalAs(UnmanagedType.U4)]
        uint dwflags);

        // RECT
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        // MONITORINFOEX
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private unsafe struct MONITORINFOEXW
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        // GetMonitorInfo
        [DllImport("user32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfoW(
            IntPtr hMonitor,
            ref MONITORINFOEXW lpmi);

        // EnumDisplaySettings
        private const uint ENUM_CURRENT_SETTINGS = unchecked((uint)-1);



        [DllImport("user32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumDisplaySettingsW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpszDeviceName,
            uint iModeNum,
            out DEVMODEW lpDevMode);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DEVMODEW
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;

            public ushort dmSpecVersion;
            public ushort dmDriverVersion;
            public ushort dmSize;
            public ushort dmDriverExtra;
            public uint dmFields;

            /*public short dmOrientation;
            public short dmPaperSize;
            public short dmPaperLength;
            public short dmPaperWidth;
            public short dmScale;
            public short dmCopies;
            public short dmDefaultSource;
            public short dmPrintQuality;*/
            // These next 4 int fields are a union with the above 8 shorts, but we don't need them right now
            public int dmPositionX;
            public int dmPositionY;
            public uint dmDisplayOrientation;
            public uint dmDisplayFixedOutput;

            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;

            public short dmLogPixels;
            public uint dmBitsPerPel;
            public uint dmPelsWidth;
            public uint dmPelsHeight;

            public uint dmNupOrDisplayFlags;
            public uint dmDisplayFrequency;

            public uint dmICMMethod;
            public uint dmICMIntent;
            public uint dmMediaType;
            public uint dmDitherType;
            public uint dmReserved1;
            public uint dmReserved2;
            public uint dmPanningWidth;
            public uint dmPanningHeight;
        }


        [DllImport("Shcore.dll")]
        static extern int GetDpiForMonitor(
             IntPtr hmonitor,
             MonitorDpiType dpiType,
             out uint dpiX,
             out uint dpiY);

        enum MonitorDpiType
        {
            MDT_EFFECTIVE_DPI = 0,
            MDT_ANGULAR_DPI = 1,
            MDT_RAW_DPI = 2
        }



    }


    public class RefreshDataModelComparer : IComparer<RefreshDataModel>
    {
        public int Compare(RefreshDataModel x, RefreshDataModel y)
        {
            if (x.Width - y.Width != 0)
                return (int)(x.Width - y.Width);
            else if (x.Height - y.Height != 0)
                return (int)(x.Height - y.Height);
            else if (x.RefreshRate - y.RefreshRate != 0)
                return (int)(x.RefreshRate - y.RefreshRate);

            return string.Compare(y.MonitorDisplay, x.MonitorDisplay, StringComparison.Ordinal);
        }
    }


    public class RefreshDataModel
    {
        public uint Width { get; set; }
        public uint Height { get; set; }
        public string MonitorDisplay { get; set; }
        public string MonitorIdName { get; set; }
        public string MonitorName => MonitorNameConverter.ConvertMonitorName(MonitorIdName);
        public string ResolutionName => $"{Width} x {Height}";
        public string FullName => $"{Width} x {Height} @ {RefreshRate} Hz";
        public string FullNameWithMonitorDisplay => $"{Width} x {Height} @ {RefreshRate} Hz @ {MonitorDisplay}";
        public string FullNameWithMonitorDisplayAndName => $"{Width} x {Height} @ {RefreshRate} Hz @ {MonitorDisplay} @ {MonitorIdName}";
        public string FullNameWithMonitorForDisplay => $"{Width} x {Height} @ {RefreshRate} Hz {MonitorDisplay.Last()}";
        public string DisplayNumber => $"{MonitorDisplay.Last()}";
        public uint RefreshRate { get; set; }
        public string RefreshRateName => RefreshRate + " Hz";

        public bool Choosed { get; set; }

    }

    public class RefreshDataModelEqualityComparer : IEqualityComparer<RefreshDataModel>
    {
        public bool Equals(RefreshDataModel x, RefreshDataModel y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return x.Width == y.Width &&
                   x.Height == y.Height &&
                   x.RefreshRate == y.RefreshRate &&
                   x.MonitorIdName == y.MonitorIdName;
        }

        public int GetHashCode(RefreshDataModel obj)
        {
            if (obj == null) return 0;

            unchecked
            {
                int hash = 17;
                hash = hash * 23 + obj.Width.GetHashCode();
                hash = hash * 23 + obj.Height.GetHashCode();
                hash = hash * 23 + obj.RefreshRate.GetHashCode();
                hash = hash * 23 + (obj.MonitorIdName?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }


    public class MonitorNameConverter
    {

        public static string ConvertMonitorName(string name)
        {
            if (name.Length<4 || name[3]!=' ')
            {
                return name;
            }

            var vendor = name.Substring(0, 3);


            if (monitorVendorMap.TryGetValue(vendor, out var fullName))
            {
                return fullName + " " + name.Substring(4);
            }

            return name;
        }

        static Dictionary<string, string> monitorVendorMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
            //     OLD LIST

    //{ "SAM", "Samsung" },
    //{ "LGE", "LG" },
    //{ "LGD", "LG" },
    //{ "DEL", "Dell" },
    //{ "HPN", "HP" },
    //{ "LEN", "Lenovo" },
    //{ "ACI", "Acer" },
    //{ "ACR", "Acer" },
    //{ "ASU", "ASUS" },
    //{ "MSI", "MSI" },
    //{ "GIG", "Gigabyte" },
    //{ "AOC", "AOC" },
    //{ "TOP", "TPV" },
    //{ "BNQ", "BenQ" },
    //{ "VSC", "ViewSonic" },
    //{ "PHL", "Philips" },
    //{ "IVM", "iiyama" },
    //{ "IIY", "iiyama" },
    //{ "EIZ", "EIZO" },
    //{ "SHP", "Sharp" },
    //{ "FUS", "Fujitsu" },
    //{ "NEC", "NEC" },
    //{ "HUA", "Huawei" },
    //{ "XIA", "Xiaomi" },
    //{ "TCL", "TCL" },
    //{ "HIS", "Hisense" },
    //{ "SKW", "Skyworth" },
    //{ "PHC", "Philco" },
    
    //{ "RZR", "Razer" },
    //{ "ALI", "Alienware" }, 
    //{ "CLE", "Clevo" },

    //{ "SEC", "Samsung" },          
    //{ "AUO", "AU Optronics" },      
    //{ "BOE", "BOE" },               
    //{ "CMO", "Chi Mei/Innolux" }, 
    //{ "CMN", "Chi Mei/Innolux" },
    //{ "IVO", "InfoVision" },

    //{ "APP", "Apple" },
    //{ "SON", "Sony" },
    //{ "PAN", "Panasonic" },
    //{ "SNY", "Sony" },
    //


            //           NEW LIST
 { "ABO", "Acer" },
{ "ACE", "Acer" },
{ "ACR", "Acer" },
{ "ACB", "Achieva Shimian" },
{ "ACH", "Achieva Shimian" },
{ "ADI", "ADI" },
{ "ADT", "Advantech" },
{ "AGN", "AG Neovo" },
{ "ALB", "Alba" },
{ "ARG", "Alba" },
{ "CAL", "Albatron" },
{ "ALC", "Alches" },
{ "APO", "ALPD" },
{ "AMZ", "Amazon" },
{ "AMH", "AMH" },
{ "AMT", "AMT" },
{ "AMW", "AMW" },
{ "ANX", "Analogix" },
{ "ACI", "Ancor" },
{ "AOC", "AOC" },
{ "AOP", "AOpen" },
{ "ASM", "Aosiman" },
{ "CYS", "Aosiman" },
{ "APP", "Apple" },
{ "ARM", "Armaggeddon" },
{ "AIC", "Arnos" },
{ "GBR", "Arzopa" },
{ "ASR", "ASRock" },
{ "ASU", "ASUS" },
{ "AUS", "ASUS" },
{ "WWW", "ASUS" },
{ "ATV", "Ativa" },
{ "AUO", "AU Optronics" },
{ "CHR", "AU Optronics" },
{ "DMO", "AU Optronics" },
{ "AVG", "Avegant" },
{ "AVT", "AVerMedia" },
{ "AXM", "AXM" },
{ "AYA", "AYANEO" },
{ "BAL", "Balance" },
{ "BGO", "Bangho" },
{ "BBK", "BBK" },
{ "BKM", "Beike" },
{ "BEK", "Beko" },
{ "MAX", "Belinea" },
{ "BNQ", "BenQ" },
{ "TOL", "Beyond TV" },
{ "HXF", "BlueCase" },
{ "BOE", "BOE" },
{ "BSE", "Bose" },
{ "BRA", "Braview" },
{ "BLS", "BUBALUS" },
{ "BUF", "BUFFALO" },
{ "CAS", "CASIO" },
{ "HAN", "Cbox" },
{ "CCE", "CCE" },
{ "CHH", "Changhong" },
{ "CMO", "Chi Mei" },
{ "CMN", "Chimei Innolux" },
{ "CSW", "China Star" },
{ "CIS", "Cisco" },
{ "CLX", "Claxan" },
{ "CND", "CND" },
{ "COB", "COBY" },
{ "WOR", "COMPAL" },
{ "CPQ", "Compaq" },
{ "CRM", "Corsair" },
{ "CMS", "Cosmos" },
{ "COR", "CPT" },
{ "CPT", "CPT" },
{ "CRU", "CRUA" },
{ "CSO", "CSOT" },
{ "CTL", "CTL" },
{ "CTX", "CTX" },
{ "FSN", "D&T" },
{ "DAE", "Daewoo" },
{ "DWE", "Daewoo" },
{ "DAH", "Dahua" },
{ "DMG", "Dark Matter" },
{ "DGV", "Deco Gear" },
{ "DEL", "Dell" },
{ "LNK", "Dell" },
{ "DON", "DENON" },
{ "LHC", "Denver" },
{ "DXP", "DEXP" },
{ "DTV", "Digital TV" },
{ "DIC", "Dinner" },
{ "DSL", "DisplayLink" },
{ "DNS", "DNS" },
{ "LRN", "Doffler" },
{ "DOS", "Dostyle" },
{ "DST", "Dostyle" },
{ "DSG", "DSGR" },
{ "DTB", "DTEN Board" },
{ "ENC", "Eizo" },
{ "EIA", "Element" },
{ "ELE", "Element" },
{ "EMT", "Element" },
{ "EGA", "Elgato" },
{ "YUN", "Elgato" },
{ "ELO", "Elo Touch" },
{ "ELA", "ELSA" },
{ "ELS", "ELSA" },
{ "EMA", "eMachines" },
{ "EPI", "Envision" },
{ "ENV", "Envision" },
{ "EHJ", "Epson" },
{ "EQD", "EQD" },
{ "HPC", "Erisson" },
{ "EST", "Estecom" },
{ "ETG", "Etigroup" },
{ "EVE", "Eve Spectrum" },
{ "EXN", "Extron" },
{ "YUK", "Flipbook" },
{ "FLU", "Fluid" },
{ "FDR", "Founder" },
{ "FND", "Founder" },
{ "FPT", "FPT" },
{ "FUJ", "Fujitsu" },
{ "FUS", "Fujitsu Siemens" },
{ "FNI", "FUNAI" },
{ "FUR", "Furrion" },
{ "GSV", "G-Story" },
{ "GBA", "GABA" },
{ "GGF", "Game Factor" },
{ "GMX", "GameMax" },
{ "GAM", "GAOMON" },
{ "GTW", "Gateway" },
{ "GWY", "Gateway" },
{ "DVA", "GE" },
{ "GEC", "Gechic" },
{ "QMX", "Gericom" },
{ "GBT", "GIGABYTE" },
{ "GSM", "Goldstar" },
{ "GRE", "GreBear" },
{ "GRN", "Green House" },
{ "GWD", "GreenWood" },
{ "GRU", "Grundig" },
{ "GRR", "Grundig" },
{ "HB@", "H-Buster" },
{ "HAI", "Haier" },
{ "HAR", "Haier" },
{ "HRE", "Haier" },
{ "SRD", "Haier" },
{ "HSG", "Hannspree" },
{ "HSD", "HannStar" },
{ "HSP", "HannStar" },
{ "VJK", "HannStar" },
{ "HSL", "Hansol" },
{ "HCM", "HCL" },
{ "HED", "Hedy" },
{ "HRT", "Hercules" },
{ "HII", "Higer" },
{ "HIK", "Hikvision" },
{ "HIS", "Hisense" },
{ "HSE", "Hisense" },
{ "HEC", "Hitachi" },
{ "HIT", "Hitachi" },
{ "HTC", "Hitachi" },
{ "HKC", "HKC" },
{ "HPN", "HP" },
{ "HWP", "HP" },
{ "HWV", "HUAWEI" },
{ "HAT", "Huion" },
{ "HUI", "Huion" },
{ "HUN", "Huion" },
{ "HUY", "HUYINIUDA" },
{ "HVR", "HVR" },
{ "HIQ", "Hyundai" },
{ "IQT", "Hyundai" },
{ "IBM", "IBM" },
{ "IVM", "Iiyama" },
{ "XSC", "Immer" },
{ "IMP", "Impression" },
{ "INC", "INCA" },
{ "ITR", "INFOTRONIC" },
{ "IVO", "InfoVision" },
{ "IOC", "INNOCN" },
{ "CMI", "InnoLux Display" },
{ "INL", "InnoLux Display" },
{ "INX", "InnoLux Display" },
{ "YCT", "InnoView" },
{ "BBY", "Insignia" },
{ "INS", "Insignia" },
{ "INZ", "Insignia" },
{ "HSJ", "Intehill" },
{ "HKN", "iQual" },
{ "HXV", "iQual" },
{ "HKM", "Japannext" },
{ "JDI", "JDI" },
{ "JEN", "Jean" },
{ "JRP", "JINGLITAI" },
{ "AMR", "JVC" },
{ "JVC", "JVC" },
{ "JXC", "JXC" },
{ "KIV", "Kivi" },
{ "KGN", "Kogan" },
{ "KOS", "KOIOS" },
{ "KOA", "Konka" },
{ "KMR", "Kramer" },
{ "KTC", "KTC" },
{ "LAC", "LaCie" },
{ "LCA", "LaCie" },
{ "LNX", "Lanix" },
{ "LDL", "LDLC" },
{ "LEC", "Lecoo" },
{ "LEN", "Lenovo" },
{ "LEO", "Lenovo" },
{ "LNV", "Lenovo" },
{ "QUA", "Lenovo" },
{ "QWA", "Lenovo" },
{ "LGD", "LG Display" },
{ "LGP", "LG Philips" },
{ "LPL", "LG Philips" },
{ "LTN", "Lite-On" },
{ "LOE", "Loewe" },
{ "LPS", "Lupus Screen" },
{ "MAC", "MacroSilicon" },
{ "MBB", "Maibenben" },
{ "MJI", "Marantz" },
{ "MTX", "Matrox" },
{ "MUS", "MECER" },
{ "MEA", "Medion" },
{ "MEB", "Medion" },
{ "MED", "Medion" },
{ "MEC", "Medion Akoya" },
{ "MEK", "MEK" },
{ "MCE", "Metz" },
{ "Mod", "MFG" },
{ "MSH", "Microsoft" },
{ "MTC", "MiTAC" },
{ "SZM", "MiTAC" },
{ "MEL", "Mitsubishi" },
{ "REG", "Mobile Pixels" },
{ "MPC", "Monoprice" },
{ "MP_", "Monoprice" },
{ "MAG", "MSI" },
{ "MSI", "MSI" },
{ "MST", "MStar" },
{ "NLK", "MStar" },
{ "MUL", "Multilaser" },
{ "NAV", "Nanovision" },
{ "NEC", "NEC" },
{ "NCI", "NECCI" },
{ "NLE", "Newline" },
{ "NSL", "Newskill" },
{ "NEW", "Newsync" },
{ "NFC", "NFREN" },
{ "NIK", "Niko" },
{ "NIX", "Nixeus" },
{ "NTI", "Nixeus" },
{ "NXG", "Nixeus" },
{ "NOA", "NOA VISION" },
{ "NOR", "Norcent" },
{ "NVT", "Novatek" },
{ "MRG", "Nreal Air" },
{ "NUG", "NU" },
{ "NVD", "Nvidia" },
{ "COS", "NVISION" },
{ "NVI", "NVISION" },
{ "OCM", "oCOSMO" },
{ "SYN", "Olevia" },
{ "OLD", "Olidata" },
{ "BDL", "OneMeeting" },
{ "ONK", "Onkyo" },
{ "ONN", "ONN" },
{ "OPT", "Optoma" },
{ "OTM", "Optoma" },
{ "ORN", "Orion" },
{ "OWC", "OWC" },
{ "PKB", "Packard Bell" },
{ "MEI", "Panasonic" },
{ "YLT", "Panasonic" },
{ "NCP", "PANDA" },
{ "PDA", "PANDA" },
{ "PBK", "PCBANK" },
{ "PQA", "PEAQ" },
{ "PEG", "PEGA" },
{ "PEA", "Pegatron" },
{ "GDH", "Philco" },
{ "PHO", "Philco" },
{ "PLC", "Philco" },
{ "PFL", "Philips" },
{ "PFT", "Philips" },
{ "PHA", "Philips" },
{ "PHG", "Philips" },
{ "PHI", "Philips" },
{ "PHL", "Philips" },
{ "PHP", "Philips" },
{ "PHT", "Philips" },
{ "PLT", "PiLot" },
{ "PIO", "Pioneer" },
{ "HYC", "Pixio" },
{ "ICB", "Pixio" },
{ "PNS", "Pixio" },
{ "PXO", "Pixio" },
{ "WAM", "Pixio" },
{ "PTS", "Plain Tree" },
{ "PLN", "Planar" },
{ "PNR", "Planar" },
{ "MKN", "Polaroid" },
{ "POL", "Polaroid" },
{ "NON", "Positivo" },
{ "PCL", "Positivo" },
{ "POS", "Positivo" },
{ "HTB", "Princeton" },
{ "PGS", "Princeton" },
{ "PRT", "Princeton" },
{ "INN", "PRISM+" },
{ "PRW", "Promethean" },
{ "PEB", "Proview" },
{ "QBL", "QBell" },
{ "QDS", "Quanta Display" },
{ "QSM", "Qushimei" },
{ "RAR", "Raritan" },
{ "RZR", "Razer" },
{ "REC", "Reconnect" },
{ "TEU", "Relisys" },
{ "RKU", "Roku" },
{ "ROL", "Rolsen" },
{ "DVM", "RoverScan" },
{ "BTC", "RS" },
{ "RUB", "Rubin" },
{ "RJT", "Ruijiang" },
{ "STK", "S2-Tek" },
{ "STC", "Sampo" },
{ "LGE", "Samsung" },
{ "SAM", "Samsung" },
{ "SDC", "Samsung" },
{ "SEC", "Samsung" },
{ "SEM", "Samsung" },
{ "SIM", "Samsung" },
{ "STN", "Samsung" },
{ "_YM", "Samsung" },
{ "XEC", "SANSUI" },
{ "SAN", "SANYO" },
{ "SPT", "Sceptre Tech" },
{ "SEE", "SEEYOO" },
{ "KDD", "Seiki" },
{ "KDI", "Seiki" },
{ "SEK", "Seiki" },
{ "STI", "Semp Toshiba" },
{ "SVR", "Sensics" },
{ "PCK", "SENSY" },
{ "SHC", "Sharp" },
{ "SHP", "Sharp" },
{ "SII", "Skyworth" },
{ "SKY", "Skyworth" },
{ "SHD", "SmallHD" },
{ "MS_", "Sony" },
{ "SNY", "Sony" },
{ "SOT", "SOTEC" },
{ "SCE", "Sun" },
{ "SUN", "Sun" },
{ "SNN", "SUNNY" },
{ "SPV", "Sunplus" },
{ "SUE", "SuperFrame" },
{ "MSC", "Syscom" },
{ "TAR", "Targa Visionary" },
{ "TAT", "Tatung" },
{ "TCL", "TCL" },
{ "TSD", "TechniMedia" },
{ "PKV", "Thomson" },
{ "TMN", "Thomson" },
{ "TTE", "Thomson" },
{ "TRG", "ThundeRobot" },
{ "TLX", "Tianma XM" },
{ "TPV", "Top Victory" },
{ "TNJ", "Toppoly" },
{ "TOP", "TopView" },
{ "LCD", "Toshiba" },
{ "TOS", "Toshiba" },
{ "TSB", "Toshiba" },
{ "PER", "Turbo-X" },
{ "UMC", "UMC" },
{ "UPV", "UPlusVision" },
{ "UPS", "UpStar" },
{ "XYA", "Valday" },
{ "VLV", "Valve" },
{ "VES", "Vestel" },
{ "IGM", "Videoseven" },
{ "HCD", "ViewSonic" },
{ "VSC", "ViewSonic" },
{ "VTK", "Viotek" },
{ "VBX", "VirtualBox" },
{ "VIT", "Vita" },
{ "IZI", "Vizio" },
{ "VIZ", "Vizio" },
{ "JRY", "VIZTA" },
{ "DUS", "VOXICON" },
{ "WAC", "Wacom" },
{ "HLT", "WaveShare" },
{ "WDE", "Westinghouse" },
{ "WDT", "Westinghouse" },
{ "WEH", "Westinghouse" },
{ "WET", "Westinghouse" },
{ "WIM", "Wimaxit" },
{ "WNX", "Wincor Nixdorf" },
{ "XER", "Xerox" },
{ "GMI", "XGIMI" },
{ "XYE", "Xiangye" },
{ "XMD", "Xiaomi" },
{ "XMI", "Xiaomi" },
{ "XVS", "XVision" },
{ "YAK", "Yakumo" },
{ "YMH", "Yamaha" },
{ "YSI", "Yashi" },
{ "YEY", "Yeyian" },
{ "FAC", "Yuraku" },
{ "ZFO", "Zifro" },
{ "ZRN", "Zoran" },

};
    }

}



