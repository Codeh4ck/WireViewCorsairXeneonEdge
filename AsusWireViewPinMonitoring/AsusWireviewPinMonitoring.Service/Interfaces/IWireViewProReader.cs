using AsusWireviewPinMonitoring.Contracts.Common;
using AsusWireviewPinMonitoring.Service.Wireview;

namespace AsusWireviewPinMonitoring.Service.Interfaces;

public interface IWireViewProReader
{
    int PollIntervalMs { get; set; }
    bool IsRunning { get; }
    string PortName { get; }
    event EventHandler<WireViewSensorData>? DataUpdated;
    event EventHandler<bool>? ConnectionChanged;
    void Start(int pollIntervalMs);
    void Dispose();
    void ClearFaults(ushort faultStatusMask = 0xFFFF, ushort faultLogMask = 0xFFFF);
    WireViewConfig? ReadConfig();
    byte[]? ReadConfigRaw();
    Task YieldToWireViewAsync(int yieldMs, CancellationToken token);
}