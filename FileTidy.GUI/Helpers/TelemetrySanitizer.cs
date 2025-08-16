using System.Text.RegularExpressions;

namespace FileTidy.GUI.Helpers;

public class TelemetrySanitizer
{
    private static readonly Regex PathLike = new(
        @"([A-Za-z]:\\[^:*?""<>|]+|/[^:*?""<>|]+)+",
        RegexOptions.Compiled);

    public static string StripPaths(string input)
        => string.IsNullOrEmpty(input) ? input : PathLike.Replace(input, "<path>");
}