using Algorist.Api.Application;
using Algorist.Api.Application.Localization;
using Algorist.Api.Endpoints;
using Algorist.Api.ErrorHandling;
using Algorist.Api.Infra;
using Algorist.Contracts.Configuration;
using Algorist.Contracts.Errors;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

var mainDatabaseConnectionString = builder.Configuration[ConfigurationKeys.MainDatabaseConnectionString];
if (string.IsNullOrWhiteSpace(mainDatabaseConnectionString))
{
    throw new InvalidOperationException(
        $"Missing required configuration value '{ConfigurationKeys.MainDatabaseConnectionString}'. " +
        "Set it as an environment variable (deployed environments) or with 'dotnet user-secrets set' (local development).");
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(mainDatabaseConnectionString);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();

var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(ServerCultures.Default)
    .AddSupportedCultures(ServerCultures.SupportedCultures)
    .AddSupportedUICultures(ServerCultures.SupportedUICultures);
localizationOptions.RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider()];
app.UseRequestLocalization(localizationOptions);

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

// Serves the webapp's static assets (including compressed, fingerprinted _framework files).
app.MapStaticAssets();

var api = app.MapGroup("/api");
api.MapHealthChecks("/health");
foreach (var module in app.Services.GetServices<IEndpointModule>())
{
    module.MapEndpoints(api);
}

// Unknown API paths must return an error code, never the webapp page.
app.MapFallback("/api/{**path}", () => ProblemResults.Problem(StatusCodes.Status404NotFound, ErrorCodes.NotFound));

// Every other non-file path returns the webapp so client-side routes work on reload/deep link.
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
