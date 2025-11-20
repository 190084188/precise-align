// Models/ROI.cs
using HalconDotNet;
using PreciseAlign.Core.Mvvm;
using System.Collections.ObjectModel;

namespace PreciseAlign.Core.Models
{
    // (我们稍后将添加 JSON 序列化功能)

    public class ROI : ObservableObject
    {
        private string _name = "ROI";
        private int _zOrder;

        public string Name { get => _name; set => SetProperty(ref _name, value); }
        public int ZOrder { get => _zOrder; set => SetProperty(ref _zOrder, value); }

        // 1. 管理的形状的维护列表
        public ObservableCollection<IShape> Shapes { get; } = new ObservableCollection<IShape>();

        // 3. 形状之间可以求交并、异或集
        public HRegion GetRegion(string operation = "union")
        {
            if (Shapes.Count == 0)
            {
                return new HRegion(); // 空区域
            }

            HRegion combinedRegion = Shapes[0].GetRegion();

            foreach (var shape in Shapes.Skip(1))
            {
                using (HRegion nextRegion = shape.GetRegion())
                {
                    switch (operation.ToLower())
                    {
                        case "intersection":
                            combinedRegion = combinedRegion.Intersection(nextRegion);
                            break;
                        case "difference":
                            combinedRegion = combinedRegion.Difference(nextRegion);
                            break;
                        // 默认是 "union"
                        default:
                            combinedRegion = combinedRegion.Union2(nextRegion);
                            break;
                    }
                }
            }
            return combinedRegion;
        }
    }
}