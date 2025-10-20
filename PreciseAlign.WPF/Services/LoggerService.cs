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

        public void LogInfo(string message)
        {
            _log.Info(message);
        }

        public void LogDebug(string message)
        {
            _log.Debug(message);
        }

        public void LogError(string message)
        {
            _log.Error(message);
        }

        public void LogError(string message, Exception ex)
        {
            // log4net会自动处理异常的堆栈跟踪信息
            _log.Error(message, ex);
        }
    }
}

