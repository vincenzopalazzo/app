using BTCPayApp.Core.Wallet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BTCPayApp.Cli.daemon;

public class Daemon: IDisposable
{
    public Daemon()
    {
        var host = Host.CreateDefaultBuilder();
        App = host.ConfigureLogging(builder => { })
            .ConfigureAppConfiguration(builder => { })
            .ConfigureServices(collection => { })
            .Build();
    }
    
    public IHost App { get; }
    
    public LightningNodeManager LNManager => App.Services.GetRequiredService<LightningNodeManager>();
    public OnChainWalletManager OnChainWalletManager => App.Services.GetRequiredService<OnChainWalletManager>();
    
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}