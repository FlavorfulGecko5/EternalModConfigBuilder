using Newtonsoft.Json.Linq;
/// <summary>
/// Reads and parses JSON configuration files for EternalModBuilder
/// Option data
/// </summary>
class ConfigBuilder
{
    /* 
    * Program Configuration Constants 
    */

    /// <summary>
    /// JSON Object Options may specify their type using this sub-property
    /// </summary>
    const string PROPERTY_TYPE = "Type";

    /// <summary>
    /// JSON Object Options may store their value in this sub-property
    /// </summary>
    const string PROPERTY_VALUE = "Value";

    /// <summary>
    /// The standard/default type. Used if the type is not specified
    /// </summary>
    const string TYPE_STANDARD = "STANDARD";

    /// <summary>
    /// The type for text block Options
    /// </summary>
    const string TYPE_TEXT = "TEXT";

    /// <summary>
    /// The type for map Options
    /// </summary>
    const string TYPE_MAP = "MAP";

    /// <summary>
    /// JSON Objects with this type will not be parsed into a value
    /// </summary>
    const string TYPE_COMMENT = "COMMENT";

    /// <summary>
    /// JSON Objects with this type will be parsed into a set of <see cref="PropagateList"/>
    /// </summary>
    const string TYPE_PROPAGATER = "PROPAGATER";

    /// <summary>
    /// A list of all Option Types built into EternalModBuilder
    /// </summary>
    static readonly List<string> RESERVED_TYPES = new List<string> 
        { TYPE_STANDARD, TYPE_TEXT, TYPE_MAP, TYPE_COMMENT, TYPE_PROPAGATER };
    
    
    /* 
    * Constants Defining Program Rules and Behavior 
    */

    /// <summary>
    /// The rules for how a JSON Object Option can have it's type defined
    /// </summary>
    const string RULES_TYPE = "An Option's '" + PROPERTY_TYPE + "' property must:\n"
    + "- Be defined as a string, or not defined at all.\n"
    + "- If undefined, the Option's assumed type will be '" + TYPE_STANDARD + "'\n\n"
    + "The list of acceptable (case-insensitive) strings are:\n"
    + "- '" + TYPE_STANDARD + "'\n"
    + "- '" + TYPE_TEXT + "'\n"
    + "- '" + TYPE_MAP + "'\n"
    + "- '" + TYPE_COMMENT + "'\n"
    + "- '" + TYPE_PROPAGATER + "'";

    /// <summary>
    /// Generic error message for when an Option is not defined properly according to it's type's rules.
    /// </summary>
    const string ERR_OPTION_DEF = "This Option is not defined in a valid way.\n\n{0}"; 



    /*
    * Static Methods
    */

    /// <summary>
    /// Uses a <see cref="ConfigBuilder"/> object to construct a 
    /// <see cref="ParsedConfig"/> from the provided configuration files
    /// </summary>
    /// <param name="configPaths">Filepaths to the configuration files.</param>
    /// <returns>A <see cref="ParsedConfig"/> containing all data read from
    /// the given configuration files.</returns>
    public static ParsedConfig buildConfig(List<string> configPaths)
    {
        ConfigBuilder builder = new ConfigBuilder();
        foreach(string path in configPaths)
            builder.parseConfigFile(path);    
        return new ParsedConfig(builder.options, builder.propagations);
    }

    /// <summary>
    /// Reads a JSON Array containing only primitive values (numbers, strings,
    /// or Booleans)
    /// </summary>
    /// <param name="listToken">The token containing the JSON array</param>
    /// <param name="writeTo">If successfully read, this variable will
    /// be set to the resulting array of data.</param>
    /// <returns>
    /// True if reading is successful.
    /// False if the data cannot be read or a list element is not of the
    /// appropriate type
    /// </returns>
    private static bool readPrimitiveList(JToken? listToken, ref string[] writeTo)
    {
        if(listToken == null || listToken.Type != JTokenType.Array)
            return false;

        string[] values = new string[listToken.Count()];
        int i = 0;
        foreach(JToken element in listToken)
        {
            switch(element.Type)
            {
                case JTokenType.Integer: case JTokenType.Float:
                case JTokenType.Boolean: case JTokenType.String:
                values[i++] = element.ToString();
                break;

                default:
                return false;
            }
        }
        writeTo = values;
        return true;
    }


    /*
    * Instance fields and methods
    */

    /// <summary>
    /// Contains all options parsed from the configuration files
    /// </summary>
    public EMBOptionDictionary options {get; private set;} = new EMBOptionDictionary();

    /// <summary>
    /// Contains all Propagation Lists read from the configuration files
    /// </summary>
    public List<PropagateList> propagations {get; private set;} = new List<PropagateList>(); 

    /// <summary>
    /// The pathway to the configuration file that is currently being parsed.
    /// </summary>
    private string configPath = "";

    /// <summary>
    /// The name of the Option that is currently being parsed.
    /// </summary>
    private string name = "";

    /// <summary>
    /// Parses the entire contents of a JSON configuration file
    /// </summary>
    /// <param name="configPathParameter">The filepath to the config. file</param>
    public void parseConfigFile(string configPathParameter)
    {
        configPath = configPathParameter;

        /*
        * Read the configuration file
        */
        const string
        ERR_BAD_CFG = "The configuration file '{0}' has a syntax error. Printing Exception message:\n\n{1}";

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
            throw new EMBConfigException(String.Format(ERR_BAD_CFG, configPath, e.Message));
        }

        /*
        * Parse all properties in the file
        */
        foreach (JProperty property in rawJson.Properties())
        {
            name = property.Name;
            JToken value = property.Value;

            const string ERR_BAD_TYPE = "The Option's type is unrecognized.\n\n{0}";

            // Execute appropriate reading function based on the type
            // or throw an error if the type is invalid
            if (value.Type == JTokenType.Object)
            {
                JObject objectToken = (JObject)value;
                JToken? type = objectToken.GetValue(PROPERTY_TYPE);

                if (type == null)
                    readStandard(objectToken.GetValue(PROPERTY_VALUE));
                else
                {
                    if (type.Type != JTokenType.String)
                        throw typeError();

                    switch (type.ToString().ToUpper())
                    {
                        case TYPE_STANDARD:
                            readStandard(objectToken.GetValue(PROPERTY_VALUE));
                        break;

                        case TYPE_TEXT:
                            readText(objectToken.GetValue(PROPERTY_VALUE));
                        break;

                        case TYPE_MAP:
                            readMap(objectToken);
                        break;

                        case TYPE_COMMENT:
                        break;

                        case TYPE_PROPAGATER:
                            readPropagater(objectToken);
                        break;

                        default:
                            throw typeError();
                    }
                    EMBConfigException typeError()
                    {
                        return ConfigError(ERR_BAD_TYPE, RULES_TYPE);
                    }
                }
            }
            else
                readStandard(value); 
        }
    }

    /// <summary>
    /// Reads a 'Standard' EternalModBuilder Option's value and stores it
    /// in the appropriate instance variables
    /// </summary>
    /// <param name="valueToken">The token containing the Option's value</param>
    /// <throws cref="EMBConfigException">
    /// The value is not defined or does not meet the formatting requirements
    /// </throws>
    private void readStandard(JToken? valueToken)
    {
        validatePrimaryName();

        const string RULES_STANDARD = "'" + TYPE_STANDARD + "' Options can be defined as follows:\n"
        + "- String: Any text encased in double-quotes.\n"
        + "- Number: An integer or floating-point value.\n"
        + "- Boolean: Either 'true' or 'false' (case sensitive).\n"
        + "- List: A list of values containing only strings, numbers and Booleans.\n"
        + "- Object: This must have a '" + PROPERTY_VALUE + "' (case-sensitive) sub-property defined in one of the above ways.";

        if (valueToken == null)
            throw invalidDefinition();
        switch (valueToken.Type)
        {
            case JTokenType.Array:
            string[] list = {};
            if (!readPrimitiveList(valueToken, ref list))
                throw invalidDefinition();
            options.Add(name, list.Length.ToString());
            for (int i = 0; i < list.Length; i++)
                options.Add(name + '[' + i + ']', list[i]);
            break;

            case JTokenType.Integer: case JTokenType.Float:
            case JTokenType.Boolean: case JTokenType.String:
            options.Add(name, valueToken.ToString());
            break;

            default:
                throw invalidDefinition();
        }
        EMBConfigException invalidDefinition()
        {
            return ConfigError(ERR_OPTION_DEF, RULES_STANDARD);
        }
    }

    private void readText(JToken? textToken)
    {
        validatePrimaryName();

        const string RULES_TEXT = "'" + TYPE_TEXT + "' Options must:\n"
        + "- Have their '" + PROPERTY_VALUE + "' property defined as a list.\n"
        + "- This list's values must consist of strings, numbers or Booleans.\n"
        + "These list elements will be merged into a single variable, with each element on it's own line of text.";

        string[] textLines = {};
        if(textToken == null || !readPrimitiveList(textToken, ref textLines))
            throw ConfigError(ERR_OPTION_DEF, RULES_TEXT);
        
        string textBlock = "";
        foreach(string line in textLines)
            textBlock += '\n' + line;
        
        // Eliminate initial newline character
        string fixedBlock = textBlock.Length == 0 ? "" : textBlock.Substring(1);
        
        options.Add(name, fixedBlock);
    }

    private void readMap(JObject map)
    {
        validatePrimaryName();

        const string PROPERTY_MAP_KEYS = "Keys";
        const string PROPERTY_MAP_VALUES = "Values";

        const string RULES_MAP = "'" + TYPE_MAP + "' Options must:\n"
        + "- Have a '" + PROPERTY_MAP_KEYS + "' and a '" + PROPERTY_MAP_VALUES + "' property.\n"
        + "- Both properties must be defined as lists of strings, numbers or Booleans.\n"
        + "- Both lists must be the same length.\n"
        + "When parsed, each key will be associated with its corresponding value in a variable";

        const string
        ERR_KEYS = "The '" + PROPERTY_MAP_KEYS + "' subproperty is not defined properly.\n\n{0}",
        ERR_VALUES = "The '" + PROPERTY_MAP_VALUES + "' subproperty is not defined properly.\n\n{0}",
        ERR_UNEQUAL_LENGTHS = "The '" + PROPERTY_MAP_KEYS + "' and '" + PROPERTY_MAP_VALUES + "' lists must be the same length.\n\n{0}";

        string[] keys = {}, values = {};

        if (!readPrimitiveList(map.GetValue(PROPERTY_MAP_KEYS), ref keys))
            throw ConfigError(ERR_KEYS, RULES_MAP);

        if (!readPrimitiveList(map.GetValue(PROPERTY_MAP_VALUES), ref values))
            throw ConfigError(ERR_VALUES, RULES_MAP);

        if (keys.Length != values.Length)
            throw ConfigError(ERR_UNEQUAL_LENGTHS, RULES_MAP);
        
        validateSubNames(keys);

        options.Add(name, keys.Length.ToString());
        for (int i = 0; i < keys.Length; i++)
            options.Add(name + '[' + keys[i] + ']', values[i]);
    }

    /// <summary>
    /// Reads a 'Propagater' EternalModBuilder Option's values and stores
    /// the results in the appropriate instance variables
    /// </summary>
    /// <param name="propagater">The Propagater JSON Object</param>
    /// <throws cref="EMBConfigException">
    /// A non-reserved sub-property is not a list of primitives 
    /// </throws>
    private void readPropagater(JObject propagater)
    {
        const string RULES_PROPAGATER = "Options with the '" + TYPE_PROPAGATER + "' type must obey these rules:\n"
        + "- All their sub-properties (except for '" + PROPERTY_TYPE + "' must be defined as lists of strings.\n"
        + "- The names and values of these string lists must be formatted according to the rules for Propagation filepaths.\n"
        + "These options are used to control EternalModBuilder's Propagation feature.";

        const string ERR_BAD_LIST = "The sub-property '{0}' is not a valid string list.\n\n{1}";

        PropagateList[] parsedLists = new PropagateList[propagater.Count - 1];
        string[] filepaths = {};
        int i = 0;
        foreach(JProperty list in propagater.Properties())
        {
            if(list.Name.Equals(PROPERTY_TYPE))
                continue;

            if(!readPrimitiveList(list.Value, ref filepaths))
                throw ConfigError(ERR_BAD_LIST, list.Name, RULES_PROPAGATER);
            try
            {
                parsedLists[i++] = new PropagateList(list.Name, filepaths);
            }
            catch (PropagateList.EMBPropagaterListException e)
            {
                throw ConfigError(e.Message);
            }
        }
        foreach (PropagateList list in parsedLists)
            propagations.Add(list);
    }

    private void validatePrimaryName()
    {
        const string RULES_OPTION_NAME = "Variable names cannot be empty, and may only contain these characters:\n"
        + "- Letters (a-z, A-Z)\n"
        + "- Numbers (0-9)\n"
        + "- Underscores (_)\n"
        + "Names are case-insensitive, so duplicate names with different capitalizations are not allowed.\n";

        const string
        ERR_INVALID_NAME = "'{0}' is an invalid name.\n\n{1}",
        ERR_DUPE_NAME = "'{0}' is already the name of another variable.\n\n{1}";

        if(!followsNameRules(name))
            throw ConfigError(ERR_INVALID_NAME, name, RULES_OPTION_NAME);

        if (options.ContainsKey(name))
            throw ConfigError(ERR_DUPE_NAME, name, RULES_OPTION_NAME);     
    }

    private void validateSubNames(string[] subnames)
    {
        const string RULES_SUBNAMES = "Subnames obey the same naming rules as primary Variable names.\n"
        + "Additionally, subnames belonging to the same variable cannot equal each other.\n"
        + "(Due to case-insensitivity, duplicate subnames with different capitalizations are not allowed.)";

        const string
        ERR_INVALID_SUBNAME = "'{0}' is an invalid subname.\n\n{1}",
        ERR_DUPE_SUBNAME = "'{0}' is being used as a subname multiple times for this variable.\n\n{1}";

        for(int i = 0; i < subnames.Length; i++)
        {
            if(!followsNameRules(subnames[i]))
                throw ConfigError(ERR_INVALID_SUBNAME, subnames[i], RULES_SUBNAMES);
            for(int j = i + 1; j < subnames.Length; j++)
                if(subnames[i].EqualsOIC(subnames[j]))
                    throw ConfigError(ERR_DUPE_SUBNAME, subnames[i], RULES_SUBNAMES);
        }
    }

    private bool followsNameRules(string nameToCheck)
    {
        if (nameToCheck.Length == 0)
            return false;

        foreach (char c in nameToCheck)
            if (!(c <= 'z' && c >= 'a'))
            if (!(c <= 'Z' && c >= 'A'))
            if (!(c <= '9' && c >= '0'))
            if (!(c == '_'))
                return false;
        return true;
    }

    EMBConfigException ConfigError(string msg, string arg0 = "", string arg1 = "")
    {
        string preamble = String.Format(
            "Problem encountered in config. file '{0}' with Property '{1}':\n",
            configPath, name
        );
        string formattedMessage = String.Format(msg, arg0, arg1);

        return new EMBConfigException(preamble + formattedMessage);
    }

    public class EMBConfigException : EMBException
    {
        public EMBConfigException(string msg) : base(msg) { }
    }
}