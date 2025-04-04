using Zero.Hotel.GameClients;

namespace Zero.Hotel.Items.Interactors;

internal class InteractorLoveShuffler : FurniInteractor
{
    public override void OnPlace(GameClient Session, RoomItem Item)
    {
        Item.ExtraData = "-1";
    }

    public override void OnRemove(GameClient Session, RoomItem Item)
    {
        Item.ExtraData = "-1";
    }

    public override void OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
    {
        if (UserHasRights && Item.ExtraData != "0")
        {
            Item.ExtraData = "0";
            Item.UpdateState(inDb: false, inRoom: true);
            Item.ReqUpdate(10);
        }
    }
}
