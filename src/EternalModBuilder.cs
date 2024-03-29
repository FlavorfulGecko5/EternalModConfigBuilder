using System.Diagnostics;
using System.Text;
/// <summary>
/// Structures and controls the execution of all mod building operations
/// </summary>
class EternalModBuilder
{
    /* Program Configuration Constants */

    /// <summary>
    /// The version identifier for this build of EternalModBuilder
    /// </summary>
    const string EXE_VERSION = "2.0.1";

    /// <summary>
    /// The name of the directory EternalModBuilder recognizes as temporary
    /// and safe to delete before/after mod building occurs.
    /// </summary>
    public const string DIR_TEMP = "eternalmodbuilder_temp";

    /// <summary>
    /// If verbose logging is enabled, log data will be written to this file.
    /// </summary>
    public const string FILE_LOG = "eternalmodbuilder_temp_log.txt";

    /// <summary>
    /// The name of the directory inside the mod that 
    /// EternalModBuilder propagates files from
    /// </summary>
    public const string DIR_PROPAGATE = "propagate";

    /// <summary>
    /// Describes all types of mod files that will be scanned for labels
    /// </summary>
    const string DESC_LABEL_FILES = ".decl, .json and decompressed .entities";





    /* Output Messages */

    /// <summary>
    /// Initial display message showing the program name and version
    /// </summary>
    const string MSG_WELCOME = "\nEternalModBuilder " + EXE_VERSION + " by FlavorfulGecko5";

    /// <summary>
    /// Output text prefacing any error message
    /// </summary>
    const string MSG_ERROR = "ERROR: ";

    /// <summary>
    /// Output text prefacing any unknown error message
    /// </summary>
    const string MSG_ERROR_UNKNOWN = MSG_ERROR + "An unknown error occurred:\n\n";

    /// <summary>
    /// Final output message when mod building fails
    /// </summary>
    const string MSG_FAILURE = "\n\nMod building halted due to the above error.\n";

    /// <summary>
    /// Final output message when mod building succeeds
    /// </summary>
    const string MSG_SUCCESS = "\nMod successfully built in {0} seconds.\n\n"
    + "Please Note:\n"
    + "- Only " + DESC_LABEL_FILES + " files are checked for labels.\n"
    + "- This program can't detect every conceivable typo you might make.\n"
    + "If your game crashes, double-check your mod files for errors.\n";       
    



    /* Static Fields and Methods */

    /// <summary>
    /// Parsed command-line argument data
    /// </summary>
    public static ArgData      runParms   {get; private set;} = new ArgData();

    /// <summary>
    /// Propagation Data
    /// </summary>
    public static List<PropagateList> propagations {get; private set;} = new List<PropagateList>();

    /// <summary>
    /// Data to write to the log file
    /// </summary>
    public static StringBuilder logData = new StringBuilder();

    /// <summary>
    /// The directory at program start.
    /// </summary>
    private static string startDir = "";

    /// <summary>
    /// The directory that mod files will be copied to and built inside of.
    /// </summary>
    private static string activeDir = "";

    /// <summary>
    /// Outputs a warning message
    /// </summary>
    /// <param name="msg">The warning message.</param>
    public static void reportWarning(string msg)
    {
        Console.WriteLine("WARNING: " + msg);
    }

    /// <summary>
    /// Program main entry point.
    /// Controls the output of error messages and resulting program termination.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>
    /// 0 if the program executes with no terminal errors.<para/>
    /// 1 if the program terminated because of a known error.<para/>
    /// 2 if the program terminated because of an unknown error.
    /// </returns>
    public static int Main(string[] args)
    {
        int exitcode = 0;
        try
        {
            Console.WriteLine(MSG_WELCOME);
            if (args.Length == 0)
            {
                Console.WriteLine(ArgData.RULES_USAGE_VERBOSE);
                return 0;
            }

            // Delete temporary files
            if(Directory.Exists(DIR_TEMP))
                Directory.Delete(DIR_TEMP, true);
            if(File.Exists(FILE_LOG))
                File.Delete(FILE_LOG);

            // Build the mod and time how long it takes
            Stopwatch embTimer = new Stopwatch();
            embTimer.Start();
            buildMod(args);
            embTimer.Stop();

            // Report successful execution and build time
            Console.WriteLine(MSG_SUCCESS, embTimer.ElapsedMilliseconds / 1000.0);
            exitcode = 0;
        }
        catch (EMBException e)
        {
            Console.WriteLine(MSG_ERROR + e.Message + MSG_FAILURE);
            exitcode = 1;
        }
        catch (Exception e)
        {
            Console.WriteLine(MSG_ERROR_UNKNOWN + e.ToString() + MSG_FAILURE);
            exitcode = 2;
        }
        if(runParms.logfile)
            File.WriteAllText(Path.Combine(startDir, FILE_LOG), logData.ToString());
        return exitcode;
    }

    /// <summary>
    /// Controls the execution of all mod building operations.
    /// </summary>
    /// <param name="args">Command-line arguments</param>
    private static void buildMod(string[] args)
    {
        // Parse argument and config data needed for the build process
        startDir = Directory.GetCurrentDirectory();
        runParms = new ArgData(args);
        if(runParms.logfile)
            logData.Append(runParms.ToString() + "\n\n");
        propagations = ConfigBuilder.buildConfig(runParms.configPaths);

        if(runParms.exeMode == ExecutionMode.READONLY)
            return;

        // If zip, use temp. directory then zip to output after processing
        activeDir = runParms.outToZip ? DIR_TEMP : runParms.outPath;

        // Clone the contents of src to the active output directory
        Directory.CreateDirectory(activeDir);
        if (runParms.srcIsZip)
            ZipUtil.unzip(runParms.srcPath, activeDir);
        else
            FSUtil.copyDirectory(runParms.srcPath, activeDir);
        
        // Execute build operations
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

        // Create zip file if necessary
        if (runParms.outToZip)
        {
            ZipUtil.makeZip(DIR_TEMP, runParms.outPath);
            Directory.Delete(DIR_TEMP, true);
        }
    }

    /// <summary>
    /// Controls the execution of file parsing and entity compression operations
    /// </summary>
    private static void parseAndCompressFiles()
    {
        List<string> labelFiles = new List<string>();
        List<string> uncompressedEntities = new List<string>();

        // Compile the lists of mod files to parse for labels
        // and uncompressed .entities files
        string[] allFiles = FSUtil.getAllFilesInCurrentDir();
        foreach(string file in allFiles)
            if(file.EndsWithOIC(".decl") || file.EndsWithOIC(".json"))
                labelFiles.Add(file);
            else if(file.EndsWithOIC(".entities"))
                if(!EntityCompressor.isEntityFileCompressed(file))
                {
                    labelFiles.Add(file);
                    uncompressedEntities.Add(file);
                }

        // Parse all label files
        multiThread(true);

        // Compress all uncompressed .entities files if configured to do so
        if(runParms.compressEntities && EntityCompressor.canCompress)
            multiThread(false);

        void multiThread(bool trueParseFalseCompress)
        {
            Object logLock = new Object();
            List<string> list = trueParseFalseCompress ? labelFiles : uncompressedEntities;

            Task[] pool = new Task[runParms.threadCount - 1];
            int filesAssigned = 0;

            // Initialize pools
            int div = list.Count / runParms.threadCount,
                rem = list.Count % runParms.threadCount;
                
            for(int i = 0; i < pool.Length; i++)
            {
                int poolCount = 0;
                if(i < rem)
                    poolCount = 1;
                poolCount += div;

                int localFiles = filesAssigned; // Prevents multithreading parameter errors
                if(trueParseFalseCompress)
                    pool[i] = Task.Factory.StartNew(() => parseTask(localFiles, localFiles + poolCount));
                else
                    pool[i] = Task.Factory.StartNew(() => compressTask(localFiles, localFiles + poolCount));
                filesAssigned += poolCount;
            }

            if(trueParseFalseCompress)
                parseTask(filesAssigned, labelFiles.Count);
            else
                compressTask(filesAssigned, uncompressedEntities.Count);
            
            try
            {
                Task.WaitAll(pool);
            }
            catch(System.AggregateException e)
            {
                // Report the first Exception logged, any others will be
                // identified with consecutive runs of the program
                throw e.InnerExceptions[0];
            }
            
            void parseTask(int start, int end)
            {
                FileParser p = new FileParser();
                for(int i = start; i < end; i++)
                    p.parseFile(labelFiles[i]);
                
                if (runParms.logfile)
                    lock(logLock)
                        logData.Append(p.log);
            }
            void compressTask(int start, int end)
            {
                for (int i = start; i < end; i++)
                    EntityCompressor.compressAndWrite(uncompressedEntities[i]);
            }
        }
    }

    /// <summary>
    /// Controls the execution of file propagation operations
    /// </summary>
    private static void propagateAll()
    {
        const string // Warning messages
        WARNING_NO_LISTS = "The '" + DIR_PROPAGATE + "' folder"
        + " exists in your mod, but no propagation lists are defined."
        + " Propagation will not occur.",

        WARNING_NO_DIR = "You have propagation lists, but no '"
        + DIR_PROPAGATE + "' folder in your mod. Propagation will not occur.";

        // Check for existence of the propagation folder and propagation lists
        // If one exists but not the other, warn the user.
        if (Directory.Exists(DIR_PROPAGATE))
        {
            if (propagations.Count == 0)
                reportWarning(WARNING_NO_LISTS);
            
            // Perform propagation and delete the propagation directory after
            foreach (PropagateList resource in propagations)
                resource.propagate();
            Directory.Delete(DIR_PROPAGATE, true);
        }
        else if (propagations.Count > 0)
            reportWarning(WARNING_NO_DIR);
    }
}