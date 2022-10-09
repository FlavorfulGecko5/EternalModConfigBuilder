using System.Data;
class ExpressionHandler
{
    private static DataTable computer = new DataTable();
    private static Dictionary<string, string> options = new Dictionary<string, string>();

    public static void setOptionList(Dictionary<string, string> optionsParameter)
    {
        options = optionsParameter;
    }

    private static string calculateResult(string exp)
    {
        // PHASE 1 - Evaluate Sub-Expressions
        int subStartIndex = exp.IndexOfCCIC(SYM_SUBEXP_START);
        int subEndIndex = subStartIndex, numEndsNeeded = 1;
        while(subStartIndex > -1)
        { 
            subEndIndex = exp.IndexOfCCIC(SYM_SUBEXP_ANY, subEndIndex + 1);
            if(subEndIndex == -1)
                throw ExpError(
                    "There is a '{0}' symbol with no '{1}' symbol following it.\n\n{2}",
                    SYM_SUBEXP_START, SYM_SUBEXP_END, RULES_SUBEXPRESSIONS);
            if(subEndIndex == exp.IndexOfCCIC(SYM_SUBEXP_START, subEndIndex))
                numEndsNeeded++;
            else if(subEndIndex == exp.IndexOfCCIC(SYM_SUBEXP_END, subEndIndex))
            {
                if(--numEndsNeeded == 0)
                {
                    // Indices used in substring calculations
                    int subExpIndex = subStartIndex + SYM_SUBEXP_START.Length;
                    int postSubExp = subEndIndex + SYM_SUBEXP_END.Length;

                    // Get sub-expression, and place result into expression
                    string subExp = exp.Substring(subExpIndex, 
                        subEndIndex - subExpIndex);
                    string subResult = calculateResult(subExp);
                    exp = exp.Substring(0, subStartIndex) + subResult 
                        + exp.Substring(postSubExp, exp.Length - postSubExp);

                    // Setup next loop check
                    subStartIndex = exp.IndexOfCCIC(SYM_SUBEXP_START);
                    subEndIndex = subStartIndex;
                    numEndsNeeded = 1;
                }
            }
        }
        if(exp.IndexOfCCIC(SYM_SUBEXP_END) > -1)
            throw ExpError(
                "There is a '{0}' symbol with no preceding '{1}' symbol.\n\n{2}",
                SYM_SUBEXP_END, SYM_SUBEXP_START, RULES_SUBEXPRESSIONS);

        // PHASE 2 - Substitute Variables
        int numIterations = 0; // Prevents infinite loops
        int openIndex = exp.IndexOf('{');
        while (openIndex > -1)
        {
            int nextOpenIndex = exp.IndexOf('{', openIndex + 1),
                closeIndex = exp.IndexOf('}', openIndex + 1);

            if (closeIndex == -1) // Also accounts for nextOpen == close (both -1)
                break;
            if (nextOpenIndex < closeIndex) // Potentially a nested brace pair
            {
                if (nextOpenIndex == -1)
                    replace();
                else
                    openIndex = nextOpenIndex;
            }
            else
                replace();

            void replace()
            {
                string name = exp.Substring(openIndex + 1, closeIndex - openIndex - 1).ToLower();
                if (options.ContainsKey(name))
                {
                    exp = exp.ReplaceCCIC('{' + name + '}', options[name]);
                    openIndex = exp.IndexOf('{');

                    if (numIterations++ == EXP_INFINITE_LOOP_THRESHOLD)
                        throw ExpError(
                            "The expression loops infinitely when inserting Option values."
                            + "\nLast edited form of the expression: '{0}'", 
                            exp);
                }
                else
                    openIndex = nextOpenIndex;
            }
        }

        // PHASE 3 - Calculate Result
        string result = "";
        try
        {
            result = computer.Compute(exp, "").ToString() ?? NULL_EXP_RESULT;
        }
        catch (Exception e)
        {
            throw ExpError("Failed to compute result."
                + "\nExpression form at evaluation: '{0}'"
                + "\n\nPrinting Error Message:\n{1}",
                exp, e.Message);
        }

        // Decl files use lowercase true/false
        // Variations in capitalization cause game crashes
        if (result.EqualsCCIC("true") || result.EqualsCCIC("false"))
            result = result.ToLower();
        return result;
    }

    public static string computeVarExpression(string exp)
    {
        return calculateResult(exp);
    }

    public static bool computeToggleExpression(string exp)
    {
        string rawResult = calculateResult(exp);
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
                throw ExpError("The expression does not evaluate"
                + " to a Boolean.\nExpression Result: '{0}'\n\n{1}", 
                rawResult, RULES_TOGGLE_RESULT);
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
            throw ExpError("The expression is missing "
                + "information required for a Loop Label.\n\n{0}",
                RULES_LOOPS);
        }

        // Evaluate the first two split strings into numbers
        // Lets Arithmetic Exceptions be thrown for catching outside this class
        stringStartNum = calculateResult(stringStartNum);
        stringEndNum   = calculateResult(stringEndNum);
        try
        {
            startNum = Convert.ToInt32(stringStartNum);
            endNum   = Convert.ToInt32(stringEndNum);
        }
        catch(System.Exception)
        {
            throw ExpError("The loop's start or end value "
                + "failed to evaluate to an integer.\n\n{0}",
                RULES_LOOPS);
        }
        if(startNum > endNum)
            throw ExpError("The loop's ending value must "
                + "be larger than it's starting value.\n\n{0}", 
                RULES_LOOPS);
        
        // Construct the final expression that will be substituted for the label
        options.Add(SYM_LOOP_INC, "");
        string expandedExp = "";
        for(int i = startNum; i <= endNum; i++)
        {
            options[SYM_LOOP_INC] = i.ToString();
            string currentExp = calculateResult(mainExp);
            expandedExp += currentExp;
        }
        options.Remove(SYM_LOOP_INC);
        return expandedExp;
    }

    private static EMBExpressionException ExpError(string msg, string arg0="", string arg1="", string arg2="")
    {
        string formattedMessage = String.Format(msg, arg0, arg1, arg2);
        return new EMBExpressionException(formattedMessage);
    }

    public class EMBExpressionException : EMBException
    {
        public EMBExpressionException(string msg) : base(msg) {}
    }
}