class ParsedConfig
{
    private bool mustCheckAllFiles;
    private List<string> filePaths { get; }
    private List<Option> configOptions { get; }

    public ParsedConfig(List<string> filePathsParameter, List<Option> configOptionsParameter, bool mustCheckAllDeclsParameter)
    {
        filePaths = filePathsParameter;
        configOptions = configOptionsParameter;
        mustCheckAllFiles = mustCheckAllDeclsParameter;
    }

    public override string ToString()
    {
        string formattedString = "ParsedConfig Object Data:\n"
        + "mustCheckAllDecls: " + mustCheckAllFiles + "\n"
        + "Files to Check:\n----------\n";

        for (int i = 0; i < filePaths.Count; i++)
            formattedString += filePaths[i] + "\n";

        formattedString += "\nOptions\n----------\n";
        for (int i = 0; i < configOptions.Count; i++)
            formattedString += configOptions[i].ToString();

        return formattedString;
    }

    public void buildMod(string inputDirectory, string outputDirectory)
    {
        // Clone the contents of inputDirectory to the outputDirectory
        if(!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);
        CopyFilesRecursively(new DirectoryInfo(inputDirectory), new DirectoryInfo(outputDirectory));

        // Begin working with the newly copied files
        Directory.SetCurrentDirectory(outputDirectory);
        string[] allFiles = Directory.GetFiles(".", "*.*", SearchOption.AllDirectories);

        string currentFilePath = "";
        if(mustCheckAllFiles)
            for(int i = 0, j = 0; i < allFiles.Length; i++)
            {
                currentFilePath = allFiles[i];

                // Check the file extension to ensure we can parse this file
                j = currentFilePath.LastIndexOf('.') + 1;
                if(Constants.SUPPORTED_FILETYPES.Contains(currentFilePath.Substring(j, currentFilePath.Length - j)))
                    parseFile(currentFilePath);
            }
        else
            for(int i = 0; i < filePaths.Count; i++)
                parseFile(filePaths[i]);
    }

    private void parseFile(string filePath)
    {
        string fileText = "";
        using(StreamReader fileReader = new StreamReader(filePath))
        {
            fileText = fileReader.ReadToEnd();
            string currentLabel = "";
            int nextLabelStartIndex = fileText.IndexOf(Constants.LABEL_BORDER_VALUE + Constants.LABEL_TYPE_PREFACE, StringComparison.CurrentCultureIgnoreCase),
                nextLabelEndIndex = 0;

            while(nextLabelStartIndex != -1)
            {
                nextLabelEndIndex = fileText.IndexOf(Constants.LABEL_BORDER_VALUE, nextLabelStartIndex + 1);
                currentLabel = fileText.Substring(nextLabelStartIndex, nextLabelEndIndex - nextLabelStartIndex + 1).ToUpper();

                for(int i = 0; i < configOptions.Count; i++)
                    if(configOptions[i].label.Equals(currentLabel))
                    {
                        switch(configOptions[i])
                        {
                            case VariableOption v:
                                fileText = fileText.Substring(0, nextLabelStartIndex) // Everything before the label
                                            + v.value  // The value we're substituting for the label
                                            + fileText.Substring(nextLabelEndIndex + 1, fileText.Length - nextLabelEndIndex - 1); // Everything after the label
                                break;
                            case ToggleOption t:
                                fileText = parseToggle(fileText, t.value, nextLabelStartIndex, nextLabelEndIndex);
                                break;
                        }
                        break;
                    }
                nextLabelStartIndex = fileText.IndexOf(Constants.LABEL_BORDER_VALUE + Constants.LABEL_TYPE_PREFACE, StringComparison.CurrentCultureIgnoreCase);
            }
        }
        using(StreamWriter fileWriter = new StreamWriter(filePath))
        {
            fileWriter.Write(fileText);
        }          
    }

    private string parseToggle(string fileText, bool optionValue, int startLabelStartIndex, int startLabelEndIndex)
    {
        string toggleLabel = Constants.LABEL_BORDER_VALUE + Constants.LABEL_TYPE_PREFACE + Constants.TYPE_TOGGLEABLE + Constants.LABEL_TYPE_NAME_SEPARATOR,
               toggleEndLabel = Constants.LABEL_BORDER_VALUE + Constants.LABEL_TYPE_PREFACE + Constants.TYPE_TOGGLEABLE_END + Constants.LABEL_BORDER_VALUE,
               toggleGeneralLabel = Constants.LABEL_BORDER_VALUE + Constants.LABEL_TYPE_PREFACE + Constants.TYPE_TOGGLEABLE;
        int numEndLabelsNeeded = 1;

        int endLabelStartIndex = startLabelStartIndex, endLabelEndIndex = 0;
        while(numEndLabelsNeeded > 0)
        {
            endLabelStartIndex = fileText.IndexOf(toggleGeneralLabel, endLabelStartIndex + 1, StringComparison.CurrentCultureIgnoreCase);
            if(endLabelStartIndex == -1)
                ; // Throw an error - there are more toggle start labels than there are end labels
            
            if(fileText.IndexOf(toggleEndLabel, endLabelStartIndex, StringComparison.CurrentCultureIgnoreCase) == endLabelStartIndex)
                numEndLabelsNeeded--;
            else
                numEndLabelsNeeded++;
        }
        endLabelEndIndex = fileText.IndexOf(Constants.LABEL_BORDER_VALUE, endLabelStartIndex + 1);

        if(optionValue)
            return fileText.Substring(0, startLabelStartIndex) // Everything before the start label
                + fileText.Substring(startLabelEndIndex + 1, endLabelStartIndex - startLabelEndIndex - 1) // Everything in-between the two labels
                + fileText.Substring(endLabelEndIndex + 1, fileText.Length - endLabelEndIndex - 1); // Everything after the end label
        else
            return fileText.Substring(0, startLabelStartIndex) // Everything before the start label
                + fileText.Substring(endLabelEndIndex + 1, fileText.Length - endLabelEndIndex - 1); // Everthing after the end label
    }

    // Used to copy the mod folder to the output directory.
    // If any folders in the output directory do not exist, they will be created.
    // If any files that are to be copied already exist, they will be overwritten.
    private static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
    {
        foreach (DirectoryInfo dir in source.GetDirectories())
            CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
        foreach (FileInfo file in source.GetFiles())
            file.CopyTo(Path.Combine(target.FullName, file.Name), true);
    }
}