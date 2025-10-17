using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HalconDotNet;
using PreciseAlign.Core.Interfaces;
using PreciseAlign.Core.Models;
using PreciseAlign.WPF.Services.Camera;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

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
        [ObservableProperty] 
        private HObject? _leftDisplayImage;
        [ObservableProperty]
        private HObject? _rightDisplayImage;
        // 为叠加图形也创建绑定属性
        [ObservableProperty]
        private HObject? _leftDisplayGraphics;
        [ObservableProperty] 
        private HObject? _rightDisplayGraphics;

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
            IsAltairCameraPresent = _allActiveCameras.Any(cam => cam.GetType().Name == "AltairCamera");
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
            LeftDisplayImage = null; 
            RightDisplayImage = null;

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
            if (anyCamera == null) return;

            // 使用反射来调用特定方法，避免强类型耦合
            try
            {
                // 查找名为 ShowControlPanel 的公共实例方法
                var methodInfo = anyCamera.GetType().GetMethod("ShowControlPanel");
                if (methodInfo != null)
                {
                    // 如果找到了，就调用它
                    methodInfo.Invoke(anyCamera, null);
                    Debug.WriteLine("ViewModel 调用 ShowControlPanel 成功。");
                }
                else
                {
                    Debug.WriteLine($"当前相机类型 '{anyCamera.GetType().Name}' 不支持打开控制面板。");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"调用 ShowControlPanel 失败: {ex.Message}");
            }
        }

        private void OnLeftCameraImageReady(object? sender, ImageReadyEventArgs e)
        {
            var imageForDisplay = e.Image.Clone();
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // 释放上一张图像的内存
                LeftDisplayImage?.Dispose();
                LeftDisplayImage = imageForDisplay;
            });

            // 之后可以异步处理图像，而不阻塞UI
            ProcessLeftImageAsync(e.Image);
        }

        private void OnRightCameraImageReady(object? sender, ImageReadyEventArgs e)
        {
            // 与左侧相机逻辑类似
            var imageForDisplay = e.Image.Clone();
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                RightDisplayImage?.Dispose();
                RightDisplayImage = imageForDisplay;
            });
            // ProcessRightImageAsync(e.Image);
        }
        private async void ProcessLeftImageAsync(HImage image)
        {
            if (_visionProcessor == null)
            {
                image.Dispose(); // 如果不处理，也要释放
                return;
            }
            var result = await _visionProcessor.ProcessImageAsync(image, CurrentStepName);
            // result.ProcessedImage 和 result.ResultGraphics 已经被Clone并且原始image被释放
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                LeftDisplayImage?.Dispose();
                LeftDisplayGraphics?.Dispose();
                LeftDisplayImage = result.ProcessedImage;
                LeftDisplayGraphics = result.ResultGraphics;
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