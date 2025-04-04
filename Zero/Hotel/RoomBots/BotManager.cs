using System.Collections.Generic;
using System.Data;
using Zero.Storage;

namespace Zero.Hotel.RoomBots;

internal class BotManager
{
	private List<RoomBot> Bots;

	public BotManager()
	{
		Bots = new List<RoomBot>();
	}

	public void LoadBots()
	{
		Bots = new List<RoomBot>();
		DataTable Data = null;
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			Data = dbClient.ReadDataTable("SELECT * FROM bots");
		}
		if (Data == null)
		{
			return;
		}
		foreach (DataRow Row in Data.Rows)
		{
			Bots.Add(new RoomBot((uint)Row["id"], (uint)Row["room_id"], (string)Row["ai_type"], (string)Row["walk_mode"], (string)Row["name"], (string)Row["motto"], (string)Row["look"], (int)Row["x"], (int)Row["y"], (int)Row["z"], (int)Row["rotation"], (int)Row["min_x"], (int)Row["min_y"], (int)Row["max_x"], (int)Row["max_y"]));
		}
	}

	public bool RoomHasBots(uint RoomId)
	{
		return GetBotsForRoom(RoomId).Count >= 1;
	}

	public List<RoomBot> GetBotsForRoom(uint RoomId)
	{
		List<RoomBot> List = new List<RoomBot>();
		lock (Bots)
		{
			foreach (RoomBot Bot in Bots)
			{
				if (Bot.RoomId == RoomId)
				{
					List.Add(Bot);
				}
			}
		}
		return List;
	}

	public RoomBot GetBot(uint BotId)
	{
		lock (Bots)
		{
			foreach (RoomBot Bot in Bots)
			{
				if (Bot.BotId == BotId)
				{
					return Bot;
				}
			}
		}
		return null;
	}
}
