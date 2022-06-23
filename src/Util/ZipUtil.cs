using System.IO.Compression;
using System.Collections.ObjectModel;
class ZipUtil
{
    public static bool isFileValidZip(string file)
    {
        if (!ExtUtil.hasExtension(file, ".zip"))
            return false;
        try
        {
            using(ZipArchive zip = ZipFile.OpenRead(file))
            {
                ReadOnlyCollection<ZipArchiveEntry> entries = zip.Entries;
            }
            return true;
        }
        catch (InvalidDataException) // Not a valid zip
        {
            return false;
        }
    }

    public static void unzip(string srcZip, string outputDir)
    {
        ZipFile.ExtractToDirectory(srcZip, outputDir);
    }
    
    public static void makeZip(string srcDir, string outputZip)
    {
        // Zip can only be created inside a pre-existing directory
        DirUtil.createDirectoryInFilePath(outputZip);
        ZipFile.CreateFromDirectory(srcDir, outputZip);
    }
}