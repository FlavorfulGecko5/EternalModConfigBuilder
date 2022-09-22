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
            catch (Exception)
            {
                throw new ArithmeticException("The expression does not evaluate"
                + " to a Boolean.\nExpression Result: '" + rawResult + "'\n\n" 
                + RULES_TOGGLE_RESULT);
            }
        }
        return resultBool;
    }
}