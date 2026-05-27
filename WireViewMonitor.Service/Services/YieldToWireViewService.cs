using System.Diagnostics;
using WireViewMonitor.Service.WireView;

namespace WireViewMonitor.Service.Services;

internal sealed class WireViewAppWatcherService(ILogger<WireViewAppWatcherService> logger, WireViewProReader reader) : BackgroundService
{
    private const int StartupCheckIntervalMs = 500;
    private const int StartupWindowSeconds = 30;
    private const int CheckIntervalMs = 1500;
    private const int YieldMs = 3000;
    private const string ProcessName = "WireView2";

    private bool _wasRunning;

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        var startupDeadline = DateTime.UtcNow.AddSeconds(StartupWindowSeconds);

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

            var delay = DateTime.UtcNow < startupDeadline ? StartupCheckIntervalMs : CheckIntervalMs;
            await Task.Delay(delay, token);
        }
    }


    private static bool IsWireViewProRunning()
    {
        try
        {
            return Process.GetProcessesByName(ProcessName).Length > 0;
        }
        catch
        {
            return false;
        }
    }
}