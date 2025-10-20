using log4net.Config;
using System.Windows;

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,            //where theme specific resource dictionaries are located
                                                //(used if a resource is not found in the page,
                                                // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly   //where the generic resource dictionary is located
                                                //(used if a resource is not found in the page,
                                                // app, or any theme specific resource dictionaries)
)]

// 添加这一行来加载log4net的配置
// ConfigFile: 指定配置文件名
// Watch: 允许在程序运行时修改配置文件并自动生效
[assembly: XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]