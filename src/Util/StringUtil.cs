using static System.StringComparison;
static class StringUtil
{
    public static bool ContainsCCIC(this string a, string b)
    {
        return a.Contains(b, CurrentCultureIgnoreCase);
    }

    public static bool EndsWithCCIC(this string a, string b)
    {
        return a.EndsWith(b, CurrentCultureIgnoreCase);
    }

    public static bool EqualsCCIC(this string a, string b)
    {
        return a.Equals(b, CurrentCultureIgnoreCase);
    }

    public static int IndexOfCCIC(this string a, string b, int startIndex = 0)
    {
        return a.IndexOf(b, startIndex, CurrentCultureIgnoreCase);
    }

    public static int LastIndexOfCCIC(this string a, string b)
    {
        return a.LastIndexOf(b, CurrentCultureIgnoreCase);
    }

    public static string ReplaceCCIC(this string a, string find, string replace)
    {
        return a.Replace(find, replace, CurrentCultureIgnoreCase);
    }
}