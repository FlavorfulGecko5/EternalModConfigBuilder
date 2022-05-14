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
                formattedString += "Invalid number of command line arguments. (Expected " + EXPECTED_ARG_COUNT + ", received " + args[0] + ")\n" + EXPECTED_USAGE;
                break;
            case BAD_ARGUMENT:
                formattedString += "Command line argument #" + args[0] + " is invalid.\n" + EXPECTED_USAGE;
                break;
            // Config. file parsing errors
            case CONFIG_NOT_TXT:
                formattedString += "The configuration file " + args[0] + " is not recognized as a .txt file.";
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
            case BAD_LABEL_FORMATTING:
                formattedString += "The Label " + args[0] + " is not formatted correctly in the configuration file.\n" + RULES_LABEL_FORMATTING;
                break;
            case OPTION_ISNT_OBJECT:
                formattedString += "The Option " + args[0] + " is not defined as a Json object in the configuration file.";
                break;
            case BAD_VARIABLE_VALUE:
                formattedString += "The Variable " + args[0] + " has it's required '" + PROPERTY_NAME_VALUE 
                    + "' property incorrectly defined, or missing entirely in the configuration file.\n" + RULES_VARIABLE_VALUE;
                break;
            case BAD_TOGGLEABLE_VALUE:
                formattedString += "The Toggleable " + args[0] + " has it's required '" + PROPERTY_NAME_VALUE 
                    + "' property incorrectly defined, or missing entirely in the configuration file.\n" + RULES_TOGGLEABLE_VALUE;
                break;
            case LOCATIONS_ISNT_STRING_ARRAY:
                formattedString += "The Option " + args[0] + " has it's '" + PROPERTY_NAME_LOCATIONS 
                    + "' property incorrectly defined in the configuration file.\n" + RULES_PROPERTY_LOCATIONS;
                break;
            case UNSUPPORTED_FILETYPE:
                formattedString += "The Option " + args[0] + " has an invalid or unsupported file '" + args[1]
                    + "' in it's '" + PROPERTY_NAME_LOCATIONS + "' list.\n" + SUPPORTED_FILETYPES_DESCRIPTION;
                break;
            case DUPLICATE_LABEL:
                formattedString += "The label " + args[0] + " is used multiple times in the configuration file."
                    + " A label may only be used to define one Option.";
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
    CONFIG_NOT_TXT,
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
    // Mod Building Errors
    MOD_DIRECTORY_NOT_FOUND,
    MOD_FILE_NOT_FOUND,
    UNRECOGNIZED_LABEL,
    INCOMPLETE_LABEL,
    EXTRA_END_TOGGLE,
    MISSING_END_TOGGLE
}