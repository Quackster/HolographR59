using System;

namespace Zero.Core;

public abstract class RandomBase : Random
{
	public RandomBase()
	{
	}

	public RandomBase(int seed)
		: base(seed)
	{
	}

	protected int GetBaseNextInt32()
	{
		return base.Next();
	}

	protected uint GetBaseNextUInt32()
	{
		return ConvertToUInt32(base.Next());
	}

	protected double GetBaseNextDouble()
	{
		return base.NextDouble();
	}

	public abstract override int Next();

	public override int Next(int maxValue)
	{
		return Next(0, maxValue);
	}

	public override int Next(int minValue, int maxValue)
	{
		return Convert.ToInt32((double)(maxValue - minValue) * Sample() + (double)minValue);
	}

	public override double NextDouble()
	{
		return Sample();
	}

	public override void NextBytes(byte[] buffer)
	{
		int i;
		int tmp;
		for (i = 0; i < buffer.Length - 4; i += 4)
		{
			tmp = Next();
			buffer[i] = Convert.ToByte(tmp & 0xFF);
			buffer[i + 1] = Convert.ToByte((tmp & 0xFF00) >> 8);
			buffer[i + 2] = Convert.ToByte((tmp & 0xFF0000) >> 16);
			buffer[i + 3] = Convert.ToByte((tmp & 0xFF000000u) >> 24);
		}
		tmp = Next();
		for (int j = 0; j < buffer.Length % 4; j++)
		{
			buffer[i + j] = Convert.ToByte((tmp & (255 << 8 * j)) >> 8 * j);
		}
	}

	protected override double Sample()
	{
		return Convert.ToDouble(Next()) / 2147483648.0;
	}

	protected static uint ConvertToUInt32(int value)
	{
		return BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);
	}

	protected static int ConvertToInt32(uint value)
	{
		return BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
	}

	protected static int ConvertToInt32(ulong value)
	{
		return BitConverter.ToInt32(BitConverter.GetBytes(value & 0x7FFFFFFF), 0);
	}
}
