// Models/ShapeHitTestResult.cs
namespace PreciseAlign.Core.Models
{
    /// <summary>
    /// 定义被命中的控制柄的类型
    /// </summary>
    public enum HitTestHandle
    {
        None,
        Body,
        TopLeft,
        Top,
        TopRight,
        Right,
        BottomRight,
        Bottom,
        BottomLeft,
        Left,
        Center,
        Rotate
    }

    /// <summary>
    /// 包含 HitTest 结果的结构
    /// </summary>
    public struct ShapeHitTestResult
    {
        public IShape? Shape { get; }
        public HitTestHandle Handle { get; }

        public bool HasHit => Handle != HitTestHandle.None && Shape != null;

        public ShapeHitTestResult(IShape? shape, HitTestHandle handle)
        {
            Shape = shape;
            Handle = handle;
        }

        public static ShapeHitTestResult NoHit => new ShapeHitTestResult(null, HitTestHandle.None);
    }
}