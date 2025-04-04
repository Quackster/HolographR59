using Zero.Hotel.GameClients;
using Zero.Messages;
using Zero.Storage;

namespace Zero.Hotel.Users.Messenger;

internal class MessengerBuddy
{
	private uint UserId;

	public bool UpdateNeeded;

	public uint Id => UserId;

	public string Username
	{
		get
		{
			GameClient Client = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(UserId);
			if (Client != null)
			{
				return Client.GetHabbo().Username;
			}
			using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
			return dbClient.ReadString("SELECT username FROM users WHERE id = '" + UserId + "' LIMIT 1");
		}
	}

	public string RealName
	{
		get
		{
			GameClient Client = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(UserId);
			if (Client != null)
			{
				return Client.GetHabbo().RealName;
			}
			using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
			return dbClient.ReadString("SELECT real_name FROM users WHERE id = '" + UserId + "' LIMIT 1");
		}
	}

	public string Look
	{
		get
		{
			GameClient Client = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(UserId);
			if (Client != null)
			{
				return Client.GetHabbo().Look;
			}
			return "";
		}
	}

	public string Motto
	{
		get
		{
			GameClient Client = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(UserId);
			if (Client != null)
			{
				return Client.GetHabbo().Motto;
			}
			return "";
		}
	}

	public string LastOnline
	{
		get
		{
			GameClient Client = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(UserId);
			if (Client == null)
			{
				using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
				{
					return dbClient.ReadString("SELECT last_online FROM users WHERE id = '" + UserId + "' LIMIT 1");
				}
			}
			return "";
		}
	}

	public bool IsOnline
	{
		get
		{
			GameClient Client = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(UserId);
			if (Client != null && Client.GetHabbo() != null && Client.GetHabbo().GetMessenger() != null && !Client.GetHabbo().GetMessenger().AppearOffline)
			{
				return true;
			}
			return false;
		}
	}

	public bool InRoom
	{
		get
		{
			if (!IsOnline)
			{
				return false;
			}
			GameClient Client = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(UserId);
			if (Client == null)
			{
				return false;
			}
			if (Client.GetHabbo().InRoom)
			{
				return true;
			}
			return false;
		}
	}

	public MessengerBuddy(uint UserId)
	{
		this.UserId = UserId;
		UpdateNeeded = false;
	}

	public void Serialize(ServerMessage Message, bool Search)
	{
		if (Search)
		{
			Message.AppendUInt(UserId);
			Message.AppendStringWithBreak(Username);
			Message.AppendStringWithBreak(Motto);
			Message.AppendBoolean(IsOnline);
			Message.AppendBoolean(InRoom);
			Message.AppendStringWithBreak("");
			Message.AppendBoolean(Bool: false);
			Message.AppendStringWithBreak(Look);
			Message.AppendStringWithBreak(LastOnline);
			Message.AppendStringWithBreak(RealName);
		}
		else
		{
			Message.AppendUInt(UserId);
			Message.AppendStringWithBreak(Username);
			Message.AppendBoolean(Bool: true);
			Message.AppendBoolean(IsOnline);
			Message.AppendBoolean(InRoom);
			Message.AppendStringWithBreak(Look);
			Message.AppendBoolean(Bool: false);
			Message.AppendStringWithBreak(Motto);
			Message.AppendStringWithBreak(LastOnline);
			Message.AppendStringWithBreak(RealName);
		}
	}
}
