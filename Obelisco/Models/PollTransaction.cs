using System.Linq;
using System.Text;
using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Obelisco;

public class PollTransaction : Transaction, IEquatable<PollTransaction>
{
    public const int Cost = 10;
    private IList<PollOption> m_options = new List<PollOption>();

    public PollTransaction()
    {
        Options = new List<PollOption>();
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

    public PollTransaction(PollTransaction transaction) : base(transaction)
    {
        Title = transaction.Title;
        Description = transaction.Description;
        m_options = transaction.Options.Select(op => new PollOption(op)).ToList();
    }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public virtual IList<PollOption> Options { get; set; }

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

    public override bool Consume(Balance balance, BlockchainContext context, ILogger? logger = null)
    {
        if (balance.Coins >= Cost)
        {
            balance.Coins -= Cost;
            return true;
        }
        logger?.LogInformation($"[PollTransition: you need {Cost} coins to create a poll.]");
        return false;
    }
}
