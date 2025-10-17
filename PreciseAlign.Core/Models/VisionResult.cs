using HalconDotNet; // 1. 引入 Halcon 命名空间，因为属性需要用到 HObject 类型

namespace PreciseAlign.Core.Models
{
    /// <summary>
    /// 封装单次视觉处理的所有结果。
    /// </summary>
    public class VisionResult
    {
        /// <summary>
        /// 处理后用于显示的图像（例如，叠加了结果的原图）。
        /// </summary>
        public HObject? ProcessedImage { get; set; }

        /// <summary>
        /// 所有要叠加显示的图形结果 (如轮廓、十字等)。
        /// 建议使用 HObject 类型，它可以是 HRegion、HXLDCont 等的集合。
        /// </summary>
        public HObject? ResultGraphics { get; set; }

        /// <summary>
        /// 计算得到的X坐标。
        /// </summary>
        public double PositionX { get; set; }

        /// <summary>
        /// 计算得到的Y坐标。
        /// </summary>
        public double PositionY { get; set; }

        /// <summary>
        /// 计算得到的角度。
        /// </summary>
        public double Angle { get; set; }

        /// <summary>
        /// (可选) 处理是否成功。
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// (可选) 处理耗时（毫秒）。
        /// </summary>
        public long ProcessingTimeMs { get; set; }
    }
}