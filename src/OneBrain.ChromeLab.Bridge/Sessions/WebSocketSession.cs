using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using OneBrain.ChromeLab.Bridge.Sessions;

namespace OneBrain.ChromeLab.Bridge.Sessions;

public sealed class WebSocketSession : IWebSocketSession
{
    private readonly IMessageHandler _handler;
    private readonly ProtocolEventBuffer _events;

    public string ClientId { get; }

    public WebSocketSession(string clientId, IMessageHandler handler, ProtocolEventBuffer events)
    {
        ClientId = clientId;
        _handler = handler;
        _events = events;
    }

    public async Task RunAsync(WebSocket socket, CancellationToken ct)
    {
        _events.Add("ws.accepted", "WebSocket accepted", clientId: ClientId);

        try
        {
            while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var received = await ReceiveTextMessageAsync(socket, ct);
                if (received == null)
                    break;

                try
                {
                    var response = await _handler.HandleAsync(received, ClientId, ct);
                    if (!string.IsNullOrEmpty(response))
                        await SendRawAsync(socket, response, ct);
                }
                catch (JsonException)
                {
                    _events.Add("protocol.error", "Malformed JSON ignored", clientId: ClientId);
                    await SendRawAsync(socket, JsonSerializer.Serialize(new
                    {
                        type = "protocol.error",
                        error = "malformed_json",
                        message = "Malformed JSON ignored."
                    }, ChromeLabProtocol.JsonOptions), ct);
                }
                catch (Exception ex)
                {
                    _events.Add("protocol.error", ex.Message, clientId: ClientId);
                    await SendRawAsync(socket, JsonSerializer.Serialize(new
                    {
                        type = "protocol.error",
                        error = "message_failed",
                        message = ex.Message
                    }, ChromeLabProtocol.JsonOptions), ct);
                }
            }
        }
        finally
        {
            _events.Add("ws.closed", "WebSocket closed", clientId: ClientId);
        }
    }

    private static async Task<string?> ReceiveTextMessageAsync(WebSocket socket, CancellationToken ct)
    {
        var buffer = new byte[16 * 1024];
        using var stream = new MemoryStream();
        while (true)
        {
            var result = await socket.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close)
                return null;
            if (result.MessageType != WebSocketMessageType.Text)
                throw new InvalidOperationException("Only text messages supported.");
            stream.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
                break;
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static async Task SendRawAsync(WebSocket socket, string payload, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
    }
}
