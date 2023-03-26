using JSONEval.ExpressionEvaluation;
using System.Text;
class FileParser
{
    private class Label
    {
        public const char CHAR_BORDER    = '$';
        public const char CHAR_SEPARATOR = '#';

        public const string TYPE_ANY          = "EMB_";
        public const string TYPE_VAR          = TYPE_ANY    + "VAR";
        public const string TYPE_TOGGLE       = TYPE_ANY    + "TOGGLE";
        public const string TYPE_TOGGLE_START = TYPE_TOGGLE;
        public const string TYPE_TOGGLE_END   = TYPE_TOGGLE + "_END";
        public const string TYPE_COMMENT      = TYPE_ANY    + "COMMENT";

        public const string DESC_TYPES = "The current valid types for labels are:\n"
        + "- 'EMB_VAR'\n"
        + "- 'EMB_TOGGLE'\n"
        + "- 'EMB_TOGGLE_END'\n"
        + "- 'EMB_COMMENT'";

        public static readonly string DESC_END_TOGGLE = CHAR_BORDER + TYPE_TOGGLE_END + CHAR_BORDER;

        public static readonly string RULES_FORMAT = 
        "Labels must have the form "
        + CHAR_BORDER + "[TYPE]" + CHAR_SEPARATOR + "[EXPRESSION]" + CHAR_BORDER + " where:\n"
        + "- [TYPE] is a pre-defined string - see examples that show all types.\n"
        + "- [EXPRESSION] is a valid arithmetic or logical expression - see examples.\n"
        + "- To insert an option from your config. files into an expression, use the notation {NAME}\n"
        + "- Case-insensitivity of all label elements is allowed.\n"
        + "- If the '" + CHAR_SEPARATOR + "' is omitted, the expression is assumed empty.";

        public static readonly string RULES_TOGGLE_BLOCK = "Each toggle label must have exactly one '"
        + DESC_END_TOGGLE + "' label placed after it.\n"
        + "These two labels define the toggle-block controlled by the expression.";



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
            int separatorIndex = raw.IndexOf(CHAR_SEPARATOR);
            if(separatorIndex == -1)
            {
                type = raw.Substring(1, raw.Length - 2).ToUpper();
                exp = "";
            }
            else
            {
                type = raw.Substring(1, separatorIndex - 1).ToUpper();
                exp = raw.Substring(separatorIndex + 1, raw.Length - separatorIndex - 2);
            }
        }
    }

    public StringBuilder log = new StringBuilder();
    private StringBuilder text = new StringBuilder();
    private string path = "";

    /// <summary>
    /// Constructs a Label object by parsing data from the current file.
    /// </summary>
    /// <param name="type">Label string to search for</param>
    /// <param name="searchStartIndex">
    /// Index to start searching from.
    /// Assumed to not be inside the expression of another label
    /// </param>
    /// <returns>
    /// Object representing the first Label found of the desired.
    /// Null is returned if no Label of the desired type is found.
    /// </returns>
    private Label? buildLabelNew(string type, int searchStartIndex)
    {
        string rawText = text.ToString();
        int start = searchStartIndex;

        while(true)
        {
            start = rawText.IndexOfOIC(Label.CHAR_BORDER + Label.TYPE_ANY, start);
            if(start == -1)
                return null;
            int end = -1;

            // Find the end of the label - ensuring no string literal labels
            // inside of expressions are parsed.
            bool inString = false;
            for(int i = start + 1; i < rawText.Length && end == -1; i++)
                switch(rawText[i])
                {
                    case Label.CHAR_BORDER:
                    if(!inString)
                        end = i;
                    break;

                    case '\'':
                    if(!inString)
                        inString = true;
                    else if(rawText[i -1] != '`')
                        inString = false;
                    break;
                }
            if(end == -1)
                throw ParseError("A label is missing a '{0}' on it's right side.\n\n{1}",
                    Label.CHAR_BORDER.ToString(), Label.RULES_FORMAT);
            
            if(start == rawText.IndexOfOIC(Label.CHAR_BORDER + type, start))
            {
                string rawLabel = rawText.Substring(start, end - start + 1);
                return new Label(start, end, rawLabel);
            }
            else
                start = end;
        }
    }

    public void parseFile(string pathParameter)
    {
        path = pathParameter;
        text.Clear();
        text.Append(File.ReadAllText(path));
        if(EternalModBuilder.runParms.logfile)
            log.Append("\n\nLabels for file '" + path + "'\n");

        // Labels are parsed sequentially by scanning the entire text file.
        Label? nextLabel = buildLabelNew(Label.TYPE_ANY, 0);
        while (nextLabel != null)
        {
            parseLabel(nextLabel);
            nextLabel = buildLabelNew(Label.TYPE_ANY, nextLabel.start);
        }

        /*
        * .decl files use true/false for bool assignments, variations cause crashes
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
                if(text[j] == 'T' || text[j] == 'F')
                    text[j] = (char)(text[j] + 32);
                break;
            }
        }

        File.WriteAllText(path, text.ToString());
    }

    private void parseLabel(Label label)
    {
        const string
        ERR_END_TOGGLE = "There is a '{0}' label with no preceding start label.\n\n{1}",
        ERR_TYPE = "The label '{0}' has an unrecognized type. \n\n{1}",
        ERR_EXPRESSION = "Failed to evaluate expression in label '{0}'\n{1}";

        string expResult = "";
        try
        {
            switch (label.type)
            {
                case Label.TYPE_VAR:
                    expResult = Evaluator.Evaluate(label.exp).GetValueString();
                    text.Replace(label.raw, expResult, label.start, label.raw.Length);
                break;

                case Label.TYPE_TOGGLE_START:
                    expResult = parseToggle();
                break;

                case Label.TYPE_COMMENT:
                    text.Replace(label.raw, "", label.start, label.raw.Length);
                break;

                case Label.TYPE_TOGGLE_END:
                    throw ParseError(ERR_END_TOGGLE, Label.DESC_END_TOGGLE, Label.RULES_TOGGLE_BLOCK);

                default:
                    throw ParseError(ERR_TYPE, label.raw, Label.DESC_TYPES);
            }
        }
        catch(JSONEval.ExpressionEvaluation.ExpressionParsingException e)
        {
            throw ParseError(ERR_EXPRESSION, label.raw, e.Message);
        }

        if(EternalModBuilder.runParms.logfile)
            log.Append(label.raw + " = '" + expResult + "'\n");
        
        string parseToggle()
        {
            const string
            ERR_NO_END = "The label '{0}' has no '{1}' label following it.\n\n{2}";
        
            int numEndLabelsNeeded = 1; // Allows nested toggles
            Label? end = new Label(label);

            while(numEndLabelsNeeded > 0)
            {
                end = buildLabelNew(Label.TYPE_TOGGLE, end.end + 1);
                if(end == null)
                    throw ParseError(ERR_NO_END, label.raw, Label.DESC_END_TOGGLE, Label.RULES_TOGGLE_BLOCK);

                if(end.type.Equals(Label.TYPE_TOGGLE_END))
                    numEndLabelsNeeded--;
                else if(end.type.Equals(Label.TYPE_TOGGLE_START))
                    numEndLabelsNeeded++;
            }

            bool resultBool = ((BoolOperand)Evaluator.Evaluate("bool(" + label.exp + ")")).value;
            if (resultBool) // Keep what's in-between, remove the labels
            {
                text.Remove(end.start, end.raw.Length);
                text.Remove(label.start, label.raw.Length);
            }
            else // Remove the labels and what's in-between them
                text.Remove(label.start, end.end - label.start + 1);
            return resultBool.ToString();
        }
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
}