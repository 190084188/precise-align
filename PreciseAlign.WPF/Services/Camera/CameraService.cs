using PreciseAlign.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace PreciseAlign.WPF.Services.Camera
{
    public class CameraService : ICameraService
    {
        private readonly Dictionary<string, ICamera> _cameras;

        public CameraService(IEnumerable<ICamera> cameras)
        {
            _cameras = cameras.ToDictionary(cam => cam.CameraId);
        }

        public ICamera? GetCamera(string cameraId)
        {
            _cameras.TryGetValue(cameraId, out var camera);
            return camera;
        }

        public IEnumerable<ICamera> AllCameras => _cameras.Values;
    }
}