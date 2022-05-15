class Constants
{
    // Constants pertaining to command-line arguments
    public const int EXPECTED_ARG_COUNT = 6;
    
    // Constants pertaining to file extensions.
    public const string CONFIG_FILE_EXTENSION = ".txt";
    public static readonly List<string> SUPPORTED_FILETYPES = new List<string>() { "decl", "json" };

    // Constants pertaining to directories
    public const string TEMPORARY_DIRECTORY = "TEMP_DIRECTORY_ETERNAL_MOD_CONFIG_BUILDER";
    
    // Constants pertaining to option names
    public const string NAME_SPECIAL_CHARACTERS = "_";

    // Constants pertaining to Option properties
    public const string PROPERTY_VALUE = "Value";
    public const string PROPERTY_LOCATIONS = "Locations";

    // Constants pertaining to expressions
    public const int INFINITE_LOOP_THRESHOLD = 500;

    // Constants pertaining to labels
    public const string LABEL_BORDER_VALUE       = "$";
    public const string LABEL_NAME_EXP_SEPARATOR = "#";
    public const string LABEL_ANY                = LABEL_BORDER_VALUE + "_INJECTOR_";
    public const string LABEL_ANY_VARIABLE       = LABEL_ANY + "VARIABLE_";
    public const string LABEL_ANY_TOGGLEABLE     = LABEL_ANY + "TOGGLEABLE_";
    public const string LABEL_END_TOGGLEABLE     = LABEL_ANY_TOGGLEABLE + "END_" + LABEL_BORDER_VALUE;

    // Rules
    public const string RULES_EXPECTED_USAGE = "Usage: ./EternalConfig.exe -c [" + CONFIG_FILE_EXTENSION
        + " config file] -s [mod directory or zip file] -o [output directory or zip file]\n"
        + "WARNING - IF THE OUTPUT DIRECTORY ALREADY EXISTS, ANY FILES INSIDE OF IT MAY BE OVERWRITTEN.\n"
        + "IF YOU OUTPUT A ZIP FILE, IT WILL OVERWRITE ANY ZIP FILE THAT ALREADY EXISTS WITH THAT NAME.";

    public const string RULES_OPTION_NAME_CHARACTERS = "Option names cannot be empty, and may only contain the following characters:\n"
        + "- Letters (a-z, A-Z)\n"
        + "- Numbers (0-9)\n"
        + "- Underscores (_)\n"
        + "Names are case-insensitive, so duplicate names with different capitalizations are not allowed.";

    public const string RULES_OPTION_VALUE = "An Option's '" + PROPERTY_VALUE + "' Property can be defined in the following ways:\n"
        + "- String: Any text encased in double-quotes.\n"
        + "- Number: An integer or floating-point value\n"
        + "- Boolean: Either 'true' or 'false' (case sensitive)\n"
        + "- Json lists and objects cannot be converted to strings and will cause an error if used.\n"
        + "- A null, empty or missing '" + PROPERTY_VALUE + "' field is not allowed.";

    public const string RULES_PROPERTY_LOCATIONS = "An Option's '" + PROPERTY_LOCATIONS + "' array must obey the following rules:\n"
        + "- A Json list is the only acceptable way to define this property.\n"
        + "- Each entry in the list should be a string, or errors will result when parsing it.\n"
        + "- Each string should represent a filepath to a supported filetype, starting from the .resources file.\n"
            + "   Example: gameresources_patch1/path/to/a/file.decl\n"
        + "- The list may be empty.\n"
        + "- If at least one '" + PROPERTY_LOCATIONS + "' list is null, undefined, or missing entirely, then every file of all "
            + "supported filetypes in your mod will be checked for labels, which may have a noticeable effect on build time " 
            + "if your mod has lots of files that don't need to be configured.";

    public const string RULES_SUPPORTED_FILETYPES = "Currently, this application only supports injecting configuration data into files of types:\n"
        + "- .decl\n- .json";

    public const string RULES_LABEL_FORMATTING = "Labels must have the form "
        + LABEL_ANY + "[TYPE]" + LABEL_NAME_EXP_SEPARATOR + "[EXPRESSION]" + LABEL_BORDER_VALUE + " where:\n"
        + "- [TYPE] is a pre-defined string, with possible values listed in the documentation.\n"
        + "- Case-insensitivity of the [TYPE] and it's preface is allowed.\n"
        + "- [EXPRESSION] is a syntactically valid arithmetic or logical expression - see documentation for examples.\n"
        + "- To insert an option from your configuration file into an expression, use the notation {NAME}";

    public const string RULES_TOGGLE_BLOCK = "Each toggle label placed in a mod file must have exactly one '" + LABEL_END_TOGGLEABLE + "' label placed after it "
        + "somewhere in the file. Together, these two labels denote what region of the file will be turned on and off by the toggle Option.";



     /* UNREVISED SHIT BELOW*/
 




    // Rules to print for the user in error messages


    public const string RULES_TOGGLEABLE_VALUE    = "A Toggleable's '" + PROPERTY_VALUE + "' Property can be defined in the following ways:\n"
        + "- Boolean: Either 'true' or 'false' (case sensitive). For all intents and purposes, this is what you should be using.\n"
        + "- String: Any string that can be parsed as 'true' or 'false' is accepted.\n"
        + "- Number: Use '1' for 'true' and '0' for 'false'. Other values give unpredictable results. Not recommended.\n"
        + "- Json lists and objects cannot be converted to strings and will cause an error if used.\n"
        + "- A null, empty or missing '" + PROPERTY_VALUE + "' field is not allowed.";
    


    // NEW STUFF

    
}

