﻿using BTCPayApp.Core.Attempt2;
using BTCPayApp.Core.Contracts;
using BTCPayApp.Core.Data;
using BTCPayServer.Lightning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NBitcoin;
using org.ldk.structs;
using LightningPayment = BTCPayApp.Core.Data.LightningPayment;

namespace BTCPayApp.Core.LDK;

public class LDKNode : IAsyncDisposable, IHostedService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly BTCPayConnectionManager _connectionManager;
    private readonly ILogger _logger;
    private readonly ChannelManager _channelManager;
    private readonly IConfigProvider _configProvider;
    private readonly OnChainWalletManager _onChainWalletManager;

    public LDKNode(
        IDbContextFactory<AppDbContext> dbContextFactory,
        BTCPayConnectionManager connectionManager,
        IServiceProvider serviceProvider, 
        LDKWalletLogger logger, 
        ChannelManager channelManager,
        IConfigProvider configProvider,
        OnChainWalletManager onChainWalletManager)
    {
        _dbContextFactory = dbContextFactory;
        _connectionManager = connectionManager;
        _logger = logger;
        _channelManager = channelManager;
        _configProvider = configProvider;
        _onChainWalletManager = onChainWalletManager;
        ServiceProvider = serviceProvider;
    }

    public PubKey NodeId => new(_channelManager.get_our_node_id());


    public IServiceProvider ServiceProvider { get; }
    private TaskCompletionSource? _started;
    private static readonly SemaphoreSlim Semaphore = new(1);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        bool exists;
        try
        {
            await Semaphore.WaitAsync(cancellationToken);
            exists = _started is not null;
            _started ??= new TaskCompletionSource();
        }
        finally
        {
            Semaphore.Release();
        }

        if (exists)
        {
            await _started.Task;
            return;
        }


        var services = ServiceProvider.GetServices<IScopedHostedService>();

        _logger.LogInformation("Starting LDKNode services");
        foreach (var service in services)
        {
            await service.StartAsync(cancellationToken);
        }

        _started.SetResult();
        _logger.LogInformation("LDKNode started");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        bool exists;
        try
        {
            await Semaphore.WaitAsync(cancellationToken);
            exists = _started is not null;
        }
        finally
        {
            Semaphore.Release();
        }

        if (!exists)
            return;

        var services = ServiceProvider.GetServices<IScopedHostedService>();
        foreach (var service in services)
        {
            await service.StopAsync(cancellationToken);
        }
    }

    public void Dispose() => DisposeAsync().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
    }
    
    
    

    private readonly TaskCompletionSource<ChannelMonitor[]?> icm = new();

    public async Task<ChannelMonitor[]> GetInitialChannelMonitors()
    {
        return await icm.Task;
    }
    
    public async Task Payment(LightningPayment lightningPayment, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await context.LightningPayments.Upsert(lightningPayment).RunAsync(cancellationToken);
    }

    public async Task PaymentUpdate(string paymentHash, bool inbound, string paymentId, bool failure,
        string? preimage, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var payment = await context.LightningPayments.SingleOrDefaultAsync(lightningPayment => 
                lightningPayment.PaymentHash == paymentHash && 
                lightningPayment.Inbound == inbound && 
                lightningPayment.PaymentId == paymentId,
            cancellationToken);
        if (payment != null)
        {
            if (failure && payment.Status == LightningPaymentStatus.Complete)
            {
                // ignore as per ldk docs that this might happen
            }
            else
            {
                payment.Status = failure ? LightningPaymentStatus.Failed : LightningPaymentStatus.Complete;
                payment.Preimage ??= preimage;
            }
            await context.SaveChangesAsync(cancellationToken);
        }

    }
    
    

    private async Task<ChannelMonitor[]> GetInitialChannelMonitors(EntropySource entropySource,
        SignerProvider signerProvider)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var data = await db.LightningChannels.Include(c => c.Setting).Select(channel => channel.Setting.Value)
            .ToArrayAsync();

        var channels = ChannelManagerHelper.GetInitialMonitors(data, entropySource, signerProvider);
        icm.SetResult(channels);
        return channels;
    }

    public async Task<byte[]?> GetRawChannelManager()
    {
        return await _configProvider.Get<byte[]>("ChannelManager") ?? Array.Empty<byte>();
    }

    public async Task UpdateChannelManager(ChannelManager serializedChannelManager)
    {
        await _configProvider.Set("ChannelManager", serializedChannelManager.write());
    }

    
    public async Task UpdateNetworkGraph(NetworkGraph networkGraph)
    {
        await _configProvider.Set("NetworkGraph", networkGraph.write());
    }

    public async Task UpdateScore(WriteableScore score)
    {
        await _configProvider.Set("Score", score.write());
    }

    
    public async Task<(byte[] serializedChannelManager, ChannelMonitor[] channelMonitors)?> GetSerializedChannelManager(
        EntropySource entropySource, SignerProvider signerProvider)
    {

        var data = await GetRawChannelManager();
        if (data is null)
        {
            icm.SetResult(Array.Empty<ChannelMonitor>());
            return null;
        }

        var channels = await GetInitialChannelMonitors(entropySource, signerProvider);
        return (data, channels);
    }

    public async Task<Script> DeriveScript()
    {
        return await _onChainWalletManager.DeriveScript(WalletDerivation.NativeSegwit);
    }

    public async Task TrackScripts(Script[] scripts)
    {
        var identifier = _onChainWalletManager.WalletConfig.Derivations[WalletDerivation.LightningScripts].Identifier;
        
        await _connectionManager.HubProxy.TrackScripts(WalletDerivation.LightningScripts,scripts.Select(script => script.ToHex()).ToArray());
    }
}