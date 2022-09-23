using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Text;

namespace Obelisco.Network;

public abstract class P2P : IDisposable
{
    public const int BUFFER_SIZE = 4 * 1024 * 1024;
    protected readonly WebSocket m_socket;
    protected readonly ILogger m_logger;
    private bool m_isDisposed;
    private BlockingCollection<Response> m_responses;

    internal P2P(ILogger logger, WebSocket socket, Guid id)
    {
        ID = id;
        m_logger = logger;
        m_socket = socket;
        m_responses = new BlockingCollection<Response>();
    }

    public bool IsDisposed => m_isDisposed;
    public Guid ID { get; }
    public bool IsFullNode { get; private set; }

    internal void Init()
    {
        var source = new CancellationTokenSource();
        var task = GetNodeType(source.Token).AsTask();
        if (!task.Wait(30 * 1000))
            throw new InvalidOperationException("The P2P dont response GetNodeType.");
        IsFullNode = task.Result;
    }

    internal async ValueTask Receive(CancellationToken cancellationToken)
    {
        var buffer = new ArraySegment<byte>(new byte[BUFFER_SIZE]);

        while (true)
        {
            WebSocketReceiveResult result = await m_socket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                OnClose(result.CloseStatus!.Value, result.CloseStatusDescription!);
                break;
            }

            if (result.Count > BUFFER_SIZE)
            {
                await m_socket.CloseAsync(WebSocketCloseStatus.MessageTooBig,
                    $"Web socket frame cannot exceed buffer size of {BUFFER_SIZE:#,##0} bytes. Send multiple frames instead.",
                    cancellationToken);
                break;
            }

            var str = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
            m_logger.LogInformation($"Receive: {str}");

            Message? message = JsonSerializer.Deserialize<Message>(str);
            LogInfo($"{this.ID}, {message?.GetType().Name ?? "NULL"}: {str}");

            await OnMessage(message, cancellationToken);
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }

    protected void Dispose(bool disposing)
    {
        if (disposing)
        {
            var close = m_socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
            close.Wait();
        }
    }

    public async ValueTask<bool> GetNodeType(CancellationToken cancellationToken)
    {
        await SendMessage(new GetNodeTypeRequest(), cancellationToken);
        return WaitResponse<NodeTypeResponse>(cancellationToken).IsFullNode;
    }

    public async ValueTask<string> GetServerAddress(CancellationToken cancellationToken)
    {
        await SendMessage(new GetServerAddressRequest(), cancellationToken);
        return WaitResponse<ServerAddressResponse>(cancellationToken).Uri;
    }

    protected abstract ValueTask GetNodeTypeResponse(CancellationToken cancellationToken);
    protected abstract ValueTask GetServerAddressResponse(CancellationToken cancellationToken);

    #region callbacks
    protected virtual async ValueTask OnMessage(Message? message, CancellationToken cancellationToken)
    {
        await Task.Yield();
        if (message is Response response)
            m_responses.Add(response);
        else
            switch (message)
            {
                case GetNodeTypeRequest m:
                    await GetNodeTypeResponse(cancellationToken);
                    break;
                case GetServerAddressRequest m:
                    await GetServerAddressResponse(cancellationToken);
                    break;
                default:
                    var str = message != null ? JsonSerializer.Serialize<Message>(message) : "NULL";
                    LogError($"Invalid message: {str}");
                    break;
            }
    }

    private void OnClose(WebSocketCloseStatus closeStatus, string description)
    {
        m_logger.LogWarning(new EventId((int)closeStatus, closeStatus.ToString()), description);
    }
    #endregion

    #region protected methods
    protected void LogError(string message)
    {
        m_logger.LogError(message);
    }

    protected void LogInfo(string message)
    {
        m_logger.LogInformation(message);
    }

    protected TResponse WaitResponse<TResponse>(CancellationToken cancellationToken) where TResponse : Response
    {
        const int timetout = 30;
        Response response;
        while (m_responses.TryTake(out response, 1000 * timetout, cancellationToken))
        {
            if (response is TResponse t)
            {
                if (t.Ok)
                    return t;
                else
                    throw new ResponseErrorException($"Error in response: {t.Message}");
            }
            else
                m_logger.LogWarning($"Miss response: '{JsonSerializer.Serialize<Message>(response)}'.");
        }
        throw new TimeoutException($"Fail to wait for response. Timeout has been reached({timetout} seconds)");
    }

    protected async ValueTask Send(string str, CancellationToken cancellationToken)
    {
        var data = Encoding.UTF8.GetBytes(str);
        var buffer = new ArraySegment<byte>(data);
        m_logger.LogInformation($"Send: {str}");
        await m_socket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
    }

    protected void Error(string message)
    {
        m_logger.LogError(message);
    }

    protected async ValueTask SendMessage(Message message, CancellationToken cancellationToken)
    {
        var str = JsonSerializer.Serialize<Message>(message);
        await Send(str, cancellationToken);
    }

    protected async ValueTask SendResponse<TResponse>(TResponse? response, CancellationToken cancellationToken, string? message = null) where TResponse : Response, new()
    {
        response ??= new TResponse() { Ok = false, Message = message ?? string.Empty };
        var str = JsonSerializer.Serialize<Message>(response);
        await Send(str, cancellationToken);
    }

    protected async ValueTask SendOkResponse(CancellationToken cancellationToken)
    {
        await SendResponse(new Response(), cancellationToken);
    }

    protected async ValueTask SendErrorResponse(string message, CancellationToken cancellationToken)
    {
        await SendResponse<Response>(null, cancellationToken, message);
    }

    protected async ValueTask SendBlockResponse(Block? block, CancellationToken cancellationToken, string? message = null)
    {
        BlockResponse? response = null;
        if (block != null)
            response = new BlockResponse() { Block = block };
        await SendResponse(response, cancellationToken, message);
    }

    #endregion
}
