using System.Data;
class ExpressionHandler
{
    private static DataTable computer = new DataTable();
    private static List<Option> options = new List<Option>();

    public static void setOptionList(List<Option> optionsParameter)
    {
        options = optionsParameter;
    }

    private static string substituteVariables(string exp)
    {
        int numIterations = 0;         // Prevents infinite loops
        bool replacedThisCycle = true; // Allows nested variables
        while (replacedThisCycle)
        {
            if (numIterations++ == EXP_INFINITE_LOOP_THRESHOLD)
                throw new ArithmeticException("The expression loops infinitely"
                    + " when inserting Option values.\n"
                    + "Last edited form of the expression: '" + exp + "'");
            
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
        return exp;
    }

    private static string calculateResult(string exp)
    {
        string result = "";

        try
        {
            result = computer.Compute(exp, "").ToString() ?? NULL_EXP_RESULT;
        }
        catch (Exception e)
        {
            throw new ArithmeticException("Failed to compute result."
                + "\nExpression form at evaluation: '" + exp + "'"
                + "\n\nPrinting Error Message:\n" + e.Message);
        }

        // Decl files use lowercase true/false
        // Variations in capitalization cause game crashes
        if (result.EqualsCCIC("true") || result.EqualsCCIC("false"))
            result = result.ToLower();
        return result;
    }

    public static string computeVarExpression(string exp)
    {
        return calculateResult(substituteVariables(exp));
    }

    public static bool computeToggleExpression(string exp)
    {
        string rawResult = calculateResult(substituteVariables(exp));
        bool resultBool = false;
        try
        {
            resultBool = Convert.ToBoolean(rawResult);
        }
        catch (System.FormatException)
        {
            try
            {
                resultBool = Convert.ToDouble(rawResult) >= 1 ? true : false;
            }
            catch (System.Exception)
            {
                throw new ArithmeticException("The expression does not evaluate"
                + " to a Boolean.\nExpression Result: '" + rawResult + "'\n\n" 
                + RULES_TOGGLE_RESULT);
            }
        }
        return resultBool;
    }

    public static string computeLoopExpression(string exp)
    {
        int indexOne = -1, indexTwo = -1, startNum = -1, endNum = -1;
        string stringStartNum = "", stringEndNum = "", mainExp = "";
        try
        {
            // Get the indexes for the separators
            indexOne = exp.IndexOf(LABEL_CHAR_LOOP_SEPARATOR);
            indexTwo = exp.IndexOf(LABEL_CHAR_LOOP_SEPARATOR, indexOne + 1);
            if(indexOne == -1 || indexTwo == -1)
                throw new System.ArgumentOutOfRangeException();
            // Split the expression using these indexes
            stringStartNum = exp.Substring(0, indexOne);
            stringEndNum = exp.Substring(indexOne + 1, indexTwo - indexOne - 1);
            mainExp = exp.Substring(indexTwo + 1, exp.Length - indexTwo - 1);

        }
        catch(System.ArgumentOutOfRangeException)
        {
            throw new ArithmeticException("The expression is missing "
                + "information required for a Loop Label.\n\n" + RULES_LOOPS);
        }

        // Evaluate the first two split strings into numbers
        // Lets Arithmetic Exceptions be thrown for catching outside this class
        stringStartNum = calculateResult(substituteVariables(stringStartNum));
        stringEndNum   = calculateResult(substituteVariables(stringEndNum));
        try
        {
            startNum = Convert.ToInt32(stringStartNum);
            endNum   = Convert.ToInt32(stringEndNum);
        }
        catch(System.Exception)
        {
            throw new ArithmeticException("The loop's start or end value "
                + "failed to evaluate to an integer.\n\n" + RULES_LOOPS);
        }
        if(startNum > endNum)
            throw new ArithmeticException("The loop's ending value must "
                + "be larger than it's starting value.\n\n" + RULES_LOOPS);
        
        // Construct the final expression that will be substituted for the label
        string expandedExp = "";
        for(int i = startNum; i <= endNum; i++)
        {
            string currentExp = mainExp.ReplaceCCIC(SYM_LOOP_INC, i.ToString());
            currentExp = calculateResult(substituteVariables(currentExp));
            expandedExp += currentExp;
        }
        return expandedExp;
    }
}