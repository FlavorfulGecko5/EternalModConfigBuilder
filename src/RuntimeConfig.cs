global using static Constants;
using static RuntimeConfig.ArgumentError;
class RuntimeConfig
{
    public static List<string> configPaths {get; private set;} = new List<string>();
    public static string srcPath  {get; private set;} = "";
    public static string outPath  {get; private set;} = "";
    public static bool   srcIsZip {get; private set;} = false;
    public static bool   outToZip {get; private set;} = false;

    public static ExecutionMode exeMode {get; private set;} = ExecutionMode.COMPLETE;
    public static LogLevel logMode {get; private set;} = LogLevel.MINIMAL;

    public static void Main(string[] args)
    {
        try
        {
            if(args.Length == 0)
                Console.WriteLine(RULES_USAGE_VERBOSE);
            else 
                run(args);
            Environment.Exit(0);
        }
        catch (EMBException e)
        {
            reportError(e.Message);
            Environment.Exit(1);
        }
        catch (Exception e)
        {
            reportError("An unknown error occurred, printing Exception:\n\n" 
                + e.ToString());
            Environment.Exit(2);
        }
    }

    private static void reportError(string msg)
    {
        Console.WriteLine(MSG_ERROR + msg + MSG_FAILURE);
    }

    private static void run(string[] args)
    {
        if (Directory.Exists(DIR_TEMP))
            Directory.Delete(DIR_TEMP, true);

        Console.WriteLine(MSG_WELCOME);
        initialize(args);
        ModBuilder builder = new ModBuilder();
        builder.buildMod();
        Console.WriteLine(MSG_SUCCESS);
    }

    private static void initialize(string[] args)
    {
        readToVariables(args);
        validateConfigArg();
        validateSourceArg();
        validateOutputArg();

        LogMaker logger = new LogMaker(LogLevel.CONFIGS);
        if(logger.mustLog)
        {
            logger.appendString(logToString());
            logger.log();
        }
    }

    public static string logToString()
    {
        string configList = "";
        foreach(string config in configPaths)
            configList += "  -" + config + "\n";
        string sourceType = srcIsZip ? "Zip File" : "Folder";
        string outputType = outToZip ? "Zip File" : "Folder"; 

        string msg = "Parsed Command-Line Argument Data:"
            + "\n - Configuration Files:\n" + configList
            + " - Source Path: " + srcPath
            + "\n - Source Type: " + sourceType
            + "\n - Output Path: " + outPath
            + "\n - Output Type: " + outputType
            + "\n - Execution Mode: " + exeMode.ToString()
            + "\n - Log Level: " + logMode.ToString();
        
        return msg;
    }

    private static void readToVariables(string[] args)
    {
        // Must-have
        bool hasConfig = false, hasSource = false, hasOutput = false;

        // Optional
        bool hasExecutionMode = false, hasLogLevel = false;

        if (args.Length % 2 != 0)
            throw ArgError(BAD_NUMBER_ARGUMENTS);

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
                        throw ArgError(DUPLICATE_ARGUMENT, "Mod");
                    srcPath = args[i + 1];
                    hasSource = true;
                break;

                case "-o":
                    if(hasOutput) 
                        throw ArgError(DUPLICATE_ARGUMENT, "output location");
                    outPath = args[i + 1];
                    hasOutput = true;
                break;
                
                case "-x":
                    if(hasExecutionMode)
                        throw ArgError(DUPLICATE_ARGUMENT, "execution mode");
                    try
                    {
                        exeMode = (ExecutionMode)Enum.Parse(
                            typeof(ExecutionMode), args[i+1].ToUpper());
                    }
                    catch(Exception)
                    {
                        throw ArgError(BAD_EXECUTION_MODE, args[i+1]);
                    }
                    hasExecutionMode = true;
                break;
                
                case "-l":
                    if(hasLogLevel)
                        throw ArgError(DUPLICATE_ARGUMENT, "log level");
                    try
                    {
                        logMode = (LogLevel)Enum.Parse(
                                typeof(LogLevel), args[i+1].ToUpper());
                    }
                    catch(Exception) 
                    {
                        throw ArgError(BAD_LOG_LEVEL, args[i+1]);
                    }
                    hasLogLevel = true;
                break;

                default:
                    throw ArgError(BAD_PARAMETER, args[i]);
            }
        }

        if(!hasConfig || !hasSource || !hasOutput)
            throw ArgError(MISSING_ARGS);
    }

    private static void validateConfigArg()
    {
        foreach(string config in configPaths)
        {
            if (!ExtUtil.hasValidConfigFileExtension(config))
                throw ArgError(BAD_CONFIG_EXTENSION, config);
            if (!File.Exists(config))
                throw ArgError(CONFIG_NOT_FOUND, config);
        }

    }

    private static void validateSourceArg()
    {
        if(File.Exists(srcPath))
        {
            if (!ZipUtil.isFileValidZip(srcPath))
                throw ArgError(MOD_NOT_VALID);

            if(FileUtil.isFileLarge(srcPath))
                throw ArgError(MOD_TOO_BIG);

            srcIsZip = true;
        }
        else if (Directory.Exists(srcPath))
        {
            if(DirUtil.isDirectoryLarge(srcPath))
                throw ArgError(MOD_TOO_BIG);
        }
        else
            throw ArgError(MOD_NOT_FOUND);
    }

    private static void validateOutputArg()
    {
        if (File.Exists(outPath))
            throw ArgError(OUTPUT_PREEXISTING_FILE);
        else if (Directory.Exists(outPath))
            if(DirUtil.dirContainsData(outPath))
                throw ArgError(OUTPUT_NONEMPTY_DIRECTORY);

        if (!srcIsZip)
            if(DirUtil.isParentDir(srcPath, outPath))
                throw ArgError(OUTPUT_INSIDE_SRC);

        outToZip = ExtUtil.hasExtension(outPath, ".zip");
    }

    public enum ArgumentError
    {
        BAD_NUMBER_ARGUMENTS,
        BAD_PARAMETER,
        BAD_EXECUTION_MODE,
        BAD_LOG_LEVEL,
        DUPLICATE_ARGUMENT,
        MISSING_ARGS,
        BAD_CONFIG_EXTENSION,
        CONFIG_NOT_FOUND,
        MOD_NOT_FOUND,
        MOD_NOT_VALID,
        MOD_TOO_BIG,
        OUTPUT_PREEXISTING_FILE,
        OUTPUT_NONEMPTY_DIRECTORY,
        OUTPUT_INSIDE_SRC,
    }

    private static EMBException ArgError(ArgumentError e, string arg = "")
    {
        string preamble = "Failed to parse command-line arguments:\n",
               msg = "";
        string[] args = {"", ""};
        switch(e)
        {
            case BAD_NUMBER_ARGUMENTS:
            msg = "Please enter an even number of arguments.\n\n{0}";
            args[0] = RULES_USAGE_MINIMAL;
            break;
            
            case BAD_PARAMETER:
            msg = "'{0}' is not a valid parameter.\n\n{1}";
            args[0] = arg;
            args[1] = RULES_USAGE_MINIMAL;
            break;

            case BAD_EXECUTION_MODE:
            msg = "'{0}' is not a valid Execution Mode.\n\n{1}";
            args[0] = arg;
            args[1] = DESC_EXEMODE;
            break;

            case BAD_LOG_LEVEL:
            msg = "'{0}' is not a valid Log Level.\n\n{1}";
            args[0] = arg;
            args[1] = DESC_LOGLEVEL;
            break;

            case DUPLICATE_ARGUMENT:
            msg = "You may only input one {0}.\n\n{1}";
            args[0] = arg;
            args[1] = RULES_USAGE_MINIMAL;
            break;

            case MISSING_ARGS:
            msg = "Missing required command line argument(s).\n\n{0}";
            args[0] = RULES_USAGE_MINIMAL;
            break;

            case BAD_CONFIG_EXTENSION:
            msg = "The configuration file '{0}' must be a {1} file.";
            args[0] = arg;
            args[1] = DESC_CFG_EXTENSIONS;
            break;

            case CONFIG_NOT_FOUND:
            msg = "Failed to find the configuration file '{0}'";
            args[0] = arg;
            break;

            case MOD_NOT_FOUND:
            msg = "The mod directory or .zip file does not exist.";
            break;

            case MOD_NOT_VALID:
            msg = "The mod file is not a valid directory or .zip file.";
            break;

            case MOD_TOO_BIG:
            msg = "Your mod may not be larger than ~{0} gigabytes.";
            args[0] = (MAX_INPUT_SIZE_BYTES / 1000000000.0).ToString();
            break;

            case OUTPUT_PREEXISTING_FILE:
            msg = "A file already exists at the output path.\n\n{0}";
            args[0] = RULES_OUTPUT;
            break;

            case OUTPUT_NONEMPTY_DIRECTORY:
            msg = "A non-empty folder exists at the output path.\n\n{0}";
            args[0] = RULES_OUTPUT;
            break;

            case OUTPUT_INSIDE_SRC:
            msg = "Your output path cannot be inside your mod folder.\n\n{0}";
            args[0] = RULES_OUTPUT;
            break;
        }
        return EMBException.buildException(preamble + msg, args); 
    }
}