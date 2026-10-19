using AsusPinMonitoring.Contracts.Common;
using AsusPinMonitoring.Service.Helpers;
using AsusPinMonitoring.Service.Interfaces;
using AsusPinMonitoring.Service.Visitors;
using LibreHardwareMonitor.Hardware;

namespace AsusPinMonitoring.Service.Services;

internal sealed class AsusPinSensorService(
    ILogger<AsusPinSensorService> logger,
    IWebSocketService webSocketService,
    IConfigStore configStore,
    ISensorReadingStore sensorReadingStore,
    IImbalanceCheckWindow imbalanceCheckWindow,
    IOvercurrentCheckWindow overcurrentCheckWindow) : BackgroundService
{
    private readonly Computer _computer = new() { IsGpuEnabled = true };

    private IReadOnlyList<(ISensor Sensor, string FriendlyName, string HardwareName)> _trackedSensors = [];

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _computer.Open();
            _computer.Accept(new SensorUpdateVisitor());

            _trackedSensors = _computer.Hardware
                .Where(x => x.HardwareType == HardwareType.GpuNvidia
                        && x.Name.Contains("rtx", StringComparison.OrdinalIgnoreCase) 
                        && x.Name.Contains("5090", StringComparison.OrdinalIgnoreCase) || x.Name.Contains("5080"))
                .SelectMany(h => h.Sensors
                    .Where(s => Maps.FriendlyNames.ContainsKey((s.Name, s.SensorType)))
                    .Select(s => (Sensor: s, FriendlyName: Maps.FriendlyNames[(s.Name, s.SensorType)], HardwareName: h.Name)))
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to open hardware monitor. Ensure the service is running with elevated privileges");
            throw;
        }

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = imbalanceCheckWindow.StartCheckWindowAsync(stoppingToken);
        _ = overcurrentCheckWindow.StartCheckWindowAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var hardware in _computer.Hardware)
            {
                try
                {
                    hardware.Update();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to update hardware {HardwareName}, skipping tick", hardware.Name);
                }
            }

            var readings = _trackedSensors
                .Where(x => x.Sensor.Value is not null)
                .Select(x => new GpuSensorReading
                {
                    HardwareName = x.HardwareName,
                    SensorName = x.FriendlyName,
                    SensorType = x.Sensor.SensorType.ToString(),
                    Value = x.Sensor.Value!.Value,
                })
                .ToList();

            var pinCurrents = readings
                .Where(r => r.SensorType == nameof(SensorType.Current)
                            && r.SensorName.StartsWith("Pin ", StringComparison.Ordinal))
                .ToList();

            if (pinCurrents.Count > 0)
            {
                var maxPin = pinCurrents.MaxBy(r => r.Value)!;
                var minPin = pinCurrents.MinBy(r => r.Value)!;

                imbalanceCheckWindow.SetMaxAmperage(maxPin.Value, maxPin.SensorName);
                imbalanceCheckWindow.SetMinAmperage(minPin.Value, minPin.SensorName);

                overcurrentCheckWindow.SetPinAmperages(
                    pinCurrents.ToDictionary(r => r.SensorName, r => r.Value));
            }

            try
            {
                var snapshot = new GpuSensorSnapshot
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    Sensors = readings,
                    CurrentImbalance = imbalanceCheckWindow.GetCurrentImbalance(),
                    WireOvercurrent = overcurrentCheckWindow.GetWireOvercurrent(),
                };

                sensorReadingStore.Update(snapshot);
                await webSocketService.BroadcastAsync(snapshot, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to broadcast sensor snapshot");
            }

            await Task.Delay(configStore.Configuration.PollIntervalMs, stoppingToken);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _computer.Close();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to close hardware monitor cleanly");
        }

        return base.StopAsync(cancellationToken);
    }
}