class Label
{
    public int       start  {get; private set;} = -1;
    public int       end    {get; private set;} = -1;
    public string    raw    {get; private set;} = "!UNDEFINED!";
    public LabelType type   {get; private set;} = LabelType.INVALID;
    public string    exp    {get; private set;} = "!UNDEFINED!";

    public Label() {}

    public Label(int startParm, int endParm, string labelParm, int separatorIndex)
    {
        start  = startParm;
        end    = endParm;
        raw    = labelParm;

        // Excludes separator index. Capitalize for switch comparisons
        string typeString = raw.Substring(0, separatorIndex).ToUpper();
        switch (typeString)
        {
            case LABEL_VAR:
                type = LabelType.VAR;
            break;

            case LABEL_TOGGLE_START:
                type = LabelType.TOGGLE_START;
            break;

            case LABEL_TOGGLE_END:
                type = LabelType.TOGGLE_END;
            break;

            case LABEL_LOOP:
                type = LabelType.LOOP;
            break;

            default:
                type = LabelType.INVALID;
            break;
        }
        exp = raw.Substring(separatorIndex + 1, raw.Length - separatorIndex - 2);
    }
}