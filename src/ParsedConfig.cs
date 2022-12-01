class ParsedConfig
{
    const string RULES_OPTION_NAME = "Variable names cannot be empty, and may only contain these characters:\n"
    + "- Letters (a-z, A-Z)\n"
    + "- Numbers (0-9)\n"
    + "- Underscores (_)\n"
    + "Names are case-insensitive, so duplicate names with different capitalizations are not allowed.\n";

    public Dictionary<string, string> options = new Dictionary<string, string>();
    public List<PropagateList> propagations = new List<PropagateList>();

    public override string ToString()
    {
        string optionList = "";
        foreach(KeyValuePair<string, string> entry in options)
            optionList += " - '" + entry.Key + "' - '" + entry.Value + "'\n";

        string propagationList = "";
        foreach(PropagateList list in propagations)
            propagationList += list.ToString() + '\n';
        
        string msg = "Parsed Configuration File Data:\n"
            + optionList + propagationList;
        
        return msg;
    }

    public void addOption(string name, string value)
    {
        name = validateName(name);
        options.Add(name, value);
    }

    public void addListOption(string name, string[] values)
    {
        name = validateName(name);
        options.Add(name, values.Length.ToString());
        for (int i = 0; i < values.Length; i++)
            options.Add(name + '[' + i + ']', values[i]);
    }

    public void addPropagationLists(PropagateList[] newLists)
    {
        foreach(PropagateList list in newLists)
            propagations.Add(list);
    }

    private string validateName(string name)
    {
        const string
        ERR_INVALID_NAME = "'{0}' is an invalid name.\n\n{1}",
        ERR_DUPE_NAME = "'{0}' is already the name of another variable.\n\n{1}";

        string fixedName = name.ToLower();
        if (fixedName.Length == 0)
            throw invalidName();

        foreach (char c in fixedName)
            if (!(c <= 'z' && c >= 'a'))
            if (!(c <= '9' && c >= '0'))
            if (!(c == '_'))
                throw invalidName();

        if (options.ContainsKey(fixedName))
            throw NameError(ERR_DUPE_NAME, name, RULES_OPTION_NAME);
        
        return fixedName;

        EMBConfigNameException invalidName()
        {
            return NameError(ERR_INVALID_NAME, name, RULES_OPTION_NAME);
        }        
    }

    private EMBConfigNameException NameError(string msg, string arg0 = "", string arg1 = "")
    {
        string formattedMessage = String.Format(msg, arg0, arg1);

        return new EMBConfigNameException(formattedMessage);
    }

    public class EMBConfigNameException : EMBException
    {
        public EMBConfigNameException(string msg) : base(msg) { }
    }
}