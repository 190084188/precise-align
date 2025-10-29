using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HalconDotNet;
using PreciseAlign.Core.Interfaces;
using PreciseAlign.Core.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Threading;

namespace PreciseAlign.WPF.ViewModels
{
    public interface IMainWindowView
    {
        void UpdateLeftDisplay(HObject image, HObject? results = null);
        void UpdateRightDisplay(HObject image, HObject? results = null);
    }
    public partial class MainViewModel : ObservableObject
    {
        public IMainWindowView? View { get; set; }
        private readonly ICameraService? _cameraService;
        private readonly IVisionProcessor? _visionProcessor;
        private readonly IProcessConfigService _processConfig;
        private readonly ILoggerService _logger;
        private readonly List<ICamera> _allActiveCameras = [];
        private readonly DispatcherTimer _timer;
        private readonly Dictionary<string, string[]> _stepCameraMapping;
        private const int ErrorImageWidth = 640;
        private const int ErrorImageHeight = 480;

        [ObservableProperty]
        private string _currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        [ObservableProperty]
        private string _currentStepName = "ACF_Alignment";
        [ObservableProperty]
        private bool _isAltairCameraPresent;

        // --- 图像和图形的绑定属性 (已重构) ---
        [ObservableProperty]
        private HObject? _leftDisplayImage;
        [ObservableProperty]
        private HObject? _rightDisplayImage;

        // 用于用户交互的ROI
        [ObservableProperty]
        private ObservableCollection<HDrawingObject> _leftDisplayInteractiveGraphics = new();
        [ObservableProperty]
        private ObservableCollection<HDrawingObject> _rightDisplayInteractiveGraphics = new();

        // 用于显示算法静态结果的属性
        [ObservableProperty]
        private HObject? _leftDisplayResultGraphics;
        [ObservableProperty]
        private HObject? _rightDisplayResultGraphics;

        private ICamera? _leftDisplayCamera;
        private ICamera? _rightDisplayCamera;

        public ObservableCollection<LogEntry> LogMessages => _logger.Messages;

        public MainViewModel(ICameraService? cameraService, IVisionProcessor? visionProcessor, IProcessConfigService? processConfig, ILoggerService? logger)
        {
            _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
            _visionProcessor = visionProcessor;
            _processConfig = processConfig ?? throw new ArgumentNullException(nameof(processConfig));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _stepCameraMapping = _processConfig.GetProcessStepCameraMapping();
            InitializeCameras();
            SelectProcessStep(CurrentStepName);
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        private void InitializeCameras()
        {
            _logger.LogInfo("开始初始化所有已配置的相机...");
            if (!_cameraService.AllCameras.Any())
            {
                _logger.LogInfo("配置文件中未找到任何相机。");
                return;
            }

            foreach (var camera in _cameraService.AllCameras)
            {
                try
                {
                    _logger.LogInfo($"正在连接相机: {camera.CameraId}...");
                    camera.Connect();
                    _logger.LogInfo($"相机 {camera.CameraId} 连接成功。");

                    _logger.LogInfo($"为相机 {camera.CameraId} 设置为连续采集模式...");
                    camera.SetTriggerMode(false);
                    _logger.LogInfo($"相机 {camera.CameraId} 已启动连续采集。");
                    _allActiveCameras.Add(camera);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"相机 '{camera.CameraId}' 初始化失败。", ex);
                }
            }
            IsAltairCameraPresent = _allActiveCameras.Any(cam => cam.GetType().Name == "AltairCamera");
            _logger.LogInfo("相机初始化流程结束。");
        }

        [RelayCommand]
        private void SelectProcessStep(string? stepName)
        {
            if (string.IsNullOrEmpty(stepName) || !_stepCameraMapping.ContainsKey(stepName))
            {
                _logger.LogInfo($"工艺步骤 '{stepName}' 无效或未在配置中找到。");
                return;
            }

            CurrentStepName = stepName;
            _logger.LogInfo($"已切换到工艺步骤: {CurrentStepName}");

            UnsubscribeCameraEvents();

            string[] cameraKeysForStep = _stepCameraMapping[stepName];

            if (cameraKeysForStep.Length > 0)
            {
                string leftCamId = Regex.Match(cameraKeysForStep[0], @"\d+").Value;
                SubscribeToCamera(leftCamId, OnLeftCameraImageReady, true);
            }
            else
            {
                _logger.LogInfo($"左相机未分配");
            }

            if (cameraKeysForStep.Length > 1)
            {
                string rightCamId = Regex.Match(cameraKeysForStep[1], @"\d+").Value;
                SubscribeToCamera(rightCamId, OnRightCameraImageReady, false);
            }
            else
            {
                _logger.LogInfo($"右相机未分配");
            }
        }
        private void SubscribeToCamera(string cameraId, EventHandler<ImageReadyEventArgs> handler, bool isLeft)
        {
            var camera = _cameraService.GetCamera(cameraId);
            if (camera != null && camera.IsConnected)
            {
                camera.ImageReady += handler;
                _logger.LogInfo($"已为 {(isLeft ? "左侧" : "右侧")} 显示区域订阅相机 {cameraId} 的图像事件。");
            }
            else
            {
                _logger.LogError($"尝试订阅相机 {cameraId} 失败: 相机未连接或未找到。");
            }
        }

        private void UnsubscribeCameraEvents()
        {
            foreach (var cam in _allActiveCameras)
            {
                cam.ImageReady -= OnLeftCameraImageReady;
                cam.ImageReady -= OnRightCameraImageReady;
            }

            LeftDisplayImage?.Dispose();
            LeftDisplayImage = null;
            RightDisplayImage?.Dispose();
            RightDisplayImage = null;
        }

        [RelayCommand]
        private void ShowGlobalControlPanel()
        {
            var anyCamera = _allActiveCameras.FirstOrDefault();
            if (anyCamera == null)
            {
                _logger.LogInfo("没有活动的相机可以打开控制面板。");
                return;
            }
            try
            {
                var methodInfo = anyCamera.GetType().GetMethod("ShowControlPanel");
                if (methodInfo != null)
                {
                    methodInfo.Invoke(anyCamera, null);
                    Debug.WriteLine("ViewModel 调用 ShowControlPanel 成功。");
                }
                else
                {
                    _logger.LogInfo($"当前相机类型 '{anyCamera.GetType().Name}' 不支持打开控制面板。");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("调用相机控制面板失败。", ex);
            }
        }

        private void OnLeftCameraImageReady(object? sender, ImageReadyEventArgs e)
        {
            var imageForDisplay = e.Image.Clone();
            View?.UpdateLeftDisplay(imageForDisplay);
            ProcessLeftImageAsync(e.Image);
        }

        private void OnRightCameraImageReady(object? sender, ImageReadyEventArgs e)
        {
            var imageForDisplay = e.Image.Clone();
            View?.UpdateRightDisplay(imageForDisplay);
            ProcessRightImageAsync(e.Image); // 保持注释，或创建对应方法
        }

        private async void ProcessLeftImageAsync(HImage image)
        {
            if (_visionProcessor == null)
            {
                image.Dispose();
                return;
            }

            var result = await _visionProcessor.ProcessImageAsync(image, CurrentStepName);

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // 1. 释放旧的图像和静态结果
                LeftDisplayImage?.Dispose();
                LeftDisplayResultGraphics?.Dispose(); // 释放旧的 HObject 结果

                // 2. 赋新值给绑定属性。这将触发HImageWindow的FullRedraw()
                // 从而确保在一个干净的画布上显示最终结果。
                LeftDisplayImage = result.ProcessedImage;       // 这是处理过的图像
                LeftDisplayResultGraphics = result.ResultGraphics; // 这是识别到的特征等结果
            });
        }

        private async void ProcessRightImageAsync(HImage image)
        {
            if (_visionProcessor == null)
            {
                image.Dispose();
                return;
            }
            var result = await _visionProcessor.ProcessImageAsync(image, CurrentStepName);
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                RightDisplayImage?.Dispose();
                RightDisplayResultGraphics?.Dispose();
                RightDisplayImage = result.ProcessedImage;
                RightDisplayResultGraphics = result.ResultGraphics;
            });
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        [RelayCommand]
        private async Task StartAlignment()
        {
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