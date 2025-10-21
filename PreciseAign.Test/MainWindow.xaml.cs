using HalconDotNet;
using System.Windows;
using System.IO;
using System;
using PreciseAlign.Test.ViewModels;

namespace PreciseAlign.Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HObject _currentImage;
        private HObject _currentGraphics;
        private int _imageIndex = 0;

        public HObject CurrentImage
        {
            get => _currentImage;
            set
            {
                _currentImage?.Dispose();
                _currentImage = value;
            }
        }

        public HObject CurrentGraphics
        {
            get => _currentGraphics;
            set
            {
                _currentGraphics?.Dispose();
                _currentGraphics = value;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            LoadInitialImage();
        }

        private void LoadInitialImage()
        {
            // 方法1：创建测试图像
            CreateTestImage();

            // 方法2：从文件加载图像（取消注释使用）
            // LoadImageFromFile("test1.jpg");
        }

        private void CreateTestImage()
        {
            try
            {
                // 创建不同模式的测试图像
                switch (_imageIndex % 3)
                {
                    case 0:
                        // 创建渐变图像
                        HOperatorSet.GenImageConst(out HObject image1, "byte", 512, 512);
                        CurrentImage = image1;
                        break;
                    case 1:
                        // 创建噪声图像
                        HOperatorSet.GenImageConst(out HObject image2, "byte", 512, 512);
                        HOperatorSet.AddNoiseWhite(image2, out HObject noisyImage, 40);
                        CurrentImage = noisyImage;
                        break;
                    case 2:
                        // 创建正弦波图像
                        HOperatorSet.GenImage1Rect(out HObject sinImage, "byte", 128, 50,1,8,8,"false",0);
                        CurrentImage = sinImage;
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建测试图像失败: {ex.Message}");
            }
        }

        private void LoadImageFromFile(string filename)
        {
            try
            {
                if (File.Exists(filename))
                {
                    HOperatorSet.ReadImage(out HObject image, filename);
                    CurrentImage = image;
                }
                else
                {
                    MessageBox.Show($"图像文件不存在: {filename}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载图像失败: {ex.Message}");
            }
        }

        private void ChangeImage_Click(object sender, RoutedEventArgs e)
        {
            _imageIndex++;
            CreateTestImage();
        }

        private void AddRectangle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 创建矩形区域
                HOperatorSet.GenRectangle1(out HObject rectangle, 100, 100, 300, 400);

                if (CurrentGraphics != null && CurrentGraphics.IsInitialized())
                {
                    // 如果已有图形，合并新的矩形
                    HOperatorSet.ConcatObj(CurrentGraphics, rectangle, out HObject newGraphics);
                    CurrentGraphics = newGraphics;
                    rectangle.Dispose();
                }
                else
                {
                    // 如果没有图形，直接设置
                    CurrentGraphics = rectangle;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建矩形失败: {ex.Message}");
            }
        }

        private void ClearGraphics_Click(object sender, RoutedEventArgs e)
        {
            CurrentGraphics?.Dispose();
            CurrentGraphics = null;
        }

        private void SwitchImage_Click(object sender, RoutedEventArgs e)
        {
            // 切换到真实图像文件测试
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "图像文件|*.jpg;*.png;*.bmp;*.tiff|所有文件|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadImageFromFile(openFileDialog.FileName);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // 清理资源
            CurrentImage?.Dispose();
            CurrentGraphics?.Dispose();
            base.OnClosed(e);
        }
    }
}