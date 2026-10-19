namespace AsusWireviewPinMonitoring.Contracts.Output;

public class WireViewConfigResponse
{
    public Dictionary<string, string> Configs { get; init; } = new();
}