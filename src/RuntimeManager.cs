global using static Constants;
class RuntimeManager
{
    public static void Main(string[] args)
    {
        try
        {
            run(args);
        }
        catch (EMBException e)
        {
            reportError(e.Message);
        }
        catch (Exception e) 
        {
            reportUnknownError(e);
        }  
    }

    private static void run(string[] args)
    {
        System.Console.WriteLine(MSG_WELCOME);
        if (Directory.Exists(DIR_TEMP))
            Directory.Delete(DIR_TEMP, true);

        ModBuilder builder = new ModBuilder(args);
        builder.buildMod();
        
        System.Console.WriteLine(MSG_SUCCESS);
    }

    private static void reportError(string msg)
    {
        System.Console.WriteLine(MSG_ERROR + msg);
        System.Console.WriteLine("\n" + MSG_FAILURE);
        Environment.Exit(1);
    }

    private static void reportUnknownError(Exception e)
    {
        reportError(String.Format(
            "An unknown error occurred, printing Exception:\n\n{0}",
            e.ToString()
        ));
    }

    public static void reportWarning(string msg, string[] args)
    {
        System.Console.WriteLine(MSG_WARNING + msg, args);
    }
}