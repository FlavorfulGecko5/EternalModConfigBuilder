using Newtonsoft.Json.Linq;
class ParsedConfig
{
    public bool hasPropagations;
    public List<Option> options { get; }
    public List<PropagateList> propagations {get;}

    // Used to avoid constant constantly passing by value
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
                ProcessErrorCode(BAD_OPTION_TYPE, name);
            else
                options.Add(new Option(name, value));
        }
        // If the property is a json list or object
        catch (System.ArgumentException)
        {
            ProcessErrorCode(BAD_OPTION_TYPE, name);
        }
    }

    private void parsePropagate()
    {
        if (hasPropagations)
            ProcessErrorCode(DUPLICATE_NAME, name);
        hasPropagations = true;
        if(!isObject)
            ProcessErrorCode(PROPAGATE_ISNT_OBJECT);

        foreach (JProperty resource in objectOption.Properties())
        {
            string copyTo = resource.Name;
            if (Path.IsPathRooted(copyTo))
                ProcessErrorCode(ROOTED_PROP_DIRECTORY, copyTo);

            string[] paths = readStringList(resource, BAD_PROP_ARRAY);
            foreach (string p in paths)
                if (Path.IsPathRooted(p))
                    ProcessErrorCode(ROOTED_PROP_FILE, copyTo, p);

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
            ProcessErrorCode(BAD_JSON_FILE, e.Message);
        }

        return rawJson;
    }

    private void validateName()
    {
        if(name.Length == 0)
            ProcessErrorCode(BAD_NAME_FORMATTING, name);
        
        foreach(char c in name)
        {
            if(!(c <= 'z' && c >= 'a'))
            if(!(c <= 'Z' && c >= 'A'))
            if(!(c <= '9' && c >= '0'))
            if(!NAME_SPECIAL_CHARACTERS.Contains(c))
                ProcessErrorCode(BAD_NAME_FORMATTING, name);
        }

        foreach(Option o in options)
            if(o.name.Equals(name, CCIC))
                ProcessErrorCode(DUPLICATE_NAME, name);
    }

    // Reads a Json string list from a property assumed to exist
    private string[] readStringList(JProperty property, ErrorCode onFail)
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
                    ProcessErrorCode(onFail, property.Name);
                else
                    list[i] = currentElement;
            }
        }
        // The property is not defined as an array
        catch (System.InvalidCastException) 
        { 
            ProcessErrorCode(onFail, property.Name); 
        }
        // A list's element is a Json list or object
        catch (System.ArgumentException) 
        { 
            ProcessErrorCode(onFail, property.Name); 
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
}