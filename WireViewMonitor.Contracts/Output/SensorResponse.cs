using WireViewMonitor.Contracts.Common;

namespace WireViewMonitor.Contracts.Output;

public class SensorResponse
{
    public PinReading[] Pins { get; init; } = [];
    public double TempInC { get; init; }
    public double TempOutC { get; init; }
    public double ExtTemp1C { get; init; } 
    public double ExtTemp2C { get; init; } 
    public double TotalCurrentA { get; init; }
    public double TotalPowerW { get; init; } 
    public int PsuCapabilityW { get; init; } 
    public ushort FaultStatus { get; init; } 
    public ushort FaultLog { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}