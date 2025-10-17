using HalconDotNet;
using PreciseAlign.Core.Interfaces;
using PreciseAlign.Core.Models;
using System.Threading.Tasks;

namespace PreciseAlign.WPF.Services.Vision
{
    public class HalconVisionProcessor : IVisionProcessor
    {
        public async Task<VisionResult> ProcessImageAsync(HImage image, string stepName)
        {
            return await Task.Run(() =>
            {
                if (image == null || !image.IsInitialized())
                {
                    return new VisionResult { IsSuccess = false };
                }

                try
                {
                    // --- 在这里执行Halcon算法 ---
                    image.GetImageSize(out HTuple width, out HTuple height);

                    HOperatorSet.GenCrossContourXld(out HObject cross, height / 2.0, width / 2.0, 100, 0);
                    var result = new VisionResult
                    {
                        // 注意：这里需要Clone一份，因为原始的image对象的所有权会转移到这个方法中
                        // 处理完毕后，原始image会被释放，所以需要一个新的副本传出去
                        ProcessedImage = image.Clone(),
                        ResultGraphics = cross.Clone(), // 图形对象也建议Clone
                        PositionX = 123.45,
                        PositionY = 678.90,
                        Angle = 1.2,
                        IsSuccess = true
                    };

                    return result;
                }
                finally
                {
                    image.Dispose();
                }
            });
        }
    }
}