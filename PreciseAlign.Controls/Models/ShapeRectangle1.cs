// Models/ShapeRectangle1.cs 
using HalconDotNet;
using System;
using System.Diagnostics;

namespace PreciseAlign.Controls.Models
{
    public class ShapeRectangle1 : BaseShape
    {
        private double _row1, _column1, _row2, _column2;
        public double Row1 { get => _row1; set => SetProperty(ref _row1, value); }
        public double Column1 { get => _column1; set => SetProperty(ref _column1, value); }
        public double Row2 { get => _row2; set => SetProperty(ref _row2, value); }
        public double Column2 { get => _column2; set => SetProperty(ref _column2, value); }
        public double CenterRow => (Row1 + Row2) / 2.0;
        public double CenterCol => (Column1 + Column2) / 2.0;
        public double MinRow => Math.Min(Row1, Row2);
        public double MaxRow => Math.Max(Row1, Row2);
        public double MinCol => Math.Min(Column1, Column2);
        public double MaxCol => Math.Max(Column1, Column2);

        public ShapeRectangle1(double row1, double col1, double row2, double col2)
        {
            _row1 = row1;
            _column1 = col1;
            _row2 = row2;
            _column2 = col2;
            Name = "Rectangle1";
        }
        public override void Draw(HWindow window)
        {
            SetDrawingStyles(window); 
            window.DispRectangle1(MinRow, MinCol, MaxRow, MaxCol);

            if (IsSelected && IsInteractive)
            {
                window.SetColor("white");
                window.SetDraw("fill");
                DrawHandle(window, MinRow, MinCol); // TL
                DrawHandle(window, MinRow, CenterCol); // T
                DrawHandle(window, MinRow, MaxCol); // TR
                DrawHandle(window, CenterRow, MaxCol); // R
                DrawHandle(window, MaxRow, MaxCol); // BR
                DrawHandle(window, MaxRow, CenterCol); // B
                DrawHandle(window, MaxRow, MinCol); // BL
                DrawHandle(window, CenterRow, MinCol); // L
                DrawHandle(window, CenterRow, CenterCol); // Center
            }
        }
        public override HRegion GetRegion()
        {
            HRegion region = new HRegion();
            region.GenRectangle1(MinRow, MinCol, MaxRow, MaxCol);
            return region;
        }
        public override ShapeHitTestResult HitTest(double row, double col)
        {
            if (!IsInteractive)
            {
                bool isHit = (row >= MinRow && row <= MaxRow && col >= MinCol && col <= MaxCol);
                return isHit ? new ShapeHitTestResult(this, HitTestHandle.Body) : ShapeHitTestResult.NoHit;
            }
            if (IsHandleHit(MinRow, MinCol, row, col)) 
                return new ShapeHitTestResult(this, HitTestHandle.TopLeft);
            if (IsHandleHit(MinRow, CenterCol, row, col)) 
                return new ShapeHitTestResult(this, HitTestHandle.Top);
            if (IsHandleHit(MinRow, MaxCol, row, col)) 
                return new ShapeHitTestResult(this, HitTestHandle.TopRight);
            if (IsHandleHit(CenterRow, MaxCol, row, col)) 
                return new ShapeHitTestResult(this, HitTestHandle.Right);
            if (IsHandleHit(MaxRow, MaxCol, row, col))
                return new ShapeHitTestResult(this, HitTestHandle.BottomRight);
            if (IsHandleHit(MaxRow, CenterCol, row, col)) 
                return new ShapeHitTestResult(this, HitTestHandle.Bottom);
            if (IsHandleHit(MaxRow, MinCol, row, col)) 
                return new ShapeHitTestResult(this, HitTestHandle.BottomLeft);
            if (IsHandleHit(CenterRow, MinCol, row, col)) 
                return new ShapeHitTestResult(this, HitTestHandle.Left);
            if (IsHandleHit(CenterRow, CenterCol, row, col)) 
                return new ShapeHitTestResult(this, HitTestHandle.Center);
            if (row >= MinRow && row <= MaxRow && col >= MinCol && col <= MaxCol)
            {
                return new ShapeHitTestResult(this, HitTestHandle.Body);
            }
            return ShapeHitTestResult.NoHit;
        }

        public override void DragHandle(HitTestHandle handle, double newRow, double newCol)
        {
            // 直接设置对应的坐标点，让矩形的Min/Max属性自动处理反向
            switch (handle)
            {
                case HitTestHandle.Body:
                case HitTestHandle.Center:
                    // 由 ViewModel.Move 处理
                    return;

                // --- 角点 ---
                case HitTestHandle.TopLeft:
                    Row1 = newRow;
                    Column1 = newCol;
                    break;
                case HitTestHandle.TopRight:
                    Row1 = newRow;
                    Column2 = newCol;
                    break;
                case HitTestHandle.BottomRight:
                    Row2 = newRow;
                    Column2 = newCol;
                    break;
                case HitTestHandle.BottomLeft:
                    Row2 = newRow;
                    Column1 = newCol;
                    break;

                // --- 边 ---
                case HitTestHandle.Top:
                    Row1 = newRow;
                    break;
                case HitTestHandle.Bottom:
                    Row2 = newRow;
                    break;
                case HitTestHandle.Left:
                    Column1 = newCol;
                    break;
                case HitTestHandle.Right:
                    Column2 = newCol;
                    break;
            }
        }


        public override void Move(double rowOffset, double colOffset)
        {
            if (!CanMove) return;
            Row1 += rowOffset;
            Row2 += rowOffset;
            Column1 += colOffset;
            Column2 += colOffset;
        }
        public override string ToString()
        {
            return $"Rectangle1: Row1={Row1:F2}, Col1={Column1:F2}, Row2={Row2:F2}, Col2={Column2:F2}, " +
                   $"MinRow={MinRow:F2}, MaxRow={MaxRow:F2}, MinCol={MinCol:F2}, MaxCol={MaxCol:F2}";
        }
    }
}