using System.Text.RegularExpressions;

namespace SmartEvents.API.Application.Helpers;

public static partial class SlugHelper
{
    public static string Generate(string text)
    {
        var slug = text.ToLowerInvariant();
        slug = NonAlphanumericRegex().Replace(slug, "-");
        slug = MultipleHyphensRegex().Replace(slug, "-");
        slug = slug.Trim('-');
        return slug;
    }

    public static string GenerateUnique(string text, string suffix)
        => $"{Generate(text)}-{suffix}";

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex(@"[\s-]+")]
    private static partial Regex MultipleHyphensRegex();
}
