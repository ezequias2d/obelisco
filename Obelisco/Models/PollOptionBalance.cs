using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Obelisco;

public class PollOptionBalance : IEquatable<PollOptionBalance>
{
	[JsonIgnore, Key, ForeignKey("PollOption")]
	public int Id { get; set; }
	public PollOption PollOption { get; set; } = null!;
	public ulong Votes { get; set; }

	public bool Equals(PollOptionBalance? other)
	{
		return other != null && PollOption.Equals(other.PollOption) && Votes == other.Votes;
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