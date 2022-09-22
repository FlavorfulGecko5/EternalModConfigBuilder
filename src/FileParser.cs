using static FileParser.Error;
class FileParser
{
    private string path, text;
    private int numBuildLabelCalls;
    private ParserLogMaker logger;

    public FileParser(List<Option> options)
    {
        path = text = "";
        logger = new ParserLogMaker();
        ExpressionHandler.setOptionList(options);
    }

    public void parseFile(string pathParameter)
    {
        path = pathParameter;
        text = FSUtil.readFileText(path);
        numBuildLabelCalls = 0;

        if(logger.mustLog)
            logger.startNewFileLog(path);

        // Labels are parsed sequentially by scanning the entire text file.
        int nextStartIndex = findNextLabelIndex(LABEL_ANY, 0);
        while (nextStartIndex != -1)
        {
            parseLabel(nextStartIndex);
            nextStartIndex = findNextLabelIndex(LABEL_ANY, nextStartIndex);
        }

        FSUtil.writeFile(path, text);
        
        if(logger.mustLog)
            logger.log();
    }

    private int findNextLabelIndex(string labelToFind, int searchStartIndex)
    {
        return text.IndexOfCCIC(labelToFind, searchStartIndex);
    }

    private void parseLabel(int startIndex)
    {
        Label label = buildLabel(startIndex);
        string expResult = "";
        try
        {
            switch (label.type)
            {
                case LabelType.VAR:
                    expResult = parseVariable(label);
                break;

                case LabelType.TOGGLE_START:
                    expResult = parseToggle(label);
                break;

                case LabelType.TOGGLE_END:
                    throw EMBError(EXTRA_END_TOGGLE);

                case LabelType.INVALID:
                    throw EMBError(BAD_LABEL_TYPE, label.raw);
            }
        }
        catch(ArithmeticException e)
        {
            throw EMBError(EXPRESSION_ERROR, label.raw, e.Message);
        }

        if (logger.mustLog)
            logger.appendLabelResult(label, expResult);
    }

    private Label buildLabel(int start)
    {
        if(numBuildLabelCalls++ == PARSER_INFINITE_LOOP_THRESHOLD)
            throw EMBError(PARSER_LOOPS_INFINITELY);
        
        NESTED_LABEL_LOOP:
        int end = text.IndexOf(LABEL_CHAR_BORDER, start + 1);
        if (end == -1)
            throw EMBError(INCOMPLETE_LABEL);   
        if(end == findNextLabelIndex(LABEL_VAR, start + 1))
        {
            parseLabel(end);
            goto NESTED_LABEL_LOOP;
        }
 
        string rawLabel = text.Substring(start, end - start + 1);
        int separator = rawLabel.IndexOf(LABEL_CHAR_SEPARATOR);
        if (separator == -1)
            throw EMBError(MISSING_EXP_SEPARATOR, rawLabel);

        Label label = new Label(start, end, rawLabel, separator);
        return label;
    }

    private string parseVariable(Label label)
    {
        string result = ExpressionHandler.computeVarExpression(label.exp);
        text = text.Substring(0, label.start) + result 
            + text.Substring(label.end + 1, text.Length - label.end - 1);
        return result;
    }

    private string parseToggle(Label start)
    {
        int numEndLabelsNeeded = 1; // Allows nested toggles
        int endStart = start.start;
        Label end = new Label();

        while(numEndLabelsNeeded > 0)
        {
            endStart = findNextLabelIndex(LABEL_TOGGLE_ANY, endStart + 1);
            if(endStart == -1)
                throw EMBError(MISSING_END_TOGGLE, start.raw);
            end = buildLabel(endStart);

            if(end.type == LabelType.TOGGLE_END)
                numEndLabelsNeeded--;
            else if(end.type == LabelType.TOGGLE_START)
                numEndLabelsNeeded++;
        }

        bool resultBool = ExpressionHandler.computeToggleExpression(start.exp);
        if (resultBool) // Keep what's in-between, remove the labels
            text = text.Substring(0, start.start)
                + text.Substring(start.end + 1, endStart - start.end - 1)
                + text.Substring(end.end + 1, text.Length - end.end - 1);
        else // Remove the labels and what's in-between them
            text = text.Substring(0, start.start)
                + text.Substring(end.end + 1, text.Length - end.end - 1);
        return resultBool.ToString();
    }

    public enum Error
    {
        INCOMPLETE_LABEL,
        PARSER_LOOPS_INFINITELY,
        MISSING_EXP_SEPARATOR,
        BAD_LABEL_TYPE,
        EXTRA_END_TOGGLE,
        MISSING_END_TOGGLE,
        EXPRESSION_ERROR
    }

    private EMBException EMBError(Error e, string arg0="", string arg1="")
    {
        string preamble = String.Format(
            "Problem encountered in mod file '{0}'\n",
            path
        );
        string msg = "";
        string[] args = {"", "", ""};
        switch(e)
        {
            case INCOMPLETE_LABEL:
            msg = "A label is missing a '{0}' on it's right side.\n\n{1}";
            args[0] = LABEL_CHAR_BORDER;
            args[1] = RULES_LABEL_FORMAT;
            break;

            case MISSING_EXP_SEPARATOR:
            msg = "The label '{0}' has no '{1}' written after the type.\n\n{2}";
            args[0] = arg0;
            args[1] = LABEL_CHAR_SEPARATOR;
            args[2] = RULES_LABEL_FORMAT;
            break;

            case PARSER_LOOPS_INFINITELY:
            msg = "Parsing this file's labels creates an infinite loop.";
            break;

            case BAD_LABEL_TYPE:
            msg = "The label '{0}' has an unrecognized type. \n\n'{1}'";
            args[0] = arg0;
            args[1] = DESC_LABEL_TYPES;
            break;

            case EXTRA_END_TOGGLE:
            msg = "There is a '{0}' label with no preceding start label.\n\n{1}";
            args[0] = DESC_LABEL_TOGGLE_END;
            args[1] = RULES_TOGGLE_BLOCK;
            break;

            case MISSING_END_TOGGLE:
            msg = "The label '{0}' has no '{1}' label following it.\n\n{2}";
            args[0] = arg0;
            args[1] = DESC_LABEL_TOGGLE_END;
            args[2] = RULES_TOGGLE_BLOCK;
            break;

            case EXPRESSION_ERROR:
            msg = "Failed to evaluate expression in label '{0}'\n{1}";
            args[0] = arg0;
            args[1] = arg1; // Error Message
            break;
        }
        // Prevents System.Format exception from label syntax
        // (Technically still possible in other EMBError functions, but
        // should not realistically happen)
        string formattedMsg = String.Format(msg, args);
        return new EMBException(preamble + formattedMsg);
    }
}