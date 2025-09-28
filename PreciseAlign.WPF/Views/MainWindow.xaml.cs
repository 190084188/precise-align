using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PreciseAlign.WPF.Views
{
    public partial class MainWindow : Window
    {
        private bool _isLightTheme = false;

        public MainWindow()
        {
            InitializeComponent();
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

                Application.Current.Resources.MergedDictionaries.Clear();
                Application.Current.Resources.MergedDictionaries.Add(newTheme);

                ThemeToggleButton.Content = _isLightTheme ? "🌙" : "☀️";
                _isLightTheme = !_isLightTheme;

                Debug.WriteLine("Theme switched successfully.");
            }
            catch (Exception ex)
            {
                // 如果文件没找到或XAML解析错误，会在这里捕获到异常
                Debug.WriteLine($"Error switching theme: {ex.Message}");
                MessageBox.Show($"切换主题失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}