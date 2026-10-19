using AsusWireviewPinMonitoring.Contracts.Common;
using AsusWireviewPinMonitoring.Service.Interfaces;

namespace AsusWireviewPinMonitoring.Service.Helpers;

internal sealed class SensorReadingStore : ISensorReadingStore
{
    private volatile GpuSensorSnapshot? _latest;
    public GpuSensorSnapshot? Latest => _latest;
    
    public void Update(GpuSensorSnapshot reading) => _latest = reading;
    public void Clear() => _latest = null;
}