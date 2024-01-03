using System.Collections.Generic;
using Amuse.UI.Models;

namespace Amuse.UI.Services
{
    public interface IDeviceService
    {
        IReadOnlyList<DeviceInfo> Devices { get; }
    }
}