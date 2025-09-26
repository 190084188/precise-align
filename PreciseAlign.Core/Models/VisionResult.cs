using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PreciseAlign.Core.Models
{
    /// <summary>
    /// 用于在UI上显示的通用几何图形结果。
    /// </summary>
    public abstract class DisplayableShape
    {
        public string Color { get; set; } = "green";
    }

    public class DisplayableContour : DisplayableShape
    {
        public List<Point> Points { get; set; } = new List<Point>();
    }

    public class DisplayableCross : DisplayableShape
    {
        public Point Center { get; set; }
        public double Size { get; set; }
        public double Angle { get; set; }
    }

    /// <summary>
    /// 定义与视觉库无关的通用视觉处理结果。
    /// </summary>
    public class VisionResult
    {
        public bool Success { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Angle { get; set; }

        /// <summary>
        /// 用于在UI上显示的几何结果列表
        /// </summary>
        public List<DisplayableShape> DisplayableResults { get; set; } = new List<DisplayableShape>();
    }
}
