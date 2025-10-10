using AxAxAltairUDrv;
using PreciseAlign.Core.Interfaces;
using PreciseAlign.Core.Models;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace PreciseAlign.WPF.Services.Camera
{
    /// <summary>
    /// 基于AxAltairU ActiveX组件的相机实现 (已根据用户提供的SDK文档完全重写)
    /// </summary>
    public class AltairCamera : ICamera
    {
        private readonly AxAxAltairU _axCamera;
        private bool _isDisposed;
        private bool _isTriggerMode; // 内部标志，用于区分连续模式和触发模式

        public string CameraId { get; }

        // 使用文档中提供的 IsPortCreated 属性来判断连接状态
        public bool IsConnected => _axCamera?.IsPortCreated ?? false;

        public event EventHandler<ImageReadyEventArgs>? ImageReady;
        //OnSurfaceFilled事件处理器委托
        private readonly IAxAltairUEvents_OnSurfaceFilledEventHandler _onSurfaceFilledHandler;

        public double Exposure
        {
            // 使用文档中的 ElShutter 属性
            get => _axCamera.ElShutter;
            set => _axCamera.ElShutter = (int)value;
        }

        public double Gain
        {
            // 使用文档中的 VGain 属性
            get => _axCamera.VGain;
            set => _axCamera.VGain = (int)value;
        }

        // 构造函数设为私有
        private AltairCamera(string cameraId, AxAxAltairU cameraInstance)
        {
            CameraId = cameraId;
            _axCamera = cameraInstance;
            _onSurfaceFilledHandler = new IAxAltairUEvents_OnSurfaceFilledEventHandler(OnSurfaceFilled);
            _axCamera.OnSurfaceFilled += _onSurfaceFilledHandler;
        }

        /// <summary>
        /// 工厂方法：确保ActiveX控件在主UI线程上创建。
        /// </summary>
        public static AltairCamera Create(string cameraId)
        {
            if (!int.TryParse(cameraId, out int deviceIndex))
            {
                throw new ArgumentException("相机ID必须是有效的数字索引。");
            }

            AxAxAltairU cameraInstance = System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var cam = new AxAxAltairU();
                cam.CreateControl();
                cam.DeviceIndex = deviceIndex;
                return cam;
            });

            return new AltairCamera(cameraId, cameraInstance);
        }

        private void OnSurfaceFilled(object sender, IAxAltairUEvents_OnSurfaceFilledEvent e)
        {
            try
            {
                if (e.surfaceHandle == 0) return;

                int width = _axCamera.ImageWidth;
                int height = _axCamera.ImageHeight;
                if (width == 0 || height == 0) return;

                // 1. 使用 GetImagePtr 获取图像数据在内存中的起始地址
                IntPtr imagePtr = (IntPtr)_axCamera.GetImagePtr(e.surfaceHandle, 0, 0);
                if (imagePtr == IntPtr.Zero) return;

                // 2. 根据颜色格式计算图像总大小（字节）
                string pixelFormat = ConvertPixelFormat((int)_axCamera.ColorFormat);
                int bytesPerPixel = pixelFormat == "BGR24" ? 3 : 1; // 假设彩色为3字节，其他为1字节
                int totalSize = width * height * bytesPerPixel;
                if (totalSize <= 0) return;

                // 3. 创建C#字节数组来接收数据
                byte[] pixelData = new byte[totalSize];

                // 4. 使用 Marshal.Copy 从内存地址安全地复制数据到C#数组
                Marshal.Copy(imagePtr, pixelData, 0, totalSize);

                // 5. 封装数据并触发事件
                var imageData = new ImageData(pixelData, width, height, pixelFormat);
                ImageReady?.Invoke(this, new ImageReadyEventArgs(imageData, CameraId));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AltairCamera] 图像处理失败: {ex.Message}");
            }
        }

        public void Connect()
        {
            if (IsConnected) return;

            // 使用文档中的 CreateChannel 方法
            if (!_axCamera.CreateChannel())
            {
                throw new Exception($"连接相机 '{CameraId}' 失败 (CreateChannel failed)。");
            }
        }

        public void Disconnect()
        {
            if (!IsConnected) return;
            // 使用文档中的 DestroyChannel 方法
            _axCamera.DestroyChannel();
        }

        public void ShowControlPanel()
        {
            // 使用SDK中的 ShowControlPanel 方法
            _axCamera.ShowControlPanel=true;
        }

        public void SetTriggerMode(bool isTriggerEnabled)
        {
            if (!IsConnected) return;
            _isTriggerMode = isTriggerEnabled;

            if (_isTriggerMode)
            {
                // 进入触发模式前，先停止连续采集 (Freeze)
                _axCamera.Freeze();
            }
            else
            {
                // 进入连续模式 (Live)
                _axCamera.Live();
            }
        }

        public void GrabOneAsync()
        {
            // 仅在触发模式下执行
            if (!IsConnected || !_isTriggerMode) return;

            // 使用文档中的 Snap(1) 方法来采集单张图像
            Task.Run(() => _axCamera.Snap(1));
        }

        private string ConvertPixelFormat(int sdkColorFormat)
        {
            // TxAxauColorFormat 枚举的映射
            // 您可能需要根据实际的枚举值进行微调
            switch (sdkColorFormat)
            {
                case 0: return "Mono8"; // 假设 0 是灰度
                case 1: return "BGR24"; // 假设 1 是彩色
                default: return "Unknown";
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                _axCamera.OnSurfaceFilled -= _onSurfaceFilledHandler;
            }
            Disconnect();
            if (_axCamera != null)
            {
                Marshal.ReleaseComObject(_axCamera);
            }
            _isDisposed = true;
        }
    }
}