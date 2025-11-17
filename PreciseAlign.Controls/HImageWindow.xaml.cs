using HalconDotNet;
using PreciseAlign.Controls.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PreciseAlign.Controls
{
    public partial class HImageWindow : UserControl
    {
        public HImageWindowViewModel ViewModel { get; }

        // *** 1. 添加此标志 ***
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
                new PropertyMetadata(null, OnImageChanged));

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
            ViewModel = new HImageWindowViewModel(this);
            this.Loaded += OnHImageWindowLoaded;
            this.Focusable = true;
            this.PreviewKeyDown += HImageWindow_PreviewKeyDown;
        }

        private void HImageWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ViewModel.HandleKeyDown(e);
        }

        private void OnHImageWindowLoaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
            _isWindowReady = true;
            RedrawSynchronous();
        }

        #region 图像层和绘制层处理

        private static void OnImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = d as HImageWindow;
            if (window != null)
            {
                window.SetPartToFitImage();
                window.RedrawSynchronous();
            }
        }

        private static void OnGraphicsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as HImageWindow;
            if (control == null) return;

            if (e.OldValue is ObservableCollection<ROI> oldCollection)
            {
                oldCollection.CollectionChanged -= control.OnGraphicsCollectionChanged;
                // 我们还需要取消订阅每个 ROI 内部的 Shapes 集合
                foreach (var roi in oldCollection)
                {
                    roi.Shapes.CollectionChanged -= control.OnShapesCollectionChanged;
                }
            }
            if (e.NewValue is ObservableCollection<ROI> newCollection)
            {
                newCollection.CollectionChanged += control.OnGraphicsCollectionChanged;
                foreach (var roi in newCollection)
                {
                    roi.Shapes.CollectionChanged += control.OnShapesCollectionChanged;
                }
            }
            control.RedrawSynchronous();
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

            if (Image != null && Image.IsInitialized())
            {
                hWindow.DispObj(Image);
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
            if (Image == null || !Image.IsInitialized()) return;

            // *** 保护 SetPartToFitImage ***
            if (!_isWindowReady || HalconWindow.HalconWindow == null) return;

            try
            {
                HOperatorSet.SmallestRectangle1(Image, out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2);
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