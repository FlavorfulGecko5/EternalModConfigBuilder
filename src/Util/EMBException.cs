class EMBException : Exception
{
    public EMBException(string message) : base(message)
    {
    }

    public static EMBException buildException(string msg, string[] args)
    {
        string formattedMsg = String.Format(msg, args);
        return new EMBException(formattedMsg);
    }
}

class ArgEMBExceptionFactory
{
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

    public static EMBException ArgError(Error e, string cfg = "", int argIndex = -1)
    {
        string preamble = "Failed to parse command-line arguments:\n",
               msg = "";
        string[] args = {"", ""};
        switch(e)
        {
            case Error.BAD_NUMBER_ARGUMENTS:
            msg = "Bad number of arguments. (Expected an even number)\n\n{0}";
            args[0] = RULES_USAGE;
            break;
            
            case Error.BAD_ARGUMENT:
            msg = "Command line argument #{0} is invalid.\n\n{1}";
            args[0] = (argIndex + 1).ToString();
            args[1] = RULES_USAGE;
            break;

            case Error.MISSING_ARGS:
            msg = "Missing required command line argument(s).\n\n{0}";
            args[0] = RULES_USAGE;
            break;

            case Error.BAD_CONFIG_EXTENSION:
            msg = "The configuration file '{0}' must be a {1} file.";
            args[0] = cfg;
            args[1] = DESC_CFG_EXTENSIONS;
            break;

            case Error.CONFIG_NOT_FOUND:
            msg = "Failed to find the configuration file '{0}'";
            args[0] = cfg;
            break;

            case Error.MOD_NOT_FOUND:
            msg = "The mod directory or .zip file does not exist.";
            break;

            case Error.MOD_NOT_VALID:
            msg = "The mod file is not a valid directory or .zip file.";
            break;

            case Error.MOD_TOO_BIG:
            msg = "Your mod may not be larger than ~{0} gigabytes.";
            args[0] = (MAX_INPUT_SIZE_BYTES / 1000000000.0).ToString();
            break;

            case Error.OUTPUT_PREEXISTING_FILE:
            msg = "A file already exists at the output path.\n\n{0}";
            args[0] = RULES_OUTPUT;
            break;

            case Error.OUTPUT_NONEMPTY_DIRECTORY:
            msg = "A non-empty folder exists at the output path.\n\n{0}";
            args[0] = RULES_OUTPUT;
            break;

            case Error.OUTPUT_INSIDE_SRC:
            msg = "Your output path cannot be inside your mod folder.\n\n{0}";
            args[0] = RULES_OUTPUT;
            break;
        }
        return EMBException.buildException(preamble + msg, args); 
    }
}