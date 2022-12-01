class FileParser
{
    private string path, text;
    private int numBuildLabelCalls;
    private ParserLogMaker logger;

    public FileParser()
    {
        path = text = "";
        logger = new ParserLogMaker();
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
            EternalModBuilder.log(logger.getMessage());
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
                case LABEL_VAR:
                    expResult = parseVariable(label);
                break;

                case LABEL_TOGGLE_START:
                    expResult = parseToggle(label);
                break;

                case LABEL_LOOP:
                    expResult = parseLoop(label);
                break;

                case LABEL_TOGGLE_END:
                    throw ParseError(
                        "There is a '{0}' label with no preceding start label.\n\n{1}", 
                        DESC_LABEL_TOGGLE_END, RULES_TOGGLE_BLOCK);

                default:
                    throw ParseError(
                        "The label '{0}' has an unrecognized type. \n\n'{1}'", 
                        label.raw, DESC_LABEL_TYPES);
            }
        }
        catch(ExpressionHandler.EMBExpressionException e)
        {
            throw ParseError(
                "Failed to evaluate expression in label '{0}'\n{1}", 
                label.raw, e.Message);
        }

        if (logger.mustLog)
            logger.appendLabelResult(label, expResult);
    }

    private Label buildLabel(int start)
    {
        if(numBuildLabelCalls++ == PARSER_INFINITE_LOOP_THRESHOLD)
            throw ParseError(
                "Parsing this file's labels creates an infinite loop.");
        
        int end = text.IndexOf(LABEL_CHAR_BORDER, start + 1);
        if (end == -1)
            throw ParseError(
                "A label is missing a '{0}' on it's right side.\n\n{1}", 
                LABEL_CHAR_BORDER, RULES_LABEL_FORMAT);
 
        string rawLabel = text.Substring(start, end - start + 1);
        int separator = rawLabel.IndexOf(LABEL_CHAR_SEPARATOR);
        if (separator == -1)
            throw ParseError(
                "The label '{0}' has no '{1}' written after the type.\n\n{2}", 
                rawLabel, LABEL_CHAR_SEPARATOR, RULES_LABEL_FORMAT);

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
                throw ParseError(
                    "The label '{0}' has no '{1}' label following it.\n\n{2}", 
                    start.raw, DESC_LABEL_TOGGLE_END, RULES_TOGGLE_BLOCK);
            end = buildLabel(endStart);

            if(end.type.Equals(LABEL_TOGGLE_END))
                numEndLabelsNeeded--;
            else if(end.type.Equals(LABEL_TOGGLE_START))
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

    private string parseLoop(Label label)
    {
        string result = ExpressionHandler.computeLoopExpression(label.exp);
        text = text.Substring(0, label.start) + result
            + text.Substring(label.end + 1, text.Length - label.end - 1);
        return result;
    }

    private EMBParserException ParseError(string msg, string arg0="", string arg1="", string arg2="")
    {
        string preamble = String.Format(
            "Problem encountered in mod file '{0}'\n", path);
        string formattedMessage = String.Format(msg, arg0, arg1, arg2);
        return new EMBParserException(preamble + formattedMessage);
    }

    public class EMBParserException : EMBException
    {
        public EMBParserException(string msg) : base (msg){}
    }

    private class ParserLogMaker : LogMaker
    {
        public ParserLogMaker() : base(LogLevel.PARSINGS) {}

        public void startNewFileLog(string path)
        {
            logMsg.Clear();
            logMsg.Append("Parsing File '" + path + "'");
        }

        public void appendLabelResult(Label l, string result)
        {
            logMsg.Append("\n - Label '" + l.raw + "' resolved to '" + result + "'");
        }
    }
}