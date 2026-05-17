using System.Text.Json;
using WireViewMonitor.Contracts.Common;
using WireViewMonitor.Contracts.Output;
using WireViewMonitor.Contracts.WireView;
using WireViewMonitor.Service.Stores;
using WireViewMonitor.Service.WireView;

namespace WireViewMonitor.Service.Services;

internal sealed class WireViewService(
    ILogger<WireViewService> logger,
    IHostApplicationLifetime appLifetime,
    WireViewProReader reader,
    WebSocketService webSocketService,
    SensorStore store,
    ConfigStore configStore) : BackgroundService
{
    // 5 attempts * 3 s = 15s startup window before giving up
    private const int MaxStartupAttempts = 5;

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        reader.ConnectionChanged += (_, connected) =>
        {
            if (connected)
            {
                logger.LogInformation("WireView Pro 2 connected on {Port}", reader.PortName);
            }
            else
            {
                logger.LogWarning("WireView Pro 2 disconnected");
                store.Clear();
            }
        };

        reader.DataUpdated += async (_, data) =>
        {
            var response = new SensorResponse
            {
                Pins = BuildPins(data),
                TempInC = data.TempInC,
                TempOutC = data.TempOutC,
                ExtTemp1C = data.ExtTemp1C,
                ExtTemp2C = data.ExtTemp2C,
                TotalCurrentA = data.TotalCurrentA,
                TotalPowerW = data.TotalPowerW,
                PsuCapabilityW = data.PsuCapabilityW,
                FaultStatus = data.FaultStatus,
                FaultLog = data.FaultLog,
                Timestamp = DateTimeOffset.UtcNow
            };

            store.Update(response);

            if (webSocketService.Count > 0)
            {
                var json = JsonSerializer.Serialize(response, AppJsonContext.Default.SensorResponse);
                await webSocketService.BroadcastAsync(json, token);
            }
        };

        reader.Start(configStore.PollIntervalMs);

        for (int attempt = 1; attempt <= MaxStartupAttempts; attempt++)
        {
            try
            {
                await Task.Delay(configStore.PollIntervalMs, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (store.Latest is not null)
            {
                goto running;
            }

            logger.LogWarning("WireView Pro 2 not found. Attempt {Attempt}/{Max}", attempt, MaxStartupAttempts);
        }

        logger.LogError("WireView Pro 2 not found after {Max} attempts. Shutting down...", MaxStartupAttempts);

        appLifetime.StopApplication();
        return;

        running:
        logger.LogInformation("WireView service running on port 5123");

        try
        {
            await Task.Delay(Timeout.Infinite, token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static PinReading[] BuildPins(WireViewSensorData sensorData)
    {
        var pins = new PinReading[6];

        for (int i = 0; i < 6; i++)
        {
            pins[i] = new PinReading
            {
                Voltage = sensorData.PinVoltage[i],
                Current = sensorData.PinCurrent[i],
                Power = sensorData.PinPower[i],
            };
        }

        return pins;
    }
}