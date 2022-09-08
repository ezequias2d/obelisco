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
        private readonly Client m_client;
        public P2PClient(Client client, ILogger logger, WebSocket socket, Guid id) : base(logger, socket, id)
        {
            m_client = client;
        }

        protected override async ValueTask OnMessage(Message? message, CancellationToken cancellationToken)
        {
            switch (message)
            {
                case GetServersRequest _:
                    await GetServersResponse(cancellationToken);
                    break;
                default:
                    await base.OnMessage(message, cancellationToken);
                    break;
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
    }
}