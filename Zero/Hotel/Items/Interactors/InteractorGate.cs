using System;
using System.Collections.Generic;
using Zero.Hotel.GameClients;
using Zero.Hotel.Rooms;

namespace Zero.Hotel.Items.Interactors;

internal class InteractorGate : FurniInteractor
{
	private int Modes;

	public InteractorGate(int Modes)
	{
		this.Modes = Modes - 1;
		if (this.Modes < 0)
		{
			this.Modes = 0;
		}
	}

	public override void OnPlace(GameClient Session, RoomItem Item)
	{
	}

	public override void OnRemove(GameClient Session, RoomItem Item)
	{
	}

	public override void OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
	{
		if (!UserHasRights)
		{
			return;
		}
		if (Modes == 0)
		{
			Item.UpdateState(inDb: false, inRoom: true);
		}
		int currentMode = 0;
		int newMode = 0;
		try
		{
			currentMode = int.Parse(Item.ExtraData);
		}
		catch (Exception)
		{
		}
		newMode = ((currentMode <= 0) ? 1 : ((currentMode < Modes) ? (currentMode + 1) : 0));
		if (newMode == 0)
		{
			if (Item.GetRoom().SquareHasUsers(Item.X, Item.Y))
			{
				return;
			}
			Dictionary<int, AffectedTile> Points = Item.GetRoom().GetAffectedTiles(Item.GetBaseItem().Length, Item.GetBaseItem().Width, Item.X, Item.Y, Item.Rot);
			if (Points == null)
			{
				Points = new Dictionary<int, AffectedTile>();
			}
			foreach (AffectedTile Tile in Points.Values)
			{
				if (Item.GetRoom().SquareHasUsers(Tile.X, Tile.Y))
				{
					return;
				}
			}
		}
		Item.ExtraData = newMode.ToString();
		Item.UpdateState();
		Item.GetRoom().GenerateMaps();
	}
}
