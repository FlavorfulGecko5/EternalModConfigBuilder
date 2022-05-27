using System.IO.Compression;
using Newtonsoft.Json.Linq;
using static System.StringComparison;
using static Constants;
using static ErrorCode;
using static ErrorReporter;
using static Util;
class EternalModConfiguration
{
    static ParsedConfig readConfig(string configFilePath)
    {
        // Data structures used to construct the ParsedConfig
        List<string> filesToCheck = new List<string>();
        List<Option> options = new List<Option>();
        List<PropagateList> resources = new List<PropagateList>();
        bool hasMissingLocations = false,
             hasPropagateProperty = false;

        // The Json file when initially read into a JObject
        JObject rawJson = new JObject(),
        // The individual option we're currently iterating through
                currentOption = new JObject();
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
            string currentName = currentRawOption.Name;
            if (currentName.Length == 0)
                ProcessErrorCode(BAD_NAME_FORMATTING, currentName);
            foreach (char c in currentName)
                if( !(c <= 'z' && c >= 'a') && !(c <= 'Z' && c >= 'A')  && !(c <= '9' && c >= '0') && !NAME_SPECIAL_CHARACTERS.Contains(c))
                    ProcessErrorCode(BAD_NAME_FORMATTING, currentName);

            // Check for duplicate names with variations in capitalization
            foreach(Option o in options)
                if (o.name.Equals(currentName, CurrentCultureIgnoreCase))
                    ProcessErrorCode(DUPLICATE_NAME, currentName);

            // Convert the raw option from JProperty to JObject
            try { currentOption = (JObject)currentRawOption.Value; }
            catch (System.InvalidCastException) { ProcessErrorCode(OPTION_ISNT_OBJECT, currentName); }

            // Check for any special properties (propagate) - above code ensures they're objects
            if (currentName.Equals(PROPAGATE_PROPERTY, CurrentCultureIgnoreCase))
            {
                if(hasPropagateProperty)  // Technically not an option, hence needing to specially check for duplicates
                    ProcessErrorCode(DUPLICATE_NAME, currentName);
                hasPropagateProperty = true;
                foreach (JProperty rawResource in currentOption.Properties())
                {
                    if(Path.IsPathRooted(rawResource.Name))
                        ProcessErrorCode(ROOTED_PROPAGATION_DIRECTORY, rawResource.Name);
                    string[]? filePaths = readRelativePathJsonStringList(currentOption[rawResource.Name], BAD_PROPAGATION_ARRAY, ROOTED_PROPAGATION_FILE, rawResource.Name);
                    if (filePaths == null)
                        ProcessErrorCode(BAD_PROPAGATION_ARRAY, rawResource.Name);
                    else
                        resources.Add(new PropagateList(rawResource.Name, filePaths));
                }
                continue;
            }

            // Read the Option's value and create an Option object for it
            try
            {
                string? currentVariableValue = (string?)currentOption[PROPERTY_VALUE];
                // If the property is defined as null, undefined, or absent entirely
                if (currentVariableValue == null)
                    ProcessErrorCode(BAD_OPTION_VALUE, currentName);
                else
                    options.Add(new Option(currentName, currentVariableValue));
            }
            // If the property is a json list or object - cannot cast these to string
            catch (System.ArgumentException) { ProcessErrorCode(BAD_OPTION_VALUE, currentName); };

            // Process the Option's Locations array, checking if it's null or invalid
            string []? currentLocations = readRelativePathJsonStringList(currentOption[PROPERTY_LOCATIONS], LOCATIONS_ISNT_STRING_ARRAY, ROOTED_LOCATIONS_FILE, currentName);
            if (currentLocations == null)
            {
                hasMissingLocations = true;
                currentLocations = new string[0];
            }
            // Process each filepath in Locations, checking if the extension is valid and eliminating duplicate filepaths
            foreach (string file in currentLocations)
            {
                if(hasValidModFileExtension(file))
                {
                    if(!filesToCheck.Contains(file))
                        filesToCheck.Add(file);
                }
                else
                    ProcessErrorCode(UNSUPPORTED_FILETYPE, currentName, file);
            }
        }
        if(hasMissingLocations)
            ProcessErrorCode(MISSING_LOCATIONS_ARRAY);
        return new ParsedConfig(filesToCheck, options, resources, hasMissingLocations);
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
}