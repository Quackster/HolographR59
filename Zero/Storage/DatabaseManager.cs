using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Threading;
using Zero.Core;

namespace Zero.Storage;

internal class DatabaseManager
{
    public bool[] AvailableClients;

    public Thread ClientMonitor;

    public DatabaseClient[] Clients;

    public int ClientStarvation;

    public Database Database;

    public DatabaseServer Server;

    public string ConnectionString
    {
        get
        {
            MySqlConnectionStringBuilder mySqlConnectionStringBuilder = new MySqlConnectionStringBuilder();
            mySqlConnectionStringBuilder.Server = Server.Hostname;
            mySqlConnectionStringBuilder.Port = Server.Port;
            mySqlConnectionStringBuilder.UserID = Server.Username;
            mySqlConnectionStringBuilder.Password = Server.Password;
            mySqlConnectionStringBuilder.Database = Database.DatabaseName;
            mySqlConnectionStringBuilder.MinimumPoolSize = Database.PoolMinSize;
            mySqlConnectionStringBuilder.MaximumPoolSize = Database.PoolMaxSize;
            return mySqlConnectionStringBuilder.ToString();
        }
    }

    public DatabaseManager(DatabaseServer _Server, Database _Database)
    {
        Server = _Server;
        Database = _Database;
        Clients = new DatabaseClient[0];
        AvailableClients = new bool[0];
        ClientStarvation = 0;
        StartClientMonitor();
    }

    public void DestroyClients()
    {
        for (int i = 0; i < Clients.Length; i++)
        {
            Clients[i].Destroy();
            Clients[i] = null;
        }
    }

    public void DestroyDatabaseManager()
    {
        StopClientMonitor();
        for (int i = 0; i < Clients.Length; i++)
        {
            try
            {
                Clients[i].Destroy();
                Clients[i] = null;
            }
            catch (NullReferenceException)
            {
            }
        }
        Server = null;
        Database = null;
        Clients = null;
        AvailableClients = null;
    }

    public DatabaseClient GetClient()
    {
        for (uint i = 0u; i < Clients.Length; i++)
        {
            if (!AvailableClients[i])
            {
                continue;
            }
            ClientStarvation = 0;
            if (Clients[i].State == ConnectionState.Closed)
            {
                try
                {
                    Clients[i].Connect();
                }
                catch (Exception ex)
                {
                    HolographEnvironment.GetLogging().WriteLine("Could not get database client: " + ex.Message, LogLevel.Error);
                }
            }
            if (Clients[i].State == ConnectionState.Open)
            {
                AvailableClients[i] = false;
                Clients[i].UpdateLastActivity();
                return Clients[i];
            }
        }
        ClientStarvation++;
        if (ClientStarvation >= (Clients.Length + 1) / 2)
        {
            ClientStarvation = 0;
            SetClientAmount((uint)((float)Clients.Length + 1.3f));
            return GetClient();
        }
        DatabaseClient Anonymous = new DatabaseClient(0u, this);
        Anonymous.Connect();
        return Anonymous;
    }

    public void MonitorClients()
    {
        while (true)
        {
            // bool flag = true;
            try
            {
                DateTime DT = DateTime.Now;
                for (int i = 0; i < Clients.Length; i++)
                {
                    if (Clients[i].State != 0 && Clients[i].InactiveTime >= 60)
                    {
                        Clients[i].Disconnect();
                    }
                }
                Thread.Sleep(10000);
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception ex2)
            {
                HolographEnvironment.GetLogging().WriteLine("An error occured in database manager client monitor: " + ex2.Message);
            }
        }
    }

    public void ReleaseClient(uint Handle)
    {
        if (Clients.Length >= Handle - 1)
        {
            AvailableClients[(int)(IntPtr)(Handle - 1)] = true;
        }
    }

    public void SetClientAmount(uint Amount)
    {
        if (Clients.Length == Amount)
        {
            return;
        }
        if (Amount < Clients.Length)
        {
            for (uint i = Amount; i < Clients.Length; i++)
            {
                Clients[i].Destroy();
                Clients[i] = null;
            }
        }
        DatabaseClient[] _Clients = new DatabaseClient[Amount];
        bool[] _AvailableClients = new bool[Amount];
        for (uint i = 0u; i < Amount; i++)
        {
            if (i < Clients.Length)
            {
                _Clients[i] = Clients[i];
                _AvailableClients[i] = AvailableClients[i];
            }
            else
            {
                _Clients[i] = new DatabaseClient(i + 1, this);
                _AvailableClients[i] = true;
            }
        }
        Clients = _Clients;
        AvailableClients = _AvailableClients;
    }

    public void StartClientMonitor()
    {
        if (ClientMonitor == null)
        {
            ClientMonitor = new Thread(MonitorClients);
            ClientMonitor.Name = "DB Monitor";
            ClientMonitor.Priority = ThreadPriority.Lowest;
            ClientMonitor.Start();
        }
    }

    public void StopClientMonitor()
    {
        if (ClientMonitor != null)
        {
            try
            {
                // Migration to .NET 8
                // ClientMonitor.Abort();
                ClientMonitor.Interrupt();
            }
            catch (ThreadInterruptedException) // catch (ThreadAbortException)
            {
            }
            ClientMonitor = null;
        }
    }
}
