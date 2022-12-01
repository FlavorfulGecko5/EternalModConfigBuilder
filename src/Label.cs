class Label
{
    public int       start  {get; private set;} = -1;
    public int       end    {get; private set;} = -1;
    public string    raw    {get; private set;} = "!UNDEFINED!";
    public string    type   {get; private set;} = "!UNDEFINED!";
    public string    exp    {get; private set;} = "!UNDEFINED!";

    public Label() {}

    public Label(int startParm, int endParm, string labelParm, int separatorIndex)
    {
        start  = startParm;
        end    = endParm;
        raw    = labelParm;

        // Excludes separator index. Capitalize for switch comparisons
        type = raw.Substring(0, separatorIndex).ToUpper();
        exp = raw.Substring(separatorIndex + 1, raw.Length - separatorIndex - 2);
    }
}