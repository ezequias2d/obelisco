using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Obelisco
{
    public class P2PClient : P2P
    {
        private const int REF_SERVERS = 10;
        private readonly Client m_client;
        private event EventHandler<PostServersRequest> m_postServers; 
        public P2PClient(Client client, ILogger logger, WebSocket socket, Guid id) : base(logger, socket, id)
        {
            m_client = client;
            Servers += OnServers;
        }

        public event EventHandler<PostServersRequest> Servers 
        {
             add => m_postServers += value; 
             remove => m_postServers -= value;
        }

        private void OnServers(object sender, PostServersRequest e)
        {
            var servers = m_client.Servers;
            var count = servers.Length;
            if (count < REF_SERVERS)
            {
                var task = Task.WhenAll(ConnectServers(servers.Select(u => u.ToString()), e.Servers, CancellationToken.None));
                task.Wait();
            }
        }

        private IEnumerable<Task> ConnectServers(IEnumerable<string> currentServers, IEnumerable<string> newServers, CancellationToken cancellationToken)
        {
            foreach (var server in newServers)
            {
                if (!currentServers.Contains(server) && Uri.TryCreate(server, UriKind.Absolute, out var uri))
                    yield return m_client.Connect(uri, cancellationToken).AsTask();
            }
        }

        protected override async ValueTask OnMessage(Message? message, CancellationToken cancellationToken)
        {
            switch (message)
            {
                case GetServersRequest _:
                    await GetServersResponse(cancellationToken);
                    break;
                case PostServersRequest m:
                    await SendOkResponse(cancellationToken);
                    m_postServers?.Invoke(this, m);
                    break;
                default:
                    await base.OnMessage(message, cancellationToken);
                    break;
            }
        }

        protected override async ValueTask GetNodeTypeResponse(CancellationToken cancellationToken)
        {
            try
            {
                await SendResponse(new NodeTypeResponse() { IsFullNode = m_client.IsFullNode }, cancellationToken);
            }
            catch (Exception ex)
            {
                await SendResponse<NodeTypeResponse>(null, cancellationToken, ex.Message);
            }
        }

        protected override async ValueTask GetServerAddressResponse(CancellationToken cancellationToken)
        {
            try
            {
                var uri = m_client is Server server ? $"{server.LocalEndpoint}" : string.Empty;
                await SendResponse(new ServerAddressResponse() { Uri = uri }, cancellationToken);
            }
            catch (Exception ex)
            {
                await SendResponse<ServerAddressResponse>(null, cancellationToken, ex.Message);
            }
        }

        private async ValueTask GetServersResponse(CancellationToken cancellationToken)
        {
            try
            {
                var servers = m_client.Servers;
                await SendResponse(new ServersResponse() { Servers = servers.Select(s => s.ToString()) }, cancellationToken);
            }
            catch (Exception ex)
            {
                await SendResponse<ServersResponse>(null, cancellationToken, ex.Message);
            }
        }

        public async ValueTask<IEnumerable<string>> GetServers(CancellationToken cancellationToken)
        {
            await SendMessage(new GetServersRequest(), cancellationToken);
            return WaitResponse<ServersResponse>(cancellationToken).Servers;
        }

        public async ValueTask<Block> GetBlock(string blockId, CancellationToken cancellationToken)
        {
            await SendMessage(new GetBlockRequest() { BlockID = blockId }, cancellationToken);
            return WaitResponse<BlockResponse>(cancellationToken).Block;
        }

        public async ValueTask<Block> GetNextBlock(string blockId, CancellationToken cancellationToken)
        {
            await SendMessage(new GetNextBlockRequest() { BlockID = blockId }, cancellationToken);
            return WaitResponse<BlockResponse>(cancellationToken).Block;
        }

        public async ValueTask<Block> GetLastBlock(CancellationToken cancellationToken)
        {
            await SendMessage(new GetLastBlockRequest(), cancellationToken);
            return WaitResponse<BlockResponse>(cancellationToken).Block;
        }

        public async ValueTask<Block> GetGenesis(CancellationToken cancellationToken)
        {
            await SendMessage(new GetGenesisRequest(), cancellationToken);
            return WaitResponse<BlockResponse>(cancellationToken).Block;
        }

        public async ValueTask<IEnumerable<PendingTransaction>> GetPendingTransaction(CancellationToken cancellationToken)
        {
            await SendMessage(new GetPendingTransactionsRequest(), cancellationToken);
            return WaitResponse<PendingTransactionsResponse>(cancellationToken).Transactions;
        }

        public async ValueTask PostBlock(Block block, CancellationToken cancellationToken)
        {
            await SendMessage(new PostBlockRequest() { Block = block }, cancellationToken);
            WaitResponse<Response>(cancellationToken);
        }

        public async ValueTask PostTransaction(Transaction transaction, CancellationToken cancellationToken)
        {
            await SendMessage(new PostTransactionRequest() { Transaction = new PendingTransaction(transaction) }, cancellationToken);
            WaitResponse<Response>(cancellationToken);
        }

        public async ValueTask PostServers(IEnumerable<string> servers, CancellationToken cancellationToken)
        {
            await SendMessage(new PostServersRequest() { Servers = servers }, cancellationToken);
            WaitResponse<Response>(cancellationToken);
        }

        public async ValueTask<int> GetDifficulty(CancellationToken cancellationToken)
        {
            await SendMessage(new GetDifficultyRequest(), cancellationToken);
            return WaitResponse<DifficultyReponse>(cancellationToken).Difficulty;
        }

        public async ValueTask<Balance> GetBalance(string owner, CancellationToken cancellationToken)
        {
            await SendMessage(new GetBalanceRequest() { Owner = owner }, cancellationToken);
            return WaitResponse<BalanceResponse>(cancellationToken).Balance;
        }
    }
}