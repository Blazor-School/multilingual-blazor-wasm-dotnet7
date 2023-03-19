using DeferredTranslationWithLocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using System.Globalization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddLocalization(options => options.ResourcesPath = "BlazorSchoolResources");

var host = builder.Build();

await SetWebsiteLanguage(host);
await host.RunAsync();

static async Task SetWebsiteLanguage(WebAssemblyHost host)
{
    var js = host.Services.GetRequiredService<IJSRuntime>();
    string userPreferenceLanguage = await js.InvokeAsync<string>("blazorCulture.get");
    string chosenLanguage = string.IsNullOrEmpty(userPreferenceLanguage) ? "en" : userPreferenceLanguage;
    CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(chosenLanguage);
    CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(chosenLanguage);
}