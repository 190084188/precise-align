using PreciseAlign.Core.Interfaces;
using System;
using System.IO;
using System.Reflection;

namespace PreciseAlign.WPF.Services.Camera
{
    public class CameraFactory
    {
        public ICamera CreateCamera(string assemblyPath, string typeName, string createMethod, string cameraId)
        {
            try
            {
                // 获取要加载的程序集
                Assembly assembly;
                if (string.IsNullOrEmpty(assemblyPath))
                {
                    // 如果路径为空，则在当前主程序集中查找类型
                    assembly = Assembly.GetExecutingAssembly();
                }
                else
                {
                    // 根据相对路径加载外部DLL
                    string fullPath = Path.Combine(AppContext.BaseDirectory, assemblyPath);
                    if (!File.Exists(fullPath))
                    {
                        throw new FileNotFoundException($"相机插件DLL未找到: {fullPath}");
                    }
                    assembly = Assembly.LoadFrom(fullPath);
                }

                // 从程序集中获取类型
                var type = assembly.GetType(typeName);
                if (type == null)
                {
                    throw new TypeLoadException($"无法从 '{assembly.FullName}' 中找到类型 '{typeName}'。");
                }

                // 根据指定的创建方式，创建实例
                object? instance;
                if (createMethod.Equals("Constructor", StringComparison.OrdinalIgnoreCase))
                {
                    // 使用构造函数创建
                    instance = Activator.CreateInstance(type, cameraId);
                }
                else
                {
                    // 调用静态工厂方法 (例如 AltairCamera.Create)
                    var method = type.GetMethod(createMethod, BindingFlags.Public | BindingFlags.Static);
                    if (method == null)
                    {
                        throw new MissingMethodException($"在类型 '{typeName}' 中未找到公共静态方法 '{createMethod}'。");
                    }
                    // 调用静态方法，参数是 cameraId
                    instance = method.Invoke(null, new object[] { cameraId });
                }

                if (instance is ICamera camera)
                {
                    return camera;
                }

                throw new InvalidCastException($"创建的实例 '{typeName}' 未实现 ICamera 接口。");
            }
            catch (Exception ex)
            {
                // 包装异常，提供更多上下文信息
                throw new ApplicationException($"创建相机ID '{cameraId}' (类型: {typeName}) 失败。", ex);
            }
        }
    }
}