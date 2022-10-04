using Newtonsoft.Json.Linq;
using static ParsedConfig.Error;
class ParsedConfig
{
    public Dictionary<string, string> options = new Dictionary<string, string>();
    public List<PropagateList> propagations = new List<PropagateList>();

    // Used to avoid constantly passing by value
    private string configPath = "", name = "";
    private JToken option = new JProperty("");

    public ParsedConfig()
    {
        foreach(string path in EternalModBuilder.configPaths)
        {
            configPath = path;
            name = "N/A";
            parseConfig();
        }
        LogMaker logger = new LogMaker(LogLevel.CONFIGS);
        if(logger.mustLog)
        {
            logger.appendString(ToString());
            logger.log();
        }
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
            throw EMBError(BAD_JSON_FILE, e.Message);
        }

        return rawJson;
    }

    private void validateName()
    {
        if(name.Length == 0)
            throw EMBError(BAD_NAME_FORMATTING);
        
        foreach(char c in name)
            if(!(c <= 'z' && c >= 'a'))
            if(!(c <= '9' && c >= '0'))
            if(!NAME_SPECIAL_CHARACTERS.Contains(c))
                throw EMBError(BAD_NAME_FORMATTING);
        
        if(options.ContainsKey(name))
            throw EMBError(DUPLICATE_NAME);
    }

    private void parseOptionValue()
    {
        string[]? values = JsonUtil.readAnyTokenValue(option);
        if(values == null)
            throw EMBError(BAD_OPTION_TYPE);

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
            throw EMBError(PROPAGATE_ISNT_OBJECT);

        foreach (JProperty list in ((JObject)option).Properties())
        {
            if (!isPropPathValid(list.Name))
                throw EMBError(NOT_LOCALREL_PROP_NAME, list.Name);

            string[]? filePaths = JsonUtil.readListTokenValue(list.Value);
            if(filePaths == null)
                throw EMBError(BAD_PROP_ARRAY, list.Name);

            foreach (string path in filePaths)
                if (!isPropPathValid(path))
                    throw EMBError(NOT_LOCALREL_PROP_PATH, list.Name, path);
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

    public enum Error
    {
        BAD_JSON_FILE,
        BAD_NAME_FORMATTING,
        DUPLICATE_NAME,
        BAD_OPTION_TYPE,
        PROPAGATE_ISNT_OBJECT,
        BAD_PROP_ARRAY,
        NOT_LOCALREL_PROP_NAME,
        NOT_LOCALREL_PROP_PATH,   
    }

    private EMBException EMBError(Error e, string arg0 = "", string arg1 = "")
    {
        string preamble = String.Format(
            "Problem encountered in config. file '{0}' with Property '{1}':\n",
            configPath,
            name
        );
        string msg = "";
        string[] args = {"", "", ""};
        switch(e)
        {
            case BAD_JSON_FILE:
            msg = "The file has a syntax error. Printing Exception message:\n\n{0}";
            args[0] = arg0; // The Exception message
            break;

            case BAD_NAME_FORMATTING:
            msg = "The name is invalid.\n\n{0}";
            args[0] = RULES_OPTION_NAME;
            break;

            case DUPLICATE_NAME:
            msg = "This name is used to define multiple Options.\n\n{0}";
            args[0] = RULES_OPTION_NAME;
            break;

            case BAD_OPTION_TYPE:
            msg = "This Option is not defined in a valid way.\n\n{0}";
            args[0] = RULES_OPTION_TYPE;
            break;

            case PROPAGATE_ISNT_OBJECT:
            msg = "This property must be defined as an object.\n\n{0}";
            args[0] = RULES_PROPAGATE;
            break;

            case BAD_PROP_ARRAY:
            msg = "This property has an invalid sub-property '{0}'\n\n{1}";
            args[0] = arg0; // Propagate list name
            args[1] = RULES_PROPAGATE;
            break;

            case NOT_LOCALREL_PROP_NAME:
            msg = "The sub-property '{0}' has a non-relative"
                    + " or backtracking name.\n\n{1}";
            args[0] = arg0; // Propagate list name
            args[1] = RULES_PROPAGATE;
            break;

            case NOT_LOCALREL_PROP_PATH:
            msg = "The list '{0}' contains non-relative"
                    + " or backtracking path '{1}'\n\n{2}";
            args[0] = arg0; // Propagate list name
            args[1] = arg1; // Propagate list element
            args[2] = RULES_PROPAGATE;
            break;
        }
        return EMBException.buildException(preamble + msg, args);
    }
}