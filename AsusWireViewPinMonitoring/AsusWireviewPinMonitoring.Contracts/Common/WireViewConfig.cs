namespace AsusWireviewPinMonitoring.Contracts.Common;

public class WireViewConfig
{
    public int Version { get; init; }
    public byte BacklightDuty { get; init; }
    public byte LoggingInterval { get; init; }
    public string? Averaging { get; init; }
    public string FanMode { get; init; } = string.Empty;
    public string FanTempSource { get; init; } = string.Empty;
    public byte FanDutyMin { get; init; }
    public byte FanDutyMax { get; init; }
    public double FanTempMinC { get; init; }
    public double FanTempMaxC { get; init; }
    public double TsFaultThresholdC { get; init; }
    public byte OcpFaultThresholdA { get; init; }
    public double WireOcpFaultThresholdA { get; init; }
    public ushort OppFaultThresholdW { get; init; }
    public byte CurrentImbalanceThreshold { get; init; }
    public byte CurrentImbalanceMinLoad { get; init; }
    public byte ShutdownWaitTimeS { get; init; }
    public ushort FaultDisplayEnable { get; init; }
    public ushort FaultBuzzerEnable { get; init; }
    public ushort FaultSoftPowerEnable { get; init; }
    public ushort FaultHardPowerEnable { get; init; }
}