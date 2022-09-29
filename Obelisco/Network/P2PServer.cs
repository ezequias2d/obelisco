using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Obelisco.Network;

public class P2PServer : P2PClient
{
    private readonly Server m_server;
    private readonly Blockchain m_blockchain;
    public P2PServer(Server server, Blockchain blockchain, ILogger logger, WebSocket socket, string ip) : base(server, logger, socket, ip)
    {
        m_server = server;
        m_blockchain = blockchain;
    }

    #region Responses

    protected override async ValueTask GetBlockResponse(string blockId, CancellationToken cancellationToken)
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

    protected override async ValueTask GetNextBlockResponse(string blockId, CancellationToken cancellationToken)
    {
        try
        {
            var block = await m_blockchain.GetNextBlock(blockId);
            await SendBlockResponse(block, cancellationToken, forceOk: true);
        }
        catch (Exception ex)
        {
            await SendBlockResponse(null, cancellationToken, ex.Message);
        }
    }

    protected override async ValueTask GetLastBlockResponse(CancellationToken cancellationToken)
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

    protected override async ValueTask GetGenesisResponse(CancellationToken cancellationToken)
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

    protected override async ValueTask PostBlockResponse(Block block, CancellationToken cancellationToken)
    {
        try
        {
            await m_blockchain.PostBlock(block, cancellationToken);
            await SendOkResponse(cancellationToken);
        }
        catch (Exception ex)
        {
            await SendErrorResponse(ex.Message, cancellationToken);
        }
    }

    protected override async ValueTask GetPendingTransactionsResponse(CancellationToken cancellationToken)
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

    protected override async ValueTask PostTransactionResponse(Transaction transaction, CancellationToken cancellationToken)
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

    protected override async ValueTask GetDifficultyResponse(CancellationToken cancellationToken)
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

    protected override async ValueTask GetBalanceResponse(string owner, CancellationToken cancellationToken)
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

    protected override async ValueTask GetTransactionResponse(string transactionSignature, bool pending, CancellationToken cancellationToken)
    {
        try
        {
            var transaction = await m_blockchain.GetTransaction(transactionSignature, pending, cancellationToken);
            await SendResponse<TransactionResponse>(new TransactionResponse() { Transaction = transaction }, cancellationToken);
        }
        catch (Exception ex)
        {
            await SendResponse<TransactionResponse>(null, cancellationToken, ex.Message);
        }
    }

    #endregion
}