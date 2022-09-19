using System.Runtime.InteropServices;

interface OodleWrapper 
{
    int compress(byte[] src, byte[] output);
}

class WindowsOodleWrapper : OodleWrapper
{
    [DllImport("oo2core_8_win64.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int OodleLZ_Compress
    (
        int codec, byte[] src, long srcLength, byte[] output, int compression,
        IntPtr opts, long offs, long unused, IntPtr scratch, long scratchSize
    );

    public int compress(byte[] src, byte[] output)
    {
        return OodleLZ_Compress(13, src, src.Length, output, 4, 
            new IntPtr(0), 0, 0, new IntPtr(0), 0);
    }
}

class LinuxOodleWrapper : OodleWrapper
{
    [DllImport("liblinoodle.so", CallingConvention = CallingConvention.Cdecl)]
    private static extern int OodleLZ_Compress
    (
        int codec, byte[] src, long srcLength, byte[] output, int compression,
        IntPtr opts, long offs, long unused, IntPtr scratch, long scratchSize
    );

    public int compress(byte[] src, byte[] output)
    {
        return OodleLZ_Compress(13, src, src.Length, output, 4,
            new IntPtr(0), 0, 0, new IntPtr(0), 0);
    }
}