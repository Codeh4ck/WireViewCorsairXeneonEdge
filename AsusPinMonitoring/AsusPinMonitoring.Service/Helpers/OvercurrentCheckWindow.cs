using AsusPinMonitoring.Contracts.Common;
using AsusPinMonitoring.Service.Interfaces;

namespace AsusPinMonitoring.Service.Helpers;

internal sealed class OvercurrentCheckWindow(IConfigStore configStore) : IOvercurrentCheckWindow
{
    private volatile IReadOnlyDictionary<string, float> _pinAmperages = new Dictionary<string, float>();

    private DateTimeOffset? _overcurrentObservedAt;

    private volatile OvercurrentStatus _overcurrentStatus = OvercurrentStatus.Normal;
    private volatile WireOvercurrent? _wireOvercurrent;

    public async Task StartCheckWindowAsync(CancellationToken cancellationToken = default)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(configStore.Configuration.PollIntervalMs));

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                Evaluate(DateTimeOffset.UtcNow);

                timer.Period = TimeSpan.FromMilliseconds(configStore.Configuration.PollIntervalMs);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void Evaluate(DateTimeOffset now)
    {
        var config = configStore.Configuration;

        var pinAmperages = _pinAmperages;

        var offendingPins = pinAmperages
            .Where(pin => pin.Value >= config.WireOvercurrentAmperageThreshold)
            .ToDictionary(pin => pin.Key, pin => pin.Value);

        if (offendingPins.Count == 0)
        {
            Reset();
            return;
        }

        _overcurrentObservedAt ??= now;

        if (now - _overcurrentObservedAt.Value >= TimeSpan.FromMilliseconds(config.WireOvercurrentThresholdMs))
        {
            _wireOvercurrent = new WireOvercurrent { PinValues = offendingPins };
            _overcurrentStatus = OvercurrentStatus.OvercurrentDetected;
        }
    }

    private void Reset()
    {
        _overcurrentObservedAt = null;
        _overcurrentStatus = OvercurrentStatus.Normal;
        _wireOvercurrent = null;
    }

    public void SetPinAmperages(IReadOnlyDictionary<string, float> pinAmperages) => _pinAmperages = pinAmperages;

    public WireOvercurrent? GetWireOvercurrent() =>
        _overcurrentStatus == OvercurrentStatus.Normal ? null : _wireOvercurrent;
}