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
}

