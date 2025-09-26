using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using PreciseAlign.Core.Interfaces;

namespace PreciseAlign.WPF.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ICamera _camera;
        private readonly IVisionProcessor _visionProcessor;
        // ... 其他服务

        public MainViewModel(ICamera camera, IVisionProcessor visionProcessor /*...*/)
        {
            _camera = camera;
            _visionProcessor = visionProcessor;
            // ...
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
