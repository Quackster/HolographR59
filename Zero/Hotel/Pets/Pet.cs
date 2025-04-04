using System;
using Zero.Hotel.Rooms;
using Zero.Messages;
using Zero.Storage;
using Zero.Util;

namespace Zero.Hotel.Pets;

internal class Pet
{
	public uint PetId;

	public uint OwnerId;

	public int VirtualId;

	public uint Type;

	public string Name;

	public string Race;

	public string Color;

	public int Expirience;

	public int Energy;

	public int Nutrition;

	public uint RoomId;

	public int X;

	public int Y;

	public double Z;

	public int Respect;

	public double CreationStamp;

	public bool PlacedInRoom;

	public int[] experienceLevels = new int[17]
	{
		100, 200, 400, 600, 1000, 1300, 1800, 2400, 3200, 4300,
		7200, 8500, 10100, 13300, 17500, 23000, 51900
	};

	public Room Room
	{
		get
		{
			if (!IsInRoom)
			{
				return null;
			}
			return HolographEnvironment.GetGame().GetRoomManager().GetRoom(RoomId);
		}
	}

	public bool IsInRoom => RoomId != 0;

	public int Level
	{
		get
		{
			for (int level = 0; level < experienceLevels.Length; level++)
			{
				if (Expirience < experienceLevels[level])
				{
					return level + 1;
				}
			}
			return experienceLevels.Length + 1;
		}
	}

	public int MaxLevel => 20;

	public int ExpirienceGoal => experienceLevels[Level - 1];

	public int MaxEnergy
	{
		get
		{
			int Level1 = Level * 20;
			return Level1 + 100;
		}
	}

	public int MaxNutrition
	{
		get
		{
			int Level2 = Level * 20;
			return Level2 + 100;
		}
	}

	public int Age => (int)Math.Floor((HolographEnvironment.GetUnixTimestamp() - CreationStamp) / 86400.0);

	public string Look => Type + " " + Race + " " + Color;

	public string OwnerName => HolographEnvironment.GetGame().GetClientManager().GetNameById(OwnerId);

	public int BasketX
	{
		get
		{
			try
			{
				using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
				dbClient.AddParamWithValue("rID", RoomId);
				return dbClient.ReadInt32("SELECT x FROM room_items WHERE room_id = @rID AND base_item = 317");
			}
			catch
			{
				return 0;
			}
		}
	}

	public int BasketY
	{
		get
		{
			try
			{
				using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
				dbClient.AddParamWithValue("rID", RoomId);
				return dbClient.ReadInt32("SELECT y FROM room_items WHERE room_id = @rID AND base_item = 317");
			}
			catch
			{
				return 0;
			}
		}
	}

	public Pet(uint PetId, uint OwnerId, uint RoomId, string Name, uint Type, string Race, string Color, int Expirience, int Energy, int Nutrition, int Respect, double CreationStamp, int X, int Y, double Z)
	{
		this.PetId = PetId;
		this.OwnerId = OwnerId;
		this.RoomId = RoomId;
		this.Name = Name;
		this.Type = Type;
		this.Race = Race;
		this.Color = Color;
		this.Expirience = Expirience;
		this.Energy = Energy;
		this.Nutrition = Nutrition;
		this.Respect = Respect;
		this.CreationStamp = CreationStamp;
		this.X = X;
		this.Y = Y;
		this.Z = Z;
		PlacedInRoom = false;
	}

	public void OnRespect()
	{
		Respect++;
		ServerMessage Message = new ServerMessage(440u);
		Message.AppendUInt(PetId);
		Message.AppendInt32(Expirience + 10);
		Room.SendMessage(Message);
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			dbClient.AddParamWithValue("petid", PetId);
			dbClient.ExecuteQuery("Update user_pets SET respect = respect + 1 WHERE id = @petid LIMIT 1");
		}
		if (Expirience <= 51900)
		{
			AddExpirience(10);
		}
	}

	public void AddExpirience(int Amount)
	{
		Expirience += Amount;
		if (Expirience >= 51900)
		{
			return;
		}
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			dbClient.AddParamWithValue("petid", PetId);
			dbClient.AddParamWithValue("expirience", Expirience);
			dbClient.ExecuteQuery("Update user_pets SET expirience = @expirience WHERE id = @petid LIMIT 1");
		}
		if (Room != null)
		{
			ServerMessage Message = new ServerMessage(609u);
			Message.AppendUInt(PetId);
			Message.AppendInt32(VirtualId);
			Message.AppendInt32(Amount);
			Room.SendMessage(Message);
			if (Expirience > ExpirienceGoal)
			{
				ServerMessage ChatMessage = new ServerMessage(24u);
				ChatMessage.AppendInt32(VirtualId);
				ChatMessage.AppendStringWithBreak("*He subido al nivel " + Level + " *");
				ChatMessage.AppendInt32(0);
				Room.SendMessage(ChatMessage);
			}
		}
	}

	public void PetEnergy(bool Add)
	{
		int MaxE;
		if (Add)
		{
			if (Energy == 100)
			{
				return;
			}
			MaxE = ((Energy <= 85) ? 10 : (MaxEnergy - Energy));
		}
		else
		{
			MaxE = 15;
		}
		int r = HolographEnvironment.GetRandomNumber(4, MaxE);
		using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
		if (!Add)
		{
			Energy -= r;
			if (Energy < 0)
			{
				dbClient.AddParamWithValue("pid", PetId);
				dbClient.ExecuteQuery("Update user_pets SET energy = 1 WHERE id = @pid LIMIT 1");
				Energy = 1;
				r = 1;
			}
			dbClient.AddParamWithValue("r", r);
			dbClient.AddParamWithValue("petid", PetId);
			dbClient.ExecuteQuery("Update user_pets SET energy = energy - @r WHERE id = @petid LIMIT 1");
		}
		else
		{
			dbClient.AddParamWithValue("r", r);
			dbClient.AddParamWithValue("petid", PetId);
			dbClient.ExecuteQuery("Update user_pets SET energy = energy + @r WHERE id = @petid LIMIT 1");
			Energy += r;
		}
	}

	public void SerializeInventory(ServerMessage Message)
	{
		Message.AppendUInt(PetId);
		Message.AppendStringWithBreak(Name);
		Message.AppendStringWithBreak(Look);
		Message.AppendBoolean(Bool: false);
	}

	public ServerMessage SerializeInfo()
	{
		ServerMessage Nfo = new ServerMessage(601u);
		Nfo.AppendUInt(PetId);
		Nfo.AppendStringWithBreak(Name);
		Nfo.AppendInt32(Level);
		Nfo.AppendInt32(MaxLevel);
		Nfo.AppendInt32(Expirience);
		Nfo.AppendInt32(ExpirienceGoal);
		Nfo.AppendInt32(Energy);
		Nfo.AppendInt32(MaxEnergy);
		Nfo.AppendInt32(Nutrition);
		Nfo.AppendInt32(MaxNutrition);
		Nfo.AppendStringWithBreak(Look);
		Nfo.AppendInt32(Respect);
		Nfo.AppendUInt(OwnerId);
		Nfo.AppendInt32(Age);
		Nfo.AppendStringWithBreak(OwnerName);
		return Nfo;
	}

	public ServerMessage MyInventory()
	{
		int Inventory = OldEncoding.decodeB64("I]" + PetId + "PBHIJKPAQARASA");
		int uInventory = Convert.ToInt32(PetId);
		ServerMessage PetInventory = new ServerMessage(605u);
		PetInventory.AppendUInt(PetId);
		if (Level == 1)
		{
			PetInventory.AppendString("JHI");
		}
		if (Level == 2)
		{
			PetInventory.AppendString("KHIJ");
		}
		if (Level == 3)
		{
			PetInventory.AppendString("PAHIJK");
		}
		if (Level == 4)
		{
			PetInventory.AppendString("PRAQDHIJKPA");
		}
		if (Level == 5)
		{
			PetInventory.AppendString("PSAQDHIJKPAQA");
		}
		if (Level == 6)
		{
			PetInventory.AppendString("PPBQDHIJKPAQARA");
		}
		if (Level == 8)
		{
			PetInventory.AppendString("PBHIJKPAQARASA\u0001");
		}
		if (Level == 11)
		{
			PetInventory.AppendString("QCHIJKPAQARASAPBQBRBSBQD");
		}
		if (Level == 12)
		{
			PetInventory.AppendString("RCHIJKPAQARASAPBQBRBSBPCQD");
		}
		return PetInventory;
	}
}
