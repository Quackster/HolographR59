using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Zero.Net;

internal class MusSocket
{
	public Socket msSocket;

	public string musIp;

	public int musPort;

	public HashSet<string> allowedIps;

	public MusSocket(string _musIp, int _musPort, string[] _allowedIps, int backlog)
	{
		musIp = _musIp;
		musPort = _musPort;
		allowedIps = new HashSet<string>();
		foreach (string ip in _allowedIps)
		{
			allowedIps.Add(ip);
		}
		try
		{
			msSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			msSocket.Bind(new IPEndPoint(IPAddress.Parse(musIp), musPort));
			msSocket.Listen(backlog);
			msSocket.BeginAccept(OnEvent_NewConnection, msSocket);
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("[.NET] » Checando mysql.ini...");
			Console.WriteLine("[.NET] » mysql.ini Encontrado Carregando Informações...");
			Console.WriteLine("");
		}
		catch (Exception ex)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("Erro 'mysql.ini' Não configurado corretamente \n" + ex.Message);
		}
	}

	public void OnEvent_NewConnection(IAsyncResult iAr)
	{
		Socket socket = ((Socket)iAr.AsyncState).EndAccept(iAr);
		MusConnection nC = new MusConnection(socket);
		msSocket.BeginAccept(OnEvent_NewConnection, msSocket);
	}
}
