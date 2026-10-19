namespace AsusWireviewPinMonitoring.Contracts.Common;

public sealed class WireViewData
{
    public double OnboardTempIn { get; init; }
    public double OnboardTempOut { get; init; }
    public double? ExternalTemp1 { get; init; }
    public double? ExternalTemp2 { get; init; }

    public int PsuCapabilityWatts { get; init; }

    public double TotalCurrent { get; init; }
    public double TotalVoltage { get; init; }
    public double TotalPower { get; init; }

    public ushort FaultStatus { get; init; }
    public ushort FaultLog { get; init; }
}