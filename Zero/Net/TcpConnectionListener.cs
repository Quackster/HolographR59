using System;
using System.Net;
using System.Net.Sockets;
using Zero.Core;

namespace Zero.Net;

internal class TcpConnectionListener
{
    private const int QUEUE_LENGTH = 0;

    private TcpListener Listener;

    private bool IsListening;

    private AsyncCallback ConnectionReqCallback;

    private TcpConnectionManager Manager;

    private TcpConnectionFactory Factory;

    public bool isListening => isListening;

    public TcpConnectionListener(string LocalIp, int Port, TcpConnectionManager Manager)
    {
        IPAddress IP = null;
        if (!IPAddress.TryParse(LocalIp, out IP))
        {
            IP = IPAddress.Loopback;
        }
        Listener = new TcpListener(IP, Port);
        ConnectionReqCallback = ConnectionRequest;
        Factory = new TcpConnectionFactory();
        this.Manager = Manager;
    }

    public void Start()
    {
        if (!IsListening)
        {
            Listener.Start();
            IsListening = true;
            WaitForNextConnection();
        }
    }

    public void Stop()
    {
        if (IsListening)
        {
            IsListening = false;
            Listener.Stop();
        }
    }

    public void Destroy()
    {
        Stop();
        Listener = null;
        Manager = null;
        Factory = null;
    }

    private void WaitForNextConnection()
    {
        if (IsListening)
        {
            Listener.BeginAcceptSocket(ConnectionReqCallback, null);
        }
    }

    private void ConnectionRequest(IAsyncResult iAr)
    {
        try
        {
            Socket Sock = Listener.EndAcceptSocket(iAr);
            TcpConnection Connection = Factory.CreateConnection(Sock);
            if (Connection != null)
            {
                Manager.HandleNewConnection(Connection);
            }
        }
        catch (Exception ex)
        {
            if (IsListening)
            {
                HolographEnvironment.GetLogging().WriteLine("[TCPListener.OnRequest]: Could not handle new connection request: " + ex.Message, LogLevel.Warning);
            }
        }
        finally
        {
            if (IsListening)
            {
                WaitForNextConnection();
            }
        }
    }
}
