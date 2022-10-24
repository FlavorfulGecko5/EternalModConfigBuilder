/// <summary>
/// Static class for file system utility methods
/// </summary>
static class FSUtil
{
    /// <summary>
    /// Checks if a directory's contents exceed a particular size
    /// </summary>
    /// <param name="dir">Path to the directory</param>
    /// <param name="maxSize">The maximum size allowed </param>
    /// <returns> 
    /// True if the maximum size is exceeded, otherwise False. 
    /// </returns>
    public static bool isDirectoryLarge(string dir, long maxSize)
    {
        long sizeCheck = checkDirSize(new DirectoryInfo(dir));
        if (sizeCheck == -1)
            return true;
        return false;

        /// <summary>
        /// Recursively sums the size of a directory, checking if it exceeds
        /// the maximum size
        /// </summary>
        /// <param name="directory"> 
        /// The DirectoryInfo for the directory whose size must be checked. 
        /// </param>
        /// <returns>
        /// The size of the directory, or -1 if the maximum allowed size
        /// is exceeded.
        /// </returns>
        long checkDirSize(DirectoryInfo directory)
        {
            long size = 0;

            // Add file sizes.
            FileInfo[] files = directory.GetFiles();
            foreach (FileInfo f in files)
                size += f.Length;
        
            // Add subdirectory sizes.
            DirectoryInfo[] subDirectories = directory.GetDirectories();
            foreach (DirectoryInfo subDir in subDirectories)
            {
                long subSize = checkDirSize(subDir);
                if (subSize == -1)
                    return -1;
                size += subSize;
            }

            if (size > maxSize)
                return -1;
            return size;
        }
    }



    /// <summary>
    /// Checks if a file exceeds a particular size
    /// </summary>
    /// <param name="file">Pathway to the file </param>
    /// <param name="maxSize">The maximum size allowed </param>
    /// <returns>
    /// True if the file size exceeds the maximum size, otherwise False
    /// </returns>
    public static bool isFileLarge(string file, long maxSize)
    {
        FileInfo fileData = new FileInfo(file);
        return fileData.Length >  maxSize;
    }



    /// <summary>
    /// Checks if a directory has anything inside of it.
    /// </summary>
    /// <param name="dir">Path to the directory</param>
    /// <returns>
    /// True if anything is inside the directory (including empty sub-folders),
    /// otherwise False.
    /// </returns>
    public static bool dirContainsData(string dir)
    {
        return Directory.EnumerateFileSystemEntries(dir).Any();
    }



    /// <summary>
    /// Checks if a directory is a parent of a pathway
    /// </summary>
    /// <param name="possParentDir">Path to the possible parent folder </param>
    /// <param name="possChildPath">Possible child pathway </param>
    /// <returns>
    /// True if the pathway is contained inside, or refers to, the folder. 
    /// Otherwise returns False.
    /// </returns>
    public static bool isParentDir(string possParentDir, string possChildPath)
    {
        string parentAbs = Path.GetFullPath(possParentDir),
               childAbs = Path.GetFullPath(possChildPath);
        return childAbs.StartsWith(parentAbs);
    }



    /// <summary>
    /// Copies the contents of a source directory into a target directory
    /// </summary>
    /// <param name="srcDir"> The source directory </param>
    /// <param name="outputDir"> The pre-existing output directory </param>
    public static void copyDirectory(string srcDir, string outputDir)
    {
        CopyDir(new DirectoryInfo(srcDir), new DirectoryInfo(outputDir));

        /// <summary>
        /// Recursively copies a directory's contents
        /// </summary>
        /// <param name="source"> DirectoryInfo for the source folder </param>
        /// <param name="target"> DirectoryInfo for the target folder </param>
        static void CopyDir(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyDir(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
        }
    }



    /// <summary>
    /// Gets every filepath in the working directory
    /// </summary>
    /// <returns>
    /// A string list containing every filepath in the working directory
    /// </returns>
    public static string[] getAllFilesInCurrentDir()
    {
        return Directory.GetFiles(".", "*", SearchOption.AllDirectories);
    }



    /// <summary>
    /// Creates the directories specified in a filepath, if they don't exist.
    /// </summary>
    /// <param name="filepath"> The filepath to create directories from </param>
    public static void createDirectoryInFilePath(string filepath)
    {
        string? directory = Path.GetDirectoryName(filepath);
        if(directory != null && !directory.Equals(""))
            Directory.CreateDirectory(directory);
    }



    /// <summary>
    /// Reads a file's entire contents to a string
    /// </summary>
    /// <param name="filePath"> Pathway to the file </param>
    /// <returns>
    /// A string containing the raw text of the file.
    /// </returns>
    public static string readFileText(string filePath)
    {
        using (StreamReader reader = new StreamReader(filePath))
            return reader.ReadToEnd();
    }



    /// <summary>
    /// Writes a string to a new file
    /// </summary>
    /// <param name="filePath"> The output destination path </param>
    /// <param name="fileText"> The text of the file to be written </param>
    public static void writeFile(string filePath, string fileText)
    {
        using (StreamWriter fileWriter = new StreamWriter(filePath))
            fileWriter.Write(fileText);
    }
}