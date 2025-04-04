using System;

namespace Zero.Hotel.Support;

public class ModerationBanException : Exception
{
	public ModerationBanException(string Reason)
		: base(Reason)
	{
	}
}
