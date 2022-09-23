using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Obelisco.Commands
{
    [Command("mine", Description = "Quits the application")]
    public class MineCommand : ICommand
    {
        private readonly State m_state;

        public MineCommand(State state)
        {
            m_state = state;
        }

        [CommandParameter(0, Description = "The validator address to mine.")]
        public string Validator { get; set; }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            if (m_state.Client == null)
                throw new InvalidOperationException();

            var token = console.GetCancellationToken();
            var transactions = m_state.Client.GetPendingTransactions(token);
            var last = await m_state.Client.Connections.First().GetLastBlock(token);

            var difficulty = m_state.Client.Connections.Select(p2p => 
            {
                var task = p2p.GetDifficulty(token).AsTask();
                task.Wait();
                return task.Result;
            }).GroupBy(n => n).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault(2);

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
                await m_state.Client.BroadcastBlock(block, token);
                console.Output.WriteLine("You mine a block!");
            }
            else
                console.Output.WriteLine("Fail to mine!");
        }
    }
}