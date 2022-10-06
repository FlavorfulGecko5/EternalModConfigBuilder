global using static Constants;
using System.Diagnostics;
class EternalModBuilder
{
    public static List<string> configPaths {get; private set;} = new List<string>();
    public static string srcPath          {get; private set;} = "";
    public static string outPath          {get; private set;} = "";
    public static bool   srcIsZip         {get; private set;} = false;
    public static bool   outToZip         {get; private set;} = false;
    public static bool   compressEntities {get; private set;} = true;

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
            Console.WriteLine(MSG_ERROR + e.Message + MSG_FAILURE);
            Environment.Exit(1);
        }
        catch (Exception e)
        {
            Console.WriteLine(MSG_ERROR_UNKNOWN + e.ToString() + MSG_FAILURE);
            Environment.Exit(2);
        }
    }

    private static void run(string[] args)
    {
        if (Directory.Exists(DIR_TEMP))
            Directory.Delete(DIR_TEMP, true);

        Console.WriteLine(MSG_WELCOME);
        Stopwatch embTimer = new Stopwatch();
        embTimer.Start();

        initialize(args);
        ModBuilder builder = new ModBuilder();
        builder.buildMod();
        
        embTimer.Stop();
        Console.WriteLine(MSG_SUCCESS, embTimer.ElapsedMilliseconds / 1000.0);
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
            + "\n - Log Level: " + logMode.ToString()
            + "\n - Compress Entities: " + compressEntities;
        
        return msg;
    }

    private static void readToVariables(string[] args)
    {
        // Must-have
        bool hasConfig = false, hasSource = false, hasOutput = false;

        // Optional
        bool hasCompEnts = false, hasExecutionMode = false, hasLogLevel = false;

        if (args.Length % 2 != 0)
            throw ArgError("Please enter an even number of arguments.\n\n{0}", 
                RULES_USAGE_MINIMAL);

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
                        throw ArgError(
                            "Compress-entities needs a true/false value.\n\n{0}",
                            DESC_COMP_ENTITIES);
                    }
                    hasCompEnts = true;
                break;
                
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
                        throw ArgError(
                            "'{0}' is not a valid Execution Mode.\n\n{1}",
                            args[i+1], DESC_EXEMODE);
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
                        throw ArgError("'{0}' is not a valid Log Level.\n\n{1}",
                            args[i+1], DESC_LOGLEVEL);
                    }
                    hasLogLevel = true;
                break;

                default:
                    throw ArgError("'{0}' is not a valid parameter.\n\n{1}",
                        args[i], RULES_USAGE_MINIMAL);
            }
            EMBArgumentException duplicateArg(string type)
            {
                return ArgError( "You may only input one {0}.\n\n{1}",
                    type, RULES_USAGE_MINIMAL);
            }
        }

        if(!hasConfig || !hasSource || !hasOutput)
            throw ArgError("Missing required command line argument(s).\n\n{0}",
                RULES_USAGE_MINIMAL);
    }

    private static void validateConfigArg()
    {
        foreach(string config in configPaths)
        {
            if(!config.EndsWithCCIC(".json") && !config.EndsWithCCIC(".txt"))
                throw ArgError(
                    "The configuration file '{0}' must be a {1} file.",
                    config, DESC_CFG_EXTENSIONS);

            if (!File.Exists(config))
                throw ArgError("Failed to find the configuration file '{0}'",
                    config);
        }

    }

    private static void validateSourceArg()
    {
        if(File.Exists(srcPath))
        {
            if (!ZipUtil.isFileValidZip(srcPath))
                throw ArgError(
                    "The mod file is not a valid directory or .zip file.");

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
            throw ArgError("The mod directory or .zip file does not exist.");
        
        EMBArgumentException modTooLarge()
        {
            return ArgError("Your mod may not be larger than ~{0} gigabytes.",
                (MAX_INPUT_SIZE_BYTES / 1000000000.0).ToString());
        }
    }

    private static void validateOutputArg()
    {
        if (File.Exists(outPath))
            throw ArgError("A file already exists at the output path.\n\n{0}",
                RULES_OUTPUT);
        else if (Directory.Exists(outPath))
            if(FSUtil.dirContainsData(outPath))
                throw ArgError(
                    "A non-empty folder exists at the output path.\n\n{0}",
                    RULES_OUTPUT);

        if (!srcIsZip)
            if(FSUtil.isParentDir(srcPath, outPath))
                throw ArgError(
                    "Your output path cannot be inside your mod folder.\n\n{0}",
                    RULES_OUTPUT);
        outToZip = outPath.EndsWithCCIC(".zip");
    }

    private static EMBArgumentException ArgError(string msg, string arg0="", string arg1 = "")
    {
        string formattedMessage = "Failed to parse command-line arguments:\n"
            + String.Format(msg, arg0, arg1);
        return new EMBArgumentException(formattedMessage);
    }

    public class EMBArgumentException : EMBException
    {
        public EMBArgumentException(string msg) : base (msg) {}
    }
}