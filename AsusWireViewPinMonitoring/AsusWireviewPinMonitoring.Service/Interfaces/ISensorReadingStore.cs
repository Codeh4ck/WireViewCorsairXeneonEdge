using AsusWireviewPinMonitoring.Contracts.Common;

namespace AsusWireviewPinMonitoring.Service.Interfaces;

internal interface ISensorReadingStore
{
    GpuSensorSnapshot? Latest { get; }
    
    void Update(GpuSensorSnapshot reading);
    void Clear();
}