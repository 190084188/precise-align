using PreciseAlign.Core.Interfaces;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PreciseAlign.WPF.Services.Camera
{
    public class CameraService : ICameraService
    {
        private readonly Dictionary<string, ICamera> _cameras;
        private readonly ILoggerService _logger;
        // 构造函数注入 IConfigService 和 CameraFactory
        public CameraService(ILoggerService logger, IConfigService configService, CameraFactory cameraFactory)
        {
            _logger = logger;
            _cameras = new Dictionary<string, ICamera>();
            InitializeCameras(configService, cameraFactory);

        }

        private void InitializeCameras(IConfigService configService, CameraFactory cameraFactory)
        {
            // 1. 获取 [Cameras] 节中定义的所有要创建的相机实例
            var camerasToCreate = configService.GetSection("Cameras");
            _logger.LogDebug("加载Config.ini中Cameras节中要创建的相机实例中...");
            foreach (var camEntry in camerasToCreate)
            {
                try
                {
                    string instanceName = camEntry.Key;       // "Cam0"
                    string pluginKey = camEntry.Value;        // "AltairCam_0"
                    string deviceIndex = Regex.Match(instanceName, @"\d+").Value; // 从 "Cam0" 提取 "0"

                    // 2. 根据插件Key，获取 [CameraPlugins] 节中对应的详细配置
                    var assemblyPath = configService.GetValue("CameraPlugins", $"{pluginKey}.Assembly");
                    var typeName = configService.GetValue("CameraPlugins", $"{pluginKey}.Type");
                    var createMethod = configService.GetValue("CameraPlugins", $"{pluginKey}.CreateMethod");

                    if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(createMethod))
                    {
                        _logger.LogWarning($"相机'{pluginKey}'的配置不完整(类型不完整或缺少创建方法)，已跳过。");
                        continue;
                    }

                    // 3. 调用工厂，使用反射创建相机实例
                    ICamera camera = cameraFactory.CreateCamera(assemblyPath, typeName, createMethod, deviceIndex);
                    _cameras.Add(deviceIndex, camera);
                    // 4. 打印日志
                    _logger.LogDebug($"相机{deviceIndex}: '{pluginKey}'已成功创建。");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"创建相机{camEntry.Key}: '{camEntry.Value}'失败，失败原因: {ex.Message}", ex);
                    // 添加下面这行代码，让程序崩溃并显示完整的异常信息
                    throw new Exception($"创建相机{camEntry.Key}: '{camEntry.Value}'失败，失败原因: {ex.Message}", ex);
                }
            }
        }

        public ICamera? GetCamera(string cameraId)
        {
            _cameras.TryGetValue(cameraId, out var camera);
            return camera;
        }

        public IEnumerable<ICamera> AllCameras => _cameras.Values;
    }
}