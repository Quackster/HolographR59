using Zero.Hotel.GameClients;

namespace Zero.Hotel.Items.Interactors;

internal class InteractorFballGate : FurniInteractor
{
    public override void OnPlace(GameClient Session, RoomItem Item)
    {
        if (!(Item.ExtraData != "") || !(Item.ExtraData != "0") || !(Item.ExtraData != "1"))
        {
            Item.ExtraData = "hd-180-1.lg-695-62.ch-210-62,hd-600-1.ch-630-62.lg-695-62";
            Item.UpdateState(inDb: true, inRoom: true);
        }
    }

    public override void OnRemove(GameClient Session, RoomItem Item)
    {
    }

    public override void OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
    {
    }
}
