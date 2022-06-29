class ExtUtil
{
    public static bool hasExtension(string path, string extension)
    {
        int extIndex = path.LastIndexOf(extension, CCIC);
        if (extIndex > -1 && extIndex == path.Length - extension.Length)
            return true;
        return false;
    }

    public static bool hasValidConfigFileExtension(string filePath)
    {
        foreach (string extension in CFG_EXTENSIONS)
            if (hasExtension(filePath, extension))
                return true;
        return false;
    }

    // Returns true if a mod file has a valid extension for containing labels
    public static bool hasValidModFileExtension(string filePath)
    {
        foreach(string extension in LABEL_FILES)
            if(hasExtension(filePath, extension))
                return true;
        return false;
    }
}