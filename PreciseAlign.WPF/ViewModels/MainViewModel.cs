using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PreciseAlign.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace PreciseAlign.WPF.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ICamera? _camera;
        private readonly IVisionProcessor? _visionProcessor;
        private readonly DispatcherTimer _timer;

        [ObservableProperty]
        private string _currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        // ... 其他服务

        public MainViewModel(ICamera? camera = null, IVisionProcessor? visionProcessor = null /*...*/)
        {
            _camera = camera;
            _visionProcessor = visionProcessor;
            // ...
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

            // 使用 lambda 表达式订阅
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }
        private void OnTimerTick(object? sender, EventArgs e)
        {
            CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        [RelayCommand]
        private async Task StartAlignment()
        {
            // 这里是核心工作流的入口
            // 1. 触发相机
            // 2. 处理图像
            // 3. 计算坐标
            // 4. 发送给PLC
        }
        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= OnTimerTick;
            }
        }
    }
}