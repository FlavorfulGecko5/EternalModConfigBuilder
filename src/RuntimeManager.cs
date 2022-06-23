global using static RuntimeManager;
global using static Constants;
class RuntimeManager
{
    public static void Main(string[] args)
    {
        try
        {
            run(args);
        }
        catch (Exception e) 
        {
            reportUnknownError(e);
        }  
    }

    private static void run(string[] args)
    {
        if (Directory.Exists(DIRECTORY_TEMP))
            Directory.Delete(DIRECTORY_TEMP, true);

        ModBuilder builder = new ModBuilder(args);
        builder.buildMod();
        
        System.Console.WriteLine(MESSAGE_SUCCESS);
    }

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