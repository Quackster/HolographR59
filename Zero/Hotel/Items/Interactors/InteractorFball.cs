using Zero.Hotel.GameClients;
using Zero.Hotel.Rooms;

namespace Zero.Hotel.Items.Interactors;

internal class InteractorFball : FurniInteractor
{
    public override void OnPlace(GameClient Session, RoomItem Item)
    {
        Item.ExtraData = "0\ufffd";
        Item.UpdateState(inDb: true, inRoom: false);
    }

    public override void OnRemove(GameClient Session, RoomItem Item)
    {
        Item.ExtraData = "0\ufffd";
    }

    public override void OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
    {
        RoomUser roomUser = Item.GetRoom().GetRoomUserByHabbo(Session.GetHabbo().Id);
        if (roomUser != null)
        {
            if (Item.InteractingUser == 0)
            {
                Item.InteractingUser = roomUser.HabboId;
            }
            if (Item.GetRoom().TilesTouching(Item.X, Item.Y, roomUser.X, roomUser.Y))
            {
                Item.ReqUpdate(0);
            }
            else
            {
                roomUser.MoveTo(Item.SquareInFront);
            }
        }
    }
}
