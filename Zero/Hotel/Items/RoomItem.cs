using Zero.Hotel.Items.Interactors;
using Zero.Hotel.Pathfinding;
using Zero.Hotel.Rooms;
using Zero.Messages;
using Zero.Storage;

namespace Zero.Hotel.Items;

internal class RoomItem
{
	public uint Id;

	public uint RoomId;

	public uint BaseItem;

	public string ExtraData;

	public int X;

	public int Y;

	public double Z;

	public int Rot;

	public string WallPos;

	public bool UpdateNeeded;

	public int UpdateCounter;

	public uint InteractingUser;

	public uint InteractingUser2;

	public Coord Coordinate => new Coord(X, Y);

	public double TotalHeight => Z + GetBaseItem().Height;

	public bool IsWallItem
	{
		get
		{
			if (GetBaseItem().Type.ToLower() == "i")
			{
				return true;
			}
			return false;
		}
	}

	public bool IsFloorItem
	{
		get
		{
			if (GetBaseItem().Type.ToLower() == "s")
			{
				return true;
			}
			return false;
		}
	}

	public Coord SquareInFront
	{
		get
		{
			Coord Sq = new Coord(X, Y);
			if (Rot == 0)
			{
				Sq.y--;
			}
			else if (Rot == 2)
			{
				Sq.x++;
			}
			else if (Rot == 4)
			{
				Sq.y++;
			}
			else if (Rot == 6)
			{
				Sq.x--;
			}
			return Sq;
		}
	}

	public Coord SquareBehind
	{
		get
		{
			Coord Sq = new Coord(X, Y);
			if (Rot == 0)
			{
				Sq.y++;
			}
			else if (Rot == 2)
			{
				Sq.x--;
			}
			else if (Rot == 4)
			{
				Sq.y--;
			}
			else if (Rot == 6)
			{
				Sq.x++;
			}
			return Sq;
		}
	}

	public FurniInteractor Interactor => GetBaseItem().InteractionType.ToLower() switch
	{
		"teleport" => new InteractorTeleport(), 
		"bottle" => new InteractorSpinningBottle(), 
		"dice" => new InteractorDice(), 
		"habbowheel" => new InteractorHabboWheel(), 
		"loveshuffler" => new InteractorLoveShuffler(), 
		"onewaygate" => new InteractorOneWayGate(), 
		"alert" => new InteractorAlert(), 
		"vendingmachine" => new InteractorVendor(), 
		"gate" => new InteractorGate(GetBaseItem().Modes), 
		"trax" => new InteractorTrax(), 
		"scoreboard" => new InteractorScoreboard(), 
		_ => new InteractorGenericSwitch(GetBaseItem().Modes), 
	};

	public RoomItem(uint Id, uint RoomId, uint BaseItem, string ExtraData, int X, int Y, double Z, int Rot, string WallPos)
	{
		this.Id = Id;
		this.RoomId = RoomId;
		this.BaseItem = BaseItem;
		this.ExtraData = ExtraData;
		this.X = X;
		this.Y = Y;
		this.Z = Z;
		this.Rot = Rot;
		this.WallPos = WallPos;
		UpdateNeeded = false;
		UpdateCounter = 0;
		InteractingUser = 0u;
		InteractingUser2 = 0u;
		string text = GetBaseItem().InteractionType.ToLower();
		if (text != null && text == "teleport")
		{
			ReqUpdate(0);
		}
	}

	public void ProcessUpdates()
	{
		UpdateCounter--;
		if (UpdateCounter > 0)
		{
			return;
		}
		UpdateNeeded = false;
		UpdateCounter = 0;
		RoomUser User = null;
		RoomUser User2 = null;
		switch (GetBaseItem().InteractionType.ToLower())
		{
		case "onewaygate":
			User = null;
			if (InteractingUser != 0)
			{
				User = GetRoom().GetRoomUserByHabbo(InteractingUser);
			}
			if (User != null && User.X == X && User.Y == Y)
			{
				ExtraData = "1";
				User.MoveTo(SquareBehind);
				ReqUpdate(0);
				UpdateState(inDb: false, inRoom: true);
			}
			else if (User != null && User.Coordinate == SquareBehind)
			{
				User.UnlockWalking();
				ExtraData = "0";
				InteractingUser = 0u;
				UpdateState(inDb: false, inRoom: true);
			}
			else if (ExtraData == "1")
			{
				ExtraData = "0";
				UpdateState(inDb: false, inRoom: true);
			}
			if (User == null)
			{
				InteractingUser = 0u;
			}
			break;
		case "teleport":
		{
			User = null;
			User2 = null;
			bool keepDoorOpen = false;
			bool showTeleEffect = false;
			if (InteractingUser != 0)
			{
				User = GetRoom().GetRoomUserByHabbo(InteractingUser);
				if (User != null)
				{
					if (User.Coordinate == Coordinate)
					{
						if (User.TeleDelay == -1)
						{
							User.TeleDelay = 1;
						}
						if (TeleHandler.IsTeleLinked(Id))
						{
							showTeleEffect = true;
							if (User.TeleDelay == 0)
							{
								uint TeleId = TeleHandler.GetLinkedTele(Id);
								uint RoomId = TeleHandler.GetTeleRoomId(TeleId);
								if (RoomId == this.RoomId)
								{
									RoomItem Item = GetRoom().GetItem(TeleId);
									if (Item == null)
									{
										User.UnlockWalking();
									}
									else
									{
										User.SetPos(Item.X, Item.Y, Item.Z);
										User.SetRot(Item.Rot);
										Item.ExtraData = "2";
										Item.UpdateState(inDb: false, inRoom: true);
										Item.InteractingUser2 = InteractingUser;
									}
								}
								else
								{
									HolographEnvironment.GetGame().GetRoomManager().AddTeleAction(new TeleUserData(User, RoomId, TeleId));
								}
								InteractingUser = 0u;
							}
							else
							{
								User.TeleDelay--;
							}
						}
						else
						{
							keepDoorOpen = true;
							User.UnlockWalking();
							InteractingUser = 0u;
							User.MoveTo(SquareInFront);
						}
					}
					else if (User.Coordinate == SquareInFront)
					{
						keepDoorOpen = true;
						if (User.IsWalking && (User.GoalX != X || User.GoalY != Y))
						{
							User.ClearMovement(Update: true);
						}
						User.CanWalk = false;
						User.AllowOverride = true;
						User.MoveTo(Coordinate);
					}
					else
					{
						InteractingUser = 0u;
					}
				}
				else
				{
					InteractingUser = 0u;
				}
			}
			if (InteractingUser2 != 0)
			{
				User2 = GetRoom().GetRoomUserByHabbo(InteractingUser2);
				if (User2 != null)
				{
					keepDoorOpen = true;
					User2.UnlockWalking();
					User2.MoveTo(SquareInFront);
				}
				InteractingUser2 = 0u;
			}
			if (keepDoorOpen)
			{
				if (ExtraData != "1")
				{
					ExtraData = "1";
					UpdateState(inDb: false, inRoom: true);
				}
			}
			else if (showTeleEffect)
			{
				if (ExtraData != "2")
				{
					ExtraData = "2";
					UpdateState(inDb: false, inRoom: true);
				}
			}
			else if (ExtraData != "0")
			{
				ExtraData = "0";
				UpdateState(inDb: false, inRoom: true);
			}
			ReqUpdate(1);
			break;
		}
		case "bottle":
			ExtraData = HolographEnvironment.GetRandomNumber(0, 7).ToString();
			UpdateState();
			break;
		case "dice":
			ExtraData = HolographEnvironment.GetRandomNumber(1, 6).ToString();
			UpdateState();
			break;
		case "habbowheel":
			ExtraData = HolographEnvironment.GetRandomNumber(1, 10).ToString();
			UpdateState();
			break;
		case "loveshuffler":
			if (ExtraData == "0")
			{
				ExtraData = HolographEnvironment.GetRandomNumber(1, 4).ToString();
				ReqUpdate(20);
			}
			else if (ExtraData != "-1")
			{
				ExtraData = "-1";
			}
			UpdateState(inDb: false, inRoom: true);
			break;
		case "alert":
			if (ExtraData == "1")
			{
				ExtraData = "0";
				UpdateState(inDb: false, inRoom: true);
			}
			break;
		case "vendingmachine":
			if (ExtraData == "1")
			{
				User = GetRoom().GetRoomUserByHabbo(InteractingUser);
				if (User != null && User.Coordinate == SquareInFront)
				{
					int randomDrink = GetBaseItem().VendingIds[HolographEnvironment.GetRandomNumber(0, GetBaseItem().VendingIds.Count - 1)];
					User.CarryItem(randomDrink);
				}
				InteractingUser = 0u;
				ExtraData = "0";
				User.UnlockWalking();
				UpdateState(inDb: false, inRoom: true);
			}
			break;
		}
	}

	public void ReqUpdate(int Cycles)
	{
		UpdateCounter = Cycles;
		UpdateNeeded = true;
	}

	public void UpdateState()
	{
		UpdateState(inDb: true, inRoom: true);
	}

	public void UpdateState(bool inDb, bool inRoom)
	{
		if (inDb)
		{
			using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
			dbClient.AddParamWithValue("extra_data", ExtraData);
			dbClient.ExecuteQuery("Update room_items SET extra_data = @extra_data WHERE id = '" + Id + "' LIMIT 1");
		}
		if (inRoom)
		{
			ServerMessage Message = new ServerMessage();
			if (IsFloorItem)
			{
				Message.Init(88u);
				Message.AppendStringWithBreak(Id.ToString());
				Message.AppendStringWithBreak(ExtraData);
			}
			else
			{
				Message.Init(85u);
				Serialize(Message);
			}
			GetRoom().SendMessage(Message);
		}
	}

	public void Serialize(ServerMessage Message)
	{
		if (IsFloorItem)
		{
			Message.AppendUInt(Id);
			Message.AppendInt32(GetBaseItem().SpriteId);
			Message.AppendInt32(X);
			Message.AppendInt32(Y);
			Message.AppendInt32(Rot);
			Message.AppendStringWithBreak(Z.ToString().Replace(',', '.'));
			Message.AppendInt32(0);
			Message.AppendStringWithBreak(ExtraData);
			Message.AppendInt32(-1);
		}
		else if (IsWallItem)
		{
			Message.AppendStringWithBreak(string.Concat(Id));
			Message.AppendInt32(GetBaseItem().SpriteId);
			Message.AppendStringWithBreak(WallPos);
			string text = GetBaseItem().InteractionType.ToLower();
			if (text != null && text == "postit")
			{
				Message.AppendStringWithBreak(ExtraData.Split(' ')[0]);
			}
			else
			{
				Message.AppendStringWithBreak(ExtraData);
			}
		}
	}

	public Item GetBaseItem()
	{
		return HolographEnvironment.GetGame().GetItemManager().GetItem(BaseItem);
	}

	public Room GetRoom()
	{
		return HolographEnvironment.GetGame().GetRoomManager().GetRoom(RoomId);
	}
}
