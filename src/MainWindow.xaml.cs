using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace DesktopClockOverlay
{
    public partial class MainWindow : Window
    {
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int GWL_EXSTYLE = -20;
        private const uint WM_SPAWN_WORKER = 0x052C;
        public string CustomText => _customText;

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private DispatcherTimer _timer;
        private string _customText = "";
        private NotifyIcon _trayIcon;
        private SettingsWindow _settingsWindow;

        public MainWindow()
        {
            InitializeComponent();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) =>
            {
                ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
                CustomTextBlock.Text = _customText;
            };

            _timer.Start();

            SetupTrayIcon();
        }
        private void SetupTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                Text = "Clock Overlay",
                Icon = System.Drawing.SystemIcons.Information,
                Visible = true
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Einstellungen", null, (s, e) => ShowSettingsWindow());
            contextMenu.Items.Add("Beenden", null, (s, e) =>
            {
                _trayIcon.Visible = false;
                System.Windows.Application.Current.Shutdown();
            });

            _trayIcon.ContextMenuStrip = contextMenu;
            _trayIcon.DoubleClick += (s, e) => ShowMainWindow();
        }

        private void ShowSettingsWindow()
        {
            if (_settingsWindow == null)
            {
                _settingsWindow = new SettingsWindow(this);
                _settingsWindow.Closed += (s, e) => _settingsWindow = null;
            }

            _settingsWindow.Show();
            _settingsWindow.Activate();
        }

        private void ShowMainWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            IntPtr hwnd = helper.Handle;

            // Spawn a WorkerW window
            IntPtr progman = FindWindow("Progman", null);
            SendMessage(progman, WM_SPAWN_WORKER, IntPtr.Zero, IntPtr.Zero);

            // Find the correct WorkerW window that contains SHELLDLL_DefView
            IntPtr workerw = IntPtr.Zero;

            // Enumerate all WorkerW windows
            IntPtr temp = IntPtr.Zero;
            do
            {
                temp = FindWindowEx(IntPtr.Zero, temp, "WorkerW", null);
                if (temp != IntPtr.Zero)
                {
                    IntPtr shellView = FindWindowEx(temp, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (shellView != IntPtr.Zero)
                    {
                        // This WorkerW contains the desktop icons - we want the one BEHIND it
                        workerw = FindWindowEx(IntPtr.Zero, temp, "WorkerW", null);
                        break;
                    }
                }
            } while (temp != IntPtr.Zero);

            // If we found the right WorkerW, set our window as its child
            if (workerw != IntPtr.Zero)
            {
                SetParent(hwnd, workerw);
            }
            else
            {
                // Fallback: try to set parent to Progman directly
                SetParent(hwnd, progman);
            }

            // Make window click-through
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
        }

        // Methoden zum Update durch das SettingsWindow

        public void UpdateCustomText(string text)
        {
            _customText = string.IsNullOrWhiteSpace(text) ? "" : text.Trim();
        }

        public void UpdateClockStyle(string fontFamily, double fontSize, SolidColorBrush color)
        {
            ClockText.FontFamily = new FontFamily(fontFamily);
            ClockText.FontSize = fontSize;
            ClockText.Foreground = color;

            // Also update the custom text styling
            CustomTextBlock.FontFamily = new FontFamily(fontFamily);
            CustomTextBlock.FontSize = fontSize * 0.5; // Make it half the size of the clock
            CustomTextBlock.Foreground = color;
        }

        public void UpdateClockPosition(double left, double top)
        {
            this.Left = left;
            this.Top = top;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}
