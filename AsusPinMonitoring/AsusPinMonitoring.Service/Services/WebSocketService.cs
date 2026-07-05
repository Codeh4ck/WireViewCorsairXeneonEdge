using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using AsusPinMonitoring.Service.Interfaces;

namespace AsusPinMonitoring.Service.Services;

internal sealed class WebSocketService(ILogger<WebSocketService> logger) : IWebSocketService, IDisposable
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _sockets = new();

    public async Task SendAsync<T>(WebSocket socket, T model, CancellationToken token = default)
    {
        var typeInfo = AppJsonContext.Default.GetTypeInfo(typeof(T));

        if (typeInfo == null)
        {
            logger.LogWarning("Could not find type info for type '{Type}'. The type is not supported by websocket", typeof(T).FullName);
            return;
        }
        
        var bytes = JsonSerializer.SerializeToUtf8Bytes(model, typeInfo);
        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
    }
    
    public async Task BroadcastAsync<T>(T model, CancellationToken token = default)
    {
        if (_sockets.IsEmpty) return;

        var typeInfo = AppJsonContext.Default.GetTypeInfo(typeof(T));
        
        if (typeInfo == null)
        {
            logger.LogWarning("Could not find type info for type '{Type}'. The type is not supported by websocket", typeof(T).FullName);
            return;
        }
        
        var bytes = JsonSerializer.SerializeToUtf8Bytes(model, typeInfo);
        var segment = new ArraySegment<byte>(bytes);
        var deadWebSockets = new List<Guid>();

        foreach (var (id, socket) in _sockets)
        {
            if (socket.State != WebSocketState.Open)
            {
                deadWebSockets.Add(id);
                continue;
            }

            try
            {
                await socket.SendAsync(segment, WebSocketMessageType.Text, true, token);
            }
            catch
            {
                deadWebSockets.Add(id);
            }
        }

        foreach (var id in deadWebSockets)
        {
            _sockets.TryRemove(id, out _);
        }
    }
    
    public async Task HandleClientAsync(WebSocket webSocket, CancellationToken token)
    {
        var id = Guid.NewGuid();
        _sockets.TryAdd(id, webSocket);

        try
        {
            var buffer = new byte[64];

            while (webSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                var result = await webSocket.ReceiveAsync(buffer, token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                }
            }
        }
        finally
        {
            _sockets.TryRemove(id, out _);

            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }
    }

    public async Task DisconnectAllAsync()
    {
        foreach (var (id, socket) in _sockets)
        {
            try
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, "Service restarting.", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to close WebSocket client {Id}", id);
            }

            _sockets.TryRemove(id, out _);
        }
    }

    public void Dispose()
    {
        foreach (var (id, socket) in _sockets)
        {
            try
            {
                socket.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not dispose WebSocket client {Id}", id);
            }
        }

        _sockets.Clear();
    }
}