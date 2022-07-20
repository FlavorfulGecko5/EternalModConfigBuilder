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
        }
    }

    private void parseVariable(Label label)
    {
        text = text.Substring(0, label.start) + label.result 
            + text.Substring(label.end + 1, text.Length - label.end - 1);
    }

    public enum Error
    {
        INCOMPLETE_LABEL
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
        }
        return EMBException.buildException(preamble + msg, args);
    }
}