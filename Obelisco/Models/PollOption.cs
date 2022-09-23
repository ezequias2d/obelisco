using System;
using System.Text.Json.Serialization;

namespace Obelisco;

public class PollOption : IEquatable<PollOption>
{
	[JsonIgnore]
	public int Id { get; set; }
	public string Title { get; set; }
	public string Description { get; set; }

	public bool Equals(PollOption? other)
	{
		return other != null && Title == other.Title && Description == other.Description;
	}

	public override bool Equals(object? obj)
	{
		return obj is PollOption other && Equals(other);
	}
}
