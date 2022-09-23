using System.Linq;
using System.Text;
using System;
using System.IO;

namespace Obelisco;

public class PollTransaction : Transaction, IEquatable<PollTransaction>
{
	public PollTransaction()
	{

	}

	public PollTransaction(string id, DateTime timestamp, string title)
	{

	}

	public string Title { get; set; }
	public string Description { get; set; }
	public PollOption[] Options { get; set; }

	public bool Equals(PollTransaction? other)
	{
		return other != null && base.Equals(other) &&
			Title == other.Title &&
			Description == other.Description &&
			Options.SequenceEqual(other.Options);
	}
	
	public override bool Equals(Transaction transaction) 
	{
		return transaction is PollTransaction pt && Equals(pt);
	}

	protected override void WriteContent(Stream stream)
	{
		stream.Write(Encoding.UTF8.GetBytes(Title));
		stream.Write(Encoding.UTF8.GetBytes(Description));

		foreach (var option in Options)
		{
			stream.Write(Encoding.UTF8.GetBytes(option.Title));
			stream.Write(Encoding.UTF8.GetBytes(option.Description));
		}

		stream.Position = 0;
	}
}
