using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Obelisco.Commands
{
	[Command("quit", Description = "Quits the application")]
	public class QuitCommand : ICommand
	{
		private readonly BlockchainContext m_context;
		private readonly ICliApplicationLifetime m_applicationLifetime;
		private readonly State m_state;

		public QuitCommand(
			BlockchainContext context,
			ICliApplicationLifetime applicationLifetime,
			State state)
		{
			m_applicationLifetime = applicationLifetime;
			m_context = context;
			m_state = state;
		}

		public async ValueTask ExecuteAsync(IConsole console)
		{
			await m_context.SaveChangesAsync();

			if (m_state.TryGetClient(console, out var client))
				client.Dispose();

			m_applicationLifetime.RequestStop();
		}
	}
}