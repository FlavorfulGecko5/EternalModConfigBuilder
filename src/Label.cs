using System.Data;
class Label
{
    private static DataTable computer = new DataTable();
    private static List<Option> options = new List<Option>();

    public static void setOptionList(List<Option> optionsParameter)
    {
        options = optionsParameter;
    }

    public LabelType type;
    public string raw, exp, result;
    public int start, end;

    public Label(int startParam, int endParam, string fullLabelParam)
    {
        start = startParam;
        end = endParam;
        raw = fullLabelParam;
        exp = result = "";
        type = LabelType.INVALID;
    }

    public bool splitLabel()
    {
        int separator = raw.IndexOf(LABEL_CHAR_SEPARATOR);
        if(separator == -1)
            return false;
        
        // Excludes separator index. Capitalize for switch comparisons
        string typeString = raw.Substring(0, separator).ToUpper();
        switch(typeString)
        {
            case LABEL_ANY_VARIABLE:
            type = LabelType.VAR;
            break;

            case LABEL_ANY_TOG:
            type = LabelType.TOGGLE_START;
            break;

            case LABEL_END_TOG:
            type = LabelType.TOGGLE_END;
            break;

            default:
            type = LabelType.INVALID;
            break;
        }

        exp = raw.Substring(separator + 1, raw.Length - separator - 2);

        return true;
    }

    public bool computeResult()
    {
        int numIterations = 0;         // Prevents infinite loops
        bool replacedThisCycle = true; // Allows nested variables
        while (replacedThisCycle)
        {
            if (numIterations++ == INFINITE_LOOP_THRESHOLD)
                return true;
            replacedThisCycle = false;

            foreach (Option option in options)
            {
                string search = '{' + option.name + '}';
                if (exp.IndexOf(search, CCIC) != -1)
                {
                    replacedThisCycle = true;
                    exp = exp.Replace(search, option.value, CCIC);
                }
            }
        }

        string? rawResult = computer.Compute(exp, "").ToString();

        if (rawResult == null)
            rawResult = NULL_EXP_RESULT;

        // Decl files use lowercase true/false
        // Variations in capitalization cause game crashes
        if (rawResult.Equals("true", CCIC) || rawResult.Equals("false", CCIC))
            rawResult = rawResult.ToLower();
        result = rawResult;

        return false;
    }
    
    public bool? resultToBool()
    {
        bool resultBool = false;
        try
        {
            resultBool = Convert.ToBoolean(result);
        }
        catch(System.FormatException)
        {
            try
            {
                resultBool = Convert.ToDouble(result) >= 1 ? true : false;
            }
            catch(Exception)
            {
                return null;
            }
        }
        return resultBool;
    }
}

enum LabelType
{
    INVALID,
    VAR,
    TOGGLE_START,
    TOGGLE_END
}