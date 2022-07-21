using System.Data;
using static Label.Error;
class Label
{
    private static DataTable computer = new DataTable();
    private static List<Option> options = new List<Option>();

    public static void setOptionList(List<Option> optionsParameter)
    {
        options = optionsParameter;
    }

    private readonly string path;
    public LabelType type;
    public string raw, exp, result;
    public int start, end;

    public Label(int startParm, int endParm, string labelParm, string pathParm)
    {
        start = startParm;
        end = endParm;
        raw = labelParm;
        path = pathParm;
        exp = result = "";
        type = LabelType.VAR;
    }

    public void split()
    {
        int separator = raw.IndexOf(LABEL_CHAR_SEPARATOR);
        if(separator == -1)
            throw EMBError(MISSING_EXP_SEPARATOR);
        
        // Excludes separator index. Capitalize for switch comparisons
        string typeString = raw.Substring(0, separator).ToUpper();
        switch(typeString)
        {
            case LABEL_ANY_VARIABLE:
            type = LabelType.VAR;
            break;

            default:
            throw EMBError(BAD_TYPE);
        }

        exp = raw.Substring(separator + 1, raw.Length - separator - 2);
    }

    public void computeResult()
    {
        int numIterations = 0;         // Prevents infinite loops
        bool replacedThisCycle = true; // Allows nested variables
        while (replacedThisCycle)
        {
            if (numIterations++ == EXP_INFINITE_LOOP_THRESHOLD)
                throw EMBError(EXP_LOOPS_INFINITELY);
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

        string? rawResult = null;

        try
        {
            rawResult = computer.Compute(exp, "").ToString();
        }
        catch(Exception e)
        {
            throw EMBError(CANT_EVAL_EXP, e.Message);
        }
        

        if (rawResult == null)
            rawResult = NULL_EXP_RESULT;

        // Decl files use lowercase true/false
        // Variations in capitalization cause game crashes
        if (rawResult.Equals("true", CCIC) || rawResult.Equals("false", CCIC))
            rawResult = rawResult.ToLower();
        result = rawResult;
    }

    public enum Error
    {
        MISSING_EXP_SEPARATOR,
        BAD_TYPE,
        EXP_LOOPS_INFINITELY,
        CANT_EVAL_EXP
    }

    private EMBException EMBError(Error e, string arg0 = "")
    {
        string preamble = String.Format(
            "Problem encountered with label '{0}' in mod file '{1}'\n",
            raw,
            path
        );
        string msg = "";
        string[] args = {"", ""};

        switch(e)
        {
            case MISSING_EXP_SEPARATOR:
            msg = "There is no '{0}' written after the type.\n\n{1}";
            args[0] = LABEL_CHAR_SEPARATOR;
            args[1] = RULES_LABEL_FORMAT;
            break;

            case BAD_TYPE:
            msg = "The type is unrecognized.\n\n{0}";
            args[0] = RULES_LABEL_TYPES;
            break;

            case EXP_LOOPS_INFINITELY:
            msg = "The expression loops infinitely when inserting Option"
                    + " values.\nLast edited form of the expression: '{0}'";
            args[0] = exp;
            break;

            case CANT_EVAL_EXP:
            msg = "Expression failed to evaluate"
                    + "\nExpression form at evaluation: '{0}'"
                    + "\n\nPrinting Error Message:\n{1}";
            args[0] = exp;
            args[1] = arg0; // Exception message
            break;
        }
        // Prevents System.Format exception from label syntax
        // (Technically still possible in other EMBError functions, but
        // should not realistically happen)
        string formattedMsg = String.Format(msg, args);
        return new EMBException(preamble + formattedMsg);
    }
}

enum LabelType
{
    VAR
}