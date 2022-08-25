using static FileParser.Error;
using static RuntimeConfig.LogLevel;
using System.Text;
class FileParser
{
    private string path, text;
    private int numBuildLabelCalls;
    private StringBuilder log;

    public FileParser(List<Option> options)
    {
        path = text = "";
        log = new StringBuilder();
        Label.setOptionList(options);
    }

    public void parseFile(string pathParameter)
    {
        path = pathParameter;
        text = FileUtil.readFileText(path);
        numBuildLabelCalls = 0;
        
        if(RuntimeConfig.logMode == PARSINGS || RuntimeConfig.logMode == VERBOSE)
        {
            log.Clear();
            log.Append("Parsing File '" + path + "'");
        }

        // Labels are parsed sequentially by scanning the entire text file.
        int nextStartIndex = findNextLabelIndex(0);
        while (nextStartIndex != -1)
        {
            parseLabel(nextStartIndex);
            nextStartIndex = findNextLabelIndex(nextStartIndex);
        }

        FileUtil.writeFile(path, text);
        
        if(RuntimeConfig.logMode == PARSINGS || RuntimeConfig.logMode == VERBOSE)
            RuntimeManager.log(log.ToString());
    }

    private int findNextLabelIndex(int searchStartIndex)
    {
        return text.IndexOf(LABEL_ANY, searchStartIndex, CCIC);
    }

    private void parseLabel(int startIndex)
    {
        Label label = buildLabel(startIndex);
        label.split();
        label.computeResult();
        
        if (RuntimeConfig.logMode == PARSINGS || RuntimeConfig.logMode == VERBOSE)
            log.Append("\n   - Label '" + label.raw + "' resolved to '" + label.result + "'");

        switch (label.type)
        {
            case LabelType.VAR:
                parseVariable(label);
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
        if(end == findNextLabelIndex(start + 1))
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

    public enum Error
    {
        INCOMPLETE_LABEL,
        PARSER_LOOPS_INFINITELY,
    }

    private EMBException EMBError(Error e)
    {
        string preamble = String.Format(
            "Problem encountered in mod file '{0}'\n",
            path
        );
        string msg = "";
        string[] args = {"", ""};
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
        }
        return EMBException.buildException(preamble + msg, args);
    }
}