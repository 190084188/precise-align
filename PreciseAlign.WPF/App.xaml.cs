using Microsoft.Extensions.DependencyInjection; // 引入DI相关的命名空间
using PreciseAlign.Core.Interfaces;
using PreciseAlign.WPF.Services;
using PreciseAlign.WPF.Services.Camera; // 引入具体服务实现的命名空间
using PreciseAlign.WPF.Services.Communication;
using PreciseAlign.WPF.Services.Vision;
using PreciseAlign.WPF.ViewModels; // 引入ViewModel的命名空间
using PreciseAlign.WPF.Views;      // 引入View的命名空间
using System;
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
            // 1. 注册配置服务和相机工厂
            services.AddSingleton<ConfigService>();
            services.AddSingleton<IConfigService>(provider => provider.GetRequiredService<ConfigService>());
            services.AddSingleton<IProcessConfigService>(provider => provider.GetRequiredService<ConfigService>());
            services.AddSingleton<CameraFactory>();
            // 2. 动态注册相机实例
            // 直接从容器解析出服务来使用
            var tempProvider = services.BuildServiceProvider();
            var configService = tempProvider.GetRequiredService<IConfigService>();
            var cameraFactory = tempProvider.GetRequiredService<CameraFactory>();
            var cameraConfigs = configService.GetCameraConfigurations();
            // config键值对，cameraConfigs是字典
            foreach (var config in cameraConfigs)
            {
                // config.Key 是 "Cam0", "Cam1" 等
                // config.Value 是 "AltairCamera", "SimulatedCamera" 等
                string deviceIndex = System.Text.RegularExpressions.Regex.Match(config.Key, @"\d+").Value;
                int id = int.Parse(deviceIndex);

                services.AddSingleton<ICamera>(provider => cameraFactory.CreateCamera(config.Value, id.ToString()));
            }
            // 3. 注册相机管理服务和其他服务
            services.AddSingleton<ICameraService, CameraService>();
            services.AddTransient<MainViewModel>();
            services.AddSingleton<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var mainWindow = ServiceProvider?.GetService<MainWindow>();
            mainWindow?.Show();
        }
    }

}
