using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Zero.Net;

internal class TcpConnectionManager
{
    private readonly int MAX_SIMULTANEOUS_CONNECTIONS = 512;

    private ConcurrentDictionary<uint, TcpConnection> Connections;

    private TcpConnectionListener Listener;

    public int AmountOfActiveConnections => Connections.Count;

    public TcpConnectionManager(string LocalIP, int Port, int maxSimultaneousConnections)
    {
        Connections = new ConcurrentDictionary<uint, TcpConnection>(/*initialCapicity*/);
        MAX_SIMULTANEOUS_CONNECTIONS = maxSimultaneousConnections;
        Listener = new TcpConnectionListener(LocalIP, Port, this);
    }

    public void DestroyManager()
    {
        Connections.Clear();
        Connections = null;
        Listener = null;
    }

    public bool ContainsConnection(uint Id)
    {
        return Connections.ContainsKey(Id);
    }

    public TcpConnection GetConnection(uint Id)
    {
        if (Connections.ContainsKey(Id))
        {
            return Connections[Id];
        }
        return null;
    }

    public TcpConnectionListener GetListener()
    {
        return Listener;
    }

    public void HandleNewConnection(TcpConnection connection)
    {
        Connections.TryAdd(connection.Id, connection);
        HolographEnvironment.GetGame().GetClientManager().StartClient(connection.Id);
    }

    public void DropConnection(uint Id)
    {
        GetConnection(Id)?.Stop();
        Connections.TryRemove(Id, out var _);
    }

    public bool VerifyConnection(uint Id)
    {
        return GetConnection(Id)?.TestConnection() ?? false;
    }
}
