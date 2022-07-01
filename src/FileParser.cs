using System.Data;
using static FileParser.Error;
class FileParser
{
    private List<Option> options;
    private DataTable computer;

    // Prevents unnecessary passing by value
    private string path, text, label, exp, result;
    private int start, end;

    public FileParser(List<Option> optionsParameter)
    {
        options = optionsParameter;
        computer = new DataTable();
        path = text = label = exp = result = ""; 
        start = end = 0;
    }

    public void parseFile(string pathParameter)
    {
        path = pathParameter;
        text = FileUtil.readFileText(path);

        // Labels are parsed sequentially by scanning the entire text file.
        start = text.IndexOf(LABEL_ANY, CCIC);
        while (start != -1)
            start = parseLabel();

        FileUtil.writeFile(path, text);
    }

    private int parseLabel()
    {
        end = findLabelEndIndex(start);
        label = getLabel(start, end);

        int separator = findExpressionSeparatorIndex();
        exp = label.Substring(separator + 1, label.Length - separator - 2);
        result = parseExpression();

        // Excludes the separator index. Capitalize for switch comparisons
        string type = label.Substring(0, separator).ToUpper();
        switch (type)
        {
            case LABEL_ANY_VARIABLE:
                parseVariable();
                break;
            case LABEL_ANY_TOG:
                parseToggle();
                break;
            default:
                ThrowError(BAD_TYPE);
                break;
        }
        // Starts search from the location of the previous label.
        return text.IndexOf(LABEL_ANY, start, CCIC);
    }

    private string parseExpression()
    {
        int numIterations = 0;         // Prevents infinite loops
        bool replacedThisCycle = true; // Allows nested variables
        while (replacedThisCycle)
        {
            if (numIterations++ == INFINITE_LOOP_THRESHOLD)
                ThrowError(EXP_LOOPS_INFINITELY);
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

        string? result = "";
        try 
        { 
            result = computer.Compute(exp, "").ToString(); 
        }
        catch (Exception e)
        { 
            ThrowError(CANT_EVAL_EXP, e.Message); 
        }

        if(result == null)
            return NULL_EXP_RESULT;

        // Decl files use lowercase true/false
        // Variations in capitalization cause game crashes
        if(result.Equals("true", CCIC) || result.Equals("false", CCIC))
            result = result.ToLower(); 

        return result;
    }

    private void parseVariable()
    {
        text = text.Substring(0, start) + result 
            + text.Substring(end + 1, text.Length - end - 1);
    }

    private void parseToggle()
    {
        // Allows nested toggles
        int numEndLabelsNeeded = 1;
        bool resultBool = false;

        int endStart = start, endEnd = 0;
        while (numEndLabelsNeeded > 0)
        {
            endStart = text.IndexOf(LABEL_ANY_TOG, endStart + 1, CCIC);
            if (endStart == -1)
                ThrowError(MISSING_END_TOGGLE);
            endEnd = findLabelEndIndex(endStart);

            if (text.IndexOf(LABEL_END_TOG, endStart, CCIC) == endStart)
                numEndLabelsNeeded--;
            else if (text.IndexOf(LABEL_START_TOG, endStart, CCIC) == endStart)
                numEndLabelsNeeded++;
            else
            {
                string endLabel = getLabel(endStart, endEnd);
                ThrowError(BAD_TOGGLE_TYPE, endLabel);
            }   
        }

        // Try to resolve the expression result to a Boolean
        try 
        { 
            resultBool = Convert.ToBoolean(result); 
        }
        catch (System.FormatException)
        {
            try 
            { 
                resultBool = Convert.ToDouble(result) >= 1 ? true : false; 
            }
            catch (Exception)
            { 
                ThrowError(BAD_TOGGLE_EXP_RESULT); 
            }
        }

        if (resultBool) // Keep what's in-between, remove the labels
            text = text.Substring(0, start)
                + text.Substring(end + 1, endStart - end - 1)
                + text.Substring(endEnd + 1, text.Length - endEnd - 1);
        else // Remove the labels and what's in-between them
            text = text.Substring(0, start) 
                + text.Substring(endEnd + 1, text.Length - endEnd - 1); 
    }

    private int findLabelEndIndex(int startIndex)
    {
        int index = text.IndexOf(LABEL_CHAR_BORDER, startIndex + 1);
        if (index == -1)
            ThrowError(INCOMPLETE_LABEL);
        return index;
    }

    private string getLabel(int startIndex, int endIndex)
    {
        return text.Substring(startIndex, endIndex - startIndex + 1);
    }

    private int findExpressionSeparatorIndex()
    {
        int separator = label.IndexOf(LABEL_CHAR_SEPARATOR);
        if (separator == -1)
        {
            // A toggle-end label with no preceding toggle-start label
            if (label.Equals(LABEL_END_TOG, CCIC))
                ThrowError(EXTRA_END_TOGGLE);
            else
                ThrowError(MISSING_EXP_SEPARATOR);
        }
        return separator;
    }

    public enum Error
    {
        INCOMPLETE_LABEL,
        MISSING_EXP_SEPARATOR,
        BAD_TYPE,
        EXP_LOOPS_INFINITELY,
        CANT_EVAL_EXP,
        EXTRA_END_TOGGLE,
        MISSING_END_TOGGLE,
        BAD_TOGGLE_TYPE,
        BAD_TOGGLE_EXP_RESULT
    }

    private void ThrowError(Error error, string arg0 = "")
    {
        string msg = String.Format(
            "Problem encountered when parsing mod file '{0}'\n",
            path
        );
        switch(error)
        {
            case INCOMPLETE_LABEL:
            msg += String.Format(
                "A label is missing a '{0}' on it's right side.\n\n{1}",
                LABEL_CHAR_BORDER,
                RULES_LABEL_FORMAT
            );
            break;

            case MISSING_EXP_SEPARATOR:
            msg += String.Format(
                "The label '{0}' has no '{1}' written after it's type.\n\n{2}",
                label,
                LABEL_CHAR_SEPARATOR,
                RULES_LABEL_FORMAT
            );
            break;

            case BAD_TYPE:
            msg += String.Format(
                "The label '{0}' has an unrecognized type.\n\n{1}",
                label,
                RULES_LABEL_FORMAT
            );
            break;

            case EXP_LOOPS_INFINITELY:
            msg += String.Format(
                "The expression in '{0}' loops infinitely when inserting Option"
                + " values.\nLast edited form of the expression: '{1}'",
                label,
                exp
            );
            break;

            case CANT_EVAL_EXP:
            msg += String.Format(
                "The expression in '{0}' failed to evaluate."
                + "\nExpression form at evaluation: '{1}'"
                + "\n\nPrinting Error Message:\n{2}",
                label,
                exp,
                arg0 // Exception message
            );
            break;

            case EXTRA_END_TOGGLE:
            msg += String.Format(
                "There is a '{0}' label with no preceding start label.\n\n{1}",
                LABEL_END_TOG,
                RULES_TOGGLE_BLOCK
            );
            break;

            case MISSING_END_TOGGLE:
            msg += String.Format(
                "The label '{0}' has no '{1}' label following it.\n\n{2}",
                label,
                LABEL_END_TOG,
                RULES_TOGGLE_BLOCK
            );
            break;

            case BAD_TOGGLE_TYPE:
            msg += String.Format(
                "There is an invalid toggle label '{0}'\n\n{1}",
                arg0, // The label
                RULES_LABEL_FORMAT
            );
            break;

            case BAD_TOGGLE_EXP_RESULT:
            msg += String.Format(
                "The expression in '{0}' cannot be evaluated into a Boolean."
                + "\nExpression Result: '{1}'\n\n{2}",
                label,
                exp,
                RULES_TOGGLE_EXP
            );
            break;
        }
        reportError(msg);
    }
}