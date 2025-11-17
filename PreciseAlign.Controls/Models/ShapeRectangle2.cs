using HalconDotNet;
using System;
using System.Diagnostics;

namespace PreciseAlign.Controls.Models
{
    public class ShapeRectangle2 : BaseShape
    {
        //   Row:
        //     中心点的Row坐标，Halcon中Default为48
        //
        //   Col:
        //     中心点的Column坐标，Halcon中Default为64
        //
        //   Phi:
        //     矩形的方向弧度，Halcon中Default为0.0
        //
        //   Length1:
        //     长边的一半，Halcon中Default为48
        //
        //   Length2:
        //     短边的一半，Halcon中Default为32
        private double _row, _column, _phi, _length1, _length2;

        public double Row { get => _row; set => SetProperty(ref _row, value); }
        public double Column { get => _column; set => SetProperty(ref _column, value); }
        public double Phi { get => _phi; set => SetProperty(ref _phi, value); }
        public double Length1 { get => _length1; set => SetProperty(ref _length1, value); }
        public double Length2 { get => _length2; set => SetProperty(ref _length2, value); }

        // 控制柄坐标缓存
        private double _hrT, _hcT;
        private double _hrL, _hcL;
        private double _hrR, _hcR;
        private double _hrB, _hcB;
        private double _hrTL, _hcTL;
        private double _hrTR, _hcTR;
        private double _hrBR, _hcBR;
        private double _hrBL, _hcBL;
        private double _hrRot, _hcRot;
        private double _hrRotArrow, _hcRotArrow;

        public ShapeRectangle2(double row, double col, double phi, double l1, double l2)
        {
            _row = row;
            _column = col;
            _phi = phi;
            _length1 = l1;
            _length2 = l2;
            Name = "Rectangle2";
        }

        /// <summary>
        /// 重新计算所有控制柄的世界坐标
        /// </summary>
        private void UpdateHandlePositions()
        {
            try
            {
                double cosPhi = Math.Cos(Phi);
                double sinPhi = Math.Sin(Phi);

                // 四个角点在本地坐标系中的坐标
                // Top-Left 本地坐标: (-Length1, -Length2)
                double localRowTL = -Length2;
                double localColTL = -Length1;
                _hrTL = Row + localRowTL * cosPhi - localColTL * sinPhi;
                _hcTL = Column + localRowTL * sinPhi + localColTL * cosPhi;

                // Top-Right 本地坐标: (-Length1, +Length2)
                double localRowTR = -Length2;
                double localColTR = Length1;
                _hrTR = Row + localRowTR * cosPhi - localColTR * sinPhi;
                _hcTR = Column + localRowTR * sinPhi + localColTR * cosPhi;

                // Bottom-Right 本地坐标: (+Length1, +Length2)
                double localRowBR = Length2;
                double localColBR = Length1;
                _hrBR = Row + localRowBR * cosPhi - localColBR * sinPhi;
                _hcBR = Column + localRowBR * sinPhi + localColBR * cosPhi;

                // Bottom-Left 本地坐标: (+Length1, -Length2)
                double localRowBL = Length2;
                double localColBL = -Length1;
                _hrBL = Row + localRowBL * cosPhi - localColBL * sinPhi;
                _hcBL = Column + localRowBL * sinPhi + localColBL * cosPhi;

                // Top 本地坐标: (-Length2, 0)
                double localRowT = -Length2;
                double localColT = 0;
                _hrT = Row + localRowT * cosPhi - localColT * sinPhi;
                _hcT = Column + localRowT * sinPhi + localColT * cosPhi;

                // Left 本地坐标: (0, -Length1)
                double localRowL = 0;
                double localColL = -Length1;
                _hrL = Row + localRowL * cosPhi - localColL * sinPhi;
                _hcL = Column + localRowL * sinPhi + localColL * cosPhi;

                // Right 本地坐标: (0, Length1)
                double localRowR = 0;
                double localColR = Length1;
                _hrR = Row + localRowR * cosPhi - localColR * sinPhi;
                _hcR = Column + localRowR * sinPhi + localColR * cosPhi;

                // Bottom 本地坐标: (Length2, 0)
                double localRowB = Length2;
                double localColB = 0;
                _hrB = Row + localRowB * cosPhi - localColB * sinPhi;
                _hcB = Column + localRowB * sinPhi + localColB * cosPhi;

                // 旋转控制柄 (右边缘中点) 本地坐标: (0, +Length2)
                double localRowRot = 0;
                double localColRot = Length1;
                _hrRot = Row + localRowRot * cosPhi - localColRot * sinPhi;
                _hcRot = Column + localRowRot * sinPhi + localColRot * cosPhi;

                // 旋转箭头尖端 (在旋转方向上延伸)
                double arrowLength = 20.0;
                double localRowArrow = 0;
                double localColArrow = Length1 + arrowLength;
                _hrRotArrow = Row + localRowArrow * cosPhi - localColArrow * sinPhi;
                _hcRotArrow = Column + localRowArrow * sinPhi + localColArrow * cosPhi;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateHandlePositions error: {ex.Message}");
            }
            
        }

        public override void Draw(HWindow window)
        {
            // 先绘制矩形
            SetDrawingStyles(window);
            window.DispRectangle2(Row, Column, Phi, Length1, Length2);

            if (IsSelected && IsInteractive)
            {
                // 在绘制控制柄前更新控制柄位置
                UpdateHandlePositions();

                window.SetColor("white");
                window.SetDraw("fill");

                // 绘制4个角点控制柄
                DrawHandle(window, _hrTL, _hcTL);
                DrawHandle(window, _hrTR, _hcTR);
                DrawHandle(window, _hrBR, _hcBR);
                DrawHandle(window, _hrBL, _hcBL);

                DrawHandle(window, _hrT, _hcT);
                DrawHandle(window, _hrL, _hcL);
                DrawHandle(window, _hrR, _hcR);
                DrawHandle(window, _hrB, _hcB);

                // 绘制中心点控制柄
                DrawHandle(window, Row, Column);

                // 绘制旋转控制柄和箭头（现在在右边中点）
                DrawHandle(window, _hrRot, _hcRot);

                // 绘制箭头线
                window.SetDraw("margin");
                window.SetLineWidth(2);
                window.DispLine(_hrRot, _hcRot, _hrRotArrow, _hcRotArrow);

                // 绘制箭头头部
                DrawArrowHead(window, _hrRot, _hcRot, _hrRotArrow, _hcRotArrow);
            }
        }

        /// <summary>
        /// 在箭头末端绘制三角形箭头头
        /// </summary>
        private void DrawArrowHead(HWindow window, double startRow, double startCol, double endRow, double endCol)
        {
            double arrowHeadSize = 8.0;

            double dx = endCol - startCol;
            double dy = endRow - startRow;
            double length = Math.Sqrt(dx * dx + dy * dy);

            if (length > 0)
            {
                dx /= length;
                dy /= length;

                double perpDx = -dy;
                double perpDy = dx;

                double tipRow = endRow;
                double tipCol = endCol;

                double leftRow = endRow - arrowHeadSize * dy + arrowHeadSize * 0.5 * perpDy;
                double leftCol = endCol - arrowHeadSize * dx + arrowHeadSize * 0.5 * perpDx;

                double rightRow = endRow - arrowHeadSize * dy - arrowHeadSize * 0.5 * perpDy;
                double rightCol = endCol - arrowHeadSize * dx - arrowHeadSize * 0.5 * perpDx;

                window.SetDraw("fill");
                window.DispPolygon(new HTuple(leftRow, tipRow, rightRow), new HTuple(leftCol, tipCol, rightCol));
            }
        }

        public override HRegion GetRegion()
        {
            HRegion region = new HRegion();
            region.GenRectangle2(Row, Column, Phi, Length1, Length2);
            return region;
        }

        public override ShapeHitTestResult HitTest(double row, double col)
        {
            if (!IsInteractive)
            {
                using (HRegion region = GetRegion())
                {
                    bool isHit = region.TestRegionPoint(row, col) == 1;
                    return isHit ? new ShapeHitTestResult(this, HitTestHandle.Body) : ShapeHitTestResult.NoHit;
                }
            }

            // 在命中测试前更新控制柄位置
            UpdateHandlePositions();

            // 检查旋转控制柄
            if (IsHandleHit(_hrRot, _hcRot, row, col))
                return new ShapeHitTestResult(this, HitTestHandle.Rotate);

            // 检查4个角点
            if (IsHandleHit(_hrTL, _hcTL, row, col))
                return new ShapeHitTestResult(this, HitTestHandle.TopLeft);
            if (IsHandleHit(_hrTR, _hcTR, row, col))
                return new ShapeHitTestResult(this, HitTestHandle.TopRight);
            if (IsHandleHit(_hrBR, _hcBR, row, col))
                return new ShapeHitTestResult(this, HitTestHandle.BottomRight);
            if (IsHandleHit(_hrBL, _hcBL, row, col))
                return new ShapeHitTestResult(this, HitTestHandle.BottomLeft);

            if (IsHandleHit(_hrT, _hcT, row, col))
                return new ShapeHitTestResult(this, HitTestHandle.Top);
            if (IsHandleHit(_hrR, _hcR, row, col))
                return new ShapeHitTestResult(this, HitTestHandle.Right);
            if (IsHandleHit(_hrB, _hcB, row, col))
                return new ShapeHitTestResult(this, HitTestHandle.Bottom);
            if (IsHandleHit(_hrL, _hcL, row, col))
                return new ShapeHitTestResult(this, HitTestHandle.Left);

            // 检查中心点
            if (IsHandleHit(Row, Column, row, col))
                return new ShapeHitTestResult(this, HitTestHandle.Center);

            // 检查矩形区域
            using (HRegion region = GetRegion())
            {
                if (region.TestRegionPoint(row, col) == 1)
                {
                    return new ShapeHitTestResult(this, HitTestHandle.Body);
                }
            }

            return ShapeHitTestResult.NoHit;
        }

        public override void DragHandle(HitTestHandle handle, double newRow, double newCol)
        {
            switch (handle)
            {
                case HitTestHandle.Body:
                case HitTestHandle.Center:
                    // 由 ViewModel.Move 处理
                    break;

                case HitTestHandle.Rotate:
                    // 旋转：计算从中心到鼠标位置的角度
                    Phi = HMisc.AngleLx(Row, Column, newRow, newCol);
                    //ResizeFromCorner(newRow, newCol);
                    break;

                case HitTestHandle.TopLeft:
                    ResizeFromCorner(newRow, newCol);
                    break;

                case HitTestHandle.TopRight:
                    ResizeFromCorner(newRow, newCol);
                    break;

                case HitTestHandle.BottomRight:
                    ResizeFromCorner(newRow, newCol);
                    break;

                case HitTestHandle.BottomLeft:
                    ResizeFromCorner(newRow, newCol);
                    break;

                //case HitTestHandle.Top:
                //    ResizeFromCorner(newRow, newCol);
                //    break;
                //case HitTestHandle.Bottom:
                //    ResizeFromCorner(newRow, newCol);
                //    break;
                //case HitTestHandle.Left:
                //    ResizeFromCorner(newRow, newCol);
                //    break;
                //case HitTestHandle.Right:
                //    ResizeFromCorner(newRow, newCol);
                //    break;
            }
        }

        /// <summary>
        /// 从角点调整矩形大小
        /// </summary>
        private void ResizeFromCorner(double newRow, double newCol)
        {
            // 将世界坐标转换回本地坐标
            double cosPhi = Math.Cos(Phi);
            double sinPhi = Math.Sin(Phi);

            // 相对中心点的坐标
            double relRow = newRow - Row;
            double relCol = newCol - Column;

            // 旋转到本地坐标系（逆时针旋转 -Phi）
            double localRow = relRow * cosPhi + relCol * sinPhi;
            double localCol = -relRow * sinPhi + relCol * cosPhi;

            // 计算新的半长和半宽（取绝对值）
            Length1 = Math.Abs(localCol);
            Length2 = Math.Abs(localRow);
        }

        public override void Move(double rowOffset, double colOffset)
        {
            if (!CanMove) return;
            Row += rowOffset;
            Column += colOffset;
        }
    }
}
