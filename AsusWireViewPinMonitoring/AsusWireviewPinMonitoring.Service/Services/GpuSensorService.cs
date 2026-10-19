using AsusWireviewPinMonitoring.Contracts.Common;
using AsusWireviewPinMonitoring.Service.Helpers;
using AsusWireviewPinMonitoring.Service.Interfaces;
using AsusWireviewPinMonitoring.Service.Visitors;
using AsusWireviewPinMonitoring.Service.Wireview;
using LibreHardwareMonitor.Hardware;

namespace AsusWireviewPinMonitoring.Service.Services;

internal sealed class GpuSensorService(
    ILogger<GpuSensorService> logger,
    IWebSocketService webSocketService,
    IConfigStore configStore,
    IWireViewProReader wireViewReader,
    ISensorReadingStore sensorReadingStore) : BackgroundService
{
    private readonly Computer _computer = new() { IsGpuEnabled = true };

    private IReadOnlyList<(ISensor Sensor, string FriendlyName, string HardwareName)> _trackedSensors = [];

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            wireViewReader.ConnectionChanged += (_, connected) =>
            {
                if (connected)
                {
                    logger.LogInformation("WireView Pro 2 connected on {Port}", wireViewReader.PortName);
                }
                else
                {
                    logger.LogWarning("WireView Pro 2 disconnected");
                    sensorReadingStore.Clear();
                }
            };

            wireViewReader.DataUpdated += WireviewReaderOnDataUpdated;

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

            wireViewReader.Start(configStore.Configuration.PollIntervalMs);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to open hardware monitor. Ensure the service is running with elevated privileges");
            throw;
        }

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _computer.Close();
            wireViewReader.Dispose();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to close hardware monitor cleanly");
        }

        return base.StopAsync(cancellationToken);
    }

    private async void WireviewReaderOnDataUpdated(object? sender, WireViewSensorData wireViewSensorData)
    {
        try
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
            
            readings.AddRange(ConvertWireViewPinsToDomain(wireViewSensorData));
            
            try
            {
                var snapshot = new GpuSensorSnapshot
                {
                    Timestamp = DateTime.UtcNow,
                    Sensors = readings,
                    WireViewData = new WireViewData
                    {
                        OnboardTempIn = TruncateTo1Decimal(wireViewSensorData.TempInC),
                        OnboardTempOut = TruncateTo1Decimal(wireViewSensorData.TempOutC),
                        ExternalTemp1 = wireViewSensorData.ExtTemp1C < 0 ? null : TruncateTo1Decimal(wireViewSensorData.ExtTemp1C),
                        ExternalTemp2 = wireViewSensorData.ExtTemp2C < 0 ? null : TruncateTo1Decimal(wireViewSensorData.ExtTemp2C),
                        TotalCurrent = TruncateTo2Decimals(wireViewSensorData.TotalCurrentA),
                        TotalVoltage = TruncateTo2Decimals(wireViewSensorData.TotalPowerW / wireViewSensorData.TotalCurrentA),
                        TotalPower = TruncateTo2Decimals(wireViewSensorData.TotalPowerW),
                        PsuCapabilityWatts = wireViewSensorData.PsuCapabilityW,
                        FaultStatus = wireViewSensorData.FaultStatus,
                        FaultLog = wireViewSensorData.FaultLog
                    }
                };

                sensorReadingStore.Update(snapshot);
                await webSocketService.BroadcastAsync(snapshot);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to broadcast sensor snapshot");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error processing WireView Pro 2 data update");
        }
    }

    private static double TruncateTo1Decimal(double value) => Math.Truncate(value * 10) / 10;
    private static double TruncateTo2Decimals(double value) => Math.Truncate(value * 100) / 100;

    private List<GpuSensorReading> ConvertWireViewPinsToDomain(WireViewSensorData wireViewSensorData)
    {
        var result = new List<GpuSensorReading>(6 * 3);

        for (var x = 0; x < 6; x++)
        {
            var pinName = $"Pin {x + 1}";

            result.Add(CreateReading("WireView Pro 2", pinName, nameof(SensorType.Current), wireViewSensorData.PinCurrent[x]));
            result.Add(CreateReading("WireView Pro 2", pinName, nameof(SensorType.Voltage), wireViewSensorData.PinVoltage[x]));
            result.Add(CreateReading("WireView Pro 2", pinName, nameof(SensorType.Power), wireViewSensorData.PinPower[x]));
        }

        return result;
    }

    private static GpuSensorReading CreateReading(string hardwareName, string sensorName, string sensorType, double value)
    {
        return new GpuSensorReading
        {
            HardwareName = hardwareName,
            SensorName = sensorName,
            SensorType = sensorType,
            Value = (float)TruncateTo2Decimals(value),
        };
    }
}