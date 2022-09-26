using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Obelisco;

public class PollOption : IEquatable<PollOption>
{
    [JsonIgnore, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int Index { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;

    [JsonIgnore]
    public virtual PollOptionBalance Balance { get; set; } = null!;

    [JsonIgnore]
    public virtual string PollId { get; set; }

    [JsonIgnore]
    public virtual PollTransaction Poll { get; set; }

    public bool Equals(PollOption? other)
    {
        return other != null && Title == other.Title && Description == other.Description;
    }

    public override bool Equals(object? obj)
    {
        return obj is PollOption other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override string? ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}
