using Microsoft.Extensions.Logging;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Obelisco.Commands
{
    [Command("find servers", Description = "Find servers and try to connect.")]
    public class FindServersCommand : ICommand
    {
        private readonly ILogger m_logger;
        private readonly IEnumerable<string> SupportedSubProtocols = new string[] { "obelisco" };
        private readonly ICliApplicationLifetime m_applicationLifetime;
        private readonly State m_state;

        public FindServersCommand(
            State state,
            ILogger<ConnectCommand> logger,
            ICliApplicationLifetime applicationLifetime)
        {
            m_state = state;

            m_logger = logger;
            m_applicationLifetime = applicationLifetime;
        }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            if (m_state.Client == null)
            {
                console.Output.WriteLine("You must init a server or client before connect.");
                return;
            }

            var token = console.GetCancellationToken();
            var tasks = m_state.Client.Connections.Select(c => c.GetServers(token).AsTask()).ToArray();
            Task.WaitAll(tasks);
            
            var set = new HashSet<string>(tasks.SelectMany(t => t.Result));
            
            console.Output.WriteLine($"Result{{{set.Count}}}:");
            foreach(var server in set)
                console.Output.WriteLine(server);

            foreach(var server in set)
            {
                console.Output.WriteLine($"Try connect to {server}");
                if (Uri.TryCreate(server, UriKind.RelativeOrAbsolute, out var uri))
                {
                    const int timeout = 5;
                    var source = new CancellationTokenSource();
                    var task = m_state.Client.Connect(uri, source.Token).AsTask();
                    if (!task.Wait(timeout * 1000))
                    {
                        source.Cancel();
                        console.Output.WriteLine($"Timeout({timeout} seconds) when try connect to {uri}");
                    }
                    else
                    {
                        console.Output.WriteLine($"Connected to {uri}");
                    }
                }
                else
                    console.Output.WriteLine($"Cannot create uri from {server}");
            }
        }
    }
}