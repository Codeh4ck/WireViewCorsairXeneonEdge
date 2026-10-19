// See https://aka.ms/new-console-template for more information

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

using var webSocket = new ClientWebSocket();

Console.WriteLine("Connecting to ws://localhost:59274/ws ...");

await webSocket.ConnectAsync(new Uri("ws://localhost:59274/ws"), CancellationToken.None);

Console.WriteLine("Connected. Waiting for messages. Press Ctrl+C to exit.\n");

var buffer = new byte[8192];

while (webSocket.State == WebSocketState.Open)
{
    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

    if (result.MessageType == WebSocketMessageType.Close)
    {
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        break;
    }

    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
    var jsonDocument = JsonDocument.Parse(json);

    Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}] Received:");
    Console.WriteLine(json);
    Console.WriteLine("----------------------------------------------------------");
    Console.WriteLine();
}