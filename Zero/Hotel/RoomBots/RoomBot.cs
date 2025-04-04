using System.Collections.Generic;
using System.Data;
using Zero.Storage;

namespace Zero.Hotel.RoomBots;

internal class RoomBot
{
	public uint BotId;

	public uint RoomId;

	public string AiType;

	public string WalkingMode;

	public string Name;

	public string Motto;

	public string Look;

	public int X;

	public int Y;

	public int Z;

	public int Rot;

	public int minX;

	public int maxX;

	public int minY;

	public int maxY;

	public List<RandomSpeech> RandomSpeech;

	public List<BotResponse> Responses;

	public bool IsPet => AiType.ToLower() == "pet";

	public RoomBot(uint BotId, uint RoomId, string AiType, string WalkingMode, string Name, string Motto, string Look, int X, int Y, int Z, int Rot, int minX, int minY, int maxX, int maxY)
	{
		this.BotId = BotId;
		this.RoomId = RoomId;
		this.AiType = AiType;
		this.WalkingMode = WalkingMode;
		this.Name = Name;
		this.Motto = Motto;
		this.Look = Look;
		this.X = X;
		this.Y = Y;
		this.Z = Z;
		this.Rot = Rot;
		this.minX = minX;
		this.minY = minY;
		this.maxX = maxX;
		this.maxY = maxY;
		LoadRandomSpeech();
		LoadResponses();
	}

	public void LoadRandomSpeech()
	{
		RandomSpeech = new List<RandomSpeech>();
		DataTable Data = null;
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			Data = dbClient.ReadDataTable("SELECT * FROM bots_speech WHERE bot_id = '" + BotId + "'");
		}
		if (Data == null)
		{
			return;
		}
		foreach (DataRow Row in Data.Rows)
		{
			RandomSpeech.Add(new RandomSpeech((string)Row["text"], HolographEnvironment.EnumToBool(Row["shout"].ToString())));
		}
	}

	public void LoadResponses()
	{
		Responses = new List<BotResponse>();
		DataTable Data = null;
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			Data = dbClient.ReadDataTable("SELECT * FROM bots_responses WHERE bot_id = '" + BotId + "'");
		}
		if (Data == null)
		{
			return;
		}
		foreach (DataRow Row in Data.Rows)
		{
			Responses.Add(new BotResponse((uint)Row["id"], (uint)Row["bot_id"], (string)Row["keywords"], (string)Row["response_text"], Row["mode"].ToString(), (int)Row["serve_id"]));
		}
	}

	public BotResponse GetResponse(string Message)
	{
		lock (Responses)
		{
			foreach (BotResponse Response in Responses)
			{
				if (Response.KeywordMatched(Message))
				{
					return Response;
				}
			}
		}
		return null;
	}

	public RandomSpeech GetRandomSpeech()
	{
		return RandomSpeech[HolographEnvironment.GetRandomNumber(0, RandomSpeech.Count - 1)];
	}

	public BotAI GenerateBotAI(int VirtualId)
	{
		return AiType.ToLower() switch
		{
			"guide" => new GuideBot(), 
			"pet" => new PetBot(VirtualId), 
			_ => new GenericBot(VirtualId), 
		};
	}
}
