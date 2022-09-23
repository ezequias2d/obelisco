using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Obelisco.Network;

public class P2PServer : P2PClient
{
    private readonly Server m_server;
    private readonly Blockchain m_blockchain;
    public P2PServer(Server server, Blockchain blockchain, ILogger logger, WebSocket socket, Guid id) : base(server, logger, socket, id)
    {
        m_server = server;
        m_blockchain = blockchain;
    }

    protected override async ValueTask OnMessage(Message? message, CancellationToken cancellationToken)
    {
        switch (message)
        {
            case GetBlockRequest m:
                await GetBlockResponse(m.BlockID, cancellationToken);
                break;
            case GetNextBlockRequest m:
                await GetNextBlockResponse(m.BlockID, cancellationToken);
                break;
            case GetLastBlockRequest m:
                await GetLastBlockResponse(cancellationToken);
                break;
            case GetGenesisRequest m:
                await GetGenesisResponse(cancellationToken);
                break;
            case PostBlockRequest m:
                await PostBlockResponse(m.Block, cancellationToken);
                break;
            case PostTransactionRequest m:
                await PostTransactionResponse(m.Transaction, cancellationToken);
                break;
            case GetPendingTransactionsRequest m:
                await GetPendingTransactionsResponse(cancellationToken);
                break;
            case GetDifficultyRequest m:
                await GetDifficultyResponse(cancellationToken);
                break;
            case GetBalanceRequest m:
                await GetBalanceResponse(m.Owner, cancellationToken);
                break;
            default:
                await base.OnMessage(message, cancellationToken);
                break;
        }
    }

    #region Responses

    private async ValueTask GetBlockResponse(string blockId, CancellationToken cancellationToken)
    {
        BlockResponse? response = null;
        string? message = null;
        try
        {
            var block = await m_blockchain.GetBlock(blockId);
            response = new BlockResponse() { Block = block };
        }
        catch (Exception ex)
        {
            message = ex.Message;
        }
        await SendResponse(response, cancellationToken, message);
    }

    private async ValueTask GetNextBlockResponse(string blockId, CancellationToken cancellationToken)
    {
        try
        {
            var block = await m_blockchain.GetNextBlock(blockId);
            await SendBlockResponse(block, cancellationToken);
        }
        catch (Exception ex)
        {
            await SendBlockResponse(null, cancellationToken, ex.Message);
        }
    }

    private async ValueTask GetLastBlockResponse(CancellationToken cancellationToken)
    {
        try
        {
            var block = await m_blockchain.GetLastBlock(cancellationToken);
            await SendBlockResponse(block, cancellationToken);
        }
        catch (Exception ex)
        {
            await SendBlockResponse(null, cancellationToken, ex.Message);
        }
    }

    private async ValueTask GetGenesisResponse(CancellationToken cancellationToken)
    {
        try
        {
            var block = m_blockchain.GetGenesis();
            await SendBlockResponse(block, cancellationToken);
        }
        catch (Exception ex)
        {
            await SendBlockResponse(null, cancellationToken, ex.Message);
        }
    }

    private async ValueTask PostBlockResponse(Block block, CancellationToken cancellationToken)
    {
        try
        {
            await m_blockchain.PostBlock(block);
            await SendOkResponse(cancellationToken);
        }
        catch (Exception ex)
        {
            await SendErrorResponse(ex.Message, cancellationToken);
        }
    }

    private async ValueTask GetPendingTransactionsResponse(CancellationToken cancellationToken)
    {
        try
        {
            var transactions = await m_blockchain.GetPendingTransactions();
            await SendResponse(new PendingTransactionsResponse() { Transactions = transactions }, cancellationToken);
        }
        catch (Exception ex)
        {
            await SendResponse<PendingTransactionsResponse>(null, cancellationToken, ex.Message);
        }
    }

    private async ValueTask PostTransactionResponse(Transaction transaction, CancellationToken cancellationToken)
    {
        try
        {
            await m_blockchain.PostTransaction(transaction);
            await SendOkResponse(cancellationToken);
        }
        catch (Exception ex)
        {
            await SendErrorResponse(ex.Message, cancellationToken);
        }
    }

    private async ValueTask GetDifficultyResponse(CancellationToken cancellationToken)
    {
        try
        {
            var difficulty = m_blockchain.Difficulty;
            await SendResponse(new DifficultyReponse() { Difficulty = difficulty }, cancellationToken);
        }
        catch (Exception ex)
        {
            await SendResponse<DifficultyReponse>(null, cancellationToken, ex.Message);
        }
    }

    private async ValueTask GetBalanceResponse(string owner, CancellationToken cancellationToken)
    {
        try
        {
            var balance = await m_blockchain.GetBalance(owner);
            await SendResponse(new BalanceResponse() { Balance = balance }, cancellationToken);
        }
        catch (Exception ex)
        {
            await SendResponse<BalanceResponse>(null, cancellationToken, ex.Message);
        }
    }
    #endregion
}