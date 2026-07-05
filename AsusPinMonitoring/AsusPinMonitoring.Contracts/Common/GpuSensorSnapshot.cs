namespace AsusPinMonitoring.Contracts.Common;

public sealed class GpuSensorSnapshot
{
    public DateTimeOffset Timestamp { get; init; }
    public List<GpuSensorReading> Sensors { get; init; } = [];
    public CurrentImbalance? CurrentImbalance { get; init; }
    public WireOvercurrent? WireOvercurrent { get; init; }
}