using System.Linq;
using System;
using System.IO;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Obelisco;

public class VoteTransaction : Transaction
{
	public VoteTransaction()
	{
	}

	public VoteTransaction(long nonce, DateTimeOffset timestamp, int option) : base(nonce, timestamp)
	{
		Option = option;
	}
	
	public string Poll { get; set; } = string.Empty;
	public int Option { get; set; }

	protected override void WriteContent(Stream stream)
	{
		// write poll id(signature)
		stream.Write(Convert.FromBase64String(Poll));
		
		// write option index
		Span<byte> bytes = stackalloc byte[4];
		Debug.Assert(BitConverter.TryWriteBytes(bytes, Option));
		stream.Write(bytes);
	}
	
	public override bool Validate(BlockchainContext context, ILogger? logger = null)
	{
		var balance = context.Balances.Find(Sender);
		if (balance == null)
		{
			logger?.LogInformation("[VoteTransition was created by someone without a balance.]");
			return false;
		}
		
		foreach (var ticket in balance.Tickets)
		{
			if (ticket.Poll == Poll)
			{
				var pollTransaction = context.PollTransactions.Find(Poll);
				if (pollTransaction == null)
				{
					logger?.LogInformation($"[VoteTransition uses a poll that cannot be retrieved.]");
					return false;
				}
				PollOption? option = pollTransaction.Options.FirstOrDefault(op => op != null && op.Index == Option, null);
				
				if (option == null)
				{
					logger?.LogInformation($"[VoteTransition is invalid because option of index {Option} don't exist.]");
					return false;
				}
				
				return true;
			}
		}
		return false;
	}
	
	public override bool Consume(BlockchainContext context)
	{
		var balance = context.Balances.Find(Sender);
		if (balance == null)
			return false;
		
		foreach (var ticket in balance.Tickets)
		{
			if (ticket.Poll == Poll)
			{
				var pollTransaction = context.PollTransactions.Find(Poll);
				if (pollTransaction == null)
					return false;
				PollOption? option = pollTransaction.Options.FirstOrDefault(op => op != null && op.Index == Option, null);
					
				if (option == null)
					return false;

				// consume ticket
				balance.Tickets.Remove(ticket);
				context.Balances.Update(balance);
				return true;
			}
			return true;
		}
		return false;
	}
}
