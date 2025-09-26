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
        /// 当新图像准备好时触发。事件参数现在是通用的 ImageData。
        /// </summary>
        event EventHandler<ImageReadyEventArgs> ImageReady;

        void Connect();
        void Disconnect();
        void SetTriggerMode(bool isTriggerMode);

        /// <summary>
        /// 异步触发一次采图。实现者负责将SDK的图像格式转换为通用的 ImageData。
        /// </summary>
        void GrabOneAsync();
    }
}
