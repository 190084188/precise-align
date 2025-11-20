// PreciseAlign.Core/Models/BaseShape.cs
using HalconDotNet;
using PreciseAlign.Core.Mvvm;
using System;

namespace PreciseAlign.Core.Models
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

        // --- 样式常量 ---
        // 控制柄在屏幕上的固定大小 (单位：屏幕像素)
        // 设定为 8px，意味着无论图像怎么缩放，屏幕上看到的控制柄永远是 8x8 像素
        protected const double HANDLE_SCREEN_SIZE = 8.0;

        // --- 缩放状态缓存 ---
        // 1.0 表示 1屏幕像素 = 1图像像素 (100%显示)
        // 0.1 表示 1屏幕像素 = 0.1图像像素 (放大查看)
        protected double _currentScale = 1.0;

        #region --- 属性实现 ---
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
        #endregion

        // --- 事件 ---
        public event Action<IShape>? SelectedChanged;

        #region --- 抽象方法 (必须由子类实现) ---
        public abstract void Draw(HWindow window);
        public abstract HRegion GetRegion();
        public abstract ShapeHitTestResult HitTest(double row, double col);
        public abstract void DragHandle(HitTestHandle handle, double newRow, double newCol);
        public virtual void Move(double rowOffset, double colOffset) { }
        #endregion

        /// <summary>
        /// 计算并更新当前的缩放系数。应在子类的 Draw 方法开头调用。
        /// </summary>
        protected void UpdateScale(HWindow window)
        {
            if (window == null) return;

            try
            {
                // 1. 获取当前显示的图像部分 (Image Coordinates)
                window.GetPart(out int r1, out int c1, out int r2, out int c2);
                // 2. 获取窗口控件的物理像素大小 (Screen Pixels)
                window.GetWindowExtents(out int winRow, out int winCol, out int winWidth, out int winHeight);

                double partHeight = Math.Abs(r2 - r1) + 1;
                double partWidth = Math.Abs(c2 - c1) + 1;

                if (winHeight <= 0 || winWidth <= 0)
                {
                    _currentScale = 1.0;
                    return;
                }

                // 3. 计算比例：1 屏幕像素对应多少图像单位
                double scaleRow = partHeight / (double)winHeight;
                double scaleCol = partWidth / (double)winWidth;

                // 取平均值作为统一的缩放因子
                _currentScale = (scaleRow + scaleCol) / 2.0;
            }
            catch
            {
                // 发生异常(如窗口未句柄)时保持默认
                _currentScale = 1.0;
            }
        }

        /// <summary>
        /// 智能绘制控制柄：利用 _currentScale 确保屏幕大小恒定
        /// </summary>
        protected void DrawHandle(HWindow window, double r, double c)
        {
            // 计算在图像坐标系下需要画多大，才能在屏幕上显示为 HANDLE_SCREEN_SIZE
            double sizeInImage = (HANDLE_SCREEN_SIZE / 2.0) * _currentScale;

            // 绘制矩形控制柄
            window.DispRectangle2(r, c, 0, sizeInImage, sizeInImage);
        }

        /// <summary>
        /// 智能检测命中：利用 _currentScale 确保鼠标容差恒定
        /// </summary>
        protected bool IsHandleHit(double handleRow, double handleCol, double testRow, double testCol)
        {
            // 基础容差是控制柄半径 + 2px 的缓冲
            double screenTolerance = (HANDLE_SCREEN_SIZE / 2.0) + 2.0;

            // 将屏幕容差转换为图像坐标系下的距离
            double imageTolerance = screenTolerance * _currentScale;

            // 避免容差过小（比如无限放大时），设置一个极小值防抖
            if (imageTolerance < 0.5) imageTolerance = 0.5;

            // 使用曼哈顿距离或欧氏距离判断（矩形柄用简单的盒式判断即可）
            return Math.Abs(handleRow - testRow) <= imageTolerance &&
                   Math.Abs(handleCol - testCol) <= imageTolerance;
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
    }
}