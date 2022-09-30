using System.Text.Json;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Obelisco.Commands
{
    [Command("get balance", Description = "Get balance of a wallet.")]
    public class GetBalanceCommand : ICommand
    {
        private readonly State m_state;

        public GetBalanceCommand(State state)
        {
            m_state = state;
        }

        [CommandParameter(0, Description = "The owner of balance to request.")]
        public string Owner { get; set; } = null!;

        public async ValueTask ExecuteAsync(IConsole console)
        {
            if (!m_state.GetClient(console, out var client))
                return;

            var token = console.GetCancellationToken();
            var balance = await client.QueryBalance(Owner, token);

            await console.Output.WriteLineAsync(balance.ToString());
        }
    }
}