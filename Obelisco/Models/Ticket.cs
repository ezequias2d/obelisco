namespace Obelisco;

public class Ticket
{
	public Ticket()
	{
		
	}
	
	public Ticket(string owner, PollTransaction poll)
	{
		Owner = owner;
		Poll = poll.Signature;
	}
	
	public string Owner { get; set; } = null!;
	
	public string Poll { get; set; } = null!;
}
