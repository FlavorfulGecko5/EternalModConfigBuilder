using System.ComponentModel;
/// <summary>
/// Describes all modes EternalModBuilder can run in
/// </summary>
public enum ExecutionMode
{
    /// <summary> Default mode where all build operations are performed </summary>
    [Description("Reads config. files and performs all build operations (default)")]
    COMPLETE,
    /// <summary> Only read config. data without building the mod </summary>
    [Description("Reads config. files but performs no build operations.")]
    READONLY,
    /// <summary> Read config. data and parse files for labels </summary>
    [Description("Reads config. files and parses labels.")]
    PARSE,
    /// <summary> Read config. data and propagate files </summary>
    [Description("Reads config. files and propagates files.")]
    PROPAGATE
}

/// <summary>
/// Describes all levels EternalModBuilder's logging feature can operate at
/// </summary>
public enum LogLevel
{
    /// <summary> Default level where only errors and warnings should be output </summary>
    [Description("Only outputs errors and warnings (default)")]
    MINIMAL,
    /// <summary> Should output parsed command-line argument and config. file data </summary>
    [Description("Outputs parsed command-line argument and configuration file data.")]
    CONFIGS,
    /// <summary> Should output what each label's expression resolves to </summary>
    [Description("Outputs what each label's expression resolved to.")]
    PARSINGS,
    /// <summary> Should output the result of each propagation </summary>
    [Description("Outputs each successful propagation")]
    PROPAGATIONS,
    /// <summary> Should output everything logged by other log levels.</summary>
    [Description("Outputs everything")]
    VERBOSE
}

/// <summary>
/// An object that parses and stores all data needing to be read from
/// command-line arguments. <para/>
/// Defines all behavior pertaining to command-line argument format and usage
/// </summary>
class ArgData
{
    /* Program Configuration Constants */

    /// <summary>
    /// The maximum size allowed for the mod input, in bytes
    /// </summary>
    const long MAX_INPUT_SIZE_BYTES = 2000000000;

    /// <summary>
    /// Describes all extensions a valid configuration file may have
    /// </summary>
    const string DESC_CFG_EXTENSIONS = ".txt or .json";





    /* Constants Defining Program Rules and Behavior */

    /// <summary>
    /// Defines the input instructions for the Compress Entities argument
    /// and how this argument will be utilized
    /// </summary>
    const string RULES_COMP_ENTITIES = "Optional Parameter - Compress Entities\n"
    + "-e [ true | false ] (Choose One)\n"
    + "If true (default), decompressed .entities files will be compressed when building the mod.\n"
    + "If false, they will not be compressed.\n"
    + "Compression can not occur if the execution mode is 'readonly' or 'propagate'";

    /// <summary>
    /// Defines the input instructions for the Execution Mode argument
    /// and how this argument will be utilized
    /// </summary>
    static readonly string RULES_EXEMODE = new String("Optional Parameter - Execution Mode\n"
    + "-x [mode] (Choose one of the following):\n" + EnumUtil.EnumToString<ExecutionMode>());

    /// <summary>
    /// Defines the input instructions for the Log Level argument
    /// and how this argument will be utilized
    /// </summary>
    static readonly string RULES_LOGLEVEL = "Optional Parameter - Log Level\n"
    + "-l [level] (Choose one of the following):\n" + EnumUtil.EnumToString<LogLevel>();

    /// <summary>
    /// Defines the rules your output location must follow and how
    /// EternalModBuilder utilizes the temporary directory
    /// </summary>
    const string RULES_OUTPUT = "Your output location must obey these rules:\n" 
    + "- If outputting to a folder, it must be empty or non-existant, unless named '" 
        + EternalModBuilder.DIR_TEMP + "'.\n"
    + "-- This directory is ALWAYS deleted when this program is executed.\n"
    + "- No file may already exist at the output location.\n"
    + "- Your output path cannot be inside of your source directory.";

    /// <summary>
    /// Shows how to run EternalModBuilder with only the mandatory
    /// command-line arguments
    /// </summary>
    const string RULES_USAGE_GENERAL = "Usage: ./EternalModBuilder -c [config "
    + DESC_CFG_EXTENSIONS + "] -s [mod folder or .zip] -o [output folder or .zip]\n"
    + "You may enter multiple configuration files (use '-c' once per file).\n\n";

    /// <summary>
    /// Shows how to run EternalModBuilder with only the mandatory command-line
    /// arguments. Details how to display information for all arguments.
    /// </summary>
    const string RULES_USAGE_MINIMAL = RULES_USAGE_GENERAL
    + "For information on optional parameters, run this application with 0 arguments.";

    /// <summary>
    /// Shows how to run EternalModBuilder by outputting information on
    /// all possible command-line arguments and how they function
    /// </summary>
    public static readonly string RULES_USAGE_VERBOSE = RULES_USAGE_GENERAL
    + RULES_COMP_ENTITIES + "\n\n" + RULES_EXEMODE + "\n\n" + RULES_LOGLEVEL + "\n";





    /* Instance Fields and Methods */

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
    /// <exception cref="EMBArgumentException">
    /// Argument parsing or validation fails for any predicted reason
    /// </exception>
    public ArgData(string[] args)
    {
        EMBArgumentException ArgError(string msg, string arg0 = "", string arg1 = "")
        {
            string formattedMessage = "Failed to parse command-line arguments:\n"
                + String.Format(msg, arg0, arg1);
            return new EMBArgumentException(formattedMessage);
        }

        /*
        * Read all arguments to variables 
        */
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
                        throw ArgError(ERR_ENTITIES, RULES_COMP_ENTITIES);
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
                        throw ArgError(ERR_EXEMODE, args[i+1], RULES_EXEMODE);
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
                        throw ArgError(ERR_LOGLEVEL, args[i+1], RULES_LOGLEVEL);
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

        /*
        * Verify all configuration files are valid
        */
        const string
        ERR_CFG_EXT = "The configuration file '{0}' must be a {1} file.",
        ERR_CFG_MISSING = "Failed to find the configuration file '{0}'";

        foreach(string config in configPaths)
        {
            // Ensure the file extension is acceptable
            if(!config.EndsWithCCIC(".json") && !config.EndsWithCCIC(".txt"))
                throw ArgError(ERR_CFG_EXT, config, DESC_CFG_EXTENSIONS);
            
            // Ensure the file exists
            if (!File.Exists(config))
                throw ArgError(ERR_CFG_MISSING, config);
        }

        /*
        * Validate the source file argument and determine if it's a
        * zip file or a directory
        */
        const string 
        ERR_MOD_INVALID = "The mod file is not a valid directory or .zip file.",
        ERR_MOD_MISSING = "The mod directory or .zip file does not exist.",
        ERR_MOD_SIZE = "Your mod may not be larger than ~{0} gigabytes.";

        bool modTooLarge = false;

        // The mod must be a valid zip file or directory that does not
        // exceed the maximum mod size requirement
        if(File.Exists(srcPath))
        {
            if (!ZipUtil.isFileValidZip(srcPath))
                throw ArgError(ERR_MOD_INVALID);
            
            modTooLarge = FSUtil.isFileLarge(srcPath, MAX_INPUT_SIZE_BYTES);
            srcIsZip = true;
        }
        else if (Directory.Exists(srcPath))
            modTooLarge = FSUtil.isDirectoryLarge(srcPath, MAX_INPUT_SIZE_BYTES);
        else
            throw ArgError(ERR_MOD_MISSING);
        
        if(modTooLarge)
            throw ArgError(ERR_MOD_SIZE, (MAX_INPUT_SIZE_BYTES / 1000000000.0).ToString());
        
        /*
        * Validate the output argument and determine if we're outputting to
        * a zip file or a directory
        */
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