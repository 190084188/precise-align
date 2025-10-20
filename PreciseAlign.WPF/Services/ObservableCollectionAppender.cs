using log4net.Appender;
using log4net.Core;
using PreciseAlign.Core.Interfaces;
using System.Collections.ObjectModel;

namespace PreciseAlign.WPF.Services
{
    /// <summary>
    /// 一个自定义的log4net Appender，它将日志事件写入一个静态的ObservableCollection中，
    /// 以便UI可以绑定并实时显示日志。
    /// </summary>
    public class ObservableCollectionAppender : AppenderSkeleton
    {
        /// <summary>
        /// 静态集合，用于存储所有日志事件。
        /// </summary>
        public static readonly ObservableCollection<LogEntry> LogEvents = new();

        /// <summary>
        /// log4net调用此方法来追加日志事件。
        /// </summary>
        protected override void Append(LoggingEvent loggingEvent)
        {
            var entry = new LogEntry(
                loggingEvent.TimeStamp.ToString("HH:mm:ss"),
                loggingEvent.Level.DisplayName,
                loggingEvent.RenderedMessage ?? string.Empty
            );

            // 必须使用Dispatcher在UI线程上更新集合
            System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                // 为防止内存无限增长，可以限制日志条数
                if (LogEvents.Count > 200)
                {
                    LogEvents.RemoveAt(0);
                }
                LogEvents.Add(entry);
            });
        }
    }
}
