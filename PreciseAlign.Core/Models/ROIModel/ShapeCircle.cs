// Models/ShapeCircle.cs (已修复)
using HalconDotNet;

namespace PreciseAlign.Core.Models
{
    public class ShapeCircle : BaseShape
    {
        // ... (属性 保持不变) ...
        private double _row, _column, _radius;
        public double Row { get => _row; set => SetProperty(ref _row, value); }
        public double Column { get => _column; set => SetProperty(ref _column, value); }
        public double Radius { get => _radius; set => SetProperty(ref _radius, value); }

        public ShapeCircle(double row, double col, double radius)
        {
            _row = row;
            _column = col;
            _radius = radius;
            Name = "Circle";
        }

        public override void Draw(HWindow window)
        {
            SetDrawingStyles(window);
            window.DispCircle(Row, Column, Radius);
            if (IsSelected && IsInteractive)
            {
                window.SetColor("white");
                window.SetDraw("fill");
                DrawHandle(window, Row - Radius, Column); // North
                DrawHandle(window, Row + Radius, Column); // South
                DrawHandle(window, Row, Column + Radius); // East
                DrawHandle(window, Row, Column - Radius); // West
                DrawHandle(window, Row, Column); // Center
            }
        }

        public override HRegion GetRegion()
        {
            HRegion region = new HRegion();
            region.GenCircle(Row, Column, Radius);
            return region;
        }

        public override ShapeHitTestResult HitTest(double row, double col)
        {
            if (!IsInteractive)
            {
                double distance = HMisc.DistancePp(row, col, Row, Column);
                return (distance <= Radius) ? new ShapeHitTestResult(this, HitTestHandle.Body) : ShapeHitTestResult.NoHit;
            }

            if (IsHandleHit(Row - Radius, Column, row, col))
                return new ShapeHitTestResult(this, HitTestHandle.Top); // North
            if (IsHandleHit(Row + Radius, Column, row, col))
                return new ShapeHitTestResult(this, HitTestHandle.Bottom); // South
            if (IsHandleHit(Row, Column + Radius, row, col))
                return new ShapeHitTestResult(this, HitTestHandle.Right); // East
            if (IsHandleHit(Row, Column - Radius, row, col))
                return new ShapeHitTestResult(this, HitTestHandle.Left); // West
            if (IsHandleHit(Row, Column, row, col))
                return new ShapeHitTestResult(this, HitTestHandle.Center);

            // 检查 Body
            double dist = HMisc.DistancePp(row, col, Row, Column);
            if (dist <= Radius)
            {
                return new ShapeHitTestResult(this, HitTestHandle.Body);
            }

            return ShapeHitTestResult.NoHit;
        }

        public override void DragHandle(HitTestHandle handle, double newRow, double newCol)
        {
            switch (handle)
            {
                case HitTestHandle.Body:
                case HitTestHandle.Center:
                    break;
                case HitTestHandle.Top:
                    Radius = Math.Abs(newRow - Row);
                    break;
                case HitTestHandle.Bottom:
                    Radius = Math.Abs(newRow - Row);
                    break;
                case HitTestHandle.Right:
                    Radius = Math.Abs(newCol - Column);
                    break;
                case HitTestHandle.Left:
                    Radius = Math.Abs(newCol - Column);
                    break;
            }
        }

        public override void Move(double rowOffset, double colOffset)
        {
            if (!CanMove) return;
            Row += rowOffset;
            Column += colOffset;
        }
    }
}