using System.Text;
class LogMaker
{
    public bool mustLog {get; private set;}
    protected StringBuilder msg = new StringBuilder();

    public LogMaker(LogLevel requiredLogLevel)
    {
        LogLevel logMode = RuntimeConfig.logMode;
        mustLog = logMode == requiredLogLevel || logMode == LogLevel.VERBOSE;
    }

    public void appendString(string appendString)
    {
        msg.Append(appendString);
    }

    public void log()
    {
        Console.WriteLine(MSG_LOG + msg.ToString());
    }

    public void reportWarning(string message)
    {
        Console.WriteLine(MSG_WARNING + msg);
    }
}

class ParserLogMaker : LogMaker
{
    public ParserLogMaker() : base(LogLevel.PARSINGS) {}

    public void startNewFileLog(string path)
    {
        msg.Clear();
        msg.Append("Parsing File '" + path + "'");
    }

    public void appendLabelResult(Label l)
    {
        msg.Append("\n - Label '" + l.raw + "' resolved to '" + l.result + "'");
    }
}

class PropagationLogMaker : LogMaker
{
    public PropagationLogMaker() : base(LogLevel.PROPAGATIONS) {}

    public void startNewPropagationLog(string listName)
    {
        msg.Append("Propagating to '" + listName + "'");
    }

    public void appendFileCopyResult(string fileName)
    {
        msg.Append("\n - Created file '" + fileName + "'");
    }

    public void appendFolderCopyResult(string folderName)
    {
        msg.Append("\n - Created folder '" + folderName + "'");
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

class ModBuilderLogMaker : LogMaker
{
    public ModBuilderLogMaker() : base(LogLevel.MINIMAL){}

    public void logWarningNoPropLists()
    {
        reportWarning(
            "The '" + DIR_PROPAGATE + "' directory exists in your mod, but no"
            + " propagation list are defined. Propagation will not occur."
        );
    }

    public void logWarningNoPropFolder()
    {
        reportWarning(
            "You have propagation lists, but no '" + DIR_PROPAGATE
                + "' directory in your mod. Propagation will not occur."
        );
    }
}