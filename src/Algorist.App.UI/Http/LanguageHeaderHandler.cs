using System.Net.Http.Headers;
using Algorist.App.UI.Localization;

namespace Algorist.App.UI.Http;

/// <summary>Sends the current app language with every API request (Accept-Language).</summary>
public sealed class LanguageHeaderHandler(LanguageState state) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.AcceptLanguage.Clear();
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(state.Current.Code));
        return base.SendAsync(request, cancellationToken);
    }
}
