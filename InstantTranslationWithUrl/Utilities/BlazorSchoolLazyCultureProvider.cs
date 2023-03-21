using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Reflection;
using System.Web;

namespace InstantTranslationWithUrl.Utilities;

public class BlazorSchoolLazyCultureProvider : IDisposable
{
    private readonly List<ComponentBase> _subscribedComponents = new();
    private readonly HttpClient _httpClient;
    private readonly IOptions<LocalizationOptions> _localizationOptions;
    private readonly BlazorSchoolResourceMemoryStorage _blazorSchoolResourceMemoryStorage;
    private readonly NavigationManager _navigationManager;
    private string _fallbackLanguage;

    public BlazorSchoolLazyCultureProvider(IHttpClientFactory httpClientFactory, IOptions<LocalizationOptions> localizationOptions, BlazorSchoolResourceMemoryStorage blazorSchoolResourceMemoryStorage, NavigationManager navigationManager)
    {
        _httpClient = httpClientFactory.CreateClient("InternalHttpClient");
        _localizationOptions = localizationOptions;
        _blazorSchoolResourceMemoryStorage = blazorSchoolResourceMemoryStorage;
        _navigationManager = navigationManager;
        _navigationManager.LocationChanged += OnLocationChanged;
        _fallbackLanguage = "";
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        string languageFromUrl = GetLanguageFromUrl();
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(languageFromUrl);
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(languageFromUrl);
        await NotifyLanguageChangeAsync();
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
        _fallbackLanguage = fallbackLanguage;
        string languageFromUrl = GetLanguageFromUrl();
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(languageFromUrl);
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(languageFromUrl);
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

    public string GetLanguageFromUrl()
    {
        var uri = new Uri(_navigationManager.Uri);
        var urlParameters = HttpUtility.ParseQueryString(uri.Query);

        return string.IsNullOrEmpty(urlParameters["language"]) ? _fallbackLanguage : urlParameters["language"]!;
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

    public void Dispose() => _navigationManager.LocationChanged -= OnLocationChanged;
}