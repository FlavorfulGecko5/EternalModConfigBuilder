using Newtonsoft.Json.Linq;
using static ParsedConfig.Error;
class ParsedConfig
{
    public bool hasPropagations;
    public List<Option> options { get; }
    public List<PropagateList> propagations {get;}

    // Used to avoid constantly passing by value
    private string configPath, name;

    private bool isObject;
    private JObject objectOption;
    private JProperty nonObjectOption;

    public ParsedConfig(string configPathParameter)
    {
        hasPropagations = false;
        options = new List<Option>();
        propagations = new List<PropagateList>();
        configPath = configPathParameter;

        name = "";
        isObject = false;
        objectOption = new JObject();
        nonObjectOption = new JProperty("");

        parseConfig();
    }

    private void parseConfig()
    {
        foreach(JProperty property in readConfig().Properties())
        {
            name = property.Name;
            try 
            {   
                objectOption = (JObject)property.Value;
                isObject = true;
            }
            catch(System.InvalidCastException)
            {
                isObject = false;
                nonObjectOption = property;
            }
            parseOption();
        }
    }

    private void parseOption()
    {
        validateName();
        if(name.Equals(PROPERTY_PROPAGATE, CCIC))
            parsePropagate();
        else
            parseOptionValue();
    }

    private void parseOptionValue()
    {
        string? value = null;
        try
        {
            if(isObject)
                value = (string?)objectOption[PROPERTY_VALUE];
            else
                value = (string?)nonObjectOption.Value;

            if (value == null)
                ThrowError(BAD_OPTION_TYPE);
            else
                options.Add(new Option(name, value));
        }
        // If the property is a json list or object
        catch (System.ArgumentException)
        {
            ThrowError(BAD_OPTION_TYPE);
        }
    }

    private void parsePropagate()
    {
        if (hasPropagations)
            ThrowError(DUPLICATE_NAME);
        hasPropagations = true;
        if(!isObject)
            ThrowError(PROPAGATE_ISNT_OBJECT);

        foreach (JProperty resource in objectOption.Properties())
        {
            string copyTo = resource.Name;
            if (Path.IsPathRooted(copyTo))
                ThrowError(ROOTED_PROP_DIRECTORY, copyTo);

            string[] paths = readStringList(resource, BAD_PROP_ARRAY);
            foreach (string p in paths)
                if (Path.IsPathRooted(p))
                    ThrowError(ROOTED_PROP_FILE, copyTo, p);

            propagations.Add(new PropagateList(copyTo, paths));
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
            StreamReader reader = new StreamReader(configPath);
            rawJson = JObject.Parse(reader.ReadToEnd(), reportExactDuplicates);
            reader.Close();
        }
        catch (Newtonsoft.Json.JsonReaderException e)
        {
            ThrowError(BAD_JSON_FILE, e.Message);
        }

        return rawJson;
    }

    private void validateName()
    {
        if(name.Length == 0)
            ThrowError(BAD_NAME_FORMATTING);
        
        foreach(char c in name)
        {
            if(!(c <= 'z' && c >= 'a'))
            if(!(c <= 'Z' && c >= 'A'))
            if(!(c <= '9' && c >= '0'))
            if(!NAME_SPECIAL_CHARACTERS.Contains(c))
                ThrowError(BAD_NAME_FORMATTING);
        }

        foreach(Option o in options)
            if(o.name.Equals(name, CCIC))
                ThrowError(DUPLICATE_NAME);
    }

    // Reads a Json string list from a property assumed to exist
    private string[] readStringList(JProperty property, Error onFail)
    {
        string[] list = new string[0];
        try
        {
            JArray rawData = (JArray)property.Value;
            list = new string[rawData.Count];

            for (int i = 0; i < rawData.Count; i++)
            {
                string? currentElement = (string?)rawData[i];
                if (currentElement == null)
                    ThrowError(onFail, property.Name);
                else
                    list[i] = currentElement;
            }
        }
        // The property is not defined as an array
        catch (System.InvalidCastException) 
        { 
            ThrowError(onFail, property.Name); 
        }
        // A list's element is a Json list or object
        catch (System.ArgumentException) 
        { 
            ThrowError(onFail, property.Name); 
        }
        return list;
    }

    public override string ToString()
    {
        string formattedString = "**********\nParsedConfig Object Data:"
        + "\n==========\nOptions:\n";

        foreach(Option option in options)
            formattedString += option.ToString() + '\n';
        
        formattedString += "==========\nPropagation Lists\n";
        foreach(PropagateList resource in propagations)
            formattedString += resource.ToString();
        formattedString += "**********";
        return formattedString;
    }

    public enum Error
    {
        BAD_JSON_FILE,
        BAD_NAME_FORMATTING,
        DUPLICATE_NAME,
        BAD_OPTION_TYPE,
        PROPAGATE_ISNT_OBJECT,
        BAD_PROP_ARRAY,
        ROOTED_PROP_DIRECTORY,
        ROOTED_PROP_FILE,   
    }

    private void ThrowError(Error error, string arg0 = "", string arg1 = "")
    {
        string msg = String.Format(
            "Problem encountered with configuration file '{0}'\n",
            configPath
        );
        switch(error)
        {
            case BAD_JSON_FILE:
            msg += String.Format(
                "There is a syntax error. Printing Exception message:\n\n{0}",
                arg0 // The exception message
            );
            break;

            case BAD_NAME_FORMATTING:
            msg += String.Format(
                "The Option '{0}' has an invalid name.\n\n{1}",
                name,
                RULES_OPTION_NAME_CHARACTERS
            );
            break;

            case DUPLICATE_NAME:
            msg += String.Format(
                "The name '{0}' is used to define multiple Options.\n\n{1}",
                name,
                RULES_OPTION_NAME_CHARACTERS
            );
            break;

            case BAD_OPTION_TYPE:
            msg += String.Format(
                "The Option '{0}' is not defined in a valid way.\n\n{1}",
                name,
                RULES_OPTION_TYPE
            );
            break;

            case PROPAGATE_ISNT_OBJECT:
            msg += String.Format(
                "The '{0}' property is not defined as an object.\n\n{1}",
                PROPERTY_PROPAGATE,
                RULES_PROPAGATE_PROPERTY
            );
            break;

            case BAD_PROP_ARRAY:
            msg += String.Format(
                "The '{0}' property has an invalid sub-property '{1}'\n\n{2}",
                PROPERTY_PROPAGATE,
                arg0, // Propagate list name
                RULES_PROPAGATE_PROPERTY
            );
            break;

            case ROOTED_PROP_DIRECTORY:
            msg += String.Format(
                "The '{0}' sub-property '{1}' has a non-relative name.\n\n{2}",
                PROPERTY_PROPAGATE,
                arg0, // Propagate list name
                RULES_PROPAGATE_PROPERTY
            );
            break;

            case ROOTED_PROP_FILE:
            msg += String.Format(
                "The '{0}' list '{1}' contains non-relative path '{2}'\n\n{3}",
                PROPERTY_PROPAGATE,
                arg0, // Propagate list name
                arg1, // Propagate list element
                RULES_PROPAGATE_PROPERTY
            );
            break;
        }
        reportError(msg);
    }
}