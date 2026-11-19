using Algorist.App.UI.Resources;
using Microsoft.Extensions.Localization;

namespace Algorist.App.UI.Errors;

/// <summary>Translates API error codes into messages in the current language.</summary>
public sealed class ErrorMessageTranslator(IStringLocalizer<AppStrings> localizer)
{
    public const string UnknownKey = "Error_Unknown";

    public string Translate(string? errorCode)
    {
        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            var message = localizer[$"Error_{errorCode}"];
            if (!message.ResourceNotFound)
            {
                return message.Value;
            }
        }

        return localizer[UnknownKey].Value;
    }
}
