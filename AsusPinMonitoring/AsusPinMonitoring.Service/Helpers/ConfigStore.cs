using Microsoft.Win32;
using System.Globalization;
using AsusPinMonitoring.Contracts.Common;
using AsusPinMonitoring.Service.Interfaces;

namespace AsusPinMonitoring.Service.Helpers;

internal sealed class ConfigStore : IConfigStore
{
    private const string RegistryKey = @"SOFTWARE\AsusPinMonitoringService";

    private volatile Configuration _configuration;
    private readonly ILogger<ConfigStore> _logger;

    public ConfigStore(ILogger<ConfigStore> logger)
    {
        _logger = logger;
        _configuration = Load();
    }
    
    public Configuration Configuration
    {
        get => _configuration;
        set
        {
            _configuration = value;
            Save(_configuration);
        }
    }
    
    public Configuration Load()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(RegistryKey);

            var pollIntervalMs = Configuration.Default.PollIntervalMs;
            if (key?.GetValue(nameof(Configuration.Default.PollIntervalMs)) is int value)
            {
                pollIntervalMs = Math.Clamp(value, 100, 30_000);
            }

            var imbalanceAmperageThreshold = Configuration.Default.ImbalanceAmperageThreshold;
            if (key?.GetValue(nameof(Configuration.Default.ImbalanceAmperageThreshold)) is int imbalanceAmperageValue)
            {
                imbalanceAmperageThreshold = Math.Clamp(imbalanceAmperageValue, 1, 10);
            }
            
            var imbalancePercentageThreshold = Configuration.Default.ImbalancePercentageThreshold;
            if (key?.GetValue(nameof(Configuration.Default.ImbalancePercentageThreshold)) is int imbalancePercentageValue)
            {
                imbalancePercentageThreshold = Math.Clamp(imbalancePercentageValue, 10, 50);
            }
            
            var maxImbalanceDurationMs = Math.Max(pollIntervalMs, 10_000);
            var imbalanceDurationThresholdMs = Math.Clamp(Configuration.Default.ImbalanceDurationThresholdMs, pollIntervalMs, maxImbalanceDurationMs);
            if (key?.GetValue(nameof(Configuration.Default.ImbalanceDurationThresholdMs)) is int imbalanceDurationThresholdValue)
            {
                imbalanceDurationThresholdMs = Math.Clamp(imbalanceDurationThresholdValue, pollIntervalMs, maxImbalanceDurationMs);
            }
            
            var wireOvercurrentAmperageThreshold = Configuration.Default.WireOvercurrentAmperageThreshold;
            if (key?.GetValue(nameof(Configuration.Default.WireOvercurrentAmperageThreshold)) is string wireOvercurrentAmperageValue
                && float.TryParse(wireOvercurrentAmperageValue, CultureInfo.InvariantCulture, out var parsedWireOvercurrentAmperage))
            {
                wireOvercurrentAmperageThreshold = Math.Clamp(parsedWireOvercurrentAmperage, 6f, 10.5f);
            }

            var maxWireOvercurrentDurationMs = Math.Max(pollIntervalMs, 10_000);
            var wireOvercurrentThresholdMs = Math.Clamp(Configuration.Default.WireOvercurrentThresholdMs, pollIntervalMs, maxWireOvercurrentDurationMs);
            if (key?.GetValue(nameof(Configuration.Default.WireOvercurrentThresholdMs)) is int wireOvercurrentThresholdValue)
            {
                wireOvercurrentThresholdMs = Math.Clamp(wireOvercurrentThresholdValue, pollIntervalMs, maxWireOvercurrentDurationMs);
            }

            return new Configuration
            {
                PollIntervalMs = pollIntervalMs,
                ImbalanceAmperageThreshold = imbalanceAmperageThreshold,
                ImbalancePercentageThreshold = imbalancePercentageThreshold,
                ImbalanceDurationThresholdMs = imbalanceDurationThresholdMs,
                WireOvercurrentAmperageThreshold = wireOvercurrentAmperageThreshold,
                WireOvercurrentThresholdMs = wireOvercurrentThresholdMs,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from registry");
        }

        return Configuration.Default;
    }

    public void Save(Configuration configuration)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(RegistryKey, writable: true);
            
            key.SetValue(nameof(Configuration.Default.PollIntervalMs), configuration.PollIntervalMs, RegistryValueKind.DWord);
            key.SetValue(nameof(Configuration.Default.ImbalanceAmperageThreshold), configuration.ImbalanceAmperageThreshold, RegistryValueKind.DWord);
            key.SetValue(nameof(Configuration.Default.ImbalancePercentageThreshold), configuration.ImbalancePercentageThreshold, RegistryValueKind.DWord);
            key.SetValue(nameof(Configuration.Default.ImbalanceDurationThresholdMs), configuration.ImbalanceDurationThresholdMs, RegistryValueKind.DWord);
            key.SetValue(nameof(Configuration.Default.WireOvercurrentAmperageThreshold), configuration.WireOvercurrentAmperageThreshold.ToString(CultureInfo.InvariantCulture), RegistryValueKind.String);
            key.SetValue(nameof(Configuration.Default.WireOvercurrentThresholdMs), configuration.WireOvercurrentThresholdMs, RegistryValueKind.DWord);
        }
        catch  (Exception ex)
        {
            _logger.LogError(ex, "Failed to save poll interval to registry");
        }
    }
}