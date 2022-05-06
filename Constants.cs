class Constants
{
    public const string LABEL_BORDER_VALUE        = "%";
    public const string LABEL_TYPE_PREFACE        = "_INJECTOR_";
    public const string LABEL_TYPE_NAME_SEPARATOR = "$";
    public const string TYPE_VARIABLE             = "VARIABLE_";
    public const string TYPE_TOGGLEABLE           = "TOGGLEABLE_";
    public const string TYPE_TOGGLEABLE_END       = "TOGGLEABLE_END_";
    public const string PROPERTY_NAME_VALUE       = "Value";
    public const string PROPERTY_NAME_LOCATIONS   = "Locations";
    public const string LABEL_FORMATTING_RULES    = "Labels must have the form "
        + LABEL_BORDER_VALUE + LABEL_TYPE_PREFACE + "[TYPE]" + LABEL_TYPE_NAME_SEPARATOR + "[NAME]" + LABEL_BORDER_VALUE + " where:\n"
        + "- [TYPE] is a pre-defined string, with possible values listed in the documentation.\n"
        + "- Case-insensitivity of the [TYPE] and it's preface is allowed, but discouraged by convention.\n"
        + "- [NAME] may only contain letters (a-z, A-Z), numbers (0-9), or underscores ( _ ). Names cannot be blank.";    
    public const string VARIABLE_VALUE_RULES      = "A Variable's '" + PROPERTY_NAME_VALUE + "' Property can be defined in the following ways:\n"
        + "- String: This is recommended by convention.\n"
        + "- Number: An integer or floating-point\n"
        + "- Boolean: Either 'true' or 'false' (case sensitive)\n"
        + "- Json lists and objects cannot be converted to strings and will cause an error if used.\n"
        + "- A null, empty or missing '" + PROPERTY_NAME_VALUE + "' field is not allowed.";

    public const string TOGGLEABLE_VALUE_RULES    = "A Toggleable's '" + PROPERTY_NAME_VALUE + "' Property can be defined in the following ways:\n"
        + "- Boolean: Either 'true' or 'false' (case sensitive). For all intents and purposes, this is what you should be using.\n"
        + "- String: Any string that can be parsed as 'true' or 'false' is accepted.\n"
        + "- Number: Use '1' for 'true' and '0' for 'false'. Other values give unpredictable results. Not recommended.\n"
        + "- Json lists and objects cannot be converted to strings and will cause an error if used.\n"
        + "- A null, empty or missing '" + PROPERTY_NAME_VALUE + "' field is not allowed.";

    public const string PROPERTY_LOCATIONS_RULES = "An Option's '" + PROPERTY_NAME_LOCATIONS + "' array must obey the following rules:\n"
        + "- A Json list is the only acceptable way to define this property.\n"
        + "- Each entry in the list should be a string, or errors will result when parsing it, or when searching for labels to replace with data.\n"
        + "- Each string should represent a filepath to a supported filetype with the option's label inside of it, starting from the .resources file. "
            + "Example: gameresources_patch1/generated/decls/some/path/to/a/supported/filetype.decl\n"
        + "- The list may be empty.\n"
        + "- If '" + PROPERTY_NAME_LOCATIONS + "' is null, undefined, or missing entirely, then every file of all supported filetypes in your mod will be checked "
            + "for labels, which may have a noticeable effect on execution time if your mod has lots of files that don't need to be configured.";
}

