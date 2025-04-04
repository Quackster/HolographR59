using Zero.Hotel.GameClients;
using Zero.Hotel.Rooms;

namespace Zero.Hotel.Items.Interactors;

internal class InteractorDice : FurniInteractor
{
    public override void OnPlace(GameClient Session, RoomItem Item)
    {
    }

    public override void OnRemove(GameClient Session, RoomItem Item)
    {
    }

    public override void OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
    {
        RoomUser User = Item.GetRoom().GetRoomUserByHabbo(Session.GetHabbo().Id);
        if (User == null)
        {
            return;
        }
        if (Item.GetRoom().TilesTouching(Item.X, Item.Y, User.X, User.Y))
        {
            if (Item.ExtraData != "-1")
            {
                if (Request == -1)
                {
                    Item.ExtraData = "0";
                    Item.UpdateState();
                }
                else
                {
                    Item.ExtraData = "-1";
                    Item.UpdateState(inDb: false, inRoom: true);
                    Item.ReqUpdate(4);
                }
            }
        }
        else
        {
            User.MoveTo(Item.SquareInFront);
        }
    }
}
