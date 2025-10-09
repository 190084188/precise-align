using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PreciseAlign.Core.Interfaces;
using System.Windows.Threading;

namespace PreciseAlign.WPF.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ICamera? _camera;
        private readonly IVisionProcessor? _visionProcessor;
        private readonly DispatcherTimer _timer;

        [ObservableProperty]
        // 右下角当前时间数据
        private string _currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        // ... 其他服务

        public MainViewModel(ICamera? camera = null, IVisionProcessor? visionProcessor = null /*...*/)
        {
            _camera = camera;
            _visionProcessor = visionProcessor;
            // ...

            // 右下角当前时间数据定时刷新Timer
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
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
            //清理刷新时间的Timer
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= OnTimerTick;
            }
        }
    }
}