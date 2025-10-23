using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;
using System.Diagnostics;

namespace PreciseAlign.Controls
{
    public partial class HImageWindow : UserControl
    {
        // ... [枚举, 状态变量, 和 ImageProperty, GraphicsProperty 定义保持不变] ...
        private enum ActiveTool { None, Move, DrawRectangle1, DrawRectangle2, DrawCircle }
        private ActiveTool _activeTool = ActiveTool.None;
        private List<ToggleButton> _toolButtons;
        private bool _isDrawing = false;
        private HDrawingObject _newlyCreatedObject;
        private double _startRow, _startCol;

        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register(
                "Image", typeof(HObject), typeof(HImageWindow),
                new PropertyMetadata(null, OnImageChanged));

        public static readonly DependencyProperty GraphicsProperty =
            DependencyProperty.Register(
                "Graphics", typeof(ObservableCollection<HDrawingObject>), typeof(HImageWindow),
                new PropertyMetadata(null, OnGraphicsChanged));

        // ★★★ 新增：用于显示算法静态结果的依赖属性 ★★★
        public static readonly DependencyProperty ResultGraphicsProperty =
            DependencyProperty.Register(
                "ResultGraphics", typeof(HObject), typeof(HImageWindow),
                new PropertyMetadata(null, OnImageChanged)); // 复用OnImageChanged来触发重绘

        public HObject Image
        {
            get { return (HObject)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        public ObservableCollection<HDrawingObject> Graphics
        {
            get { return (ObservableCollection<HDrawingObject>)GetValue(GraphicsProperty); }
            set { SetValue(GraphicsProperty, value); }
        }

        public HObject ResultGraphics
        {
            get { return (HObject)GetValue(ResultGraphicsProperty); }
            set { SetValue(ResultGraphicsProperty, value); }
        }

        public HImageWindow()
        {
            InitializeComponent();
            _toolButtons = new List<ToggleButton>
            {
                BtnPointer, BtnMove, BtnDrawRectangle1, BtnDrawRectangle2, BtnDrawCircle
            };
            SetValue(GraphicsProperty, new ObservableCollection<HDrawingObject>());
        }

        private static void OnImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as HImageWindow)?.DisplayImage();
        }

        private static void OnGraphicsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as HImageWindow;
            if (control == null) return;

            if (e.OldValue is ObservableCollection<HDrawingObject> oldCollection)
            {
                oldCollection.CollectionChanged -= control.OnGraphicsCollectionChanged;
                control.ClearAllDrawingObjects(oldCollection);
            }
            if (e.NewValue is ObservableCollection<HDrawingObject> newCollection)
            {
                newCollection.CollectionChanged += control.OnGraphicsCollectionChanged;
                control.AttachAllDrawingObjects(newCollection);
            }
        }

        private void OnGraphicsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var hWindow = HalconWindow.HalconWindow;
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (HDrawingObject item in e.NewItems)
                {
                    hWindow.AttachDrawingObjectToWindow(item);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (HDrawingObject item in e.OldItems)
                {
                    hWindow.DetachDrawingObjectFromWindow(item);
                }
            }
        }

        private void DisplayImage()
        {
            Dispatcher.Invoke(() =>
            {
                var hWindow = HalconWindow.HalconWindow;
                hWindow.ClearWindow();

                if (Image != null && Image.IsInitialized())
                {
                    SetPartToFitImage();
                    hWindow.DispObj(Image);
                }

                // ★★★ 修改：叠加显示静态结果图形 ★★★
                if (ResultGraphics != null && ResultGraphics.IsInitialized())
                {
                    // 在这里可以为结果设置特定颜色，例如红色
                    hWindow.SetColor("red");
                    hWindow.SetLineWidth(2);
                    hWindow.DispObj(ResultGraphics);
                }

                // 重新附加所有交互式绘图对象，确保它们在最顶层
                AttachAllDrawingObjects(Graphics);
            });
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                Filter = "图像文件 (*.png;*.jpg;*.bmp;*.tif)|*.png;*.jpg;*.bmp;*.tif*.tiff;*.gif|所有文件|*.*",
                Title = "加载图像文件"
            };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    HObject ho_Image;
                    HOperatorSet.ReadImage(out ho_Image, ofd.FileName);
                    Image = ho_Image;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"加载图像文件失败：{ex}");
                }
            }
        }

        private void SetPartToFitImage()
        {
            if (Image == null || !Image.IsInitialized()) return;

            // 方案1: 如果对象是 HImage (最理想的情况)
            if (Image is HImage himage)
            {
                himage.GetImageSize(out HTuple width, out HTuple height);
                HalconWindow.HalconWindow.SetPart(0, 0, height.I - 1, width.I - 1);
            }
            // 方案2: 如果对象不是 HImage (例如 HRegion, HXLD)
            else
            {
                try
                {
                    // --- 第1步：获取对象的实际边界 ---
                    // 使用 SmallestRectangle1 作为通用方法，它对 HImage, HRegion 等都有效
                    HOperatorSet.SmallestRectangle1(Image, out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2);

                    double objHeight = row2.D - row1.D;
                    double objWidth = col2.D - col1.D;

                    // 如果对象没有尺寸，则无需操作
                    if (objHeight <= 0 || objWidth <= 0) return;

                    // --- 第2步：获取控件的实际像素尺寸 ---
                    double viewHeight = HalconWindow.ActualHeight;
                    double viewWidth = HalconWindow.ActualWidth;

                    if (viewHeight <= 0 || viewWidth <= 0) return;

                    // --- 第3步：比较对象和控件的长宽比 ---
                    double objAspectRatio = objWidth / objHeight;
                    double viewAspectRatio = viewWidth / viewHeight;

                    double newRow1, newCol1, newRow2, newCol2;

                    if (objAspectRatio > viewAspectRatio)
                    {
                        // 对象比控件更“宽”，以对象的宽度为基准
                        double newHeight = objWidth / viewAspectRatio;
                        double centerRow = (row1.D + row2.D) / 2;
                        newRow1 = centerRow - newHeight / 2;
                        newRow2 = centerRow + newHeight / 2;
                        newCol1 = col1.D;
                        newCol2 = col2.D;
                    }
                    else
                    {
                        // 对象比控件更“高”或比例相同，以对象的高度为基准
                        double newWidth = objHeight * viewAspectRatio;
                        double centerCol = (col1.D + col2.D) / 2;
                        newCol1 = centerCol - newWidth / 2;
                        newCol2 = centerCol + newWidth / 2;
                        newRow1 = row1.D;
                        newRow2 = row2.D;
                    }

                    // --- 第4步：使用新计算的坐标调用 SetPart ---
                    HalconWindow.HalconWindow.SetPart(newRow1, newCol1, newRow2, newCol2);
                }
                catch (HalconException)
                {
                    // 如果对象是空的或无法计算边界，则不进行任何操作
                }
            }
        }

        private void BtnFit_Click(object sender, RoutedEventArgs e) => SetPartToFitImage();

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearAllDrawingObjects(Graphics);
            Graphics.Clear();
        }
        private void Tool_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as ToggleButton;
            foreach (var button in _toolButtons)
            {
                if (button != clickedButton)
                {
                    button.IsChecked = false;
                }
            }
            if (_toolButtons.All(b => b.IsChecked == false))
            {
                BtnPointer.IsChecked = true;
            }
            UpdateActiveTool();
        }

        private void UpdateActiveTool()
        {
            HalconWindow.HMoveContent = false;

            if (BtnMove.IsChecked == true) _activeTool = ActiveTool.Move;
            else if (BtnDrawRectangle1.IsChecked == true) _activeTool = ActiveTool.DrawRectangle1;
            else if (BtnDrawRectangle2.IsChecked == true) _activeTool = ActiveTool.DrawRectangle2;
            else if (BtnDrawCircle.IsChecked == true) _activeTool = ActiveTool.DrawCircle;
            else _activeTool = ActiveTool.None;

            if (_activeTool == ActiveTool.Move)
            {
                HalconWindow.HMoveContent = true;
            }
        }
        private void HalconWindow_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (_activeTool == ActiveTool.None || _activeTool == ActiveTool.Move) return;

            if (Graphics == null)
            {
                SetValue(GraphicsProperty, new ObservableCollection<HDrawingObject>());
            }

            _isDrawing = true;
            _startRow = e.Row;
            _startCol = e.Column;

            switch (_activeTool)
            {
                case ActiveTool.DrawRectangle1:
                    _newlyCreatedObject = HDrawingObject.CreateDrawingObject(
                        HDrawingObject.HDrawingObjectType.RECTANGLE1, _startRow, _startCol, _startRow, _startCol);
                    break;
                case ActiveTool.DrawRectangle2:
                    _newlyCreatedObject = HDrawingObject.CreateDrawingObject(
                        HDrawingObject.HDrawingObjectType.RECTANGLE2, _startRow, _startCol, 0, 1, 1);
                    break;
                case ActiveTool.DrawCircle:
                    _newlyCreatedObject = HDrawingObject.CreateDrawingObject(
                        HDrawingObject.HDrawingObjectType.CIRCLE, _startRow, _startCol, 1);
                    break;
            }

            if (_newlyCreatedObject != null)
            {
                Graphics.Add(_newlyCreatedObject);
            }
        }

        private void HalconWindow_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (!_isDrawing || _newlyCreatedObject == null) return;

            switch (_activeTool)
            {
                case ActiveTool.DrawRectangle1:
                    _newlyCreatedObject.SetDrawingObjectParams(
                        new HTuple("row1", "column1", "row2", "column2"),
                        new HTuple(_startRow, _startCol, e.Row, e.Column));
                    break;
                case ActiveTool.DrawRectangle2:
                    double angle = Math.Atan2(_startRow - e.Row, e.Column - _startCol);
                    double length1 = Math.Sqrt(Math.Pow(e.Column - _startCol, 2) + Math.Pow(e.Row - _startRow, 2)) / 2;
                    double midRow = (_startRow + e.Row) / 2;
                    double midCol = (_startCol + e.Column) / 2;
                    _newlyCreatedObject.SetDrawingObjectParams(
                         new HTuple("row", "column", "phi", "length1", "length2"),
                         new HTuple(midRow, midCol, angle, length1, length1 / 2));
                    break;
                case ActiveTool.DrawCircle:
                    double radius = Math.Sqrt(Math.Pow(e.Row - _startRow, 2) + Math.Pow(e.Column - _startCol, 2));
                    _newlyCreatedObject.SetDrawingObjectParams("radius", radius);
                    break;
            }
        }

        private void HalconWindow_HMouseUp(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (!_isDrawing) return;

            _isDrawing = false;
            _newlyCreatedObject = null;

            BtnPointer.IsChecked = true;
            Tool_Click(BtnPointer, new RoutedEventArgs());
        }
        private void ClearAllDrawingObjects(IEnumerable<HDrawingObject> collection)
        {
            if (collection == null) return;
            var hWindow = HalconWindow.HalconWindow;
            foreach (var obj in collection.ToList()) // ToList() creates a copy to avoid modification issues
            {
                if (obj.IsInitialized())
                {
                    hWindow.DetachDrawingObjectFromWindow(obj);
                }
            }
        }


        private void AttachAllDrawingObjects(IEnumerable<HDrawingObject> collection)
        {
            if (collection == null) return;
            var hWindow = HalconWindow.HalconWindow;
            foreach (var obj in collection)
            {
                if (obj.IsInitialized())
                {
                    hWindow.AttachDrawingObjectToWindow(obj);
                }
            }
        }
    }
}