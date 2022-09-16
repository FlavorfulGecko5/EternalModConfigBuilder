class ExtUtil
{
    public static bool hasExtension(string path, string extension)
    {
        int extIndex = path.embLastIndexOf(extension);
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
}