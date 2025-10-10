using System.Collections.Generic;

namespace PreciseAlign.Core.Interfaces
{
    public interface IProcessConfigService
    {
        Dictionary<string, string[]> GetProcessStepCameraMapping();
    }
}