// HImageWindowViewModel.cs

using HalconDotNet;
using Microsoft.Win32;
using PreciseAlign.Controls.Mvvm;
using PreciseAlign.Core.Models;
using PreciseAlign.Core.Mvvm;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace PreciseAlign.Controls
{
    public enum ActiveToolMode
    {
        None,
        Selection,
        Moving,
        DrawRectangle1,
        DrawRectangle2,
        DrawCircle
    }

    public class HImageWindowViewModel : ObservableObject
    {
        #region 事件声明 (View 订阅这些事件以更新 UI)

        /// <summary>
        /// 请求 View 执行重绘 (通常对应 RedrawSynchronous)
        /// </summary>
        public event Action? RequestRepaint;

        /// <summary>
        /// 请求 View 执行图像适应窗口 (SetPartToFitImage)
        /// </summary>
        public event Action? RequestAutoFit;

        #endregion

        #region 属性设置
        private const string SELECTED_COLOR = "cyan";
        private const string DESELECTED_COLOR = "red";
        // --- 核心数据属性 ---
        private HObject? _image;
        /// <summary>
        /// 当前显示的图像
        /// </summary>
        public HObject? Image
        {
            get => _image;
            set
            {
                // 注意：这里暂不Dispose旧图像，因为可能由外部DP管理生命周期。
                // 如果确定完全由VM管理，应在此处 Dispose _image
                if (SetProperty(ref _image, value))
                {
                    // 图像变更自动触发重绘和适应窗口
                    RequestAutoFit?.Invoke();
                    RequestRepaint?.Invoke();
                }
            }
        }
        // 图形集合 (直接在 VM 中管理)
        private ObservableCollection<ROI> _graphics = new ObservableCollection<ROI>();
        public ObservableCollection<ROI> Graphics
        {
            get => _graphics;
            set
            {
                if (_graphics != value)
                {
                    // 1. 取消订阅旧集合的事件
                    if (_graphics != null)
                    {
                        _graphics.CollectionChanged -= OnGraphicsCollectionChanged;
                        foreach (var roi in _graphics)
                        {
                            roi.Shapes.CollectionChanged -= OnShapesCollectionChanged;
                        }
                    }

                    _graphics = value ?? new ObservableCollection<ROI>();

                    // 2. 订阅新集合的事件
                    if (_graphics != null)
                    {
                        _graphics.CollectionChanged += OnGraphicsCollectionChanged;
                        foreach (var roi in _graphics)
                        {
                            roi.Shapes.CollectionChanged += OnShapesCollectionChanged;
                        }
                    }

                    OnPropertyChanged();
                    RequestRepaint?.Invoke();
                }
            }
        }
        // 交互状态
        private double _mouseStartRow, _mouseStartCol;

        private enum InteractionState { None, CreatingShape, DraggingHandle, DraggingBody, AreaSelecting }
        private InteractionState _currentState = InteractionState.None;

        private IShape? _activeShape;
        private HitTestHandle _activeHandle;

        private ActiveToolMode _activeTool;
        public ActiveToolMode ActiveTool
        {
            get => _activeTool;
            set
            {
                if (SetProperty(ref _activeTool, value))
                {
                    // 切换工具时重置状态
                    _currentState = InteractionState.None;
                    _activeShape = null;
                }
            }
        }

        public HWindow? AreaSelectionWindow { get; set; }

        private readonly ObservableCollection<IShape> _selectedObjects = [];

        private long _lastRenderTime = 0;
        private const int MIN_RENDER_INTERVAL = 15;

        #endregion

        #region 命令
        public ICommand LoadImageCommand { get; }
        public ICommand FitImageCommand { get; }
        public ICommand ClearGraphicsCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        #endregion



        public HImageWindowViewModel()
        {
            LoadImageCommand = new RelayCommand(LoadImage);
            FitImageCommand = new RelayCommand(() => RequestAutoFit?.Invoke());
            ClearGraphicsCommand = new RelayCommand(ClearGraphics);
            DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => _selectedObjects.Any());

            _selectedObjects.CollectionChanged += (s, e) =>
            {
                UpdateAllObjectStyles();
                (DeleteSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
            };

            ActiveTool = ActiveToolMode.Selection;

            // Graphics 集合变更监听
            Graphics.CollectionChanged += (s, e) => RequestRepaint?.Invoke();
        }

        // 监听 ROI 列表的变化 (例如：添加/删除了一个 ROI 分组)
        private void OnGraphicsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // 当有新 ROI 被添加时，我们要监听这个 ROI 内部 Shapes 的变化
            if (e.NewItems != null)
            {
                foreach (ROI newItem in e.NewItems)
                {
                    newItem.Shapes.CollectionChanged += OnShapesCollectionChanged;
                }
            }
            // 当 ROI 被移除时，取消监听
            if (e.OldItems != null)
            {
                foreach (ROI oldItem in e.OldItems)
                {
                    oldItem.Shapes.CollectionChanged -= OnShapesCollectionChanged;
                }
            }
            RequestRepaint?.Invoke();
        }
        // 监听具体 Shape 的变化 (例如：ROI 里的矩形被删除了)
        private void OnShapesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RequestRepaint?.Invoke();
        }
        #region 命令实现
        private void LoadImage()
        {
            var ofd = new OpenFileDialog()
            {
                Filter = "图像文件 (*.png;*.jpg;*.bmp;*.tif)|*.png;*.jpg;*.bmp*.tiff;*.gif|所有文件|*.*",
                Title = "加载图像文件"
            };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    HOperatorSet.ReadImage(out HObject ho_Image, ofd.FileName);
                    Image = ho_Image;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"加载图像文件失败：{ex}");
                }
            }
        }

        private void ClearGraphics()
        {
            Graphics.Clear();
            _selectedObjects.Clear();
            RequestRepaint?.Invoke();
        }

        private void DeleteSelected()
        {
            if (Graphics == null) return;
            bool changed = false;
            foreach (var roi in Graphics)
            {
                var shapesToRemove = roi.Shapes.Where(s => _selectedObjects.Contains(s)).ToList();
                foreach (var shape in shapesToRemove)
                {
                    roi.Shapes.Remove(shape);
                    changed = true;
                }
            }
            _selectedObjects.Clear();
            if (changed) RequestRepaint?.Invoke();
        }
        #endregion

        #region 事件处理程序 (完全重构)

        public void HandleMouseDown(HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            _mouseStartRow = e.Row;
            _mouseStartCol = e.Column;
            _currentState = InteractionState.None; // 重置状态

            if (ActiveTool == ActiveToolMode.Selection)
            {
                ShapeHitTestResult hitResult = FindHitShape(e.Row, e.Column);
                if (hitResult.HasHit)
                {
                    _activeShape = hitResult.Shape;
                    _activeHandle = hitResult.Handle;
                    if (_activeHandle == HitTestHandle.Body || _activeHandle == HitTestHandle.Center)
                    {
                        _currentState = InteractionState.DraggingBody;
                    }
                    else
                    {
                        _currentState = InteractionState.DraggingHandle;
                    }
                    if (Keyboard.Modifiers != ModifierKeys.Control)
                    {
                        if (!_selectedObjects.Contains(_activeShape!))
                        {
                            _selectedObjects.Clear();
                            _selectedObjects.Add(_activeShape!);
                        }
                    }
                    else
                    {
                        if (_selectedObjects.Contains(_activeShape!))
                        {
                            _selectedObjects.Remove(_activeShape!);
                        }
                        else
                        {
                            _selectedObjects.Add(_activeShape!);
                        }
                    }
                }
                else
                {
                    _currentState = InteractionState.AreaSelecting;
                    if (Keyboard.Modifiers != ModifierKeys.Control)
                    {
                        _selectedObjects.Clear();
                    }
                }
            }
            else if (ActiveTool >= ActiveToolMode.DrawRectangle1)
            {
                // --- 开始创建新形状 ---
                _currentState = InteractionState.CreatingShape;
                try
                {
                    switch (ActiveTool)
                    {
                        case ActiveToolMode.DrawRectangle1:
                            _activeShape = new ShapeRectangle1(e.Row, e.Column, e.Row, e.Column);
                            break;
                        case ActiveToolMode.DrawRectangle2:
                            _activeShape = new ShapeRectangle2(e.Row, e.Column, 0, 0, 0);
                            break;
                        case ActiveToolMode.DrawCircle:
                            _activeShape = new ShapeCircle(e.Row, e.Column, 0);
                            break;
                        default:
                            _currentState = InteractionState.None;
                            return;
                    }
                    _activeShape.Color = SELECTED_COLOR;
                    var defaultRoi = Graphics.FirstOrDefault(r => r.Name == "Default");
                    if (defaultRoi == null)
                    {
                        defaultRoi = new ROI { Name = "Default" };
                        Graphics.Add(defaultRoi);
                        // Graphics CollectionChanged 会触发重绘，但这里也可以显式调用以确保响应
                        // RequestRepaint?.Invoke();
                    }
                    defaultRoi.Shapes.Add(_activeShape);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"创建 Shape 失败: {ex.Message}");
                    _currentState = InteractionState.None;
                }
            }
        }
        public void HandleMouseMove(HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            // TODO: 更新鼠标坐标显示
            long currentTime = Stopwatch.GetTimestamp();
            // 将 Timestamp 转换为毫秒 (TimeSpan.TicksPerMillisecond 在某些高频计时器下不适用，这是通用写法)
            long elapsedMs = (currentTime - _lastRenderTime) * 1000 / Stopwatch.Frequency;
            if (elapsedMs < MIN_RENDER_INTERVAL)
            {
                return; // 还没到刷新时间，跳过重绘请求
            }
            _lastRenderTime = currentTime;
            switch (_currentState)
            {
                case InteractionState.DraggingBody:
                    double rowOffset = e.Row - _mouseStartRow;
                    double colOffset = e.Column - _mouseStartCol;
                    _activeShape?.Move(rowOffset, colOffset);
                    _mouseStartRow = e.Row;
                    _mouseStartCol = e.Column;
                    RequestRepaint?.Invoke();
                    break;

                case InteractionState.DraggingHandle:
                    _activeShape?.DragHandle(_activeHandle, e.Row, e.Column);
                    RequestRepaint?.Invoke();
                    break;
                case InteractionState.CreatingShape:
                    try
                    {
                        if (_activeShape is ShapeRectangle1 rect)
                        {
                            rect.Row2 = e.Row;
                            rect.Column2 = e.Column;
                        }
                        else if (_activeShape is ShapeRectangle2 rect2)
                        {
                            rect2.Phi = HMisc.AngleLx(rect2.Row, rect2.Column, e.Row, e.Column);
                            rect2.Length1 = HMisc.DistancePp(rect2.Row, rect2.Column, e.Row, e.Column);
                            rect2.Length2 = rect2.Length1 / 2; // 固定比例
                        }
                        else if (_activeShape is ShapeCircle circ)
                        {
                            circ.Radius = HMisc.DistancePp(circ.Row, circ.Column, e.Row, e.Column);
                        }
                        RequestRepaint?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"更新 Shape 失败: {ex.Message}");
                    }
                    break;
                case InteractionState.AreaSelecting:
                    if (AreaSelectionWindow != null)
                    {
                        RequestRepaint?.Invoke();
                        try
                        {
                            double r1 = Math.Min(_mouseStartRow, e.Row);
                            double c1 = Math.Min(_mouseStartCol, e.Column);
                            double r2 = Math.Max(_mouseStartRow, e.Row);
                            double c2 = Math.Max(_mouseStartCol, e.Column);
                            AreaSelectionWindow.SetColor("green");
                            AreaSelectionWindow.SetDraw("margin");
                            AreaSelectionWindow.SetLineWidth(2);
                            AreaSelectionWindow.SetLineStyle(new HTuple(10, 5));
                            AreaSelectionWindow.DispRectangle1(r1, c1, r2, c2);
                        }
                        catch (HalconException) { /* 忽略绘制错误 */ }
                    }
                    break;
            }
        }
        public void HandleMouseUp(HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            switch (_currentState)
            {
                case InteractionState.AreaSelecting:
                    // --- 完成区域选择 ---
                    double r1 = Math.Min(_mouseStartRow, e.Row);
                    double c1 = Math.Min(_mouseStartCol, e.Column);
                    double r2 = Math.Max(_mouseStartRow, e.Row);
                    double c2 = Math.Max(_mouseStartCol, e.Column);

                    if (Math.Abs(r2 - r1) > 5 || Math.Abs(c2 - c1) > 5) // 检查是否是拖动
                    {
                        HRegion selectionRegion;
                        try
                        {
                            selectionRegion = new HRegion(r1, c1, r2, c2);
                            if (Graphics != null)
                            {
                                foreach (var shape in Graphics.SelectMany(r => r.Shapes))
                                {
                                    using (HRegion roiRegion = shape.GetRegion())
                                    using (HRegion intersection = selectionRegion.Intersection(roiRegion))
                                    {
                                        if (intersection.Area > 0 && !_selectedObjects.Contains(shape))
                                        { _selectedObjects.Add(shape); }
                                    }
                                }
                            }
                            selectionRegion.Dispose();
                        }
                        catch (HalconException ex) { Debug.WriteLine($"区域选择失败: {ex.Message}"); }
                    }
                    RequestRepaint?.Invoke();
                    break;

                case InteractionState.CreatingShape:
                    // --- 完成形状创建 ---
                    if (_activeShape != null)
                    {
                        _activeShape.Color = DESELECTED_COLOR; // 设置最终颜色
                        _activeShape.IsSelected = true; // 自动选中
                        _selectedObjects.Clear();
                        _selectedObjects.Add(_activeShape);
                    }
                    ActiveTool = ActiveToolMode.Selection;
                    RequestRepaint?.Invoke();
                    break;

                case InteractionState.DraggingBody:
                case InteractionState.DraggingHandle:
                    // --- 完成拖动/调整大小 ---
                    RequestRepaint?.Invoke();
                    break;
            }

            // 重置所有状态
            _currentState = InteractionState.None;
            _activeShape = null;
            _activeHandle = HitTestHandle.None;
        }
        public void HandleDoubleClick(double row, double col)
        {
            if (Graphics == null || ActiveTool != ActiveToolMode.Selection) return;

            // 1. 找到该点上的所有形状
            var allHits = new List<IShape>();
            foreach (var shape in Graphics.SelectMany(r => r.Shapes))
            {
                if (shape.HitTest(row, col).HasHit)
                {
                    allHits.Add(shape);
                }
            }

            if (allHits.Count == 0) return;
            if (allHits.Count == 1)
            {
                _selectedObjects.Clear();
                _selectedObjects.Add(allHits[0]);
                return;
            }

            // 2. 循环选择逻辑
            var currentSelection = _selectedObjects.FirstOrDefault(s => allHits.Contains(s));
            int nextIndex = 0;
            if (currentSelection != null)
            {
                int currentIndex = allHits.IndexOf(currentSelection);
                nextIndex = (currentIndex + 1) % allHits.Count;
            }

            var shapeToSelect = allHits[nextIndex];
            _selectedObjects.Clear();
            _selectedObjects.Add(shapeToSelect);

            // (ZOrder 逻辑 暂时简化为仅选择)
        }
        public void HandleKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (DeleteSelectedCommand.CanExecute(null))
                {
                    DeleteSelectedCommand.Execute(null);
                }
            }
        }
        #endregion

        #region 样式辅助
        private ShapeHitTestResult FindHitShape(double row, double col)
        {
            if (Graphics == null) return ShapeHitTestResult.NoHit;

            // 迭代所有 ROI 和 Shapes
            var allShapes = Graphics.SelectMany(r => r.Shapes);

            // 优先检查已选中的（因为可能在拖拽控制柄） 
            foreach (var shape in _selectedObjects.Reverse())
            {
                var result = shape.HitTest(row, col);
                if (result.HasHit) return result;
            }

            // 命中未选中的对象
            foreach (var shape in allShapes.Reverse())
            {
                if (_selectedObjects.Contains(shape)) continue;
                var result = shape.HitTest(row, col);
                if (result.HasHit) return result;
            }
            return ShapeHitTestResult.NoHit;
        }

        private void UpdateAllObjectStyles()
        {
            if (Graphics == null) return;
            foreach (var shape in Graphics.SelectMany(r => r.Shapes))
            {
                bool isSelected = _selectedObjects.Contains(shape);
                shape.IsSelected = isSelected;
                shape.Color = isSelected ? SELECTED_COLOR : DESELECTED_COLOR;
                shape.IsInteractive = isSelected;
            }
            RequestRepaint?.Invoke(); // 强制重绘以显示样式
        }
        #endregion
    }
}