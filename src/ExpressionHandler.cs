using System.Data;
class EMBOptionDictionary : Dictionary<string, string>
{
    const string SYM_SUBEXP_START = "!sub";
    const string SYM_SUBEXP_START_B = "{" + SYM_SUBEXP_START + "}";
    const string SYM_SUBEXP_LOOP = "!subloop";
    const string SYM_SUBEXP_LOOP_B = "{" + SYM_SUBEXP_LOOP + "}";
    const string SYM_SUBEXP_END = "!subend";
    const string SYM_SUBEXP_END_B = "{" + SYM_SUBEXP_END + "}";

    const string LABEL_CHAR_LOOP_SEPARATOR = "&";
    const int EXP_INFINITE_LOOP_THRESHOLD = 500;
    const string NULL_EXP_RESULT = "NULL";

    const string SYM_LOOP_INC = "inc";

    const string RULES_SUBEXPRESSIONS = "A subexpression block:\n"
    + "- Starts with the symbol '" + SYM_SUBEXP_START_B + "' or '" + SYM_SUBEXP_LOOP_B
    + "'\n- Ends with the symbol '" + SYM_SUBEXP_END_B 
    + "'\nAnything inside a subexpression block will be fully evaluated before the rest of the expression.";

    const string RULES_LOOPS = "Loop subexpressions have the form "
    + SYM_SUBEXP_LOOP_B + "[Start]" + LABEL_CHAR_LOOP_SEPARATOR
    + "[Stop]" + LABEL_CHAR_LOOP_SEPARATOR + "[Expression]" + SYM_SUBEXP_END_B + " where:\n"
    + "- [Start] and [Stop] are expressions that evaluate to integers - You may NOT place loops inside of these.\n"
    + "- [Start] is less than or equal to [Stop]\n"
    + "- You may use '{[COUNT]" + SYM_LOOP_INC + "}' in [Expression] to get the value of the current loop iteration.\n"
    + "   > [COUNT] is a number of exclamation marks, corresponding to the number of nested loops.\n"
    + "When evaluated, a loop will repeat [Expression] once for every integer between [Start] and [Stop], inclusive.";

    const string RULES_TOGGLE_RESULT = "Expressions in toggle labels must yield one of these results:\n"
    + "- A Boolean (true/false) value, from a logical expression or from reading a string.\n"
    + "- A numerical value. A number less than one is interpeted as false, and one or higher is interpreted as true.";


    private DataTable computer = new DataTable();
    private int numLoops = 0;

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
                    exp = evalSubExpression(exp, openIndex, false);
                    openIndex = exp.IndexOf('{');
                    break;

                    case SYM_SUBEXP_LOOP:
                    exp = evalSubExpression(exp, openIndex, true);
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
        return result;
    }

    private string evalSubExpression(string exp, int startIndex, bool loops)
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
            if (endIndex == exp.IndexOfOIC(SYM_SUBEXP_END_B, endIndex))
                numEndsNeeded--;
            else if (endIndex == exp.IndexOfOIC(SYM_SUBEXP_START_B, endIndex)
                || endIndex == exp.IndexOfOIC(SYM_SUBEXP_LOOP_B, endIndex))
                numEndsNeeded++;
        }
        // Indices used in substring calculations
        int subExpIndex = startIndex + (loops ? SYM_SUBEXP_LOOP_B.Length : SYM_SUBEXP_START_B.Length);
        int postSubExp = endIndex + SYM_SUBEXP_END_B.Length;

        // Get sub-expression, and place result into expression
        string subExp = exp.Substring(subExpIndex, endIndex - subExpIndex);

        string subResult = loops ? computeLoopExpression(subExp) : calculateResult(subExp);

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
        numLoops++;

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
            mainExp = exp.Substring(indexTwo + 1);

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
        string incrementerVar = SYM_LOOP_INC.PadLeft(SYM_LOOP_INC.Length + numLoops, '!');
        this.Add(incrementerVar, "");
        string expandedExp = "";
        for(int i = startNum; i <= endNum; i++)
        {
            this[incrementerVar] = i.ToString();
            string currentExp = calculateResult(mainExp);
            expandedExp += currentExp;
        }
        this.Remove(incrementerVar);

        numLoops--;
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