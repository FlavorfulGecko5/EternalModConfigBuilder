class PropagateList
{
    public string name {get;}
    public string[] filePaths {get;}

    public PropagateList(string nameParameter, string[] filePathsParameter)
    {
        name = nameParameter;
        filePaths = filePathsParameter;
    }

    public override string ToString()
    {
        string formattedString = " - Propagation List '" + name + "'";
        foreach(string file in filePaths)
            formattedString += "\n  - '" + file + "'";
        return formattedString;
    }

    public void propagate()
    {
        PropagationLogMaker logger = new PropagationLogMaker();
        if(logger.mustLog)
            logger.startNewPropagationLog(name);
        foreach(string path in filePaths)
        {
            string  copyFrom = Path.Combine(DIR_PROPAGATE, path),
                    copyTo = Path.Combine(name, path);
            
            if(File.Exists(copyFrom))
            {
                FSUtil.createDirectoryInFilePath(copyTo);
                File.Copy(copyFrom, copyTo, true);
                if(logger.mustLog)
                    logger.appendFileCopyResult(copyTo);
            }
            else if (Directory.Exists(copyFrom))
            {
                Directory.CreateDirectory(copyTo);
                FSUtil.copyDirectory(copyFrom, copyTo);
                if(logger.mustLog)
                    logger.appendFolderCopyResult(copyTo);
            }    
            else
                logger.logWarningMissingFile(path, name);
        }
        if(logger.mustLog)
            EternalModBuilder.log(logger.getMessage());
    }

    private class PropagationLogMaker : LogMaker
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
            EternalModBuilder.reportWarning(warning);
        }
    }
}