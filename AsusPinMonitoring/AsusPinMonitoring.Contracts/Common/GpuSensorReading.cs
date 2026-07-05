namespace AsusPinMonitoring.Contracts.Common;

public sealed class GpuSensorReading
{
    public string HardwareName { get; init; } = string.Empty;
    public string SensorName { get; init; } = string.Empty;
    public string SensorType { get; init; } = string.Empty;
    public float Value { get; init; }
}