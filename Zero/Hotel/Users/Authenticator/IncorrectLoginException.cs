using System;

namespace Zero.Hotel.Users.Authenticator;

public class IncorrectLoginException : Exception
{
	public IncorrectLoginException(string Reason)
		: base(Reason)
	{
	}
}
