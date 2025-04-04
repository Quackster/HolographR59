using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using Zero.Hotel.Support;
using Zero.Messages;
using Zero.Storage;

namespace Zero.Hotel.GameClients;

internal class GameClientManager
{
	private Thread ConnectionChecker;

	private Dictionary<uint, GameClient> Clients;

	public int ClientCount => Clients.Count;

	public void LogClonesOut(string Username)
	{
		List<uint> ToRemove = new List<uint>();
		lock (Clients)
		{
			Dictionary<uint, GameClient>.Enumerator eClients = Clients.GetEnumerator();
			while (eClients.MoveNext())
			{
				GameClient Client = eClients.Current.Value;
				if (Client.GetHabbo() != null && Client.GetHabbo().Username.ToLower() == Username.ToLower())
				{
					ToRemove.Add(Client.ClientId);
				}
			}
		}
		for (int i = 0; i < ToRemove.Count; i++)
		{
			Clients[ToRemove[i]].Disconnect();
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
		lock (Clients)
		{
			Dictionary<uint, GameClient>.Enumerator eClients = Clients.GetEnumerator();
			while (eClients.MoveNext())
			{
				GameClient Client = eClients.Current.Value;
				if (Client.GetHabbo() != null)
				{
					int newCredits = 0;
					using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
					{
						newCredits = (int)dbClient.ReadDataRow("SELECT credits FROM users WHERE id = '" + Client.GetHabbo().Id + "' LIMIT 1")[0];
					}
					int oldBalance = Client.GetHabbo().Credits;
					Client.GetHabbo().Credits = newCredits;
					if (oldBalance < 3000)
					{
						Client.GetHabbo().UpdateCreditsBalance(InDatabase: false);
						Client.SendNotif("Atualização de Créditos" + Convert.ToChar(13) + "-----------------------------------" + Convert.ToChar(13) + "Acabamos de encher os seus créditos até 3000 - divirta-se! A atualização de créditos próxima ocorrerá em três horas.");
					}
					else if (oldBalance >= 3000)
					{
						Client.SendNotif("Atualização de Créditos" + Convert.ToChar(13) + "-----------------------------------" + Convert.ToChar(13) + "Desculpe! Porque o seu saldo é de 3000 ou superior, mas não temos encher os seus créditos. A atualização de créditos próxima ocorrerá em três horas.");
					}
				}
			}
		}
	}

	public void CheckForAllBanConflicts()
	{
		Dictionary<GameClient, ModerationBanException> ConflictsFound = new Dictionary<GameClient, ModerationBanException>();
		lock (Clients)
		{
			Dictionary<uint, GameClient>.Enumerator eClients = Clients.GetEnumerator();
			while (eClients.MoveNext())
			{
				GameClient Client = eClients.Current.Value;
				try
				{
					HolographEnvironment.GetGame().GetBanManager().CheckForBanConflicts(Client);
				}
				catch (ModerationBanException value)
				{
					ConflictsFound.Add(Client, value);
				}
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
			lock (Clients)
			{
				Dictionary<uint, GameClient>.Enumerator eClients = Clients.GetEnumerator();
				while (eClients.MoveNext())
				{
					GameClient Client = eClients.Current.Value;
					if (Client.GetHabbo() != null && HolographEnvironment.GetGame().GetPixelManager().NeedsUpdate(Client))
					{
						HolographEnvironment.GetGame().GetPixelManager().GivePixels(Client);
					}
				}
			}
		}
		catch (Exception ex)
		{
			HolographEnvironment.GetLogging().WriteLine("[Zero.CheckPixelUpdates]: " + ex.Message);
		}
	}

	public GameClientManager()
	{
		Clients = new Dictionary<uint, GameClient>();
	}

	public void Clear()
	{
		lock (Clients)
		{
			Clients.Clear();
		}
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
		lock (Clients)
		{
			return Clients.Remove(ClientId);
		}
	}

	public void StartClient(uint ClientId)
	{
		lock (Clients)
		{
			Clients.Add(ClientId, new GameClient(ClientId));
			Clients[ClientId].StartConnection();
		}
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
			//bool flag = true;
			ServerMessage PingMessage = new ServerMessage(50u);
			try
			{
				List<uint> TimedOutClients = new List<uint>();
				List<GameClient> ToPing = new List<GameClient>();
				lock (Clients)
				{
					Dictionary<uint, GameClient>.Enumerator eClients = Clients.GetEnumerator();
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
					catch (Exception)
					{
					}
				}
				Thread.Sleep(interval);
			}
			catch (ThreadAbortException)
			{
			}
		}
	}

	public GameClient GetClientByHabbo(uint HabboId)
	{
		lock (Clients)
		{
			Dictionary<uint, GameClient>.Enumerator eClients = Clients.GetEnumerator();
			while (eClients.MoveNext())
			{
				GameClient Client = eClients.Current.Value;
				if (Client.GetHabbo() == null || Client.GetHabbo().Id != HabboId)
				{
					continue;
				}
				return Client;
			}
		}
		return null;
	}

	public GameClient GetClientByHabbo(string Name)
	{
		lock (Clients)
		{
			Dictionary<uint, GameClient>.Enumerator eClients = Clients.GetEnumerator();
			while (eClients.MoveNext())
			{
				GameClient Client = eClients.Current.Value;
				if (Client.GetHabbo() == null || !(Client.GetHabbo().Username.ToLower() == Name.ToLower()))
				{
					continue;
				}
				return Client;
			}
		}
		return null;
	}

	public void BroadcastMessage(ServerMessage Message)
	{
		BroadcastMessage(Message, "");
	}

	public void BroadcastMessage(ServerMessage Message, string FuseRequirement)
	{
		lock (Clients)
		{
			Dictionary<uint, GameClient>.Enumerator eClients = Clients.GetEnumerator();
			while (eClients.MoveNext())
			{
				GameClient Client = eClients.Current.Value;
				try
				{
					if (FuseRequirement.Length <= 0 || (Client.GetHabbo() != null && Client.GetHabbo().HasFuse(FuseRequirement)))
					{
						Client.SendMessage(Message);
					}
				}
				catch (Exception)
				{
				}
			}
		}
	}

	public void CheckEffects()
	{
		lock (Clients)
		{
			Dictionary<uint, GameClient>.Enumerator eClients = Clients.GetEnumerator();
			while (eClients.MoveNext())
			{
				GameClient Client = eClients.Current.Value;
				if (Client.GetHabbo() != null && Client.GetHabbo().GetAvatarEffectsInventoryComponent() != null)
				{
					Client.GetHabbo().GetAvatarEffectsInventoryComponent().CheckExpired();
				}
			}
		}
	}
}
