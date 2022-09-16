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
        Label.setOptionList(options);
    }

    public void parseFile(string pathParameter)
    {
        path = pathParameter;
        text = FileUtil.readFileText(path);
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

        FileUtil.writeFile(path, text);
        
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

        if(label.type == LabelType.TOGGLE_END)
            throw EMBError(EXTRA_END_TOGGLE);

        label.computeResult();
        if(logger.mustLog)
            logger.appendLabelResult(label);

        switch (label.type)
        {
            case LabelType.VAR:
                parseVariable(label);
            break;
            
            case LabelType.TOGGLE_START:
                parseToggle(label);
            break;
        }
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
        Label label = new Label(start, end, rawLabel, path);
        
        return label;
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
        PARSER_LOOPS_INFINITELY,
        EXTRA_END_TOGGLE,
        MISSING_END_TOGGLE
    }

    private EMBException EMBError(Error e, string rawLabel="")
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

            case PARSER_LOOPS_INFINITELY:
            msg = "Parsing this file's labels creates an infinite loop.";
            break;

            case EXTRA_END_TOGGLE:
            msg = "There is a '{0}' label with no preceding start label.\n\n{1}";
            args[0] = DESC_LABEL_TOGGLE_END;
            args[1] = RULES_TOGGLE_BLOCK;
            break;

            case MISSING_END_TOGGLE:
            msg = "The label '{0}' has no '{1}' label following it.\n\n{2}";
            args[0] = rawLabel;
            args[1] = DESC_LABEL_TOGGLE_END;
            args[2] = RULES_TOGGLE_BLOCK;
            break;
        }
        string formattedMsg = String.Format(msg, args);
        return new EMBException(preamble + formattedMsg);
    }
}