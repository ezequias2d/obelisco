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

namespace Obelisco
{
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
            : base(socketClientFactory, loggerFactory.CreateLogger<Client>(), new ConcurrentDictionary<Uri, (P2P, Task)>())
        {
            m_logger = loggerFactory.CreateLogger<P2PServer>();
            m_blockchain = blockchain;
            m_loggerFactory = loggerFactory;
            m_socketServerFactory = socketServerFactory;
            m_supportedSubProtocols = new HashSet<string>(supportedSubProtocols ?? Enumerable.Empty<string>());
            m_listener = null!;
        }

        public EndPoint LocalEndpoint => m_listener.LocalEndpoint;
        public int Port { get; private set; }

        public void Listen(int port, CancellationToken cancellationToken)
        {
            try
            {
                m_listener = new TcpListener(IPAddress.Any, port);
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

                            var p2p = new P2PServer(this, m_blockchain, m_loggerFactory.CreateLogger<P2PServer>(), webSocket, Guid.NewGuid());
                            
                            var task = Task.Run(async () => 
                            {
                                // this worker thread stays alive until either of the following happens:
                                // Client sends a close conection request OR
                                // An unhandled exception is thrown OR
                                // The server is disposed
                                m_logger.LogInformation("Server: Connection opened. Reading Http header from stream");
                                try
                                {
                                    await p2p.Receive(cancellationToken);
                                }
                                catch (Exception ex)
                                {
                                    m_logger.LogError(ex, "Error on server p2p receive.");
                                }
                                finally
                                {
                                    tcpClient.Client.Close();
                                    tcpClient.Close();
                                }
                                m_logger.LogInformation("Server: Connection closed"); 
                            }, cancellationToken);
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
                        m_task?.Wait();
                    }
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex.ToString());
                }

                m_logger.LogInformation("Web Server disposed");
            }
            Dispose(disposing);
        }
    }
}