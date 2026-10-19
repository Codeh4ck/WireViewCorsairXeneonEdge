namespace AsusWireviewPinMonitoring.Contracts.Common;

public sealed class GpuSensorSnapshot
{
    public DateTime Timestamp { get; init; }
    public List<GpuSensorReading> Sensors { get; init; } = [];
    public required WireViewData WireViewData { get; init; }
}