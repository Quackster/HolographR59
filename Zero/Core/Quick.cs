using System;

namespace Zero.Core;

public class Quick : RandomBase
{
    private static readonly uint a = 1099087573u;

    private ulong i;

    public Quick()
        : this(Convert.ToInt32(DateTime.Now.Ticks & 0x7FFFFFFF))
    {
    }

    public Quick(int seed)
        : base(seed)
    {
        i = Convert.ToUInt64(GetBaseNextInt32());
    }

    public override int Next()
    {
        i = a * i;
        return RandomBase.ConvertToInt32(i);
    }
}
