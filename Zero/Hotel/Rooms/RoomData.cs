using System.Collections.Generic;
using System.Data;
using Zero.Messages;

namespace Zero.Hotel.Rooms;

internal class RoomData
{
	public uint Id;

	public string Name;

	public string Description;

	public string Type;

	public string Owner;

	public string Password;

	public int State;

	public int Category;

	public int UsersNow;

	public int UsersMax;

	public string ModelName;

	public string CCTs;

	public int Score;

	public List<string> Tags;

	public bool AllowPets;

	public bool AllowPetsEating;

	public bool AllowWalkthrough;

	public bool Hidewall;

	private RoomIcon myIcon;

	public RoomEvent Event;

	public string Wallpaper;

	public string Floor;

	public string Landscape;

	public bool IsPublicRoom
	{
		get
		{
			if (Type.ToLower() == "public")
			{
				return true;
			}
			return false;
		}
	}

	public RoomIcon Icon => myIcon;

	public int TagCount => Tags.Count;

	public RoomModel Model => HolographEnvironment.GetGame().GetRoomManager().GetModel(ModelName);

	public void FillNull(uint Id)
	{
		this.Id = Id;
		Name = "Unknown Room";
		Description = "-";
		Type = "private";
		Owner = "-";
		Category = 0;
		UsersNow = 0;
		UsersMax = 0;
		ModelName = "NO_MODEL";
		CCTs = "";
		Score = 0;
		Tags = new List<string>();
		AllowPets = true;
		AllowPetsEating = false;
		AllowWalkthrough = true;
		Hidewall = false;
		Password = "";
		Wallpaper = "0.0";
		Floor = "0.0";
		Landscape = "0.0";
		Event = null;
		myIcon = new RoomIcon(1, 1, new Dictionary<int, int>());
	}

	public void Fill(DataRow Row)
	{
		Id = (uint)Row["id"];
		Name = (string)Row["caption"];
		Description = (string)Row["description"];
		Type = (string)Row["roomtype"];
		Owner = (string)Row["owner"];
		switch (Row["state"].ToString().ToLower())
		{
		case "open":
			State = 0;
			break;
		case "password":
			State = 2;
			break;
		default:
			State = 1;
			break;
		}
		Category = (int)Row["category"];
		UsersNow = (int)Row["users_now"];
		UsersMax = (int)Row["users_max"];
		ModelName = (string)Row["model_name"];
		CCTs = (string)Row["public_ccts"];
		Score = (int)Row["score"];
		Tags = new List<string>();
		AllowPets = HolographEnvironment.EnumToBool(Row["allow_pets"].ToString());
		AllowPetsEating = HolographEnvironment.EnumToBool(Row["allow_pets_eat"].ToString());
		AllowWalkthrough = HolographEnvironment.EnumToBool(Row["allow_walkthrough"].ToString());
		Hidewall = HolographEnvironment.EnumToBool(Row["allow_hidewall"].ToString());
		Password = (string)Row["password"];
		Wallpaper = (string)Row["wallpaper"];
		Floor = (string)Row["floor"];
		Landscape = (string)Row["landscape"];
		Event = null;
		Dictionary<int, int> IconItems = new Dictionary<int, int>();
		string[] array;
		if (Row["icon_items"].ToString() != "")
		{
			array = Row["icon_items"].ToString().Split('|');
			foreach (string Bit in array)
			{
				IconItems.Add(int.Parse(Bit.Split(',')[0]), int.Parse(Bit.Split(',')[1]));
			}
		}
		myIcon = new RoomIcon((int)Row["icon_bg"], (int)Row["icon_fg"], IconItems);
		array = Row["tags"].ToString().Split(',');
		foreach (string Tag in array)
		{
			Tags.Add(Tag);
		}
	}

	public void Fill(Room Room)
	{
		Id = Room.RoomId;
		Name = Room.Name;
		Description = Room.Description;
		Type = Room.Type;
		Owner = Room.Owner;
		Category = Room.Category;
		State = Room.State;
		UsersNow = Room.UsersNow;
		UsersMax = Room.UsersMax;
		ModelName = Room.ModelName;
		CCTs = Room.CCTs;
		Score = Room.Score;
		Tags = Room.Tags;
		AllowPets = Room.AllowPets;
		AllowPetsEating = Room.AllowPetsEating;
		AllowWalkthrough = Room.AllowWalkthrough;
		myIcon = Room.Icon;
		Password = Room.Password;
		Event = Room.Event;
		Wallpaper = Room.Wallpaper;
		Floor = Room.Floor;
		Landscape = Room.Landscape;
	}

	public void Serialize(ServerMessage Message, bool ShowEvents)
	{
		Message.AppendUInt(Id);
		if (Event == null || !ShowEvents)
		{
			Message.AppendBoolean(Bool: false);
			Message.AppendStringWithBreak(Name);
			Message.AppendStringWithBreak(Owner);
			Message.AppendInt32(State);
			Message.AppendInt32(UsersNow);
			Message.AppendInt32(UsersMax);
			Message.AppendStringWithBreak(Description);
			Message.AppendBoolean(Bool: true);
			Message.AppendBoolean(Bool: true);
			Message.AppendInt32(Score);
			Message.AppendInt32(Category);
			Message.AppendStringWithBreak("");
			Message.AppendInt32(TagCount);
			foreach (string Tag in Tags)
			{
				Message.AppendStringWithBreak(Tag);
			}
		}
		else
		{
			Message.AppendBoolean(Bool: true);
			Message.AppendStringWithBreak(Event.Name);
			Message.AppendStringWithBreak(Owner);
			Message.AppendInt32(State);
			Message.AppendInt32(UsersNow);
			Message.AppendInt32(UsersMax);
			Message.AppendStringWithBreak(Event.Description);
			Message.AppendBoolean(Bool: true);
			Message.AppendBoolean(Bool: true);
			Message.AppendInt32(Score);
			Message.AppendInt32(Event.Category);
			Message.AppendStringWithBreak(Event.StartTime);
			Message.AppendInt32(Event.Tags.Count);
			foreach (string Tag in Event.Tags)
			{
				Message.AppendStringWithBreak(Tag);
			}
		}
		Icon.Serialize(Message);
		Message.AppendBoolean(Bool: true);
	}
}
