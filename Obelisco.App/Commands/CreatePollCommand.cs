using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Obelisco.App.Commands;

[Command("create poll", Description = "send message")]
public class CreatePollCommand : ICommand
{
    private readonly ICliApplicationLifetime m_applicationLifetime;
    private readonly State m_state;

    public CreatePollCommand(
        State state,
        ICliApplicationLifetime applicationLifetime)
    {
        m_applicationLifetime = applicationLifetime;
        m_state = state;
    }

    [CommandParameter(0, Description = "The account password to use encrypt private key.")]
    public string Password { get; set; } = null!;

    [CommandParameter(1, Description = "Private key file.")]
    public string KeyFile { get; set; } = null!;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var token = console.GetCancellationToken();

        if (!m_state.TryGetClient(console, out var client))
            return;

        if (!console.Login(Password, KeyFile, out var account))
            return;

        var balanceTask = client.QueryBalance(Convert.ToBase64String(account.PublicKey), token);

        if (!console.ReadString("Title", out var title))
            return;

        if (!console.ReadString("Description", out var description))
            return;

        var options = new List<PollOption>();
        while (options.Count < 2 || console.ConfirmMessage("Add more?"))
        {
            if (!console.ReadString("Option Title", out var optionTitle))
                return;

            if (!console.ReadString("Option Description", out var optionDescription))
                return;

            options.Add(new PollOption() { Title = optionTitle, Description = optionDescription });
        }


        var balance = await balanceTask;
        var poll = new PollTransaction(balance.Nonce + 1, DateTimeOffset.Now, title, description, options.ToArray());

        poll.Sign(account);

        await client.BroadcastTransation(poll, token);
    }
}