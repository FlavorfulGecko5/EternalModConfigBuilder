class FileUtil
{
    public static bool isFileLarge(string file)
    {
        FileInfo fileData = new FileInfo(file);
        return fileData.Length > MAX_INPUT_SIZE_BYTES;
    }

    public static string readFileText(string filePath)
    {
        using (StreamReader reader = new StreamReader(filePath))
            return reader.ReadToEnd();
    }

    public static void writeFile(string filePath, string fileText)
    {
        using (StreamWriter fileWriter = new StreamWriter(filePath))
            fileWriter.Write(fileText);
    }
}