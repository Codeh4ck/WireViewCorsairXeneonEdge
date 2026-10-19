using System.Net.WebSockets;

namespace AsusWireviewPinMonitoring.Service.Interfaces;

public interface IWebSocketService
{
    Task SendAsync<T>(WebSocket socket, T model, CancellationToken token = default);
    Task BroadcastAsync<T>(T model, CancellationToken token = default);
    Task HandleClientAsync(WebSocket webSocket, CancellationToken token);
    Task DisconnectAllAsync();
}