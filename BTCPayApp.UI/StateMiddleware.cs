﻿using BTCPayApp.Core;
using BTCPayApp.Core.Contracts;
using BTCPayApp.UI.Features;
using Fluxor;

namespace BTCPayApp.UI;

public class StateMiddleware : Middleware
{
    private readonly IConfigProvider _configProvider;
    private readonly BTCPayAppConfigManager _btcPayAppConfigManager;
    private readonly BTCPayConnection _btcPayConnection;
    private readonly LightningNodeManager _lightningNodeManager;

    private const string UiStateConfigKey = "uistate";

    public StateMiddleware(IConfigProvider configProvider, 
        BTCPayAppConfigManager btcPayAppConfigManager, 
        BTCPayConnection btcPayConnection, 
        LightningNodeManager lightningNodeManager)
    {
        _configProvider = configProvider;
        _btcPayAppConfigManager = btcPayAppConfigManager;
        _btcPayConnection = btcPayConnection;
        _lightningNodeManager = lightningNodeManager;
    }

    public override async Task InitializeAsync(IDispatcher dispatcher, IStore store)
    {
        if (store.Features.TryGetValue(typeof(UIState).FullName, out var uiStateFeature))
        {
            var existing = await _configProvider.Get<UIState>(UiStateConfigKey);
            if (existing is not null)
            {
                uiStateFeature.RestoreState(existing);
            }

            uiStateFeature.StateChanged += async (sender, args) =>
            {
                await _configProvider.Set(UiStateConfigKey, (UIState)uiStateFeature.GetState());
            };
        }

        await base.InitializeAsync(dispatcher, store);

        ListenIn(dispatcher);
    }

    private void ListenIn(IDispatcher dispatcher)
    {
        dispatcher.Dispatch(new RootState.LoadingAction(true));
        _btcPayAppConfigManager.PairConfigUpdated +=
            (_, config) => dispatcher.Dispatch(new RootState.PairConfigLoadedAction(config));
        _btcPayAppConfigManager.WalletConfigUpdated += (_, config) =>
            dispatcher.Dispatch(new RootState.WalletConfigLoadedAction(config));
        _ = _btcPayAppConfigManager.Loaded.Task.ContinueWith(_ =>
        {
            dispatcher.Dispatch(new RootState.LoadingAction(false));
            dispatcher.Dispatch(new RootState.WalletConfigLoadedAction(_btcPayAppConfigManager.WalletConfig));
            dispatcher.Dispatch(new RootState.PairConfigLoadedAction(_btcPayAppConfigManager.PairConfig));
        });
        _btcPayConnection.ConnectionChanged += (sender, args) =>
            dispatcher.Dispatch(new RootState.BTCPayConnectionUpdatedAction(_btcPayConnection.Connection?.State));
        _lightningNodeManager.StateChanged += (sender, args) =>
            dispatcher.Dispatch(new RootState.LightningNodeStateUpdatedAction(args));
    }
}
