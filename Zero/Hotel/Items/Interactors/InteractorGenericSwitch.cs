using System;
using Zero.Hotel.GameClients;

namespace Zero.Hotel.Items.Interactors;

internal class InteractorGenericSwitch : FurniInteractor
{
	private int Modes;

	public InteractorGenericSwitch(int Modes)
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
		if (UserHasRights && Modes != 0)
		{
			int currentMode = 0;
			// int newMode = 0;
			try
			{
				currentMode = int.Parse(Item.ExtraData);
			}
			catch (Exception)
			{
			}
			Item.ExtraData = ((currentMode <= 0) ? 1 : ((currentMode < Modes) ? (currentMode + 1) : 0)).ToString();
			Item.UpdateState();
		}
	}
}
