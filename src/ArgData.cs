/// <summary>
/// An object that parses and stores all data needing to be read from
/// command-line arguments. <para/>
/// </summary>
class ArgData
{
    /// <summary>
    /// Configuration file paths
    /// </summary>
    public List<string>  configPaths      {get; private set;} = new List<string>();

    /// <summary>
    /// The path to the mod to be built.
    /// </summary>
    public string        srcPath          {get; private set;} = "";

    /// <summary>
    /// The path to the built mod's output location
    /// </summary>
    public string        outPath          {get; private set;} = "";

    /// <summary>
    /// True if the input mod is a .zip file, False if it's a directory
    /// </summary>
    public bool          srcIsZip         {get; private set;} = false;

    /// <summary>
    /// True if outputting the mod to a .zip file, False if to a directory
    /// </summary>
    public bool          outToZip         {get; private set;} = false;

    /// <summary>
    /// True if any uncompressed .entities files should be compressed for the
    /// built mod, False if they shouldn't be.
    /// </summary>
    public bool          compressEntities {get; private set;} = true;

    /// <summary>
    /// The ExecutionMode to be used by EternalModBuilder
    /// </summary>
    public ExecutionMode exeMode          {get; private set;} = ExecutionMode.COMPLETE;

    /// <summary>
    /// The LogLevel to be used by EternalModBuilder
    /// </summary>
    public LogLevel      logMode          {get; private set;} = LogLevel.MINIMAL;

    /// <summary>
    /// Gets a string representation of this object
    /// </summary>
    /// <returns>A string detailing all instance variable values</returns>
    public override string ToString()
    {
        string configList = "";
        foreach(string config in configPaths)
            configList += "  -" + config + "\n";
        string sourceType = srcIsZip ? "Zip File" : "Folder";
        string outputType = outToZip ? "Zip File" : "Folder"; 

        string msg = "Parsed Command-Line Argument Data:"
            + "\n - Configuration Files:\n" + configList
            + " - Source Path: "            + srcPath
            + "\n - Source Type: "          + sourceType
            + "\n - Output Path: "          + outPath
            + "\n - Output Type: "          + outputType
            + "\n - Execution Mode: "       + exeMode.ToString()
            + "\n - Log Level: "            + logMode.ToString()
            + "\n - Compress Entities: "    + compressEntities;
        
        return msg;
    }

    /// <summary>
    /// Empty constructor for instantiating a default ArgData object
    /// </summary>
    public ArgData(){}

    /// <summary>
    /// Constructs an ArgData object by parsing a set of well-formatted
    /// command line arguments.
    /// </summary>
    /// <param name="args">Command-line arguments</param>
    /// <exception cref="EMBArgumentException">Parsing fails for any predicted reason</exception>
    public ArgData(string[] args)
    {
        const string // Error messages used when throwing Exceptions
        ERR_NUM_ARGS = "Please enter an even number of arguments.\n\n{0}",
        ERR_BAD_PARAM = "'{0}' is not a valid parameter.\n\n{1}",
        ERR_DUPE_ARG = "You may only input one {0}.\n\n{1}",
        ERR_MISSING_ARGS = "Missing required command line argument(s).\n\n{0}",
        ERR_ENTITIES = "Compress-entities needs a true/false value.\n\n{0}",
        ERR_LOGLEVEL = "'{0}' is not a valid Log Level.\n\n{1}",
        ERR_EXEMODE = "'{0}' is not a valid Execution Mode.\n\n{1}";
        
        // Required arguments
        bool hasConfig = false, hasSource = false, hasOutput = false;

        // Optional arguments - default values will be used
        bool hasCompEnts = false, hasExecutionMode = false, hasLogLevel = false;

        if (args.Length % 2 != 0)
            throw ArgError(ERR_NUM_ARGS, RULES_USAGE_MINIMAL);

        for (int i = 0; i < args.Length; i += 2)
        {
            switch (args[i].ToLower())
            {
                case "-c":
                    configPaths.Add(args[i + 1]);
                    hasConfig = true;
                break;

                case "-s":
                    if(hasSource)
                        throw duplicateArg("Mod");
                    srcPath = args[i + 1];
                    hasSource = true;
                break;

                case "-o":
                    if(hasOutput) 
                        throw duplicateArg("output location");
                    outPath = args[i + 1];
                    hasOutput = true;
                break;

                case "-e":
                    if(hasCompEnts)
                        throw duplicateArg("compress-entites setting");
                    try
                    {
                        compressEntities = Boolean.Parse(args[i+1]);
                    }
                    catch(System.FormatException)
                    {
                        throw ArgError(ERR_ENTITIES, DESC_COMP_ENTITIES);
                    }
                    hasCompEnts = true;
                break;
                
                // The string -> Enum parsing algorithm used below is
                // extremely slow, but acceptable since it may only occur
                // twice per execution.
                case "-x":
                    if(hasExecutionMode)
                        throw duplicateArg("execution mode");
                    try
                    {
                        exeMode = (ExecutionMode)Enum.Parse(
                            typeof(ExecutionMode), args[i+1].ToUpper());
                    }
                    catch(Exception)
                    {
                        throw ArgError(ERR_EXEMODE, args[i+1], DESC_EXEMODE);
                    }
                    hasExecutionMode = true;
                break;
                
                case "-l":
                    if(hasLogLevel)
                        throw duplicateArg("log level");
                    try
                    {
                        logMode = (LogLevel)Enum.Parse(
                                typeof(LogLevel), args[i+1].ToUpper());
                    }
                    catch(Exception) 
                    {
                        throw ArgError(ERR_LOGLEVEL, args[i+1], DESC_LOGLEVEL);
                    }
                    hasLogLevel = true;
                break;

                default:
                    throw ArgError(ERR_BAD_PARAM, args[i], RULES_USAGE_MINIMAL);
            }
            EMBArgumentException duplicateArg(string type)
            {
                return ArgError(ERR_DUPE_ARG, type, RULES_USAGE_MINIMAL);
            }
        }

        if(!hasConfig || !hasSource || !hasOutput)
            throw ArgError(ERR_MISSING_ARGS, RULES_USAGE_MINIMAL);

        // Due to aggregate data and dependencies on each other
        // (i.e. inputting multiple config. files, source path needed
        // to validate the output path), these validation functions should
        // only execute when all args have been parsed, and in this order
        validateConfigArg();
        validateSourceArg();
        validateOutputArg();
        if(EternalModBuilder.mustLog(LogLevel.CONFIGS))
            EternalModBuilder.log(ToString());
    }

    /// <summary>
    /// Validates all parsed configuration filepaths
    /// </summary>
    /// <exception cref="EMBArgumentException">A filepath cannot be validated</exception>
    private void validateConfigArg()
    {
        const string
        ERR_BAD_EXT = "The configuration file '{0}' must be a {1} file.",
        ERR_CFG_MISSING = "Failed to find the configuration file '{0}'";

        foreach(string config in configPaths)
        {
            // Ensure the file extension is acceptable
            if(!config.EndsWithCCIC(".json") && !config.EndsWithCCIC(".txt"))
                throw ArgError(ERR_BAD_EXT, config, DESC_CFG_EXTENSIONS);
            
            // Ensure the file exists
            if (!File.Exists(config))
                throw ArgError(ERR_CFG_MISSING, config);
        }

    }

    /// <summary>
    /// Validates the parsed source path argument
    /// </summary>
    /// <exception cref="EMBArgumentException">The source path cannot be validated</exception>
    private void validateSourceArg()
    {
        const string 
        ERR_MOD_INVALID = "The mod file is not a valid directory or .zip file.",
        ERR_MOD_MISSING = "The mod directory or .zip file does not exist.",
        ERR_MOD_SIZE = "Your mod may not be larger than ~{0} gigabytes.";

        // The mod must be a valid zip file or directory that does not
        // exceed the maximum mod size requirement
        if(File.Exists(srcPath))
        {
            if (!ZipUtil.isFileValidZip(srcPath))
                throw ArgError(ERR_MOD_INVALID);

            if(FSUtil.isFileLarge(srcPath))
                throw modTooLarge();

            srcIsZip = true;
        }
        else if (Directory.Exists(srcPath))
        {
            if(FSUtil.isDirectoryLarge(srcPath))
                throw modTooLarge();
        }
        else
            throw ArgError(ERR_MOD_MISSING);
        
        EMBArgumentException modTooLarge()
        {
            return ArgError(ERR_MOD_SIZE, (MAX_INPUT_SIZE_BYTES / 1000000000.0).ToString());
        }
    }

    /// <summary>
    /// Validates the parsed output path argument
    /// </summary>
    /// <exception cref="EMBArgumentException">The output path cannot be validated</exception>
    private void validateOutputArg()
    {
        const string
        ERR_FILE_EXISTS = "A file already exists at the output path.\n\n{0}",
        ERR_NONEMPTY_FOLDER = "A non-empty folder exists at the output path.\n\n{0}",
        ERR_OUTPUT_IN_SRC = "Your output path cannot be inside your mod folder.\n\n{0}";

        // The output path is invalid if any data already
        // exists at that location
        if (File.Exists(outPath))
            throw ArgError(ERR_FILE_EXISTS, RULES_OUTPUT);
        else if (Directory.Exists(outPath))
            if(FSUtil.dirContainsData(outPath))
                throw ArgError(ERR_NONEMPTY_FOLDER, RULES_OUTPUT);

        // If not prevented, this can cause a recursive folder copy loop.
        if (!srcIsZip) 
            if(FSUtil.isParentDir(srcPath, outPath))
                throw ArgError(ERR_OUTPUT_IN_SRC, RULES_OUTPUT);
        outToZip = outPath.EndsWithCCIC(".zip");
    }

    /// <summary>
    /// Generates an Exception with a formatted message based on the supplied
    /// string inputs.
    /// </summary>
    /// <param name="msg">The message body</param>
    /// <param name="arg0">Optional first argument</param>
    /// <param name="arg1">Option second argument</param>
    /// <returns>An EMBArgumentException containing the formatted message</returns>
    private EMBArgumentException ArgError(string msg, string arg0="", string arg1 = "")
    {
        string formattedMessage = "Failed to parse command-line arguments:\n"
            + String.Format(msg, arg0, arg1);
        return new EMBArgumentException(formattedMessage);
    }

    /// <summary>
    /// Used to represent and report any predictable errors that arise
    /// when parsing EternalModBuilder's command-line arguments
    /// </summary>
    public class EMBArgumentException : EMBException
    {
        /// <summary>
        /// Instantiates a new EMBArgumentException
        /// </summary>
        /// <param name="msg">The Exception message</param>
        public EMBArgumentException(string msg) : base (msg) {}
    }
}