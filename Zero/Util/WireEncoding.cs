using System;

namespace Zero.Util;

public static class WireEncoding
{
	public const byte NEGATIVE = 72;

	public const byte POSITIVE = 73;

	public const int MAX_INTEGER_BYTE_AMOUNT = 6;

	public static byte[] EncodeInt32(int i)
	{
		byte[] wf = new byte[6];
		int pos = 0;
		int numBytes = 1;
		int startPos = pos;
		int negativeMask = ((i < 0) ? 4 : 0);
		i = Math.Abs(i);
		wf[pos++] = (byte)(64 + (i & 3));
		for (i >>= 2; i != 0; i >>= 6)
		{
			numBytes++;
			wf[pos++] = (byte)(64 + (i & 0x3F));
		}
		wf[startPos] = (byte)(wf[startPos] | (numBytes << 3) | negativeMask);
		byte[] bzData = new byte[numBytes];
		for (int x = 0; x < numBytes; x++)
		{
			bzData[x] = wf[x];
		}
		return bzData;
	}

	public static int DecodeInt32(byte[] bzData, out int totalBytes)
	{
		int pos = 0;
		int v = 0;
		bool negative = (bzData[pos] & 4) == 4;
		totalBytes = (bzData[pos] >> 3) & 7;
		v = bzData[pos] & 3;
		pos++;
		int shiftAmount = 2;
		for (int b = 1; b < totalBytes; b++)
		{
			v |= (bzData[pos] & 0x3F) << shiftAmount;
			shiftAmount = 2 + 6 * b;
			pos++;
		}
		if (negative)
		{
			v *= -1;
		}
		return v;
	}
}
