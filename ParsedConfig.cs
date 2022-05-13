using System.IO.Compression;
class ParsedConfig
{
    private bool mustCheckAllFiles;
    private List<string> filePaths { get; }
    private List<Option> configOptions { get; }

    public ParsedConfig(List<string> filePathsParameter, List<Option> configOptionsParameter, bool mustCheckAllFilesParameter)
    {
        filePaths = filePathsParameter;
        configOptions = configOptionsParameter;
        mustCheckAllFiles = mustCheckAllFilesParameter;
    }

    public Option? hasOption(string label)
    {
        for(int i = 0; i < configOptions.Count; i++)
        {
            if(configOptions[i].label.Equals(label))
                return configOptions[i];
        }
        return null;
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

    public void buildMod(string inputDirectory, bool inputIsZip, string outputDirectory, bool outputToZip)
    {
        string tempDirectory = "TEMP_DIRECTORY_ETERNAL_MOD_CONFIGURATOR",
               activeOutputDirectory = outputToZip ? tempDirectory : outputDirectory,
               workingDirectory = Directory.GetCurrentDirectory(), 
               currentFilePath = "";
        try
        {
            // Clone the contents of inputDirectory to the outputDirectory
            if (!Directory.Exists(activeOutputDirectory))
                Directory.CreateDirectory(activeOutputDirectory);

            if(inputIsZip)
                ZipFile.ExtractToDirectory(inputDirectory, activeOutputDirectory);
            else
                CopyFilesRecursively(new DirectoryInfo(inputDirectory), new DirectoryInfo(activeOutputDirectory));

            // Begin working with the newly copied files
            Directory.SetCurrentDirectory(activeOutputDirectory);
            string[] allFiles = Directory.GetFiles(".", "*.*", SearchOption.AllDirectories);

            if (mustCheckAllFiles)
                for (int i = 0, j = 0; i < allFiles.Length; i++)
                {
                    currentFilePath = allFiles[i];

                    // Check the file extension to ensure we can parse this file
                    j = currentFilePath.LastIndexOf('.') + 1;
                    if (Constants.SUPPORTED_FILETYPES.Contains(currentFilePath.Substring(j, currentFilePath.Length - j)))
                        parseFile(currentFilePath);
                }
            else
                // All files in filepaths have already had their extensions validated
                for (int i = 0; i < filePaths.Count; i++)
                {
                    currentFilePath = filePaths[i];

                    // Verify the file actually exists in the mod
                    if(File.Exists(currentFilePath))
                        parseFile(currentFilePath);
                    else
                        goto CATCH_FILE_NOT_FOUND;
                }
            
            if(outputToZip)
            {
                Directory.SetCurrentDirectory(workingDirectory);
                if(File.Exists(outputDirectory))
                    File.Delete(outputDirectory);
                ZipFile.CreateFromDirectory(tempDirectory, outputDirectory);
                Directory.Delete(tempDirectory, true);
            }
            return;
        }
        catch (System.IO.DirectoryNotFoundException)
        {ErrorReporter.ProcessErrorCode(ErrorCode.MOD_DIRECTORY_NOT_FOUND, new string[]{ inputDirectory });}
        catch (Exception e)
        {ErrorReporter.ProcessErrorCode(ErrorCode.UNKNOWN_ERROR,           new string[]{ e.ToString()   });}
        CATCH_FILE_NOT_FOUND:
         ErrorReporter.ProcessErrorCode(ErrorCode.MOD_FILE_NOT_FOUND,      new string[]{ currentFilePath});
    }

    private void parseFile(string filePath)
    {
        // The full text of the file
        string fileText = "",
        // The label we're currently parsing
               label = "",
        // The string we search the fileText for to find the next label
               searchString = Constants.LABEL_BORDER_VALUE + Constants.LABEL_TYPE_PREFACE;
        // The start and end indices of the label we're currently parsing
        int labelStartIndex = 0, labelEndIndex = 0;

        using(StreamReader fileReader = new StreamReader(filePath))
        {
            // Labels are parsed sequentially by scanning the entire text file.
            fileText = fileReader.ReadToEnd();
            labelStartIndex = fileText.IndexOf(searchString, StringComparison.CurrentCultureIgnoreCase);

            while(labelStartIndex != -1)
            {
                labelEndIndex = fileText.IndexOf(Constants.LABEL_BORDER_VALUE, labelStartIndex + 1);
                if(labelEndIndex == -1)
                    goto CATCH_INCOMPLETE_LABEL;
                label = fileText.Substring(labelStartIndex, labelEndIndex - labelStartIndex + 1).ToUpper();

                // Check if the label exists in the config file - null if it doesn't.
                switch (hasOption(label))
                {
                    case null:
                        // Edge: A toggleable-end label with no toggleable-beginning label preceding it
                        if(label.Equals(Constants.LABEL_BORDER_VALUE + Constants.LABEL_TYPE_PREFACE + Constants.TYPE_TOGGLEABLE_END + Constants.LABEL_BORDER_VALUE))
                            goto CATCH_EXTRA_END_TOGGLE;
                        else
                            goto CATCH_UNRECOGNIZED_LABEL;

                    case VariableOption v:
                        fileText = fileText.Substring(0, labelStartIndex) // Everything before the label
                            // The value we're substituting for the label
                            + v.value
                            // Everything after the label
                            + fileText.Substring(labelEndIndex + 1, fileText.Length - labelEndIndex - 1);
                        break;
                    case ToggleOption t:
                        fileText = parseToggle(filePath, fileText, label, t.value, labelStartIndex, labelEndIndex);
                        break;
                }
                // Starts search from the location of the previous label.
                labelStartIndex = fileText.IndexOf(searchString, labelStartIndex, StringComparison.CurrentCultureIgnoreCase);
            }
        }
        using(StreamWriter fileWriter = new StreamWriter(filePath))
        {
            fileWriter.Write(fileText);
        }
        return;

        CATCH_INCOMPLETE_LABEL:
        ErrorReporter.ProcessErrorCode(ErrorCode.INCOMPLETE_LABEL, new string[]{ filePath });
        CATCH_UNRECOGNIZED_LABEL:
        ErrorReporter.ProcessErrorCode(ErrorCode.UNRECOGNIZED_LABEL, new string[]{ filePath, label });
        CATCH_EXTRA_END_TOGGLE:
        ErrorReporter.ProcessErrorCode(ErrorCode.EXTRA_END_TOGGLE, new string[]{ filePath });
    }

    private string parseToggle(string filePath, string fileText, string label, bool optionValue, int startLabelStartIndex, int startLabelEndIndex)
    {
        string toggleEndLabel = Constants.LABEL_BORDER_VALUE + Constants.LABEL_TYPE_PREFACE + Constants.TYPE_TOGGLEABLE_END + Constants.LABEL_BORDER_VALUE,
            toggleGeneralLabel = Constants.LABEL_BORDER_VALUE + Constants.LABEL_TYPE_PREFACE + Constants.TYPE_TOGGLEABLE,
            currentSubLabel = "";
        int numEndLabelsNeeded = 1;

        int endLabelStartIndex = startLabelStartIndex, endLabelEndIndex = 0;
        while(numEndLabelsNeeded > 0)
        {
            endLabelStartIndex = fileText.IndexOf(toggleGeneralLabel, endLabelStartIndex + 1, StringComparison.CurrentCultureIgnoreCase);
            if(endLabelStartIndex == -1)
                goto CATCH_MISSING_END_TOGGLE;
            
            if(fileText.IndexOf(toggleEndLabel, endLabelStartIndex, StringComparison.CurrentCultureIgnoreCase) == endLabelStartIndex)
                numEndLabelsNeeded--;
            // No need to go too in-depth with error checking here, since one way or another invalid labels will get detected in parseFile
            else
            {
                // Just check if the toggle-non-end label we found is actually valid.
                // This way, all labels are validated even if the toggle is false
                endLabelEndIndex = fileText.IndexOf(Constants.LABEL_BORDER_VALUE, endLabelStartIndex + 1);
                if(endLabelEndIndex == -1)
                    goto CATCH_INCOMPLETE_LABEL;
                currentSubLabel = fileText.Substring(endLabelStartIndex, endLabelEndIndex - endLabelStartIndex + 1).ToUpper();

                if(hasOption(currentSubLabel) != null)
                    numEndLabelsNeeded++;
                else
                    goto CATCH_UNRECOGNIZED_LABEL;
            }
                
        }
        endLabelEndIndex = fileText.IndexOf(Constants.LABEL_BORDER_VALUE, endLabelStartIndex + 1);

        if(optionValue)
            return fileText.Substring(0, startLabelStartIndex) // Everything before the start label
                + fileText.Substring(startLabelEndIndex + 1, endLabelStartIndex - startLabelEndIndex - 1) // Everything in-between the two labels
                + fileText.Substring(endLabelEndIndex + 1, fileText.Length - endLabelEndIndex - 1); // Everything after the end label
        else
            return fileText.Substring(0, startLabelStartIndex) // Everything before the start label
                + fileText.Substring(endLabelEndIndex + 1, fileText.Length - endLabelEndIndex - 1); // Everthing after the end label
        
        CATCH_MISSING_END_TOGGLE:
        ErrorReporter.ProcessErrorCode(ErrorCode.MISSING_END_TOGGLE, new string[]{ filePath, label });
        CATCH_INCOMPLETE_LABEL:
        ErrorReporter.ProcessErrorCode(ErrorCode.INCOMPLETE_LABEL, new string[]{ filePath });
        CATCH_UNRECOGNIZED_LABEL:
        ErrorReporter.ProcessErrorCode(ErrorCode.UNRECOGNIZED_LABEL, new string[]{ filePath, currentSubLabel });

        // This return statement will never execute
        return "";
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