using InstantTranslationWithCookies;
using InstantTranslationWithCookies.Utilities;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Use either eager loading or lazy loading resource
await UseEagerCultureProviderAsync(builder);
// Use either eager loading or lazy loading resource
//await UseLazyCultureProviderAsync(builder);

static async Task UseEagerCultureProviderAsync(WebAssemblyHostBuilder builder)
{
    builder.Services.AddLocalization(options => options.ResourcesPath = "BlazorSchoolResources");
    builder.Services.AddScoped(sp => (IJSUnmarshalledRuntime)sp.GetRequiredService<IJSRuntime>());
    builder.Services.AddScoped<BlazorSchoolEagerCultureProvider>();

    var wasmHost = builder.Build();
    var culturesProvider = wasmHost.Services.GetService<BlazorSchoolEagerCultureProvider>();

    if (culturesProvider is not null)
    {
        await culturesProvider.LoadCulturesAsync("fr", "en");
        await culturesProvider.SetStartupLanguageAsync("fr");
    }

    await wasmHost.RunAsync();
}

static async Task UseLazyCultureProviderAsync(WebAssemblyHostBuilder builder)
{
    builder.Services.AddLocalization(options => options.ResourcesPath = "BlazorSchoolResources");
    builder.Services.AddHttpClient("InternalHttpClient", httpClient => httpClient.BaseAddress = new(builder.HostEnvironment.BaseAddress));
    builder.Services.AddScoped(typeof(IStringLocalizer<>), typeof(BlazorSchoolStringLocalizer<>));
    builder.Services.AddScoped<BlazorSchoolLazyCultureProvider>();
    builder.Services.AddScoped<BlazorSchoolResourceMemoryStorage>();

    var wasmHost = builder.Build();
    var culturesProvider = wasmHost.Services.GetService<BlazorSchoolLazyCultureProvider>();
    culturesProvider?.SetStartupLanguage("fr");

    await wasmHost.RunAsync();
}