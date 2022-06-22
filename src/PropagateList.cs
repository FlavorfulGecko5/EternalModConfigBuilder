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
            string  copyFrom = Path.Combine(DIRECTORY_PROPAGATE, path),
                    copyTo = Path.Combine(name, path);
            // Null values should be impossible
            string? directory = Path.GetDirectoryName(path);
            if(File.Exists(copyFrom) && directory != null)
            {
                if(!directory.Equals(""))
                    Directory.CreateDirectory(Path.Combine(name, directory));
                File.Copy(copyFrom, copyTo, true);
            }
            else if (Directory.Exists(copyFrom))
            {
                Directory.CreateDirectory(copyTo);
                ExtUtil.CopyDir(new DirectoryInfo(copyFrom), new DirectoryInfo(copyTo));
            }    
            else
                ThrowError(PROPAGATE_PATH_NOT_FOUND, path);
        }
    }

    public enum Error
    {
        PROPAGATE_PATH_NOT_FOUND
    }

    private void ThrowError(Error error, string arg0 = "")
    {
        switch(error)
        {
            case PROPAGATE_PATH_NOT_FOUND:
            reportWarning(String.Format(
                "The path '{0}' in propagation list '{1}' does not exist in"
                + " '{2}'. This path will be ignored.",
                arg0, // The filepath
                name,
                DIRECTORY_PROPAGATE
            ));
            break;
        }

    }
}