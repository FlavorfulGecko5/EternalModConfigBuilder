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
    /// JSON Objectw with this type are used to define a custom Type
    /// </summary>
    const string TYPE_TYPEDEF = "TYPEDEF";

    /// <summary>
    /// A list of all Option Types built into EternalModBuilder
    /// </summary>
    static readonly string[] RESERVED_TYPES =  
        { TYPE_STANDARD, TYPE_TEXT, TYPE_MAP, TYPE_COMMENT, TYPE_PROPAGATER, TYPE_TYPEDEF };

    /// <summary>
    /// The value property of custom types may have this string inserted into it
    /// To represent the name of the Options using this type
    /// </summary>
    const string SYM_TYPEDEF_NAME = "{!THIS}";
    
    
    /* 
    * Constants Defining Program Rules and Behavior 
    */

    /// <summary>
    /// The rules for how a JSON Object Option can have it's type defined
    /// </summary>
    const string RULES_TYPE = "An Option's '" + PROPERTY_TYPE + "' property must:\n"
    + "- Be defined as a string, or not defined at all.\n"
    + "- If undefined, the Option's assumed type will be '" + TYPE_STANDARD + "'\n\n"
    + "The built-in list of acceptable (case-insensitive) strings are:\n"
    + "- '" + TYPE_STANDARD + "'\n"
    + "- '" + TYPE_TEXT + "'\n"
    + "- '" + TYPE_MAP + "'\n"
    + "- '" + TYPE_COMMENT + "'\n"
    + "- '" + TYPE_PROPAGATER + "'\n"
    + "- '" + TYPE_TYPEDEF + "'";

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
    /// Contains all user-defined types read from the configuration files
    /// </summary>
    private EMBTypeDefDictionary userTypes = new EMBTypeDefDictionary();

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
                    string typeString = type.ToString().ToUpper();
                    switch (typeString)
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

                        case TYPE_TYPEDEF:
                            readTypeDef(objectToken);
                        break;

                        default:
                            if (userTypes.ContainsKey(typeString))
                                readCustomType(objectToken, userTypes[typeString]);
                            else
                                throw typeError();
                        break;
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

        const string RULES_KEYS = "In a '" + TYPE_MAP + "' Option, the '" + PROPERTY_MAP_KEYS + "' list's elements must:\n"
        + "- Obey the normal variable naming rules.\n"
        + "- Cannot have duplicate values (even with variations in capitalization)";

        const string
        ERR_KEYS = "The '" + PROPERTY_MAP_KEYS + "' subproperty is not defined properly.\n\n{0}",
        ERR_VALUES = "The '" + PROPERTY_MAP_VALUES + "' subproperty is not defined properly.\n\n{0}",
        ERR_UNEQUAL_LENGTHS = "The '" + PROPERTY_MAP_KEYS + "' and '" + PROPERTY_MAP_VALUES + "' lists must be the same length.\n\n{0}",
        ERR_KEY_NAME = "'{0}' is an invalid name for a key.\n\n{1}",
        ERR_KEY_DUPLICATE = "'{0}' is being used as a key multiple times.\n\n{1}";

        string[] keys = {}, values = {};

        if (!readPrimitiveList(map.GetValue(PROPERTY_MAP_KEYS), ref keys))
            throw ConfigError(ERR_KEYS, RULES_MAP);

        if (!readPrimitiveList(map.GetValue(PROPERTY_MAP_VALUES), ref values))
            throw ConfigError(ERR_VALUES, RULES_MAP);

        if (keys.Length != values.Length)
            throw ConfigError(ERR_UNEQUAL_LENGTHS, RULES_MAP);

        for(int i = 0; i < keys.Length; i++)
        {
            if(!followsNameRules(keys[i]))
                throw ConfigError(ERR_KEY_NAME, keys[i], RULES_KEYS);
            for(int j = i + 1; j < keys.Length; j++)
                if(keys[i].EqualsOIC(keys[j]))
                    throw ConfigError(ERR_KEY_DUPLICATE, keys[i], RULES_KEYS);
        }

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

    private void readTypeDef(JObject typeObject)
    {
        /*
        * Type Object name rules and validation
        */
        const string RULES_TYPEDEF_NAME = "A '" + TYPE_TYPEDEF + "' Option's name will become it's '" + PROPERTY_TYPE + "' string. The name must:\n"
        + "- Obey the same naming rules as normal variable names.\n"
        + "- Not match the type string of any other standard or user-defined types.";

        const string
        ERR_INVALID_NAME = "'{0}' is not a valid name for a custom type.\n\n{1}",
        ERR_DUPLICATE_USERTYPE = "'{0}' is already defined as a custom type.\n\n{1}",
        ERR_DUPLICATE_RESERVEDTYPE = "'{0}' is already a built-in type.\n\n{1}";

        if(!followsNameRules(name))
            throw ConfigError(ERR_INVALID_NAME, name, RULES_TYPEDEF_NAME);
        if(userTypes.ContainsKey(name))
            throw ConfigError(ERR_DUPLICATE_USERTYPE, name, RULES_TYPEDEF_NAME);
        foreach(string reservedType in RESERVED_TYPES)
            if(reservedType.EqualsOIC(name))
                throw ConfigError(ERR_DUPLICATE_RESERVEDTYPE, name, RULES_TYPEDEF_NAME);
        
        /*
        * Type properties list rules, reading and validation
        */
        const string PROPERTY_TYPEDEF_PROPLIST = "Using";

        const string RULES_TYPEDEF_PROPERTIES = "'" + TYPE_TYPEDEF + "' Options must have a '" + PROPERTY_TYPEDEF_PROPLIST + "' property.\n"
        + "- This property must be defined as a list of strings.\n"
        + "- Each string in this list must follow standard Option naming conventions.\n"
        + "- No string may equal each other or a property used by a '" + TYPE_TYPEDEF + "' Option (even with variations in capitalization).\n"
        + "These strings define what JSON object subproperties are utilized by this type.";

        const string
        ERR_PROPLIST_DEFINITION = "The '" + PROPERTY_TYPEDEF_PROPLIST + "' subproperty is not defined correctly.\n\n{0}",
        ERR_PROPLIST_ELEMENT = "The '" + PROPERTY_TYPEDEF_PROPLIST + "' list contains an invalid or duplicate string '{0}'\n\n{1}";

        string[] propList = {};
        if(!readPrimitiveList(typeObject.GetValue(PROPERTY_TYPEDEF_PROPLIST), ref propList))
            throw ConfigError(ERR_PROPLIST_DEFINITION, RULES_TYPEDEF_PROPERTIES);
        
        for (int i = 0; i < propList.Length; i++)
        {
            if (!followsNameRules(propList[i]))
                throw invalidPropName();

            if(
                propList[i].EqualsOIC(PROPERTY_TYPE)
                || propList[i].EqualsOIC(PROPERTY_VALUE)
                || propList[i].EqualsOIC(PROPERTY_TYPEDEF_PROPLIST)
            )
                throw invalidPropName();

            for (int j = i + 1; j < propList.Length; j++)
                if (propList[i].EqualsOIC(propList[j]))
                    throw invalidPropName();
            
            EMBConfigException invalidPropName()
            {
                return ConfigError(ERR_PROPLIST_ELEMENT, propList[i], RULES_TYPEDEF_PROPERTIES);
            }
        }
        
        /*
        * TypeDef value property rules, reading and validation
        */
        const string RULES_TYPEDEF_VALUE = "'" + TYPE_TYPEDEF + "' Options have a non-mandatory '" + PROPERTY_VALUE + "' property where:\n"
        + "- It can be defined as a list of strings, numbers and Booleans.\n"
        + "- When parsed, it's elements will be consecutively merged into a single string\n"
        + "If defined with strings, you may insert '" + SYM_TYPEDEF_NAME + "' into this value.\n"
        + "When creating Options using this custom type, this symbol will be replaced with the name of the Option.";

        const string
        ERR_VALUE = "The '" + PROPERTY_VALUE + "' property is not defined correctly.\n\n{0}";

        string parsedValue = "";
        JToken? valueToken = typeObject.GetValue(PROPERTY_VALUE);
        if(valueToken != null)
        {
            string[] rawValues = {};
            if(!readPrimitiveList(valueToken, ref rawValues))
                throw ConfigError(ERR_VALUE, RULES_TYPEDEF_VALUE);
            foreach(string element in rawValues)
                parsedValue += element;
        }

        userTypes.Add(name, new TypeDef(propList, parsedValue));

        /*
        Console.WriteLine("Type Name '" + name + "'\nUsing:");
        foreach(string s in propList)
            Console.WriteLine("- '" + s + "'");
        Console.WriteLine("Value: '" + parsedValue + "'");
        */
    }

    private void readCustomType(JObject objectToken, TypeDef type)
    {
        validatePrimaryName();

        const string RULES_CUSTOM_PROPS = "An Option that uses a custom type must:\n"
        + "- Have all of that Type's properties defined.\n"
        + "- These may be defined as strings, numbers, Booleans, or lists containing them.\n"
        + "Note that the '" + PROPERTY_VALUE + "' property cannot be defined or redefined here.";

        const string
        ERR_PROP_MISSING = "The required sub-property '{0}' is not defined.\n\n{1}",
        ERR_PROP_DEF = "The sub-property '{0}' is not defined in a valid way.\n\n{1}";

        foreach(string prop in type.properties)
        {
            JToken? propToken = objectToken.GetValue(prop);
            if(propToken == null)
                throw ConfigError(ERR_PROP_MISSING, prop, RULES_CUSTOM_PROPS);

            switch(propToken.Type)
            {
                case JTokenType.Array:
                string[] listElements = {};
                if(!readPrimitiveList(propToken, ref listElements))
                    throw propDefError();
                options.Add(name + '.' + prop, listElements.Length.ToString());
                for(int i = 0; i < listElements.Length; i++)
                    options.Add(name + '.' + prop + '[' + i + ']', listElements[i]);
                break;

                case JTokenType.Integer: case JTokenType.Float:
                case JTokenType.String: case JTokenType.Boolean:
                options.Add(name + '.' + prop, propToken.ToString());
                break;

                default:
                    throw propDefError();
            }

            EMBConfigException propDefError()
            {
                return ConfigError(ERR_PROP_DEF, prop, RULES_CUSTOM_PROPS);
            }   
        }
        string processedValue = type.value.ReplaceOIC(SYM_TYPEDEF_NAME, name);
        options.Add(name, processedValue);
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