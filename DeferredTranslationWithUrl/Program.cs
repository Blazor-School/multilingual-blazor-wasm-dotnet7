using DeferredTranslationWithUrl;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddLocalization(options => options.ResourcesPath = "BlazorSchoolResources");

var host = builder.Build();

SetWebsiteLanguage(host);
await host.RunAsync();

static void SetWebsiteLanguage(WebAssemblyHost host)
{
    var navigationManager = host.Services.GetRequiredService<NavigationManager>();
    var uri = new Uri(navigationManager.Uri);
    var urlParameters = HttpUtility.ParseQueryString(uri.Query);
    var defaultCulture =  CultureInfo.GetCultureInfo("fr");
    string urlQueryKey = "language";
    CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(urlParameters[urlQueryKey] ?? defaultCulture.Name);
    CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(urlParameters[urlQueryKey] ?? defaultCulture.Name);
}