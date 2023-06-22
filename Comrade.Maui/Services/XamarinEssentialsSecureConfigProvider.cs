﻿using System.Text.Json;
using Comrade.Core.Contracts;

namespace Comrade.Maui.Services;

public class XamarinEssentialsSecureConfigProvider : ISecureConfigProvider
{
    public async Task<T?> Get<T>(string key)
    {
        var raw = await SecureStorage.GetAsync(key);
        return string.IsNullOrEmpty(raw) ? default : JsonSerializer.Deserialize<T>(raw);
    }

    public async Task Set<T>(string key, T? value)
    {
        if (value is null)
        {
            SecureStorage.Remove(key);
        }
        else
        {
            await SecureStorage.SetAsync(key, JsonSerializer.Serialize(value));
        }
    }
}