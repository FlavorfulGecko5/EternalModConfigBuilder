using static PropagateList.Error;
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
        string formattedString = "Propagation List '" + name + "'\n";
        foreach(string file in filePaths)
            formattedString += "\t'" + file + "'\n";
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
            }
            else if (Directory.Exists(copyFrom))
            {
                Directory.CreateDirectory(copyTo);
                DirUtil.copyDirectory(copyFrom, copyTo);
            }    
            else
                EMBWarning(PROPAGATE_PATH_NOT_FOUND, path);
        }
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