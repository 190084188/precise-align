using PreciseAlign.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PreciseAlign.WPF.Services.Camera
{
    public class CameraService : ICameraService
    {
        private readonly Dictionary<string, ICamera> _cameras;

        // 构造函数注入 IConfigService 和 CameraFactory
        public CameraService(IConfigService configService, CameraFactory cameraFactory)
        {
            _cameras = new Dictionary<string, ICamera>();
            InitializeCameras(configService, cameraFactory);
        }

        private void InitializeCameras(IConfigService configService, CameraFactory cameraFactory)
        {
            // 1. 获取 [Cameras] 节中定义的所有要创建的相机实例
            var camerasToCreate = configService.GetSection("Cameras");

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
                        Console.WriteLine($"[警告] 相机插件 '{pluginKey}' 的配置不完整，已跳过。");
                        continue;
                    }

                    // 3. 调用工厂，使用反射创建相机实例
                    ICamera camera = cameraFactory.CreateCamera(assemblyPath, typeName, createMethod, deviceIndex);
                    _cameras.Add(deviceIndex, camera);

                    Console.WriteLine($"[成功] 相机 '{deviceIndex}' (类型: {typeName}) 已成功创建。");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[错误] 初始化相机 '{camEntry.Key}' 失败: {ex.Message}");
                    // 在实际应用中，这里应该使用日志记录，并可能弹出错误提示
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