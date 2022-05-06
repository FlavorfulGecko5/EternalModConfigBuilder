class ParsedConfig
{
    private bool mustCheckAllDecls;
    private List<string> filePaths {get;}
    private List<Option> configOptions {get;}

    public ParsedConfig(List<string> filePathsParameter, List<Option> configOptionsParameter, bool mustCheckAllDeclsParameter)
    {
        filePaths = filePathsParameter;
        configOptions = configOptionsParameter;
        mustCheckAllDecls = mustCheckAllDeclsParameter;
    }

    public override string ToString()
    {
        string formattedString = "ParsedConfig Object Data:\n"
        + "mustCheckAllDecls: " + mustCheckAllDecls + "\n"
        + "Files to Check:\n----------\n";

        for(int i = 0; i < filePaths.Count; i++)
            formattedString += filePaths[i] + "\n";
        
        formattedString += "\nOptions\n----------\n";
        for(int i = 0; i < configOptions.Count; i++)
            formattedString += configOptions[i].ToString();

        return formattedString;
    }

    public void buildMod(string inputDirectory, string outputDirectory)
    {
        
    }
}