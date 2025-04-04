using Zero.Hotel.GameClients;
using Zero.Messages;
using Zero.Storage;

namespace Zero.Hotel.Users.Messenger;

internal class MessengerRequest
{
	private uint xRequestId;

	private uint ToUser;

	private uint FromUser;

	public uint RequestId => FromUser;

	public uint To => ToUser;

	public uint From => FromUser;

	public string SenderUsername
	{
		get
		{
			GameClient Client = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(From);
			if (Client != null)
			{
				return Client.GetHabbo().Username;
			}
			using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
			return dbClient.ReadString("SELECT username FROM users WHERE id = '" + From + "' LIMIT 1");
		}
	}

	public MessengerRequest(uint RequestId, uint ToUser, uint FromUser)
	{
		xRequestId = RequestId;
		this.ToUser = ToUser;
		this.FromUser = FromUser;
	}

	public void Serialize(ServerMessage Request)
	{
		Request.AppendUInt(FromUser);
		Request.AppendStringWithBreak(SenderUsername);
		Request.AppendStringWithBreak(FromUser.ToString());
	}
}
