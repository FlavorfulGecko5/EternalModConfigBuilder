using static PropagateList.Error;
using static RuntimeConfig.LogLevel;
using System.Text;
class PropagateList
{
    private StringBuilder log;
    public string name {get;}
    public string[] filePaths {get;}

    public PropagateList(string nameParameter, string[] filePathsParameter)
    {
        name = nameParameter;
        filePaths = filePathsParameter;
        log = new StringBuilder();
        if(RuntimeConfig.logMode == PROPAGATIONS || RuntimeConfig.logMode == VERBOSE)
            log.Append("Propagating to '" + name + "'");    
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
        foreach(string path in filePaths)
        {
            string  copyFrom = Path.Combine(DIR_PROPAGATE, path),
                    copyTo = Path.Combine(name, path);
            
            if(File.Exists(copyFrom))
            {
                DirUtil.createDirectoryInFilePath(copyTo);
                File.Copy(copyFrom, copyTo, true);
                if (RuntimeConfig.logMode == PROPAGATIONS || RuntimeConfig.logMode == VERBOSE)
                    log.Append("\n   - Created file '" + copyTo + "'");
            }
            else if (Directory.Exists(copyFrom))
            {
                Directory.CreateDirectory(copyTo);
                DirUtil.copyDirectory(copyFrom, copyTo);
                if (RuntimeConfig.logMode == PROPAGATIONS || RuntimeConfig.logMode == VERBOSE)
                    log.Append("\n   - Created folder '" + copyTo + "'");
            }    
            else
                EMBWarning(PROPAGATE_PATH_NOT_FOUND, path);
        }

        if (RuntimeConfig.logMode == PROPAGATIONS || RuntimeConfig.logMode == VERBOSE)
            RuntimeManager.log(log.ToString());
    }

    public enum Error
    {
        PROPAGATE_PATH_NOT_FOUND
    }

    private void EMBWarning(Error e, string arg0 = "")
    {
        string msg = "";
        string[] args = {"", "", ""};
        switch(e)
        {
            case PROPAGATE_PATH_NOT_FOUND:
            msg = "The path '{0}' in propagation list '{1}' does not exist in"
                    + " '{2}'. This path will be ignored.";
            args[0] = arg0; // The filepath
            args[1] = name;
            args[2] = DIR_PROPAGATE;
            break;
        }
        RuntimeManager.reportWarning(msg, args);
    }
}