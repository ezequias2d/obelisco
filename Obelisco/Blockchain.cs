using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;
using Nito.AsyncEx;
using Microsoft.EntityFrameworkCore;
using System.Net.WebSockets;

namespace Obelisco
{
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

            // remove from pending transactions and add to complete transations
            var transactionIds = block.Transactions.Select((t) => t.Id);
            var transactionsProcessed = await m_context.PendingTransactions.Where((t) => transactionIds.Contains(t.Id)).ToListAsync();
            m_context.PendingTransactions.RemoveRange(transactionsProcessed);

            // atualiza balanço
            var reward = GetReward(block);
            var balance = m_context.Balances.Find(block.Validator);
            if (balance == null)
            {
                balance = new Balance() 
                {
                    Owner = block.Validator,
                    Coins = reward
                };
                await m_context.Balances.AddAsync(balance);
            }
            else
            {
                balance.Coins += reward;
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
            return await m_context.Balances.FindAsync(ownerId);
        }

        public async ValueTask PostTransaction(PendingTransaction transaction)
        {
            if (await ValidateTransaction(transaction))
                throw new InvalidTransactionException("The sender must has coins to send mensage.");

            m_context.PendingTransactions.Add(transaction);
        }

        private int GetReward(Block block)
        {
            var fee = block.Transactions.Count;
            return 10 + (fee / 4);
        }

        public async ValueTask<IEnumerable<PendingTransaction>> GetPendingTransactions()
        {
            return m_context.PendingTransactions
                .ToList()
                .TakeWhile((t, i) => i < 256);
        }
    }
}
