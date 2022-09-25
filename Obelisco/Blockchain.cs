using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;
using Nito.AsyncEx;
using Microsoft.EntityFrameworkCore;

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

		#region Validate Transactions of Block
		var transactionsAreValid = true;
		foreach (var transaction in block.Transactions)
		{
			var t = m_context.FindTransaction(transaction.Signature);
			if (t != null && !t.Pending)
				throw new InvalidBlockException($"Transaction {transaction.Signature} are in blockchain.");
			
			transactionsAreValid &= transaction.Validate(m_context, m_logger);
		}

		if (!transactionsAreValid)
			throw new InvalidBlockException("One or more transactions are invalid.");
		#endregion

		m_lastBlockHash = block.Hash;
		// add block
		var addBlock = m_context.Blocks.AddAsync(block);

		// add complete transactions or update pending transactions to be complete
		foreach (var transaction in block.Transactions)
		{
			var finded = m_context.FindTransaction(transaction.Signature);
			
				
			if (finded != null)
			{
				// update transaction to complete
				
				if (finded.GetType() != transaction.GetType())
					throw new InvalidTransactionException("Something went wrong, transaction types are wrong!");
					
				if (finded.Pending == false)
					throw new NotImplementedException("Something went wrong, you are trying to add a block with a transaction already completed!");
				
				finded.Pending = false;
				m_context.UpdateTransaction(finded);
			}
			else
			{
				// add transaction
				transaction.Pending = false;
				await m_context.AddTransactionAsync(transaction);
			}

			switch (transaction) 
			{
				case PollTransaction pt:
					var balance = m_context.Balances.Find(transaction.Sender);
					if (balance == null)
						throw new NotImplementedException();
					balance.Polls.Add(pt);
					m_context.Balances.Update(balance);
					break;
			}
			break;
		}

		// reward
		var reward = GetReward(block);
		var validatorBalance = m_context.Balances.Find(block.Validator);
		if (validatorBalance == null)
		{
			validatorBalance = new Balance()
			{
				Owner = block.Validator,
				Coins = reward,
				Polls = new List<PollTransaction>()
			};
			await m_context.Balances.AddAsync(validatorBalance);
		}
		else
			validatorBalance.Coins += reward;

		await addBlock;
		await m_context.SaveChangesAsync();
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
		if (balance != null)
			balance.Polls = m_context.PollTransactions.Where(t => t.Sender == ownerId).ToList();
		return balance ?? new Balance() { Owner = ownerId, Coins = 0, Polls = new List<PollTransaction>() };
	}

	public async ValueTask PostTransaction(Transaction transaction)
	{
		if (!transaction.Validate(m_context, m_logger))
			throw new InvalidTransactionException("The sender must has capacity to post transaction.");
		
		var finded = m_context.FindTransaction(transaction);
		if (finded != null && finded.Pending)
			throw new InvalidTransactionException("The transaction has already been completed.");
			
		if (finded == null)
		{
			transaction.Pending = true;
			await m_context.AddAsync(transaction);
		}

		await m_context.SaveChangesAsync(CancellationToken.None);
	}

	private int GetReward(Block block)
	{
		var fee = block.Transactions.Count;
		return 10 + (fee / 4);
	}

	public async ValueTask<ICollection<Transaction>> GetPendingTransactions()
	{
		var pending = await m_context.PollTransactions
			.Where(pt => pt.Pending)
			.ToListAsync();
			
		return pending.TakeWhile((t, i) => i < 256)
			.Select<PollTransaction, Transaction>(t => t)
			.ToList();
	}
}