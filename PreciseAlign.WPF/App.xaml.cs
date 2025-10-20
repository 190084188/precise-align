using Microsoft.Extensions.DependencyInjection; // 引入DI相关的命名空间
using PreciseAlign.Core.Interfaces;
using PreciseAlign.WPF.Services;
using PreciseAlign.WPF.Services.Camera; // 引入具体服务实现的命名空间
using PreciseAlign.WPF.Services.Vision;
using PreciseAlign.WPF.ViewModels; // 引入ViewModel的命名空间
using PreciseAlign.WPF.Views;      // 引入View的命名空间
using System.Windows;

namespace PreciseAlign.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // 1. 注册基础服务和工厂
            services.AddSingleton<ConfigService>();
            services.AddSingleton<IConfigService>(provider => provider.GetRequiredService<ConfigService>());
            services.AddSingleton<IProcessConfigService>(provider => provider.GetRequiredService<ConfigService>());
            // 注册日志服务
            services.AddSingleton<ILoggerService, LoggerService>();
            services.AddSingleton<IVisionProcessor, HalconVisionProcessor>();

            services.AddSingleton<CameraFactory>();
            // 注册视觉处理器
            // 2. 注册 CameraService (单例)。
            //    DI容器会自动将上面注册的 IConfigService 和 CameraFactory 注入给它。
            services.AddSingleton<ICameraService, CameraService>();

            // 3. 注册ViewModel和View
            services.AddSingleton<MainWindow>();
            services.AddTransient<MainViewModel>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var mainWindow = ServiceProvider?.GetService<MainWindow>();
            mainWindow?.Show();
        }
    }

}
