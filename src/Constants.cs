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
/// Enumerates all types that configuration files can use to define
/// an Option as.
/// </summary>
public enum OptionType
{
    STANDARD_PRIMITIVE,
    STANDARD_LIST,
    COMMENT,
    PROPAGATER
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

    /// <summary>
    /// Describes what each OptionType enum represents and
    /// what behavior is expected when they're utilized.
    /// </summary>
    public const string SUMMARY_OPTIONTYPE = 
      "Standard   - A primitive (number/Boolean/String) value or list of primitives.\n"
    + "Comment    - Any Options using this type will be ignored when reading the config.\n"
    + "Propagater - Used to control EternalModBuilder's propagation feature.";
}

interface Constants
{
    // Constants pertaining to directories
    
    const string DIR_PROPAGATE = "propagate";

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