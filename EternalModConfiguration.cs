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
               typeStartIndex = Constants.LABEL_BORDER_VALUE.Length + Constants.LABEL_TYPE_PREFACE.Length;

        // The current variable option's value
        string? currentVariableValue = "";
        // The current toggleable option's value
        bool?   currentToggleableValue = false;

        // The current option's Locations array
        JArray? currentLocations = new JArray();
        // Whether a Locations array is undefined or not
        bool    hasMissingLocations = false;
        // The current filepath we're iterating through - initially read into here
        string? nullCurrentFilePath = "";
        // The current filepath we're iterating through from the Locations array
        string  currentFilePath = "",
        // The current filepath's file extension
                currentFileExtension = "";
        try
        {
            using (StreamReader fileReader = new StreamReader(configFilePath))
            {
                rawJson = JObject.Parse(fileReader.ReadToEnd());
                rawOptions = rawJson.Properties();

                foreach (JProperty currentRawOption in rawOptions)
                {
                    // The label syntax is parsed and error-checked inside this try-block
                    currentLabel = currentRawOption.Name.ToUpper();
                    try
                    {
                        separatorIndex = currentLabel.IndexOf(Constants.LABEL_TYPE_NAME_SEPARATOR, typeStartIndex);
                        if ( // Does the first part of the label match the form [LABEL_BORDER_VALUE][LABEL_TYPE_PREFACE]?
                            !currentLabel.Substring(0, typeStartIndex).Equals(Constants.LABEL_BORDER_VALUE + Constants.LABEL_TYPE_PREFACE)
                            // Is the final character of the label [LABEL_BORDER_VALUE]?
                            || !currentLabel[currentLabel.Length - 1].ToString().Equals(Constants.LABEL_BORDER_VALUE)
                            // Is there exactly one [LABEL_TYPE_NAME_SEPARATOR] character in the label?
                            || (separatorIndex != currentLabel.LastIndexOf(Constants.LABEL_TYPE_NAME_SEPARATOR) || separatorIndex == -1))
                            goto CATCH_BAD_LABEL;

                        // Duplicate switch statement, but this keeps the Option Label Type validation and Option Property validation
                        // completely separate from each other.
                        currentType = currentLabel.Substring(typeStartIndex, separatorIndex - typeStartIndex);
                        switch (currentType)
                        {
                            case Constants.TYPE_VARIABLE:
                            case Constants.TYPE_TOGGLEABLE:
                                break;
                            default:
                                goto CATCH_BAD_LABEL;
                        }

                        // Validate that the label name meets rules for allowed characters
                        currentName = currentLabel.Substring(separatorIndex + 1, currentLabel.Length - separatorIndex - 2);
                        if (currentName.Length == 0)
                            goto CATCH_BAD_LABEL;
                        for (int i = 0; i < currentName.Length; i++)
                            if (!Char.IsAscii(currentName[i]) || (!Char.IsLetterOrDigit(currentName[i]) && currentName[i] != '_'))
                                goto CATCH_BAD_LABEL;
                    }
                    // Will be thrown by String.substring() if an index/length parameter is out of bounds.
                    catch (System.ArgumentOutOfRangeException) { goto CATCH_BAD_LABEL; }       

                    // Convert the raw option from JProperty to JObject
                    try { currentOption = (JObject)currentRawOption.Value; }
                    catch (System.InvalidCastException) { goto CATCH_OPTION_ISNT_OBJECT; }

                    // Read the Option's value and create the appropriate 
                    // Option object based upon it's type
                    switch (currentType)
                    {
                        case Constants.TYPE_VARIABLE:
                            try
                            {
                                currentVariableValue = (string?)currentOption[Constants.PROPERTY_NAME_VALUE];
                                // If the property is defined as null, undefined, or absent entirely
                                if (currentVariableValue == null)
                                    goto CATCH_BAD_VARIABLE_VALUE;
                                options.Add(new VariableOption(currentLabel, currentVariableValue));
                            }
                            // If the property is a list or object - cannot cast these to string
                            catch (System.ArgumentException) { goto CATCH_BAD_VARIABLE_VALUE; };
                            break;

                        case Constants.TYPE_TOGGLEABLE:
                            try
                            {
                                currentToggleableValue = (bool?)currentOption[Constants.PROPERTY_NAME_VALUE];
                                // If the property is defined as null, undefined, or absent entirely
                                if (currentToggleableValue == null)
                                    goto CATCH_BAD_TOGGLEABLE_VALUE;
                                options.Add(new ToggleOption(currentLabel, (bool)currentToggleableValue));
                            }
                            // If the property is a string that doesn't convert to 'true' or 'false'
                            catch (System.FormatException) { goto CATCH_BAD_TOGGLEABLE_VALUE; }
                            // If the property is a list or object - cannot cast these to string
                            catch (System.ArgumentException) { goto CATCH_BAD_TOGGLEABLE_VALUE; }
                            break;
                    }

                    // Process the Option's Locations array, checking if it's null or invalid
                    try
                    {
                        currentLocations = (JArray?)currentOption[Constants.PROPERTY_NAME_LOCATIONS];
                        if (currentLocations == null)
                        {
                            hasMissingLocations = true;
                            currentLocations = new JArray();
                        }
                    }
                    catch (System.InvalidCastException) { goto CATCH_LOCATIONS_NOT_STRING_ARRAY; }

                    // Process each filepath in Locations, checking if it's parseable as a Json string,
                    // and eliminating duplicate filepaths
                    try
                    {
                        for (int i = 0, j = 0; i < currentLocations.Count; i++)
                        {
                            nullCurrentFilePath = (string?)currentLocations[i];
                            if(nullCurrentFilePath == null)
                                goto CATCH_LOCATIONS_NOT_STRING_ARRAY;

                            // This is just to fix a warning in the ErrorCode calls
                            currentFilePath = nullCurrentFilePath;
                            
                            // Check if the file extension is valid
                            // This appears to be out-of-bounds-safe
                            j = currentFilePath.LastIndexOf('.') + 1;
                            currentFileExtension = currentFilePath.Substring(j, currentFilePath.Length - j);
                            if(!Constants.SUPPORTED_FILETYPES.Contains(currentFileExtension))
                                goto CATCH_UNSUPPORTED_FILETYPE;
                            
                            // Check if this filePath has already been identified as having labels.
                            if(!filesToCheck.Contains(currentFilePath))
                                filesToCheck.Add(currentFilePath);
                        }
                    }
                    catch (System.ArgumentException) {goto CATCH_LOCATIONS_NOT_STRING_ARRAY;}
                }
            }
            return new ParsedConfig(filesToCheck, options, hasMissingLocations);
        }
        catch (System.IO.DirectoryNotFoundException)
        { ErrorReporter.ProcessErrorCode(ErrorCode.DIRECTORY_NOT_FOUND,         new string[] { configFilePath }); }
        catch (FileNotFoundException)
        { ErrorReporter.ProcessErrorCode(ErrorCode.CONFIG_NOT_FOUND,            new string[] { configFilePath }); }
        catch (Newtonsoft.Json.JsonReaderException e)
        { ErrorReporter.ProcessErrorCode(ErrorCode.BAD_JSON_FILE,               new string[] { e.Message }); }
        catch (Exception e)
        { ErrorReporter.ProcessErrorCode(ErrorCode.UNKNOWN_ERROR,               new string[] { e.ToString() }); }
        CATCH_BAD_LABEL:
          ErrorReporter.ProcessErrorCode(ErrorCode.BAD_LABEL_FORMATTING,        new string[] { currentLabel });
        CATCH_OPTION_ISNT_OBJECT:
          ErrorReporter.ProcessErrorCode(ErrorCode.OPTION_ISNT_OBJECT,          new string[] { currentLabel });
        CATCH_BAD_VARIABLE_VALUE:
          ErrorReporter.ProcessErrorCode(ErrorCode.BAD_VARIABLE_VALUE,          new string[] { currentLabel });
        CATCH_BAD_TOGGLEABLE_VALUE:
          ErrorReporter.ProcessErrorCode(ErrorCode.BAD_TOGGLEABLE_VALUE,        new string[] { currentLabel });
        CATCH_LOCATIONS_NOT_STRING_ARRAY:
          ErrorReporter.ProcessErrorCode(ErrorCode.LOCATIONS_ISNT_STRING_ARRAY, new string[] { currentLabel });
        CATCH_UNSUPPORTED_FILETYPE:
          ErrorReporter.ProcessErrorCode(ErrorCode.UNSUPPORTED_FILETYPE,        new string[] { currentLabel, currentFilePath });

        // Return empty ParsedConfig to prevent warnings. This line won't ever be executed.
        return new ParsedConfig(new List<string>() { }, new List<Option>() { }, false);
    }

    static void Main(string[] args)
    {
        ParsedConfig config = readConfig("./testfiles/variableDemoConfig.json");
        System.Console.WriteLine(config.ToString());
        config.buildMod("sourceDirectory", "outputDirectory");
    }
}