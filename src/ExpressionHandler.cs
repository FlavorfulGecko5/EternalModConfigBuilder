using System.Data;
class EMBOptionDictionary : Dictionary<string, string>
{
    const string SYM_SUBEXP_START = "!sub";
    const string SYM_SUBEXP_START_B = "{" + SYM_SUBEXP_START + "}";
    const string SYM_SUBEXP_LOOP = "!subloop";
    const string SYM_SUBEXP_LOOP_B = "{" + SYM_SUBEXP_LOOP + "}";
    const string SYM_SUBEXP_END = "!subend";
    const string SYM_SUBEXP_END_B = "{" + SYM_SUBEXP_END + "}";

    const string RULES_SUBEXPRESSIONS = "A subexpression block:\n"
    + "- Starts with the symbol '" + SYM_SUBEXP_START_B + "' or '" + SYM_SUBEXP_LOOP_B
    + "'\n- Ends with the symbol '" + SYM_SUBEXP_END_B 
    + "'\nAnything inside a subexpression block will be fully evaluated before the rest of the expression.";

    private DataTable computer = new DataTable();

    public EMBOptionDictionary() : base(StringComparer.OrdinalIgnoreCase) {}

    private string calculateResult(string exp)
    {
        // PHASE 1 - Substitute Variables
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
                switch(name)
                {
                    case SYM_SUBEXP_START:
                    exp = evalSubExpression(exp, openIndex);
                    openIndex = exp.IndexOf('{');
                    break;

                    case SYM_SUBEXP_END:
                        throw ExpError(
                            "There is a '{0}' symbol with no starting symbol preceding it.\n\n{1}",
                            SYM_SUBEXP_END_B, RULES_SUBEXPRESSIONS);

                    default:
                        if (ContainsKey(name))
                        {
                            exp = exp.Substring(0, openIndex) + this[name] + exp.Substring(closeIndex + 1);
                            openIndex = exp.IndexOf('{');

                            if (numIterations++ == EXP_INFINITE_LOOP_THRESHOLD)
                                throw ExpError(
                                    "The expression loops infinitely when inserting Option values."
                                    + "\nLast edited form of the expression: '{0}'", exp);
                        }
                        else
                            openIndex = nextOpenIndex;
                    break;
                }
            }
        }

        // PHASE 2 - Calculate Result
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
        if (result.EqualsOIC("true") || result.EqualsOIC("false"))
            result = result.ToLower();
        return result;
    }

    private string evalSubExpression(string exp, int startIndex)
    {
        int endIndex = startIndex, numEndsNeeded = 1;
        while (numEndsNeeded > 0)
        {
            // This algorithm functions by assuming all sub labels start
            // with the same set of characters
            endIndex = exp.IndexOfOIC('{' + SYM_SUBEXP_START, endIndex + 1);
            if (endIndex == -1)
                throw ExpError(
                    "There is a subexpression-starting symbol with no '{0}' symbol following it.\n\n{1}",
                    SYM_SUBEXP_END_B, RULES_SUBEXPRESSIONS);
            if (endIndex == exp.IndexOfOIC(SYM_SUBEXP_START_B, endIndex))
                numEndsNeeded++;
            else if (endIndex == exp.IndexOfOIC(SYM_SUBEXP_END_B, endIndex))
                numEndsNeeded--;
        }
        // Indices used in substring calculations
        int subExpIndex = startIndex + SYM_SUBEXP_START_B.Length;
        int postSubExp = endIndex + SYM_SUBEXP_END_B.Length;

        // Get sub-expression, and place result into expression
        string subExp = exp.Substring(subExpIndex, endIndex - subExpIndex);
        string subResult = calculateResult(subExp);
        exp = exp.Substring(0, startIndex) + subResult
            + exp.Substring(postSubExp, exp.Length - postSubExp);

        return exp;
    }

    public string computeVarExpression(string exp)
    {
        return calculateResult(exp);
    }

    public bool computeToggleExpression(string exp)
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

    public string computeLoopExpression(string exp)
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
        this.Add(SYM_LOOP_INC, "");
        string expandedExp = "";
        for(int i = startNum; i <= endNum; i++)
        {
            this[SYM_LOOP_INC] = i.ToString();
            string currentExp = calculateResult(mainExp);
            expandedExp += currentExp;
        }
        this.Remove(SYM_LOOP_INC);
        return expandedExp;
    }

    private EMBExpressionException ExpError(string msg, string arg0 = "", string arg1 = "", string arg2 = "")
    {
        string formattedMessage = String.Format(msg, arg0, arg1, arg2);
        return new EMBExpressionException(formattedMessage);
    }

    public class EMBExpressionException : EMBException
    {
        public EMBExpressionException(string msg) : base(msg) { }
    }
}