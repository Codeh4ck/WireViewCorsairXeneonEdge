using WireViewMonitor.Contracts.Output;

namespace WireViewMonitor.Service.Stores;

internal sealed class SensorStore
{
    private volatile SensorResponse? _latest;
    public SensorResponse? Latest => _latest;
    
    public void Update(SensorResponse r) => _latest = r;
    public void Clear() => _latest = null;
}