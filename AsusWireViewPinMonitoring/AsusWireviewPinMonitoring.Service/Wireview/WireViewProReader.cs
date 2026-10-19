using System.IO.Ports;
using System.Text;
using AsusWireviewPinMonitoring.Contracts.Common;
using AsusWireviewPinMonitoring.Service.Interfaces;

namespace AsusWireviewPinMonitoring.Service.Wireview;

public sealed class WireViewProReader(ILogger<WireViewProReader> logger) : IDisposable, IWireViewProReader
{
    private const int Baud = 115200;
    private const int StructSize = 100;
    private const string WelcomeString = "Thermal Grizzly WireView Pro II"; // 31 chars + \0 = 32 bytes

    private const int DefaultPollMs = 500;
    private const int ReadTimeoutMs = 500;
    private const int LockTimeoutMs = 1000;

    private const int OffTs = 0;
    private const int OffReadings = 12;
    private const int OffHpwrCap = 94;
    private const int OffFaultStatus = 96;
    private const int OffFaultLog = 98;

    private const int PsVoltage = 0;
    private const int PsCurrent = 4;
    private const int PsSize = 12;

    private const int ConfigV1Size = 64; // Marshal.SizeOf<DeviceConfigStructV1>()
    private const int ConfigV2Size = 72; // Marshal.SizeOf<DeviceConfigStructV2>()
    private const int ConfigV3Size = 88; // Marshal.SizeOf<DeviceConfigStructV3>()

    private const byte CmdReadVendorData = 0x01;
    private const byte CmdReadSensors = 0x04;
    private const byte CmdReadConfig = 0x05;

    // Protocol IDs, not Windows USB VID/PID pairs. Add compatible models here.
    private static readonly Dictionary<(byte VendorId, byte ProductId), string> SupportedDevices = new()
    {
        [(0xEF, 0x05)] = "WireView Pro II",
        [(0xEF, 0x06)] = "WireView Pro II Noctua Edition"
    };

    private readonly object _portGate = new();
    private SharedSerialPort? _port;
    private CancellationTokenSource? _cts;
    private Task? _worker;

    private volatile int _pollIntervalMs = DefaultPollMs;
    private volatile bool _yieldingToWireViewApp;

    private bool _handshakeComplete;

    public int PollIntervalMs
    {
        get => _pollIntervalMs;
        set => _pollIntervalMs = Math.Clamp(value, 100, 30_000);
    }

    public event EventHandler<WireViewSensorData>? DataUpdated;
    public event EventHandler<bool>? ConnectionChanged;

    public bool IsRunning => _worker is { IsCompleted: false };
    public string PortName { get; private set; } = string.Empty;

    public void Start(int pollIntervalMs = DefaultPollMs)
    {
        if (_worker is { IsCompleted: false })
        {
            return;
        }

        _pollIntervalMs = Math.Clamp(pollIntervalMs, 100, 30000);
        _cts = new CancellationTokenSource();
        _worker = Task.Run(() => LoopAsync(_cts.Token));
    }

    public void Dispose()
    {
        _cts?.Cancel();
        try
        {
            _worker?.Wait(2000);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error while waiting for reader loop to stop");
        }

        _cts = null;
        _worker = null;
        DisposePort();
    }

    private void DisposePort()
    {
        lock (_portGate)
        {
            try
            {
                _port?.Dispose();
            }
            catch
            {
                // ignored
            }

            _port = null;
            PortName = string.Empty;
            _handshakeComplete = false;
        }
    }

    private async Task LoopAsync(CancellationToken token)
    {
        var wasConnected = false;

        while (!token.IsCancellationRequested)
        {
            if (_yieldingToWireViewApp)
            {
                await Task.Delay(100, token);
                continue;
            }

            var data = ReadAvailableSensors(token);

            if (data is not null)
            {
                if (!wasConnected)
                {
                    wasConnected = true;
                    ConnectionChanged?.Invoke(this, true);
                }

                DataUpdated?.Invoke(this, data);
            }
            else if (wasConnected)
            {
                wasConnected = false;
                ConnectionChanged?.Invoke(this, false);
            }

            await Task.Delay(_pollIntervalMs, token);
        }
    }

    private WireViewSensorData? ReadAvailableSensors(CancellationToken token)
    {
        lock (_portGate)
        {
            var ports = FindComPorts();
            // Keep the active device first instead of switching with enumeration order.
            if (ports.Remove(PortName))
            {
                ports.Insert(0, PortName);
            }

            foreach (var portName in ports)
            {
                if (token.IsCancellationRequested || _yieldingToWireViewApp)
                {
                    break;
                }

                try
                {
                    if (_port is null || PortName != portName)
                    {
                        DisposePort();
                        PortName = portName;
                        _port = new SharedSerialPort(portName, Baud, Parity.None, 8, StopBits.One)
                        {
                            ReadTimeout = ReadTimeoutMs,
                            WriteTimeout = ReadTimeoutMs
                        };
                    }

                    var data = ReadSensors();
                    if (data is not null)
                    {
                        return data;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error probing or reading WireView on {Port}", portName);
                }

                DisposePort();
            }

            DisposePort();
            return null;
        }
    }

    private WireViewSensorData? ReadSensors()
    {
        if (_port is null)
        {
            return null;
        }

        if (!Monitor.TryEnter(_portGate, LockTimeoutMs))
        {
            logger.LogWarning("Could not acquire port lock on {Port}, skipping poll", PortName);
            return null;
        }

        try
        {
            if (_port is null || !_port.Open())
            {
                logger.LogDebug("Could not open WireView candidate {Port}", PortName);
                return null;
            }

            if (!_handshakeComplete)
            {
                if (!Handshake(_port))
                {
                    _port.Close();
                    return null;
                }

                _handshakeComplete = true;
            }

            _port.DiscardInBuffer();
            _port.Write([CmdReadSensors], 0, 1);

            var buffer = ReadExact(_port, StructSize);
            _port.Close();

            if (buffer is null)
            {
                _handshakeComplete = false;
                logger.LogDebug("Incomplete WireView sensor response on {Port}", PortName);
            }

            return buffer is null ? null : Parse(buffer);
        }
        finally
        {
            _port?.Close();
            Monitor.Exit(_portGate);
        }
    }

    public void ClearFaults(ushort faultStatusMask = 0xFFFF, ushort faultLogMask = 0xFFFF)
    {
        if (_port is null)
        {
            logger.LogWarning("Cannot clear faults - no port available");
            return;
        }

        if (!Monitor.TryEnter(_portGate, LockTimeoutMs))
        {
            logger.LogWarning("Could not acquire port lock on {Port}, skipping fault clear", PortName);
            return;
        }

        try
        {
            if (_port is null || !_port.Open())
            {
                return;
            }

            if (!Handshake(_port))
            {
                return;
            }

            _port.DiscardInBuffer();

            _port.Write([
                14,
                (byte)(faultStatusMask & byte.MaxValue),
                (byte)(faultStatusMask >> 8 & byte.MaxValue),
                (byte)(faultLogMask & byte.MaxValue),
                (byte)(faultLogMask >> 8 & byte.MaxValue)
            ], 0, 5);

            _port.Close();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to clear faults on {Port}", PortName);
        }
        finally
        {
            _port?.Close();
            Monitor.Exit(_portGate);
        }
    }

    public WireViewConfig? ReadConfig()
    {
        if (_port is null)
        {
            return null;
        }

        if (!Monitor.TryEnter(_portGate, LockTimeoutMs))
        {
            logger.LogWarning("Could not acquire port lock on {Port}, skipping config read", PortName);
            return null;
        }

        try
        {
            if (_port is null || !_port.Open())
            {
                return null;
            }

            _port.DiscardInBuffer();

            if (!Handshake(_port))
            {
                _port.Close();
                return null;
            }

            // Read first 4 bytes to determine config version (version is at byte index 2)
            _port.DiscardInBuffer();
            _port.Write([CmdReadConfig], 0, 1);
            var versionBuf = ReadExact(_port, 4);

            _port.Close();

            if (versionBuf is null)
            {
                return null;
            }

            var version = versionBuf[2];

            var structSize = version switch
            {
                0 => ConfigV1Size,
                1 => ConfigV2Size,
                2 => ConfigV3Size,
                _ => 0
            };

            if (structSize == 0)
            {
                return null;
            }

            // Full read with correct size
            if (!_port.Open())
            {
                return null;
            }

            _port.DiscardInBuffer();
            _port.Write([CmdReadConfig], 0, 1);

            var buffer = ReadExact(_port, structSize);
            _port.Close();

            return buffer is null ? null : ParseConfig(buffer, version);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read config on {Port}", PortName);
            return null;
        }
        finally
        {
            _port?.Close();
            Monitor.Exit(_portGate);
        }
    }

    public byte[]? ReadConfigRaw()
    {
        if (_port is null)
        {
            return null;
        }

        if (!Monitor.TryEnter(_portGate, LockTimeoutMs))
        {
            logger.LogWarning("Could not acquire port lock on {Port}, skipping raw config read", PortName);
            return null;
        }

        try
        {
            if (_port is null || !_port.Open())
            {
                return null;
            }

            _port.DiscardInBuffer();

            if (!Handshake(_port))
            {
                _port.Close();
                return null;
            }

            // Read first 4 bytes to determine config version (version is at byte index 2)
            _port.DiscardInBuffer();
            _port.Write([CmdReadConfig], 0, 1);
            var versionBuf = ReadExact(_port, 4);

            _port.Close();

            if (versionBuf is null)
            {
                return null;
            }

            var version = versionBuf[2];

            var structSize = version switch
            {
                0 => ConfigV1Size,
                1 => ConfigV2Size,
                2 => ConfigV3Size,
                _ => 0
            };

            if (structSize == 0)
            {
                return null;
            }

            // Full read with correct size
            if (!_port.Open())
            {
                return null;
            }

            _port.DiscardInBuffer();
            _port.Write([CmdReadConfig], 0, 1);

            var buffer = ReadExact(_port, structSize);
            _port.Close();

            return buffer;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read raw config on {Port}", PortName);
            return null;
        }
        finally
        {
            _port?.Close();
            Monitor.Exit(_portGate);
        }
    }

    private bool Handshake(SharedSerialPort port)
    {
        _handshakeComplete = false;
        var responseSize = WelcomeString.Length + 1; // 32 bytes

        port.DiscardInBuffer();
        byte[]? buf;
        try
        {
            port.RtsEnable = true;
            port.Write([], 0, 0);
            buf = ReadExact(port, responseSize);
        }
        finally
        {
            port.RtsEnable = false;
        }

        if (buf is null)
        {
            logger.LogDebug("No complete WireView welcome response on {Port}", port.PortName);
            return false;
        }

        var response = Encoding.ASCII.GetString(buf, 0, responseSize).TrimEnd('\0');
        if (!string.Equals(response, WelcomeString, StringComparison.Ordinal))
        {
            logger.LogDebug("Unexpected WireView welcome response on {Port}: {Response}", port.PortName, response);
            return false;
        }

        port.DiscardInBuffer();
        port.Write([CmdReadVendorData], 0, 1);
        var vendorData = ReadExact(port, 3);
        if (vendorData is null)
        {
            logger.LogDebug("Incomplete WireView vendor response on {Port}", port.PortName);
            return false;
        }

        if (!SupportedDevices.TryGetValue((vendorData[0], vendorData[1]), out var model))
        {
            logger.LogWarning("Unsupported WireView device on {Port}: vendor {VendorId:X2}, product {ProductId:X2}",
                port.PortName, vendorData[0], vendorData[1]);
            return false;
        }

        logger.LogInformation("Identified {Model} on {Port}, firmware {FirmwareVersion}", model, port.PortName, vendorData[2]);
        _handshakeComplete = true;
        return true;
    }

    private static byte[]? ReadExact(SerialPort port, int size)
    {
        var buffer = new byte[size];
        var offset = 0;
        var start = Environment.TickCount;

        while (offset < size && Environment.TickCount - start < ReadTimeoutMs)
        {
            if (port.BytesToRead > 0)
            {
                offset += port.Read(buffer, offset, size - offset);
            }
            else
            {
                Thread.Sleep(1); // yield CPU instead of spinning
            }
        }

        return offset == size ? buffer : null;
    }

    private static WireViewSensorData Parse(byte[] b)
    {
        var tempIn = ReadI16(b, OffTs + 0) / 10.0;
        var tempOut = ReadI16(b, OffTs + 2) / 10.0;
        var extTemp1 = ReadI16(b, OffTs + 4) / 10.0;
        var extTemp2 = ReadI16(b, OffTs + 6) / 10.0;

        var voltage = new double[6];
        var current = new double[6];
        var power = new double[6];

        for (var i = 0; i < 6; i++)
        {
            var @base = OffReadings + i * PsSize;
            voltage[i] = ReadI16(b, @base + PsVoltage) / 1000.0; // mV → V
            current[i] = ReadU32(b, @base + PsCurrent) / 1000.0; // mA → A
            power[i] = voltage[i] * current[i];
        }

        var psuCapW = b[OffHpwrCap] switch { 0 => 600, 1 => 450, 2 => 300, 3 => 150, _ => 0 };

        return new WireViewSensorData(
            voltage, current, power,
            tempIn, tempOut, extTemp1, extTemp2,
            psuCapW,
            ReadU16(b, OffFaultStatus),
            ReadU16(b, OffFaultLog));
    }

    private static WireViewConfig ParseConfig(byte[] b, int version)
    {
        // 1 byte padding at [35] for FanConfig alignment
        var fanMode = b[36];
        var tempSource = b[37];
        var dutyMin = b[38];
        var dutyMax = b[39];
        var fanTempMin = ReadI16(b, 40) / 10.0;
        var fanTempMax = ReadI16(b, 42) / 10.0;

        var backlightDuty = b[44];
        // 1 byte padding at [45] for ushort alignment

        var faultDisplayEnable = ReadU16(b, 46);
        var faultBuzzerEnable = ReadU16(b, 48);
        var faultSoftPowerEnable = ReadU16(b, 50);
        var faultHardPowerEnable = ReadU16(b, 52);
        var tsFaultThreshold = ReadI16(b, 54) / 10.0;
        var ocpFaultThreshold = b[56];
        var wireOcpFaultThreshold = b[57];
        var oppFaultThreshold = ReadU16(b, 58);
        var currentImbalanceThreshold = b[60];
        var currentImbalanceMinLoad = b[61];
        var shutdownWaitTime = b[62];
        var loggingInterval = b[63];

        string? averaging = version >= 1
            ? b[64] switch
            {
                0 => "22ms",
                1 => "44ms",
                2 => "89ms",
                3 => "177ms",
                4 => "354ms",
                5 => "709ms",
                6 => "1417ms",
                _ => null
            }
            : null;

        return new WireViewConfig
        {
            Version = version,
            BacklightDuty = backlightDuty,
            LoggingInterval = loggingInterval,
            Averaging = averaging,
            FanMode = fanMode == 0 ? "Curve" : "Fixed",
            FanTempSource = tempSource switch
            {
                0 => "TsIn", 1 => "TsOut", 2 => "Ts1", 3 => "Ts2", 4 => "Tmax", _ => "Unknown"
            },
            FanDutyMin = dutyMin,
            FanDutyMax = dutyMax,
            FanTempMinC = fanTempMin,
            FanTempMaxC = fanTempMax,
            TsFaultThresholdC = tsFaultThreshold,
            OcpFaultThresholdA = ocpFaultThreshold,
            WireOcpFaultThresholdA = wireOcpFaultThreshold / 10.0,
            OppFaultThresholdW = oppFaultThreshold,
            CurrentImbalanceThreshold = currentImbalanceThreshold,
            CurrentImbalanceMinLoad = currentImbalanceMinLoad,
            ShutdownWaitTimeS = shutdownWaitTime,
            FaultDisplayEnable = faultDisplayEnable,
            FaultBuzzerEnable = faultBuzzerEnable,
            FaultSoftPowerEnable = faultSoftPowerEnable,
            FaultHardPowerEnable = faultHardPowerEnable
        };
    }

    public async Task YieldToWireViewAsync(int yieldMs, CancellationToken token)
    {
        if (_yieldingToWireViewApp)
        {
            return;
        }

        _yieldingToWireViewApp = true;
        logger.LogInformation("Yielding to WireView Pro 2 app for {Ms}ms", yieldMs);

        DisposePort();

        try
        {
            await Task.Delay(yieldMs, token);
        }
        finally
        {
            _yieldingToWireViewApp = false;
            logger.LogInformation("Yield complete. Resuming normal polling...");
        }
    }

    private static short ReadI16(byte[] b, int o) => (short)(b[o] | b[o + 1] << 8);
    private static ushort ReadU16(byte[] b, int o) => (ushort)(b[o] | b[o + 1] << 8);
    private static uint ReadU32(byte[] b, int o) => (uint)(b[o] | b[o + 1] << 8 | b[o + 2] << 16 | b[o + 3] << 24);

    private List<string> FindComPorts()
    {
        try
        {
            var ports = Stm32PortFinder.FindMatchingComPorts();
            if (ports.Count == 0)
            {
                logger.LogDebug("No matching WireView COM ports found");
            }

            return ports;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to enumerate COM ports");
            return [];
        }
    }
}