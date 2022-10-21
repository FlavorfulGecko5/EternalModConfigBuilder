using Newtonsoft.Json.Linq;
class ParsedConfig
{
    public Dictionary<string, string> options = new Dictionary<string, string>();
    public List<PropagateList> propagations = new List<PropagateList>();

    // Used to avoid constantly passing by value
    private string configPath = "", name = "";
    private JToken option = new JProperty("");

    public ParsedConfig() {}

    public ParsedConfig(List<string> configPaths)
    {
        foreach (string path in configPaths)
        {
            configPath = path;
            name = "N/A";
            parseConfig();
        }
        if(EternalModBuilder.mustLog(LogLevel.CONFIGS))
            EternalModBuilder.log(ToString());
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

    private void parseConfig()
    {
        foreach(JProperty property in readConfig().Properties())
        {
            name = property.Name.ToLower(); // Options are stored lowercase
            option = property.Value;

            validateName();
            if(name[0] == '_') // Special comment variables
                continue;

            if (name.EqualsCCIC(PROPERTY_PROPAGATE))
                parsePropagate();
            else
                parseOptionValue();
        }
    }

    private JObject readConfig()
    {
        JObject rawJson = new JObject();
        JsonLoadSettings reportExactDuplicates = new JsonLoadSettings()
        {
            DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
        };

        try
        {
            string text = FSUtil.readFileText(configPath);
            rawJson = JObject.Parse(text, reportExactDuplicates);
        }
        catch (Newtonsoft.Json.JsonReaderException e)
        {
            throw ConfigError(
                "The file has a syntax error. Printing Exception message:\n\n{0}",
                e.Message);
        }

        return rawJson;
    }

    private void validateName()
    {
        if(name.Length == 0)
            throw invalidName();
        
        foreach(char c in name)
            if(!(c <= 'z' && c >= 'a'))
            if(!(c <= '9' && c >= '0'))
            if(!NAME_SPECIAL_CHARACTERS.Contains(c))
                throw invalidName();
        
        if(options.ContainsKey(name))
            throw ConfigError(
                "This name is used to define multiple Options.\n\n{0}",
                RULES_OPTION_NAME);
        
        EMBConfigException invalidName()
        {
            return ConfigError("The name is invalid.\n\n{0}", RULES_OPTION_NAME);
        }
    }

    private void parseOptionValue()
    {
        string[]? values = JsonUtil.readAnyTokenValue(option);
        if(values == null)
            throw ConfigError(
                "This Option is not defined in a valid way.\n\n{0}",
                RULES_OPTION_TYPE);

        options.Add(name, values[0]);        
        if(values.Length > 1)
        {
            for (int i = 1; i < values.Length; i++)
                options.Add(name + '[' + (i - 1) + ']', values[i]);
        }
    }

    private void parsePropagate()
    {
        string workingDir = Directory.GetCurrentDirectory();

        if(option.Type != JTokenType.Object)
            throw ConfigError(
                "This property must be defined as an object.\n\n{0}",
                RULES_PROPAGATE);

        foreach (JProperty list in ((JObject)option).Properties())
        {
            if (!isPropPathValid(list.Name))
                throw ConfigError(
                    "The sub-property '{0}' has a non-relative or backtracking name.\n\n{1}",
                    list.Name, RULES_PROPAGATE);

            string[]? filePaths = JsonUtil.readListTokenValue(list.Value);
            if(filePaths == null)
                throw ConfigError(
                    "This property has an invalid sub-property '{0}'\n\n{1}",
                    list.Name, RULES_PROPAGATE);

            foreach (string path in filePaths)
                if (!isPropPathValid(path))
                    throw ConfigError(
                       "The list '{0}' contains non-relative or backtracking path '{1}'\n\n{2}",
                       list.Name, path, RULES_PROPAGATE);
            propagations.Add(new PropagateList(list.Name, filePaths));
        }

        /// <summary>
        /// Ensures a propagation pathway is valid
        /// </summary>
        /// <param name="propPath"> The propagation pathway to check </param>
        /// <returns>
        /// True if the path is not empty, is relative, and does not backtrack.
        /// Otherwise, returns false
        /// </returns>
        bool isPropPathValid(string propPath)
        {
            if(propPath.Length == 0)
                return false;
            if(Path.IsPathRooted(propPath))
                return false;
            string pathAbs = Path.GetFullPath(propPath);
            return pathAbs.StartsWith(workingDir);
        }
    }

    private EMBConfigException ConfigError(string msg, string arg0="", string arg1="", string arg2="")
    {
        string preamble = String.Format(
            "Problem encountered in config. file '{0}' with Property '{1}':\n", 
            configPath, name
        );
        string formattedMessage = String.Format(msg, arg0, arg1, arg2);

        return new EMBConfigException(preamble + formattedMessage);
    }

    public class EMBConfigException : EMBException
    {
        public EMBConfigException(string msg) : base (msg) {}
    }
}