using System.Globalization;

namespace Algorist.App.UI.Localization;

/// <summary>
/// Dates typed as text in the current language's day/month/year order (pt-BR and en-GB: dd/MM/yyyy),
/// so the format follows the app language instead of the browser's date picker.
/// </summary>
public static class DateInput
{
    /// <summary>The current culture's short date pattern, with 2-digit day/month and 4-digit year.</summary>
    public static string Pattern(CultureInfo? culture = null)
    {
        var format = (culture ?? CultureInfo.CurrentCulture).DateTimeFormat;
        var order = format.ShortDatePattern.Replace("'", string.Empty)
            .Split(format.DateSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToLowerInvariant(part[0]) switch
            {
                'd' => "dd",
                'm' => "MM",
                _ => "yyyy",
            });
        return string.Join(format.DateSeparator, order);
    }

    public static string Format(DateOnly? date, CultureInfo? culture = null) =>
        date?.ToString(Pattern(culture), culture ?? CultureInfo.CurrentCulture) ?? string.Empty;

    /// <summary>
    /// Reads a typed date: the culture pattern with '/', '-', '.' or ' ' as separator, 1- or 2-digit day and month,
    /// or 8 digits without separators. Returns null when empty or not a real date.
    /// </summary>
    public static DateOnly? Parse(string? text, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        var value = (text ?? string.Empty).Trim();
        if (value.Length == 0)
        {
            return null;
        }

        var pattern = Pattern(culture);
        var separator = culture.DateTimeFormat.DateSeparator;
        if (value.All(char.IsAsciiDigit) && value.Length == 8)
        {
            value = InsertSeparators(value, pattern, separator);
        }
        else
        {
            foreach (var other in new[] { "/", "-", ".", " " })
            {
                value = value.Replace(other, separator);
            }
        }

        var shortPattern = pattern.Replace("dd", "d").Replace("MM", "M");
        return DateOnly.TryParseExact(value, [pattern, shortPattern], culture, DateTimeStyles.None, out var date) ? date : null;
    }

    private static string InsertSeparators(string digits, string pattern, string separator)
    {
        var parts = new List<string>();
        var index = 0;
        foreach (var part in pattern.Split(separator))
        {
            parts.Add(digits.Substring(index, part.Length));
            index += part.Length;
        }

        return string.Join(separator, parts);
    }
}
