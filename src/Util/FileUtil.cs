class FileUtil
{
    public static bool isFileLarge(string file)
    {
        FileInfo fileData = new FileInfo(file);
        return fileData.Length > MAX_INPUT_SIZE_BYTES;
    }
}