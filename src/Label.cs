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

    public string    path   {get; private set;} = "!UNDEFINED!";
    public int       start  {get; private set;} = -1;
    public int       end    {get; private set;} = -1;
    public string    raw    {get; private set;} = "!UNDEFINED!";
    public LabelType type   {get; private set;} = LabelType.VAR;
    public string    exp    {get; private set;} = "!UNDEFINED!";
    public string    result {get; private set;} = "!UNCOMPUTED!";

    public Label() {}

    public Label(int startParm, int endParm, string labelParm, string pathParm)
    {
        start  = startParm;
        end    = endParm;
        raw    = labelParm;
        path   = pathParm;

        int separator = raw.IndexOf(LABEL_CHAR_SEPARATOR);
        if (separator == -1)
            throw EMBError(MISSING_EXP_SEPARATOR);

        // Excludes separator index. Capitalize for switch comparisons
        string typeString = raw.Substring(0, separator).ToUpper();
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
                if (exp.IndexOfCCIC(search) != -1)
                {
                    replacedThisCycle = true;
                    exp = exp.ReplaceCCIC(search, option.value);
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
        if (rawResult.EqualsCCIC("true") || rawResult.EqualsCCIC("false"))
            rawResult = rawResult.ToLower();
        result = rawResult;
    }

    public bool resultToToggleBool()
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
                throw EMBError(CANT_EVAL_TOGGLE_BOOL);
            }
        }
        return resultBool;
    }

    public enum Error
    {
        MISSING_EXP_SEPARATOR,
        BAD_TYPE,
        EXP_LOOPS_INFINITELY,
        CANT_EVAL_EXP,
        CANT_EVAL_TOGGLE_BOOL
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
            args[0] = DESC_LABEL_TYPES;
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

            case CANT_EVAL_TOGGLE_BOOL:
            msg = "The expression does not evaluate to a Boolean."
                + "\nExpression Result: '{0}'\n\n{1}";
            args[0] = result;
            args[1] = RULES_TOGGLE_RESULT;
            break;
        }
        // Prevents System.Format exception from label syntax
        // (Technically still possible in other EMBError functions, but
        // should not realistically happen)
        string formattedMsg = String.Format(msg, args);
        return new EMBException(preamble + formattedMsg);
    }
}