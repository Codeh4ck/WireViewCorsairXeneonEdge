using AsusPinMonitoring.Contracts.Common;
using AsusPinMonitoring.Service.Interfaces;

namespace AsusPinMonitoring.Service.Helpers;

internal sealed class ImbalanceCheckWindow(IConfigStore configStore) : IImbalanceCheckWindow
{
    private float _maxAmperage;
    private float _minAmperage;
    private string _maxPinName = string.Empty;
    private string _minPinName = string.Empty;

    private bool _checkActive;

    private DateTimeOffset? _imbalanceObservedAt;

    private volatile ImbalanceStatus _imbalanceStatus = ImbalanceStatus.Normal;
    private volatile CurrentImbalance? _currentImbalance;

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

        var max = _maxAmperage;
        var min = _minAmperage;
        var maxPinName = _maxPinName;
        var minPinName = _minPinName;

        if (!_checkActive)
        {
            if (max < config.ImbalanceAmperageThreshold)
            {
                return;
            }

            _checkActive = true;
            _imbalanceObservedAt = null;
        }
        else if (max < config.ImbalanceAmperageThreshold)
        {
            Reset();
            return;
        }

        var average = (max + min) / 2f;
        var percentDifference = average > 0f
            ? Math.Abs(max - min) / average * 100d
            : 0d;

        if (percentDifference >= config.ImbalancePercentageThreshold)
        {
            _imbalanceObservedAt ??= now;

            if (now - _imbalanceObservedAt.Value >= TimeSpan.FromMilliseconds(config.ImbalanceDurationThresholdMs))
            {
                _currentImbalance = new CurrentImbalance
                {
                    MaxPinAmperage = max,
                    MaxPinName = maxPinName,
                    MinPinAmperage = min,
                    MinPinName = minPinName,
                    PercentageDifference = (int)Math.Round(percentDifference),
                    Timestamp = now,
                };
                _imbalanceStatus = ImbalanceStatus.ImbalanceDetected;
            }
        }
        else
        {
            _imbalanceObservedAt = null;
        }
    }

    private void Reset()
    {
        _checkActive = false;
        _imbalanceObservedAt = null;
        _imbalanceStatus = ImbalanceStatus.Normal;
        _currentImbalance = null;
    }

    public void SetMaxAmperage(float maxAmperage, string maxPinName)
    {
        _maxAmperage = maxAmperage;
        _maxPinName = maxPinName;
    }

    public void SetMinAmperage(float minAmperage, string minPinName)
    {
        _minAmperage = minAmperage;
        _minPinName = minPinName;
    }

    public CurrentImbalance? GetCurrentImbalance() =>
        _imbalanceStatus == ImbalanceStatus.Normal ? null : _currentImbalance;
}