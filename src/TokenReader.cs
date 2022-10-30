using Newtonsoft.Json.Linq;

public enum OptionType
{
    DEFAULT_PRIMITIVE,
    DEFAULT_LIST,
    COMMENT,
    PROPAGATER
}

class TokenReader
{
    const string PROPERTY_TYPE = "Type";
    const string PROPERTY_VALUE = "Value";

    const string RULES_TYPE = "An option's '" + PROPERTY_TYPE + "' property must:\n"
    + "- Be defined as a string, or not at all.\n"
    + "- The list of acceptable strings are:\n"
    + "-- 'Default'\n"
    + "If undefined, the option's assumed type will be 'Default'.";

    const string RULES_DEFAULT = "'Default' Options can be defined as follows:\n"
    + "- String: Any text encased in double-quotes.\n"
    + "- Number: An integer or floating-point value.\n"
    + "- Boolean: Either 'true' or 'false' (case sensitive).\n"
    + "- List: A list of values containing only strings, numbers and Booleans.\n"
    + "- Object: This must have a '" + PROPERTY_VALUE + "' (case-sensitive) sub-property defined in one of the above ways.";

    const string RULES_PROPAGATER = "'Propagater' Options must obey these rules:\n"
    + "- They must be defined as objects.\n"
    + "- Their '" + PROPERTY_TYPE + "' field must be set to the string 'Propagater'\n"
    + "- All their other sub-properties must be defined as a list of strings.\n"
    + "These options are used to control EternalModBuilder's propagation feature."; 

    public OptionType          lastTokenType {get; private set;}
    public string              val_defaultPrimitive = "";
    public string[]            val_defaultList      = {};
    public PropagateList[]     val_propagater       = {};

    public void read(JToken token)
    {
        const string ERR_BAD_TYPE = "The option's type is unrecognized.\n\n{0}";
        if(token.Type == JTokenType.Object)
        {
            JObject objectToken = (JObject)token;
            JToken? type = objectToken.GetValue(PROPERTY_TYPE);
            if(type == null)
                readDefault(objectToken.GetValue(PROPERTY_VALUE));

            else
            {
                if(type.Type != JTokenType.String)
                    throw typeError();
                
                switch(type.ToString().ToLower())
                {
                    case "default":
                        readDefault(objectToken.GetValue(PROPERTY_VALUE));
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
            }
        }
        else
            readDefault(token);
        EMBConfigValueException typeError()
        {
            return ValueError(ERR_BAD_TYPE, RULES_TYPE);
        }
    }

    private void readDefault(JToken? valueToken)
    {
        const string ERR_DEFAULT = "This Option is not defined in a valid way.\n\n{0}";
        if(valueToken == null)
            throw invalidDefinition();
        switch(valueToken.Type)
        {
            case JTokenType.Array:
            lastTokenType = OptionType.DEFAULT_LIST;
            if(!readPrimitiveList(valueToken, ref val_defaultList))
                throw invalidDefinition();
            break;

            case JTokenType.Integer: case JTokenType.Float:
            case JTokenType.Boolean: case JTokenType.String:
            lastTokenType = OptionType.DEFAULT_PRIMITIVE;
            val_defaultPrimitive = valueToken.ToString();
            break;

            default:
            throw invalidDefinition();
        }
        EMBConfigValueException invalidDefinition()
        {
            return ValueError(ERR_DEFAULT, RULES_DEFAULT);
        }
    }

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