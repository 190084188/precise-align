using PreciseAlign.Core.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace PreciseAlign.WPF.Services
{
    public class ConfigService : IConfigService, IProcessConfigService
    {
        private readonly string _path;

        // 用于读取INI文件的Win32 API
        [DllImport("kernel32", CharSet = CharSet.Unicode, EntryPoint = "GetPrivateProfileStringW")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode, EntryPoint = "GetPrivateProfileSectionW")]
        private static extern int GetPrivateProfileSection(string section, byte[] retVal, int size, string filePath);

        /// <summary>
        /// 构造函数，初始化配置文件的路径。
        /// 获取当前执行程序的目录，并组合成配置文件 "Config\Config.ini" 的完整路径。
        /// 如果配置文件存在，则在调试输出中打印该路径。
        /// </summary>
        public ConfigService()
        {
            var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (exePath == null)
            {
                throw new InvalidOperationException("无法获取当前执行程序的目录。");
            }
            _path = Path.Combine(exePath, "Config","Config.ini");
            
        }

        /// <summary>
        /// 获取所有相机的配置信息。
        /// 遍历假设最多5个相机（Cam0 到 Cam4），从配置文件的 "CameraType" 节点读取每个相机的类型。
        /// 如果读取到的相机类型不为空，则将其添加到返回的字典中，键为 "Cam{i}"，值为对应的相机类型。
        /// </summary>
        /// <returns>包含相机编号与对应相机类型的字典</returns>
        public Dictionary<string, string> GetCameraConfigurations()
        {
            var configs = new Dictionary<string, string>();
            for (int i = 0; i <= 4; i++) // 假设最多5个相机
            {
                var key = $"Cam{i}";
                var retVal = new StringBuilder(255);
                GetPrivateProfileString("CameraType", key, "", retVal, 255, _path);
                string cameraType = retVal.ToString();
                Debug.WriteLine($"正在读取配置: Section=CameraType, Key={key}, Value='{cameraType}', Path='{_path}'");
                if (!string.IsNullOrEmpty(cameraType))
                {
                    configs[key] = cameraType;
                }
            }
            return configs;
        }

        /// <summary>
        /// 获取流程步骤与相机ID的映射关系。
        /// 从配置文件的 "ProcessSteps" 节点读取所有键值对，每个键为流程步骤名称，值为以逗号分隔的相机ID列表。
        /// 将这些键值对解析后存入字典并返回，键为流程步骤名称，值为对应的相机ID字符串数组。
        /// </summary>
        /// <returns>包含流程步骤名称与对应相机ID数组的字典</returns>
        public Dictionary<string, string[]> GetProcessStepCameraMapping()
        {
            var mapping = new Dictionary<string, string[]>();
            byte[] buffer = new byte[2048];
            GetPrivateProfileSection("ProcessSteps", buffer, buffer.Length, _path);

            string allKeys = Encoding.Unicode.GetString(buffer).Trim('\0');
            string[] keyValuePairs = allKeys.Split('\0');

            foreach (var keyValuePair in keyValuePairs)
            {
                if (string.IsNullOrWhiteSpace(keyValuePair)) continue;
                string[] parts = keyValuePair.Split('=');
                if (parts.Length == 2)
                {
                    string stepName = parts[0];
                    string[] cameraIds = parts[1].Split(',').Select(id => id.Trim()).ToArray();
                    mapping[stepName] = cameraIds;
                }
            }
            return mapping;
        }
    }
}