/// <summary>
/// Describes all modes EternalModBuilder can run in
/// </summary>
public enum ExecutionMode
{
    COMPLETE,
    READONLY,
    PARSE,
    PROPAGATE
}

/// <summary>
/// Describes all levels EternalModBuilder's logging feature can operate at
/// </summary>
public enum LogLevel
{
    MINIMAL,
    CONFIGS,
    PARSINGS,
    PROPAGATIONS,
    VERBOSE
}

/// <summary>
/// Contains string descriptions for any enums defined in this file
/// </summary>
class EnumDesc
{
    /// <summary>
    /// Describes what behavior is expected for each element of the
    /// ExecutionMode enum when they're utilized
    /// </summary>
    public const string SUMMARY_EXEMODE = 
      "complete  - Reads config. files and performs all build operations (default)\n"
    + "readonly  - Reads config. files but performs no build operations.\n"
    + "parse     - Reads config. files and parses labels.\n"
    + "propagate - Reads config. files and propagates files.";

    /// <summary>
    /// Describes what behavior is expected for each element of the
    /// LogLevel enum when they're utilized
    /// </summary>
    public const string SUMMARY_LOGLEVEL = 
      "minimal      - Only outputs errors and warnings (default)\n"
    + "configs      - Outputs parsed command-line argument and configuration file data.\n"
    + "parsings     - Outputs what each label's expression resolved to.\n"
    + "propagations - Outputs each successful propagation.\n"
    + "verbose      - Outputs everything";
}

interface Constants
{
    // Constants pertaining to directories
    
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
        INVALID,
        VAR,
        TOGGLE_START,
        TOGGLE_END,
        LOOP
    }
    const string LABEL_CHAR_BORDER         = "$";
    const string LABEL_CHAR_SEPARATOR      = "#";
    const string LABEL_CHAR_LOOP_SEPARATOR = "&";
    const string LABEL_ANY            = LABEL_CHAR_BORDER + "EMB_";
    const string LABEL_VAR            = LABEL_ANY + "VAR";
    const string LABEL_TOGGLE_ANY     = LABEL_ANY + "TOGGLE";
    const string LABEL_TOGGLE_START   = LABEL_TOGGLE_ANY;
    const string LABEL_TOGGLE_END     = LABEL_TOGGLE_ANY + "_END";
    const string LABEL_LOOP           = LABEL_ANY + "LOOP";
    const string DESC_LABEL_TYPES = "The current valid types for labels are:\n"
    + "- 'EMB_VAR'\n"
    + "- 'EMB_TOGGLE'\n"
    + "- 'EMG_TOGGLE_END'\n"
    + "- 'EMB_LOOP'";
    const string DESC_LABEL_TOGGLE_END = LABEL_TOGGLE_END + LABEL_CHAR_SEPARATOR + LABEL_CHAR_BORDER;

    const string SYM_LOOP_INC = "!inc";
    const string SYM_SUBEXP_START = "!sub";
    const string SYM_SUBEXP_END   = "!subend";

    // Rules
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
    + "Names are case-insensitive, so duplicate names with different capitalizations are not allowed.\n"
    + "Options with names beginning with an underscore (_) will be treated as comments, and won't be processed as variables.";
    
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

    const string RULES_LOOPS = "Loop labels have the form "
    + LABEL_LOOP + LABEL_CHAR_SEPARATOR + "[Start]" + LABEL_CHAR_LOOP_SEPARATOR
    + "[Stop]" + LABEL_CHAR_LOOP_SEPARATOR + "[Expression]" + LABEL_CHAR_BORDER + " where:\n"
    + "- [Start] and [Stop] are expressions that evaluate to integers.\n"
    + "- [Start] is less than or equal to [Stop]\n"
    + "- You may use '{" + SYM_LOOP_INC + "}' in [Expression] to get the value of the current loop iteration.\n"
    + "When evaluated, a loop will repeat [Expression] once for every integer between [Start] and [Stop], inclusive.";

    const string RULES_SUBEXPRESSIONS = "A subexpression block:\n"
    + "- Starts with the symbol '{" + SYM_SUBEXP_START 
    + "}'\n- Ends with the symbol '{" + SYM_SUBEXP_END 
    + "}'\nAnything inside a subexpression block will be fully evaluated before the rest of the expression.";
}