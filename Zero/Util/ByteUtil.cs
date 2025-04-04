namespace Zero.Util;

internal class ByteUtil
{
    public static byte[] ChompBytes(byte[] bzBytes, int Offset, int numBytes)
    {
        int End = Offset + numBytes;
        if (End > bzBytes.Length)
        {
            End = bzBytes.Length;
        }
        int chunkLength = End - numBytes;
        if (numBytes > bzBytes.Length)
        {
            numBytes = bzBytes.Length;
        }
        if (numBytes < 0)
        {
            numBytes = 0;
        }
        byte[] bzChunk = new byte[numBytes];
        for (int x = 0; x < numBytes; x++)
        {
            bzChunk[x] = bzBytes[Offset++];
        }
        return bzChunk;
    }
}
