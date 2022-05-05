class Constants
{
    public const char   LABEL_BORDER_VALUE = '%';
    public const char   LABEL_TYPE_NAME_SEPARATOR = '$';
    public const string LABEL_STRING_VARIABLE     = "%_INJECTOR_VARIABLE_";
    public const string LABEL_TOGGLEABLE_START    = "%_INJECTOR_TOGGLEABLE_";
    public const string LABEL_TOGGLEABLE_END      = "%_INJECTOR_TOGGLEABLE_END_%";
    public const string LABEL_FORMATTING_RULES    = "Labels must have the form %cType%cName";

            // A label must have the form %Type$Name%, where:
        // - The types are explicitly defined. Case-insensitivity is allowed but discouraged by convention.
        // - The name may only have letters (a-z, A-Z), numbers (0-9) and underscores (_)
}

