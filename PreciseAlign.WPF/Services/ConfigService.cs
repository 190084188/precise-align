using IniParser;
using IniParser.Model;
using PreciseAlign.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PreciseAlign.WPF.Services
{
    /// <summary>
    /// 使用 ini-parser 库实现的现代化配置服务。
    /// 此类同时实现了通用配置读取接口和特定于流程的配置接口。
    /// </summary>
    public class ConfigService : IConfigService, IProcessConfigService
    {
        private readonly IniData _configData;

        public ConfigService()
        {
            var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                ?? throw new InvalidOperationException("无法获取当前执行程序的目录。");

            string configFilePath = Path.Combine(exePath, "Config", "Config.ini");

            if (!File.Exists(configFilePath))
            {
                throw new FileNotFoundException("配置文件未找到！", configFilePath);
            }

            var parser = new FileIniDataParser();
            _configData = parser.ReadFile(configFilePath);
        }

        #region IConfigService 实现

        /// <summary>
        /// 获取INI文件中指定节(Section)下的所有键值对。
        /// </summary>
        public Dictionary<string, string> GetSection(string section)
        {
            if (_configData.Sections.ContainsSection(section))
            {
                return _configData[section].ToDictionary(kvp => kvp.KeyName, kvp => kvp.Value);
            }
            // 如果节不存在，返回一个空字典，避免调用方出错
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// 获取INI文件中指定节和键的值。
        /// </summary>
        public string GetValue(string section, string key)
        {
            return _configData[section]?[key] ?? string.Empty;
        }

        #endregion

        #region IProcessConfigService 实现

        /// <summary>
        /// 获取流程步骤与相机ID的映射关系。
        /// </summary>
        public Dictionary<string, string[]> GetProcessStepCameraMapping()
        {
            var mapping = new Dictionary<string, string[]>();
            var sectionData = GetSection("ProcessSteps");

            foreach (var kvp in sectionData)
            {
                string stepName = kvp.Key;
                string cameraIdsValue = kvp.Value;

                if (!string.IsNullOrWhiteSpace(cameraIdsValue))
                {
                    // 按逗号分割，并移除每个ID前后的空格
                    string[] cameraIds = cameraIdsValue.Split(',')
                                                       .Select(id => id.Trim())
                                                       .ToArray();
                    mapping[stepName] = cameraIds;
                }
            }
            return mapping;
        }

        // 注意：旧的 GetCameraConfigurations 方法不再需要，
        // 因为 CameraService 现在会使用更通用的 GetSection("Cameras")。
        // 如果为了兼容旧的 IConfigService 接口定义而必须保留，可以这样实现：
        // public Dictionary<string, string> GetCameraConfigurations() => GetSection("Cameras");

        #endregion
    }
}
