using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace InstantTranslationWithLocalStorage.Utilities;

public class BlazorSchoolStringLocalizer<TComponent> : IStringLocalizer<TComponent> where TComponent : ComponentBase
{
    private readonly BlazorSchoolResourceMemoryStorage _blazorSchoolResourceMemoryStorage;

    public LocalizedString this[string name] => FindLocalziedString(name);
    public LocalizedString this[string name, params object[] arguments] => FindLocalziedString(name, arguments);

    public BlazorSchoolStringLocalizer(BlazorSchoolResourceMemoryStorage blazorSchoolResourceMemoryStorage)
    {
        _blazorSchoolResourceMemoryStorage = blazorSchoolResourceMemoryStorage;
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => throw new NotImplementedException("We do not need to implement this method. This method is not support asynchronous anyway.");

    private LocalizedString FindLocalziedString(string name, object[]? arguments = null)
    {
        LocalizedString result = new(name, "", true, "External resource");
        _blazorSchoolResourceMemoryStorage.JsonComponentResources.TryGetValue(new(typeof(TComponent).FullName!, CultureInfo.DefaultThreadCurrentCulture!.Name), out string? jsonResource);

        if (string.IsNullOrEmpty(jsonResource))
        {
            return result;
        }

        var jObject = JObject.Parse(jsonResource);
        bool success = jObject.TryGetValue(name, out var jToken);

        if (success)
        {
            string value = jToken!.Value<string>()!;

            if (arguments is not null)
            {
                value = string.Format(value, arguments);
            }

            result = new(name, value, false, "External resource");
        }

        return result;
    }
}