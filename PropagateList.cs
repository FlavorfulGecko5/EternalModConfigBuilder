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
            string  copyFrom = Path.Combine(PROPAGATE_DIRECTORY, path),
                    copyTo = Path.Combine(name, path);
            // Null values should be impossible
            string? directory = Path.GetDirectoryName(path);
            if(File.Exists(copyFrom) && directory != null)
            {
                Directory.CreateDirectory(Path.Combine(name, directory));
                File.Copy(copyFrom, copyTo, true);
            }
            else if (Directory.Exists(copyFrom))
            {
                Directory.CreateDirectory(copyTo);
                CopyDir(new DirectoryInfo(copyFrom), new DirectoryInfo(copyTo));
            }    
            else
                ProcessErrorCode(PROPAGATE_PATH_NOT_FOUND, path, name);
        }
    }
}