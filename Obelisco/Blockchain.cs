using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;
using Nito.AsyncEx;

namespace Obelisco;

public class Blockchain
{
    private readonly ILogger m_logger;
    private readonly BlockchainContext m_context;
    private readonly AsyncReaderWriterLock m_lastBlockLock;
    private string m_lastBlockHash;
    private int m_difficulty;

    private static ThreadLocal<Random> m_random = new ThreadLocal<Random>(() => new Random());

    public Blockchain(ILogger<Blockchain> logger, BlockchainContext context)
    {
        m_logger = logger;
        m_context = context;
        m_difficulty = 2;

        Block genesis = GetGenesis();
        m_lastBlockHash = genesis.Hash;

        Block block = m_context.Blocks.Find(genesis.Hash) ?? genesis;
        if (block == genesis)
        {
            m_context.Blocks.Add(genesis);
            m_context.SaveChangesAsync();
        }

        while (block.Next != null)
        {
            block = block.Next;
            m_lastBlockHash = block.Hash;
        }

        m_lastBlockLock = new AsyncReaderWriterLock();
    }

    public int Difficulty => m_difficulty;

    public async ValueTask<Block> GetBlock(string blockId)
    {
        var block = await m_context.Blocks.FindAsync(blockId);

        if (block is null)
            throw new ArgumentOutOfRangeException("The block dont exist.");

        return block;
    }

    public async ValueTask<Block> GetNextBlock(string blockId)
    {
        var block = await m_context.Blocks.FindAsync(blockId);

        if (block is null || block.Next is null)
            throw new ArgumentOutOfRangeException("The block is the last.");

        return block.Next;
    }

    public async ValueTask<Block> GetLastBlock(CancellationToken cancellationToken)
    {
        using var _ = await m_lastBlockLock.ReaderLockAsync(cancellationToken);
        return await GetBlock(m_lastBlockHash);
    }

    public async ValueTask PostBlock(Block block)
    {
        using var _ = await m_lastBlockLock.WriterLockAsync();

        if (block.PreviousHash != m_lastBlockHash)
            throw new InvalidBlockException("The previous hash is not of the last block.");

        if (!block.IsValid(m_difficulty))
            throw new InvalidBlockException("The block is invalid.");

        foreach (var transaction in block.Transactions)
        {
            if (!await ValidateTransaction(transaction))
                throw new InvalidBlockException("One or more transactions are invalid.");
        }

        m_lastBlockHash = block.Hash;
        // add block
        var addBlock = m_context.Blocks.AddAsync(block);

        var polls = new List<PollTransaction>();
        // add complete transactions or update pending transactions to be complete
        foreach (var transaction in block.Transactions)
        {
            switch (transaction)
            {
                case PollTransaction pt:
                    var finded = m_context.PollTransactions.Find(pt.Signature);

                    if (finded != null)
                    {
                        // update transaction to complete
                        if (finded.Equals(pt))
                        {
                            if (finded.Pending == false)
                                throw new NotImplementedException("Something went wrong, you are trying to add a block with a transaction already completed!");
                            finded.Pending = true;
                            m_context.PollTransactions.Update(finded);
                        }
                        else
                            throw new NotImplementedException("Something went wrong, two different transactions with the same signature!");
                    }
                    else
                    {
                        // add transaction
                        pt.Pending = false;
                        m_context.PollTransactions.Add(pt);
                    }

                    polls.Add(pt);
                    break;
            }
        }


        // atualiza balanço
        var reward = GetReward(block);
        var balance = m_context.Balances.Find(block.Validator);
        if (balance == null)
        {
            balance = new Balance()
            {
                Owner = block.Validator,
                Coins = reward,
                Polls = polls
            };
            await m_context.Balances.AddAsync(balance);
        }
        else
        {
            balance.Coins += reward;
            foreach (var poll in polls)
                balance.Polls.Add(poll);
        }

        await addBlock;
        await m_context.SaveChangesAsync();
    }

    private async ValueTask<bool> ValidateTransaction(Transaction transaction)
    {
        var balance = await GetBalance(transaction.Sender);
        if (balance == null || balance.Coins <= 0)
            return false;
        return true;
    }

    public Block GetGenesis()
    {
        var now = 0;
        var block = new Block()
        {
            Version = 1,
            Timestamp = now,
            Transactions = new List<Transaction>(),
            Validator = "=",
            PreviousHash = null!,
            Difficulty = 0
        };
        block.Hash = block.CalculateHash();

        return block;
    }

    public async ValueTask<Balance> GetBalance(string ownerId)
    {
        var balance = await m_context.Balances.FindAsync(ownerId);
        return balance ?? new Balance() { Owner = ownerId, Coins = 0 };
    }

    public async ValueTask PostTransaction(Transaction transaction)
    {
        if (await ValidateTransaction(transaction))
            throw new InvalidTransactionException("The sender must has coins to send mensage.");

        switch (transaction)
        {
            case PollTransaction pt:
                pt.Pending = true;
                await m_context.PollTransactions.AddAsync(pt);
                break;
            default:
                throw new NotImplementedException($"The transaction of type {transaction.GetType().Name} lacks to be implemented.");
        }
        await m_context.SaveChangesAsync(CancellationToken.None);
    }

    private int GetReward(Block block)
    {
        var fee = block.Transactions.Count;
        return 10 + (fee / 4);
    }

    public async ValueTask<IEnumerable<Transaction>> GetPendingTransactions()
    {
        return m_context.PollTransactions
            .Where(pt => pt.Pending)
            .ToList()
            .TakeWhile((t, i) => i < 256);
    }
}