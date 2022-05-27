using Newtonsoft.Json.Linq;
using static System.StringComparison;
using static ErrorReporter;
using static Constants;
class Util
{
    public static bool hasExtension(string filePath, string extension)
    {
        int extensionIndex = filePath.LastIndexOf(extension, CurrentCultureIgnoreCase);
        if (extensionIndex > -1)
            if (extensionIndex == filePath.Length - extension.Length)
                return true;
        return false;
    }

    // Returns true if a mod file has a valid extension for containing labels, otherwise false
    public static bool hasValidModFileExtension(string filePath)
    {
        foreach(string extension in SUPPORTED_FILETYPES)
        {
            if(hasExtension(filePath, extension))
                return true;
        }
        return false;
    }

    // Reads a Json list that is assumed can be parsed into an array of strings.
    // Possible return values:
    // 1. null - if the Json property is missing entirely, undefined or equals null
    // 2. A list of strings if the Json property is a list, and all of it's elements are strings.
    public static string[]? readJsonStringList(JToken? inputProperty, ErrorCode terminateOnFail, string arg0 = "")
    {
        string[]? list = null;
        try
        {
            JArray? rawData = (JArray?)inputProperty;
            // If the property is missing entirely, undefined or equals null
            if (rawData == null)
                return null;
            list = new string[rawData.Count];
            
            for(int i = 0; i < rawData.Count; i++)
            {
                string? nullCurrentElement = (string?)rawData[i];
                if(nullCurrentElement == null)
                    ProcessErrorCode(terminateOnFail, arg0);
                else
                    list[i] = nullCurrentElement;
            }
        }
        // The property is not defined as an array
        catch (System.InvalidCastException) { ProcessErrorCode(terminateOnFail, arg0); }
        // A list's element is a Json list or object
        catch (System.ArgumentException) { ProcessErrorCode(terminateOnFail, arg0); }
        return list;
    }

    // Used to copy the mod folder to the output directory.
    // If any folders in the output directory do not exist, they will be created.
    // If any files that are to be copied already exist, they will be overwritten.
    public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
    {
        foreach (DirectoryInfo dir in source.GetDirectories())
            CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
        foreach (FileInfo file in source.GetFiles())
            file.CopyTo(Path.Combine(target.FullName, file.Name), true);
    }
}