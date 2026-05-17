using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using WireViewMonitor.Service.WireView;

namespace WireViewMonitor.Service.Services;

internal sealed class WireViewAppWatcherService(ILogger<WireViewAppWatcherService> logger, WireViewProReader reader) : BackgroundService
{
    private const int CheckIntervalMs = 5000;
    private const int YieldMs = 3000;
    
    private const string ProcessName = "WireView2";
    private const string ThermalGrizzlySignature = "HRB 14296";

    private bool _wasRunning;
    private string? _verifiedPath;

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var isRunning = IsWireViewProRunning();

                switch (isRunning)
                {
                    case true when !_wasRunning:
                        logger.LogInformation("WireView Pro 2 application detected. Yielding for {Ms}ms", YieldMs);

                        await reader.YieldToWireViewAsync(YieldMs, token);

                        logger.LogInformation("Yield complete. Resuming normal polling...");
                        break;
                    case false:
                        _verifiedPath = null;
                        break;
                }

                _wasRunning = isRunning;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error in WireView app watcher");
            }

            await Task.Delay(CheckIntervalMs, token);
        }
    }

    private bool IsWireViewProRunning()
    {
        try
        {
            var processes = Process.GetProcessesByName(ProcessName);
            if (processes.Length == 0)
            {
                return false;
            }

            foreach (var process in processes)
            {
                try
                {
                    var fileName = process.MainModule?.FileName;
                    if (fileName is null)
                    {
                        continue;
                    }

                    if (!Path.GetFileName(fileName).Contains(ProcessName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (fileName == _verifiedPath)
                    {
                        return true;
                    }

#pragma warning disable SYSLIB0057
                    var cert = new X509Certificate2(X509Certificate.CreateFromSignedFile(fileName));
#pragma warning restore SYSLIB0057

                    if (cert.Subject.Contains(ThermalGrizzlySignature, StringComparison.Ordinal))
                    {
                        _verifiedPath = fileName;
                        return true;
                    }
                }
                catch
                {
                    // Unsigned, access denied, or invalid cert — skip
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}