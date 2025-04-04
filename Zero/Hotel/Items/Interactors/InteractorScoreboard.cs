using System;
using Zero.Hotel.GameClients;

namespace Zero.Hotel.Items.Interactors;

internal class InteractorScoreboard : FurniInteractor
{
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
		int NewMode = 0;
		try
		{
			NewMode = int.Parse(Item.ExtraData);
		}
		catch (Exception)
		{
		}
		switch (Request)
		{
		case 0:
			if (NewMode <= -1)
			{
				NewMode = 0;
			}
			else if (NewMode >= 0)
			{
				NewMode = -1;
			}
			break;
		case 1:
			NewMode--;
			if (NewMode < 0)
			{
				NewMode = 0;
			}
			break;
		case 2:
			NewMode++;
			if (NewMode >= 100)
			{
				NewMode = 0;
			}
			break;
		}
		Item.ExtraData = NewMode.ToString();
		Item.UpdateState();
	}
}
