using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ninja.WebSockets;

using Obelisco.Network;

namespace Obelisco;

public class Server : Client
{
    private readonly Blockchain m_blockchain;
    private readonly ILoggerFactory m_loggerFactory;
    private readonly IWebSocketServerFactory m_socketServerFactory;
    private readonly HashSet<string> m_supportedSubProtocols;
    private TcpListener m_listener;
    private ILogger m_logger;
    private Task? m_task;

    public Server(
        IWebSocketServerFactory socketServerFactory,
        IWebSocketClientFactory socketClientFactory,
        ILoggerFactory loggerFactory,
        IEnumerable<string> supportedSubProtocols,
        Blockchain blockchain)
        : base(socketClientFactory, loggerFactory.CreateLogger<Client>(), new ConcurrentDictionary<string, (P2PClient, Task)>())
    {
        m_logger = loggerFactory.CreateLogger<P2PServer>();
        m_blockchain = blockchain;
        m_loggerFactory = loggerFactory;
        m_socketServerFactory = socketServerFactory;
        m_supportedSubProtocols = new HashSet<string>(supportedSubProtocols ?? Enumerable.Empty<string>());
        m_listener = null!;
        IsFullNode = true;
    }

    public EndPoint LocalEndpoint => m_listener.LocalEndpoint;
    public int Port { get; private set; }

    public void Listen(int port, CancellationToken cancellationToken)
    {
        try
        {
            m_listener = new TcpListener(IPAddress.Loopback, port);
            m_listener.Start();
            m_task = Task.Run(async () =>
            {
                while (true)
                {
                    TcpClient tcpClient = await m_listener.AcceptTcpClientAsync();

                    Stream stream = tcpClient.GetStream();
                    WebSocketHttpContext context = await m_socketServerFactory.ReadHttpHeaderFromStreamAsync(stream);

                    if (context.IsWebSocketRequest)
                    {
                        string? subProtocol = GetSubProtocol(context.WebSocketRequestedProtocols);
                        var options = new WebSocketServerOptions() { KeepAliveInterval = TimeSpan.FromSeconds(30), SubProtocol = subProtocol };
                        m_logger.LogInformation("Http header has requested an upgrade to Web Socket protocol. Negotiating Web Socket handshake");

                        WebSocket webSocket = await m_socketServerFactory.AcceptWebSocketAsync(context, options);

                        m_logger.LogInformation("Web Socket handshake response sent. Stream ready.");

                        var p2p = new P2PServer(this, m_blockchain, m_loggerFactory.CreateLogger<P2PServer>(), webSocket, "");

                        string remote = $"ws://{tcpClient.Client.RemoteEndPoint!.ToString()}";
                        if (p2p.IsFullNode)
                        {
                            remote = $"ws://{await p2p.GetServerAddress(cancellationToken)}";
                        }

                        var uri = new Uri(remote);
                        var uriString = uri.ToString();
                        p2p.IP = uriString;

                        var cancellationTokenSource = new CancellationTokenSource();

                        var task = Task.Run(async () =>
                        {
                            // this worker thread stays alive until either of the following happens:
                            // Client sends a close conection request OR
                            // An unhandled exception is thrown OR
                            // The server is disposed
                            m_logger?.LogInformation("Server: Connection opened. Reading Http header from stream");
                            try
                            {
                                await p2p.Receive(cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                m_logger?.LogError(ex, "Error on server p2p receive.");
                            }
                            finally
                            {
                                m_logger?.LogInformation("Server: Connection closed");
                                m_connections.Remove(uriString);
                                cancellationTokenSource.Cancel();

                                tcpClient.Client.Close();
                                tcpClient.Close();
                                tcpClient.Dispose();
                                webSocket.Dispose();
                            }
                        });

                        p2p.Init(cancellationTokenSource.Token);

                        await OnConnected(uri, p2p);
                        m_connections[uriString] = (p2p, task);
                    }
                    else
                    {
                        m_logger.LogInformation("Http header contains no web socket upgrade request. Ignoring");
                    }

                }
            }, cancellationToken);
        }
        catch (SocketException ex)
        {
            string message = string.Format("Error listening on port {0}. Make sure IIS or another application is not running and consuming your port.", port);
            throw new Exception(message, ex);
        }
    }

    protected override P2PClient CreateP2PClient(Client client, ILogger logger, WebSocket socket, string ip)
    {
        return new P2PServer(this, m_blockchain, m_logger, socket, ip);
    }

    private string? GetSubProtocol(IList<string> requestedSubProtocols)
    {
        foreach (string subProtocol in requestedSubProtocols)
        {
            // match the first sub protocol that we support (the client should pass the most preferable sub protocols first)
            if (m_supportedSubProtocols.Contains(subProtocol))
            {
                m_logger.LogInformation($"Http header has requested sub protocol {subProtocol} which is supported");

                return subProtocol;
            }
        }

        if (requestedSubProtocols.Count > 0)
        {
            m_logger.LogWarning($"Http header has requested the following sub protocols: {string.Join(", ", requestedSubProtocols)}. There are no supported protocols configured that match.");
        }

        return null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // safely attempt to shut down the listener
            try
            {
                if (m_listener != null)
                {
                    if (m_listener.Server != null)
                    {
                        m_listener.Server.Close();
                    }

                    m_listener.Stop();
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex.ToString());
            }

            m_logger.LogInformation("Web Server disposed");
        }
        base.Dispose(disposing);
    }

    public async ValueTask Sync(CancellationToken cancellationToken)
    {
        var ttask = base
            .GetPendingTransactions(cancellationToken)
            .AsTask()
            .ContinueWith<IEnumerable<Transaction>>(t =>
        {
            foreach (var transaction in t.Result)
                m_blockchain.PostTransaction(transaction).AsTask().Wait();
            return t.Result;
        });

        var currentLastBlock = await m_blockchain.GetLastBlock(cancellationToken);
        Block? nextBlock = await base.QueryNextBlock(currentLastBlock.Hash, cancellationToken).AsTask();
        while (nextBlock != null && currentLastBlock.Hash != nextBlock.Hash)
        {
            await m_blockchain.PostBlock(nextBlock, cancellationToken);
            currentLastBlock = nextBlock;
            nextBlock = await base.QueryNextBlock(currentLastBlock.Hash, cancellationToken).AsTask();
        }

        await ttask;
    }

    public override async ValueTask BroadcastTransation(Transaction transaction, CancellationToken cancellationToken)
    {
        await m_blockchain.PostTransaction(transaction);
        await base.BroadcastTransation(transaction, cancellationToken);
    }

    public override async ValueTask BroadcastBlock(Block block, CancellationToken cancellationToken)
    {
        await m_blockchain.PostBlock(block, cancellationToken);
        await base.BroadcastBlock(block, cancellationToken);
    }

    public override async ValueTask<IEnumerable<Transaction>> GetPendingTransactions(CancellationToken cancellationToken)
    {
        var transactions = await m_blockchain.GetPendingTransactions();
        if (transactions.Count < 128)
        {
            var otherTransactions = await base.GetPendingTransactions(cancellationToken);

            await Task.WhenAll(
                otherTransactions.Select(
                    t =>
                    {
                        t.Pending = true;
                        return m_blockchain.PostTransaction(t).AsTask();
                    }
                )
            );

            return transactions.Union(otherTransactions);
        }

        return transactions;
    }

    public override async ValueTask<Block?> QueryBlock(string blockId, CancellationToken cancellationToken)
    {
        return await m_blockchain.GetBlock(blockId);
    }

    public override async ValueTask<Block?> QueryLastBlock(CancellationToken cancellationToken)
    {
        return await m_blockchain.GetLastBlock(cancellationToken);
    }

    public override async ValueTask<Block?> QueryNextBlock(string blockId, CancellationToken cancellationToken)
    {
        return await m_blockchain.GetNextBlock(blockId);
    }

    public override async ValueTask<int> QueryDifficulty(CancellationToken cancellationToken)
    {
        await Task.Yield();
        return m_blockchain.Difficulty;
    }

    public override async ValueTask<Balance> QueryBalance(string owner, CancellationToken cancellationToken, IEnumerable<Balance>? balances = null)
    {
        return await m_blockchain.GetBalance(owner);
    }

    public override async ValueTask<TTransaction> QueryTransaction<TTransaction>(string transactionSignature, bool pending, CancellationToken cancellationToken)
    {
        var task = m_blockchain.GetTransaction(transactionSignature, pending, cancellationToken);
        return ((await task) as TTransaction)!;
    }
}