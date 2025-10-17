using AxAxAltairUDrv;
using PreciseAlign.Core.Interfaces;
using PreciseAlign.Core.Models;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using HalconDotNet;

namespace PreciseAlign.Camera.Altair
{
    /// <summary>
    /// 基于AxAltairU ActiveX组件的相机实现
    /// </summary>
    public class AltairCamera : ICamera
    {
        private readonly AxAxAltairU _axCamera;
        private bool _isDisposed;
        private bool _isTriggerMode;

        public string CameraId { get; }
        public bool IsConnected => _axCamera?.IsPortCreated ?? false;

        public event EventHandler<ImageReadyEventArgs>? ImageReady;

        private readonly IAxAltairUEvents_OnSurfaceFilledEventHandler _onSurfaceFilledHandler;

        public double Exposure
        {
            get => _axCamera.ElShutter;
            set => _axCamera.ElShutter = (int)value;
        }

        public double Gain
        {
            get => _axCamera.VGain;
            set => _axCamera.VGain = (int)value;
        }

        public void HorzFlip(bool flipped)
        {
            // 使用文档中的 HorzFlip 属性
            _axCamera.HorzFlip = flipped;
        }
        public void VertFlip(bool flipped)
        {
            // 使用文档中的 VertFlip 属性
            _axCamera.VertFlip = flipped;
        }

        // 构造函数设为私有，使用工厂方法创建
        private AltairCamera(string cameraId, AxAxAltairU cameraInstance)
        {
            CameraId = cameraId;
            _axCamera = cameraInstance;
            _onSurfaceFilledHandler = new IAxAltairUEvents_OnSurfaceFilledEventHandler(OnSurfaceFilled);
            _axCamera.OnSurfaceFilled += _onSurfaceFilledHandler;
        }

        /// <summary>
        /// 工厂方法：确保ActiveX控件在主UI线程上创建。
        /// 注意：这个Create方法现在是公开的，但之后会被反射调用，而不是直接调用。
        /// </summary>
        public static ICamera Create(string cameraId)
        {
            if (!int.TryParse(cameraId, out int deviceIndex))
            {
                throw new ArgumentException("相机ID必须是有效的数字索引。");
            }

            // 因为ActiveX组件必须在UI线程上创建，所以这里仍然依赖WPF的Dispatcher
            AxAxAltairU cameraInstance = Application.Current.Dispatcher.Invoke(() =>
            {
                var cam = new AxAxAltairU();
                cam.CreateControl();
                // 此处设置了要连接的相机ID，在Connect前必须获取相机ID
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

                // 获取图像数据在内存中的起始地址
                IntPtr imagePtr = (IntPtr)_axCamera.GetImagePtr(e.surfaceHandle, 0, 0);
                if (imagePtr == IntPtr.Zero) return;
                // 直接从内存指针创建HImage对象
                HImage newImage = new HImage();
                string pixelFormat = ConvertPixelFormat((int)_axCamera.ColorFormat);
                if (pixelFormat == "MONO8")
                {
                    // 对于8位灰度图，使用 GenImage1
                    newImage.GenImage1("byte", width, height, imagePtr);
                }
                else if (pixelFormat == "RGB24")
                {
                    // 对于24位彩色图，使用 GenImageInterleaved
                    newImage.GenImageInterleaved(imagePtr, "rgb", width, height, -1, "byte", 0, 0, 0, 0, -1, 0);
                }
                else
                {
                    // 不支持的格式，直接返回
                    return;
                }
                ImageReady?.Invoke(this, new ImageReadyEventArgs(newImage, CameraId));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AltairCamera] 图像处理失败: {ex.Message}");
            }
        }

        public void Connect()
        {
            if (IsConnected) return;
            // 连接相机之前需要指定相机ID，否则无法连接，此相机类在构造时已经传入了CameraId。
            if (!_axCamera.CreateChannel())
            {
                // 初始连接相机失败时重置相机
                if (!_axCamera.ResetChannel())
                {

                }
                else
                {
                    throw new Exception($"连接相机 '{CameraId}' 失败 (CreateChannel failed)。");
                }
            }
        }

        public void Disconnect()
        {
            if (!IsConnected) return;
            //使用AltairU相机时,欲终止相机通道前，必须先Freeze()再DestroyChannel()释出相机权限。
            _axCamera.Freeze();
            _axCamera.DestroyChannel();
        }

        public void SetTriggerMode(bool isTriggerEnabled)
        {
            if (!IsConnected) return;
            _isTriggerMode = isTriggerEnabled;

            if (_isTriggerMode)
            {
                _axCamera.Freeze();
            }
            else
            {
                _axCamera.Live();
            }
        }

        public void GrabOneAsync()
        {
            if (!IsConnected || !_isTriggerMode) return;
            Task.Run(() => _axCamera.Snap(1));
        }

        public void ShowControlPanel()
        {
            _axCamera.ShowControlPanel = true;
        }

        private string ConvertPixelFormat(int sdkColorFormat)
        {
            switch (sdkColorFormat)
            {
                case 1: return "MONO8"; // 1 = AXAU_COLOR_FORMAT_GREYLEVEL
                case 2: return "RGB24"; // 2 = AXAU_COLOR_FORMAT_RGB24
                case 0: // AXAU_COLOR_FORMAT_NONE
                default:
                    return "Unknown";
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