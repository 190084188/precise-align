using PreciseAlign.WPF.ViewModels;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
namespace PreciseAlign.WPF.Views
{
    public partial class MainWindow : Window
    {
        private bool _isLightTheme = true;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var themeUri = _isLightTheme
                    ? "/Themes/DarkTheme.xaml"
                    : "/Themes/LightTheme.xaml";

                Debug.WriteLine($"Attempting to switch to theme: {themeUri}");

                var newTheme = new ResourceDictionary
                {
                    Source = new Uri(themeUri, UriKind.Relative)
                };

                System.Windows.Application.Current.Resources.MergedDictionaries.Clear();
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(newTheme);

                ThemeToggleButton.Content = _isLightTheme ? "🌙" : "☀️";
                _isLightTheme = !_isLightTheme;

                Debug.WriteLine("Theme switched successfully.");
            }
            catch (Exception ex)
            {
                // 如果文件没找到或XAML解析错误，会在这里捕获到异常
                Debug.WriteLine($"Error switching theme: {ex.Message}");
                System.Windows.MessageBox.Show($"切换主题失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.Dispose();
            }
        }

        #region 禁用双击图标关闭程序
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // 获取窗口句柄并添加消息钩子
            if (PresentationSource.FromVisual(this) is HwndSource source)
            {
                source.AddHook(WndProc);
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // 系统命令消息
            if (msg == 0x0112)
            {
                int command = wParam.ToInt32();

                // 由双击窗口图标触发的特定系统命令
                // SC_CLOSE (0xF060) | HTSYSMENU (3)
                // SC_MOUSEMENU (0xF090) | HTSYSMENU (3)
                // 拦截关闭命令和双击图标命令
                if (command == 0xF063 || command == 0xF093)
                {
                    // 阻止默认行为
                    handled = true;

                    return IntPtr.Zero;
                }
            }

            return IntPtr.Zero;
        }
        #endregion
    }
}