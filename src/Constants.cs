using static System.StringComparison;
interface Constants
{
    // Constants pertaining published file data
    const string EXE_NAME = "EternalModBuilder";
    const string EXE_VERSION = "Beta 1.2.0";

    // Abbreviated form of CurrentCultureIgnoreCase
    const StringComparison CCIC = CurrentCultureIgnoreCase;

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

    // Constants pertaining to expressions
    const int INFINITE_LOOP_THRESHOLD = 500;
    const string NULL_EXP_RESULT = "NULL";

    // Constants pertaining to labels
    const string LABEL_CHAR_BORDER    = "$";
    const string LABEL_CHAR_SEPARATOR = "#";
    const string LABEL_ANY            = LABEL_CHAR_BORDER + "_INJECTOR_";
    const string LABEL_ANY_VARIABLE   = LABEL_ANY + "VARIABLE_";
    const string LABEL_ANY_TOG    = LABEL_ANY + "TOGGLE_";
    const string LABEL_START_TOG  = LABEL_ANY_TOG + LABEL_CHAR_SEPARATOR;
    const string LABEL_END_TOG    = LABEL_ANY_TOG + "END_" + LABEL_CHAR_BORDER;

    // Constants pertaining to program termination and error messages
    const string MSG_WELCOME = EXE_NAME + " " + EXE_VERSION + " by FlavorfulGecko5";
    const string MSG_ERROR   = "ERROR: ";
    const string MSG_WARNING = "WARNING: ";

    const string MSG_SUCCESS = "Your mod was successfully built.\n\n"
    + "Please Note:\n"
    + "- Only " + DESC_LABEL_FILES + " files are checked for labels.\n"
    + "- This program can't detect every conceivable typo you might make.\n"
    + "If your game crashes, double-check your mod files for errors.\n";       
    
    const string MSG_FAILURE = "Mod building halted due to the above error.";

    // Rules
    const string RULES_USAGE = "Usage: ./" + EXE_NAME + ".exe -c [config "
    + DESC_CFG_EXTENSIONS + "] -s [mod folder or .zip] -o [output folder or .zip]\n"
    + "You may enter multiple configuration files (use '-c' once per file).";

    const string RULES_OUTPUT = "Your output location must obey these rules:\n" 
    + "- If outputting to a folder, it must be empty or non-existant, unless named '" + DIR_TEMP + "'.\n"
    + "-- This directory is ALWAYS deleted when this program is executed.\n"
    + "- No file may already exist at the output location.\n"
    + "- Your output path cannot be inside of your source directory.";
    
    const string RULES_OPTION_TYPE = "Options must be defined in one of the following ways:\n"
    + "- String: Any text encased in double-quotes.\n"
    + "- Number: An integer or floating-point value.\n"
    + "- Boolean: Either 'true' or 'false' (case sensitive).\n"
    + "- Object: This must have a '" + PROPERTY_VALUE + "' (case-sensitive) property defined as a string, number or Boolean.";

    const string RULES_OPTION_NAME = "Option names cannot be empty, and may only contain these characters:\n"
    + "- Letters (a-z, A-Z)\n"
    + "- Numbers (0-9)\n"
    + "- Underscores (_)\n"
    + "Names are case-insensitive, so duplicate names with different capitalizations are not allowed.";
    
    const string RULES_PROPAGATE = "Configuration files may have '" + PROPERTY_PROPAGATE + "' properties that must obey these rules:\n"
    + "- These properties must be defined as objects.\n"
    + "- Each sub-property's name should be a relative directory.\n"
    + "- Each sub-property must be defined as a list of strings.\n" 
    + "- These strings must be relative paths to files or directories inside your mod's '" + DIR_PROPAGATE + "' folder.\n"
    + "When your mod is built, listed files/directories will be copied to the directory specified by the list's name.";

    const string RULES_LABEL_FORMAT = "Labels must have the form " + LABEL_ANY 
    + "[TYPE]" + LABEL_CHAR_SEPARATOR + "[EXPRESSION]" + LABEL_CHAR_BORDER + " where:\n"
    + "- [TYPE] is a pre-defined string - see examples that show all types.\n"
    + "- Case-insensitivity of [TYPE] and it's preface is allowed.\n"
    + "- [EXPRESSION] is a valid arithmetic or logical expression - see examples.\n"
    + "- To insert an option from your config. files into an expression, use the notation {NAME}";

    const string RULES_TOGGLE_BLOCK = "Each toggle label must have exactly one '" + LABEL_END_TOG + "' label placed after it.\n"
    + "These two labels define the toggle-block controlled by the expression.";

    const string RULES_TOGGLE_EXP = "Expressions in toggle labels must yield one of the following types of results:\n"
    + "- A Boolean (true/false) value, from evaluating a logical expression or translating a string to one of these values.\n"
    + "- A numerical value. A number less than one is interpeted as false, and one or higher is interpreted as true.";
}