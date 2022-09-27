using System.Text;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Obelisco.App.Commands;


[Command("get public key", Description = "Show public key.")]
public class GetPublicKeyCommand : ICommand
{
    public GetPublicKeyCommand()
    {
    }

    [CommandParameter(0, Description = "Password.")]
    public string Password { get; set; } = null!;

    [CommandParameter(1, Description = "Private key file.")]
    public string AccountFile { get; set; } = null!;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (!console.Login(Password, AccountFile, out var account))
            return;

        await console.Output.WriteLineAsync($"Your public key is {Convert.ToBase64String(account.PublicKey)}");
    }
}
