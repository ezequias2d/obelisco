using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Obelisco.Commands
{
    [Command("mine", Description = "Mine a block for and give coins to a validator wallet.")]
    public class MineCommand : ICommand
    {
        private readonly State m_state;

        public MineCommand(State state)
        {
            m_state = state;
        }

        [CommandParameter(0, Description = "The validator address to mine.")]
        public string Validator { get; set; } = null!;

        public async ValueTask ExecuteAsync(IConsole console)
        {
            if (!m_state.GetClient(console, out var client))
                return;

            var token = console.GetCancellationToken();
            var transactions = client.GetPendingTransactions(token);
            var last = await client.QueryLastBlock(token);

            if (last == null)
            {
                await console.Error.WriteLineAsync("Fail to retrieves the last block.");
                return;
            }

            var difficulty = await client.QueryDifficulty(token);

            var block = new Block()
            {
                Version = 1,
                Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
                Transactions = new List<Transaction>(await transactions),
                Validator = Validator,
                Nonce = 0,
                Difficulty = difficulty,
                PreviousHash = last.Hash,
            };

            if (block.TryMine(difficulty, token))
            {
                await client.BroadcastBlock(block, token);
                console.Output.WriteLine("You mine a block!");
            }
            else
                console.Output.WriteLine("Fail to mine!");
        }
    }
}