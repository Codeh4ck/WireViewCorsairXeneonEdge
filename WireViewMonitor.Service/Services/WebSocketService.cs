using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace WireViewMonitor.Service.Services;

internal sealed class WebSocketService(ILogger<WebSocketService> logger) : IDisposable
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _sockets = new();

    public void Add(Guid id, WebSocket socket) => _sockets[id] = socket;

    public void Remove(Guid id) => _sockets.TryRemove(id, out _);

    public int Count => _sockets.Count;

    public async Task BroadcastAsync(string json, CancellationToken token = default)
    {
        if (_sockets.IsEmpty) return;

        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        foreach (var (id, socket) in _sockets)
        {
            if (socket.State != WebSocketState.Open)
            {
                Remove(id);
                continue;
            }

            try
            {
                await socket.SendAsync(segment, WebSocketMessageType.Text, true, token);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send to WebSocket client {Id}. Removing...", id);
                Remove(id);
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

            Remove(id);
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