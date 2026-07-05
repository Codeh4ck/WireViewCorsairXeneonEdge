using AsusPinMonitoring.Contracts.Common;

namespace AsusPinMonitoring.Service.Interfaces;

public interface IImbalanceCheckWindow
{
    Task StartCheckWindowAsync(CancellationToken cancellationToken = default);
    void SetMaxAmperage(float maxAmperage, string maxPinName);
    void SetMinAmperage(float minAmperage, string minPinName);
    CurrentImbalance? GetCurrentImbalance();
}