global using static Constants;
using static ArgEMBExceptionFactory.Error;
using static ArgEMBExceptionFactory;
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
            run(args);
        }
        catch (EMBException e)
        {
            reportError(e.Message);
        }
        catch (Exception e)
        {
            reportUnknownError(e);
        }
    }

    private static void run(string[] args)
    {
        Console.WriteLine(MSG_WELCOME);
        if (Directory.Exists(DIR_TEMP))
            Directory.Delete(DIR_TEMP, true);

        initialize(args);
        ModBuilder builder = new ModBuilder();
        builder.buildMod();

        Console.WriteLine(MSG_SUCCESS);
    }

    private static void reportError(string msg)
    {
        Console.WriteLine(MSG_ERROR + msg);
        Console.WriteLine("\n" + MSG_FAILURE);
        Environment.Exit(1);
    }

    private static void reportUnknownError(Exception e)
    {
        reportError(String.Format(
            "An unknown error occurred, printing Exception:\n\n{0}",
            e.ToString()
        ));
    }

    public static void initialize(string[] args)
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
            configList += "      --" + config + "\n";
        string sourceType = srcIsZip ? "Zip File" : "Folder";
        string outputType = outToZip ? "Zip File" : "Folder"; 

        string msg = "Parsed Command-Line Argument Data:"
            + "\n   - Configuration Files:\n" + configList
            + "   - Source Path: " + srcPath
            + "\n   - Source Type: " + sourceType
            + "\n   - Output Path: " + outPath
            + "\n   - Output Type: " + outputType
            + "\n   - Execution Mode: " + exeMode.ToString()
            + "\n   - Log Level: " + logMode.ToString();
        
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
                    if (!hasSource)
                    {
                        srcPath = args[i + 1];
                        hasSource = true;
                    }
                    else
                        goto CATCH_INVALID_ARGUMENT;
                    break;

                case "-o":
                    if (!hasOutput)
                    {
                        outPath = args[i + 1];
                        hasOutput = true;
                    }
                    else
                        goto CATCH_INVALID_ARGUMENT;
                    break;
                
                case "-x":
                    if(!hasExecutionMode)
                    {
                        int index = args[i + 1].toEnumIndex<ExecutionMode>();
                        if(index != -1)
                            exeMode = (ExecutionMode)index;
                        else
                            goto CATCH_INVALID_ARGUMENT;
                        hasExecutionMode = true;
                    }
                    else
                        goto CATCH_INVALID_ARGUMENT;
                    break;
                
                case "-l":
                    if(!hasLogLevel)
                    {
                        int index = args[i + 1].toEnumIndex<LogLevel>();
                        if(index != -1)
                            logMode = (LogLevel)index;
                        else
                            goto CATCH_INVALID_ARGUMENT;
                        hasLogLevel = true;
                    }
                    else
                        goto CATCH_INVALID_ARGUMENT;
                    break;

                default:
                CATCH_INVALID_ARGUMENT:
                    throw ArgError(BAD_ARGUMENT, argIndex: i);
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
}