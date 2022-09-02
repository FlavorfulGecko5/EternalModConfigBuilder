interface Constants
{
    // Constants pertaining published file data
    const string EXE_NAME = "EternalModBuilder";
    const string EXE_VERSION = "INDEV-Beta 1.6.0";

    // Enums and Constants for the Execution Mode command-line argument
    public enum ExecutionMode
    {
        COMPLETE,
        READONLY,
        PARSE,
        PROPAGATE
    }
    const string DESC_EXEMODE = "Optional Parameter - Execution Mode\n"
    + "-x [ complete | readonly | parse | propagate ] (Choose One)\n"
    + "complete  - Reads config. files and performs all build operations (default)\n"
    + "readonly  - Reads config. files but performs no build operations.\n"
    + "parse     - Reads config. files and parses labels.\n"
    + "propagate - Reads config. files and propagates files.";

    // Enums and constants for the Log Level command-line argument
    public enum LogLevel
    {
        MINIMAL,
        CONFIGS,
        PARSINGS,
        PROPAGATIONS,
        VERBOSE
    }
    const string DESC_LOGLEVEL = "Optional Parameter - Log Level\n"
    + "-l [ minimal | configs | parsing | propagations | all ] (Choose One)\n"
    + "minimal      - Only outputs errors and warnings (default)\n"
    + "configs      - Outputs parsed command-line argument and configuration file data.\n"
    + "parsings     - Outputs what each label's expression resolved to.\n"
    + "propagations - Outputs each successful propagation.\n"
    + "verbose      - Outputs everything";

    // Constants pertaining to command-line arguments
    const long MAX_INPUT_SIZE_BYTES = 2000000000; // ~2 gigabytes
    
    // Constants pertaining to file extensions.
    static readonly string[] CFG_EXTENSIONS = new string[] {".txt", ".json"};
    const string DESC_CFG_EXTENSIONS = ".txt or .json";
    static readonly string[] LABEL_FILES = new string[] {".decl", ".json"};
    const string DESC_LABEL_FILES = ".decl and .json";

    // Constants pertaining to directories
    const string DIR_TEMP = "eternalmodbuilder_temp";
    const string DIR_PROPAGATE = "propagate";
    
    // Constants pertaining to option names
    const string NAME_SPECIAL_CHARACTERS = "_";

    // Constants pertaining to Json property names
    const string PROPERTY_VALUE = "Value";
    const string PROPERTY_PROPAGATE = "Propagate";

    // Constants pertaining to Label and expression parsing
    const int PARSER_INFINITE_LOOP_THRESHOLD = 1000;
    const int EXP_INFINITE_LOOP_THRESHOLD = 500;
    const string NULL_EXP_RESULT = "NULL";

    // Enums and Constants pertaining to labels
    enum LabelType
    {
        VAR,
        TOGGLE_START,
        TOGGLE_END
    }
    const string LABEL_CHAR_BORDER    = "$";
    const string LABEL_CHAR_SEPARATOR = "#";
    const string LABEL_ANY            = LABEL_CHAR_BORDER + "EMB_";
    const string LABEL_VAR            = LABEL_ANY + "VAR";
    const string LABEL_TOGGLE_ANY     = LABEL_ANY + "TOGGLE";
    const string LABEL_TOGGLE_START   = LABEL_TOGGLE_ANY;
    const string LABEL_TOGGLE_END     = LABEL_TOGGLE_ANY + "_END";
    const string DESC_LABEL_TYPES = "The current valid types for labels are:\n"
    + "- 'EMB_VAR'\n"
    + "- 'EMB_TOGGLE'\n"
    + "- 'EMG_TOGGLE_END'";
    const string DESC_LABEL_TOGGLE_END = LABEL_TOGGLE_END + LABEL_CHAR_SEPARATOR + LABEL_CHAR_BORDER;

    // Constants pertaining to log messages
    const string MSG_WELCOME = "\n" + EXE_NAME + " " + EXE_VERSION + " by FlavorfulGecko5";
    const string MSG_ERROR   = "ERROR: ";
    const string MSG_WARNING = "WARNING: ";
    const string MSG_LOG     = "LOG: ";

    const string MSG_SUCCESS = "\nYour mod was successfully built.\n\n"
    + "Please Note:\n"
    + "- Only " + DESC_LABEL_FILES + " files are checked for labels.\n"
    + "- This program can't detect every conceivable typo you might make.\n"
    + "If your game crashes, double-check your mod files for errors.\n";       
    
    const string MSG_FAILURE = "\n\nMod building halted due to the above error.\n";

    // Rules
    const string RULES_USAGE_GENERAL = "Usage: ./" + EXE_NAME + ".exe -c [config "
    + DESC_CFG_EXTENSIONS + "] -s [mod folder or .zip] -o [output folder or .zip]\n"
    + "You may enter multiple configuration files (use '-c' once per file).\n\n";

    const string RULES_USAGE_MINIMAL = RULES_USAGE_GENERAL 
    + "For information on optional parameters, run this application with 0 arguments.";

    const string RULES_USAGE_VERBOSE = RULES_USAGE_GENERAL + DESC_EXEMODE + "\n\n" + DESC_LOGLEVEL;

    const string RULES_OUTPUT = "Your output location must obey these rules:\n" 
    + "- If outputting to a folder, it must be empty or non-existant, unless named '" + DIR_TEMP + "'.\n"
    + "-- This directory is ALWAYS deleted when this program is executed.\n"
    + "- No file may already exist at the output location.\n"
    + "- Your output path cannot be inside of your source directory.";
    
    const string RULES_OPTION_TYPE = "Options must be defined in one of the following ways:\n"
    + "- String: Any text encased in double-quotes.\n"
    + "- Number: An integer or floating-point value.\n"
    + "- Boolean: Either 'true' or 'false' (case sensitive).\n"
    + "- List: A list of values containing only strings, numbers and Booleans.\n"
    + "- Object: This must have a '" + PROPERTY_VALUE + "' (case-sensitive) sub-property defined in one of the above ways.";

    const string RULES_OPTION_NAME = "Option names cannot be empty, and may only contain these characters:\n"
    + "- Letters (a-z, A-Z)\n"
    + "- Numbers (0-9)\n"
    + "- Underscores (_)\n"
    + "Names are case-insensitive, so duplicate names with different capitalizations are not allowed.";
    
    const string RULES_PROPAGATE = "Configuration files may have '" + PROPERTY_PROPAGATE + "' properties that must obey these rules:\n"
    + "- These properties must be defined as objects.\n"
    + "- Each sub-property's name should be a relative, non-backtracking directory.\n"
    + "- Each sub-property must be defined as a list of strings.\n" 
    + "- These strings must be relative paths to files or directories inside your mod's '" + DIR_PROPAGATE + "' folder.\n"
    + "When your mod is built, listed files/directories will be copied to the directory specified by the list's name.";

    const string RULES_LABEL_FORMAT = "Labels must have the form " + LABEL_CHAR_BORDER 
    + "[TYPE]" + LABEL_CHAR_SEPARATOR + "[EXPRESSION]" + LABEL_CHAR_BORDER + " where:\n"
    + "- [TYPE] is a pre-defined string - see examples that show all types.\n"
    + "- [EXPRESSION] is a valid arithmetic or logical expression - see examples.\n"
    + "- To insert an option from your config. files into an expression, use the notation {NAME}\n"
    + "- Case-insensitivity of all label elements is allowed.";

    const string RULES_TOGGLE_BLOCK = "Each toggle label must have exactly one '"
    + DESC_LABEL_TOGGLE_END + "' label placed after it.\n"
    + "These two labels define the toggle-block controlled by the expression.";

    const string RULES_TOGGLE_RESULT = "Expressions in toggle labels must yield one of these results:\n"
    + "- A Boolean (true/false) value, from a logical expression or from reading a string.\n"
    + "- A numerical value. A number less than one is interpeted as false, and one or higher is interpreted as true.";
}