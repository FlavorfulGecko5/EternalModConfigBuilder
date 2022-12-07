class EMBOptionDictionary : Dictionary<string, string>
{
    public EMBOptionDictionary() : base(StringComparer.OrdinalIgnoreCase) {}     
}

class ParsedConfig
{
    public EMBOptionDictionary options {get; private set;} = new EMBOptionDictionary();
    public List<PropagateList> propagations {get; private set;} = new List<PropagateList>();

    public ParsedConfig() {}

    public ParsedConfig(EMBOptionDictionary optionsParm, List<PropagateList> propagationsParm)
    {
        options = optionsParm;
        propagations = propagationsParm;
    }

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
}