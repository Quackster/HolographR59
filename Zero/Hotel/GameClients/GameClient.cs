using System;
using System.Collections.Generic;
using Zero.Core;
using Zero.Hotel.Misc;
using Zero.Hotel.Support;
using Zero.Hotel.Users;
using Zero.Hotel.Users.Authenticator;
using Zero.Messages;
using Zero.Net;
using Zero.Storage;
using Zero.Util;

namespace Zero.Hotel.GameClients;

internal class GameClient
{
	private uint Id;

	private TcpConnection Connection;

	private GameClientMessageHandler MessageHandler;

	private Habbo Habbo;

	public bool PongOK;

	public uint ClientId => Id;

	public bool LoggedIn
	{
		get
		{
			if (Habbo == null)
			{
				return false;
			}
			return true;
		}
	}

	public GameClient(uint ClientId)
	{
		Id = ClientId;
		Connection = HolographEnvironment.GetConnectionManager().GetConnection(ClientId);
		MessageHandler = new GameClientMessageHandler(this);
	}

	public TcpConnection GetConnection()
	{
		return Connection;
	}

	public GameClientMessageHandler GetMessageHandler()
	{
		return MessageHandler;
	}

	public Habbo GetHabbo()
	{
		return Habbo;
	}

	public void StartConnection()
	{
		if (Connection != null)
		{
			PongOK = true;
			MessageHandler.RegisterGlobal();
			MessageHandler.RegisterHandshake();
			MessageHandler.RegisterHelp();
			TcpConnection.RouteReceivedDataCallback DataRouter = HandleConnectionData;
			Connection.Start(DataRouter);
		}
	}

	public void Login(string AuthTicket)
	{
		try
		{
			Habbo NewHabbo = Authenticator.TryLoginHabbo(AuthTicket);
			HolographEnvironment.GetGame().GetClientManager().LogClonesOut(NewHabbo.Username);
			Habbo = NewHabbo;
			Habbo.LoadData();
		}
		catch (IncorrectLoginException ex)
		{
			SendNotif("Login error: " + ex.Message);
			Disconnect();
			return;
		}
		try
		{
			HolographEnvironment.GetGame().GetBanManager().CheckForBanConflicts(this);
		}
		catch (ModerationBanException ex2)
		{
			SendBanMessage(ex2.Message);
			Disconnect();
			return;
		}
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			dbClient.ExecuteQuery("Update users SET online = '1', auth_ticket = '', ip_last = '" + GetConnection().IPAddress + "' WHERE id = '" + GetHabbo().Id + "' LIMIT 1");
			dbClient.ExecuteQuery("Update user_info SET login_timestamp = '" + HolographEnvironment.GetUnixTimestamp() + "' WHERE user_id = '" + GetHabbo().Id + "' LIMIT 1");
		}
		List<string> Rights = HolographEnvironment.GetGame().GetRoleManager().GetRightsForHabbo(GetHabbo());
		GetMessageHandler().GetResponse().Init(2u);
		GetMessageHandler().GetResponse().AppendInt32(Rights.Count);
		foreach (string Right in Rights)
		{
			GetMessageHandler().GetResponse().AppendStringWithBreak(Right);
		}
		GetMessageHandler().SendResponse();
		if (GetHabbo().HasFuse("fuse_mod"))
		{
			SendMessage(HolographEnvironment.GetGame().GetModerationTool().SerializeTool());
			HolographEnvironment.GetGame().GetModerationTool().SendOpenTickets(this);
		}
		SendMessage(GetHabbo().GetAvatarEffectsInventoryComponent().Serialize());
		MessageHandler.GetResponse().Init(290u);
		MessageHandler.GetResponse().AppendBoolean(Bool: true);
		MessageHandler.GetResponse().AppendBoolean(Bool: false);
		MessageHandler.SendResponse();
		MessageHandler.GetResponse().Init(3u);
		MessageHandler.SendResponse();
		MessageHandler.GetResponse().Init(517u);
		MessageHandler.GetResponse().AppendBoolean(Bool: true);
		MessageHandler.SendResponse();
		if (HolographEnvironment.GetGame().GetPixelManager().NeedsUpdate(this))
		{
			HolographEnvironment.GetGame().GetPixelManager().GivePixels(this);
		}
		MessageHandler.GetResponse().Init(455u);
		MessageHandler.GetResponse().AppendUInt(GetHabbo().HomeRoom);
		MessageHandler.SendResponse();
		MessageHandler.GetResponse().Init(458u);
		MessageHandler.GetResponse().AppendInt32(30);
		MessageHandler.GetResponse().AppendInt32(GetHabbo().FavoriteRooms.Count);
		foreach (uint Id in GetHabbo().FavoriteRooms)
		{
			MessageHandler.GetResponse().AppendUInt(Id);
		}
		MessageHandler.SendResponse();
		HolographEnvironment.GetGame().GetAchievementManager().UnlockAchievement(this, 11u, 1);
		if (GetHabbo().HasFuse("fuse_use_club_badge") && !GetHabbo().GetBadgeComponent().HasBadge("HC1"))
		{
			GetHabbo().GetBadgeComponent().GiveBadge("HC1", InDatabase: true);
		}
		else if (!GetHabbo().HasFuse("fuse_use_club_badge") && GetHabbo().GetBadgeComponent().HasBadge("HC1"))
		{
			GetHabbo().GetBadgeComponent().RemoveBadge("HC1");
		}
		if (GetHabbo().HasFuse("fuse_use_vip_badge") && !GetHabbo().GetBadgeComponent().HasBadge("ACH_VipClub1"))
		{
			GetHabbo().GetBadgeComponent().GiveBadge("ACH_VipClub1", InDatabase: true);
		}
		else if (!GetHabbo().HasFuse("fuse_use_vip_badge") && GetHabbo().GetBadgeComponent().HasBadge("ACH_VipClub1"))
		{
			GetHabbo().GetBadgeComponent().RemoveBadge("ACH_VipClub1");
		}
		if (GetHabbo().Look != "hd-180-1.ch-210-66.lg-270-82.sh-290-91.hr-100-")
		{
			HolographEnvironment.GetGame().GetAchievementManager().UnlockAchievement(this, 1u, 1);
		}
		if (GetHabbo().Tags != null)
		{
			HolographEnvironment.GetGame().GetAchievementManager().UnlockAchievement(this, 7u, 1);
		}
		if (GetHabbo().Motto != null)
		{
			HolographEnvironment.GetGame().GetAchievementManager().UnlockAchievement(this, 5u, 1);
		}
		MessageHandler.RegisterUsers();
		MessageHandler.RegisterMessenger();
		MessageHandler.RegisterCatalog();
		MessageHandler.RegisterNavigator();
		MessageHandler.RegisterRooms();
	}

	public void SendBanMessage(string Message)
	{
		ServerMessage BanMessage = new ServerMessage(35u);
		BanMessage.AppendStringWithBreak("A moderator has kicked you from the hotel:", 13);
		BanMessage.AppendStringWithBreak(Message);
		GetConnection().SendMessage(BanMessage);
	}

	public void SendNotif(string Message)
	{
		SendNotif(Message, FromHotelManager: false);
	}

	public void SendNotif(string Message, bool FromHotelManager)
	{
		ServerMessage nMessage = new ServerMessage();
		if (FromHotelManager)
		{
			nMessage.Init(139u);
		}
		else
		{
			nMessage.Init(161u);
		}
		nMessage.AppendStringWithBreak(Message);
		GetConnection().SendMessage(nMessage);
	}

	public void SendNotif(string Message, string Url)
	{
		ServerMessage nMessage = new ServerMessage(161u);
		nMessage.AppendStringWithBreak(Message);
		nMessage.AppendStringWithBreak(Url);
		GetConnection().SendMessage(nMessage);
	}

	public void Stop()
	{
		if (GetHabbo() != null)
		{
			Habbo.OnDisconnect();
			Habbo = null;
		}
		if (GetConnection() != null)
		{
			Connection = null;
		}
		if (GetMessageHandler() != null)
		{
			MessageHandler.Destroy();
			MessageHandler = null;
		}
	}

	public void Disconnect()
	{
		HolographEnvironment.GetGame().GetClientManager().StopClient(Id);
	}

	public void HandleConnectionData(ref byte[] data)
	{
		if (data[0] == 64)
		{
			int pos = 0;
			while (pos < data.Length)
			{
				try
				{
					int MessageLength = Base64Encoding.DecodeInt32(new byte[3]
					{
						data[pos++],
						data[pos++],
						data[pos++]
					});
					uint MessageId = Base64Encoding.DecodeUInt32(new byte[2]
					{
						data[pos++],
						data[pos++]
					});
					byte[] Content = new byte[MessageLength - 2];
					for (int i = 0; i < Content.Length; i++)
					{
						Content[i] = data[pos++];
					}
					ClientMessage Message = new ClientMessage(MessageId, Content);
					MessageHandler.HandleRequest(Message);
				}
				catch (EntryPointNotFoundException ex)
				{
					HolographEnvironment.GetLogging().WriteLine("User D/C: " + ex.Message, LogLevel.Error);
					Disconnect();
				}
			}
		}
		else
		{
			Connection.SendData(CrossdomainPolicy.GetXmlPolicy());
		}
	}

	public void SendMessage(ServerMessage Message)
	{
		if (Message != null)
		{
			GetConnection().SendMessage(Message);
		}
	}
}
