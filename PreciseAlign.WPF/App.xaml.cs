using Microsoft.Extensions.DependencyInjection; // 引入DI相关的命名空间
using PreciseAlign.Core.Interfaces;
using PreciseAlign.WPF.Services.Camera; // 引入具体服务实现的命名空间
using PreciseAlign.WPF.Services.Communication;
using PreciseAlign.WPF.Services.Vision;
using PreciseAlign.WPF.ViewModels; // 引入ViewModel的命名空间
using PreciseAlign.WPF.Views;      // 引入View的命名空间
using System;
using System.Windows;

namespace PreciseAlign.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
    }

}
