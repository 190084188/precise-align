using log4net;
using PreciseAlign.Core.Interfaces;
using System.Collections.ObjectModel;
using System.Reflection;

namespace PreciseAlign.WPF.Services
{
    /// <summary>
    /// 使用log4net库实现的日志服务
    /// </summary>
    public class LoggerService : ILoggerService
    {
        // 获取一个log4net记录器实例
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// 从自定义的UI Appender中获取日志集合
        /// </summary>
        public ObservableCollection<LogEntry> Messages => ObservableCollectionAppender.LogEvents;

        /// <summary>
        /// 用于记录应用程序正常的运行流程和重要状态信息。通常用于生产环境来跟踪系统的行为。例如：程序启动/关闭、用户登录成功、收到API请求。
        /// </summary>
        public void LogInfo(string message)
        {
            _log.Info("[信息]:"+message);
        }

        /// <summary>
        /// 输出详细的调试信息，例如：输入输出参数、关键变量的值、方法进入和退出。
        /// </summary>
        public void LogDebug(string message)
        {
            _log.Debug("[调试]:"+message);
        }

        /// <summary>
        /// 输出错误异常信息，例如：程序异常、数据库连接失败、文件读写失败。
        /// </summary>
        /// <param name="message"></param>
        public void LogError(string message)
        {
            _log.Error("[错误]:" + message);
        }

        /// <summary>
        /// 输出附带错误类型的错误异常信息，例如：FileNotFoundException等等。
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public void LogError(string message, Exception ex)
        {
            // log4net会自动处理异常的堆栈跟踪信息
            _log.Error("[错误]:"+message, ex);
        }

        /// <summary>
        /// 输出警告事件，例如：使用了默认配置、重试了某个操作、临近资源阈值。
        /// </summary>
        public void LogWarning(string message)
        {
            _log.Warn("[警告]:"+message);
        }
    }
}

