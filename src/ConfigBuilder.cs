using Newtonsoft.Json.Linq;
class ConfigBuilder
{
    public static ParsedConfig buildConfig(List<string> configPaths)
    {
        ConfigBuilder builder = new ConfigBuilder();
        foreach(string path in configPaths)
            builder.parseConfigFile(path);    
        return builder.config;
    }

    public ParsedConfig config {get; private set;} = new ParsedConfig();
    private TokenReader reader = new TokenReader();

    public void parseConfigFile(string configPath)
    {
        /*
        * Read the configuration file
        */
        const string
        ERR_BAD_CFG = "The file has a syntax error. Printing Exception message:\n\n{0}";

        string name = "N/A";

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
                reader.read(property.Value);

                switch (reader.lastTokenType)
                {
                    case OptionType.STANDARD_PRIMITIVE:
                        config.addOption(name, reader.val_standardPrimitive);
                    break;

                    case OptionType.STANDARD_LIST:
                        config.addListOption(name, reader.val_standardList);
                    break;
                    
                    case OptionType.COMMENT:
                    break;

                    case OptionType.PROPAGATER:
                        config.addPropagationLists(reader.val_propagater);
                    break;
                }
            }
            catch(TokenReader.EMBConfigValueException e)
            {
                throw ConfigError(e.Message);
            }
            catch(ParsedConfig.EMBConfigNameException e)
            {
                throw ConfigError(e.Message);
            }
            catch(PropagateList.EMBPropagaterListException e)
            {
                throw ConfigError(e.Message);
            }
        }

        EMBConfigException ConfigError(string msg, string arg0 = "")
        {
            string preamble = String.Format(
                "Problem encountered in config. file '{0}' with Property '{1}':\n",
                configPath, name
            );
            string formattedMessage = String.Format(msg, arg0);

            return new EMBConfigException(preamble + formattedMessage);
        }
    }

    public class EMBConfigException : EMBException
    {
        public EMBConfigException(string msg) : base(msg) { }
    }
}