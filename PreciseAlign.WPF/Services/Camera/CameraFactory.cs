using PreciseAlign.Core.Interfaces;
using System;

namespace PreciseAlign.WPF.Services.Camera
{
    /// <summary>
    /// 相机创建工厂，根据类型字符串创建具体的相机实例。
    /// </summary>
    public class CameraFactory
    {
        public ICamera CreateCamera(string cameraType, string cameraId)
        {
            return cameraType switch
            {
                "AltairCamera" => AltairCamera.Create(cameraId),
                "SimulatedCamera" => new SimulatedCamera(cameraId), // 假设SimulatedCamera已实现
                // 未来可以添加 "HikvisionCamera" => new HikvisionCamera(cameraId),
                _ => throw new NotSupportedException($"相机类型 '{cameraType}' 不支持。")
            };
        }
    }
}