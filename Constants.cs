class Constants
{
    public const string LABEL_BORDER_VALUE        = "%";
    public const string LABEL_TYPE_PREFACE        = "_INJECTOR_";
    public const string LABEL_TYPE_NAME_SEPARATOR = "$";
    public const string TYPE_STRING_VARIABLE      = "VARIABLE_";
    public const string TYPE_TOGGLEABLE           = "TOGGLEABLE_";
    public const string TYPE_TOGGLEABLE_END       = "TOGGLEABLE_END_";
    public const string LABEL_FORMATTING_RULES    = "Labels must have the form "
        + LABEL_BORDER_VALUE + LABEL_TYPE_PREFACE + "[TYPE]" + LABEL_TYPE_NAME_SEPARATOR + "[NAME]" + LABEL_BORDER_VALUE + " where:\n"
        + "- [TYPE] is a pre-defined string, with possible values listed in the documentation.\n"
        + "- Case-insensitivity of the [TYPE] and it's preface is allowed, but discouraged by convention.\n"
        + "- [NAME] may only contain letters (a-z, A-Z), numbers (0-9), or underscores ( _ ). Names cannot be blank.\n";
}

