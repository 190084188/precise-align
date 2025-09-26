using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreciseAlign.Core.Models
{
    /// <summary>
    /// 一个与视觉库无关的通用图像数据容器。
    /// 包含了解析图像所需的原始像素数据和元数据。
    /// </summary>
    public class ImageData : IDisposable
    {
        /// <summary>
        /// 包含图像像素的原始字节数组。
        /// </summary>
        public byte[] PixelData { get; }

        /// <summary>
        /// 图像宽度（像素）。
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// 图像高度（像素）。
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// 像素格式，例如 "Mono8", "BGR24", "RGB24"。
        /// 使用标准化字符串，便于不同库之间转换。
        /// </summary>
        public string PixelFormat { get; }

        /// <summary>
        /// (可选) 保留对原始图像对象的引用，以便在需要时进行类型转换，
        /// 但核心接口不应依赖它。
        /// </summary>
        public object? OriginalSource { get; }

        public ImageData(byte[] pixelData, int width, int height, string pixelFormat, object? originalSource = null)
        {
            PixelData = pixelData;
            Width = width;
            Height = height;
            PixelFormat = pixelFormat;
            OriginalSource = originalSource;
        }

        /// <summary>
        /// 如果原始对象是可释放的，则释放它。
        /// </summary>
        public void Dispose()
        {
            (OriginalSource as IDisposable)?.Dispose();
        }
    }
}
