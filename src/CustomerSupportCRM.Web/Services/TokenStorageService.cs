using Microsoft.JSInterop;

namespace CustomerSupportCRM.Web.Services;

/// <summary>Thin wrapper over browser localStorage. Blazor's built-in JS interop can call global JS members by dotted name, so no custom JS file is needed for simple calls like this.</summary>
public class TokenStorageService(IJSRuntime jsRuntime)
{
    private const string StorageKey = "csc_auth_token";

    public ValueTask<string?> GetTokenAsync() =>
        jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);

    public ValueTask SetTokenAsync(string token) =>
        jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, token);

    public ValueTask ClearTokenAsync() =>
        jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
}
