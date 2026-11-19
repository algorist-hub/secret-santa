using Algorist.Api.Application.Resources;
using Algorist.Api.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Algorist.Api.Tests;

/// <summary>Endpoints that exist only in tests.</summary>
internal sealed class TestEndpoints : IEndpointModule
{
    public const string ThrowPath = "/api/test/throw";
    public const string LocalizedPath = "/api/test/localized";
    public const string SecretMessage = "secret internal detail";

    public void MapEndpoints(IEndpointRouteBuilder api)
    {
        api.MapGet("/test/throw", IResult () => throw new InvalidOperationException(SecretMessage));
        api.MapGet("/test/localized", (IStringLocalizer<EmailTexts> texts) => Results.Text(texts["Email_Greeting"].Value));
    }
}
