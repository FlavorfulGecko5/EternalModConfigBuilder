class TypeDef
{
    public string[] properties { get; private set; }
    public string value { get; private set; }

    public TypeDef(string[] propertiesParam, string valueParam)
    {
        properties = propertiesParam;
        value = valueParam;
    }
}

class EMBTypeDefDictionary : Dictionary<string, TypeDef>
{
    public EMBTypeDefDictionary() : base(StringComparer.OrdinalIgnoreCase) { }
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