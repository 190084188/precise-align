using HalconDotNet;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace PreciseAlign.Controls
{
    public partial class HImageWindow : UserControl
    {
        #region 属性设置

        // 工具状态枚举列表，用于指示工具按钮的激活状态，默认为选择工具
        private enum ActiveTool { Selection, DrawRectangle1, DrawRectangle2, DrawCircle }
        private ActiveTool _activeTool = ActiveTool.Selection;

        private readonly List<ToggleButton> _toolButtons;

        private bool _isDrawing = false;
        private HDrawingObject? _newlyCreatedObject;

        private bool _isPanning = false;
        private Point _panStartPoint;
        private HTuple? _initialPart;

        private bool _isAreaSelecting = false;

        private double _startRow, _startCol;

        private readonly ObservableCollection<HDrawingObject> _selectedObjects = [];

        private HWindow? _selectionWindow;
        // 定义ROI的样式
        private const string SELECTED_COLOR = "cyan";
        private const string DESELECTED_COLOR = "red";



        #region 依赖属性
        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register(
                "Image", typeof(HObject), typeof(HImageWindow),
                new PropertyMetadata(null, OnImageChanged));

        public static readonly DependencyProperty GraphicsProperty =
            DependencyProperty.Register(
                "Graphics", typeof(ObservableCollection<HDrawingObject>), typeof(HImageWindow),
                new PropertyMetadata(null, OnGraphicsChanged));

        public static readonly DependencyProperty ResultGraphicsProperty =
            DependencyProperty.Register(
                "ResultGraphics", typeof(HObject), typeof(HImageWindow),
                new PropertyMetadata(null, OnImageChanged));

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
        #endregion
        #endregion

        public HImageWindow()
        {
            InitializeComponent();
            _toolButtons = [BtnDrawRectangle1, BtnDrawRectangle2, BtnDrawCircle] ;
            SetValue(GraphicsProperty, new ObservableCollection<HDrawingObject>());
            Focusable = true;
            Loaded += (s, e) => Focus();
            PreviewKeyDown += HImageWindow_PreviewKeyDown;

            HalconWindow.PreviewMouseRightButtonDown += HalconWindow_PreviewMouseRightButtonDown;
            HalconWindow.PreviewMouseRightButtonUp += HalconWindow_PreviewMouseRightButtonUp;
            HalconWindow.MouseMove += HalconWindow_WPFMouseMove;

            HalconWindow.HMoveContent = false;

            _selectedObjects.CollectionChanged += OnSelectedObjectsChanged;
        }

        #region Image and Graphics Handling

        private static void OnImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = d as HImageWindow;
            if (window != null)
            {
                window.SetPartToFitImage();
                window.FullRedraw();
            }
        }

        public void UpdateImage(HObject image, HObject results = null)
        {
            if (image == null || !image.IsInitialized()) return;
            var hWindow = HalconWindow.HalconWindow;
            Image = image;
            if (results != null && results.IsInitialized())
            {
                ResultGraphics = results;
            }
        }

        public void FullRedraw()
        {
            Dispatcher.Invoke(() =>
            {
                var hWindow = HalconWindow.HalconWindow;
                hWindow.ClearWindow();

                if (Image != null && Image.IsInitialized())
                {
                    hWindow.DispObj(Image);
                }

                if (ResultGraphics != null && ResultGraphics.IsInitialized())
                {
                    hWindow.SetColor("red");
                    hWindow.SetLineWidth(2);
                    hWindow.DispObj(ResultGraphics);
                }

                //AttachAllDrawingObjects(Graphics);
            });
        }

        // 当Graphics属性发生变化时，将Graphics附加到Halcon窗口
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

        // 当有新的HDrawingObject添加到Graphics时，将它们附加到Halcon窗口
        private void OnGraphicsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var hWindow = HalconWindow.HalconWindow;
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems!=null)
            {
                foreach (HDrawingObject item in e.NewItems)
                {
                    hWindow.AttachDrawingObjectToWindow(item);
                    item.OnSelect(OnDrawingObjectSelected);
                    SetObjectStyle(item, DESELECTED_COLOR, 2);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                foreach (HDrawingObject item in e.OldItems)
                {
                    if (_selectedObjects.Contains(item))
                    {
                        _selectedObjects.Remove(item);
                    }
                    hWindow.DetachDrawingObjectFromWindow(item);
                }
            }
        }
        #endregion

        #region Selection, Deletion, and Panning (Mouse/Keyboard)

        private void OnDrawingObjectSelected(HDrawingObject dobj, HWindow window, string type)
        {
            Debug.WriteLine($"OnDrawingObjectSelected: {type}");
            if (type == "on_select")
            {
                if (!_selectedObjects.Contains(dobj))
                {
                    _selectedObjects.Clear();
                    _selectedObjects.Add(dobj);
                }
                Debug.WriteLine($"_selectedObjects.Contains(dobj) = {_selectedObjects.Contains(dobj)}");
                Focus();
            }
        }

        private void OnSelectedObjectsChanged(object sender, NotifyCollectionChangedEventArgs e) 
        {
            Debug.WriteLine($"SelectedObjects集合发生变化: {e.Action}");
            UpdateAllObjectStyles();
        }

        private void HImageWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && _selectedObjects.Any())
            {
                var itemsToRemove = _selectedObjects.ToList();
                foreach (var item in itemsToRemove)
                {
                    Graphics.Remove(item);
                }
                _selectedObjects.Clear();
                e.Handled = true;
            }
        }

        private void HalconWindow_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawing) return;
            _isPanning = true;
            _panStartPoint = e.GetPosition(HalconWindow);
            HalconWindow.HalconWindow.GetPart(out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2);
            _initialPart = new HTuple(row1, col1, row2, col2);
            HalconWindow.CaptureMouse();
        }

        private void HalconWindow_WPFMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPanning) return;

            Point currentPoint = e.GetPosition(HalconWindow);
            double deltaX = currentPoint.X - _panStartPoint.X;
            double deltaY = currentPoint.Y - _panStartPoint.Y;

            double partWidth = _initialPart[3].D - _initialPart[1].D;
            double partHeight = _initialPart[2].D - _initialPart[0].D;

            if (partWidth <= 0 || partHeight <= 0) return;

            double zoomX = HalconWindow.ActualWidth / partWidth;
            double zoomY = HalconWindow.ActualHeight / partHeight;

            double deltaRow = -deltaY / zoomY;
            double deltaCol = -deltaX / zoomX;

            HalconWindow.HalconWindow.SetPart(
                _initialPart[0].D + deltaRow,
                _initialPart[1].D + deltaCol,
                _initialPart[2].D + deltaRow,
                _initialPart[3].D + deltaCol
            );
        }

        private void HalconWindow_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isPanning = false;
            HalconWindow.ReleaseMouseCapture();
        }
        #endregion

        #region UI Button Logic (No Changes)
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

                double newRow1, newCol1, newRow2, newCol2;

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
            catch (HalconException)
            {
                // Ignore
            }
        }

        private void BtnFit_Click(object sender, RoutedEventArgs e) => SetPartToFitImage();

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearAllDrawingObjects(Graphics);
            Graphics.Clear();
            _selectedObjects.Clear();
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
            UpdateActiveTool();
        }
        #endregion

        #region Main Drawing and Selection Logic

        private void UpdateActiveTool()
        {
            if (BtnDrawRectangle1.IsChecked == true) _activeTool = ActiveTool.DrawRectangle1;
            else if (BtnDrawRectangle2.IsChecked == true) _activeTool = ActiveTool.DrawRectangle2;
            else if (BtnDrawCircle.IsChecked == true) _activeTool = ActiveTool.DrawCircle;
            else _activeTool = ActiveTool.Selection;
        }

        private void HalconWindow_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (Mouse.RightButton == MouseButtonState.Pressed) return;

            _startRow = e.Row;
            _startCol = e.Column;

            // 检查是否点击在ROI上
            bool isClickInsideROI = false;
            if (Graphics != null)
            {
                foreach (var dobj in Graphics)
                {
                    using (var region = new HRegion(dobj.GetDrawingObjectIconic()))
                    {
                        if (region.TestRegionPoint(e.Row, e.Column) > 0)
                        {
                            isClickInsideROI = true;
                            break;
                        }
                    }
                }
            }

            if (_activeTool != ActiveTool.Selection)
            {
                // 开始绘制新ROI时，清除所有已选中的ROI
                ResetSelectedObjectAppearance();
                if (Graphics == null) SetValue(GraphicsProperty, new ObservableCollection<HDrawingObject>());
                _isDrawing = true;
                switch (_activeTool)
                {
                    case ActiveTool.DrawRectangle1:
                        double r1 = Math.Min(_startRow, e.Row);
                        double c1 = Math.Min(_startCol, e.Column);
                        double r2 = Math.Max(_startRow, e.Row);
                        double c2 = Math.Max(_startCol, e.Column);
                        _newlyCreatedObject = HDrawingObject.CreateDrawingObject(HDrawingObject.HDrawingObjectType.RECTANGLE1, _startRow, _startCol, _startRow, _startCol);
                        break;
                    case ActiveTool.DrawRectangle2:
                        _newlyCreatedObject = HDrawingObject.CreateDrawingObject(HDrawingObject.HDrawingObjectType.RECTANGLE2, _startRow, _startCol, 0, 1, 1);
                        break;
                    case ActiveTool.DrawCircle:
                        _newlyCreatedObject = HDrawingObject.CreateDrawingObject(HDrawingObject.HDrawingObjectType.CIRCLE, _startRow, _startCol, 1);
                        break;
                }
                if (_newlyCreatedObject != null && Graphics != null) Graphics.Add(_newlyCreatedObject);
            }
            else if (_activeTool == ActiveTool.Selection && !isClickInsideROI)
            {
                _isAreaSelecting = true;
                ResetSelectedObjectAppearance();
                HalconWindow.HDrawingObjectsModifier = HSmartWindowControlWPF.DrawingObjectsModifier.Ctrl;
                _selectedObjects.Clear();
            }
        }

        private void HalconWindow_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (_isDrawing && _newlyCreatedObject != null)
            {
                switch (_activeTool)
                {
                    case ActiveTool.DrawRectangle1:
                        double r1 = Math.Min(_startRow, e.Row);
                        double c1 = Math.Min(_startCol, e.Column);
                        double r2 = Math.Max(_startRow, e.Row);
                        double c2 = Math.Max(_startCol, e.Column);
                        _newlyCreatedObject.SetDrawingObjectParams(
                            new HTuple("row1", "column1", "row2", "column2"),
                            new HTuple(r1, c1, r2, c2));
                        break;
                    case ActiveTool.DrawRectangle2:
                        double angle = Math.Atan2(_startRow - e.Row, e.Column - _startCol);
                        double length1 = Math.Sqrt(Math.Pow(e.Column - _startCol, 2) + Math.Pow(e.Row - _startRow, 2)) / 2;
                        if (length1 < 0.001) length1 = 0.001;
                        _newlyCreatedObject.SetDrawingObjectParams(
                            new HTuple("row", "column", "phi", "length1", "length2"),
                            new HTuple((_startRow + e.Row) / 2, (_startCol + e.Column) / 2, angle, length1, length1 / 2));
                        break;
                    case ActiveTool.DrawCircle:
                        double radius = Math.Sqrt(Math.Pow(e.Row - _startRow, 2) + Math.Pow(e.Column - _startCol, 2));
                        if (radius < 0.001) radius = 0.001;
                        _newlyCreatedObject.SetDrawingObjectParams("radius", radius);
                        break;
                }
            }
            else if (_isAreaSelecting)
            {
                FullRedraw();

                double r1 = Math.Min(_startRow, e.Row);
                double c1 = Math.Min(_startCol, e.Column);
                double r2 = Math.Max(_startRow, e.Row);
                double c2 = Math.Max(_startCol, e.Column);
                if (_selectionWindow == null)
                {
                    _selectionWindow = HalconWindow.HalconWindow;
                    _selectionWindow.SetColor("green");
                    _selectionWindow.SetDraw("margin");
                    _selectionWindow.SetLineStyle(new HTuple(10, 5));
                }
                _selectionWindow?.DispRectangle1(r1, c1, r2, c2);
            }
        }

        private void HalconWindow_HMouseUp(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (_isDrawing)
            {
                _isDrawing = false;
                _newlyCreatedObject = null;

                // 自动退出绘图模式，activeButton为当前被选中的ToggleButton
                var activeButton = _toolButtons.FirstOrDefault(b => b.IsChecked == true);
                if (activeButton != null)
                {
                    activeButton.IsChecked = false;
                    UpdateActiveTool();
                }

                // 延迟重绘确保状态正确
                Dispatcher.BeginInvoke(new Action(FullRedraw), DispatcherPriority.ContextIdle);
            }

            else if (_isAreaSelecting)
            {
                _isAreaSelecting = false;
                this.HalconWindow.HDrawingObjectsModifier = HalconDotNet.HSmartWindowControlWPF.DrawingObjectsModifier.None;
                double r1 = Math.Min(_startRow, e.Row);
                double c1 = Math.Min(_startCol, e.Column);
                double r2 = Math.Max(_startRow, e.Row);
                double c2 = Math.Max(_startCol, e.Column);

                using (var selectionRectangle = new HRegion())
                {
                    // 只有真正的拖动（非点击）才进行区域选择
                    if (Math.Abs(r1 - r2) > 1 || Math.Abs(c1 - c2) > 1)
                    {
                        selectionRectangle.GenRectangle1(r1, c1, r2, c2);
                        foreach (var dobj in Graphics)
                        {
                            using (HRegion objRegion = new HRegion(dobj.GetDrawingObjectIconic()))
                            using (HRegion intersection = selectionRectangle.Intersection(objRegion))
                            {
                                if (intersection.Area > 0.001)
                                {
                                    if (!_selectedObjects.Contains(dobj))
                                    {
                                        _selectedObjects.Add(dobj);
                                    }
                                }
                            }
                        }
                    }
                }
                FullRedraw();
                this.Focus();
            }
        }
        #endregion

        #region 辅助函数
        private void ClearAllDrawingObjects(IEnumerable<HDrawingObject> collection)
        {
            if (collection == null) return;
            var hWindow = HalconWindow.HalconWindow;
            foreach (var obj in collection.ToList())
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
                    obj.OnSelect(OnDrawingObjectSelected);

                    // [实现功能 1] 附加时，根据是否在选中列表来设置样式
                    if (_selectedObjects.Contains(obj))
                    {
                        SetObjectStyle(obj, SELECTED_COLOR, 2);
                    }
                    else
                    {
                        SetObjectStyle(obj, DESELECTED_COLOR, 2);
                    }
                }
            }
        }

        private void SetObjectStyle(HDrawingObject dobj, string color, int lineWidth)
        {
            if (dobj != null && dobj.IsInitialized())
            {
                try
                {
                    dobj.SetDrawingObjectParams("color", color);
                    dobj.SetDrawingObjectParams("line_width", lineWidth);
                }
                catch (HalconException ex)
                {
                    Debug.WriteLine($"SetObjectStyle failed: {ex.Message}");
                }
            }
        }
        private void UpdateAllObjectStyles()
        {
            if (Graphics == null) return;
            foreach (var dobj in Graphics)
            {
                if (_selectedObjects.Contains(dobj))
                {
                    SetObjectStyle(dobj, SELECTED_COLOR, 2);
                }
                else
                {
                    SetObjectStyle(dobj, DESELECTED_COLOR, 2);
                }
            }
        }
        private void ResetSelectedObjectAppearance()
        {
            _selectedObjects.Clear();
        }
        #endregion
    }
}