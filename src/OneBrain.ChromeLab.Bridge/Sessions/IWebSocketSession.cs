using System.Net.WebSockets;
using System.Text.Json;

namespace OneBrain.ChromeLab.Bridge.Sessions;

public interface IWebSocketSession
{
    string ClientId { get; }
    Task RunAsync(WebSocket socket, CancellationToken ct);
}

public interface IMessageHandler
{
    Task<string?> HandleAsync(string json, string clientId, CancellationToken ct);
}
