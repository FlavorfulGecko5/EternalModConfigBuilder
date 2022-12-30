using System.Text;
class FileParser
{
    private class Label
    {
        public int    start { get; private set; }
        public int    end   { get; private set; }
        public string raw   { get; private set; }
        public string type  { get; private set; }
        public string exp   { get; private set; }

        public Label(Label copyFrom) 
        { 
            start = copyFrom.start;
            end = copyFrom.end;
            raw = copyFrom.raw;
            type = copyFrom.type;
            exp = copyFrom.exp;
        }

        public Label(int startParm, int endParm, string labelParm)
        {
            start = startParm;
            end = endParm;
            raw = labelParm;

            // Instead of throwing error for missing separator, assume the whole
            // label body is the type. This allows for labels to omit an expression
            // if it isn't used (such as for end-toggle labels)
            int separatorIndex = raw.IndexOf(LABEL_CHAR_SEPARATOR);
            if(separatorIndex == -1)
            {
                type = raw.Substring(0, raw.Length - LABEL_CHAR_BORDER.Length).ToUpper();
                exp = "";
            }
            else
            {
                // Excludes separator index. Capitalize for switch comparisons
                type = raw.Substring(0, separatorIndex).ToUpper();
                exp = raw.Substring(separatorIndex + 1, raw.Length - separatorIndex - 2);
            }
        }
    }

    private string path;
    private StringBuilder text;
    private int numBuildLabelCalls;
    private ParserLogMaker logger;
    private EMBOptionDictionary expHandler;

    public FileParser(EMBOptionDictionary options)
    {
        path = "";
        text = new StringBuilder();
        logger = new ParserLogMaker();
        expHandler = options;
    }

    public void parseFile(string pathParameter)
    {
        path = pathParameter;
        text.Clear();
        text.Append(File.ReadAllText(path));
        numBuildLabelCalls = 0;

        if(logger.mustLog)
            logger.startNewFileLog(path);

        // Labels are parsed sequentially by scanning the entire text file.
        Label? nextLabel = buildLabel(LABEL_ANY, 0);
        while (nextLabel != null)
        {
            parseLabel(nextLabel);
            nextLabel = buildLabel(LABEL_ANY, nextLabel.start);
        }

        /*
        * .decl files true/false for bool assignments, variations cause crashes
        * C# uses True/False for representing bools as strings
        * 
        * This algorithm is necessary to fixup expressions that affect
        * bool assignment statements without putting the burden on the user
        * to work around it. 
        *
        * Possible TODO: Ensure this only executes on .decl and .entities files
        */
        for(int i = 0; i < text.Length; i++)
        {
            if(text[i] != '=')
                continue;
            for(int j = i + 1; j < text.Length; j++)
            {
                if(Char.IsWhiteSpace(text[j]))
                    continue;
                else if(text[j] == 'T' || text[j] == 'F')
                    text[j] = (char)(text[j] + 32);
                break;
            }
        }

        File.WriteAllText(path, text.ToString());
        
        if(logger.mustLog)
            EternalModBuilder.log(logger.getMessage());
    }

    private void parseLabel(Label label)
    {
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
        catch(EMBOptionDictionary.EMBExpressionException e)
        {
            throw ParseError(
                "Failed to evaluate expression in label '{0}'\n{1}", 
                label.raw, e.Message);
        }

        if (logger.mustLog)
            logger.appendLabelResult(label, expResult);
    }

    private Label? buildLabel(string labelToFind, int searchStartIndex)
    {
        string rawText = text.ToString();
        int start = rawText.IndexOfOIC(labelToFind, searchStartIndex);
        if(start == -1)
            return null;
        
        int end = rawText.IndexOf(LABEL_CHAR_BORDER, start + 1);
        if (end == -1)
            throw ParseError(
                "A label is missing a '{0}' on it's right side.\n\n{1}", 
                LABEL_CHAR_BORDER, RULES_LABEL_FORMAT);
 
        string rawLabel = rawText.Substring(start, end - start + 1);

        Label label = new Label(start, end, rawLabel);
        return label;
    }

    private string parseVariable(Label label)
    {
        string result = expHandler.computeVarExpression(label.exp);
        text.Replace(label.raw, result, label.start, label.raw.Length);
        return result;
    }

    private string parseToggle(Label start)
    {
        int numEndLabelsNeeded = 1; // Allows nested toggles
        Label? end = new Label(start);

        while(numEndLabelsNeeded > 0)
        {
            end = buildLabel(LABEL_TOGGLE_ANY, end.start + 1);
            if(end == null)
                throw ParseError(
                    "The label '{0}' has no '{1}' label following it.\n\n{2}", 
                    start.raw, DESC_LABEL_TOGGLE_END, RULES_TOGGLE_BLOCK);

            if(end.type.Equals(LABEL_TOGGLE_END))
                numEndLabelsNeeded--;
            else if(end.type.Equals(LABEL_TOGGLE_START))
                numEndLabelsNeeded++;
        }

        bool resultBool = expHandler.computeToggleExpression(start.exp);
        if (resultBool) // Keep what's in-between, remove the labels
        {
            text.Remove(end.start, end.raw.Length);
            text.Remove(start.start, start.raw.Length);
        }
        else // Remove the labels and what's in-between them
            text.Remove(start.start, end.end - start.start + 1);
        return resultBool.ToString();
    }

    private string parseLoop(Label label)
    {
        string result = expHandler.computeLoopExpression(label.exp);
        text.Replace(label.raw, result, label.start, label.raw.Length);
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