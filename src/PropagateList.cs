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
            
            if(!DirUtil.isParentDir(DIR_PROPAGATE, copyFrom))
            {
                ThrowError(PATH_OUTSIDE_PROPAGATE, path);
                continue;
            }
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
                ThrowError(PROPAGATE_PATH_NOT_FOUND, path);
        }
    }

    public enum Error
    {
        PATH_OUTSIDE_PROPAGATE,
        PROPAGATE_PATH_NOT_FOUND
    }

    private void ThrowError(Error error, string arg0 = "")
    {
        switch(error)
        {
            case PATH_OUTSIDE_PROPAGATE:
            reportWarning(String.Format(
                "The path '{0}' in propagation list '{1}' points outside of"
                + " '{2}'. This path will be ignored.",
                arg0,
                name,
                DIR_PROPAGATE
            ));
            break;

            case PROPAGATE_PATH_NOT_FOUND:
            reportWarning(String.Format(
                "The path '{0}' in propagation list '{1}' does not exist in"
                + " '{2}'. This path will be ignored.",
                arg0, // The filepath
                name,
                DIR_PROPAGATE
            ));
            break;
        }

    }
}