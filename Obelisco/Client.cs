using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ninja.WebSockets;

namespace Obelisco
{
    public class Client : IDisposable
    {
        private readonly IWebSocketClientFactory m_socketFactory;
        private readonly ILogger m_logger;
        protected readonly IDictionary<Uri, (P2P p2p, Task Task)> m_connections;

        public Client(IWebSocketClientFactory socketFactory, ILogger<Client> logger) 
            : this(socketFactory, logger, new Dictionary<Uri, (P2P, Task)>()) { }

        protected Client(IWebSocketClientFactory socketFactory, ILogger<Client> logger, IDictionary<Uri, (P2P, Task)> connections)
        {
            m_socketFactory = socketFactory;
            m_logger = logger;
            m_connections = connections;
        }

        public bool IsDisposed { get; protected set; }
        public IEnumerable<Uri> Servers => m_connections.Keys.ToArray();

        public virtual async ValueTask Connect(Uri uri, CancellationToken cancellationToken)
        {
            if (m_connections.TryGetValue(uri, out var connection) && !connection.p2p.IsDisposed)
                throw new InvalidOperationException($"The client is already connected to '{uri}'.");

            var socket = await m_socketFactory.ConnectAsync(uri);
            var p2p = new P2PClient(this, m_logger, socket, Guid.NewGuid());;

            var task = Task.Run(async() =>
            {
                await p2p.Receive(cancellationToken);
            }, cancellationToken);

            m_connections[uri] = (p2p, task);
        }

        public void Dispose()
        {
            IsDisposed = true;
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var connection in m_connections.Values)
                {
                    connection.p2p.Dispose();
                    connection.Task.Wait();
                }
                m_connections.Clear();
            }
        }
    }
}