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
    /// JSON Objects with this type will not be parsed into a value
    /// </summary>
    const string TYPE_COMMENT = "COMMENT";

    /// <summary>
    /// JSON Objects with this type will be parsed into a set of <see cref="PropagateList"/>
    /// </summary>
    const string TYPE_PROPAGATER = "PROPAGATER";

    
    
    /* 
    * Constants Defining Program Rules and Behavior 
    */

    /// <summary>
    /// The rules for how a JSON Object Option can have it's type defined
    /// </summary>
    const string RULES_TYPE = "An option's '" + PROPERTY_TYPE + "' property must:\n"
    + "- Be defined as a string, or not defined at all.\n"
    + "- If undefined, the Option's assumed type will be '" + TYPE_STANDARD + "'\n\n"
    + "The list of acceptable (case-insensitive) strings are:\n"
    + "- '" + TYPE_STANDARD + "'\n"
    + "- '" + TYPE_COMMENT + "'\n"
    + "- '" + TYPE_PROPAGATER + "'";

    /// <summary>
    /// The rules for how a 'Standard' Option can be defined
    /// </summary>
    const string RULES_STANDARD = "'" + TYPE_STANDARD + "' Options can be defined as follows:\n"
    + "- String: Any text encased in double-quotes.\n"
    + "- Number: An integer or floating-point value.\n"
    + "- Boolean: Either 'true' or 'false' (case sensitive).\n"
    + "- List: A list of values containing only strings, numbers and Booleans.\n"
    + "- Object: This must have a '" + PROPERTY_VALUE + "' (case-sensitive) sub-property defined in one of the above ways.";

    /// <summary>
    /// The rules for how a 'Propagater' Option can be defined
    /// </summary>
    const string RULES_PROPAGATER = "Options with the'" + TYPE_PROPAGATER + "' type must obey these rules:\n"
    + "- All their sub-properties (except for '" + PROPERTY_TYPE + "' must be defined as lists of strings.\n"
    + "- The names and values of these string lists must be formatted according to the rules for Propagation filepaths."
    + "These options are used to control EternalModBuilder's Propagation feature."; 



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
        return builder.config;
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
    /// Contains all data parsed from the configuration files
    /// </summary>
    public ParsedConfig config {get; private set;} = new ParsedConfig();

    /// <summary>
    /// The pathway to the configuration file that is currently being parsed.
    /// </summary>
    private string configPath = "";

    /// <summary>
    /// The name of the Option that is currently being parsed.
    /// </summary>
    private string name = "";

    /// <summary>
    /// Parses the entire contents of a JSON configuration file and adds
    /// the parsed data to <see cref="config"/>
    /// </summary>
    /// <param name="configPathParameter">The filepath to the config. file</param>
    public void parseConfigFile(string configPathParameter)
    {
        configPath = configPathParameter;
        name = "N/A";

        /*
        * Read the configuration file
        */
        const string
        ERR_BAD_CFG = "The file has a syntax error. Printing Exception message:\n\n{0}";

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
            throw ConfigError(ERR_BAD_CFG, e.Message);
        }

        /*
        * Parse all properties in the file
        */
        foreach (JProperty property in rawJson.Properties())
        {
            name = property.Name; 
            try
            {
                read(property.Value);
            }
            catch(ParsedConfig.EMBConfigNameException e)
            {
                throw ConfigError(e.Message);
            }
        }
    }

    /// <summary>
    /// Reads a JToken formatted as an EternalModBuilder Option and stores
    /// it's values and type in the appropriate instance fields.
    /// </summary>
    /// <param name="token">The Option to be read</param>
    /// <exception cref="EMBConfigException">
    /// The data cannot be parsed for any reason, including having
    /// improper formatting or structuring.
    /// </exception>
    public void read(JToken token)
    {
        const string ERR_BAD_TYPE = "The Option's type is unrecognized.\n\n{0}";

        // Execute appropriate reading function based on the type
        // or throw an error if the type is invalid
        if(token.Type == JTokenType.Object)
        {
            JObject objectToken = (JObject)token;
            JToken? type = objectToken.GetValue(PROPERTY_TYPE);

            if(type == null)
                readStandard(objectToken.GetValue(PROPERTY_VALUE));
            else
            {
                if(type.Type != JTokenType.String)
                    throw typeError();
                
                switch(type.ToString().ToLower())
                {
                    case "standard":
                        readStandard(objectToken.GetValue(PROPERTY_VALUE));
                    return;

                    case "comment":
                    return;

                    case "propagater":
                        readPropagater(objectToken);
                    return;

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
            readStandard(token);
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
        const string ERR_STANDARD = "This Option is not defined in a valid way.\n\n{0}";

        if (valueToken == null)
            throw invalidDefinition();
        switch (valueToken.Type)
        {
            case JTokenType.Array:
            string[] list = {};
            if (!readPrimitiveList(valueToken, ref list))
                throw invalidDefinition();
            config.addListOption(name, list);
            break;

            case JTokenType.Integer: case JTokenType.Float:
            case JTokenType.Boolean: case JTokenType.String:
            config.addOption(name, valueToken.ToString());
            break;

            default:
                throw invalidDefinition();
        }
        EMBConfigException invalidDefinition()
        {
            return ConfigError(ERR_STANDARD, RULES_STANDARD);
        }
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
        config.addPropagationLists(parsedLists);
    }

    EMBConfigException ConfigError(string msg, string arg0 = "", string arg1 = "")
    {
        string preamble = String.Format(
            "Problem encountered in config. file '{0}' with Property '{1}':\n",
            configPath, name
        );
        string formattedMessage = String.Format(msg, arg0);

        return new EMBConfigException(preamble + formattedMessage);
    }

    public class EMBConfigException : EMBException
    {
        public EMBConfigException(string msg) : base(msg) { }
    }
}