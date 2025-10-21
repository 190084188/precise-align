using HalconDotNet;
using System.Windows;
using System.Windows.Controls;

namespace PreciseAlign.Controls
{
    public partial class HImageWindow : UserControl
    {
        // --- 定义用于数据绑定的依赖属性 ---
        // 主图像的依赖属性
        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register(
                "Image",
                typeof(HObject),
                typeof(HImageWindow),
                new PropertyMetadata(null, OnImageChanged));

        // 叠加图形的依赖属性
        public static readonly DependencyProperty GraphicsProperty =
            DependencyProperty.Register(
                "Graphics",
                typeof(HObject),
                typeof(HImageWindow),
                new PropertyMetadata(null, OnGraphicsChanged));

        // --- 包装依赖属性，方便在代码中使用 ---
        public HObject Image
        {
            get { return (HObject)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        public HObject Graphics
        {
            get { return (HObject)GetValue(GraphicsProperty); }
            set { SetValue(GraphicsProperty, value); }
        }

        public HImageWindow()
        {
            InitializeComponent();
        }

        // --- 属性变化时的回调函数 ---
        // 当 Image 属性变化时（即ViewModel给它赋了新值），此方法被调用
        private static void OnImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as HImageWindow;
            control?.Render(); // 重新渲染
        }

        // 当 Graphics 属性变化时，此方法被调用
        private static void OnGraphicsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as HImageWindow;
            control?.Render(); // 重新渲染
        }

        // --- 核心显示逻辑 ---
        private void Render()
        {
            // 在UI线程上执行Halcon操作
            Dispatcher.Invoke(() =>
            {
                var hWindow = HalconWindow.HalconWindow;

                // 如果主图像为空，则清空窗口
                if (Image == null || !Image.IsInitialized())
                {
                    HOperatorSet.ClearWindow(hWindow);
                    return;
                }

                // 获取图像尺寸，并设置显示区域
                if (Image is HImage)
                {
                    // 2. 将 HObject 强制转换为 HImage，然后调用 GetImageSize
                    ((HImage)Image).GetImageSize(out HTuple width, out HTuple height);
                    HOperatorSet.SetPart(hWindow, 0, 0, height - 1, width - 1);
                }

                // a. 显示主图像
                HOperatorSet.DispObj(Image, hWindow);

                // b. 叠加显示图形
                if (Graphics != null && Graphics.IsInitialized())
                {
                    // 在这里可以设置图形的显示属性，例如颜色、线宽等
                    HOperatorSet.SetColor(hWindow, "green");
                    HOperatorSet.SetLineWidth(hWindow, 2);
                    HOperatorSet.DispObj(Graphics, hWindow);
                }
            });
        }
    }
}