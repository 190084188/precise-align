// HImageWindowViewModel.cs (已完全修复)

using HalconDotNet;
using Microsoft.Win32;
using PreciseAlign.Controls.Mvvm;
using PreciseAlign.Controls.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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

    public class HImageWindowViewModel : INotifyPropertyChanged
    {
        #region 属性设置
        private const string SELECTED_COLOR = "cyan";
        private const string DESELECTED_COLOR = "red";

        private readonly HImageWindow _view;
        private double _mouseStartRow, _mouseStartCol;

        // --- 唯一的交互状态 ---
        private enum InteractionState { None, CreatingShape, DraggingHandle, DraggingBody, AreaSelecting }
        private InteractionState _currentState = InteractionState.None;

        // --- 交互期间的活动对象 ---
        private IShape? _activeShape;
        private HitTestHandle _activeHandle;

        private ActiveToolMode _activeTool;
        public ActiveToolMode ActiveTool
        {
            get => _activeTool;
            set
            {
                if (_activeTool != value)
                {
                    _activeTool = value;
                    OnPropertyChanged();
                    UpdateOperationMode();
                }
            }
        }
        #endregion

        #region 命令
        public ICommand LoadImageCommand { get; }
        public ICommand FitImageCommand { get; }
        public ICommand ClearGraphicsCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        #endregion

        private readonly ObservableCollection<IShape> _selectedObjects = [];

        public HImageWindowViewModel(HImageWindow view)
        {
            _view = view;

            LoadImageCommand = new RelayCommand(LoadImage);
            FitImageCommand = new RelayCommand(_view.SetPartToFitImage);
            ClearGraphicsCommand = new RelayCommand(ClearGraphics);
            DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => _selectedObjects.Any());

            _selectedObjects.CollectionChanged += (s, e) =>
            {
                UpdateAllObjectStyles();
                (DeleteSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
            };

            ActiveTool = ActiveToolMode.Selection;
        }

        private void UpdateOperationMode()
        {
            if (ActiveTool == ActiveToolMode.Moving)
            {
                _view.HalconWindow.HMoveContent = true;
            }
            else
            {
                _view.HalconWindow.HMoveContent = false;
            }
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
                    _view.Image = ho_Image;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"加载图像文件失败：{ex}");
                }
            }
        }

        private void ClearGraphics()
        {
            if (_view.Graphics != null)
            {
                _view.Graphics.Clear();
            }
            _selectedObjects.Clear();
        }

        private void DeleteSelected()
        {
            if (_view.Graphics == null) return;
            foreach (var roi in _view.Graphics)
            {
                var shapesToRemove = roi.Shapes.Where(s => _selectedObjects.Contains(s)).ToList();
                foreach (var shape in shapesToRemove)
                {
                    roi.Shapes.Remove(shape);
                }
            }
            _selectedObjects.Clear();
        }
        #endregion

        #region 事件处理程序 (完全重构)

        public void HandleMouseDown(HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            _view.Focus();
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
                    var defaultRoi = _view.Graphics.FirstOrDefault(r => r.Name == "Default");
                    if (defaultRoi == null)
                    {
                        defaultRoi = new ROI { Name = "Default" };
                        _view.Graphics.Add(defaultRoi);
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
            System.Threading.Thread.Sleep(5);
            switch (_currentState)
            {
                case InteractionState.DraggingBody:
                    double rowOffset = e.Row - _mouseStartRow;
                    double colOffset = e.Column - _mouseStartCol;
                    _activeShape?.Move(rowOffset, colOffset);
                    _mouseStartRow = e.Row;
                    _mouseStartCol = e.Column;
                    _view.RedrawSynchronous();
                    break;

                case InteractionState.DraggingHandle:
                    _activeShape?.DragHandle(_activeHandle, e.Row, e.Column);
                    _view.RedrawSynchronous();
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
                        _view.RedrawSynchronous();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"更新 Shape 失败: {ex.Message}");
                    }
                    break;

                case InteractionState.AreaSelecting:
                    // --- 绘制选择框 (不变) ---
                    var hWindow = _view.HalconWindow.HalconWindow;
                    _view.RedrawSynchronous();
                    hWindow.SetColor("green");
                    hWindow.SetDraw("margin");
                    hWindow.SetLineStyle(new HTuple(10, 5));
                    hWindow.DispRectangle1(_mouseStartRow, _mouseStartCol, e.Row, e.Column);
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
                            if (_view.Graphics != null)
                            {
                                foreach (var shape in _view.Graphics.SelectMany(r => r.Shapes))
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
                    _view.FullRedraw();
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
                    _view.RedrawSynchronous();
                    break;

                case InteractionState.DraggingBody:
                case InteractionState.DraggingHandle:
                    // --- 完成拖动/调整大小 ---
                    _view.RedrawSynchronous();
                    break;
            }

            // 重置所有状态
            _currentState = InteractionState.None;
            _activeShape = null;
            _activeHandle = HitTestHandle.None;
        }
        public void HandleDoubleClick(double row, double col)
        {
            if (_view.Graphics == null || ActiveTool != ActiveToolMode.Selection) return;

            // 1. 找到该点上的 *所有* 形状
            var allHits = new List<IShape>();
            foreach (var shape in _view.Graphics.SelectMany(r => r.Shapes))
            {
                if (shape.HitTest(row, col).HasHit)
                {
                    allHits.Add(shape);
                }
            }

            if (allHits.Count <= 1) return;

            // 2. 找到当前选中的
            var currentSelection = _selectedObjects.FirstOrDefault(s => allHits.Contains(s));
            int nextIndex = 0;

            if (currentSelection != null)
            {
                int currentIndex = allHits.IndexOf(currentSelection);
                nextIndex = (currentIndex + 1) % allHits.Count;
            }

            var shapeToSelect = allHits[nextIndex];

            // 3. 循环选择
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
            if (_view.Graphics == null) return ShapeHitTestResult.NoHit;

            // 迭代所有 ROI 和 Shapes
            var allShapes = _view.Graphics.SelectMany(r => r.Shapes);

            // 优先命中选中的对象
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
            if (_view.Graphics == null) return;
            foreach (var shape in _view.Graphics.SelectMany(r => r.Shapes))
            {
                bool isSelected = _selectedObjects.Contains(shape);
                shape.IsSelected = isSelected;
                shape.Color = isSelected ? SELECTED_COLOR : DESELECTED_COLOR;
                shape.IsInteractive = isSelected;
            }
            _view.RedrawSynchronous(); // 强制重绘以显示样式
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}