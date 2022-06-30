using Newtonsoft.Json.Linq;
using static ParsedConfig.Error;
class ParsedConfig
{
    public List<Option> options;
    public List<PropagateList> propagations;

    // Used to avoid constantly passing by value
    private string configPath, name;
    private JToken option;

    public ParsedConfig(string configPathParameter)
    {
        options = new List<Option>();
        propagations = new List<PropagateList>();
        configPath = configPathParameter;
        name = "";
        option = new JProperty("");
        parseConfig();
    }

    private void parseConfig()
    {
        foreach(JProperty property in readConfig().Properties())
        {
            name = property.Name;
            option = property.Value;
            validateName();
            if (name.Equals(PROPERTY_PROPAGATE, CCIC))
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
            string text = FileUtil.readFileText(configPath);
            rawJson = JObject.Parse(text, reportExactDuplicates);
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
            if(!(c <= 'z' && c >= 'a'))
            if(!(c <= 'Z' && c >= 'A'))
            if(!(c <= '9' && c >= '0'))
            if(!NAME_SPECIAL_CHARACTERS.Contains(c))
                ThrowError(BAD_NAME_FORMATTING);

        foreach(Option o in options)
            if(o.name.Equals(name, CCIC))
                ThrowError(DUPLICATE_NAME);
    }

    private void parseOptionValue()
    {
        string? value = JsonUtil.readTokenValue(option, true);
        if(value == null)
            ThrowError(BAD_OPTION_TYPE);
        else
            options.Add(new Option(name, value));
    }

    private void parsePropagate()
    {
        if(option.Type != JTokenType.Object)
            ThrowError(PROPAGATE_ISNT_OBJECT);

        foreach (JProperty list in ((JObject)option).Properties())
        {
            if (Path.IsPathRooted(list.Name))
                ThrowError(ROOTED_PROP_DIRECTORY, list.Name);

            string[]? filePaths = JsonUtil.readList(list.Value);
            if(filePaths == null)
                ThrowError(BAD_PROP_ARRAY, list.Name);
            else
            {
                foreach (string path in filePaths)
                    if (Path.IsPathRooted(path))
                        ThrowError(ROOTED_PROP_FILE, list.Name, path);

                propagations.Add(new PropagateList(list.Name, filePaths));
            }
        }
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
                RULES_OPTION_NAME
            );
            break;

            case DUPLICATE_NAME:
            msg += String.Format(
                "The name '{0}' is used to define multiple Options.\n\n{1}",
                name,
                RULES_OPTION_NAME
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
                name,
                RULES_PROPAGATE
            );
            break;

            case BAD_PROP_ARRAY:
            msg += String.Format(
                "The '{0}' property has an invalid sub-property '{1}'\n\n{2}",
                name,
                arg0, // Propagate list name
                RULES_PROPAGATE
            );
            break;

            case ROOTED_PROP_DIRECTORY:
            msg += String.Format(
                "The '{0}' sub-property '{1}' has a non-relative name.\n\n{2}",
                name,
                arg0, // Propagate list name
                RULES_PROPAGATE
            );
            break;

            case ROOTED_PROP_FILE:
            msg += String.Format(
                "The '{0}' list '{1}' contains non-relative path '{2}'\n\n{3}",
                name,
                arg0, // Propagate list name
                arg1, // Propagate list element
                RULES_PROPAGATE
            );
            break;
        }
        reportError(msg);
    }
}