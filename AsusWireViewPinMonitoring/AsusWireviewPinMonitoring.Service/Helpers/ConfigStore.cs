using Microsoft.Win32;
using System.Globalization;
using AsusWireviewPinMonitoring.Contracts.Common;
using AsusWireviewPinMonitoring.Service.Interfaces;

namespace AsusWireviewPinMonitoring.Service.Helpers;

internal sealed class ConfigStore : IConfigStore
{
    private const string RegistryKey = @"SOFTWARE\AsusWireviewPinMonitoringService";

    private volatile Configuration _configuration;
    
    private readonly ILogger<ConfigStore> _logger;
    private readonly IWireViewProReader _wireViewProReader;

    public ConfigStore(ILogger<ConfigStore> logger, IWireViewProReader wireViewProReader)
    {
        _logger = logger;
        _wireViewProReader = wireViewProReader;
        
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

            return new Configuration
            {
                PollIntervalMs = pollIntervalMs,
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
            
            _wireViewProReader.PollIntervalMs = configuration.PollIntervalMs;
        }
        catch  (Exception ex)
        {
            _logger.LogError(ex, "Failed to save poll interval to registry");
        }
    }
}