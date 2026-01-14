using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MarketplaceBuilder.Api.Helpers;

public static class SlugHelper
{
    private static readonly Regex InvalidCharsRegex = new(@"[^a-z0-9\s-]", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex MultipleHyphensRegex = new(@"-+", RegexOptions.Compiled);

    public static string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Normalize to decomposed form
        text = text.Normalize(NormalizationForm.FormD);

        // Remove diacritics (accents)
        var chars = text.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        text = new string(chars.ToArray()).Normalize(NormalizationForm.FormC);

        // Convert to lowercase
        text = text.ToLowerInvariant();

        // Remove invalid characters
        text = InvalidCharsRegex.Replace(text, "");

        // Replace whitespace with hyphens
        text = WhitespaceRegex.Replace(text.Trim(), "-");

        // Replace multiple hyphens with single hyphen
        text = MultipleHyphensRegex.Replace(text, "-");

        // Trim hyphens from ends
        text = text.Trim('-');

        // Limit length
        if (text.Length > 80)
            text = text.Substring(0, 80).TrimEnd('-');

        return text;
    }
}
