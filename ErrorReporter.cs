using static Constants;
using static ErrorCode;
class ErrorReporter
{
    public static void ProcessErrorCode(ErrorCode error, string[] args)
    {
        bool terminateProgram = true;
        string formattedString = "ERROR: ";
        switch (error)
        {
            // Anything not handled below
            case UNKNOWN_ERROR:
                formattedString += "An unknown error occurred, printing Exception:\n" + args[0];
                break;
            // Command-line argument errors
            case BAD_NUMBER_ARGUMENTS:
                formattedString += "Invalid number of command line arguments. (Expected " + EXPECTED_ARG_COUNT + ", received " + args[0] + ")\n" + RULES_EXPECTED_USAGE;
                break;
            case BAD_ARGUMENT:
                formattedString += "Command line argument #" + args[0] + " is invalid.\n" + RULES_EXPECTED_USAGE;
                break;
            // Config. file parsing errors
            case BAD_CONFIG_EXTENSION:
                formattedString += "The configuration file " + args[0] + " must be a " + CONFIG_FILE_EXTENSION + " file.";
                break;
            case CONFIG_DIRECTORY_NOT_FOUND:
                formattedString += "The configuration file is in a non-existant directory: " + args[0];
                break;
            case CONFIG_NOT_FOUND:
                formattedString += "Failed to find the configuration file in the specified directory: " + args[0];
                break;
            case BAD_JSON_FILE:
                formattedString += "The configuration file has a syntax error, printing Exception message:\n" + args[0];
                break;
            case BAD_NAME_FORMATTING:
                formattedString += "The Option name '" + args[0] + "' is invalid.\n" + RULES_OPTION_NAME_CHARACTERS;
                break;
            case DUPLICATE_NAME:
                formattedString += "The name " + args[0] + " is used multiple times in the configuration file."
                    + " A name may only be used to define one Option.";
                break;
            case OPTION_ISNT_OBJECT:
                formattedString += "The Option " + args[0] + " is not defined as a Json object in the configuration file.";
                break;
            case BAD_OPTION_VALUE:
                formattedString += "The Option " + args[0] + " has it's required '" + PROPERTY_VALUE 
                    + "' property incorrectly defined, or missing entirely in the configuration file.\n" + RULES_OPTION_VALUE;
                break;
            case LOCATIONS_ISNT_STRING_ARRAY:
                formattedString += "The Option " + args[0] + " has it's '" + PROPERTY_LOCATIONS 
                    + "' property incorrectly defined in the configuration file.\n" + RULES_PROPERTY_LOCATIONS;
                break;
            case UNSUPPORTED_FILETYPE:
                formattedString += "The Option " + args[0] + " has an invalid or unsupported file '" + args[1]
                    + "' in it's '" + PROPERTY_LOCATIONS + "' list.\n" + RULES_SUPPORTED_FILETYPES;
                break;
            // Mod building errors
            case MOD_DIRECTORY_NOT_FOUND:
                formattedString += "The mod directory " + args[0] + " does not exist.";
                break;
            case MOD_FILE_NOT_FOUND:
                formattedString += "The file " + args[0] + " was specified in the configuration file but does not actually exist in the mod."
                    + " This filepath will be ignored.";
                terminateProgram = false;
                break;
            case UNRECOGNIZED_LABEL:
                formattedString += "The file " + args[0] + " contains an unrecognized label '" + args[1] + "'\n"
                    + "This label may have an invalid format or location, have a typo in the name, or be missing entirely from the configuration file.";
                break;
            case INCOMPLETE_LABEL:
                formattedString += "The file " + args[0] + " has an incomplete label that's missing a '" + LABEL_BORDER_VALUE + "' on it's right side.";
                break;
            case EXTRA_END_TOGGLE:
                formattedString += "The file " + args[0] + " has an '" + SPECIAL_TOGGLEABLE_END
                    + "' label with no accompanying start label.\n" + RULES_TOGGLE_BLOCK;
                break;
            case MISSING_END_TOGGLE:
                formattedString += "The file " + args[0] + " has a toggleable label " + args[1] + " without a '" + SPECIAL_TOGGLEABLE_END
                    + "' label to denote the end of the toggle block.\n" + RULES_TOGGLE_BLOCK;
                break;
        }
        System.Console.WriteLine(formattedString);
        
        if(terminateProgram)
            Environment.Exit(1);
    }
}

enum ErrorCode
{
    // Anything not handled below
    UNKNOWN_ERROR,
    // Command-line argument errors
    BAD_NUMBER_ARGUMENTS,
    BAD_ARGUMENT,
    // Config. file parsing errors
    BAD_CONFIG_EXTENSION,
    CONFIG_DIRECTORY_NOT_FOUND,
    CONFIG_NOT_FOUND,
    BAD_JSON_FILE,
    BAD_NAME_FORMATTING,
    DUPLICATE_NAME,
    OPTION_ISNT_OBJECT,
    BAD_OPTION_VALUE,
    LOCATIONS_ISNT_STRING_ARRAY,
    UNSUPPORTED_FILETYPE,
    // Mod Building Errors
    MOD_DIRECTORY_NOT_FOUND,
    MOD_FILE_NOT_FOUND,
    UNRECOGNIZED_LABEL,
    INCOMPLETE_LABEL,
    EXTRA_END_TOGGLE,
    MISSING_END_TOGGLE
}