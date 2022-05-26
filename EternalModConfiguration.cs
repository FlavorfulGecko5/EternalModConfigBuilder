using System.IO.Compression;
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
        JObject rawJson = new JObject();

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
            StreamReader fileReader = new StreamReader(configFilePath);
            // Allows for detection of exact duplicates.
            rawJson = JObject.Parse(fileReader.ReadToEnd(), new JsonLoadSettings() { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            fileReader.Close();
        }
        catch (Newtonsoft.Json.JsonReaderException e)
        { ProcessErrorCode(BAD_JSON_FILE, e.Message); }

        foreach (JProperty currentRawOption in rawJson.Properties())
        {
            // Validate that the name meets rules for allowed characters
            currentName = currentRawOption.Name;
            if (currentName.Length == 0)
                ProcessErrorCode(BAD_NAME_FORMATTING, currentName);
            for (int i = 0; i < currentName.Length; i++)
                if( !(currentName[i] <= 'z' && currentName[i] >= 'a') && !(currentName[i] <= 'Z' && currentName[i] >= 'A') 
                    && !(currentName[i] <= '9' && currentName[i] >= '0') && !NAME_SPECIAL_CHARACTERS.Contains(currentName[i]))
                    ProcessErrorCode(BAD_NAME_FORMATTING, currentName);

            // Check for duplicate names with variations in capitalization
            for (int i = 0; i < options.Count; i++)
                if (options[i].name.Equals(currentName, CurrentCultureIgnoreCase))
                    ProcessErrorCode(DUPLICATE_NAME, currentName);

            // Convert the raw option from JProperty to JObject
            try { currentOption = (JObject)currentRawOption.Value; }
            catch (System.InvalidCastException) { ProcessErrorCode(OPTION_ISNT_OBJECT, currentName); }

            // Check for any special properties (propagate)
            // The above code protects against duplicate special properties,
            // and ensures the special properties are Json objects.
            if (currentName.Equals(PROPAGATE_PROPERTY, CurrentCultureIgnoreCase))
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
                    ProcessErrorCode(BAD_OPTION_VALUE, currentName);
                else
                    options.Add(new Option(currentName, currentVariableValue));
            }
            // If the property is a json list or object - cannot cast these to string
            catch (System.ArgumentException) { ProcessErrorCode(BAD_OPTION_VALUE, currentName); };


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
                        ProcessErrorCode(LOCATIONS_ISNT_STRING_ARRAY, currentName);
                    else
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
            catch (System.InvalidCastException) { ProcessErrorCode(LOCATIONS_ISNT_STRING_ARRAY, currentName); }
            // If the list element is a json list or object
            catch (System.ArgumentException) { ProcessErrorCode(LOCATIONS_ISNT_STRING_ARRAY, currentName); }
        }
        return new ParsedConfig(filesToCheck, options, resources, hasMissingLocations);
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
        // The temporary directory is not removed when the program terminates due to errors.
        // This leads to future executions failing because you can't unzip to an already-existing directory.
        if (Directory.Exists(TEMPORARY_DIRECTORY))
            Directory.Delete(TEMPORARY_DIRECTORY, true);
        
        /*******************************
         * READ COMMAND-LINE ARGUMENTS *
         *******************************/
        string configPath = "", sourcePath = "", outputPath = "";
        bool hasConfig = false, hasSource = false, hasOutput = false;
        
        if(args.Length != EXPECTED_ARG_COUNT)
            ProcessErrorCode(BAD_NUMBER_ARGUMENTS, args.Length.ToString());
        
        // Read in each pair of arguments
        // Validates that the same parameter hasn't been entered multiple times.
        for(int i = 0; i < args.Length; i += 2)
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
                    CATCH_INVALID_ARGUMENT:
                    ProcessErrorCode(BAD_ARGUMENT, (i + 1).ToString());
                    break;
            }
        }

        /***************************************************
         * VALIDATE FILEPATH ARGUMENT TYPES AND EXISTENCES *
         ***************************************************/
        /*
        * First validate that the config file has the proper extension, and that it exists.
        */
        if(!hasExtension(configPath, CONFIG_FILE_EXTENSION))
            ProcessErrorCode(BAD_CONFIG_EXTENSION, configPath);
        if(!File.Exists(configPath))
            ProcessErrorCode(CONFIG_NOT_FOUND, configPath);

        /*
        * Next validate whether the source is a valid zip file or directory, and if it exists.
        * Detects edge cases (such as a directory ending in .zip, or other file types ending in .zip)
        */
        bool sourceIsZip = false;
        // The source is a file
        if(File.Exists(sourcePath))
        {
            // Does it have a .zip extension
            if(hasExtension(sourcePath, ".zip"))
                // Is it an actual, valid zip file or some other file disguised as a zip file.
                // This level of error checking is likely completely unnecessary to implement, but whatever.
                try { using (ZipArchive z = ZipFile.OpenRead(sourcePath))
                {
                    System.Collections.ObjectModel.ReadOnlyCollection<ZipArchiveEntry> entries = z.Entries;
                    sourceIsZip = true;
                }}
                catch (InvalidDataException) {ProcessErrorCode(MOD_NOT_VALID, sourcePath);}
                catch(Exception e){ProcessErrorCode(UNKNOWN_ERROR, e.ToString());}
            else
                ProcessErrorCode(MOD_NOT_VALID, sourcePath);
        }
        // Source does not exist as a directory either
        else if(!Directory.Exists(sourcePath))
            ProcessErrorCode(MOD_NOT_FOUND, sourcePath);

        /*
        * Finally, determine whether the output is a valid zip file or directory
        */
        bool outputToZip = hasExtension(outputPath, ".zip");
        // Any pre-existing file at the output location will throw an error
        if(File.Exists(outputPath))
            ProcessErrorCode(OUTPUT_PREEXISTING_FILE, outputPath);
        // A non-empty directory at the output location will throw an error
        else if(Directory.Exists(outputPath))
            try
            {
                if(Directory.EnumerateFileSystemEntries(outputPath).Any())
                    ProcessErrorCode(OUTPUT_NONEMPTY_DIRECTORY, outputPath);
            }
            catch(Exception e) {ProcessErrorCode(UNKNOWN_ERROR, e.ToString());}

        /*************
         * BUILD MOD *
         *************/
        try
        {
            ParsedConfig config = readConfig(configPath);
            System.Console.WriteLine(config.ToString());
            config.buildMod(sourcePath, sourceIsZip, outputPath, outputToZip);
        }
        // Any unexpected errors that arise from the building process will be caught here
        catch (Exception e) 
        {ProcessErrorCode(UNKNOWN_ERROR, e.ToString());}

        System.Console.WriteLine(MESSAGE_SUCCESS);
    }

    static bool hasExtension(string filePath, string extension)
    {
        int extensionIndex = filePath.LastIndexOf(extension, CurrentCultureIgnoreCase);
        if(extensionIndex > -1)
            if(extensionIndex == filePath.Length - extension.Length)
                return true;
        return false;
    }
}