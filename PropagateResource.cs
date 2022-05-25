using static Constants;
class PropagateResource
{
    public string name {get;}
    public string[] filePaths {get;}

    public PropagateResource(string nameParamter, string[] filePathsParameter)
    {
        name = nameParamter;
        filePaths = filePathsParameter;
    }

    public override string ToString()
    {
        string formattedString = "PropagateResource object with resource name '" + name + "' contains the following filepaths: \n";
        for(int i = 0; i < filePaths.Length; i++)
            formattedString += filePaths[i] + '\n';
        formattedString += "-----\n";

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