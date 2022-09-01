using static System.StringComparison;
static class StringUtil
{
    public static bool embContains(this string a, string b)
    {
        return a.Contains(b, CurrentCultureIgnoreCase);
    }

    public static bool embEquals(this string a, string b)
    {
        return a.Equals(b, CurrentCultureIgnoreCase);
    }

    public static int embIndexOf(this string a, string b, int startIndex = 0)
    {
        return a.IndexOf(b, startIndex, CurrentCultureIgnoreCase);
    }

    public static int embLastIndexOf(this string a, string b)
    {
        return a.LastIndexOf(b, CurrentCultureIgnoreCase);
    }

    public static string embReplace(this string a, string find, string replace)
    {
        return a.Replace(find, replace, CurrentCultureIgnoreCase);
    }
}