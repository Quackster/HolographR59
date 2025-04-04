using System;
using Zero.Hotel.GameClients;

namespace Zero.Hotel.Items.Interactors;

internal class InteractorTrax : FurniInteractor
{
	private int Modes;

	public InteractorTrax()
	{
		Modes = 1;
	}

	public override void OnPlace(GameClient Session, RoomItem Item)
	{
	}

	public override void OnRemove(GameClient Session, RoomItem Item)
	{
	}

	public override void OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
	{
		if (UserHasRights)
		{
			int i1 = 0;
			int i2 = 0;
			try
			{
				i1 = int.Parse(Item.ExtraData);
			}
			catch (Exception)
			{
			}
			i2 = ((i1 <= 0) ? 1 : ((i1 < Modes) ? (i1 + 1) : 0));
			Item.ReqUpdate(0);
			Item.ExtraData = i2.ToString();
			Item.UpdateState();
		}
	}
}
