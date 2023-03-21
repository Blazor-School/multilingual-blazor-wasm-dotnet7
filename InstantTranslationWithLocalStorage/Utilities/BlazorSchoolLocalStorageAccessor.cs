using System.Runtime.InteropServices.JavaScript;

namespace InstantTranslationWithLocalStorage.Utilities;

public static partial class BlazorSchoolLocalStorageAccessor
{
    [JSImport("globalThis.localStorage.setItem")]
    public static partial void SetItem(string key, string value);
    
    [JSImport("globalThis.localStorage.getItem")]
    public static partial string GetItem(string key);
}
