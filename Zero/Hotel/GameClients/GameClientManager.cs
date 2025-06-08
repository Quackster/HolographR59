using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using Zero.Hotel.Support;
using Zero.Messages;
using Zero.Storage;
using System.Collections.Concurrent;

namespace Zero.Hotel.GameClients;

internal class GameClientManager
{
    private Thread ConnectionChecker;

    private ConcurrentDictionary<uint, GameClient> Clients;

    public int ClientCount => Clients.Count;

    public void LogClonesOut(string Username)
    {
        List<uint> ToRemove = new List<uint>();

        foreach (var kvp in this.Clients)
        {
            GameClient Client = kvp.Value;

            if (Client.GetHabbo() != null && Client.GetHabbo().Username.ToLower() == Username.ToLower())
            {
                ToRemove.Add(Client.ClientId);
                continue;
            }
        }

        for (int i = 0; i < ToRemove.Count; i++)
        {
            this.Clients[ToRemove[i]].Disconnect();
        }
    }

    public string GetNameById(uint Id)
    {
        GameClient Cl = GetClientByHabbo(Id);

        if (Cl != null)
        {
            return Cl.GetHabbo().Username;
        }

        DataRow Row = null;

        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            Row = dbClient.ReadDataRow("SELECT username FROM users WHERE id = '" + Id + "' LIMIT 1");
        }

        if (Row == null)
        {
            return "Unknown User";
        }

        return (string)Row[0];
    }

    public void DeployHotelCreditsUpdate()
    {
        foreach (var kvp in this.Clients)
        {
            GameClient Client = kvp.Value;

            if (Client.GetHabbo() == null)
            {
                continue;
            }

            int newCredits = 0;

            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                newCredits = (int)dbClient.ReadDataRow("SELECT credits FROM users WHERE id = '" + Client.GetHabbo().Id + "' LIMIT 1")[0];
            }

            int oldBalance = Client.GetHabbo().Credits;

            Client.GetHabbo().Credits = newCredits;

            if (oldBalance < 3000)
            {
                Client.GetHabbo().UpdateCreditsBalance(false);
                Client.SendNotif("Uber Credits Update" + Convert.ToChar(13) + "-----------------------------------" + Convert.ToChar(13) + "We have refilled your credits up to 3000 - enjoy! The next credits update will occur in 3 hours.", "http://uber.meth0d.org/credits");
            }
            else if (oldBalance >= 3000)
            {
                Client.SendNotif("Uber Credits Update" + Convert.ToChar(13) + "-----------------------------------" + Convert.ToChar(13) + "Sorry! Because your credit balance is 3000 or higher, we have not refilled your credits. The next credits update will occur in 3 hours.", "http://uber.meth0d.org/credits");
            }
        }
    }
    public void CheckForAllBanConflicts()
    {
        Dictionary<GameClient, ModerationBanException> ConflictsFound = new Dictionary<GameClient, ModerationBanException>();

        foreach (var kvp in this.Clients)
        {
            GameClient Client = kvp.Value;

            try
            {
                HolographEnvironment.GetGame().GetBanManager().CheckForBanConflicts(Client);
            }

            catch (ModerationBanException e)
            {
                ConflictsFound.Add(Client, e);
            }
        }


        foreach (KeyValuePair<GameClient, ModerationBanException> Data in ConflictsFound)
        {
            Data.Key.SendBanMessage(Data.Value.Message);
            Data.Key.Disconnect();
        }
    }

    public void CheckPixelUpdates()
    {
        try
        {
            foreach (var kvp in this.Clients)
            {
                GameClient Client = kvp.Value;

                if (Client.GetHabbo() == null || !HolographEnvironment.GetGame().GetPixelManager().NeedsUpdate(Client))
                {
                    continue;
                }

                HolographEnvironment.GetGame().GetPixelManager().GivePixels(Client);
            }
        }

        catch (Exception e)
        {
            HolographEnvironment.GetLogging().WriteLine("[GCMExt.CheckPixelUpdates]: " + e.Message);
        }
    }

    public GameClientManager()
    {
        this.Clients = new ConcurrentDictionary<uint, GameClient>();
    }

    public void Clear()
    {
        Clients.Clear();
    }

    public GameClient GetClient(uint ClientId)
    {
        if (Clients.ContainsKey(ClientId))
        {
            return Clients[ClientId];
        }
        return null;
    }

    public bool RemoveClient(uint ClientId)
    {
        return Clients.TryRemove(ClientId, out var _);
    }

    public void StartClient(uint ClientId)
    {

        Clients.TryAdd(ClientId, new GameClient(ClientId));
        Clients[ClientId].StartConnection();
    }

    public void StopClient(uint ClientId)
    {
        GameClient Client = GetClient(ClientId);
        if (Client != null)
        {
            HolographEnvironment.GetConnectionManager().DropConnection(ClientId);
            Client.Stop();
            RemoveClient(ClientId);
        }
    }

    public void StartConnectionChecker()
    {
        if (ConnectionChecker == null)
        {
            ConnectionChecker = new Thread(TestClientConnections);
            ConnectionChecker.Name = "Checagem de Conexão";
            ConnectionChecker.Priority = ThreadPriority.Lowest;
            ConnectionChecker.Start();
        }
    }

    public void StopConnectionChecker()
    {
        if (ConnectionChecker != null)
        {
            try
            {
                // Migration to .NET 8
                // ConnectionChecker.Abort();
                ConnectionChecker.Interrupt();
            }
            catch (ThreadInterruptedException) // catch (ThreadAbortException)
            {
            }
            ConnectionChecker = null;
        }
    }

    private void TestClientConnections()
    {
        int interval = int.Parse(HolographEnvironment.GetConfig().data["client.ping.interval"]);

        if (interval <= 100)
        {
            throw new ArgumentException("Invalid configuration value for ping interval! Must be above 100 miliseconds.");
        }

        while (true)
        {
            ServerMessage PingMessage = new ServerMessage(50);

            try
            {
                List<uint> TimedOutClients = new List<uint>();
                List<GameClient> ToPing = new List<GameClient>();

                /*
                badlock (this.Clients)
                {
                    ConcurrentDictionary<uint, GameClient>.Enumerator eClients = this.Clients.GetEnumerator();                     

                    while (eClients.MoveNext())
                    {
                        GameClient Client = eClients.Current.Value;

                        if (Client.PongOK)
                        {
                            Client.PongOK = false;
                            ToPing.Add(Client);
                        }
                        else
                        {
                            TimedOutClients.Add(Client.ClientId);
                        }
                    }
                }*/

                foreach (var kvp in this.Clients)
                {
                    GameClient Client = kvp.Value;

                    if (Client.PongOK)
                    {
                        Client.PongOK = false;
                        ToPing.Add(Client);
                    }
                    else
                    {
                        TimedOutClients.Add(Client.ClientId);
                    }
                }

                foreach (uint ClientId in TimedOutClients)
                {
                    HolographEnvironment.GetGame().GetClientManager().StopClient(ClientId);
                }

                foreach (GameClient Client in ToPing)
                {
                    try
                    {
                        Client.GetConnection().SendMessage(PingMessage);
                    }
                    catch (Exception) { }
                }

                Thread.Sleep(interval);
            }
            catch (ThreadAbortException) { }
        }
    }

    public GameClient GetClientByHabbo(uint HabboId)
    {
        foreach (var kvp in this.Clients)
        {
            GameClient Client = kvp.Value;

            if (Client.GetHabbo() == null)
            {
                continue;
            }

            if (Client.GetHabbo().Id == HabboId)
            {
                return Client;
            }
        }

        return null;
    }

    public GameClient GetClientByHabbo(string Name)
    {
        foreach (var kvp in this.Clients)
        {
            GameClient Client = kvp.Value;

            if (Client.GetHabbo() == null)
            {
                continue;
            }

            if (Client.GetHabbo().Username.ToLower() == Name.ToLower())
            {
                return Client;
            }
        }

        return null;
    }

    public void BroadcastMessage(ServerMessage Message)
    {
        BroadcastMessage(Message, "");
    }

    public void BroadcastMessage(ServerMessage Message, String FuseRequirement)
    {
        foreach (var kvp in this.Clients)
        {
            GameClient Client = kvp.Value;

            try
            {
                if (FuseRequirement.Length > 0)
                {
                    if (Client.GetHabbo() == null || !Client.GetHabbo().HasFuse(FuseRequirement))
                    {
                        continue;
                    }
                }

                Client.SendMessage(Message);
            }
            catch (Exception) { }
        }
    }

    public void CheckEffects()
    {
        foreach (var kvp in this.Clients)
        {
            GameClient Client = kvp.Value;

            if (Client.GetHabbo() == null || Client.GetHabbo().GetAvatarEffectsInventoryComponent() == null)
            {
                continue;
            }

            Client.GetHabbo().GetAvatarEffectsInventoryComponent().CheckExpired();
        }
    }
}
