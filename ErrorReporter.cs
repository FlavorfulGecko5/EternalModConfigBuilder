using static Constants;
using static ErrorCode;
class ErrorReporter
{
    public static void ProcessErrorCode(ErrorCode error, string arg0 = "", string arg1 = "", string arg2 = "", string arg3 = "")
    {
        bool terminateProgram = true;
        string formattedString = "ERROR: ";
        switch (error)
        {
            // Anything not handled below
            case UNKNOWN_ERROR:
                formattedString += "An unknown error occurred, printing Exception:\n" + arg0;
                break;
            // Command-line argument errors
            case BAD_NUMBER_ARGUMENTS:
                formattedString += "Invalid number of command line arguments. (Expected " + EXPECTED_ARG_COUNT + ", received " + arg0 + ")\n" + RULES_EXPECTED_USAGE;
                break;
            case BAD_ARGUMENT:
                formattedString += "Command line argument #" + arg0 + " is invalid.\n" + RULES_EXPECTED_USAGE;
                break;
            // Config. file parsing errors
            case BAD_CONFIG_EXTENSION:
                formattedString += "The configuration file " + arg0 + " must be a " + CONFIG_FILE_EXTENSION + " file.";
                break;
            case CONFIG_DIRECTORY_NOT_FOUND:
                formattedString += "The configuration file is in a non-existant directory: " + arg0;
                break;
            case CONFIG_NOT_FOUND:
                formattedString += "Failed to find the configuration file in the specified directory: " + arg0;
                break;
            case BAD_JSON_FILE:
                formattedString += "The configuration file has a syntax error, printing Exception message:\n" + arg0;
                break;
            case BAD_NAME_FORMATTING:
                formattedString += "The Option name '" + arg0 + "' is invalid.\n" + RULES_OPTION_NAME_CHARACTERS;
                break;
            case DUPLICATE_NAME:
                formattedString += "The name " + arg0 + " is used multiple times in the configuration file."
                    + " A name may only be used to define one Option.";
                break;
            case OPTION_ISNT_OBJECT:
                formattedString += "The Option " + arg0 + " is not defined as a Json object in the configuration file.";
                break;
            case BAD_OPTION_VALUE:
                formattedString += "The Option " + arg0 + " has it's required '" + PROPERTY_VALUE 
                    + "' property incorrectly defined, or missing entirely in the configuration file.\n" + RULES_OPTION_VALUE;
                break;
            case LOCATIONS_ISNT_STRING_ARRAY:
                formattedString += "The Option " + arg0 + " has it's '" + PROPERTY_LOCATIONS 
                    + "' property incorrectly defined in the configuration file.\n" + RULES_PROPERTY_LOCATIONS;
                break;
            case UNSUPPORTED_FILETYPE:
                formattedString += "The Option " + arg0 + " has an invalid or unsupported file '" + arg1
                    + "' in it's '" + PROPERTY_LOCATIONS + "' list. This file will not be checked.\n" + RULES_SUPPORTED_FILETYPES;
                terminateProgram = false;
                break;
            // Mod building errors
            case MOD_DIRECTORY_NOT_FOUND:
                formattedString += "The mod directory " + arg0 + " does not exist.";
                break;
            case MOD_FILE_NOT_FOUND:
                formattedString += "The file " + arg0 + " was specified in the configuration file but does not actually exist in the mod."
                    + " This filepath will be ignored.";
                terminateProgram = false;
                break;
            case INCOMPLETE_LABEL:
                formattedString += "The file " + arg0 + " has an incomplete label that's missing a '" + LABEL_BORDER_VALUE 
                    + "' on it's right side.\n" + RULES_LABEL_FORMATTING;
                break;
            case MISSING_EXP_SEPARATOR:
                formattedString += "The file " + arg0 + " has a label " + arg1 + " with no '" + LABEL_NAME_EXP_SEPARATOR
                    + "' character separating the type from the expression. Unable to parse this label.\n" + RULES_LABEL_FORMATTING;
                break;
            case UNRECOGNIZED_TYPE:
                formattedString += "The file " + arg0 + " contains a label " + arg1 + " with an unrecognized type.\n" + RULES_LABEL_FORMATTING;
                break;
            case EXP_LOOPS_INFINITELY:
                formattedString += "The file " + arg0 + " contains a label " + arg1 + " whose expression caused an infinite loop when "
                    + "attempting to replace Option names with values.\n" + "Last edited form of the expression: \"" + arg2 + "\"\n"; 
                break;
            case EXP_FAILED_TO_EVALUATE:
                formattedString += "The file " + arg0 + " contains a label " + arg1 + " whose expression caused an error during evaluation.\n"
                    + "Expression form at evaluation: \"" + arg2 + "\"\n"
                    + "\nPrinting Error Message:\n" + arg3;
                break;
            case EXTRA_END_TOGGLE:
                formattedString += "The file " + arg0 + " has an '" + LABEL_END_TOGGLEABLE
                    + "' label with no accompanying start label.\n" + RULES_TOGGLE_BLOCK;
                break;
            case MISSING_END_TOGGLE:
                formattedString += "The file " + arg0 + " has a toggle label " + arg1 + " without a '" + LABEL_END_TOGGLEABLE
                    + "' label to denote the end of the toggle block.\n" + RULES_TOGGLE_BLOCK;
                break;
            case BAD_TOGGLE_EXP_RESULT:
                formattedString += "The file " + arg0 + "has a toggle label " + arg1 + " whose expression result cannot be interpreted "
                    + "correctly by a toggle label.\n" + "Expression Result: \"" + arg2 + "\"\n" + RULES_TOGGLE_EXP_RESULT;
                break;
        }
        System.Console.WriteLine(formattedString + "\n" + MESSAGE_FAILURE);
        
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
    INCOMPLETE_LABEL,
    MISSING_EXP_SEPARATOR,
    UNRECOGNIZED_TYPE,
    EXP_LOOPS_INFINITELY,
    EXP_FAILED_TO_EVALUATE,
    EXTRA_END_TOGGLE,
    MISSING_END_TOGGLE,
    BAD_TOGGLE_EXP_RESULT
}