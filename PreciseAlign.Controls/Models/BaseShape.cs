// Models/BaseShape.cs
using HalconDotNet;
using PreciseAlign.Controls.Mvvm;
using System;

namespace PreciseAlign.Controls.Models
{
    public abstract class BaseShape : ObservableObject, IShape
    {
        private string _name = string.Empty;
        private int _zOrder;
        private bool _isSelected;
        private bool _isInteractive = true;
        private string _color = "red";
        private int _lineWidth = 2;
        private HTuple _lineStyle = new HTuple();
        private string _drawMode = "margin";
        private int _fillTransparency; // 0-100
        private bool _canMove = true;

        public Guid Id { get; } = Guid.NewGuid();
        public abstract ShapeHitTestResult HitTest(double row, double col);
        public abstract void DragHandle(HitTestHandle handle, double newRow, double newCol);
        // --- 属性实现 ---
        public string Name { get => _name; set => SetProperty(ref _name, value); }
        public int ZOrder { get => _zOrder; set => SetProperty(ref _zOrder, value); }
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    SelectedChanged?.Invoke(this); // 触发回调
                }
            }
        }
        public bool IsInteractive { get => _isInteractive; set => SetProperty(ref _isInteractive, value); }
        public string Color { get => _color; set => SetProperty(ref _color, value); }
        public int LineWidth { get => _lineWidth; set => SetProperty(ref _lineWidth, value); }
        public HTuple LineStyle { get => _lineStyle; set => SetProperty(ref _lineStyle, value); }
        public string DrawMode { get => _drawMode; set => SetProperty(ref _drawMode, value); }
        public int FillTransparency { get => _fillTransparency; set => SetProperty(ref _fillTransparency, value); }
        public bool CanMove { get => _canMove; set => SetProperty(ref _canMove, value); }

        // --- 事件 ---
        public event Action<IShape>? SelectedChanged;

        // --- 抽象方法 (必须由子类实现) ---
        public abstract void Draw(HWindow window);
        public abstract HRegion GetRegion();

        // --- 通用方法 ---
        public virtual void Move(double rowOffset, double colOffset)
        {
            // 在子类中实现以修改坐标
        }

        /// <summary>
        /// 辅助方法：在绘制前设置 HALCON 样式
        /// </summary>
        protected void SetDrawingStyles(HWindow window)
        {
            window.SetColor(Color);
            window.SetDraw(DrawMode);
            window.SetLineWidth(LineWidth);
            window.SetLineStyle(LineStyle);
        }
        /// <summary>
        /// 辅助方法：在 (r, c) 处绘制一个 5x5 的控制柄
        /// </summary>
        protected void DrawHandle(HWindow window, double r, double c)
        {
            window.DispRectangle2(r, c, 0, 5, 5);
        }
        /// <summary>
        /// 辅助方法：检查 (r, c) 是否命中了 5x5 的控制柄
        /// </summary>
        protected bool IsHandleHit(double handleRow, double handleCol, double testRow, double testCol)
        {
            // (5 像素容差)
            return (Math.Abs(handleRow - testRow) <= 5 && Math.Abs(handleCol - testCol) <= 5);
        }
    }
}