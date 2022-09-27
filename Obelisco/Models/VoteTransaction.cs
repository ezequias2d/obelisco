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

    public VoteTransaction(long nonce, string poll, int option, DateTimeOffset timestamp) : base(nonce, timestamp)
    {
        Poll = poll;
        Option = option;
    }

    public VoteTransaction(VoteTransaction transaction) : base(transaction)
    {
        Poll = transaction.Poll;
        Option = transaction.Option;
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

    public override bool Consume(Balance balance, BlockchainContext context, ILogger? logger = null)
    {
        foreach (var ticket in balance.UnusedTickets)
        {
            if (ticket.Poll == Poll)
            {
                var pollTransaction = context.PollTransactions.Find(Poll);
                if (pollTransaction == null)
                {
                    logger?.LogInformation($"[VoteTransaction uses a poll that cannot be retrieved.]");
                    return false;
                }

                if (pollTransaction.Pending)
                {
                    logger?.LogInformation($"[VoteTransaction uses a poll that is in pending state.]");
                    return false;
                }

                PollOption? option = pollTransaction.Options.FirstOrDefault(op => op != null && op.Index == Option, null);

                if (option == null)
                {
                    logger?.LogInformation($"[VoteTransaction is invalid because option of index {Option} don't exist.]");
                    return false;
                }

                // consume ticket
                ticket.Used = true;
                balance.UnusedTickets.Remove(ticket);
                balance.UsedTickets.Add(ticket);

                return true;
            }
        }
        return false;
    }
}
