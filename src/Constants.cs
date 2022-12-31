interface Constants
{
    // Constants pertaining to directories
    
    

    // Constants pertaining to Label and expression parsing
    const int EXP_INFINITE_LOOP_THRESHOLD = 500;
    const string NULL_EXP_RESULT = "NULL";

    // Enums and Constants pertaining to labels
    const string LABEL_CHAR_LOOP_SEPARATOR = "&";



    const string RULES_TOGGLE_RESULT = "Expressions in toggle labels must yield one of these results:\n"
    + "- A Boolean (true/false) value, from a logical expression or from reading a string.\n"
    + "- A numerical value. A number less than one is interpeted as false, and one or higher is interpreted as true.";


}