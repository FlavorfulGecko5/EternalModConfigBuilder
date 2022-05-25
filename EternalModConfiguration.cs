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
        List<PropagateResource> resources = new List<PropagateResource>();

        // The Json file when initially read into a JObject
        JObject rawJson;

        // The individual option we're currently iterating through
        JObject currentOption = new JObject();
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
                ProcessErrorCode(BAD_CONFIG_EXTENSION, configFilePath);
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
                            ProcessErrorCode(DUPLICATE_NAME, currentName);

                    // Convert the raw option from JProperty to JObject
                    try { currentOption = (JObject)currentRawOption.Value; }
                    catch (System.InvalidCastException) { ProcessErrorCode(OPTION_ISNT_OBJECT, currentName); }

                    // Check for any special properties (propagate)
                    // TODO - Throw error if propagate was already defined using boolean value
                    if (currentName.Equals(PROPAGATE_PROPERTY))
                    {
                        resources = readPropagateData(currentOption);
                        continue;
                    }

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

                        // Process each filepath in Locations, checking if it's parseable as a Json string,
                        // and eliminating duplicate filepaths
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
                                ProcessErrorCode(UNSUPPORTED_FILETYPE, currentName, currentFilePath);
                            // Check if this filePath has already been identified as having labels.
                            else if (!filesToCheck.Contains(currentFilePath))
                                filesToCheck.Add(currentFilePath);
                        }
                    }
                    // Locations is not defined as an array
                    catch (System.InvalidCastException) { goto CATCH_LOCATIONS_NOT_STRING_ARRAY; }
                    // If the list element is a json list or object
                    catch (System.ArgumentException) { goto CATCH_LOCATIONS_NOT_STRING_ARRAY; }
                }
            }
            return new ParsedConfig(filesToCheck, options, resources, hasMissingLocations);
        }
        catch (System.IO.DirectoryNotFoundException)
        { ProcessErrorCode(CONFIG_DIRECTORY_NOT_FOUND, configFilePath); }
        catch (FileNotFoundException)
        { ProcessErrorCode(CONFIG_NOT_FOUND, configFilePath); }
        catch (Newtonsoft.Json.JsonReaderException e)
        { ProcessErrorCode(BAD_JSON_FILE, e.Message); }
        // If the same error code might be used in multiple lines, it will be called here 
        CATCH_BAD_NAME:
          ProcessErrorCode(BAD_NAME_FORMATTING, currentName);
        CATCH_BAD_OPTION_VALUE:
          ProcessErrorCode(BAD_OPTION_VALUE, currentName);
        CATCH_LOCATIONS_NOT_STRING_ARRAY:
          ProcessErrorCode(LOCATIONS_ISNT_STRING_ARRAY, currentName);      

        // Return empty ParsedConfig to prevent warnings. This line won't ever be executed.
        return new ParsedConfig(new List<string>() { }, new List<Option>() { }, new List<PropagateResource>(), false);
    }

    static List<PropagateResource> readPropagateData(JObject propagateData)
    {
        List<PropagateResource> resourceLists = new List<PropagateResource>();
        JArray? currentResource = new JArray();
        string? nullCurrentFilePath = "";
        string[] currentFilePaths;

        foreach (JProperty rawResource in propagateData.Properties())
        {
            // Check if the current sub-property is an array.
            try
            {
                currentResource = (JArray?)propagateData[rawResource.Name];
                if (currentResource == null)
                    currentResource = new JArray();

                // Process each item in the currentResource
                currentFilePaths = new string[currentResource.Count];

                for (int i = 0; i < currentResource.Count; i++)
                {
                    // This is just to fix a null warning in the ErrorCode calls
                    nullCurrentFilePath = (string?)currentResource[i];
                    if (nullCurrentFilePath == null)
                        ProcessErrorCode(BAD_PROPAGATION_ARRAY, rawResource.Name);
                    else
                        currentFilePaths[i] = nullCurrentFilePath;
                }
                resourceLists.Add(new PropagateResource(rawResource.Name, currentFilePaths));
            }
            // The resource property is not an array.
            catch (System.InvalidCastException) { ProcessErrorCode(BAD_PROPAGATION_ARRAY, rawResource.Name); }
            // If the list element is a json list or object
            catch (System.ArgumentException) { ProcessErrorCode(BAD_PROPAGATION_ARRAY, rawResource.Name); }

        }
        return resourceLists;      
    }

    static void Main(string[] args)
    {
        string configPath = "", sourcePath = "", outputPath = "";
        bool hasConfig = false, hasSource = false, hasOutput = false, 
             sourceIsZip = false, outputToZip = false;
        int i = 0;
        
        if(args.Length != EXPECTED_ARG_COUNT)
            ProcessErrorCode(BAD_NUMBER_ARGUMENTS, args.Length.ToString());
        
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
        
        try
        {
            ParsedConfig config = readConfig(configPath);
            System.Console.WriteLine(config.ToString());
            config.buildMod(sourcePath, sourceIsZip, outputPath, outputToZip);
        }
        // Any unexpected errors that arise from the building process will be caught here
        catch (Exception e) 
        {ProcessErrorCode(UNKNOWN_ERROR, e.ToString());}

        // Report successful execution
        System.Console.WriteLine(MESSAGE_SUCCESS);
        return;
        
        CATCH_INVALID_ARGUMENT:
        ProcessErrorCode(BAD_ARGUMENT,(i + 1).ToString());
    }
}