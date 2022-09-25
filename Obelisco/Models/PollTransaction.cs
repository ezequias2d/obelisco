using System.Linq;
using System.Text;
using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Obelisco;

public class PollTransaction : Transaction, IEquatable<PollTransaction>
{
	public const int Cost = 10;
	public PollTransaction()
	{

	}

	public PollTransaction(long nonce, DateTimeOffset timestamp, string title, string description, PollOption[] options) : base(nonce, timestamp)
	{
		Title = title;
		Description = description;
		Options = options;
		
		// set indexes
		for (var i = 0; i < options.Length; i++)
			options[i].Index = i;
	}

	public string Title { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public virtual IList<PollOption> Options { get; set; } = new List<PollOption>();

	public bool Equals(PollTransaction? other)
	{
		return other != null && base.Equals(other) &&
			Title == other.Title &&
			Description == other.Description &&
			Options.SequenceEqual(other.Options);
	}
	
	public override bool Equals(Transaction? transaction) 
	{
		return transaction != null && transaction is PollTransaction pt && Equals(pt);
	}

	protected override void WriteContent(Stream stream)
	{
		stream.Write(Encoding.UTF8.GetBytes(Title));
		stream.Write(Encoding.UTF8.GetBytes(Description));

		foreach (var option in Options.OrderBy(o => o.Index))
		{
			stream.Write(Encoding.UTF8.GetBytes(option.Title));
			stream.Write(Encoding.UTF8.GetBytes(option.Description));
		}

		stream.Position = 0;
	}

	public override bool Validate(BlockchainContext context, ILogger? logger = null)
	{
		var balance = context.Balances.Find(Sender);
		if (balance == null)
		{
			logger?.LogInformation("[PollTransition was created by someone without a balance.]");
			return false;
		}
		
		if (balance.Coins < Cost)
			return false;
		return true;
	}
	
	public override bool Consume(BlockchainContext context) 
	{
		var balance = context.Balances.Find(Sender);
		if (balance == null)
			return false;
			
		if (balance.Coins >= Cost)
		{
			balance.Coins -= Cost;
			context.Balances.Update(balance);
			return true;
		}
		return false;
	}
}
