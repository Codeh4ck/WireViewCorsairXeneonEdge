using AsusPinMonitoring.Contracts.Input;
using AsusPinMonitoring.Contracts.Output;
using AsusPinMonitoring.Service;
using AsusPinMonitoring.Service.Helpers;
using AsusPinMonitoring.Service.Interfaces;
using AsusPinMonitoring.Service.Services;
using Microsoft.Extensions.Hosting.WindowsServices;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Host.UseWindowsService(o => o.ServiceName = "AsusPinMonitoringService");

builder.Logging
    .ClearProviders()
    .AddEventLog(s =>
    {
        s.SourceName = "AsusPinMonitoringService";
        s.Filter = (_, level) => level >= LogLevel.Warning;
    });

if (!WindowsServiceHelpers.IsWindowsService())
{
    builder.Logging.AddConsole();
}

builder.Services
    .ConfigureHttpJsonOptions(o => o.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default))
    .AddSingleton<IWebSocketService, WebSocketService>()
    .AddSingleton<ISensorReadingStore, SensorReadingStore>()
    .AddSingleton<IConfigStore, ConfigStore>()
    .AddSingleton<IImbalanceCheckWindow, ImbalanceCheckWindow>()
    .AddSingleton<IOvercurrentCheckWindow, OvercurrentCheckWindow>()
    .AddHostedService<AsusPinSensorService>()
    .AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.WebHost.ConfigureKestrel(k =>
    k.ListenLocalhost(59274));

await using var app = builder.Build();
app.UseWebSockets().UseCors();

app.MapGet("/config",
    (IConfigStore configStore) => Results.Json(configStore.Configuration.ToResponse(), AppJsonContext.Default.GetConfigResponse));

app.MapPut("/config", (UpdateConfigRequest request, IConfigStore configStore) =>
{
    configStore.Configuration = request.ToConfiguration();
    return Results.Json(configStore.Configuration.ToResponse(), AppJsonContext.Default.GetConfigResponse);
});

app.MapGet("/sensors", (ISensorReadingStore store) =>
    store.Latest is { } data ? Results.Ok(data) : Results.Json(ErrorResponse.SensorSnapshotNotAvailable, AppJsonContext.Default.ErrorResponse, statusCode: 503));

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

app.Map("/ws", async (HttpContext ctx, IWebSocketService webSocketService, ISensorReadingStore store, CancellationToken token) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    var socket = await ctx.WebSockets.AcceptWebSocketAsync();

    if (store.Latest is { } snapshot)
    {
        await webSocketService.SendAsync(socket, snapshot, token);
    }

    await webSocketService.HandleClientAsync(socket, token);
});

await app.RunAsync();