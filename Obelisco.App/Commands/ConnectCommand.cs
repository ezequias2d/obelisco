using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Obelisco.Commands
{
    [Command("connect", Description = "Create and start the server.")]
    public class ConnectCommand : ICommand
    {
        private readonly IEnumerable<string> SupportedSubProtocols = new string[] { "obelisco" };
        private readonly State m_state;

        public ConnectCommand(
            State state,
            ICliApplicationLifetime applicationLifetime)
        {
            m_state = state;
            Uri = "ws://127.0.0.1:1234/obelisco";
        }

        [CommandParameter(0, Description = "The uri of server to connect.")]
        public string Uri { get; set; }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            if (!m_state.TryGetClient(console, out var client))
            {
                console.Output.WriteLine("You must init a server or client before connect.");
                return;
            }
            var uri = new Uri("ws://" + Uri);
            await client.Connect(uri, CancellationToken.None);

            var str = $"Connected to {Uri}";
            console.Output.WriteLine(str);
        }
    }
}