using System.IO.Compression;
using System.Data;
using static System.StringComparison;
using static Constants;
using static ErrorCode;
using static ErrorReporter;
using static Util;
class ParsedConfig
{
    private bool mustCheckAllFiles;
    private List<string> filePaths { get; }
    private List<Option> configOptions { get; }
    private List<PropagateList> propagationLists {get;}
    private DataTable expressionEvaluator;

    public ParsedConfig(List<string> filePathsParameter, List<Option> configOptionsParameter, 
        List<PropagateList> propagationListsParameter, bool mustCheckAllFilesParameter)
    {
        filePaths = filePathsParameter;
        configOptions = configOptionsParameter;
        propagationLists = propagationListsParameter;
        mustCheckAllFiles = mustCheckAllFilesParameter;
        expressionEvaluator = new DataTable();
    }

    public override string ToString()
    {
        string formattedString = "**********\nParsedConfig Object Data:\n==========\n"
        + "mustCheckAllDecls: " + mustCheckAllFiles + "\n==========\n"
        + "Files to Check:\n";

        foreach(string file in filePaths)
            formattedString += '\'' + file + "'\n";

        formattedString += "==========\nOptions\n";
        foreach(Option option in configOptions)
            formattedString += option.ToString() + '\n';
        
        formattedString += "==========\nPropagation Lists\n";
        foreach(PropagateList resource in propagationLists)
            formattedString += resource.ToString();
        formattedString += "**********";
        return formattedString;
    }

    public void buildMod(string inputDirectory, bool inputIsZip, string outputDirectory, bool outputToZip)
    {
        // If the output is a zip file, we should use a temporary directory 
        // instead of assuming a directory of the same w/o the extension is available to use
        string activeOutputDirectory = outputToZip ? TEMPORARY_DIRECTORY : outputDirectory,
               workingDirectory = Directory.GetCurrentDirectory();

        // Clone the contents of inputDirectory to the active outputDirectory
        if (!Directory.Exists(activeOutputDirectory))
            Directory.CreateDirectory(activeOutputDirectory);
        if(inputIsZip)
            ZipFile.ExtractToDirectory(inputDirectory, activeOutputDirectory);
        else
            CopyFilesRecursively(new DirectoryInfo(inputDirectory), new DirectoryInfo(activeOutputDirectory));

        // Begin working with the newly copied files
        Directory.SetCurrentDirectory(activeOutputDirectory);
        if (mustCheckAllFiles)
        {
            string[] allFiles = Directory.GetFiles(".", "*.*", SearchOption.AllDirectories);
            foreach(string file in allFiles)
                if(hasValidModFileExtension(file))
                    parseFile(file);
        }
        // All files in filepaths have already had their extensions validated when parsing the config
        else foreach(string file in filePaths)
            if (File.Exists(file))
                parseFile(file);
            else
                ProcessErrorCode(LOCATIONS_FILE_NOT_FOUND, file);

        // Handle Propagation
        if (Directory.Exists(PROPAGATE_DIRECTORY))
        {
            foreach (PropagateList resource in propagationLists)
                resource.propagate();
            Directory.Delete(PROPAGATE_DIRECTORY, true);
        }

        if (outputToZip)
        {
            Directory.SetCurrentDirectory(workingDirectory);
            ZipFile.CreateFromDirectory(TEMPORARY_DIRECTORY, outputDirectory);
            Directory.Delete(TEMPORARY_DIRECTORY, true);
        }
    }

    private void parseFile(string filePath)
    {
        StreamReader fileReader = new StreamReader(filePath);
        string fileText = fileReader.ReadToEnd();
        fileReader.Close();

        // Labels are parsed sequentially by scanning the entire text file.
        int labelStartIndex = fileText.IndexOf(LABEL_ANY, CurrentCultureIgnoreCase);
        while (labelStartIndex != -1)
        {
            // Get the complete label by utilizing the border characters.
            int labelEndIndex = fileText.IndexOf(LABEL_BORDER_VALUE, labelStartIndex + 1);
            if (labelEndIndex == -1)
                ProcessErrorCode(INCOMPLETE_LABEL, filePath);
            string label = fileText.Substring(labelStartIndex, labelEndIndex - labelStartIndex + 1);

            // Isolate the expression, if it exists
            int expressionSeparatorIndex = label.IndexOf(LABEL_NAME_EXP_SEPARATOR);
            if (expressionSeparatorIndex == -1)
            {
                // Edge Case: A toggleable-end label with no toggleable-beginning label preceding it
                if (label.Equals(LABEL_END_TOGGLEABLE, CurrentCultureIgnoreCase))
                    ProcessErrorCode(EXTRA_END_TOGGLE, filePath);
                else
                    ProcessErrorCode(MISSING_EXP_SEPARATOR, filePath, label);
            }

            // Don't uppercase the expression to prevent string-literals from being modified
            string expression = label.Substring(expressionSeparatorIndex + 1, label.Length - expressionSeparatorIndex - 2),
                   expressionResult = parseExpression(filePath, label, expression);

            // Everything up to and excluding the separator index
            string type = label.Substring(0, expressionSeparatorIndex).ToUpper();

            // Check if the type is valid - default if it isn't.
            switch (type)
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
                    ProcessErrorCode(UNRECOGNIZED_TYPE, filePath, label);
                    break;
            }
            // Starts search from the location of the previous label.
            labelStartIndex = fileText.IndexOf(LABEL_ANY, labelStartIndex, CurrentCultureIgnoreCase);
        }
        using(StreamWriter fileWriter = new StreamWriter(filePath))
            fileWriter.Write(fileText);
    }

    private string parseExpression(string filePath, string label, string expression)
    {
        int numIterations = 0;         // Prevents infinite loops
        bool replacedThisCycle = true; // Allows options to represent other options
        while(replacedThisCycle)
        {
            if(numIterations++ == INFINITE_LOOP_THRESHOLD)
                ProcessErrorCode(EXP_LOOPS_INFINITELY, filePath, label, expression);
            replacedThisCycle = false;

            foreach(Option option in configOptions)
            {
                string currentSearchString = '{' + option.name + '}';
                if(expression.IndexOf(currentSearchString, CurrentCultureIgnoreCase) != -1)
                {
                    replacedThisCycle = true;
                    expression = expression.Replace(currentSearchString, option.value, CurrentCultureIgnoreCase);
                }
            }    
        }

        string? result = "";
        try {result = expressionEvaluator.Compute(expression, "").ToString(); }
        catch(Exception e)
        {ProcessErrorCode(EXP_FAILED_TO_EVALUATE, filePath, label, expression, e.Message);}

        if(result != null)
            return result;
        else
            return "null"; 
    }

    private string parseToggle(string filePath, string fileText, string label, string expressionResult, int startLabelStartIndex, int startLabelEndIndex)
    {
        // Allows for toggles to be nested inside of other toggles
        int numEndLabelsNeeded = 1;
        bool interpretedResult = false;

        int endLabelStartIndex = startLabelStartIndex, endLabelEndIndex = 0;
        while(numEndLabelsNeeded > 0)
        {
            endLabelStartIndex = fileText.IndexOf(LABEL_ANY_TOGGLEABLE, endLabelStartIndex + 1, CurrentCultureIgnoreCase);
            if(endLabelStartIndex == -1)
                ProcessErrorCode(MISSING_END_TOGGLE, filePath, label);
            
            if(fileText.IndexOf(LABEL_END_TOGGLEABLE, endLabelStartIndex, CurrentCultureIgnoreCase) == endLabelStartIndex)
                numEndLabelsNeeded--;
            // No need to go too in-depth with error checking here, since one way or another invalid labels will get detected in parseFile
            else
            {
                // Iffy error-checking, but errors are detected

                // Just check if the toggle-non-end label we found is actually valid.
                // This way, all labels are validated even if the toggle is false
                endLabelEndIndex = fileText.IndexOf(LABEL_BORDER_VALUE, endLabelStartIndex + 1);
                if(endLabelEndIndex == -1)
                    ProcessErrorCode(INCOMPLETE_LABEL, filePath);

                // If true, the type is that of a toggle start label
                if(fileText.IndexOf(LABEL_ANY_TOGGLEABLE + LABEL_NAME_EXP_SEPARATOR, endLabelStartIndex, CurrentCultureIgnoreCase) == endLabelStartIndex)
                    numEndLabelsNeeded++;
                else
                    ProcessErrorCode(UNRECOGNIZED_TYPE, filePath, fileText.Substring(endLabelStartIndex, endLabelEndIndex - endLabelStartIndex + 1));
            }
        }
        endLabelEndIndex = fileText.IndexOf(LABEL_BORDER_VALUE, endLabelStartIndex + 1);

        // Try to resolve the expression result as a Boolean. If it's not possible, throw an error
        try{interpretedResult = Convert.ToBoolean(expressionResult);}
        catch(System.FormatException)
        {
            try{interpretedResult = Convert.ToDouble(expressionResult) >= 1 ? true : false;}
            catch(Exception)
            {ProcessErrorCode(BAD_TOGGLE_EXP_RESULT, filePath, label, expressionResult);}
        }

        if(interpretedResult)
            return fileText.Substring(0, startLabelStartIndex) // Everything before the start label
                + fileText.Substring(startLabelEndIndex + 1, endLabelStartIndex - startLabelEndIndex - 1) // Everything in-between the two labels
                + fileText.Substring(endLabelEndIndex + 1, fileText.Length - endLabelEndIndex - 1); // Everything after the end label
        else
            return fileText.Substring(0, startLabelStartIndex) // Everything before the start label
                + fileText.Substring(endLabelEndIndex + 1, fileText.Length - endLabelEndIndex - 1); // Everthing after the end label
    }
}