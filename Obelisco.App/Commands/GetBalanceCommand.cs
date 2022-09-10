using System.Text.Json;
using Microsoft.Extensions.Logging;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Obelisco.Commands
{
    [Command("get balance", Description = "send message")]
    public class GetBalanceCommand : ICommand
    {
        private readonly ILogger m_logger;
        private readonly State m_state;

        public GetBalanceCommand(
            State state,
            ILogger<QuitCommand> logger)
        {
            m_logger = logger;
            m_state = state;
        }

        [CommandParameter(0, Description = "The owner of balance to request.")]
        public string Owner { get; set; }

        public async ValueTask ExecuteAsync(IConsole console)
        {
             if (m_state.Client == null)
            {
                console.Output.WriteLine("You must init a server or client before connect.");
                return;
            }

            var token = console.GetCancellationToken();
            var balances = m_state.Client.Connections.Select(c => c.GetBalance(Owner, token).AsTask()).ToArray();
            Task.WaitAll(balances);

            var balance = balances.Select(t => t.Result).GroupBy(b => b).OrderByDescending(g => g.Count()).Select(g => g.Key).First();
            
            var str = JsonSerializer.Serialize<Balance>(balance, new JsonSerializerOptions() { WriteIndented = true });
            await console.Output.WriteLineAsync(str);
        }
    }
}