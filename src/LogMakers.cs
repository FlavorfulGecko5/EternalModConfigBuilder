using System.Text;
class LogMaker
{
    public bool mustLog {get; private set;}
    protected StringBuilder logMsg = new StringBuilder();

    public LogMaker(LogLevel requiredLogLevel)
    {
        LogLevel logMode = EternalModBuilder.logMode;
        mustLog = logMode == requiredLogLevel || logMode == LogLevel.VERBOSE;
    }

    public void appendString(string appendString)
    {
        logMsg.Append(appendString);
    }

    public void log()
    {
        Console.WriteLine(MSG_LOG + logMsg.ToString());
    }

    public static void reportWarning(string warningMessage)
    {
        Console.WriteLine(MSG_WARNING + warningMessage);
    }
}

class ParserLogMaker : LogMaker
{
    public ParserLogMaker() : base(LogLevel.PARSINGS) {}

    public void startNewFileLog(string path)
    {
        logMsg.Clear();
        logMsg.Append("Parsing File '" + path + "'");
    }

    public void appendLabelResult(Label l, string result)
    {
        logMsg.Append("\n - Label '" + l.raw + "' resolved to '" + result + "'");
    }
}

class PropagationLogMaker : LogMaker
{
    public PropagationLogMaker() : base(LogLevel.PROPAGATIONS) {}

    public void startNewPropagationLog(string listName)
    {
        logMsg.Append("Propagating to '" + listName + "'");
    }

    public void appendFileCopyResult(string fileName)
    {
        logMsg.Append("\n - Created file '" + fileName + "'");
    }

    public void appendFolderCopyResult(string folderName)
    {
        logMsg.Append("\n - Created folder '" + folderName + "'");
    }

    public void logWarningMissingFile(string path, string listName)
    {
        string warning = String.Format(
            "The path '{0}' in propagation list '{1}' does not exist in"
                + " '{2}'. This path will be ignored.",
            path,
            listName,
            DIR_PROPAGATE
        );
        reportWarning(warning);
    }
}