using static FileParser.Error;
class FileParser
{
    private static readonly Label emptyLabel = new Label(-1, -1, "");
    private string path, text;

    public FileParser(List<Option> options)
    {
        path = text = "";
        Label.setOptionList(options);
    }

    public void parseFile(string pathParameter)
    {
        path = pathParameter;
        text = FileUtil.readFileText(path);

        // Labels are parsed sequentially by scanning the entire text file.
        int nextStartIndex = text.IndexOf(LABEL_ANY, CCIC);
        while (nextStartIndex != -1)
        {
            Label label = buildLabel(nextStartIndex);
            parseLabel(label);
            nextStartIndex = text.IndexOf(LABEL_ANY, nextStartIndex, CCIC);
        }

        FileUtil.writeFile(path, text);
    }

    private Label buildLabel(int start)
    {
        int end = text.IndexOf(LABEL_CHAR_BORDER, start + 1);
        if (end == -1)
            ThrowError(INCOMPLETE_LABEL, emptyLabel);
 
        string rawLabel = text.Substring(start, end - start + 1);
        Label label = new Label(start, end, rawLabel);
        
        bool successfullySplitLabel = label.splitLabel();
        if (!successfullySplitLabel)
            ThrowError(MISSING_EXP_SEPARATOR, label);
           
        return label;
    }

    private void parseLabel(Label label)
    {
        switch(label.type)
        {
            case LabelType.VAR:
            parseExpression(label);
            parseVariable(label);
            break;

            case LabelType.TOGGLE_START:
            parseExpression(label);
            parseToggle(label);
            break;

            case LabelType.TOGGLE_END:
            ThrowError(EXTRA_END_TOGGLE, emptyLabel);
            break;

            case LabelType.INVALID:
            ThrowError(BAD_TYPE, label);
            break;
        }
    }

    private void parseExpression(Label label)
    {
        bool expressionLoopedInfinitely = false;
        try
        {
            expressionLoopedInfinitely = label.computeResult();
        }
        catch(Exception e)
        {
            ThrowError(CANT_EVAL_EXP, label, e.Message);
        }

        if(expressionLoopedInfinitely)
            ThrowError(EXP_LOOPS_INFINITELY, label);
    }

    private void parseVariable(Label label)
    {
        text = text.Substring(0, label.start) + label.result 
            + text.Substring(label.end + 1, text.Length - label.end - 1);
    }

    private void parseToggle(Label start)
    {
        // Allows nested toggles
        int numEndLabelsNeeded = 1;

        int endStart = start.start;
        Label end = emptyLabel;
        while (numEndLabelsNeeded > 0)
        {
            endStart = text.IndexOf(LABEL_ANY_TOG, endStart + 1, CCIC);
            if (endStart == -1)
                ThrowError(MISSING_END_TOGGLE, start);
            end = buildLabel(endStart);
            switch(end.type)
            {
                case LabelType.TOGGLE_START:
                numEndLabelsNeeded++;
                break;

                case LabelType.TOGGLE_END:
                numEndLabelsNeeded--;
                break;

                default:
                ThrowError(BAD_TOGGLE_TYPE, end);
                break;
            }
        }

        // TODO - Fix this up with exception system improvements
        bool resultBool = false;
        bool? tempResultBool = start.resultToBool();
        if(tempResultBool == null)
            ThrowError(BAD_TOGGLE_EXP_RESULT, start);
        else
            resultBool = (bool)tempResultBool;

        if (resultBool) // Keep what's in-between, remove the labels
            text = text.Substring(0, start.start)
                + text.Substring(start.end + 1, endStart - start.end - 1)
                + text.Substring(end.end + 1, text.Length - end.end - 1);
        else // Remove the labels and what's in-between them
            text = text.Substring(0, start.start) 
                + text.Substring(end.end + 1, text.Length - end.end - 1);
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

    private void ThrowError(Error error, Label label, string arg0 = "")
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
                label.raw,
                LABEL_CHAR_SEPARATOR,
                RULES_LABEL_FORMAT
            );
            break;

            case BAD_TYPE:
            msg += String.Format(
                "The label '{0}' has an unrecognized type.\n\n{1}",
                label.raw,
                RULES_LABEL_FORMAT
            );
            break;

            case EXP_LOOPS_INFINITELY:
            msg += String.Format(
                "The expression in '{0}' loops infinitely when inserting Option"
                + " values.\nLast edited form of the expression: '{1}'",
                label.raw,
                label.exp
            );
            break;

            case CANT_EVAL_EXP:
            msg += String.Format(
                "The expression in '{0}' failed to evaluate."
                + "\nExpression form at evaluation: '{1}'"
                + "\n\nPrinting Error Message:\n{2}",
                label.raw,
                label.exp,
                arg0 // Exception message
            );
            break;

            case EXTRA_END_TOGGLE:
            msg += String.Format(
                "There is a '{0}' label with no preceding start label.\n\n{1}",
                LABEL_END_TOG + LABEL_CHAR_SEPARATOR + LABEL_CHAR_BORDER,
                RULES_TOGGLE_BLOCK
            );
            break;

            case MISSING_END_TOGGLE:
            msg += String.Format(
                "The label '{0}' has no '{1}' label following it.\n\n{2}",
                label.raw,
                LABEL_END_TOG + LABEL_CHAR_SEPARATOR + LABEL_CHAR_BORDER,
                RULES_TOGGLE_BLOCK
            );
            break;

            case BAD_TOGGLE_TYPE:
            msg += String.Format(
                "There is an invalid toggle label '{0}'\n\n{1}",
                label.raw, // The label
                RULES_LABEL_FORMAT
            );
            break;

            case BAD_TOGGLE_EXP_RESULT:
            msg += String.Format(
                "The expression in '{0}' cannot be evaluated into a Boolean."
                + "\nExpression Result: '{1}'\n\n{2}",
                label.raw,
                label.exp,
                RULES_TOGGLE_EXP
            );
            break;
        }
        reportError(msg);
    }
}