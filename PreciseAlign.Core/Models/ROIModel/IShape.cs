// Models/IShape.cs
using HalconDotNet;

namespace PreciseAlign.Core.Models
{
    public interface IShape
    {
        // 属性
        Guid Id { get; }
        string Name { get; set; }
        int ZOrder { get; set; }
        bool IsSelected { get; set; }
        bool IsInteractive { get; set; }
        string Color { get; set; }
        int LineWidth { get; set; }
        HTuple LineStyle { get; set; } // (实线: new HTuple(), 虚线: new HTuple(10, 5))
        string DrawMode { get; set; } // (轮廓: "margin", 填充: "fill")
        int FillTransparency { get; set; } // (0=不透明, 100=透明 - 将映射到 HALCON)
        bool CanMove { get; set; }

        // 回调/事件
        event Action<IShape> SelectedChanged;

        // 方法
        /// <summary>
        /// 使用 HOperatorSet 在窗口上绘制自己
        /// </summary>
        void Draw(HWindow window);

        /// <summary>
        /// 检查坐标是否击中了该形状或其控制柄
        /// </summary>
        ShapeHitTestResult HitTest(double row, double col);

        /// <summary>
        /// 响应控制柄拖动事件
        /// </summary>
        void DragHandle(HitTestHandle handle, double newRow, double newCol);

        /// <summary>
        /// 按偏移量移动形状
        /// </summary>
        void Move(double rowOffset, double colOffset);

        /// <summary>
        /// 获取此形状的 HALCON 区域
        /// </summary>
        HRegion GetRegion();
    }
}