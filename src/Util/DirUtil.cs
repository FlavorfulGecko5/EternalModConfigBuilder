using static System.IO.SearchOption;
class DirUtil
{
    public static bool isDirectoryLarge(string dir)
    {
        long sizeCheck = checkDirSize(new DirectoryInfo(dir));
        if (sizeCheck == -1)
            return true;
        return false;
    }

    private static long checkDirSize(DirectoryInfo directory)
    {
        long size = 0;
        // Add file sizes.
        FileInfo[] files = directory.GetFiles();
        foreach (FileInfo f in files)
        {
            size += f.Length;
        }
        // Add subdirectory sizes.
        DirectoryInfo[] subDirectories = directory.GetDirectories();
        foreach (DirectoryInfo subDir in subDirectories)
        {
            long subSize = checkDirSize(subDir);
            if (subSize == -1)
                return -1;
            size += subSize;
        }

        if (size > MAX_INPUT_SIZE_BYTES)
            return -1;
        return size;
    }

    public static bool dirContainsData(string dir)
    {
        return Directory.EnumerateFileSystemEntries(dir).Any();
    }

    public static bool isParentDir(string possParentDir, string possChildPath)
    {
        string parentAbs = Path.GetFullPath(possParentDir),
               childAbs = Path.GetFullPath(possChildPath);

        return childAbs.Contains(parentAbs, CCIC);
    }

    public static bool isPathLocalRelative(string path, string currentDirAbs)
    {
        if(path.Equals(""))
            return false;
        if(Path.IsPathRooted(path))
            return false;
        
        string pathAbs = Path.GetFullPath(path);
        return pathAbs.Contains(currentDirAbs, CCIC);
    }

    public static void copyDirectory(string srcDir, string outputDir)
    {
        CopyDir(new DirectoryInfo(srcDir), new DirectoryInfo(outputDir));
    }

    private static void CopyDir(DirectoryInfo source, DirectoryInfo target)
    {
        foreach (DirectoryInfo dir in source.GetDirectories())
            CopyDir(dir, target.CreateSubdirectory(dir.Name));
        foreach (FileInfo file in source.GetFiles())
            file.CopyTo(Path.Combine(target.FullName, file.Name), true);
    }

    public static string[] getAllDirectoryFiles(string dir)
    {
        return Directory.GetFiles(dir, "*.*", AllDirectories);
    }

    public static void createDirectoryInFilePath(string filepath)
    {
        string? directory = Path.GetDirectoryName(filepath);
        if(directory != null && !directory.Equals(""))
            Directory.CreateDirectory(directory);
    }
}