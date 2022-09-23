using System;

namespace Obelisco;

public class PollOption : IEquatable<PollOption>
{
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
