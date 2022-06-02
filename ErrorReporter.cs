class ErrorReporter
{
    public static void ProcessErrorCode(ErrorCode error, string arg0 = "", string arg1 = "", string arg2 = "", string arg3 = "")
    {
        bool terminateProgram = true;
        string formattedString = "";
        switch (error)
        {
            // Anything not handled below
            case UNKNOWN_ERROR:
                formattedString += "An unknown error occurred, printing Exception:\n" + arg0;
                break;
            // Command-line argument errors
            case BAD_NUMBER_ARGUMENTS:
                formattedString += "Invalid number of command line arguments. (Expected " + EXPECTED_ARG_COUNT + ", received " + arg0 + ")\n\n" + RULES_EXPECTED_USAGE;
                break;
            case BAD_ARGUMENT:
                formattedString += "Command line argument #" + arg0 + " is invalid.\n\n" + RULES_EXPECTED_USAGE;
                break;
            case BAD_CONFIG_EXTENSION:
                formattedString += "The configuration file '" + arg0 + "' must be a " + CONFIG_FILE_EXTENSION + " file.";
                break;
            case CONFIG_NOT_FOUND:
                formattedString += "Failed to find the configuration file '" + arg0 + "'";
                break;
            case MOD_NOT_FOUND:
                formattedString += "The mod directory or .zip file '" + arg0 + "' does not exist.";
                break;
            case MOD_NOT_VALID:
                formattedString += "The mod location '" + arg0 + "' is not recognized as a valid directory or .zip file.";
                break;
            case OUTPUT_PREEXISTING_FILE:
                formattedString += "There is a pre-existing file at your output location " + arg0 + "\n\n" + RULES_OUTPUT_LOCATION;
                break;
            case OUTPUT_NONEMPTY_DIRECTORY:
                formattedString += "There is a pre-existing, non-empty directory at your output location " + arg0 + "\n\n" + RULES_OUTPUT_LOCATION;
                break;
            // Config. file parsing errors
            case BAD_JSON_FILE:
                formattedString += "The configuration file has a syntax error, printing Exception message:\n" + arg0;
                break;
            case OPTION_ISNT_OBJECT:
                formattedString += "The Option '" + arg0 + "' is not defined as a Json object in the configuration file.";
                break;
            case BAD_NAME_FORMATTING:
                formattedString += "The Option name '" + arg0 + "' is invalid.\n\n" + RULES_OPTION_NAME_CHARACTERS;
                break;
            case DUPLICATE_NAME:
                formattedString += "The name '" + arg0 + "' is used multiple times in the configuration file."
                    + " A name may only be used to define one Option.";
                break;
            case BAD_OPTION_VALUE:
                formattedString += "The Option '" + arg0 + "' has it's required '" + PROPERTY_VALUE 
                    + "' property incorrectly defined, or missing entirely in the configuration file.\n\n" + RULES_OPTION_VALUE;
                break;
            case BAD_LOCATIONS_ARRAY:
                formattedString += "The Option '" + arg0 + "' has it's '" + PROPERTY_LOCATIONS 
                    + "' property incorrectly defined in the configuration file.\n\n" + RULES_PROPERTY_LOCATIONS;
                break;
            case ROOTED_LOCATIONS_FILE:
                formattedString += "The Option '" + arg0 + "' has a non-relative filepath '" + arg1 + "' inside it's '"
                    + PROPERTY_LOCATIONS + "' property. All listed filepaths MUST be relative.";
                break;
            case UNSUPPORTED_FILETYPE:
                formattedString += "The Option '" + arg0 + "' has an invalid or unsupported file '" + arg1
                    + "' in it's '" + PROPERTY_LOCATIONS + "' list. This file will not be checked. " + RULES_SUPPORTED_FILETYPES;
                terminateProgram = false;
                break;
            case MISSING_LOCATIONS_ARRAY:
                formattedString += "One of more of your configuration file's Options is missing it's '" + PROPERTY_LOCATIONS + "' property."
                    + " ALL of your mod's supported files will be checked for labels.";
                terminateProgram = false;
                break;
            // Propagation Errors
            case BAD_PROP_ARRAY:
                formattedString += "The special '" + PROPAGATE_PROPERTY + "' property in the configuration file has an incorrectly defined "
                    + "sub-property '" + arg0 + "'\n\n" + RULES_PROPAGATE_PROPERTY;
                break;
            case ROOTED_PROP_DIRECTORY:
                formattedString += "The '" + PROPAGATE_PROPERTY + "' property in the configuration file has a sub-property named '" + arg0
                    + "', which is a non-relative filepath. These sub-properties MUST have relative filepaths as their names.";
                break;
            case ROOTED_PROP_FILE:
                formattedString += "The '" + PROPAGATE_PROPERTY + "' property in the configuration file has a list '" + arg0 + "' with filepath '"
                    + arg1 + "'. This filepath is non-relative, when all listed filepaths MUST be relative.";
                break;
            case PROPAGATE_DIR_NO_LISTS:
                formattedString += "The '" + PROPAGATE_DIRECTORY + "' directory exists in your mods folder, but no valid propagation lists are defined. "
                    + "Propagation will not occur.";
                terminateProgram = false;
                break;
            case PROPAGATE_LISTS_NO_DIR:
                formattedString += "You have valid propagation lists, but the '" + PROPAGATE_DIRECTORY 
                    + "' directory does not exist in your mod folder. Propagation will not occur.";
                terminateProgram = false;
                break;
            case PROPAGATE_PATH_NOT_FOUND:
                formattedString += "The path '" + arg0 + "' in propagation list '" + arg1 + "' could not be found in the '"
                    + PROPAGATE_DIRECTORY + "' folder. This file will be ignored.";
                terminateProgram = false;
                break;
            // Mod building errors
            case LOCATIONS_FILE_NOT_FOUND:
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
            case BAD_TYPE:
                formattedString += "The file " + arg0 + " contains a label " + arg1 + " with an unrecognized type.\n" + RULES_LABEL_FORMATTING;
                break;
            case EXP_LOOPS_INFINITELY:
                formattedString += "The file " + arg0 + " contains a label " + arg1 + " whose expression caused an infinite loop when "
                    + "attempting to replace Option names with values.\n" + "Last edited form of the expression: \"" + arg2 + "\"\n"; 
                break;
            case CANT_EVAL_EXP:
                formattedString += "The file " + arg0 + " contains a label " + arg1 + " whose expression caused an error during evaluation.\n"
                    + "Expression form at evaluation: \"" + arg2 + "\"\n"
                    + "\nPrinting Error Message:\n" + arg3;
                break;
            case EXTRA_END_TOGGLE:
                formattedString += "The file " + arg0 + " has an '" + LABEL_END_TOG
                    + "' label with no accompanying start label.\n" + RULES_TOGGLE_BLOCK;
                break;
            case MISSING_END_TOGGLE:
                formattedString += "The file " + arg0 + " has a toggle label " + arg1 + " without a '" + LABEL_END_TOG
                    + "' label to denote the end of the toggle block.\n" + RULES_TOGGLE_BLOCK;
                break;
            case BAD_TOGGLE_TYPE:
                formattedString += "The file " + arg0 + " has an invalid toggle label '" + arg1 + "'\n\n" + RULES_TOGGLE_BLOCK;
                break;
            case BAD_TOGGLE_EXP_RESULT:
                formattedString += "The file " + arg0 + " has a toggle label " + arg1 + " whose expression result cannot be interpreted "
                    + "correctly by a toggle label.\n" + "Expression Result: \"" + arg2 + "\"\n" + RULES_TOGGLE_EXP_RESULT;
                break;
        }
        
        if(terminateProgram)
        {
            System.Console.WriteLine(MESSAGE_ERROR + formattedString);
            System.Console.WriteLine("\n" + MESSAGE_FAILURE);
            Environment.Exit(1);
        }
        else
            System.Console.WriteLine(MESSAGE_WARNING + formattedString);
    }
}

enum ErrorCode
{
    // Anything not handled below
    UNKNOWN_ERROR,
    // Command-line argument errors
    BAD_NUMBER_ARGUMENTS,
    BAD_ARGUMENT,
    BAD_CONFIG_EXTENSION,
    CONFIG_NOT_FOUND,
    MOD_NOT_FOUND,
    MOD_NOT_VALID,
    OUTPUT_PREEXISTING_FILE,
    OUTPUT_NONEMPTY_DIRECTORY,
    // Config. file parsing errors
    BAD_JSON_FILE,
    OPTION_ISNT_OBJECT,
    BAD_NAME_FORMATTING,
    DUPLICATE_NAME,
    BAD_OPTION_VALUE,
    BAD_LOCATIONS_ARRAY,
    ROOTED_LOCATIONS_FILE,
    UNSUPPORTED_FILETYPE,
    MISSING_LOCATIONS_ARRAY,
    // Propagation Errors
    BAD_PROP_ARRAY,
    ROOTED_PROP_DIRECTORY,
    ROOTED_PROP_FILE,
    PROPAGATE_DIR_NO_LISTS,
    PROPAGATE_LISTS_NO_DIR,
    PROPAGATE_PATH_NOT_FOUND,
    // Mod Building Errors
    LOCATIONS_FILE_NOT_FOUND,
    INCOMPLETE_LABEL,
    MISSING_EXP_SEPARATOR,
    BAD_TYPE,
    EXP_LOOPS_INFINITELY,
    CANT_EVAL_EXP,
    EXTRA_END_TOGGLE,
    MISSING_END_TOGGLE,
    BAD_TOGGLE_TYPE,
    BAD_TOGGLE_EXP_RESULT
}