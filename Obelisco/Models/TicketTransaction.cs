using System;
using System.IO;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Obelisco;

public class TicketTransaction : Transaction
{
    public const int Cost = 1;
    public TicketTransaction()
    {

    }

    public TicketTransaction(long nonce, string owner, string poll, DateTimeOffset timestamp) : base(nonce, timestamp)
    {
        Owner = owner;
        Poll = poll;
    }

    /// <summary>
    /// Ticket receiver.
    /// </summary>
    /// <value>The public Key identifier of owner of this ticket.</value>
    public string Owner { get; set; } = null!;

    /// <summary>
    /// Poll Source.
    /// </summary>
    /// <value>The poll that this ticket is valid.</value>
    public string Poll { get; set; } = null!;

    /// <summary>
    /// Metadata(not part of the blockchain)
    /// </summary>
    [JsonIgnore]
    public bool Used { get; set; } = false;

    public override bool Consume(BlockchainContext context)
    {
        var poll = context.PollTransactions.Find(Poll);

        if (poll == null || Sender != poll.Sender)
            return false;

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

    public override bool Validate(BlockchainContext context, ILogger? logger = null)
    {
        var poll = context.PollTransactions.Find(Poll);

        if (poll == null)
        {
            logger?.LogInformation($"[TicketTransaction was created with invalid poll: '{Poll}'.]");
            return false;
        }

        if (Sender != poll.Sender)
        {
            logger?.LogInformation("[TicketTransaction only the poll owner can create a transaction for that poll.]");
            return false;
        }

        var balance = context.Balances.Find(Sender);
        if (balance == null)
        {
            logger?.LogInformation("[TicketTransaction was created by someone without a balance.]");
            return false;
        }

        if (balance.Coins < Cost)
        {
            logger?.LogInformation($"[TicketTransaction you must has {Cost} coin to create a ticket.]");
            return false;
        }

        return true;
    }

    protected override void WriteContent(Stream stream)
    {
        var ownerId = Convert.FromBase64String(Owner);
        var pollId = Convert.FromBase64String(Poll);

        stream.Write(ownerId);
        stream.Write(pollId);
    }
}
