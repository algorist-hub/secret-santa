using Algorist.App.UI.Resources;
using Algorist.Contracts.Validation;
using Microsoft.Extensions.Localization;

namespace Algorist.App.UI.Localization;

/// <summary>Translates shared field error codes (<see cref="FieldErrors"/>) as <c>Validation_&lt;Code&gt;</c>.</summary>
public static class ValidationText
{
    public static string? Message(this IStringLocalizer<AppStrings> localizer, FieldError? error) =>
        error switch
        {
            null => null,
            { Limit: { } limit } => localizer[$"Validation_{error.Code}", limit].Value,
            _ => localizer[$"Validation_{error.Code}"].Value,
        };
}
