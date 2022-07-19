using static FileParser.Error;
class FileParser
{
    private static readonly Label emptyLabel = new Label(-1, -1, "", "");
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
            throw EMBError(INCOMPLETE_LABEL);
 
        string rawLabel = text.Substring(start, end - start + 1);
        Label label = new Label(start, end, rawLabel, path);

        label.splitLabel();
           
        return label;
    }

    private void parseLabel(Label label)
    {
        switch(label.type)
        {
            case LabelType.VAR:
            label.computeResult();
            parseVariable(label);
            break;

            case LabelType.TOGGLE_START:
            label.computeResult();
            parseToggle(label);
            break;

            case LabelType.TOGGLE_END:
            throw EMBError(EXTRA_END_TOGGLE);
        }
    }

    private void parseVariable(Label label)
    {
        text = text.Substring(0, label.start) + label.result 
            + text.Substring(label.end + 1, text.Length - label.end - 1);
    }

    private void parseToggle(Label start)
    {
        int numEndLabelsNeeded = 1; // Allows nested toggles
        int endStart = start.start;
        Label end = emptyLabel;
        while (numEndLabelsNeeded > 0)
        {
            endStart = text.IndexOf(LABEL_ANY_TOG, endStart + 1, CCIC);
            if (endStart == -1)
                throw EMBError(MISSING_END_TOGGLE, start.raw);
            end = buildLabel(endStart);

            // Non-toggle types get filtered out by the search string
            if(end.type == LabelType.TOGGLE_END)
                numEndLabelsNeeded--;
            else if (end.type == LabelType.TOGGLE_START)
                numEndLabelsNeeded++;
        }

        bool resultBool = start.resultToToggleBool();
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
        EXTRA_END_TOGGLE,
        MISSING_END_TOGGLE,
    }

    private EMBException EMBError(Error e, string arg0 = "")
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

            case EXTRA_END_TOGGLE:
            msg = "There is a '{0}' label with no preceding start label.\n\n{1}";
            args[0] = LABEL_END_TOG + LABEL_CHAR_SEPARATOR + LABEL_CHAR_BORDER;
            args[1] = RULES_TOGGLE_BLOCK;
            break;

            case MISSING_END_TOGGLE:
            msg = "The label '{0}' has no '{1}' label following it.\n\n{2}";
            args[0] = arg0; // Label raw text
            args[1] = LABEL_END_TOG + LABEL_CHAR_SEPARATOR + LABEL_CHAR_BORDER;
            args[2] = RULES_TOGGLE_BLOCK;
            break;
        }
        return EMBException.buildException(preamble + msg, args);
    }
}