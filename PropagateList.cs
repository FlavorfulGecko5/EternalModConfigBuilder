using static Constants;
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
        string? currentFilePath = "";
        foreach(string file in filePaths)
        {
            if(File.Exists(Path.Combine(PROPAGATE_DIRECTORY, file)))
            {
                currentFilePath = Path.GetDirectoryName(file);
                // Since the file exists, there should be no conditions where this value is null
                if(currentFilePath != null)    
                {
                    Directory.CreateDirectory(Path.Combine(name, currentFilePath));
                    File.Copy(Path.Combine(PROPAGATE_DIRECTORY, file), Path.Combine(name, file), true);
                }
            }
        }
    }
}