using AsusPinMonitoring.Contracts.Common;

namespace AsusPinMonitoring.Service.Interfaces;

internal interface ISensorReadingStore
{
    GpuSensorSnapshot? Latest { get; }
    
    void Update(GpuSensorSnapshot reading);
    void Clear();
}