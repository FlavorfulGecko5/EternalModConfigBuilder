using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
class EternalModConfiguration
{
    static ParsedConfig readConfig(string configFilePath)
    {
        // The final lists of decl filepaths
        // and configuration option objects that will
        // be used to construct the ParsedConfig object
        List<string> filesToCheck = new List<string>();
        List<Option> options = new List<Option>();

        // The Json file when initially read into a JObject
        JObject rawJson;
        // Each individual option divided into one JProperty each
        IEnumerable<JProperty> rawOptions;

        // The individual option we're currently iterating through
        JObject currentOption;

        // The current option's label, it's type, and
        // the index at which the character separating the 
        // type and name is located
        string currentLabel, currentType;
        int separatorIndex;

        // Used when error-checking to ensure the label is syntactically valid
        // A label must have the form %Type$Name%, where:
        // - The types are explicitly defined. Case-insensitivity is allowed but discouraged by convention.
        // - The name may only have letters (a-z, A-Z), numbers (0-9) and underscores (_)
        bool hasStartEndPercentChars, hasSingleSeparatorChar, hasValidType, hasValidName;

        JArray currentLocations;

        bool isDuplicateFile;
        try
        {
            using (StreamReader fileReader = new StreamReader(configFilePath))
            {
                rawJson = JObject.Parse(fileReader.ReadToEnd());
                rawOptions = rawJson.Properties();

                foreach (JProperty currentRawOption in rawOptions)
                {
                    currentLabel = currentRawOption.Name;
                    separatorIndex = currentLabel.IndexOf(Constants.LABEL_TYPE_NAME_SEPARATOR);
                    hasStartEndPercentChars = hasSingleSeparatorChar = hasValidType = hasValidName = true;

                    currentType = currentLabel.Substring(0, separatorIndex);

                    currentOption = (JObject)currentRawOption.Value;

                    switch (currentType)
                    {
                        case Constants.LABEL_STRING_VARIABLE:
                            options.Add(new StringOption(currentLabel, (string)currentOption["Value"]));
                            break;
                        case Constants.LABEL_TOGGLEABLE_START:
                            options.Add(new ToggleOption(currentLabel, (bool)currentOption["State"]));
                            break;
                    }

                    currentLocations = (JArray)currentOption["Locations"];


                    for (int i = 0; i < currentLocations.Count; i++)
                    {
                        isDuplicateFile = false;
                        for (int j = 0; j < filesToCheck.Count; j++)
                        {
                            if (((string)currentLocations[i]).Equals(filesToCheck[j]))
                            {
                                isDuplicateFile = true;
                                break;
                            }
                        }
                        if (!isDuplicateFile)
                        {
                            filesToCheck.Add((string)currentLocations[i]);
                        }
                    }
                }
            }
        }
        catch(System.IO.DirectoryNotFoundException) 
        { ErrorReporter.ProcessErrorCode(ErrorCode.DIRECTORY_NOT_FOUND, new string[]{configFilePath  }); }
        catch(FileNotFoundException)                
        { ErrorReporter.ProcessErrorCode(ErrorCode.CONFIG_NOT_FOUND,    new string[]{configFilePath  }); }
        catch(Newtonsoft.Json.JsonReaderException e)
        { ErrorReporter.ProcessErrorCode(ErrorCode.BAD_JSON_FILE,       new string[]{e.Message       }); }
        catch(Exception e)                          
        { ErrorReporter.ProcessErrorCode(ErrorCode.UNKNOWN_ERROR,       new string[]{e.ToString()    }); };
        return new ParsedConfig(filesToCheck, options);
    }

    static void Main(string[] args)
    {
        ParsedConfig config = readConfig("./testfiles/bad.json");
    }
}
