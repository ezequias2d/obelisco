using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Obelisco;

public class PollOptionBalance : IEquatable<PollOptionBalance>
{
    public PollOptionBalance()
    {

    }

    public PollOptionBalance(PollOptionBalance balance)
    {
        Id = balance.Id;
        Votes = balance.Votes;
    }

    [JsonIgnore, Key]
    public int Id { get; set; }
    public int Index { get; set; }
    public ulong Votes { get; set; }

    public bool Equals(PollOptionBalance? other)
    {
        return other != null && Id == other.Id && Index == other.Index && Votes == other.Votes;
    }

    public override bool Equals(object? obj)
    {
        return obj is PollOptionBalance other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}