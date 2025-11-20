// Models/ShapeRectangle2.cs
using HalconDotNet;
using System.Diagnostics;

namespace PreciseAlign.Core.Models
{
    /// <summary>
    /// 带方向的矩形 (Rectangle2)
    /// 使用 HHomMat2D 矩阵进行坐标变换，替代手动三角函数计算
    /// </summary>
    public class ShapeRectangle2 : BaseShape
    {
        // --- 核心属性 ---
        private double _row, _column, _phi, _length1, _length2;

        public double Row { get => _row; set => SetProperty(ref _row, value); }
        public double Column { get => _column; set => SetProperty(ref _column, value); }
        public double Phi { get => _phi; set => SetProperty(ref _phi, value); }
        public double Length1 { get => _length1; set => SetProperty(ref _length1, value); }
        public double Length2 { get => _length2; set => SetProperty(ref _length2, value); }

        // --- 缓存的世界坐标 (UI渲染和命中测试用) ---
        // 4个角点
        private double _hrTL, _hcTL; // TopLeft
        private double _hrTR, _hcTR; // TopRight
        private double _hrBR, _hcBR; // BottomRight
        private double _hrBL, _hcBL; // BottomLeft

        // 4个边中点
        private double _hrT, _hcT;   // Top
        private double _hrR, _hcR;   // Right
        private double _hrB, _hcB;   // Bottom
        private double _hrL, _hcL;   // Left

        // 旋转控制柄和箭头
        private double _hrRot, _hcRot;
        private double _hrArrowTip, _hcArrowTip;
        private double _hrArrowL, _hcArrowL;
        private double _hrArrowR, _hcArrowR;

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
        /// 使用 HALCON 矩阵更新所有控制柄的世界坐标
        /// </summary>
        private void UpdateHandlePositions()
        {
            try
            {
                // 1. 构建变换矩阵：局部坐标 -> 世界坐标
                // 顺序：单位矩阵 -> 旋转(Phi) -> 平移(Row, Col)
                // Ref: https://www.mvtec.com/doc/halcon/2105/en/hom_mat2d_rotate.html
                // Ref: https://www.mvtec.com/doc/halcon/2105/en/hom_mat2d_translate.html

                HHomMat2D mat = new HHomMat2D();
                mat.HomMat2dIdentity();
                // 先绕局部原点(0,0)旋转
                mat = mat.HomMat2dRotate(Phi, 0, 0);
                // 再平移到全局位置
                mat = mat.HomMat2dTranslate(Row, Column);

                // 2. 定义局部坐标 (Local Coordinates)
                // HALCON Rectangle2 坐标系定义:
                // X轴(Col方向) = Length1方向, Y轴(Row方向) = Length2方向
                // 局部中心为 (0,0)

                // 角点 (Row, Col) = (Y, X)
                // TopLeft: (-Length2, -Length1)
                TransformPoint(mat, -Length2, -Length1, out _hrTL, out _hcTL);
                TransformPoint(mat, -Length2, Length1, out _hrTR, out _hcTR);
                TransformPoint(mat, Length2, Length1, out _hrBR, out _hcBR);
                TransformPoint(mat, Length2, -Length1, out _hrBL, out _hcBL);

                // 边中点
                TransformPoint(mat, -Length2, 0, out _hrT, out _hcT);       // Top
                TransformPoint(mat, 0, Length1, out _hrR, out _hcR);        // Right
                TransformPoint(mat, Length2, 0, out _hrB, out _hcB);        // Bottom
                TransformPoint(mat, 0, -Length1, out _hrL, out _hcL);       // Left

                // 旋转控制柄 (位于右边中点)
                _hrRot = _hrR;
                _hcRot = _hcR;

                // 旋转箭头 (在右侧延伸)
                double arrowDist = 20.0;
                double arrowSize = 5.0;

                // 箭头尖端 (0, Length1 + arrowDist)
                TransformPoint(mat, 0, Length1 + arrowDist, out _hrArrowTip, out _hcArrowTip);
                // 箭头左翼 (-arrowSize, Length1 + arrowDist - arrowSize)
                TransformPoint(mat, -arrowSize, Length1 + arrowDist - arrowSize, out _hrArrowL, out _hcArrowL);
                // 箭头右翼 (+arrowSize, Length1 + arrowDist - arrowSize)
                TransformPoint(mat, arrowSize, Length1 + arrowDist - arrowSize, out _hrArrowR, out _hcArrowR);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Matrix update failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 辅助：应用矩阵变换点
        /// Ref: https://www.mvtec.com/doc/halcon/2105/en/affine_trans_point_2d.html
        /// </summary>
        private void TransformPoint(HHomMat2D mat, double localRow, double localCol, out double worldRow, out double worldCol)
        {
            // 注意 HALCON API 顺序: affine_trans_point_2d(Mat, Row, Col, out Row, out Col)
            worldRow = mat.AffineTransPoint2d(localRow, localCol, out worldCol);
        }

        public override void Draw(HWindow window)
        {
            UpdateScale(window); // 更新缩放比例
            UpdateHandlePositions(); // 确保绘制前是最新的

            //SetDrawingStyles(window);
            window.SetColor(Color);
            window.SetLineWidth(LineWidth);
            window.SetLineStyle(LineStyle);
            // 使用 XLD 绘制平滑轮廓 (Anti-aliased)
            HOperatorSet.GenRectangle2ContourXld(out HObject xld, Row, Column, Phi, Length1, Length2);
            try
            {
                // 使用 DispObj 绘制 (DispObj 可以自动处理 HObject 里的 XLD 数据)
                // 不要使用 DispXld，也不要强转
                window.DispObj(xld);
            }
            finally
            {
                // 必须显式释放，否则内存泄漏
                xld.Dispose();
            }

            if (IsSelected && IsInteractive)
            {
                window.SetColor("white");
                window.SetDraw("fill");

                // 绘制角点和边点
                DrawHandle(window, _hrTL, _hcTL);
                DrawHandle(window, _hrTR, _hcTR);
                DrawHandle(window, _hrBR, _hcBR);
                DrawHandle(window, _hrBL, _hcBL);
                DrawHandle(window, _hrT, _hcT);
                DrawHandle(window, _hrR, _hcR);
                DrawHandle(window, _hrB, _hcB);
                DrawHandle(window, _hrL, _hcL);

                // 中心点
                DrawHandle(window, Row, Column);

                // 旋转指示线
                window.SetColor("yellow");
                window.SetLineWidth(1);
                window.DispLine(_hrRot, _hcRot, _hrArrowTip, _hcArrowTip);

                // 绘制箭头 (三角形) - 只需要三个顶点的世界坐标
                window.DispPolygon(
                    new HTuple(_hrArrowTip, _hrArrowL, _hrArrowR, _hrArrowTip),
                    new HTuple(_hcArrowTip, _hcArrowL, _hcArrowR, _hcArrowTip));
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
                // 简单点测试：使用逆变换检查是否在范围内会更快，但 TestRegionPoint 更简单准确
                using (HRegion region = GetRegion())
                {
                    return region.TestRegionPoint(row, col) == 1
                        ? new ShapeHitTestResult(this, HitTestHandle.Body)
                        : ShapeHitTestResult.NoHit;
                }
            }

            UpdateHandlePositions(); // 确保命中测试使用最新坐标

            // 旋转手柄
            if (IsHandleHit(_hrRot, _hcRot, row, col)) return new ShapeHitTestResult(this, HitTestHandle.Rotate);

            // 角点
            if (IsHandleHit(_hrTL, _hcTL, row, col)) return new ShapeHitTestResult(this, HitTestHandle.TopLeft);
            if (IsHandleHit(_hrTR, _hcTR, row, col)) return new ShapeHitTestResult(this, HitTestHandle.TopRight);
            if (IsHandleHit(_hrBR, _hcBR, row, col)) return new ShapeHitTestResult(this, HitTestHandle.BottomRight);
            if (IsHandleHit(_hrBL, _hcBL, row, col)) return new ShapeHitTestResult(this, HitTestHandle.BottomLeft);

            // 边点
            if (IsHandleHit(_hrT, _hcT, row, col)) return new ShapeHitTestResult(this, HitTestHandle.Top);
            if (IsHandleHit(_hrR, _hcR, row, col)) return new ShapeHitTestResult(this, HitTestHandle.Right);
            if (IsHandleHit(_hrB, _hcB, row, col)) return new ShapeHitTestResult(this, HitTestHandle.Bottom);
            if (IsHandleHit(_hrL, _hcL, row, col)) return new ShapeHitTestResult(this, HitTestHandle.Left);

            // 中心
            if (IsHandleHit(Row, Column, row, col)) return new ShapeHitTestResult(this, HitTestHandle.Center);

            // Body
            using (HRegion region = GetRegion())
            {
                if (region.TestRegionPoint(row, col) == 1) return new ShapeHitTestResult(this, HitTestHandle.Body);
            }

            return ShapeHitTestResult.NoHit;
        }

        public override void DragHandle(HitTestHandle handle, double newRow, double newCol)
        {
            switch (handle)
            {
                case HitTestHandle.Rotate:
                    // 旋转逻辑：计算鼠标与中心的角度
                    // Ref: https://www.mvtec.com/doc/halcon/2105/en/angle_lx.html
                    Phi = HMisc.AngleLx(Row, Column, newRow, newCol);
                    break;

                case HitTestHandle.TopLeft:
                case HitTestHandle.TopRight:
                case HitTestHandle.BottomRight:
                case HitTestHandle.BottomLeft:
                case HitTestHandle.Top:
                case HitTestHandle.Bottom:
                case HitTestHandle.Left:
                case HitTestHandle.Right:
                    ResizeFromPoint(newRow, newCol);
                    break;
            }
        }

        /// <summary>
        /// 通用的拉伸逻辑：利用矩阵逆变换将鼠标坐标映射回局部坐标系
        /// </summary>
        private void ResizeFromPoint(double mouseRow, double mouseCol)
        {
            try
            {
                // 1. 构建正向矩阵 (同 UpdateHandlePositions)
                HHomMat2D mat = new HHomMat2D();
                mat.HomMat2dIdentity();
                mat = mat.HomMat2dRotate(Phi, 0, 0);
                mat = mat.HomMat2dTranslate(Row, Column);

                // 2. 计算逆矩阵 (World -> Local)
                // Ref: https://www.mvtec.com/doc/halcon/2105/en/hom_mat2d_invert.html
                HHomMat2D invMat = mat.HomMat2dInvert();

                // 3. 将鼠标的世界坐标转换回局部坐标 (LocalRow, LocalCol)
                // 局部坐标系下：LocalCol 对应 Length1，LocalRow 对应 Length2
                double localRow = invMat.AffineTransPoint2d(mouseRow, mouseCol, out double localCol);

                // 4. 更新长度 (取绝对值)
                // 这里的逻辑可以根据具体拖动了哪个Handle来细化
                // 目前简化为：拖动任意点，都根据该点到中心的距离更新对应的长宽

                // 如果想保持中心不动且支持非对称拖动，逻辑会更复杂
                // 这里实现的是：中心固定，改变 Length1/Length2
                Length1 = Math.Abs(localCol);
                Length2 = Math.Abs(localRow);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Resize failed: {ex.Message}");
            }
        }

        public override void Move(double rowOffset, double colOffset)
        {
            // 【关键修复】: 更新中心点坐标
            Row += rowOffset;
            Column += colOffset;

            // 无需手动更新其他点，因为它们会在 Draw() 和 HitTest() 中根据新的 Row/Column 重新计算
            // UpdateHandlePositions(); // 不需要在 Move 内部调用，Draw/HitTest 会自动调用
        }
    }
}