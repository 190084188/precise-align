using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreciseAlign.Core.Models
{
    public class ImageReadyEventArgs : EventArgs
    {
        /// <summary>
        /// 采集到的通用图像数据。
        /// </summary>
        public ImageData Image { get; }

        /// <summary>
        /// 产生此图像的相机ID。
        /// </summary>
        public string CameraId { get; }

        public ImageReadyEventArgs(ImageData image, string cameraId)
        {
            Image = image;
            CameraId = cameraId;
        }
    }
}
