using System;

namespace Zero.Util;

public class Base64Encoding
{
	public const byte NEGATIVE = 64;

	public const byte POSITIVE = 65;

	public static byte[] EncodeInt32(int i, int numBytes)
	{
		byte[] bzRes = new byte[numBytes];
		for (int j = 1; j <= numBytes; j++)
		{
			int k = (numBytes - j) * 6;
			bzRes[j - 1] = (byte)(64 + ((i >> k) & 0x3F));
		}
		return bzRes;
	}

	public static byte[] Encodeuint(uint i, int numBytes)
	{
		return EncodeInt32((int)i, numBytes);
	}

	public static int DecodeInt32(byte[] bzData)
	{
		int i = 0;
		int j = 0;
		for (int k = bzData.Length - 1; k >= 0; k--)
		{
			int x = bzData[k] - 64;
			if (j > 0)
			{
				x *= (int)Math.Pow(64.0, j);
			}
			i += x;
			j++;
		}
		return i;
	}

	public static uint DecodeUInt32(byte[] bzData)
	{
		return (uint)DecodeInt32(bzData);
	}
}
