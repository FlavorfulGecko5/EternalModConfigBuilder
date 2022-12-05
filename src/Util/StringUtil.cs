using static System.StringComparison;
static class StringUtil
{
    public static bool EndsWithOIC(this string a, string b)
    {
        return a.EndsWith(b, OrdinalIgnoreCase);
    }

    public static bool EqualsOIC(this string a, string b)
    {
        return a.Equals(b, OrdinalIgnoreCase);
    }

    public static int IndexOfOIC(this string a, string b, int startIndex = 0)
    {
        return a.IndexOf(b, startIndex, OrdinalIgnoreCase);
    }

    public static string ReplaceOIC(this string a, string find, string replace)
    {
        return a.Replace(find, replace, OrdinalIgnoreCase);
    }
}