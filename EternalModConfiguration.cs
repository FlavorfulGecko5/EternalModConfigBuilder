using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using static System.StringComparison;
using static Constants;
using static ErrorCode;
using static ErrorReporter;
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

        // The individual option we're currently iterating through
        JObject currentOption;
        // The current option's name.
        string  currentName = "";
        // The current variable option's value
        string? currentVariableValue = "";

        // The current option's Locations array
        JArray? currentLocations = new JArray();
        // Whether a Locations array is undefined or not
        bool    hasMissingLocations = false;
        // The current filepath we're iterating through - initially read into here
        string? nullCurrentFilePath = "";
        // The current filepath we're iterating through from the Locations array
        string currentFilePath = "",
        // The current filepath's file extension
               currentFileExtension = "";
        try
        {
            if (configFilePath.LastIndexOf(CONFIG_FILE_EXTENSION, CurrentCultureIgnoreCase) != configFilePath.Length - CONFIG_FILE_EXTENSION.Length)
                goto CATCH_BAD_CONFIG_EXTENSION;
            using (StreamReader fileReader = new StreamReader(configFilePath))
            {
                rawJson = JObject.Parse(fileReader.ReadToEnd());

                foreach (JProperty currentRawOption in rawJson.Properties())
                {
                    // Convert to uppercase to allow case-insensitivity
                    currentName = currentRawOption.Name.ToUpper();

                    // Validate that the name meets rules for allowed characters
                    if (currentName.Length == 0)
                        goto CATCH_BAD_NAME;
                    for (int i = 0; i < currentName.Length; i++)
                        if (!Char.IsAscii(currentName[i]) || (!Char.IsLetterOrDigit(currentName[i]) && !NAME_SPECIAL_CHARACTERS.Contains(currentName[i])))
                            goto CATCH_BAD_NAME;

                    // Check for duplicate names
                    // This won't identify exact duplicates, only duplicates with variations in capitalization
                    // (Newtonsoft seems to filter out exact-duplicate properties, using the final one defined)
                    for (int i = 0; i < options.Count; i++)
                        if (options[i].name.Equals(currentName))
                            goto CATCH_DUPLICATE_NAME;

                    // Convert the raw option from JProperty to JObject
                    try { currentOption = (JObject)currentRawOption.Value; }
                    catch (System.InvalidCastException) { goto CATCH_OPTION_ISNT_OBJECT; }

                    // Read the Option's value and create an Option object for it
                    try
                    {
                        currentVariableValue = (string?)currentOption[PROPERTY_VALUE];
                        // If the property is defined as null, undefined, or absent entirely
                        if (currentVariableValue == null)
                            goto CATCH_BAD_OPTION_VALUE;
                        options.Add(new Option(currentName, currentVariableValue));
                    }
                    // If the property is a json list or object - cannot cast these to string
                    catch (System.ArgumentException) { goto CATCH_BAD_OPTION_VALUE; };


                    // Process the Option's Locations array, checking if it's null or invalid
                    try
                    {
                        currentLocations = (JArray?)currentOption[PROPERTY_LOCATIONS];
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
                            // This is just to fix a null warning in the ErrorCode calls
                            nullCurrentFilePath = (string?)currentLocations[i];
                            if (nullCurrentFilePath == null)
                                goto CATCH_LOCATIONS_NOT_STRING_ARRAY;
                            currentFilePath = nullCurrentFilePath;

                            // Check if the file extension is valid
                            // This appears to be out-of-bounds-safe
                            j = currentFilePath.LastIndexOf('.') + 1;
                            currentFileExtension = currentFilePath.Substring(j, currentFilePath.Length - j);
                            if (!SUPPORTED_FILETYPES.Contains(currentFileExtension))
                                goto CATCH_UNSUPPORTED_FILETYPE;

                            // Check if this filePath has already been identified as having labels.
                            if (!filesToCheck.Contains(currentFilePath))
                                filesToCheck.Add(currentFilePath);
                        }
                    }
                    // If the list element is a json list or object
                    catch (System.ArgumentException) { goto CATCH_LOCATIONS_NOT_STRING_ARRAY; }
                }
            }
            return new ParsedConfig(filesToCheck, options, hasMissingLocations);
        }
        catch (System.IO.DirectoryNotFoundException)
        { ProcessErrorCode(CONFIG_DIRECTORY_NOT_FOUND, new string[] { configFilePath }); }
        catch (FileNotFoundException)
        { ProcessErrorCode(CONFIG_NOT_FOUND, new string[] { configFilePath }); }
        catch (Newtonsoft.Json.JsonReaderException e)
        { ProcessErrorCode(BAD_JSON_FILE, new string[] { e.Message }); }
        catch (Exception e)
        { ProcessErrorCode(UNKNOWN_ERROR, new string[] { e.ToString() }); }
        CATCH_BAD_CONFIG_EXTENSION:
          ProcessErrorCode(BAD_CONFIG_EXTENSION, new string[] { configFilePath });
        CATCH_BAD_NAME:
          ProcessErrorCode(BAD_NAME_FORMATTING, new string[] { currentName });
        CATCH_DUPLICATE_NAME:
          ProcessErrorCode(DUPLICATE_NAME, new string[] { currentName });
        CATCH_OPTION_ISNT_OBJECT:
          ProcessErrorCode(OPTION_ISNT_OBJECT, new string[] { currentName });
        CATCH_BAD_OPTION_VALUE:
          ProcessErrorCode(BAD_OPTION_VALUE, new string[] { currentName });
        CATCH_LOCATIONS_NOT_STRING_ARRAY:
          ProcessErrorCode(LOCATIONS_ISNT_STRING_ARRAY, new string[] { currentName });
        CATCH_UNSUPPORTED_FILETYPE:
          ProcessErrorCode(UNSUPPORTED_FILETYPE, new string[] { currentName, currentFilePath });

        // Return empty ParsedConfig to prevent warnings. This line won't ever be executed.
        return new ParsedConfig(new List<string>() { }, new List<Option>() { }, false);
    }

    static void Main(string[] args)
    {
        string configPath = "", sourcePath = "", outputPath = "";
        bool hasConfig = false, hasSource = false, hasOutput = false, 
             sourceIsZip = false, outputToZip = false;
        int i = 0;
        
        if(args.Length != EXPECTED_ARG_COUNT)
            goto CATCH_INVALID_NUMBER_ARGUMENTS;
        
        // Read in each pair of arguments
        // Validates that the same parameter hasn't been entered multiple times.
        for(i = 0; i < args.Length; i += 2)
        {
            switch(args[i].ToLower())
            {
                case "-c":
                    if(!hasConfig)
                    {
                        configPath = args[i + 1];
                        hasConfig = true;
                    }
                    else
                        goto CATCH_INVALID_ARGUMENT;
                    break;

                case "-s":
                    if(!hasSource)
                    {
                        sourcePath = args[i+1];
                        hasSource = true;
                    }
                    else
                        goto CATCH_INVALID_ARGUMENT; 
                    break;

                case "-o":
                    if(!hasOutput)
                    {
                        outputPath = args[i+1];
                        hasOutput = true;
                    }
                    else
                        goto CATCH_INVALID_ARGUMENT;
                    break;

                default:
                    goto CATCH_INVALID_ARGUMENT;
            }
        }
        
        // Check whether the input and outputs are zip files or directories
        if(sourcePath.LastIndexOf(".zip", CurrentCultureIgnoreCase) == sourcePath.Length - 4)
            sourceIsZip = true;
        if(outputPath.LastIndexOf(".zip", CurrentCultureIgnoreCase) == outputPath.Length - 4)
            outputToZip = true;
        
        ParsedConfig config = readConfig(configPath);
        config.buildMod(sourcePath, sourceIsZip, outputPath, outputToZip);
        return;

        CATCH_INVALID_NUMBER_ARGUMENTS:
        ProcessErrorCode(BAD_NUMBER_ARGUMENTS, new string[]{args.Length.ToString()});
        CATCH_INVALID_ARGUMENT:
        ProcessErrorCode(BAD_ARGUMENT,         new string[]{(i + 1).ToString()});
    }
}