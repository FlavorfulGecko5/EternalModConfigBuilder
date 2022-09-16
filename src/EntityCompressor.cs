using System.Runtime.InteropServices;
class EntityCompressor
{
    [DllImport("oo2core_8_win64.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int OodleLZ_Compress
    (
        int codec, byte[] src, long srcLength, byte[] output, int compression,
        IntPtr opts, long offs, long unused, IntPtr scratch, long scratchSize
    );      

    public static void compressAndWrite(string filePath)
    {
        byte[] src = File.ReadAllBytes(filePath);
        byte[] output = new byte[src.Length + 65536];
        int outputLength = OodleLZ_Compress(13, src, src.Length, output, 4, new IntPtr(0), 0, 0, new IntPtr(0), 0);

       

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