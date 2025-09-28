using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using PreciseAlign.Core.Interfaces;

namespace PreciseAlign.WPF.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ICamera _camera;
        private readonly IVisionProcessor _visionProcessor;
        [ObservableProperty]
        private string currentTime;
        // ... 其他服务

        public MainViewModel(ICamera camera, IVisionProcessor visionProcessor /*...*/)
        {
            _camera = camera;
            _visionProcessor = visionProcessor;
            // ...

            DispatcherTimer timer = new DispatcherTimer
            {
                // 设置计时器每1秒触发一次
                Interval = TimeSpan.FromSeconds(1)
            };

            // 3. 定义计时器触发时执行的事件
            timer.Tick += (sender, e) =>
            {
                // 更新时间属性，UI会自动响应变化
                CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            };

            // 4. 启动计时器
            timer.Start();

            // 立即设置一次初始时间，避免UI启动时该字段为空
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
    }
}
