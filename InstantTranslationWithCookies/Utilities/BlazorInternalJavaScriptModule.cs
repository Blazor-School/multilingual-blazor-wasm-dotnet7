using System.Runtime.InteropServices.JavaScript;

namespace InstantTranslationWithCookies.Utilities;

public static partial class BlazorInternalJavaScriptModule
{
    [JSImport("Blazor._internal.getSatelliteAssemblies")]
    [return: JSMarshalAs<JSType.Promise<JSType.Object>>]
    public static partial object[] GetSatelliteAssemblies(string[] culturesToLoad);
}
