class ErrorReporter
{
    public static void reportError(string msg)
    {
        System.Console.WriteLine(MESSAGE_ERROR + msg);
        System.Console.WriteLine("\n" + MESSAGE_FAILURE);
        Environment.Exit(1);
    }

    public static void reportUnknownError(Exception e)
    {
        reportError(String.Format(
            "An unknown error occurred, printing Exception:\n\n{0}",
            e.ToString()
        ));   
    }

    public static void reportWarning(string msg)
    {
        System.Console.WriteLine(MESSAGE_WARNING + msg);
    }
}