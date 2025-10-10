using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PreciseAlign.Core.Interfaces;
using PreciseAlign.Core.Models;
using PreciseAlign.WPF.Services.Camera;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Text.RegularExpressions;

namespace PreciseAlign.WPF.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ICameraService? _cameraService;
        private readonly IVisionProcessor? _visionProcessor;
        private readonly IProcessConfigService _processConfig;
        private readonly List<ICamera> _allActiveCameras = new List<ICamera>();
        private readonly DispatcherTimer _timer;

        // --- 状态属性 ---
        [ObservableProperty]// 右下角当前时间数据
        private string _currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        [ObservableProperty] 
        private string _currentStepName = "ACF_Alignment";
        [ObservableProperty] 
        private bool _isAltairCameraPresent;

        // --- 两个显示区域的界面绑定属性 ---
        [ObservableProperty] private ImageSource? _leftDisplayImageSource;
        [ObservableProperty] private ImageSource? _rightDisplayImageSource;

        private readonly Dictionary<string, string[]> _stepCameraMapping;
        private ICamera? _leftDisplayCamera;
        private ICamera? _rightDisplayCamera;

        public MainViewModel(ICameraService? cameraService = null, IVisionProcessor? visionProcessor = null, IProcessConfigService? processConfig=null)
        {
            _cameraService = cameraService;
            _visionProcessor = visionProcessor;
            _processConfig = processConfig;
            _stepCameraMapping = _processConfig.GetProcessStepCameraMapping();

            InitializeCameras();
            SelectProcessStep(CurrentStepName);
            // 右下角当前时间数据定时刷新Timer
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        private void InitializeCameras()
        {
            foreach (var camera in _cameraService.AllCameras)
            {
                try
                {
                    camera.SetTriggerMode(false);
                    _allActiveCameras.Add(camera);
                    Debug.WriteLine($"Camera '{camera.CameraId}' initialized successfully.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Camera '{camera.CameraId}' failed to initialize: {ex.Message}");
                }
            }
            IsAltairCameraPresent = _allActiveCameras.Any(cam => cam is AltairCamera);
        }

        [RelayCommand]
        private void SelectProcessStep(string? stepName)
        {
            if (string.IsNullOrEmpty(stepName) || !_stepCameraMapping.ContainsKey(stepName)) return;

            CurrentStepName = stepName;

            // 与先前显示相机解除事件处理程序绑定
            if (_leftDisplayCamera != null) _leftDisplayCamera.ImageReady -= OnLeftCameraImageReady;
            if (_rightDisplayCamera != null) _rightDisplayCamera.ImageReady -= OnRightCameraImageReady;

            // 清空显示区域图像数据
            LeftDisplayImageSource = null;
            RightDisplayImageSource = null;

            // 从新的步骤中获取相机ID
            string[] cameraKeysForStep = _stepCameraMapping[stepName];

            // 注册并绑定新的显示相机事件处理程序

            if (cameraKeysForStep.Length > 0)
            {
                // 从 "Cam0" 中提取数字 "0"
                string deviceIndex = Regex.Match(cameraKeysForStep[0], @"\d+").Value;
                _leftDisplayCamera = _cameraService.GetCamera(deviceIndex);
                if (_leftDisplayCamera != null)
                {
                    _leftDisplayCamera.ImageReady += OnLeftCameraImageReady;
                }
            }

            // 5. 如果当前步骤的相机个数大于1，为右侧画面的相机注册并绑定新的显示相机事件处理程序
            if (cameraKeysForStep.Length > 1)
            {
                string deviceIndex = Regex.Match(cameraKeysForStep[1], @"\d+").Value;
                _rightDisplayCamera = _cameraService.GetCamera(deviceIndex);
                if (_rightDisplayCamera != null)
                {
                    _rightDisplayCamera.ImageReady += OnRightCameraImageReady;
                }
            }
        }

        [RelayCommand]
        private void ShowGlobalControlPanel()
        {
            var anyCamera = _allActiveCameras.FirstOrDefault();
            // 检查 firstCamera 的真实类型是否为 AltairCamera
            if (anyCamera is AltairCamera altairCam)
            {
                // 如果是，就安全地调用其独有的 showControlPanel 方法
                altairCam.ShowControlPanel();
                Debug.WriteLine("viewmodel调用showControlPanel");

            }
            else
            {
                Debug.WriteLine("当前相机不支持打开控制面板。");
            }
        }

        private void OnLeftCameraImageReady(object? sender, ImageReadyEventArgs e)
        {
            var bitmap = CreateBitmapSourceFromImageData(e.Image);
            bitmap.Freeze();
            System.Windows.Application.Current.Dispatcher.Invoke(() => LeftDisplayImageSource = bitmap);
        }

        private void OnRightCameraImageReady(object? sender, ImageReadyEventArgs e)
        {
            var bitmap = CreateBitmapSourceFromImageData(e.Image);
            bitmap.Freeze();
            System.Windows.Application.Current.Dispatcher.Invoke(() => RightDisplayImageSource = bitmap);
        }

        private static BitmapSource CreateBitmapSourceFromImageData(ImageData imageData)
        {
            var pf = imageData.PixelFormat == "BGR24" ? PixelFormats.Bgr24 : PixelFormats.Gray8;
            int stride = (imageData.Width * pf.BitsPerPixel + 7) / 8;
            return BitmapSource.Create(imageData.Width, imageData.Height, 96, 96, pf, null, imageData.PixelData, stride);
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