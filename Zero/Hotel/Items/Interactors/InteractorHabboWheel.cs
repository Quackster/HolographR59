using Zero.Hotel.GameClients;

namespace Zero.Hotel.Items.Interactors;

internal class InteractorHabboWheel : FurniInteractor
{
	public override void OnPlace(GameClient Session, RoomItem Item)
	{
		Item.ExtraData = "-1";
		Item.ReqUpdate(10);
	}

	public override void OnRemove(GameClient Session, RoomItem Item)
	{
		Item.ExtraData = "-1";
	}

	public override void OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
	{
		if (UserHasRights && Item.ExtraData != "-1")
		{
			Item.ExtraData = "-1";
			Item.UpdateState();
			Item.ReqUpdate(10);
		}
	}
}
