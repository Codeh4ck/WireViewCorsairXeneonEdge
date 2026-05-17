using Microsoft.Win32;
using WireViewMonitor.Service.WireView;

namespace WireViewMonitor.Service.Stores;

internal sealed class ConfigStore
{
    private const string RegistryKey = @"SOFTWARE\WireViewSensorService";
    private const string PollValueName = "PollIntervalMs";
    private const int DefaultPollMs = 1_000;

    private volatile int _pollIntervalMs;
    
    private readonly ILogger<ConfigStore> _logger;
    private readonly WireViewProReader _reader;

    public ConfigStore(ILogger<ConfigStore> logger, WireViewProReader reader)
    {
        _logger = logger;
        _reader = reader;

        _pollIntervalMs = Load();
    }
    
    public int PollIntervalMs
    {
        get => _pollIntervalMs;
        set
        {
            _pollIntervalMs = Math.Clamp(value, 100, 30_000);
            Save(_pollIntervalMs);
            _reader.PollIntervalMs = _pollIntervalMs;
        }
    }
    
    private int Load()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(RegistryKey);

            if (key?.GetValue(PollValueName) is int value)
            {
                return Math.Clamp(value, 100, 30_000);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load poll interval from registry");
        }

        return DefaultPollMs;
    }

    private void Save(int value)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(RegistryKey, writable: true);
            key.SetValue(PollValueName, value, RegistryValueKind.DWord);
        }
        catch  (Exception ex)
        {
            _logger.LogError(ex, "Failed to save poll interval to registry");
        }
    }
}