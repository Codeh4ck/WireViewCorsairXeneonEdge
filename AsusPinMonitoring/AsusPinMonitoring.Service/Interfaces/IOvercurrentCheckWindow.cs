using AsusPinMonitoring.Contracts.Common;

namespace AsusPinMonitoring.Service.Interfaces;

public interface IOvercurrentCheckWindow
{
    Task StartCheckWindowAsync(CancellationToken cancellationToken = default);
    void SetPinAmperages(IReadOnlyDictionary<string, float> pinAmperages);
    WireOvercurrent? GetWireOvercurrent();
}