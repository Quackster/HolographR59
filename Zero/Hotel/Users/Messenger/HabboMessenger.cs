using System.Collections.Generic;
using System.Data;
using Zero.Hotel.GameClients;
using Zero.Messages;
using Zero.Storage;

namespace Zero.Hotel.Users.Messenger;

internal class HabboMessenger
{
	private uint UserId;

	private List<MessengerBuddy> Buddies;

	private List<MessengerRequest> Requests;

	public bool AppearOffline;

	public HabboMessenger(uint UserId)
	{
		Buddies = new List<MessengerBuddy>();
		Requests = new List<MessengerRequest>();
		this.UserId = UserId;
	}

	public void LoadBuddies()
	{
		Buddies = new List<MessengerBuddy>();
		DataTable Data = null;
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			Data = dbClient.ReadDataTable("SELECT user_two_id FROM messenger_friendships WHERE user_one_id = '" + UserId + "'");
		}
		if (Data == null)
		{
			return;
		}
		foreach (DataRow Row in Data.Rows)
		{
			Buddies.Add(new MessengerBuddy((uint)Row["user_two_id"]));
		}
	}

	public void LoadRequests()
	{
		Requests = new List<MessengerRequest>();
		DataTable Data = null;
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			Data = dbClient.ReadDataTable("SELECT * FROM messenger_requests WHERE to_id = '" + UserId + "'");
		}
		if (Data == null)
		{
			return;
		}
		foreach (DataRow Row in Data.Rows)
		{
			Requests.Add(new MessengerRequest((uint)Row["id"], (uint)Row["to_id"], (uint)Row["from_id"]));
		}
	}

	public void ClearBuddies()
	{
		Buddies.Clear();
	}

	public void ClearRequests()
	{
		Requests.Clear();
	}

	public MessengerRequest GetRequest(uint RequestId)
	{
		lock (Requests)
		{
			List<MessengerRequest>.Enumerator eRequests = Requests.GetEnumerator();
			while (eRequests.MoveNext())
			{
				MessengerRequest Request = eRequests.Current;
				if (Request.RequestId == RequestId)
				{
					return Request;
				}
			}
		}
		return null;
	}

	public void OnStatusChanged(bool instantUpdate)
	{
		lock (Buddies)
		{
			List<MessengerBuddy>.Enumerator eBuddies = Buddies.GetEnumerator();
			while (eBuddies.MoveNext())
			{
				MessengerBuddy Buddy = eBuddies.Current;
				GameClient Client = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(Buddy.Id);
				if (Client != null && Client.GetHabbo() != null && Client.GetHabbo().GetMessenger() != null)
				{
					Client.GetHabbo().GetMessenger().SetUpdateNeeded(UserId);
					if (instantUpdate)
					{
						Client.GetHabbo().GetMessenger().ForceUpdate();
					}
				}
			}
		}
	}

	public bool SetUpdateNeeded(uint UserId)
	{
		lock (Buddies)
		{
			List<MessengerBuddy>.Enumerator eBuddies = Buddies.GetEnumerator();
			while (eBuddies.MoveNext())
			{
				MessengerBuddy Buddy = eBuddies.Current;
				if (Buddy.Id == UserId)
				{
					Buddy.UpdateNeeded = true;
					return true;
				}
			}
		}
		return false;
	}

	public void ForceUpdate()
	{
		GetClient().SendMessage(SerializeUpdates());
	}

	public bool RequestExists(uint UserOne, uint UserTwo)
	{
		if (UserOne == UserTwo)
		{
			return true;
		}
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			if (dbClient.ReadDataRow("SELECT * FROM messenger_requests WHERE to_id = '" + UserOne + "' AND from_id = '" + UserTwo + "' LIMIT 1") != null)
			{
				return true;
			}
			if (dbClient.ReadDataRow("SELECT * FROM messenger_requests WHERE to_id = '" + UserTwo + "' AND from_id = '" + UserOne + "' LIMIT 1") != null)
			{
				return true;
			}
		}
		return false;
	}

	public bool FriendshipExists(uint UserOne, uint UserTwo)
	{
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			if (dbClient.ReadDataRow("SELECT * FROM messenger_friendships WHERE user_one_id = '" + UserOne + "' AND user_two_id = '" + UserTwo + "' LIMIT 1") != null)
			{
				return true;
			}
			if (dbClient.ReadDataRow("SELECT * FROM messenger_friendships WHERE user_one_id = '" + UserTwo + "' AND user_two_id = '" + UserOne + "' LIMIT 1") != null)
			{
				return true;
			}
		}
		return false;
	}

	public void HandleAllRequests()
	{
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			dbClient.ExecuteQuery("DELETE FROM messenger_requests WHERE to_id = '" + UserId + "'");
		}
		ClearRequests();
	}

	public void HandleRequest(uint FromId)
	{
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			dbClient.AddParamWithValue("userid", UserId);
			dbClient.AddParamWithValue("fromid", FromId);
			dbClient.ExecuteQuery("DELETE FROM messenger_requests WHERE to_id = @userid AND from_id = @fromid LIMIT 1");
		}
		if (GetRequest(FromId) != null)
		{
			Requests.Remove(GetRequest(FromId));
		}
	}

	public void CreateFriendship(uint UserTwo)
	{
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			dbClient.AddParamWithValue("toid", UserTwo);
			dbClient.AddParamWithValue("userid", UserId);
			dbClient.ExecuteQuery("INSERT INTO messenger_friendships (user_one_id,user_two_id) VALUES (@userid,@toid)");
			dbClient.ExecuteQuery("INSERT INTO messenger_friendships (user_one_id,user_two_id) VALUES (@toid,@userid)");
		}
		OnNewFriendship(UserTwo);
		GameClient User = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(UserTwo);
		if (User != null && User.GetHabbo().GetMessenger() != null)
		{
			User.GetHabbo().GetMessenger().OnNewFriendship(UserId);
		}
	}

	public void DestroyFriendship(uint UserTwo)
	{
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			dbClient.AddParamWithValue("toid", UserTwo);
			dbClient.AddParamWithValue("userid", UserId);
			dbClient.ExecuteQuery("DELETE FROM messenger_friendships WHERE user_one_id = @toid AND user_two_id = @userid LIMIT 1");
			dbClient.ExecuteQuery("DELETE FROM messenger_friendships WHERE user_one_id = @userid AND user_two_id = @toid LIMIT 1");
		}
		OnDestroyFriendship(UserTwo);
		GameClient User = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(UserTwo);
		if (User != null && User.GetHabbo().GetMessenger() != null)
		{
			User.GetHabbo().GetMessenger().OnDestroyFriendship(UserId);
		}
	}

	public void OnNewFriendship(uint Friend)
	{
		MessengerBuddy Buddy = new MessengerBuddy(Friend);
		Buddy.UpdateNeeded = true;
		Buddies.Add(Buddy);
		ForceUpdate();
	}

	public void OnDestroyFriendship(uint Friend)
	{
		lock (Buddies)
		{
			foreach (MessengerBuddy Buddy in Buddies)
			{
				if (Buddy.Id == Friend)
				{
					Buddies.Remove(Buddy);
					break;
				}
			}
		}
		GetClient().GetMessageHandler().GetResponse().Init(13u);
		GetClient().GetMessageHandler().GetResponse().AppendInt32(0);
		GetClient().GetMessageHandler().GetResponse().AppendInt32(1);
		GetClient().GetMessageHandler().GetResponse().AppendInt32(-1);
		GetClient().GetMessageHandler().GetResponse().AppendUInt(Friend);
		GetClient().GetMessageHandler().SendResponse();
	}

	public void RequestBuddy(string UserQuery)
	{
		DataRow Row = null;
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			dbClient.AddParamWithValue("query", UserQuery.ToLower());
			Row = dbClient.ReadDataRow("SELECT id,block_newfriends FROM users WHERE username = @query LIMIT 1");
		}
		if (Row == null)
		{
			return;
		}
		if (HolographEnvironment.EnumToBool(Row["block_newfriends"].ToString()))
		{
			GetClient().GetMessageHandler().GetResponse().Init(260u);
			GetClient().GetMessageHandler().GetResponse().AppendInt32(39);
			GetClient().GetMessageHandler().GetResponse().AppendInt32(3);
			GetClient().GetMessageHandler().SendResponse();
			return;
		}
		uint ToId = (uint)Row["id"];
		if (RequestExists(UserId, ToId))
		{
			return;
		}
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			dbClient.AddParamWithValue("toid", ToId);
			dbClient.AddParamWithValue("userid", UserId);
			dbClient.ExecuteQuery("INSERT INTO messenger_requests (to_id,from_id) VALUES (@toid,@userid)");
		}
		GameClient ToUser = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(ToId);
		if (ToUser != null && ToUser.GetHabbo() != null)
		{
			uint RequestId = 0u;
			using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
			{
				dbClient.AddParamWithValue("toid", ToId);
				dbClient.AddParamWithValue("userid", UserId);
				RequestId = (uint)dbClient.ReadInt32("SELECT id FROM messenger_requests WHERE to_id = @toid AND from_id = @userid ORDER BY id DESC LIMIT 1");
			}
			MessengerRequest Request = new MessengerRequest(RequestId, ToId, UserId);
			ToUser.GetHabbo().GetMessenger().OnNewRequest(RequestId, ToId, UserId);
			ServerMessage NewFriendNotif = new ServerMessage(132u);
			Request.Serialize(NewFriendNotif);
			ToUser.SendMessage(NewFriendNotif);
		}
	}

	public void OnNewRequest(uint Request, uint ToId, uint UserId)
	{
		Requests.Add(new MessengerRequest(Request, ToId, UserId));
	}

	public void SendInstantMessage(uint ToId, string Message)
	{
		if (!FriendshipExists(ToId, UserId))
		{
			DeliverInstantMessageError(6, ToId);
			return;
		}
		GameClient Client = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(ToId);
		if (Client == null || Client.GetHabbo().GetMessenger() == null)
		{
			DeliverInstantMessageError(5, ToId);
			return;
		}
		if (GetClient().GetHabbo().Muted)
		{
			DeliverInstantMessageError(4, ToId);
			return;
		}
		if (Client.GetHabbo().Muted)
		{
			DeliverInstantMessageError(3, ToId);
		}
		Client.GetHabbo().GetMessenger().DeliverInstantMessage(Message, UserId);
	}

	public void DeliverInstantMessage(string Message, uint ConversationId)
	{
		ServerMessage InstantMessage = new ServerMessage(134u);
		InstantMessage.AppendUInt(ConversationId);
		InstantMessage.AppendString(Message);
		GetClient().SendMessage(InstantMessage);
	}

	public void DeliverInstantMessageError(int ErrorId, uint ConversationId)
	{
		ServerMessage Error = new ServerMessage(261u);
		Error.AppendInt32(ErrorId);
		Error.AppendUInt(ConversationId);
		GetClient().SendMessage(Error);
	}

	public ServerMessage SerializeFriends()
	{
		ServerMessage Friends = new ServerMessage(12u);
		Friends.AppendInt32(600);
		Friends.AppendInt32(200);
		Friends.AppendInt32(600);
		Friends.AppendInt32(900);
		Friends.AppendBoolean(Bool: false);
		Friends.AppendInt32(Buddies.Count);
		lock (Buddies)
		{
			foreach (MessengerBuddy Buddy in Buddies)
			{
				Buddy.Serialize(Friends, Search: false);
			}
		}
		return Friends;
	}

	public ServerMessage SerializeUpdates()
	{
		List<MessengerBuddy> UpdateBuddies = new List<MessengerBuddy>();
		int UpdateCount = 0;
		lock (Buddies)
		{
			foreach (MessengerBuddy Buddy in Buddies)
			{
				if (Buddy.UpdateNeeded)
				{
					UpdateCount++;
					UpdateBuddies.Add(Buddy);
					Buddy.UpdateNeeded = false;
				}
			}
		}
		ServerMessage Updates = new ServerMessage(13u);
		Updates.AppendInt32(0);
		Updates.AppendInt32(UpdateCount);
		Updates.AppendInt32(0);
		foreach (MessengerBuddy Buddy in UpdateBuddies)
		{
			Buddy.Serialize(Updates, Search: false);
			Updates.AppendBoolean(Bool: false);
		}
		return Updates;
	}

	public ServerMessage SerializeRequests()
	{
		ServerMessage Reqs = new ServerMessage(314u);
		Reqs.AppendInt32(Requests.Count);
		Reqs.AppendInt32(Requests.Count);
		lock (Requests)
		{
			foreach (MessengerRequest Request in Requests)
			{
				Request.Serialize(Reqs);
			}
		}
		return Reqs;
	}

	public ServerMessage PerformSearch(string SearchQuery)
	{
		DataTable Results = null;
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			dbClient.AddParamWithValue("query", SearchQuery + "%");
			Results = dbClient.ReadDataTable("SELECT id FROM users WHERE username LIKE @query LIMIT 50");
		}
		List<DataRow> friendData = new List<DataRow>();
		List<DataRow> othersData = new List<DataRow>();
		if (Results != null)
		{
			foreach (DataRow Row in Results.Rows)
			{
				if (FriendshipExists(UserId, (uint)Row["id"]))
				{
					friendData.Add(Row);
				}
				else
				{
					othersData.Add(Row);
				}
			}
		}
		ServerMessage Search = new ServerMessage(435u);
		Search.AppendInt32(friendData.Count);
		foreach (DataRow Row in friendData)
		{
			new MessengerBuddy((uint)Row["id"]).Serialize(Search, Search: true);
		}
		Search.AppendInt32(othersData.Count);
		foreach (DataRow Row in othersData)
		{
			new MessengerBuddy((uint)Row["id"]).Serialize(Search, Search: true);
		}
		return Search;
	}

	private GameClient GetClient()
	{
		return HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(UserId);
	}

	public List<MessengerBuddy> GetBuddies()
	{
		return Buddies;
	}
}
