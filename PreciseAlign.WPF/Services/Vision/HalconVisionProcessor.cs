using HalconDotNet;
using PreciseAlign.Core.Interfaces;
using PreciseAlign.Core.Models;

namespace PreciseAlign.WPF.Services.Vision
{
    public class HalconVisionProcessor : IVisionProcessor
    {
        public async Task<VisionResult> ProcessImageAsync(HImage image, string stepName)
        {
            // 检查输入有效性
            return await Task.Run(() =>
            {
                if (image == null || !image.IsInitialized())
                {
                    return new VisionResult { IsSuccess = false };
                }
                HObject? cross = null;
                try
                {
                    // --- 在这里执行Halcon算法 ---
                    image.GetImageSize(out HTuple width, out HTuple height);
                    HOperatorSet.GenCrossContourXld(out cross, height / 2.0, width / 2.0, 100, 0);
                    var result = new VisionResult
                    {
                        // 必须 Clone，因为原始 image 马上要在下面被 Dispose
                        ProcessedImage = image.Clone(),
                        // 必须 Clone，因为局部变量 cross 马上要在下面被 Dispose
                        ResultGraphics = cross.Clone(),
                        // 定位的坐标信息
                        PositionX = 123.45,
                        PositionY = 678.90,
                        Angle = 1.2,
                        IsSuccess = true
                    };

                    return result;
                }
                finally
                {
                    image?.Dispose();
                    // 释放中间产生的临时对象
                    if (cross != null && cross.IsInitialized())
                    {
                        cross.Dispose();
                    }
                }
            });
        }
    }
}