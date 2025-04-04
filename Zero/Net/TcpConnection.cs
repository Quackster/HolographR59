using System;
using System.Net.Sockets;
using System.Threading;
using Zero.Core;
using Zero.Messages;
using Zero.Util;

namespace Zero.Net;

internal class TcpConnection
{
	public delegate void RouteReceivedDataCallback(ref byte[] Data);

	private const int RCV_BUFFER_SIZE = 512;

	private static int RCV_MILLI_DELAY = 0;

	public readonly uint Id;

	public readonly DateTime Created;

	private Socket Socket;

	private byte[] Buffer;

	private AsyncCallback DataReceivedCallback;

	private RouteReceivedDataCallback RouteDataCallback;

	public int AgeInSeconds
	{
		get
		{
			int s = (int)(DateTime.Now - Created).TotalSeconds;
			if (s < 0)
			{
				s = 0;
			}
			return s;
		}
	}

	public string IPAddress
	{
		get
		{
			if (Socket == null)
			{
				return "";
			}
			return Socket.RemoteEndPoint.ToString().Split(':')[0];
		}
	}

	public bool IsAlive
	{
		get
		{
			if (Socket == null)
			{
				return false;
			}
			return true;
		}
	}

	public TcpConnection(uint Id, Socket Sock)
	{
		this.Id = Id;
		Socket = Sock;
		Created = DateTime.Now;
	}

	public void Start(RouteReceivedDataCallback DataRouter)
	{
		Buffer = new byte[512];
		DataReceivedCallback = DataReceived;
		RouteDataCallback = DataRouter;
		WaitForData();
	}

	public void Stop()
	{
		if (IsAlive)
		{
			Socket.Close();
			Socket = null;
			Buffer = null;
			DataReceivedCallback = null;
		}
	}

	public bool TestConnection()
	{
		try
		{
			Socket socket = Socket;
			byte[] buffer = new byte[1];
			return socket.Send(buffer) > 0;
		}
		catch
		{
		}
		return false;
	}

	private void ConnectionDead()
	{
		HolographEnvironment.GetGame().GetClientManager().StopClient(Id);
	}

	public void SendData(byte[] Data)
	{
		if (!IsAlive)
		{
			return;
		}
		try
		{
			Socket.Send(Data);
		}
		catch (SocketException)
		{
			ConnectionDead();
		}
		catch (ObjectDisposedException)
		{
			ConnectionDead();
		}
		catch
		{
			HolographEnvironment.GetLogging().WriteLine("Ocorreu um Erro!", LogLevel.Error);
			ConnectionDead();
		}
	}

	public void SendData(string Data)
	{
		SendData(HolographEnvironment.GetDefaultEncoding().GetBytes(Data));
	}

	public Socket GetSocket()
	{
		return Socket;
	}

	public void SendMessage(ServerMessage Message)
	{
		if (Message != null)
		{
			SendData(Message.GetBytes());
		}
	}

	private void WaitForData()
	{
		if (IsAlive)
		{
			try
			{
				Socket.BeginReceive(Buffer, 0, 512, SocketFlags.None, DataReceivedCallback, null);
			}
			catch (SocketException)
			{
				ConnectionDead();
			}
			catch (ObjectDisposedException)
			{
				ConnectionDead();
			}
			catch
			{
				ConnectionDead();
			}
		}
	}

	private void DataReceived(IAsyncResult iAr)
	{
		if (IsAlive)
		{
			if (RCV_MILLI_DELAY > 0)
			{
				Thread.Sleep(RCV_MILLI_DELAY);
			}
			int rcvBytesCount = 0;
			try
			{
				rcvBytesCount = Socket.EndReceive(iAr);
			}
			catch (ObjectDisposedException)
			{
				ConnectionDead();
				return;
			}
			catch
			{
				ConnectionDead();
				return;
			}
			if (rcvBytesCount < 1)
			{
				ConnectionDead();
				return;
			}
			byte[] toProcess = ByteUtil.ChompBytes(Buffer, 0, rcvBytesCount);
			RouteData(ref toProcess);
			WaitForData();
		}
	}

	private void RouteData(ref byte[] Data)
	{
		if (RouteDataCallback != null)
		{
			RouteDataCallback(ref Data);
		}
	}
}
