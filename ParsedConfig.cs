using System.IO.Compression;
using System.Data;
using static System.StringComparison;
using static Constants;
using static ErrorCode;
using static ErrorReporter;
class ParsedConfig
{
    private bool mustCheckAllFiles;
    private List<string> filePaths { get; }
    private List<Option> configOptions { get; }
    private DataTable expressionEvaluator;
    public ParsedConfig(List<string> filePathsParameter, List<Option> configOptionsParameter, bool mustCheckAllFilesParameter)
    {
        filePaths = filePathsParameter;
        configOptions = configOptionsParameter;
        mustCheckAllFiles = mustCheckAllFilesParameter;
        expressionEvaluator = new DataTable();
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
        // If the output is a zip file, we should use a temporary directory 
        // instead of assuming a directory of the same w/o the extension is available to use
        string activeOutputDirectory = outputToZip ? TEMPORARY_DIRECTORY : outputDirectory,
               workingDirectory = Directory.GetCurrentDirectory(), 
               currentFilePath = "";
        try
        {
            // Clone the contents of inputDirectory to the active outputDirectory
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
                    if (SUPPORTED_FILETYPES.Contains(currentFilePath.Substring(j, currentFilePath.Length - j)))
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
                        // Need this here to prevent unwanted shutdowns
                        ProcessErrorCode(MOD_FILE_NOT_FOUND, new string[]{currentFilePath});
                }
            
            if(outputToZip)
            {
                Directory.SetCurrentDirectory(workingDirectory);
                if(File.Exists(outputDirectory))
                    File.Delete(outputDirectory);
                ZipFile.CreateFromDirectory(TEMPORARY_DIRECTORY, outputDirectory);
                Directory.Delete(TEMPORARY_DIRECTORY, true);
            }
        }
        catch (System.IO.DirectoryNotFoundException)
        {ProcessErrorCode(MOD_DIRECTORY_NOT_FOUND, new string[]{ inputDirectory });}
        catch (Exception e)
        {ProcessErrorCode(UNKNOWN_ERROR,           new string[]{ e.ToString()   });}
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

    private void parseFile(string filePath)
    {
        // The full text of the file
        string fileText = "",
        // The label we're currently parsing
               label = "",
        // The expression in the current label
               expression = "",
               expressionResult = "";
        // The start, end and separator indices of the label we're currently parsing
        int labelStartIndex = 0, labelEndIndex = 0, expressionSeparatorIndex = 0;

        using(StreamReader fileReader = new StreamReader(filePath))
        {
            // Labels are parsed sequentially by scanning the entire text file.
            fileText = fileReader.ReadToEnd();
            labelStartIndex = fileText.IndexOf(LABEL_ANY, CurrentCultureIgnoreCase);

            while(labelStartIndex != -1)
            {
                // Get the complete label by utilizing the border characters.
                labelEndIndex = fileText.IndexOf(LABEL_BORDER_VALUE, labelStartIndex + 1);
                if(labelEndIndex == -1)
                    goto CATCH_INCOMPLETE_LABEL;
                label = fileText.Substring(labelStartIndex, labelEndIndex - labelStartIndex + 1);

                // Isolate the expression, if it exists
                expressionSeparatorIndex = label.IndexOf(LABEL_NAME_EXP_SEPARATOR);
                if(expressionSeparatorIndex == -1)
                    goto CATCH_INCOMPLETE_LABEL; // NEED A NEW ERRORCODE FOR THIS

                // Don't uppercase the expression to prevent string-literals from being modified
                expression = label.Substring(expressionSeparatorIndex + 1, label.Length - expressionSeparatorIndex - 2);
                expressionResult = parseExpression(expression);

                // Everything up to and excluding the separator index
                label = label.Substring(0, expressionSeparatorIndex).ToUpper();

                // Check if the label is valid - default if it isn't.
                switch (label)
                {
                    case LABEL_ANY_VARIABLE:
                        fileText = fileText.Substring(0, labelStartIndex) // Everything before the label
                            // The value we're substituting for the label
                            + expressionResult
                            // Everything after the label
                            + fileText.Substring(labelEndIndex + 1, fileText.Length - labelEndIndex - 1);
                        break;
                    case LABEL_ANY_TOGGLEABLE:
                        fileText = parseToggle(filePath, fileText, label, expressionResult, labelStartIndex, labelEndIndex);
                        break;

                    default:
                        // Edge Case: A toggleable-end label with no toggleable-beginning label preceding it
                        if (label.Equals(LABEL_END_TOGGLEABLE))
                            goto CATCH_EXTRA_END_TOGGLE;
                        else
                            goto CATCH_UNRECOGNIZED_TYPE;
                }
                // Starts search from the location of the previous label.
                labelStartIndex = fileText.IndexOf(LABEL_ANY, labelStartIndex, CurrentCultureIgnoreCase);
            }
        }
        using(StreamWriter fileWriter = new StreamWriter(filePath))
        {
            fileWriter.Write(fileText);
        }
        return;

        CATCH_INCOMPLETE_LABEL:
        ProcessErrorCode(INCOMPLETE_LABEL, new string[]{ filePath });
        CATCH_UNRECOGNIZED_TYPE:
        ProcessErrorCode(UNRECOGNIZED_TYPE, new string[]{ filePath, label });
        CATCH_EXTRA_END_TOGGLE:
        ProcessErrorCode(EXTRA_END_TOGGLE, new string[]{ filePath });
    }

    private string parseExpression(string originalExpression)
    {
        int numIterations = 0;
        bool replacedThisCycle = true;
        string expression = originalExpression, currentSearchString = "";
        while(replacedThisCycle && numIterations++ < INFINITE_LOOP_THRESHOLD)
        {
            replacedThisCycle = false;
            for (int i = 0; i < configOptions.Count; i++)
            {
                currentSearchString = '{' + configOptions[i].name + '}';
                if(expression.IndexOf(currentSearchString, CurrentCultureIgnoreCase) != -1)
                {
                    replacedThisCycle = true;
                    expression = expression.Replace(currentSearchString, configOptions[i].value, CurrentCultureIgnoreCase);
                }
            }    
        }
        string? result = expressionEvaluator.Compute(expression, "").ToString();
        if(result != null)
            return result;
        else
            return "null"; 
    }

    private string parseToggle(string filePath, string fileText, string label, string expressionResult, int startLabelStartIndex, int startLabelEndIndex)
    {
        int numEndLabelsNeeded = 1;

        int endLabelStartIndex = startLabelStartIndex, endLabelEndIndex = 0;
        while(numEndLabelsNeeded > 0)
        {
            endLabelStartIndex = fileText.IndexOf(LABEL_ANY_TOGGLEABLE, endLabelStartIndex + 1, CurrentCultureIgnoreCase);
            if(endLabelStartIndex == -1)
                goto CATCH_MISSING_END_TOGGLE;
            
            if(fileText.IndexOf(LABEL_END_TOGGLEABLE, endLabelStartIndex, CurrentCultureIgnoreCase) == endLabelStartIndex)
                numEndLabelsNeeded--;
            // No need to go too in-depth with error checking here, since one way or another invalid labels will get detected in parseFile
            else
            {
                // Just check if the toggle-non-end label we found is actually valid.
                // This way, all labels are validated even if the toggle is false
                endLabelEndIndex = fileText.IndexOf(LABEL_BORDER_VALUE, endLabelStartIndex + 1);
                if(endLabelEndIndex == -1)
                    goto CATCH_INCOMPLETE_LABEL;
                numEndLabelsNeeded++;
            }
                
        }
        endLabelEndIndex = fileText.IndexOf(LABEL_BORDER_VALUE, endLabelStartIndex + 1);

        // TODO: FAILED CONVERSIONS, NUMERICAL RESULTS
        bool toggleStatus = Convert.ToBoolean(expressionResult);
        if(toggleStatus)
            return fileText.Substring(0, startLabelStartIndex) // Everything before the start label
                + fileText.Substring(startLabelEndIndex + 1, endLabelStartIndex - startLabelEndIndex - 1) // Everything in-between the two labels
                + fileText.Substring(endLabelEndIndex + 1, fileText.Length - endLabelEndIndex - 1); // Everything after the end label
        else
            return fileText.Substring(0, startLabelStartIndex) // Everything before the start label
                + fileText.Substring(endLabelEndIndex + 1, fileText.Length - endLabelEndIndex - 1); // Everthing after the end label
        
        CATCH_MISSING_END_TOGGLE:
        ProcessErrorCode(MISSING_END_TOGGLE, new string[]{ filePath, label });
        CATCH_INCOMPLETE_LABEL:
        ProcessErrorCode(INCOMPLETE_LABEL, new string[]{ filePath });

        // This return statement will never execute
        return "";
    }
}