using Zero.Hotel.GameClients;

namespace Zero.Hotel.Items.Interactors;

internal class InteractorSpinningBottle : FurniInteractor
{
	public override void OnPlace(GameClient Session, RoomItem Item)
	{
		Item.ExtraData = "0";
		Item.UpdateState(inDb: true, inRoom: false);
	}

	public override void OnRemove(GameClient Session, RoomItem Item)
	{
		Item.ExtraData = "0";
	}

	public override void OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
	{
		if (Item.ExtraData != "-1")
		{
			Item.ExtraData = "-1";
			Item.UpdateState(inDb: false, inRoom: true);
			Item.ReqUpdate(3);
		}
	}
}
