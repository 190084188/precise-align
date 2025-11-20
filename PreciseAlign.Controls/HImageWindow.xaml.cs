using HalconDotNet;
using PreciseAlign.Core.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PreciseAlign.Controls
{
    public partial class HImageWindow : UserControl
    {
        public HImageWindowViewModel ViewModel { get; }
        private bool _isWindowReady = false;

        #region 依赖属性
        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register(
                "Image", typeof(HObject), typeof(HImageWindow),
                new PropertyMetadata(null, OnImageChanged));

        public static readonly DependencyProperty GraphicsProperty =
            DependencyProperty.Register(
                "Graphics", typeof(ObservableCollection<ROI>), typeof(HImageWindow),
                new PropertyMetadata(
                    null,
                    OnGraphicsChanged,
                    OnCoerceGraphicsValue));

        public static readonly DependencyProperty ResultGraphicsProperty =
            DependencyProperty.Register(
                "ResultGraphics", typeof(HObject), typeof(HImageWindow),
                new PropertyMetadata(null, OnResultGraphicsChanged));

        public HObject Image
        {
            get { return (HObject)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        public ObservableCollection<ROI> Graphics
        {
            get { return (ObservableCollection<ROI>)GetValue(GraphicsProperty); }
            set { SetValue(GraphicsProperty, value); }
        }

        public HObject ResultGraphics
        {
            get { return (HObject)GetValue(ResultGraphicsProperty); }
            set { SetValue(ResultGraphicsProperty, value); }
        }
        private static object OnCoerceGraphicsValue(DependencyObject d, object baseValue)
        {
            if (baseValue == null)
            {
                return new ObservableCollection<ROI>();
            }
            return baseValue;
        }
        #endregion

        public HImageWindow()
        {
            InitializeComponent();
            ViewModel = new HImageWindowViewModel();
            // 2. 订阅 VM 事件 -> 触发 View 行为
            ViewModel.RequestRepaint += () => FullRedraw();
            ViewModel.RequestAutoFit += () => SetPartToFitImage();
            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(HImageWindowViewModel.ActiveTool))
                {
                    // 只有在 "Moving" 模式下，才允许 HALCON 原生拖拽
                    HalconWindow.HMoveContent = ViewModel.ActiveTool == ActiveToolMode.Moving;
                }
            };
            Loaded += OnHImageWindowLoaded;
            Focusable = true;
            PreviewKeyDown += HImageWindow_PreviewKeyDown;
        }

        private void HImageWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ViewModel.HandleKeyDown(e);
        }

        private void OnHImageWindowLoaded(object sender, RoutedEventArgs e)
        {
            Focus();
            _isWindowReady = true;
            if (HalconWindow.HalconWindow != null)
            {
                ViewModel.AreaSelectionWindow = HalconWindow.HalconWindow;
                // 开启抗锯齿，让线条更平滑
                // Ref: https://www.mvtec.com/doc/halcon/2105/en/set_window_param.html
                HalconWindow.HalconWindow.SetWindowParam("anti_aliasing", "true");
                HalconWindow.HalconWindow.SetWindowParam("graphics_stack_max_element_num", 500); // 优化重绘性能
                //ViewModel.AreaSelectionWindow.SetFont("Arial-14-B"); // 预设字体
            }
            HalconWindow.HMoveContent = false;
            RedrawSynchronous();
        }

        #region 图像层和绘制层处理

        private static void OnImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = d as HImageWindow;
            if (window?.ViewModel == null) return;
            // 同步到 VM
            window.ViewModel.Image = e.NewValue as HObject;
            // 释放旧的 HObject
            if (e.OldValue is HObject oldImage && oldImage.IsInitialized())
            {
                oldImage.Dispose();
            }
            window.SetPartToFitImage();
            window.RedrawSynchronous();
        }

        private static void OnGraphicsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as HImageWindow;
            if (control?.ViewModel == null) return;
            var newCollection = e.NewValue as ObservableCollection<ROI>;
            control.ViewModel.Graphics = newCollection ?? new ObservableCollection<ROI>();
        }

        private static void OnResultGraphicsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = d as HImageWindow;
            if (window == null) return;
            if (e.OldValue is HObject oldGraphics && oldGraphics.IsInitialized())
            {
                oldGraphics.Dispose();
            }

            // ResultGraphics 变化时不需要 SetPartToFitImage
            window.RedrawSynchronous();
        }

        private void OnGraphicsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // 订阅新 ROI 的内部集合
            if (e.NewItems != null)
            {
                foreach (ROI roi in e.NewItems)
                {
                    roi.Shapes.CollectionChanged += OnShapesCollectionChanged;
                }
            }
            // 取消订阅旧 ROI
            if (e.OldItems != null)
            {
                foreach (ROI roi in e.OldItems)
                {
                    roi.Shapes.CollectionChanged -= OnShapesCollectionChanged;
                }
            }
            RedrawSynchronous();
        }

        private void OnShapesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RedrawSynchronous();
        }


        public void FullRedraw()
        {
            Dispatcher.Invoke(() =>
            {
                RedrawSynchronous();
            });
        }

        public void RedrawSynchronous()
        {
            if (!_isWindowReady)
            {
                return;
            }
            if (!Dispatcher.CheckAccess())
            {
                // 我们不在UI线程上，所以 Invoke 到UI线程
                Dispatcher.Invoke(RedrawSynchronous);
                return;
            }

            var hWindow = HalconWindow.HalconWindow;
            if (hWindow == null) return;

            hWindow.ClearWindow();

            if (ViewModel != null && ViewModel.Image != null && ViewModel.Image.IsInitialized())
            {
                hWindow.DispObj(ViewModel.Image);
            }

            // --- 新的渲染循环 (遍历 ROI) ---
            if (Graphics != null)
            {
                // 按 ROI 的 ZOrder 排序，然后按 Shape 的 ZOrder 排序
                var allShapes = Graphics.OrderBy(r => r.ZOrder)
                                        .SelectMany(r => r.Shapes)
                                        .OrderBy(s => s.ZOrder);

                foreach (var shape in allShapes)
                {
                    try
                    {
                        shape.Draw(hWindow);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error drawing shape {shape.Name}: {ex.Message}");
                    }
                }
            }
            // --- 结束 ---

            if (ResultGraphics != null && ResultGraphics.IsInitialized())
            {
                hWindow.SetColor("red");
                hWindow.SetLineWidth(2);
                hWindow.DispObj(ResultGraphics);
            }
        }
        #endregion

        #region 绘制和选择ROI事件 (转发给VM)
        private void HalconWindow_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            this.Focus();
            ViewModel.HandleMouseDown(e);
        }

        private void HalconWindow_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            ViewModel.HandleMouseMove(e);
        }

        private void HalconWindow_HMouseUp(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            ViewModel.HandleMouseUp(e);
        }
        private void HalconWindow_HMouseDoubleClick(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            try
            {
                ViewModel.HandleDoubleClick(e.Row, e.Column);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"HandleDoubleClick 失败: {ex.Message}");
            }
        }
        #endregion

        #region 辅助函数
        public void SetPartToFitImage()
        {
            if (ViewModel == null || ViewModel.Image == null || !ViewModel.Image.IsInitialized())
                return;

            // *** 保护 SetPartToFitImage ***
            if (!_isWindowReady || HalconWindow.HalconWindow == null) return;

            try
            {
                HOperatorSet.SmallestRectangle1(ViewModel.Image, out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2);
                double objHeight = row2.D - row1.D;
                double objWidth = col2.D - col1.D;
                if (objHeight <= 0 || objWidth <= 0) return;
                double viewHeight = HalconWindow.ActualHeight;
                double viewWidth = HalconWindow.ActualWidth;
                if (viewHeight <= 0 || viewWidth <= 0) return;
                double objAspectRatio = objWidth / objHeight;
                double viewAspectRatio = viewWidth / viewHeight;
                double newRow1 = 0.0, newCol1 = 0.0, newRow2 = 0.0, newCol2 = 0.0;
                if (objAspectRatio > viewAspectRatio)
                {
                    double newHeight = objWidth / viewAspectRatio;
                    double centerRow = (row1.D + row2.D) / 2;
                    newRow1 = centerRow - newHeight / 2;
                    newRow2 = centerRow + newHeight / 2;
                    newCol1 = col1.D;
                    newCol2 = col2.D;
                }
                else
                {
                    double newWidth = objHeight * viewAspectRatio;
                    double centerCol = (col1.D + col2.D) / 2;
                    newCol1 = centerCol - newWidth / 2;
                    newCol2 = centerCol + newWidth / 2;
                    newRow1 = row1.D;
                    newRow2 = row2.D;
                }
                HalconWindow.HalconWindow.SetPart(newRow1, newCol1, newRow2, newCol2);
            }
            catch (HalconException) { }
        }
        #endregion
    }
}