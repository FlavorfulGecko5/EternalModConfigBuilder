global using static Constants;
using System.Diagnostics;
class EternalModBuilder
{
    public static ArgData      runParms   {get; private set;} = new ArgData();
    public static ParsedConfig configData {get; private set;} = new ParsedConfig();
    private static string startDir = "", activeDir = "";

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

    public static bool mustLog(LogLevel targetLevel)
    {
        return runParms.logMode == targetLevel || runParms.logMode == LogLevel.VERBOSE;
    }

    public static void log(string msg)
    {
        Console.WriteLine(MSG_LOG + msg);
    }

    public static void reportWarning(string msg)
    {
        Console.WriteLine(MSG_WARNING + msg);
    }

    private static void run(string[] args)
    {
        if (Directory.Exists(DIR_TEMP))
            Directory.Delete(DIR_TEMP, true);

        Console.WriteLine(MSG_WELCOME);
        Stopwatch embTimer = new Stopwatch();
        embTimer.Start();
        buildMod(args);
        embTimer.Stop();
        Console.WriteLine(MSG_SUCCESS, embTimer.ElapsedMilliseconds / 1000.0);
    }

    private static void buildMod(string[] args)
    {
        runParms = new ArgData(args);
        configData = new ParsedConfig(runParms.configPaths);
        
        if(runParms.exeMode == ExecutionMode.READONLY)
            return;

        startDir = Directory.GetCurrentDirectory();
        createActiveOutputDir();
        Directory.SetCurrentDirectory(activeDir);
        switch(runParms.exeMode)
        {
            case ExecutionMode.COMPLETE:
            parseAndCompressFiles();
            propagateAll();
            break;

            case ExecutionMode.PARSE:
            parseAndCompressFiles();
            break;

            case ExecutionMode.PROPAGATE:
            propagateAll();
            break;
        }
        Directory.SetCurrentDirectory(startDir);
        finishBuilding();
    }

    private static void createActiveOutputDir()
    {
        // If zip, use temp. directory then zip to output after processing
        activeDir = runParms.outToZip ? DIR_TEMP : runParms.outPath;

        // Clone the contents of src to the active output directory
        Directory.CreateDirectory(activeDir);
        if (runParms.srcIsZip)
            ZipUtil.unzip(runParms.srcPath, activeDir);
        else
            FSUtil.copyDirectory(runParms.srcPath, activeDir);
    }

    private static void parseAndCompressFiles()
    {
        ExpressionHandler.setOptionList(configData.options);
        FileParser parser = new FileParser();
        List<string> labelFiles = new List<string>();
        List<string> uncompressedEntities = new List<string>();

        string[] allFiles = FSUtil.getAllFilesInCurrentDir();
        foreach(string file in allFiles)
            if(file.EndsWithCCIC(".decl") || file.EndsWithCCIC(".json"))
                labelFiles.Add(file);
            else if(file.EndsWithCCIC(".entities"))
                if(!EntityCompressor.isEntityFileCompressed(file))
                {
                    labelFiles.Add(file);
                    uncompressedEntities.Add(file);
                }
                    
        foreach(string file in labelFiles)
            parser.parseFile(file);
        
        if(runParms.compressEntities && EntityCompressor.canCompress)
            foreach(string entityFile in uncompressedEntities)
                EntityCompressor.compressAndWrite(entityFile);
    }

    private static void propagateAll()
    {
        const string 
        WARNING_NO_LISTS = "The '" + DIR_PROPAGATE + "' folder"
        + " exists in your mod, but no propagation lists are defined."
        + " Propagation will not occur.",

        WARNING_NO_DIR = "You have propagation lists, but no '"
        + DIR_PROPAGATE + "' folder in your mod. Propagation will not occur.";

        if (Directory.Exists(DIR_PROPAGATE))
        {
            if (configData.propagations.Count == 0)
                reportWarning(WARNING_NO_LISTS);

            foreach (PropagateList resource in configData.propagations)
                resource.propagate();

            Directory.Delete(DIR_PROPAGATE, true);
        }
        else if (configData.propagations.Count > 0)
            reportWarning(WARNING_NO_DIR);
    }
    
    private static void finishBuilding()
    {
        if(runParms.outToZip)
        {
            ZipUtil.makeZip(DIR_TEMP, runParms.outPath);
            Directory.Delete(DIR_TEMP, true);
        }
    }
}