using HalconDotNet;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace PreciseAlign.Test.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private HObject _image;
        private HObject _graphics;

        // 绑定到 HImageWindow.Image 的属性
        public HObject Image
        {
            get { return _image; }
            set { _image = value; OnPropertyChanged(); }
        }

        // 绑定到 HImageWindow.Graphics 的属性
        public HObject Graphics
        {
            get { return _graphics; }
            set { _graphics = value; OnPropertyChanged(); }
        }

        // --- 命令 ---
        public ICommand GenerateRoiCommand { get; }
        public ICommand LoadImage1Command { get; }
        public ICommand LoadImage2Command { get; }
        public ICommand ClearAllCommand { get; }


        public MainViewModel()
        {
            // 初始化命令
            GenerateRoiCommand = new RelayCommand(GenerateRoi);
            LoadImage1Command = new RelayCommand(LoadImage1);
            LoadImage2Command = new RelayCommand(LoadImage2);
            ClearAllCommand = new RelayCommand(ClearAll);
        }

        private void GenerateRoi()
        {
            // 生成一个矩形作为ROI
            HOperatorSet.GenRectangle1(out HObject rect, 100, 100, 300, 400);
            Graphics = rect;
        }

        private void LoadImage1()
        {
            Console.WriteLine("111");
            // 生成一个512x512的图像
            HOperatorSet.GenImageConst(out HObject image1, "byte", 512, 512);

            // 1. 获取图像的整个区域
            HOperatorSet.GetDomain(image1, out HObject imageDomain);

            // 2. 使用 Intensity 算子直接获取均值和标准差
            HOperatorSet.Intensity(imageDomain, image1, out HTuple meanValue, out HTuple deviation);

            // 下面的 ScaleImage 逻辑可以保留，用于视觉上区分
            HOperatorSet.ScaleImage(image1, out HObject scaledImage, 2, 100);
            Image = scaledImage;

            // 清理临时对象
            imageDomain.Dispose();
        }

        private void LoadImage2()
        {
            // 生成一个灰度值为200的600x800图像
            HOperatorSet.GenImageConst(out HObject image2, "byte", 600, 800);
            HOperatorSet.ScaleImage(image2, out HObject scaledImage, 1, 200);

            Image = scaledImage;
        }

        private void ClearAll()
        {
            // 清理资源
            Image?.Dispose();
            Graphics?.Dispose();
            Image = null;
            Graphics = null;
        }


        // --- INotifyPropertyChanged 实现 ---
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public event EventHandler? CanExecuteChanged;
        public RelayCommand(Action execute) { _execute = execute; }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
    }

    
}
