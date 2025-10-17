using System.Collections.Generic;

namespace PreciseAlign.Core.Interfaces
{
    /// <summary>
    /// 提供通用的配置读取功能。
    /// </summary>
    public interface IConfigService
    {
        /// <summary>
        /// 获取INI文件中指定节(Section)下的所有键值对。
        /// </summary>
        /// <param name="section">节的名称，例如 "Cameras"。</param>
        /// <returns>一个包含该节所有键值对的字典。</returns>
        Dictionary<string, string> GetSection(string section);

        /// <summary>
        /// 获取INI文件中指定节和键的值。
        /// </summary>
        /// <param name="section">节的名称，例如 "CameraPlugins"。</param>
        /// <param name="key">键的名称，例如 "AltairCam_0.Assembly"。</param>
        /// <returns>对应的字符串值。</returns>
        string GetValue(string section, string key);
    }
}