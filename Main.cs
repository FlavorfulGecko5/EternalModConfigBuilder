global using static Constants;
global using static ErrorCode;
global using static ErrorReporter;
global using static Util;
class EternalModConfiguration
{
    static void Main(string[] args)
    {
        try
        {
            run(args);
        }
        catch (Exception e) 
        {
            ProcessErrorCode(UNKNOWN_ERROR, e.ToString());
        }  
    }

    private static void run(string[] args)
    {
        if (Directory.Exists(TEMP_DIRECTORY))
            Directory.Delete(TEMP_DIRECTORY, true);

        ArgContainer validArgs = new ArgContainer(args);
        ParsedConfig config = new ParsedConfig(validArgs.configPath);

        //System.Console.WriteLine(config.ToString());

        ModBuilder builder = new ModBuilder(config, validArgs);
        builder.buildMod();
        
        System.Console.WriteLine(MESSAGE_SUCCESS);
    }
}