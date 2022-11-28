using Newtonsoft.Json.Linq;
/// <summary>
/// Reads and parses EternalModBuilder Option data read from JSON 
/// configuration files
/// </summary>
class TokenReader
{
    /* Program Configuration Constants */

    /// <summary>
    /// JSON Object Options may specify their type using this sub-property
    /// </summary>
    const string PROPERTY_TYPE = "Type";

    /// <summary>
    /// Typical JSON Object Options will store their value in this sub-property
    /// </summary>
    const string PROPERTY_VALUE = "Value";





    /* Constants Defining Program Rules and Behavior */

    /// <summary>
    /// The rules for how a JSON Object Option can have it's type defined
    /// </summary>
    const string RULES_TYPE = "An option's '" + PROPERTY_TYPE + "' property must:\n"
    + "- Be defined as a string, or not defined at all.\n"
    + "- If undefined, the Option's assumed type will be 'Standard'\n\n"
    + "The list of acceptable strings are:\n"
    + EnumDesc.SUMMARY_OPTIONTYPE;

    /// <summary>
    /// The rules for how a 'Standard' Option can be defined
    /// </summary>
    const string RULES_STANDARD = "'Standard' Options can be defined as follows:\n"
    + "- String: Any text encased in double-quotes.\n"
    + "- Number: An integer or floating-point value.\n"
    + "- Boolean: Either 'true' or 'false' (case sensitive).\n"
    + "- List: A list of values containing only strings, numbers and Booleans.\n"
    + "- Object: This must have a '" + PROPERTY_VALUE + "' (case-sensitive) sub-property defined in one of the above ways.";

    /// <summary>
    /// The rules for how a 'Propagater' Option can be defined
    /// </summary>
    const string RULES_PROPAGATER = "'Propagater' Options must obey these rules:\n"
    + "- They must be defined as objects.\n"
    + "- Their '" + PROPERTY_TYPE + "' field must be set to the string 'Propagater'\n"
    + "- All their other sub-properties must be defined as lists of strings.\n"
    + "These options are used to control EternalModBuilder's propagation feature."; 




    
    /* Instance Fields and Methods */

    /// <summary>
    /// The OptionType of the last successfully parsed JToken
    /// </summary>
    public OptionType          lastTokenType {get; private set;}

    /// <summary>
    /// The value of the last successfully parsed Standard Primitive JToken
    /// </summary>
    public string              val_standardPrimitive = "";

    /// <summary>
    /// The value of the last successfully parsed Standard List JToken
    /// </summary>
    public string[]            val_standardList      = {};

    /// <summary>
    /// The data of the last successfully parsed Propagater JToken
    /// </summary>
    public PropagateList[]     val_propagater        = {};

    /// <summary>
    /// Reads a JToken formatted as an EternalModBuilder Option and stores
    /// it's values and type in the appropriate instance fields.
    /// </summary>
    /// <param name="token">The Option to be read</param>
    /// <exception cref="EMBConfigValueException">
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
                    break;

                    case "comment":
                        lastTokenType = OptionType.COMMENT;
                    return;

                    case "propagater":
                        readPropagater(objectToken);
                    return;

                    default:
                        throw typeError();
                }
                EMBConfigValueException typeError()
                {
                    return ValueError(ERR_BAD_TYPE, RULES_TYPE);
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
    /// <throws cref="EMBConfigValueException">
    /// The value is not defined or does not meet the formatting requirements
    /// </throws>
    private void readStandard(JToken? valueToken)
    {
        const string ERR_STANDARD = "This Option is not defined in a valid way.\n\n{0}";

        if(valueToken == null)
            throw invalidDefinition();
        switch(valueToken.Type)
        {
            case JTokenType.Array:
            lastTokenType = OptionType.STANDARD_LIST;
            if(!readPrimitiveList(valueToken, ref val_standardList))
                throw invalidDefinition();
            break;

            case JTokenType.Integer: case JTokenType.Float:
            case JTokenType.Boolean: case JTokenType.String:
            lastTokenType = OptionType.STANDARD_PRIMITIVE;
            val_standardPrimitive = valueToken.ToString();
            break;

            default:
            throw invalidDefinition();
        }
        EMBConfigValueException invalidDefinition()
        {
            return ValueError(ERR_STANDARD, RULES_STANDARD);
        }
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
    private bool readPrimitiveList(JToken? listToken, ref string[] writeTo)
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

    /// <summary>
    /// Reads a 'Propagater' EternalModBuilder Option's values and stores
    /// the results in the appropriate instance variables
    /// </summary>
    /// <param name="propagater">The Propagater JSON Object</param>
    /// <throws cref="EMBConfigValueException">
    /// A non-reserved sub-property is not a list of primitives 
    /// </throws>
    private void readPropagater(JObject propagater)
    {
        const string ERR_BAD_LIST = "The sub-property '{0}' is not a valid string list.\n\n{1}";

        lastTokenType = OptionType.PROPAGATER;
        val_propagater = new PropagateList[propagater.Count - 1];
        string[] filepaths = {};
        int i = 0;
        foreach(JProperty list in propagater.Properties())
        {
            if(list.Name.Equals(PROPERTY_TYPE))
                continue;

            if(!readPrimitiveList(list.Value, ref filepaths))
                throw ValueError(ERR_BAD_LIST, list.Name, RULES_PROPAGATER);
            
            val_propagater[i++] = new PropagateList(list.Name, filepaths);
        }
    }

    private EMBConfigValueException ValueError(string msg, string arg0="", string arg1="")
    {
        string formattedMessage = String.Format(msg, arg0, arg1);
        return new EMBConfigValueException(formattedMessage);
    }

    public class EMBConfigValueException : EMBException
    {
        public EMBConfigValueException(string msg) : base(msg) { }
    }
}