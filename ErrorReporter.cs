class ErrorReporter
{
    public static void ProcessErrorCode(ErrorCode error, string[] args)
    {
        bool terminateProgram = true;
        string formattedString = "ERROR: ";
        switch (error)
        {
            case ErrorCode.UNKNOWN_ERROR:
                formattedString += "An unknown error occurred, printing Exception:\n" + args[0];
                break;
            case ErrorCode.CONFIG_DIRECTORY_NOT_FOUND:
                formattedString += "The configuration file is in a non-existant directory: " + args[0];
                break;
            case ErrorCode.CONFIG_NOT_FOUND:
                formattedString += "Failed to find the configuration file in the specified directory: " + args[0];
                break;
            case ErrorCode.BAD_JSON_FILE:
                formattedString += "The configuration file has a syntax error, printing Exception message:\n" + args[0];
                break;
            case ErrorCode.BAD_LABEL_FORMATTING:
                formattedString += "The Label " + args[0] + " is not formatted correctly in the configuration file.\n" + Constants.LABEL_FORMATTING_RULES;
                break;
            case ErrorCode.OPTION_ISNT_OBJECT:
                formattedString += "The Option " + args[0] + " is not defined as a Json object in the configuration file.";
                break;
            case ErrorCode.BAD_VARIABLE_VALUE:
                formattedString += "The Variable " + args[0] + " has it's required '" + Constants.PROPERTY_NAME_VALUE 
                    + "' property incorrectly defined, or missing entirely in the configuration file.\n" + Constants.VARIABLE_VALUE_RULES;
                break;
            case ErrorCode.BAD_TOGGLEABLE_VALUE:
                formattedString += "The Toggleable " + args[0] + " has it's required '" + Constants.PROPERTY_NAME_VALUE 
                    + "' property incorrectly defined, or missing entirely in the configuration file.\n" + Constants.TOGGLEABLE_VALUE_RULES;
                break;
            case ErrorCode.LOCATIONS_ISNT_STRING_ARRAY:
                formattedString += "The Option " + args[0] + " has it's '" + Constants.PROPERTY_NAME_LOCATIONS 
                    + "' property incorrectly defined in the configuration file.\n" + Constants.PROPERTY_LOCATIONS_RULES;
                break;
            case ErrorCode.UNSUPPORTED_FILETYPE:
                formattedString += "The Option " + args[0] + " has an invalid or unsupported file '" + args[1]
                    + "' in it's '" + Constants.PROPERTY_NAME_LOCATIONS + "' list.\n" + Constants.SUPPORTED_FILETYPES_DESCRIPTION;
                break;
            case ErrorCode.DUPLICATE_LABEL:
                formattedString += "The label " + args[0] + " is used multiple times in the configuration file."
                    + " A label may only be used to define one Option.";
                break;
            case ErrorCode.MOD_DIRECTORY_NOT_FOUND:
                formattedString += "The mod directory " + args[0] + " does not exist.";
                break;
            case ErrorCode.MOD_FILE_NOT_FOUND:
                formattedString += "The file " + args[0] + " was specified in the configuration file but does not actually exist in the mod."
                    + " This filepath will be ignored.";
                terminateProgram = false;
                break;
            case ErrorCode.UNRECOGNIZED_LABEL:
                formattedString += "The file " + args[0] + " contains an unrecognized label '" + args[1] + "'\n"
                    + "This label may have an invalid format or location, have a typo in the name, or be missing entirely from the configuration file.";
                break;
            case ErrorCode.INCOMPLETE_LABEL:
                formattedString += "The file " + args[0] + " has an incomplete label that's missing a '" + Constants.LABEL_BORDER_VALUE + "' on it's right side.";
                break;
            case ErrorCode.EXTRA_END_TOGGLE:
                formattedString += "The file " + args[0] + " has an '"
                    + Constants.LABEL_BORDER_VALUE + Constants.LABEL_TYPE_PREFACE + Constants.TYPE_TOGGLEABLE_END + Constants.LABEL_BORDER_VALUE 
                    + "' label with no accompanying start label.";
                break;
            case ErrorCode.MISSING_END_TOGGLE:
                formattedString += "The file " + args[0] + " has a toggleable label " + args[1] + " without a '"
                    + Constants.LABEL_BORDER_VALUE + Constants.LABEL_TYPE_PREFACE + Constants.TYPE_TOGGLEABLE_END + Constants.LABEL_BORDER_VALUE 
                    + "' label to denote the end of the toggle block.";
                break;
            case ErrorCode.CONFIG_NOT_JSON:
                formattedString += "The configuration file " + args[0] + " is not recognized as a .json file.";
                break;
            // Bad command line argument error codes
            case ErrorCode.BAD_NUMBER_ARGUMENTS:
                formattedString += "Invalid number of command line arguments. (Expected " + args[0] + ", received " + args[1] + ")\n" + Constants.EXPECTED_USAGE;
                break;
            case ErrorCode.BAD_ARGUMENT:
                formattedString += "Command line argument #" + args[0] + " is invalid.\n" + Constants.EXPECTED_USAGE;
                break;
        }
        System.Console.WriteLine(formattedString);
        
        if(terminateProgram)
            Environment.Exit(1);
    }
}

enum ErrorCode
{
    UNKNOWN_ERROR,
    CONFIG_DIRECTORY_NOT_FOUND,
    CONFIG_NOT_FOUND,
    BAD_JSON_FILE,
    BAD_LABEL_FORMATTING,
    OPTION_ISNT_OBJECT,
    BAD_VARIABLE_VALUE,
    BAD_TOGGLEABLE_VALUE,
    LOCATIONS_ISNT_STRING_ARRAY,
    UNSUPPORTED_FILETYPE,
    DUPLICATE_LABEL,
    MOD_DIRECTORY_NOT_FOUND,
    MOD_FILE_NOT_FOUND,
    UNRECOGNIZED_LABEL,
    INCOMPLETE_LABEL,
    EXTRA_END_TOGGLE,
    MISSING_END_TOGGLE,
    CONFIG_NOT_JSON,
    // Bad command line argument error codes.
    BAD_NUMBER_ARGUMENTS,
    BAD_ARGUMENT
}