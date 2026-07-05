using AsusPinMonitoring.Contracts.Common;
using AsusPinMonitoring.Service.Interfaces;

namespace AsusPinMonitoring.Service.Helpers;

internal sealed class SensorReadingStore : ISensorReadingStore
{
    private volatile GpuSensorSnapshot? _latest;
    public GpuSensorSnapshot? Latest => _latest;
    
    public void Update(GpuSensorSnapshot reading) => _latest = reading;
    public void Clear() => _latest = null;
}