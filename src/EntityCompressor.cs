using System.Runtime.InteropServices;
class EntityCompressor
{
    private static readonly OodleWrapper oodle;
    public static readonly bool canCompress;

    static EntityCompressor()
    {
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            oodle = new LinuxOodleWrapper();
        else
            oodle = new WindowsOodleWrapper();
        
        try
        {
            byte[] testInput = new byte[1000];
            byte[] testOutput = new byte[1000];
            oodle.compress(testInput, testOutput);
            canCompress = true;
        }
        catch(Exception)
        {
            LogMaker.reportWarning("The file(s) required for compressing "
            + ".entities files are missing or corrupted. Compression of "
            + ".entities files will NOT occur. See installation instructions "
            + "for more details.");
            canCompress = false;
        }
    }

    public static void compressAndWrite(string filePath)
    {
        byte[] src = File.ReadAllBytes(filePath);
        byte[] output = new byte[src.Length + 65536];
        
        int outputLength = oodle.compress(src, output);
        if(outputLength < 0)
        {
            LogMaker.reportWarning("Failed to compress '" + filePath
                + "' - Your built mod will contain the uncompressed version.");
            return;
        }

        byte[] resizedOutput = new byte[16 + outputLength];
        byte[] decompressedSizeBytes = BitConverter.GetBytes((long)src.Length);
        byte[] compressedSizeBytes = BitConverter.GetBytes((long)outputLength);
        Buffer.BlockCopy(decompressedSizeBytes, 0, resizedOutput, 0, 8);
        Buffer.BlockCopy(compressedSizeBytes, 0, resizedOutput, 8, 8);
        Buffer.BlockCopy(output, 0, resizedOutput, 16, outputLength);

        File.Delete(filePath);
        File.WriteAllBytes(filePath, resizedOutput);
    }

    public static bool isEntityFileCompressed(string filePath)
    {
        using(StreamReader reader = new StreamReader(filePath))
        {
            string firstLine = reader.ReadLine() ?? "";
            if(firstLine.Equals("Version 7"))
                return false;
        }
        return true;
    } 
}