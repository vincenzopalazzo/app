using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Spectre.Console;

namespace BTCPayApp.Cli.commans;

[Command("getinfo", Description = "Return the information about ln node")]
public class GetInfo : ICommand
{
    public ValueTask ExecuteAsync(IConsole console)
    {
        // Echo the confirmation back to the terminal
        AnsiConsole.Write(new Markup("[bold white]Hello[/] [white]World![/]"));
        return default;
    }
}
