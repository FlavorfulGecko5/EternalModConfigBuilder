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
        string formattedString = "   - Propagation List '" + name + "'";
        foreach(string file in filePaths)
            formattedString += "\n      -- '" + file + "'";
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
                DirUtil.createDirectoryInFilePath(copyTo);
                File.Copy(copyFrom, copyTo, true);
                if(logger.mustLog)
                    logger.appendFileCopyResult(copyTo);
            }
            else if (Directory.Exists(copyFrom))
            {
                Directory.CreateDirectory(copyTo);
                DirUtil.copyDirectory(copyFrom, copyTo);
                if(logger.mustLog)
                    logger.appendFolderCopyResult(copyTo);
            }    
            else
                logger.logWarningMissingFile(path, name);
        }
        if(logger.mustLog)
            logger.log();
    }
}