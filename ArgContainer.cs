using System.IO.Compression;
using System.Collections.ObjectModel;
class ArgContainer
{
    public string configPath = "", srcPath = "", outPath = "";
    public bool srcIsZip = false, outToZip = false;

    public ArgContainer(string[] args)
    {
        readToVariables(args);
        validateConfigArg();
        validateSourceArg();
        validateOutputArg();
    }

    private void readToVariables(string[] args)
    {
        bool hasConfig = false, hasSource = false, hasOutput = false;

        if (args.Length != EXPECTED_ARG_COUNT)
            ProcessErrorCode(BAD_NUMBER_ARGUMENTS, args.Length.ToString());

        for (int i = 0; i < args.Length; i += 2)
        {
            switch (args[i].ToLower())
            {
                case "-c":
                    if (!hasConfig)
                    {
                        configPath = args[i + 1];
                        hasConfig = true;
                    }
                    else
                        goto CATCH_INVALID_ARGUMENT;
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

                default:
                CATCH_INVALID_ARGUMENT:
                    ProcessErrorCode(BAD_ARGUMENT, (i + 1).ToString());
                    break;
            }
        }
    }

    private void validateConfigArg()
    {
        // Validate extension and existance
        if (!hasExtension(configPath, CONFIG_FILE_EXTENSION))
            ProcessErrorCode(BAD_CONFIG_EXTENSION, configPath);
        if (!File.Exists(configPath))
            ProcessErrorCode(CONFIG_NOT_FOUND, configPath);
    }

    private void validateSourceArg()
    {
        if(File.Exists(srcPath))
        {
            if(!hasExtension(srcPath, ".zip"))
                ProcessErrorCode(MOD_NOT_VALID, srcPath);

            // Check if it's an actual, valid zipfile.
            try
            {
                using(ZipArchive z = ZipFile.OpenRead(srcPath))
                {
                    ReadOnlyCollection<ZipArchiveEntry> entries = z.Entries;
                    srcIsZip = true;
                }
            }
            catch (InvalidDataException) // Not a valid zip
            { 
                ProcessErrorCode(MOD_NOT_VALID, srcPath); 
            }
        }
        else if (!Directory.Exists(srcPath))
            ProcessErrorCode(MOD_NOT_FOUND, srcPath);
    }

    private void validateOutputArg()
    {
        if (File.Exists(outPath))
            ProcessErrorCode(OUTPUT_PREEXISTING_FILE, outPath);
        else if (Directory.Exists(outPath))
        {
            if (Directory.EnumerateFileSystemEntries(outPath).Any())
                ProcessErrorCode(OUTPUT_NONEMPTY_DIRECTORY, outPath);
        }
        outToZip = hasExtension(outPath, ".zip");
    }
}