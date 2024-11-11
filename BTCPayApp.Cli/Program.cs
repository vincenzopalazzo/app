// BTCPayApp command line interface

using CliFx;
using Microsoft.Extensions.DependencyInjection;

namespace BTCPayApp.Cli;

public class Cli
{
    static async Task<int> Main(string[] args) =>
        await new CliApplicationBuilder()
            .AddCommandsFromThisAssembly()
            .UseTypeActivator(commandTypes =>
            {
                // We use Microsoft.Extensions.DependencyInjection for injecting dependencies in commands
                var services = new ServiceCollection();

                // Register all commands as transient services
                foreach (var commandType in commandTypes)
                    services.AddTransient(commandType);

                return services.BuildServiceProvider();
            })
            .Build()
            .RunAsync();
}
