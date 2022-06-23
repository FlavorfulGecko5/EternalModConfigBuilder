using static ArgContainer.Error;
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
            ThrowError(BAD_NUMBER_ARGUMENTS, args.Length.ToString());

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
                    ThrowError(BAD_ARGUMENT, (i + 1).ToString());
                    break;
            }
        }
    }

    private void validateConfigArg()
    {
        if (!ExtUtil.hasValidConfigFileExtension(configPath))
            ThrowError(BAD_CONFIG_EXTENSION);
        if (!File.Exists(configPath))
            ThrowError(CONFIG_NOT_FOUND);
    }

    private void validateSourceArg()
    {
        if(File.Exists(srcPath))
        {
            if (!ZipUtil.isFileValidZip(srcPath))
                ThrowError(MOD_NOT_VALID);

            if(FileUtil.isFileLarge(srcPath))
                ThrowError(MOD_TOO_BIG);

            srcIsZip = true;
        }
        else if (Directory.Exists(srcPath))
        {
            if(DirUtil.isDirectoryLarge(srcPath))
                ThrowError(MOD_TOO_BIG);
        }
        else
            ThrowError(MOD_NOT_FOUND);
    }

    private void validateOutputArg()
    {
        if (File.Exists(outPath))
            ThrowError(OUTPUT_PREEXISTING_FILE);
        else if (Directory.Exists(outPath))
            if(DirUtil.dirContainsData(outPath))
                ThrowError(OUTPUT_NONEMPTY_DIRECTORY);

        if (!srcIsZip)
            if(DirUtil.isParentDir(srcPath, outPath))
                ThrowError(OUTPUT_INSIDE_SRC);

        outToZip = ExtUtil.hasExtension(outPath, ".zip");
    }

    public enum Error
    {
        BAD_NUMBER_ARGUMENTS,
        BAD_ARGUMENT,
        BAD_CONFIG_EXTENSION,
        CONFIG_NOT_FOUND,
        MOD_NOT_FOUND,
        MOD_NOT_VALID,
        MOD_TOO_BIG,
        OUTPUT_PREEXISTING_FILE,
        OUTPUT_NONEMPTY_DIRECTORY,
        OUTPUT_INSIDE_SRC,
    }

    private void ThrowError(Error error, string arg0 = "")
    {
        switch(error)
        {
            case BAD_NUMBER_ARGUMENTS:
            reportError(String.Format
            (
                "Bad number of arguments. (Expected {0}, received {1})\n\n{2}",
                EXPECTED_ARG_COUNT,
                arg0, // args.length
                RULES_EXPECTED_USAGE
            ));
            break;
            
            case BAD_ARGUMENT:
            reportError(String.Format(
                "Command line argument #{0} is invalid.\n\n{1}",
                arg0, // The invalid argument number
                RULES_EXPECTED_USAGE
            ));
            break;

            case BAD_CONFIG_EXTENSION:
            reportError(String.Format(
                "The configuration file '{0}' must be a {1} file.",
                configPath,
                DESC_CONFIG_EXTENSIONS
            ));
            break;

            case CONFIG_NOT_FOUND:
            reportError(String.Format(
                "Failed to find the configuration file '{0}'",
                configPath
            ));
            break;

            case MOD_NOT_FOUND:
            reportError(String.Format(
                "The mod directory or .zip file '{0}' does not exist.",
                srcPath
            ));
            break;

            case MOD_NOT_VALID:
            reportError(String.Format(
                "The mod '{0}' is not a valid directory or .zip file.",
                srcPath
            ));
            break;

            case MOD_TOO_BIG:
            reportError(String.Format(
                "Your mod may not be larger than ~{0} gigabytes.",
                MAX_INPUT_SIZE_BYTES / 1000000000.0
            ));
            break;

            case OUTPUT_PREEXISTING_FILE:
            reportError(String.Format(
                "A file exists at the output path '{0}'\n\n{1}",
                outPath,
                RULES_OUTPUT_LOCATION
            ));
            break;

            case OUTPUT_NONEMPTY_DIRECTORY:
            reportError(String.Format(
                "A non-empty folder exists at the output path '{0}'\n\n{1}",
                outPath,
                RULES_OUTPUT_LOCATION
            ));
            break;

            case OUTPUT_INSIDE_SRC:
            reportError(String.Format(
                "Your output path cannot be inside your mod directory.\n\n{0}",
                RULES_OUTPUT_LOCATION
            ));
            break;
        }
    }
}