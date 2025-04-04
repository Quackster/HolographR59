using slockimp;

namespace System.Threading;

internal class ADL
{
	public static void Enter(object obj)
	{
		imp.DoLock(obj);
	}

	public static void Exit(object obj)
	{
		imp.DoUnlock(obj);
	}

	public static bool TryEnter(object obj)
	{
		return imp.DoTryEnter(obj);
	}

	public static bool TryEnter(object obj, int millisecondsTimeout)
	{
		return imp.DoTryEnter(obj, millisecondsTimeout);
	}

	public static bool TryEnter(object obj, TimeSpan timeout)
	{
		return imp.DoTryEnter(obj, timeout);
	}
}
