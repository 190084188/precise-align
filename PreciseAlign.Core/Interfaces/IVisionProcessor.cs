using PreciseAlign.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreciseAlign.Core.Interfaces
{
    public interface IVisionProcessor
    {
        /// <summary>
        /// 处理一个通用的 ImageData 对象，并返回通用的 VisionResult。
        /// </summary>
        /// <param name="image">待处理的图像。</param>
        /// <returns>视觉处理结果。</returns>
        Task<VisionResult> ProcessImageAsync(ImageData image);
    }
}
