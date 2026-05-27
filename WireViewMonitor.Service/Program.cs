using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting.WindowsServices;
using WireViewMonitor.Contracts.Input;
using WireViewMonitor.Contracts.Output;
using WireViewMonitor.Service;
using WireViewMonitor.Service.Services;
using WireViewMonitor.Service.Stores;
using WireViewMonitor.Service.WireView;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Host.UseWindowsService(o => o.ServiceName = "WireViewSensorService");

builder.Logging
    .ClearProviders()
    .AddEventLog(s =>
    {
        s.SourceName = "WireViewSensorService";
        s.Filter = (_, level) => level >= LogLevel.Warning;
    });

if (!WindowsServiceHelpers.IsWindowsService())
{
    builder.Logging.AddConsole();
}

builder.Services
    .ConfigureHttpJsonOptions(o => o.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default))
    .AddSingleton<SensorStore>()
    .AddSingleton<WireViewProReader>()
    .AddSingleton<WebSocketService>()
    .AddSingleton<ConfigStore>()
    .AddHostedService<WireViewService>()
    .AddHostedService<WireViewAppWatcherService>()
    .AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.WebHost.ConfigureKestrel(k =>
    k.ListenLocalhost(59273));

await using var app = builder.Build();
app.UseWebSockets().UseCors();

app.MapGet("/sensors", (SensorStore store) =>
    store.Latest is { } data
        ? Results.Ok(data)
        : Results.Json(ErrorResponse.DeviceNotAvailableOrDisconnected,
            AppJsonContext.Default.ErrorResponse, statusCode: 503));

app.MapGet("/health",
    (SensorStore store) => Results.Json(new HealthResponse { Status = store.Latest is not null ? "ok" : "no_device" },
        AppJsonContext.Default.HealthResponse));

app.MapGet("/config",
    (ConfigStore config) => Results.Json(new ConfigResponse { PollIntervalMs = config.PollIntervalMs }, AppJsonContext.Default.ConfigResponse));

app.MapPost("/config", (ConfigRequest request, ConfigStore config) =>
{
    config.PollIntervalMs = request.PollIntervalMs;
    return Results.Json(new ConfigResponse { PollIntervalMs = config.PollIntervalMs }, AppJsonContext.Default.ConfigResponse);
});

app.MapPost("/faults/clear", (ClearFaultsRequest request, WireViewProReader reader, SensorStore store) =>
{
    if (store.Latest is null)
    {
        return Results.Json(ErrorResponse.DeviceNotAvailableOrDisconnected,
            AppJsonContext.Default.ErrorResponse, statusCode: 503);
    }

    var statusMask = (ushort)(0xFFFF & ~request.FaultStatusMask);
    var logMask = (ushort)(0xFFFF & ~request.FaultLogMask);

    reader.ClearFaults(statusMask, logMask);
    return Results.Ok();
});

app.MapGet("/device/config", (WireViewProReader reader, SensorStore store) =>
{
    if (store.Latest is null)
    {
        return Results.Json(ErrorResponse.DeviceNotAvailableOrDisconnected, AppJsonContext.Default.ErrorResponse, statusCode: 503);
    }

    var config = reader.ReadConfig();
    return config is not null
        ? Results.Ok(config)
        : Results.Json(ErrorResponse.DeviceNotAvailableOrDisconnected, AppJsonContext.Default.ErrorResponse, statusCode: 500);
});

app.MapGet("/device/config/raw", (WireViewProReader reader) =>
{
    var raw = reader.ReadConfigRaw();
    return raw is not null
        ? Results.Ok(new { bytes = BitConverter.ToString(raw), length = raw.Length })
        : Results.Problem("Failed to read raw config.");
});

app.MapPost("/restart", async (WebSocketService webSocketService) =>
{
    await webSocketService.DisconnectAllAsync();

    _ = Task.Run(async () =>
    {
        await Task.Delay(500);
        Environment.Exit(1);
    });

    return Results.Ok();
});

app.Map("/ws", async (HttpContext ctx, WebSocketService webSocketService, SensorStore store) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = 400;
        return;
    }

    var socket = await ctx.WebSockets.AcceptWebSocketAsync();
    var id = Guid.NewGuid();

    webSocketService.Add(id, socket);

    if (store.Latest is { } latest)
    {
        var json = JsonSerializer.Serialize(latest, AppJsonContext.Default.SensorResponse);
        var bytes = Encoding.UTF8.GetBytes(json);
        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    var buffer = new byte[64];

    while (socket.State == WebSocketState.Open)
    {
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        if (result.MessageType == WebSocketMessageType.Close)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
        }
    }

    webSocketService.Remove(id);
});

await app.RunAsync();