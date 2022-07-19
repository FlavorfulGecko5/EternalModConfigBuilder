using static ArgContainer.Error;
class ArgContainer
{
    public List<string> configPaths = new List<string>();
    public string srcPath = "", outPath = "";
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

        if (args.Length % 2 != 0)
            throw EMBError(BAD_NUMBER_ARGUMENTS);

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

                default:
                CATCH_INVALID_ARGUMENT:
                    throw EMBError(BAD_ARGUMENT, i + 1);
            }
        }

        if(!hasConfig || !hasSource || !hasOutput)
            throw EMBError(MISSING_ARGS);
    }

    private void validateConfigArg()
    {
        for(int i = 0; i < configPaths.Count; i++)
        {
            if (!ExtUtil.hasValidConfigFileExtension(configPaths[i]))
                throw EMBError(BAD_CONFIG_EXTENSION, i);
            if (!File.Exists(configPaths[i]))
                throw EMBError(CONFIG_NOT_FOUND, i);
        }

    }

    private void validateSourceArg()
    {
        if(File.Exists(srcPath))
        {
            if (!ZipUtil.isFileValidZip(srcPath))
                throw EMBError(MOD_NOT_VALID);

            if(FileUtil.isFileLarge(srcPath))
                throw EMBError(MOD_TOO_BIG);

            srcIsZip = true;
        }
        else if (Directory.Exists(srcPath))
        {
            if(DirUtil.isDirectoryLarge(srcPath))
                throw EMBError(MOD_TOO_BIG);
        }
        else
            throw EMBError(MOD_NOT_FOUND);
    }

    private void validateOutputArg()
    {
        if (File.Exists(outPath))
            throw EMBError(OUTPUT_PREEXISTING_FILE);
        else if (Directory.Exists(outPath))
            if(DirUtil.dirContainsData(outPath))
                throw EMBError(OUTPUT_NONEMPTY_DIRECTORY);

        if (!srcIsZip)
            if(DirUtil.isParentDir(srcPath, outPath))
                throw EMBError(OUTPUT_INSIDE_SRC);

        outToZip = ExtUtil.hasExtension(outPath, ".zip");
    }

    public enum Error
    {
        BAD_NUMBER_ARGUMENTS,
        BAD_ARGUMENT,
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

    private EMBException EMBError(Error e, int arg0 = -1)
    {
        string preamble = "Failed to parse command-line arguments:\n",
               msg = "";
        string[] args = {"", ""};
        switch(e)
        {
            case BAD_NUMBER_ARGUMENTS:
            msg = "Bad number of arguments. (Expected an even number)\n\n{0}";
            args[0] = RULES_USAGE;
            break;
            
            case BAD_ARGUMENT:
            msg = "Command line argument #{0} is invalid.\n\n{1}";
            args[0] = arg0.ToString();
            args[1] = RULES_USAGE;
            break;

            case MISSING_ARGS:
            msg = "Missing required command line argument(s).\n\n{0}";
            args[0] = RULES_USAGE;
            break;

            case BAD_CONFIG_EXTENSION:
            msg = "The configuration file '{0}' must be a {1} file.";
            args[0] = configPaths[arg0];
            args[1] = DESC_CFG_EXTENSIONS;
            break;

            case CONFIG_NOT_FOUND:
            msg = "Failed to find the configuration file '{0}'";
            args[0] = configPaths[arg0];
            break;

            case MOD_NOT_FOUND:
            msg = "The mod directory or .zip file '{0}' does not exist.";
            args[0] = srcPath;
            break;

            case MOD_NOT_VALID:
            msg = "The mod '{0}' is not a valid directory or .zip file.";
            args[0] = srcPath;
            break;

            case MOD_TOO_BIG:
            msg = "Your mod may not be larger than ~{0} gigabytes.";
            args[0] = (MAX_INPUT_SIZE_BYTES / 1000000000.0).ToString();
            break;

            case OUTPUT_PREEXISTING_FILE:
            msg = "A file exists at the output path '{0}'\n\n{1}";
            args[0] = outPath;
            args[1] = RULES_OUTPUT;
            break;

            case OUTPUT_NONEMPTY_DIRECTORY:
            msg = "A non-empty folder exists at the output path '{0}'\n\n{1}";
            args[0] = outPath;
            args[1] = RULES_OUTPUT;
            break;

            case OUTPUT_INSIDE_SRC:
            msg = "Your output path cannot be inside your mod folder.\n\n{0}";
            args[0] = RULES_OUTPUT;
            break;
        }
        return EMBException.buildException(preamble + msg, args); 
    }
}