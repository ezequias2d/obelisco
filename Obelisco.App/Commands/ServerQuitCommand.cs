using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Obelisco.App.Commands;

[Command("server quit", Description = "Quits server mode.")]
public class ServerQuitCommand : ICommand
{
    private readonly BlockchainContext m_context;
    private readonly ICliApplicationLifetime m_applicationLifetime;
    private readonly State m_state;

    public ServerQuitCommand(
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

        if (m_state.GetServer(console, out var server))
        {
            server.Dispose();
            m_state.Server = null;
            m_state.Client = null;
            await console.Error.WriteLineAsync("Server mode off!");
        }
        else
            await console.Error.WriteLineAsync("Sever mode is not on!");
    }
}