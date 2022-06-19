class Util
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
        foreach (string extension in CONFIG_EXTENSIONS)
            if (hasExtension(filePath, extension))
                return true;
        return false;
    }

    // Returns true if a mod file has a valid extension for containing labels
    public static bool hasValidModFileExtension(string filePath)
    {
        foreach(string extension in LABEL_FILETYPES)
            if(hasExtension(filePath, extension))
                return true;
        return false;
    }

    // Copy a source directory's contents to the target directory
    public static void CopyDir(DirectoryInfo source, DirectoryInfo target)
    {
        foreach (DirectoryInfo dir in source.GetDirectories())
            CopyDir(dir, target.CreateSubdirectory(dir.Name));
        foreach (FileInfo file in source.GetFiles())
            file.CopyTo(Path.Combine(target.FullName, file.Name), true);
    }
}