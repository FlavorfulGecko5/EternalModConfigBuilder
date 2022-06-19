global using static Constants;
global using static ErrorReporter;
global using static Util;
class RuntimeManager
{
    static void Main(string[] args)
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

        ArgContainer validArgs = new ArgContainer(args);
        ParsedConfig config = new ParsedConfig(validArgs.configPath);

        //System.Console.WriteLine(config.ToString());

        ModBuilder builder = new ModBuilder(config, validArgs);
        builder.buildMod();
        
        System.Console.WriteLine(MESSAGE_SUCCESS);
    }
}