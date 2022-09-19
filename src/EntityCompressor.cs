using System.Runtime.InteropServices;
class EntityCompressor
{
    private static OodleWrapper oodle;

    static EntityCompressor()
    {
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            oodle = new LinuxOodleWrapper();
        else
            oodle = new WindowsOodleWrapper();
    }

    public static void compressAndWrite(string filePath)
    {
        byte[] src = File.ReadAllBytes(filePath);
        byte[] output = new byte[src.Length + 65536];
        int outputLength = oodle.compress(src, output);

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