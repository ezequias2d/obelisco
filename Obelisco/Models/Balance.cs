using System.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Obelisco;

public class Balance : IEquatable<Balance>
{
    public Balance()
    {

    }

    public Balance(string owner)
    {
        Owner = owner;
        Coins = 0;
        Nonce = 0;
        Polls = new List<PollTransaction>();
        UnusedTickets = new List<TicketTransaction>();
        UsedTickets = new List<TicketTransaction>();
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string Owner { get; set; } = string.Empty;
    public int Coins { get; set; }
    public long Nonce { get; set; }

    [NotMapped]
    public virtual IList<TicketTransaction> UnusedTickets { get; set; } = new List<TicketTransaction>();

    [NotMapped]
    public virtual IList<TicketTransaction> UsedTickets { get; set; } = new List<TicketTransaction>();

    [NotMapped]
    public virtual IList<PollTransaction> Polls { get; set; } = new List<PollTransaction>();

    [NotMapped]
    public virtual IList<PollBalance> PollBalances { get; set; } = new List<PollBalance>();

    public bool Equals(Balance? other)
    {
        return other != null && Owner == other.Owner && Coins == other.Coins;
    }

    public override bool Equals(object? obj)
    {
        return obj is Balance other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Owner, Coins);
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize<Balance>(this, new JsonSerializerOptions() { WriteIndented = true });
    }

    public Balance GetSnapshot()
    {
        return new(Owner)
        {
            Coins = Coins,
            Nonce = Nonce,
            UnusedTickets = new List<TicketTransaction>(UnusedTickets.Select(t => new TicketTransaction(t))),
            UsedTickets = new List<TicketTransaction>(UsedTickets.Select(t => new TicketTransaction(t))),
            Polls = new List<PollTransaction>(Polls.Select(t => new PollTransaction(t))),
            PollBalances = new List<PollBalance>(PollBalances.Select(b => new PollBalance(b)))
        };
    }
}