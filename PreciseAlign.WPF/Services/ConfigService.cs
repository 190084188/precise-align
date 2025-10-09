using PreciseAlign.Core.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace PreciseAlign.WPF.Services
{
    public class ConfigService : IConfigService
    {
        private readonly string _path;

        // 用于读取INI文件的Win32 API
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public ConfigService()
        {
            var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _path = Path.Combine(exePath, "Config","Config.ini");
            if (File.Exists(_path)){ Debug.WriteLine(_path); }
            
        }

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
    }
}