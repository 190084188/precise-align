using PreciseAlign.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreciseAlign.Core.Interfaces
{
    public interface ICamera : IDisposable
    {
        string CameraId { get; }
        bool IsConnected { get; }

        /// <summary>
        /// (建议新增) 获取或设置相机曝光时间 (单位: 微秒)
        /// </summary>
        double Exposure { get; set; }

        /// <summary>
        /// (建议新增) 获取或设置相机增益
        /// </summary>
        double Gain { get; set; }

        /// <summary>
        /// 当新图像准备好时触发。事件参数现在是通用的 ImageData。
        /// </summary>
        event EventHandler<ImageReadyEventArgs>? ImageReady;

        void Connect();
        void Disconnect();
        void SetTriggerMode(bool isTriggerMode);

        /// <summary>
        /// 异步触发一次采图。实现者负责将SDK的图像格式转换为通用的 ImageData。
        /// </summary>
        void GrabOneAsync();
    }
}