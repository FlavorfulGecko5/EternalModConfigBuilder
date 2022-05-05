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

        // The current option's label.
        string currentLabel = "",
        // The current label's type. 
               currentType = "",
        // The current label's name
               currentName = "";
        // The index where [LABEL_TYPE_NAME_SEPARATOR] is located
        int    separatorIndex = -1,
        // The length of [LABEL_BORDER_VALUE][LABEL_TYPE_PREFACE], and the index where the type should begin
               typeStartIndex   = Constants.LABEL_BORDER_VALUE.Length + Constants.LABEL_TYPE_PREFACE.Length;

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
                    // BEGINNING OF LABEL PARSING
                    currentLabel = currentRawOption.Name;
                    try
                    {
                        separatorIndex = currentLabel.IndexOf(Constants.LABEL_TYPE_NAME_SEPARATOR, typeStartIndex);
                        if( // Does the first part of the label match the form [LABEL_BORDER_VALUE][LABEL_TYPE_PREFACE]?
                            !currentLabel.Substring(0, typeStartIndex).Equals(Constants.LABEL_BORDER_VALUE + Constants.LABEL_TYPE_PREFACE)
                            // Is the final character of the label [LABEL_BORDER_VALUE]?
                            || !currentLabel[currentLabel.Length - 1].ToString().Equals(Constants.LABEL_BORDER_VALUE)
                            // Is there exactly one [LABEL_TYPE_NAME_SEPARATOR] character in the label?
                            || (separatorIndex != currentLabel.LastIndexOf(Constants.LABEL_TYPE_NAME_SEPARATOR) || separatorIndex == -1))
                            goto CATCH_BAD_LABEL;
                        
                        // Duplicate switch statement, but this keeps the Option Label Type validation and Option Property validation
                        // completely separate from each other.
                        currentType = currentLabel.Substring(typeStartIndex, separatorIndex - typeStartIndex);
                        switch(currentType)
                        {
                            case Constants.TYPE_STRING_VARIABLE: case Constants.TYPE_TOGGLEABLE:
                                break;
                            default:
                                goto CATCH_BAD_LABEL;
                        }

                        // Validate that the label name meets rules for allowed characters
                        currentName = currentLabel.Substring(separatorIndex + 1, currentLabel.Length - separatorIndex - 2);
                        if(currentName.Length == 0)
                            goto CATCH_BAD_LABEL;
                        for(int i = 0; i < currentName.Length; i++)
                            if(!Char.IsAscii(currentName[i]) || (!Char.IsLetterOrDigit(currentName[i]) && currentName[i] != '_'))
                                goto CATCH_BAD_LABEL;
                        
                        System.Console.WriteLine("currentLabel: '" + currentLabel + "'");
                        System.Console.WriteLine("currentType: '" + currentType + "'");
                        System.Console.WriteLine("currentName: '" + currentName + "'");
                    }
                    catch(System.ArgumentOutOfRangeException) {goto CATCH_BAD_LABEL;} // Would be thrown by String.substring() if an index parameter is out of bounds.

                    // UNREVISED BEYOND THIS POINT
                    // TODO - ADD DUPLICATE LABEL CHECK
                    /*
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

                    // END OF LABEL PARSING
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
                    */
                }
            }
            return new ParsedConfig(filesToCheck, options);
        }
        catch(System.IO.DirectoryNotFoundException) 
        { ErrorReporter.ProcessErrorCode(ErrorCode.DIRECTORY_NOT_FOUND,  new string[]{configFilePath  }); }
        catch(FileNotFoundException)                
        { ErrorReporter.ProcessErrorCode(ErrorCode.CONFIG_NOT_FOUND,     new string[]{configFilePath  }); }
        catch(Newtonsoft.Json.JsonReaderException e)
        { ErrorReporter.ProcessErrorCode(ErrorCode.BAD_JSON_FILE,        new string[]{e.Message       }); }
        catch(Exception e)                          
        { ErrorReporter.ProcessErrorCode(ErrorCode.UNKNOWN_ERROR,        new string[]{e.ToString()    }); };
        CATCH_BAD_LABEL:
        System.Console.WriteLine("currentLabel: '" + currentLabel + "'");
        System.Console.WriteLine("currentType: '" + currentType + "'");
        System.Console.WriteLine("currentName: '" + currentName + "'");
          ErrorReporter.ProcessErrorCode(ErrorCode.BAD_LABEL_FORMATTING, new string[]{currentLabel    });

        return null;
    }

    static void Main(string[] args)
    {
        ParsedConfig config = readConfig("./testfiles/SampleConfig.json");
    }
}
