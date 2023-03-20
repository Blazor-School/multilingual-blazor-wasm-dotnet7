using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Localization;
using Microsoft.JSInterop;
using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;

namespace InstantTranslationWithCookies.Utilities;

public class BlazorSchoolEagerCultureProvider
{
    private readonly IJSUnmarshalledRuntime _invoker;
    private const string _getSatelliteAssemblies = "Blazor._internal.getSatelliteAssemblies";
    private const string _readSatelliteAssemblies = "Blazor._internal.readSatelliteAssemblies";
    private readonly List<ComponentBase> _subscribedComponents = new();

    public BlazorSchoolEagerCultureProvider(IJSUnmarshalledRuntime invoker)
    {
        _invoker = invoker;
    }

    public async ValueTask LoadCulturesAsync(params string[] cultureNames)
    {
        var cultures = cultureNames.Select(n => CultureInfo.GetCultureInfo(n));
        var culturesToLoad = cultures.Select(c => c.Name).ToList();
        await _invoker.InvokeUnmarshalled<string[], object?, object?, Task<object>>(_getSatelliteAssemblies, culturesToLoad.ToArray(), null, null);
        object[]? assemblies = _invoker.InvokeUnmarshalled<object?, object?, object?, object[]>(_readSatelliteAssemblies, null, null, null);

        for (int i = 0; i < assemblies.Length; i++)
        {
            using var stream = new MemoryStream((byte[])assemblies[i]);
            AssemblyLoadContext.Default.LoadFromStream(stream);
        }
    }

    public async Task SetStartupLanguageAsync(string fallbackLanguage)
    {
        var jsRuntime = (IJSRuntime)_invoker;
        string cookie = await jsRuntime.InvokeAsync<string>("BlazorSchoolUtil.getCookieValue", CookieRequestCultureProvider.DefaultCookieName);
        var result = CookieRequestCultureProvider.ParseCookieValue(cookie);

        if (result is null)
        {
            var defaultCulture = CultureInfo.GetCultureInfo(fallbackLanguage);
            CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;
        }
        else
        {
            string storedLanguage = result.Cultures.First().Value;
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(storedLanguage);
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(storedLanguage);
        }
    }

    public void SubscribeLanguageChange(ComponentBase component) => _subscribedComponents.Add(component);

    public void UnsubscribeLanguageChange(ComponentBase component) => _subscribedComponents.Remove(component);

    public void NotifyLanguageChange()
    {
        foreach (var component in _subscribedComponents)
        {
            if (component is not null)
            {
                var stateHasChangedMethod = component.GetType()?.GetMethod("StateHasChanged", BindingFlags.Instance | BindingFlags.NonPublic);
                stateHasChangedMethod?.Invoke(component, null);
            }
        }
    }
}