class ParsedConfig
{
    const string RULES_OPTION_NAME = "Variable names cannot be empty, and may only contain these characters:\n"
    + "- Letters (a-z, A-Z)\n"
    + "- Numbers (0-9)\n"
    + "- Underscores (_)\n"
    + "Names are case-insensitive, so duplicate names with different capitalizations are not allowed.\n";

    const string RULES_SUBNAMES = "Subnames obey the same naming rules as primary Variable names.\n"
    + "Additionally, subnames belonging to the same variable cannot equal each other.\n"
    + "(Due to case-insensitivity, duplicate subnames with different capitalizations are not allowed.)";

    public Dictionary<string, string> options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
        validatePrimaryName(name);
        options.Add(name, value);
    }

    public void addListOption(string name, string[] values)
    {
        validatePrimaryName(name);
        options.Add(name, values.Length.ToString());
        for (int i = 0; i < values.Length; i++)
            options.Add(name + '[' + i + ']', values[i]);
    }

    public void addMapOption(string name, string[] keys, string[] values)
    {
        validatePrimaryName(name);
        validateSubNames(keys);

        options.Add(name, keys.Length.ToString());
        for(int i = 0; i < keys.Length; i++)
            options.Add(name + '[' + keys[i] + ']', values[i]);
    }

    public void addPropagationLists(PropagateList[] newLists)
    {
        foreach(PropagateList list in newLists)
            propagations.Add(list);
    }

    private void validatePrimaryName(string name)
    {
        const string
        ERR_INVALID_NAME = "'{0}' is an invalid name.\n\n{1}",
        ERR_DUPE_NAME = "'{0}' is already the name of another variable.\n\n{1}";

        if(!followsNameRules(name))
            throw NameError(ERR_INVALID_NAME, name, RULES_OPTION_NAME);

        if (options.ContainsKey(name))
            throw NameError(ERR_DUPE_NAME, name, RULES_OPTION_NAME);     
    }

    private void validateSubNames(string[] subnames)
    {
        const string
        ERR_INVALID_SUBNAME = "'{0}' is an invalid subname.\n\n{1}",
        ERR_DUPE_SUBNAME = "'{0}' is being used as a subname multiple times for this variable.\n\n{1}";

        for(int i = 0; i < subnames.Length; i++)
        {
            if(!followsNameRules(subnames[i]))
                throw NameError(ERR_INVALID_SUBNAME, subnames[i], RULES_SUBNAMES);
            for(int j = i + 1; j < subnames.Length; j++)
                if(subnames[i].EqualsOIC(subnames[j]))
                    throw NameError(ERR_DUPE_SUBNAME, subnames[i], RULES_SUBNAMES);
        }
    }

    private bool followsNameRules(string name)
    {
        if (name.Length == 0)
            return false;

        foreach (char c in name)
            if (!(c <= 'z' && c >= 'a'))
            if (!(c <= 'Z' && c >= 'A'))
            if (!(c <= '9' && c >= '0'))
            if (!(c == '_'))
                return false;
        return true;
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