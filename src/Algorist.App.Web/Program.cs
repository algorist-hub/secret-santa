using Algorist.App.UI;
using Algorist.App.UI.Localization;
using Algorist.App.Web.Platform;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<AppRoot>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The API is served from the same origin as the webapp.
builder.Services.AddAppUI(new Uri(builder.HostEnvironment.BaseAddress));
builder.Services.AddWebPlatform();

var host = builder.Build();

// Choose and apply the language before the first render (stored → detected → Select Language screen).
await host.Services.GetRequiredService<LanguageBootstrapper>().InitializeAsync();
var language = host.Services.GetRequiredService<LanguageState>().Current;
await host.Services.GetRequiredService<IJSRuntime>().InvokeVoidAsync("algoristPlatform.setDocumentLanguage", language.Code);

await host.RunAsync();
