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
                formattedString = String.Format
                (
                    "An unknown error occurred, printing Exception:\n\n{0}", 
                    arg0 // Exception ToString()
                );
                break;
            // Command-line argument errors
            case BAD_NUMBER_ARGUMENTS:
                formattedString = String.Format
                (
                    "Invalid number of command line arguments. (Expected {0}, received {1})\n\n{2}",
                    EXPECTED_ARG_COUNT, 
                    arg0, // Number of arguments received. 
                    RULES_EXPECTED_USAGE
                );
                break;
            case BAD_ARGUMENT:
                formattedString = String.Format
                (
                    "Command line argument #{0} is invalid.\n\n{1}",
                    arg0, // The invalid argument
                    RULES_EXPECTED_USAGE
                );
                break;
            case BAD_CONFIG_EXTENSION:
                formattedString = String.Format
                (
                    "The configuration file '{0}' must be a '{1}' file.",
                    arg0, // The config. filepath 
                    CONFIG_FILE_EXTENSION
                );
                break;
            case CONFIG_NOT_FOUND:
                formattedString = String.Format
                (
                    "Failed to find the configuration file '{0}'",
                    arg0 // The config. filepath
                );
                break;
            case MOD_NOT_FOUND:
                formattedString = String.Format
                (
                    "The mod directory or .zip file '{0}' does not exist.",
                    arg0 // The mod filepath
                );
                break;
            case MOD_NOT_VALID:
                formattedString = String.Format
                (
                    "The mod file/folder '{0}' is not recognized as a valid directory or .zip file.",
                    arg0 // The mod filepath
                );
                break;
            case OUTPUT_PREEXISTING_FILE:
                formattedString = String.Format
                (
                    "There is a pre-existing file at your output location '{0}'\n\n{1}",
                    arg0, // The output path
                    RULES_OUTPUT_LOCATION
                );
                break;
            case OUTPUT_NONEMPTY_DIRECTORY:
                formattedString = String.Format
                (
                    "There is a pre-existing, non-empty directory at your output location '{0}'\n\n{1}",
                    arg0, // The output path
                    RULES_OUTPUT_LOCATION
                );
                break;
            // Config. file parsing errors
            case BAD_JSON_FILE:
                formattedString = String.Format
                (
                    "The configuration file has a syntax error, printing Exception message:\n\n{0}",
                    arg0 // The exception message
                );
                break;
            case OPTION_ISNT_OBJECT:
                formattedString = String.Format
                (
                    "The Option '{0}' is not defined as a Json object in the configuration file.",
                    arg0 // The option name
                );
                break;
            case BAD_NAME_FORMATTING:
                formattedString = String.Format
                (
                    "The Option name '{0}' is invalid.\n\n{1}",
                    arg0, // The option name
                    RULES_OPTION_NAME_CHARACTERS
                );
                break;
            case DUPLICATE_NAME:
                formattedString = String.Format
                (
                    "The name '{0}' is used multiple times in the configuration file. A name may only be used to define one Option.",
                    arg0 // The option name
                );
                break;
            case BAD_OPTION_VALUE:
                formattedString = String.Format
                (
                    "The Option '{0}' has it's required '{1}' property incorrectly defined, or missing entirely in the configuration file.\n\n{2}",
                    arg0, // The option name
                    PROPERTY_VALUE,
                    RULES_OPTION_VALUE
                );
                break;
            // Propagation Errors
            case BAD_PROP_ARRAY:
                formattedString = String.Format
                (
                    "The '{0}' property in the configuration file has an incorrectly defined sub-property '{1}'\n\n{2}",
                    PROPAGATE_PROPERTY,
                    arg0, // Propagate list name
                    RULES_PROPAGATE_PROPERTY
                );
                break;
            case ROOTED_PROP_DIRECTORY:
                formattedString = String.Format
                (
                    "The '{0}' property in the configuration file has a sub-property named '{1}', which is a non-relative filepath. These sub-properties MUST have relative filepaths as their names.",
                    PROPAGATE_PROPERTY,
                    arg0 // Propagate list name
                );
                break;
            case ROOTED_PROP_FILE:
                formattedString = String.Format
                (
                    "The '{0}' property in the configuration file has a list '{1}' with filepath '{2}'. This filepath is non-relative, when all listed filepaths MUST be relative.",
                    PROPAGATE_PROPERTY,
                    arg0, // The list name
                    arg1 // The non-relative list element
                );
                break;
            case PROPAGATE_DIR_NO_LISTS:
                formattedString = String.Format
                (
                    "The '{0}' directory exists in your mod folder, but no valid propagation lists are defined. Propagation will not occur.",
                    PROPAGATE_DIRECTORY
                );
                terminateProgram = false;
                break;
            case PROPAGATE_LISTS_NO_DIR:
                formattedString = String.Format
                (
                    "You have valid propagation lists, but the '{0}' directory does not exist in your mod folder. Propagation will not occur.",
                    PROPAGATE_DIRECTORY
                );
                terminateProgram = false;
                break;
            case PROPAGATE_PATH_NOT_FOUND:
                formattedString = String.Format
                (
                    "The path '{0}' in propagation list '{1}' could not be found in the '{2}' folder. This path will be ignored.",
                    arg0, // The filepath
                    arg1, // The list name
                    PROPAGATE_DIRECTORY
                );
                terminateProgram = false;
                break;
            // Mod building errors
            case INCOMPLETE_LABEL:
                formattedString = String.Format
                (
                    "The file '{0}' has an incomplete label that's missing a '{1}' on it's right side.\n\n{2}",
                    arg0, // The file name
                    LABEL_BORDER_VALUE,
                    RULES_LABEL_FORMATTING
                );
                break;
            case MISSING_EXP_SEPARATOR:
                formattedString = String.Format
                (
                    "The file '{0}' has a label '{1}' with no '{2}' separating the type from the expression.\n\n{3}",
                    arg0, // The file name
                    arg1, // The label 
                    LABEL_NAME_EXP_SEPARATOR,
                    RULES_LABEL_FORMATTING
                );
                break;
            case BAD_TYPE:
                formattedString = String.Format
                (
                    "The file '{0}' contains a label '{1}' with an unrecognized type.\n\n{2}",
                    arg0, // The file name
                    arg1, // The label
                    RULES_LABEL_FORMATTING
                );
                break;
            case EXP_LOOPS_INFINITELY:
                formattedString = String.Format
                (
                    "The file '{0}' contains a label '{1}' whose expression loops infinitely when replacing Option names with values.\nLasted edited form of the expression: '{2}'",
                    arg0, // The file name
                    arg1, // The label
                    arg2  // The last-edited form of the expression
                );
                break;
            case CANT_EVAL_EXP:
                formattedString = String.Format
                (
                    "The file '{0}' contains a label '{1}' whose expression caused an error during evaluation.\nExpression form at evaluation: '{2}'\nPrinting Error Message\n{3}",
                    arg0, // The file name
                    arg1, // The label
                    arg2, // Expression at evaluation
                    arg3 // Exception message
                );
                break;
            case EXTRA_END_TOGGLE:
                formattedString = String.Format
                (
                    "The file '{0}' has an '{1}' label with no accompanying start label.\n\n{2}",
                    arg0, // File name
                    LABEL_END_TOG,
                    RULES_TOGGLE_BLOCK
                );
                break;
            case MISSING_END_TOGGLE:
                formattedString = String.Format
                (
                    "The file '{0}' has a toggle label '{1}' without a '{2}' label to denote the end of the toggle block.\n\n{3}",
                    arg0, // The file name
                    arg1, // The label
                    LABEL_END_TOG,
                    RULES_TOGGLE_BLOCK
                );
                break;
            case BAD_TOGGLE_TYPE:
                formattedString = String.Format
                (
                    "The file '{0}' has an invalid toggle label '{1}'\n\n{2}",
                    arg0, // The file name
                    arg1, // The label
                    RULES_TOGGLE_BLOCK
                );
                break;
            case BAD_TOGGLE_EXP_RESULT:
                formattedString = String.Format
                (
                    "The file '{0}' has a toggle label '{1}' whose expression result cannot be interpreted as a Boolean value.\nExpression Result: '{2}'\n\n{3}",
                    arg0, // File name
                    arg1, // Label
                    arg2, // Expression result
                    RULES_TOGGLE_EXP_RESULT
                );
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
    // Propagation Errors
    BAD_PROP_ARRAY,
    ROOTED_PROP_DIRECTORY,
    ROOTED_PROP_FILE,
    PROPAGATE_DIR_NO_LISTS,
    PROPAGATE_LISTS_NO_DIR,
    PROPAGATE_PATH_NOT_FOUND,
    // Mod Building Errors
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