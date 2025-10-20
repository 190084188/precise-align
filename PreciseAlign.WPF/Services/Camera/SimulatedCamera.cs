using PreciseAlign.Core.Interfaces;
using PreciseAlign.Core.Models;

namespace PreciseAlign.WPF.Services.Camera
{
    // 一个简单的模拟相机，用于在没有硬件时进行测试
    public class SimulatedCamera : ICamera
    {
        public string CameraId { get; }
        public bool IsConnected { get; private set; }
        public double Exposure { get; set; }
        public double Gain { get; set; }
        public event EventHandler<ImageReadyEventArgs>? ImageReady;

        public SimulatedCamera(string cameraId) { CameraId = cameraId; }
        public void HorzFlip(bool flipped)
        {
        }
        public void VertFlip(bool flipped)
        {
        }
        public void Connect() { IsConnected = true; Console.WriteLine($"模拟相机 '{CameraId}' 已连接。"); }
        public void Disconnect() { IsConnected = false; }
        public void GrabOneAsync() { Console.WriteLine($"模拟相机 '{CameraId}' 触发了一次拍照。"); }
        public void SetFlip(bool h, bool v) { }
        public void SetTriggerMode(bool isTrigger) { }
        public void ShowControlPanel() { }
        public void Dispose() { Disconnect(); }
    }
}