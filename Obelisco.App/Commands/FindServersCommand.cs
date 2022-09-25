using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Obelisco.Commands
{
    [Command("find servers", Description = "Find servers and try to connect.")]
    public class FindServersCommand : ICommand
    {
        private readonly State m_state;

        public FindServersCommand(State state)
        {
            m_state = state;
        }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            if (!m_state.TryGetClient(console, out var client))
                return;

            var token = console.GetCancellationToken();
            var tasks = client.Connections.Select(c => c.GetServers(token).AsTask()).ToArray();
            await Task.WhenAll(tasks);

            var set = new HashSet<string>(tasks.SelectMany(t => t.Result));

            await console.Output.WriteLineAsync($"Result{{{set.Count}}}:");
            foreach (var server in set)
                await console.Output.WriteLineAsync(server);

            foreach (var server in set)
            {
                await console.Output.WriteLineAsync($"Try connect to {server}");
                if (Uri.TryCreate(server, UriKind.RelativeOrAbsolute, out var uri))
                {
                    const int timeout = 5;
                    var source = new CancellationTokenSource();
                    var task = client.Connect(uri, source.Token).AsTask();
                    if (!task.Wait(timeout * 1000))
                    {
                        source.Cancel();
                        await console.Output.WriteLineAsync($"Timeout({timeout} seconds) when try connect to {uri}");
                    }
                    else
                    {
                        await console.Output.WriteLineAsync($"Connected to {uri}");
                    }
                }
                else
                    await console.Output.WriteLineAsync($"Cannot create uri from {server}");
            }
        }
    }
}