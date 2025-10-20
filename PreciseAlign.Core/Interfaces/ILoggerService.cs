using System.Collections.ObjectModel;

namespace PreciseAlign.Core.Interfaces
{
    /// <summary>
    /// 用于UI绑定的日志条目数据模型。
    /// 这是一个非泛型记录，以避免与其他库中的泛型LogEntry冲突。
    /// </summary>
    public record LogEntry(string Timestamp, string Level, string Message);

    /// <summary>
    /// 提供日志记录服务的接口
    /// </summary>
    public interface ILoggerService
    {
        /// <summary>
        /// 可供UI绑定的日志消息集合
        /// </summary>
        ObservableCollection<LogEntry> Messages { get; }

        void LogInfo(string message);
        void LogDebug(string message);
        void LogError(string message);

        /// <summary>
        /// 记录错误信息，并包含异常详情
        /// </summary>
        void LogError(string message, Exception ex);
    }
}
