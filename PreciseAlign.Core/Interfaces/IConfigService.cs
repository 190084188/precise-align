using System.Collections.Generic;

namespace PreciseAlign.Core.Interfaces
{
    public interface IConfigService
    {
        /// <summary>
        /// 获取配置的所有相机类型定义。
        /// </summary>
        /// <returns>一个字典，键是相机ID（如"Cam1"），值是相机类型（如"AltairCamera"）。</returns>
        Dictionary<string, string> GetCameraConfigurations();
    }
}