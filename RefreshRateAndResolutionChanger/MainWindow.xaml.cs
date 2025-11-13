using Hardcodet.Wpf.TaskbarNotification;
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
                SetPossibleRefreshRate(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AllResolutionMode)));
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

        public MainWindow()
        {
            InitializeComponent();

            this.timer = new DispatcherTimer();
            this.timer.Interval = TimeSpan.FromMilliseconds(1000);
            this.timer.Tick += OnTimerTick;
            this.timer.Start();

            LoadFromFilePosiibleRefreshrateList();
            Refresh_RefreshText();
            SetTray();

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
        }
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        private unsafe void OnTimerTick(object sender, object e)
        {

            var RefreshRate = GetActualResolutionAndRefresRategString();

            if (RefreshRate.Split('@')[0] == this.textBlockActualRefreshRate.Text.Split('@')[0])
            {
                SetLabelRefreshRateAndHeader(RefreshRate);
            }
            else
            {
                SetPossibleRefreshRate(AllResolutionMode);
                SetLabelRefreshRateAndHeader(RefreshRate);
            }
        }

        MONITORINFOEXW monitorInfo;
        DEVMODEW devMode;
        void SetDEVMODEW_and_MONITORINFOEXW()
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
            monitorInfo = new MONITORINFOEXW();
            monitorInfo.cbSize = (uint)Marshal.SizeOf<MONITORINFOEXW>();

            bool bResult = GetMonitorInfoW(hmonitor, ref monitorInfo);
            if (!bResult)
            {
                throw new Exception("GetMonitorInfoW returned FALSE ☹");
            }

            // 4. Get the current display settings for that monitor, which includes the resolution and refresh rate.
            devMode = new DEVMODEW();
            devMode.dmSize = (ushort)Marshal.SizeOf<DEVMODEW>();
        }


        private string GetActualResolutionAndRefresRategString()
        {
            SetDEVMODEW_and_MONITORINFOEXW();

            bool bResult = EnumDisplaySettingsW(monitorInfo.szDevice, ENUM_CURRENT_SETTINGS, out devMode);
            if (!bResult)
            {
                throw new Exception("EnumDisplaySettingsW returned FALSE ☹");
            }

            // Done!
            return string.Format("{0} x {1} @ {2}Hz", devMode.dmPelsWidth, devMode.dmPelsHeight, devMode.dmDisplayFrequency);
        }


        private void SetPossibleRefreshRate(bool allResolution = false)
        {
            SetDEVMODEW_and_MONITORINFOEXW();

            bool bResult = EnumDisplaySettingsW(monitorInfo.szDevice, ENUM_CURRENT_SETTINGS, out devMode);
            if (!bResult)
            {
                throw new Exception("EnumDisplaySettingsW returned FALSE ☹");
            }

            uint currentHeight = devMode.dmPelsHeight;
            uint currentWidth = devMode.dmPelsWidth;

            uint i = 0;
            //PosiibleRefreshrateList.Clear();

            List<RefreshDataModel> temPList = new List<RefreshDataModel>();

            string actualResolitionandRefresh = string.Format("{0} x {1} @ {2}Hz", devMode.dmPelsWidth, devMode.dmPelsHeight, devMode.dmDisplayFrequency);


            while (EnumDisplaySettingsW(monitorInfo.szDevice, i++, out devMode))
            {

                if (allResolution || (currentWidth == devMode.dmPelsWidth && currentHeight == devMode.dmPelsHeight))
                {
                    var t = new RefreshDataModel
                    {
                        RefreshRate = devMode.dmDisplayFrequency
                    ,
                        Height = devMode.dmPelsHeight,
                        Width = devMode.dmPelsWidth
                    };

                    if (actualResolitionandRefresh.Split('@')[0] == this.textBlockActualRefreshRate.Text.Split('@')[0])
                    {

                        var item = PosiibleRefreshrateList.Where(a => a.RefreshRate == devMode.dmDisplayFrequency
                        && a.Width == devMode.dmPelsWidth && a.Height == devMode.dmPelsHeight).FirstOrDefault();
                        if (item != null && item.Choosed)
                        {
                            t.Choosed = true;
                        }
                    }

                    temPList.Add(t);
                }
            }

            var groups = temPList.GroupBy(a => a.ResolutionName);

            var newList = new List<RefreshDataModel>();

            foreach (var item in groups)
            {
                var reff = item.GroupBy(a => a.RefreshRate).Select(a => a.First()).ToList();
                reff.ForEach(a => newList.Add(a));
            }

            PosiibleRefreshrateList.Clear();
            newList.Sort(new RefreshDataModelComparer());
            newList.Reverse();
            newList.ForEach(a => PosiibleRefreshrateList.Add(a));

            //temPList = temPList.GroupBy(a => a.RefreshRate).Select(a => a.First()).ToList();
            //temPList.Sort(new RefreshDataModelComparer());
            //PosiibleRefreshrateList.Clear();
            //temPList.ForEach(a => PosiibleRefreshrateList.Add(a));

        }

        ObservableCollection<RefreshDataModel> _posiibleRefreshrateList = new ObservableCollection<RefreshDataModel>();

        public ObservableCollection<RefreshDataModel> PosiibleRefreshrateList => _posiibleRefreshrateList;


        private string SetFrequerency(uint hertz)
        {
            SetDEVMODEW_and_MONITORINFOEXW();

            bool bResult = EnumDisplaySettingsW(monitorInfo.szDevice, ENUM_CURRENT_SETTINGS, out devMode);
            if (!bResult)
            {
                throw new Exception("EnumDisplaySettingsW returned FALSE ☹");
            }

            devMode.dmDisplayFrequency = hertz;
            devMode.dmFields = 0x00400000;
            ChangeDisplaySettingsW(ref devMode, 0);


            // Done!
            return string.Format("{0} x {1} @ {2}Hz", devMode.dmPelsWidth, devMode.dmPelsHeight, devMode.dmDisplayFrequency);
        }

        private string SetResolutionAndFrequerency(string data)
        {

            var (width, height, refresh) = GetResAndFreqFromString(data);

            SetDEVMODEW_and_MONITORINFOEXW();

            bool bResult = EnumDisplaySettingsW(monitorInfo.szDevice, ENUM_CURRENT_SETTINGS, out devMode);
            if (!bResult)
            {
                throw new Exception("EnumDisplaySettingsW returned FALSE ☹");
            }

            devMode.dmPelsWidth = width;
            devMode.dmPelsHeight = height;

            //devMode.dmBitsPerPel = (uint)32;
            devMode.dmDisplayFrequency = refresh;
            //devMode.dmFields = 0x00400000;
            devMode.dmFields = 0x00080000 | 0x00100000 | 0x00400000;
            //ChangeDisplaySettingsW(ref devMode, 0);
            ChangeDisplaySettingsW(ref devMode, 0);


            // Done!
            return string.Format("{0} x {1} @ {2}Hz", devMode.dmPelsWidth, devMode.dmPelsHeight, devMode.dmDisplayFrequency);
        }

        // MonitorFromWindow
        private const uint MONITOR_DEFAULTTONEAREST = 2;

        [DllImport("user32.dll", SetLastError = false)]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);


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

        public event PropertyChangedEventHandler PropertyChanged;

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
                    timer.Stop();
                    break;
                default:
                    timer.Start();
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

        void Refresh_RefreshText()
        {
            this.textBlockActualRefreshRate.Text = GetActualResolutionAndRefresRategString();
            header.Header = this.textBlockActualRefreshRate.Text;
            SetPossibleRefreshRate(AllResolutionMode);
        }

        private void Button_Click_Refresh(object sender, RoutedEventArgs e)
        {
            //_posiibleRefreshrateList.Clear();
            Refresh_RefreshText();
        }

        private void Button_Click_Save(object sender, RoutedEventArgs e)
        {
            SetTray();
            Save();
            DirtySetting = false;
        }


        void SetTray()
        {

            while (ContextMenu.Items.Count > 1)
            {
                ContextMenu.Items.RemoveAt(ContextMenu.Items.Count - 1);
            }

            foreach (var item in PosiibleRefreshrateList)
            {
                if (item.Choosed)
                {
                    var menuItem = new MenuItem();
                    menuItem.Header = item.FullName;
                    menuItem.Tag = item.RefreshRate;
                    menuItem.Click += (a, b) =>
                    {
                        //SetFrequerency(uint.Parse(((MenuItem)a).Tag.ToString()));
                        SetResolutionAndFrequerency(((MenuItem)a).Header.ToString());

                    };
                    ContextMenu.Items.Add(menuItem);

                }
            }
        }


        void Save()
        {
            List<string> listToSave = new List<string> { RunStartup.ToString(), RunAsMinimalized.ToString(), AllResolutionMode.ToString(), TestTime.ToString(),  "<RES>" };

            //listToSave.Add(this.textBlockActualRefreshRate.Text.Split('@')[0]);

            PosiibleRefreshrateList.Where(a => a.Choosed).Select(a => a.FullName.ToString()).ToList().ForEach(a => listToSave.Add(a));

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
                    PosiibleRefreshrateList.Clear();

                    while (!file.EndOfStream)
                    {
                        line = file.ReadLine();
                        var (Width, Height, Refresh) = GetResAndFreqFromString(line);

                        PosiibleRefreshrateList.Add(new RefreshDataModel
                        {
                            RefreshRate = Refresh,
                            Height = Height,
                            Width = Width,
                            Choosed = true
                        });
                    }
                }
            }
            catch
            {

            }
        }

        private async void Button_Click_TestAsync(object sender, RoutedEventArgs e)
        {
            var data = (RefreshDataModel)((Button)sender).DataContext;
            var actualRefreshAndResolution = GetActualResolutionAndRefresRategString();


            SetResolutionAndFrequerency(data.FullName);
            SetLabelRefreshRateAndHeader(GetActualResolutionAndRefresRategString());

            StackPanelAll.IsEnabled = false;
            
            //////////////////////

            // get dpi
            var dpiScaleX = VisualTreeHelper.GetDpi(this).DpiScaleX;
            var dpiScaleY = VisualTreeHelper.GetDpi(this).DpiScaleY;
    
            // get screen size
            var screenWidth = SystemParameters.PrimaryScreenWidth / dpiScaleX;
            var screenHeight = SystemParameters.PrimaryScreenHeight / dpiScaleY;
           
         
            var popupWidth = Popup.ActualWidth;
            var popupHeight = Popup.ActualHeight;

            var scaledWidth = popupWidth/ dpiScaleX;
            var scaledHeight = popupHeight / dpiScaleY;

            var offsetX = (screenWidth- popupWidth) / 2;
            var offsetY = (screenHeight - popupHeight) / 2;


            Popup.Placement = System.Windows.Controls.Primitives.PlacementMode.AbsolutePoint;
            Popup.HorizontalOffset = offsetX;
            Popup.VerticalOffset = offsetY;
    
            //////////////////////////////////


            Popup.IsOpen = true;
            Popup_Label1.Content = "Test: " + GetActualResolutionAndRefresRategString();

            await Task.Run(async () =>
            {

                for (int i = TestTime; i > 0; i--)
                {

                    this.Dispatcher.Invoke(() => { Popup_Label2.Content = i; });
                    await Task.Delay(1000);
                }

            });

            StackPanelAll.IsEnabled = true;

            SetResolutionAndFrequerency(actualRefreshAndResolution);

            SetLabelRefreshRateAndHeader(GetActualResolutionAndRefresRategString());
            Popup.IsOpen = false;
        }

        void SetLabelRefreshRateAndHeader(string name)
        {
            this.textBlockActualRefreshRate.Text = name;
            header.Header = this.textBlockActualRefreshRate.Text;
            header.IsEnabled = false;
        }

        void SetActalRefreshRateAndHeaderLabel()
        {
            var RefreshRate = GetActualResolutionAndRefresRategString();

            SetLabelRefreshRateAndHeader(RefreshRate);
        }

        private uint GetActualRefreshRate()
        {
            var refS = GetActualResolutionAndRefresRategString();

            refS = refS.Split('@')[1].Trim();

            refS = refS.Substring(0, refS.Length - 2);

            return uint.Parse(refS);

        }


        //eg. 1920 x 1600 @ 60Hz"
        private (uint Width, uint Height, uint Refresh) GetResAndFreqFromString(string data)
        {

            var split1 = data.Split('@');

            var refS = split1[1].Substring(0, split1[1].Length - 2).Trim();

            var refU = uint.Parse(refS);

            var heightU = uint.Parse(split1[0].Split('x')[1].Trim());
            var widthU = uint.Parse(split1[0].Split('x')[0].Trim());


            return (widthU, heightU, refU);

        }

        private void AppSave_Click(object sender, RoutedEventArgs e)
        {
            Save();
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
    }


    public class RefreshDataModelComparer : IComparer<RefreshDataModel>
    {
        public int Compare(RefreshDataModel x, RefreshDataModel y)
        {
            if (x.Width - y.Width != 0)
                return (int)(x.Width - y.Width);
            else
            if (x.Height - y.Height != 0)
                return (int)(x.Height - y.Height);

            return (int)(x.RefreshRate - y.RefreshRate);
        }
    }


    public class RefreshDataModel
    {
        public uint Width { get; set; }
        public uint Height { get; set; }
        public string ResolutionName => $"{Width} x {Height}";
        public string FullName => $"{Width} x {Height} @ {RefreshRate} Hz";
        public uint RefreshRate { get; set; }
        public string RefreshRateName => RefreshRate + " Hz";

        public bool Choosed { get; set; }

    }
}
