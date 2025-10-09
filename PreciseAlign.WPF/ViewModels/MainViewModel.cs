using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PreciseAlign.Core.Interfaces;
using PreciseAlign.Core.Models;
using PreciseAlign.WPF.Services.Camera;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PreciseAlign.WPF.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ICameraService? _cameraService;
        private readonly List<ICamera> _cameras = new List<ICamera>();
        private readonly IVisionProcessor? _visionProcessor;
        private readonly DispatcherTimer _timer;

        [ObservableProperty]// 右下角当前时间数据
        private string _currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // 用于绑定到UI上Image控件的属性
        [ObservableProperty]
        private ImageSource? _camera1ImageSource;
        [ObservableProperty]
        private ImageSource? _camera2ImageSource;
        [ObservableProperty]
        private ImageSource? _camera3ImageSource;
        [ObservableProperty]
        private ImageSource? _camera4ImageSource;
        [ObservableProperty]
        private ImageSource? _camera5ImageSource;

        // ... 其他服务

        public MainViewModel(ICameraService? cameraService = null, IVisionProcessor? visionProcessor = null)
        {
            _cameraService = cameraService;
            InitializeCameras();
            _visionProcessor = visionProcessor;
            // ...

            // 右下角当前时间数据定时刷新Timer
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }
        private void InitializeCameras()
        {
            var cam0 = _cameraService?.GetCamera("0");
            try
            {
                if (cam0 != null)
                {
                    cam0.ImageReady += (s, e) => UpdateImageSource(e, 1);
                    cam0.Connect();
                    cam0.SetTriggerMode(false);
                    _cameras.Add(cam0);
                }
            }
            catch (Exception ex) { }
            var cam1 = _cameraService?.GetCamera("1");
            try
            {
                if (cam1 != null)
                {
                    cam1.ImageReady += (s, e) => UpdateImageSource(e, 1);
                    cam1.Connect();
                    cam1.SetTriggerMode(false);
                    _cameras.Add(cam1);
                }
            }
            catch (Exception ex) { }
            var cam2 = _cameraService?.GetCamera("2");
            try
            {
                if (cam2 != null)
                {
                    cam2.ImageReady += (s, e) => UpdateImageSource(e, 2);
                    cam2.Connect();
                    cam2.SetTriggerMode(false);
                    _cameras.Add(cam2);
                }
            }
            catch (Exception ex) { }
            var cam3 = _cameraService?.GetCamera("3");
            try
            {
                if (cam3 != null)
                {
                    cam3.ImageReady += (s, e) => UpdateImageSource(e, 2);
                    cam3.Connect();
                    cam3.SetTriggerMode(false);
                    _cameras.Add(cam3);
                }
            }
            catch (Exception ex) { }
            var cam4 = _cameraService?.GetCamera("4");
            try
            {
                if (cam4 != null)
                {
                    cam4.ImageReady += (s, e) => UpdateImageSource(e, 2);
                    cam4.Connect();
                    cam4.SetTriggerMode(false);
                    _cameras.Add(cam4);
                }
            }
            catch (Exception ex) { }
        }
        private void UpdateImageSource(ImageReadyEventArgs e, int cameraIndex)
        {
            var bitmapSource = CreateBitmapSourceFromImageData(e.Image);
            bitmapSource.Freeze();

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                switch (cameraIndex)
                {
                    case 1: Camera1ImageSource = bitmapSource; break;
                    case 2: Camera2ImageSource = bitmapSource; break;
                        // ... 更新其他相机的ImageSource
                }
            });
        }
        private BitmapSource CreateBitmapSourceFromImageData(ImageData imageData)
        {
            var pixelFormat = imageData.PixelFormat == "BGR24" ? PixelFormats.Bgr24 : PixelFormats.Gray8;
            int stride = (imageData.Width * pixelFormat.BitsPerPixel + 7) / 8;
            return BitmapSource.Create(imageData.Width, imageData.Height, 96, 96, pixelFormat, null, imageData.PixelData, stride);
        }

        private void OnCamera1ImageReady(object? sender, ImageReadyEventArgs e)
        {
            // 核心：将相机数据转换为UI图像
            // 由于相机事件可能在后台线程触发，我们需要使用Dispatcher确保在UI线程上更新图像
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Camera1ImageSource = CreateBitmapSourceFromImageData(e.Image);
            });
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