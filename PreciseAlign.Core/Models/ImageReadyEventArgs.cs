using System;
using HalconDotNet;

namespace PreciseAlign.Core.Models
{
    public class ImageReadyEventArgs : EventArgs
    {
        /// <summary>
        /// 采集到的 Halcon 图像对象
        /// </summary>
        public HImage Image { get; } // 2. 将属性类型从 ImageData 修改为 HImage

        /// <summary>
        /// 触发该事件的相机ID
        /// </summary>
        public string CameraId { get; }

        public ImageReadyEventArgs(HImage image, string cameraId) // 3. 修改构造函数参数
        {
            Image = image;
            CameraId = cameraId;
        }
    }
}
