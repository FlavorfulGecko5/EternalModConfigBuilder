using System.Text;
class LogMaker
{
    public bool mustLog {get; private set;}
    protected StringBuilder logMsg;

    public LogMaker(LogLevel requiredLogLevel)
    {
        mustLog = EternalModBuilder.mustLog(requiredLogLevel);
        logMsg = new StringBuilder();
    }

    public string getMessage()
    {
        return logMsg.ToString();
    }
}