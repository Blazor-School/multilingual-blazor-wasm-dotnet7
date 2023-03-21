using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using System.Globalization;
using System.Reflection;

namespace InstantTranslationWithLocalStorage.Utilities;

public class BlazorSchoolLazyCultureProvider
{
    private readonly List<ComponentBase> _subscribedComponents = new();
    private readonly HttpClient _httpClient;
    private readonly IOptions<LocalizationOptions> _localizationOptions;
    private readonly BlazorSchoolResourceMemoryStorage _blazorSchoolResourceMemoryStorage;

    public BlazorSchoolLazyCultureProvider(IHttpClientFactory httpClientFactory, IOptions<LocalizationOptions> localizationOptions, BlazorSchoolResourceMemoryStorage blazorSchoolResourceMemoryStorage)
    {
        _httpClient = httpClientFactory.CreateClient("InternalHttpClient");
        _localizationOptions = localizationOptions;
        _blazorSchoolResourceMemoryStorage = blazorSchoolResourceMemoryStorage;
    }

    private async Task<string> LoadCultureAsync(ComponentBase component)
    {
        if (string.IsNullOrEmpty(_localizationOptions.Value.ResourcesPath))
        {
            throw new Exception("ResourcePath not set.");
        }

        string componentName = component.GetType().FullName!;

        if (_blazorSchoolResourceMemoryStorage.JsonComponentResources.TryGetValue(new(componentName, CultureInfo.DefaultThreadCurrentCulture!.Name), out string? resultFromMemory))
        {
            return resultFromMemory;
        }

        var message = await _httpClient.GetAsync(ComposeComponentPath(componentName, CultureInfo.DefaultThreadCurrentCulture!.Name));
        string result;

        if (message.IsSuccessStatusCode is false)
        {
            var retryMessage = await _httpClient.GetAsync(ComposeComponentPath(componentName));

            if (retryMessage.IsSuccessStatusCode is false)
            {
                throw new Exception($"Cannot find the fallback resource for {componentName}.");
            }
            else
            {
                result = await message.Content.ReadAsStringAsync();
            }
        }
        else
        {
            result = await message.Content.ReadAsStringAsync();
        }

        _blazorSchoolResourceMemoryStorage.JsonComponentResources[new(componentName, CultureInfo.DefaultThreadCurrentCulture!.Name)] = result;

        return result;
    }

    public void SetStartupLanguage(string fallbackLanguage)
    {
        string languageFromLocalStorage = BlazorSchoolLocalStorageAccessor.GetItem("BlazorSchoolInstantTranslation");

        if (string.IsNullOrEmpty(languageFromLocalStorage))
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(fallbackLanguage);
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(fallbackLanguage);
        }
        else
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(languageFromLocalStorage);
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(languageFromLocalStorage);
        }
    }

    public async Task SubscribeLanguageChangeAsync(ComponentBase component)
    {
        _subscribedComponents.Add(component);
        await LoadCultureAsync(component);
    }

    public void UnsubscribeLanguageChange(ComponentBase component) => _subscribedComponents.Remove(component);

    public async Task NotifyLanguageChangeAsync()
    {
        foreach (var component in _subscribedComponents)
        {
            if (component is not null)
            {
                await LoadCultureAsync(component);
                var stateHasChangedMethod = component.GetType()?.GetMethod("StateHasChanged", BindingFlags.Instance | BindingFlags.NonPublic);
                stateHasChangedMethod?.Invoke(component, null);
            }
        }
    }

    private string ComposeComponentPath(string componentTypeName, string language = "")
    {
        var nameParts = componentTypeName.Split('.').ToList();
        nameParts.Insert(1, _localizationOptions.Value.ResourcesPath);
        nameParts.RemoveAt(0);
        string componentName = nameParts.Last();
        nameParts[^1] = string.IsNullOrEmpty(language) ? $"{componentName}.json" : $"{componentName}.{language}.json";
        string resourceLocaltion = string.Join("/", nameParts);

        return resourceLocaltion;
    }
}