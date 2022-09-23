using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Obelisco;

public class Balance : IEquatable<Balance>
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string Owner { get; set; } = null!;
    public int Coins { get; set; }
    public virtual ICollection<PollTransaction> Polls { get; set; }

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
        return JsonSerializer.Serialize<Balance>(this, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}