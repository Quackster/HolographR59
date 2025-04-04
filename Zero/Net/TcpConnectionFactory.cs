using System.Net.Sockets;

namespace Zero.Net;

internal class TcpConnectionFactory
{
	private uint ZeroConnectionCounter;

	public uint Count => ZeroConnectionCounter;

	public TcpConnection CreateConnection(Socket ZeroSock)
	{
		if (ZeroSock == null)
		{
			return null;
		}
		return new TcpConnection(ZeroConnectionCounter++, ZeroSock);
	}
}
