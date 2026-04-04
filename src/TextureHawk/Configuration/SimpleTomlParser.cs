namespace TextureHawk.Configuration;

/// <summary>
/// Minimal TOML parser supporting the subset needed for processor definitions.
/// </summary>
/// <remarks>
/// Handles: comments (#), key = value pairs (string, int, float, bool),
/// [table] headers, [[array-of-tables]], and string arrays.
/// Does NOT handle: multiline strings, date/time, inline tables, nested keys.
/// This is an intentionally minimal in-house implementation.
/// </remarks>
public static class SimpleTomlParser
{
    /// <summary>
    /// Parses TOML text into a nested dictionary structure.
    /// </summary>
    /// <returns>
    /// A dictionary where keys map to: string, long, double, bool,
    /// <see cref="List{T}"/> of string, <see cref="Dictionary{TKey,TValue}"/> (table),
    /// or <see cref="List{T}"/> of dictionaries (array of tables).
    /// </returns>
    public static Dictionary<string, object> Parse(string input)
    {
        var root = new Dictionary<string, object>(StringComparer.Ordinal);
        Dictionary<string, object> currentTable = root;
        string? arrayOfTablesKey = null;

        foreach (var rawLine in input.AsSpan().EnumerateLines())
        {
            var line = rawLine.Trim();
            if (line.IsEmpty || line[0] == '#')
                continue;

            var lineStr = line.ToString();

            // [[array-of-tables]]
            if (lineStr.StartsWith("[[") && lineStr.EndsWith("]]"))
            {
                var key = lineStr[2..^2].Trim();
                arrayOfTablesKey = key;

                if (!root.TryGetValue(key, out var existing))
                {
                    existing = new List<Dictionary<string, object>>();
                    root[key] = existing;
                }

                var list = (List<Dictionary<string, object>>)existing;
                currentTable = new Dictionary<string, object>(StringComparer.Ordinal);
                list.Add(currentTable);
                continue;
            }

            // [table]
            if (lineStr.StartsWith('[') && lineStr.EndsWith(']'))
            {
                var key = lineStr[1..^1].Trim();
                arrayOfTablesKey = null;

                if (!root.TryGetValue(key, out var existing))
                {
                    existing = new Dictionary<string, object>(StringComparer.Ordinal);
                    root[key] = existing;
                }

                currentTable = (Dictionary<string, object>)existing;
                continue;
            }

            // key = value
            var eqIndex = lineStr.IndexOf('=');
            if (eqIndex <= 0)
                continue;

            var k = lineStr[..eqIndex].Trim();
            var v = lineStr[(eqIndex + 1)..].Trim();

            // Strip inline comment (outside of strings)
            if (v.Length > 0 && v[0] != '"' && v[0] != '[')
            {
                var commentIdx = v.IndexOf('#');
                if (commentIdx >= 0)
                    v = v[..commentIdx].TrimEnd();
            }

            currentTable[k] = ParseValue(v);
        }

        return root;
    }

    private static object ParseValue(string value)
    {
        if (value.Length == 0)
            return "";

        // Quoted string
        if (value.StartsWith('"') && value.EndsWith('"') && value.Length >= 2)
            return Unescape(value[1..^1]);

        // Boolean
        if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
            return true;
        if (value.Equals("false", StringComparison.OrdinalIgnoreCase))
            return false;

        // String array ["a", "b", "c"]
        if (value.StartsWith('[') && value.EndsWith(']'))
            return ParseStringArray(value[1..^1]);

        // Float (contains '.')
        if (value.Contains('.') && double.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var d))
            return d;

        // Integer
        if (long.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var l))
            return l;

        // Unquoted string fallback
        return value;
    }

    private static List<string> ParseStringArray(string inner)
    {
        var result = new List<string>();
        var span = inner.AsSpan().Trim();

        while (!span.IsEmpty)
        {
            span = span.TrimStart();
            if (span.IsEmpty)
                break;

            if (span[0] == '"')
            {
                span = span[1..];
                var endQuote = span.IndexOf('"');
                if (endQuote < 0)
                    break;
                result.Add(span[..endQuote].ToString());
                span = span[(endQuote + 1)..].TrimStart();
                if (!span.IsEmpty && span[0] == ',')
                    span = span[1..];
            }
            else
            {
                // Unquoted element
                var comma = span.IndexOf(',');
                if (comma < 0)
                {
                    result.Add(span.Trim().ToString());
                    break;
                }
                result.Add(span[..comma].Trim().ToString());
                span = span[(comma + 1)..];
            }
        }

        return result;
    }

    private static string Unescape(string s) => s
        .Replace("\\n", "\n")
        .Replace("\\t", "\t")
        .Replace("\\\\", "\\")
        .Replace("\\\"", "\"");
}
