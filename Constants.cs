using static System.StringComparison;
class Constants
{
    // Constants pertaining to the names of published files
    public const string EXECUTABLE_NAME = "EternalModBuilder";

    // Constants pertaining to command-line arguments
    public const int EXPECTED_ARG_COUNT = 6;
    
    // Constants pertaining to file extensions.
    public static readonly string[] CONFIG_EXTENSIONS = new string[] {".txt", ".json"};
    public const string DESC_CONFIG_EXTENSIONS = ".txt or .json";
    public static readonly string[] SUPPORTED_FILETYPES = new string[] { ".decl", ".json" };

    // Constants pertaining to directories
    public const string TEMP_DIRECTORY = "eternalmodbuilder_temp";

    // Abbreviated form of CurrentCultureIgnoreCase
    public const StringComparison CCIC = CurrentCultureIgnoreCase;
    
    // Constants pertaining to option names
    public const string NAME_SPECIAL_CHARACTERS = "_";

    // Constants pertaining to Option properties
    public const string PROPERTY_VALUE = "Value";

    // Constants pertaining to expressions
    public const int INFINITE_LOOP_THRESHOLD = 500;
    public const string NULL_EXP_RESULT = "null";

    // Constants pertaining to labels
    public const string LABEL_BORDER_VALUE       = "$";
    public const string LABEL_NAME_EXP_SEPARATOR = "#";
    public const string LABEL_ANY                = LABEL_BORDER_VALUE + "_INJECTOR_";
    public const string LABEL_ANY_VARIABLE       = LABEL_ANY + "VARIABLE_";
    public const string LABEL_ANY_TOG            = LABEL_ANY + "TOGGLE_";
    public const string LABEL_START_TOG          = LABEL_ANY_TOG + LABEL_NAME_EXP_SEPARATOR;
    public const string LABEL_END_TOG            = LABEL_ANY_TOG + "END_" + LABEL_BORDER_VALUE;

    // Constants pertaining to file propagation
    public const string PROPAGATE_PROPERTY  = "Propagate";
    public const string PROPAGATE_DIRECTORY = "propagate";

    // Constants pertaining to program termination and error messages
    public const string MESSAGE_ERROR   = EXECUTABLE_NAME + " ERROR: ";
    public const string MESSAGE_WARNING = EXECUTABLE_NAME + " WARNING: ";

    public const string MESSAGE_SUCCESS = "Your mod has been successfully built from the configuration file.\n"
        + "Please remember that this program cannot catch every conceivable typo made when inserting labels into your mod files.\n"
        + "If your game crashes after injecting the mod, please double-check your mod files for errors.";
    
    public const string MESSAGE_FAILURE = "Mod building has halted due to the critical errors described above";

    // Rules
    public const string RULES_EXPECTED_USAGE = "Usage: ./"  + EXECUTABLE_NAME + ".exe -c [" + DESC_CONFIG_EXTENSIONS
        + " config file] -s [mod directory or zip file] -o [output directory or zip file]\n"
        + "WARNING - IF THE OUTPUT DIRECTORY ALREADY EXISTS, IT MUST BE EMPTY UNLESS USING '" + TEMP_DIRECTORY
        + "'\nIF YOU OUTPUT A ZIP FILE, THERE MUST BE NO OTHER FILE THAT EXISTS WITH THE SAME NAME AND PATHWAY.";

    public const string RULES_OUTPUT_LOCATION = "For data security and safety purposes, your output location must obey the following rules:\n" 
        + "- If outputting to a directory, it must be empty or non-existant, unless you use '" + TEMP_DIRECTORY
        + "' as your output directory. This directory will ALWAYS be deleted if it is detected at the start of program execution.\n"
        + "- There must be no pre-existing file at the output location. This program will not delete pre-existing files.";
    
    public const string RULES_OPTION_TYPE = "Options must have their values defined in one of the following ways:\n"
        + "- String: Any text encased in double-quotes.\n"
        + "- Number: An integer or floating-point value.\n"
        + "- Boolean: Either 'true' or 'false' (case sensitive).\n"
        + "- Json Object: This must have a '" + PROPERTY_VALUE + "' (case-sensitive) property defined as a string, number or Boolean.\n"
        + "- Json lists are not allowed.\n"
        + "- A null or empty Option value is not allowed.";

    public const string RULES_OPTION_NAME_CHARACTERS = "Option names cannot be empty, and may only contain the following characters:\n"
        + "- Letters (a-z, A-Z)\n"
        + "- Numbers (0-9)\n"
        + "- Underscores (_)\n"
        + "Names are case-insensitive, so duplicate names with different capitalizations are not allowed.";
    
    public const string RULES_PROPAGATE_PROPERTY = "Configuration files may have a '" + PROPAGATE_PROPERTY + "' property that must defined in a special way:\n"
        + "- This property must be a Json object.\n"
        + "- Each sub-property's name should be a relative directory.\n"
        + "- Each sub-property must be defined as a list of strings.\n" 
        + "- These strings must be relative paths to files or directories inside your unbuilt mod's '" + PROPAGATE_DIRECTORY + "' folder.\n"
        + "When your mod is built, files/directories listed in the sub-property arrays will be copied to the directory specified in the list's name.";

    public const string RULES_LABEL_FORMATTING = "Labels must have the form "
        + LABEL_ANY + "[TYPE]" + LABEL_NAME_EXP_SEPARATOR + "[EXPRESSION]" + LABEL_BORDER_VALUE + " where:\n"
        + "- [TYPE] is a pre-defined string, with possible values listed in the documentation.\n"
        + "- Case-insensitivity of the [TYPE] and it's preface is allowed.\n"
        + "- [EXPRESSION] is a syntactically valid arithmetic or logical expression - see documentation for examples.\n"
        + "- To insert an option from your configuration file into an expression, use the notation {NAME}";

    public const string RULES_TOGGLE_BLOCK = "Each toggle label placed in a mod file must have exactly one '" + LABEL_END_TOG + "' label placed after it "
        + "somewhere in the file. Together, these two labels denote what region of the file will be turned on and off by the toggle Option.";

    public const string RULES_TOGGLE_EXP_RESULT = "A Toggle label's expression must yield one of the following types of results:\n"
        + "- A Boolean (true/false) value, from evaluating a logical expression or translating a string to one of these values.\n"
        + "- A numerical value. A number less than one is interpeted as false, and one or higher is interpreted as true.";
}