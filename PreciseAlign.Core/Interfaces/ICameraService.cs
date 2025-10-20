namespace PreciseAlign.Core.Interfaces
{
    public interface ICameraService
    {
        ICamera? GetCamera(string cameraId);
        IEnumerable<ICamera> AllCameras { get; }
    }
}