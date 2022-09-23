using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ninja.WebSockets;

using Obelisco.Network;

namespace Obelisco;

public class Client : IDisposable
{
	private readonly IWebSocketClientFactory m_socketFactory;
	private readonly ILogger m_logger;
	protected readonly IDictionary<Uri, (P2PClient p2p, Task Task)> m_connections;
	private event EventHandler<P2PClient> m_connected;
	private int m_connectionNotifyCount = 0;

	public Client(IWebSocketClientFactory socketFactory, ILogger<Client> logger)
		: this(socketFactory, logger, new Dictionary<Uri, (P2PClient, Task)>()) { }

	protected Client(IWebSocketClientFactory socketFactory, ILogger<Client> logger, IDictionary<Uri, (P2PClient, Task)> connections)
	{
		m_socketFactory = socketFactory;
		m_logger = logger;
		m_connections = connections;
		IsFullNode = false;
	}

	public event EventHandler<P2PClient> Connected
	{
		add => m_connected += value;
		remove => m_connected -= value;
	}

	public bool IsFullNode { get; protected set; }
	public bool IsDisposed { get; protected set; }
	public Uri[] Servers => m_connections.Where(p => p.Value.p2p.IsFullNode).Select(p => p.Key).ToArray();
	public P2PClient[] Connections => m_connections.Values.Select(e => e.p2p).ToArray();

	public virtual async ValueTask Connect(Uri uri, CancellationToken cancellationToken)
	{
		if (m_connections.TryGetValue(uri, out var connection) && !connection.p2p.IsDisposed)
			throw new InvalidOperationException($"The client is already connected to '{uri}'.");

		var socket = await m_socketFactory.ConnectAsync(uri);
		var p2p = new P2PClient(this, m_logger, socket, Guid.NewGuid());

		var task = Task.Run(async () =>
		{
			await p2p.Receive(cancellationToken);
		}, cancellationToken);

		p2p.Init();
		await OnConnected(uri, p2p);
		m_connections[uri] = (p2p, task);
	}

	protected async ValueTask OnConnected(Uri uri, P2PClient e)
	{
		m_connectionNotifyCount++;

		if (e.IsFullNode)
			await BroadcastServers(new[] { uri.ToString() }, CancellationToken.None);

		m_connected?.Invoke(this, e);
	}

	public async ValueTask BroadcastTransation(Transaction transaction, CancellationToken cancellationToken)
	{
		await Task.WhenAll(
			Connections.Select(
				p2p => p2p.PostTransaction(transaction, cancellationToken).AsTask()
			)
		);
	}

	public async ValueTask BroadcastBlock(Block block, CancellationToken cancellationToken)
	{
		await Task.WhenAll(
			Connections.Select(
				p2p => p2p.PostBlock(block, cancellationToken).AsTask()
			)
		);
	}

	public async ValueTask BroadcastServers(IEnumerable<string> servers, CancellationToken cancellationToken)
	{
		await Task.WhenAll(
			Connections.Select(
				p2p => p2p.PostServers(servers, cancellationToken).AsTask()
			)
		);
	}

	public async ValueTask<IEnumerable<Transaction>> GetPendingTransactions(CancellationToken cancellationToken)
	{
		var result = await Task.WhenAll(
			Connections.Select(
				p2p => p2p.GetPendingTransaction(cancellationToken).AsTask()
			)
		);

		return new HashSet<Transaction>(result.SelectMany(l => l)).TakeWhile((_, i) => i < 256);
	}

	public async ValueTask<Block> QueryNextBlock(string blockId, CancellationToken cancellationToken)
	{
		var result = await Task.WhenAll(
			Connections.Select(
				p2p => p2p.GetNextBlock(blockId, cancellationToken).AsTask()
			)
		);

		return result.GroupBy(b => b).OrderByDescending(g => g.Count()).Select(g => g.Key).First();
	}

	public async ValueTask<Balance> QueryBalance(string owner, CancellationToken cancellationToken)
	{
		var result = await Task.WhenAll(
			Connections.Select(
				p2p => p2p.GetBalance(owner, cancellationToken).AsTask()
			)
		);

		return result.GroupBy(b => b).OrderByDescending(g => g.Count()).Select(g => g.Key).First();
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