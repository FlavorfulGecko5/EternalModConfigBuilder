using System.Data;
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
        StreamReader reader = new StreamReader(path);
        text = reader.ReadToEnd();
        reader.Close();

        // Labels are parsed sequentially by scanning the entire text file.
        start = text.IndexOf(LABEL_ANY, CCIC);
        while (start != -1)
            start = parseLabel();

        using (StreamWriter fileWriter = new StreamWriter(path))
            fileWriter.Write(text);
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
                ProcessErrorCode(BAD_TYPE, path, label);
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
                ProcessErrorCode(EXP_LOOPS_INFINITELY, path, label, exp);
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
            ProcessErrorCode(CANT_EVAL_EXP, path, label, exp, e.Message); 
        }

        if(result == null)
            return NULL_EXP_RESULT;

        // Decl files use lowercase true/false
        // Variations in capitalization cause game crashes
        if(result.Equals("true", CCIC) || result.Equals("false", CCIC))
            result = result.ToLower(); // 

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
                ProcessErrorCode(MISSING_END_TOGGLE, path, label);
            endEnd = findLabelEndIndex(endStart);

            if (text.IndexOf(LABEL_END_TOG, endStart, CCIC) == endStart)
                numEndLabelsNeeded--;
            else if (text.IndexOf(LABEL_START_TOG, endStart, CCIC) == endStart)
                numEndLabelsNeeded++;
            else
            {
                string endLabel = getLabel(endStart, endEnd);
                ProcessErrorCode(BAD_TOGGLE_TYPE, path, endLabel);
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
                ProcessErrorCode(BAD_TOGGLE_EXP_RESULT, path, label, result); 
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
        int index = text.IndexOf(LABEL_BORDER_VALUE, startIndex + 1);
        if (index == -1)
            ProcessErrorCode(INCOMPLETE_LABEL, path);
        return index;
    }

    private string getLabel(int startIndex, int endIndex)
    {
        return text.Substring(startIndex, endIndex - startIndex + 1);
    }

    private int findExpressionSeparatorIndex()
    {
        int separator = label.IndexOf(LABEL_NAME_EXP_SEPARATOR);
        if (separator == -1)
        {
            // A toggle-end label with no preceding toggle-start label
            if (label.Equals(LABEL_END_TOG, CCIC))
                ProcessErrorCode(EXTRA_END_TOGGLE, path);
            else
                ProcessErrorCode(MISSING_EXP_SEPARATOR, path, label);
        }
        return separator;
    }
}