class ErrorReporter
{
    public static void ProcessErrorCode(ErrorCode error, string[] args)
    {
        string formattedString = "ERROR: ";
        switch (error)
        {
            case ErrorCode.UNKNOWN_ERROR:
                formattedString += "An unknown error occurred, printing Exception:\n" + args[0];
                break;
            case ErrorCode.DIRECTORY_NOT_FOUND:
                formattedString += "The mod config. is in a non-existant directory: " + args[0];
                break;
            case ErrorCode.CONFIG_NOT_FOUND:
                formattedString += "Failed to find mod config. file " + args[0];
                break;
            case ErrorCode.BAD_JSON_FILE:
                formattedString += "The config. file has a syntax error, printing Exception message:\n" + args[0];
                break;
            case ErrorCode.BAD_LABEL_FORMATTING:
                formattedString += "The label " + args[0] + " is not formatted correctly.\n" + Constants.LABEL_FORMATTING_RULES;
                break;
        }
        terminateWithError(formattedString);
    }

    private static void terminateWithError(string outputString)
    {
        System.Console.WriteLine(outputString);
        Environment.Exit(1);
    }
}

enum ErrorCode
{
    UNKNOWN_ERROR,
    DIRECTORY_NOT_FOUND,
    CONFIG_NOT_FOUND,
    BAD_JSON_FILE,
    BAD_LABEL_FORMATTING
}

