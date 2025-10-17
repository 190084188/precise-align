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
        /// 获取或设置相机曝光时间 (单位: 微秒)
        /// </summary>
        double Exposure { get; set; }

        /// <summary>
        /// 获取或设置相机增益
        /// </summary>
        double Gain { get; set; }

        /// <summary>
        /// 设置相机画面水平翻转（左右）
        /// </summary>
        void HorzFlip(bool flipped);

        /// <summary>
        /// 设置相机画面竖直翻转（上下）
        /// </summary>
        void VertFlip(bool flipped);

        /// <summary>
        /// 当新图像准备好时触发。事件参数现在是通用的HImage。
        /// </summary>
        event EventHandler<ImageReadyEventArgs>? ImageReady;

        void Connect();
        void Disconnect();
        void SetTriggerMode(bool isTriggerMode);

        /// <summary>
        /// 异步触发一次采图。实现者负责将SDK的图像格式转换为通用的HImage。
        /// </summary>
        void GrabOneAsync();
    }
}