using Zero.Hotel.GameClients;
using Zero.Hotel.Pathfinding;
using Zero.Hotel.Rooms;

namespace Zero.Hotel.Items.Interactors;

internal class InteractorVendor : FurniInteractor
{
    public override void OnPlace(GameClient Session, RoomItem Item)
    {
        Item.ExtraData = "0";
        if (Item.InteractingUser != 0)
        {
            RoomUser User = Item.GetRoom().GetRoomUserByHabbo(Item.InteractingUser);
            if (User != null)
            {
                User.CanWalk = true;
            }
        }
    }

    public override void OnRemove(GameClient Session, RoomItem Item)
    {
        Item.ExtraData = "0";
        if (Item.InteractingUser != 0)
        {
            RoomUser User = Item.GetRoom().GetRoomUserByHabbo(Item.InteractingUser);
            if (User != null)
            {
                User.CanWalk = true;
            }
        }
    }

    public override void OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
    {
        if (!(Item.ExtraData != "1") || Item.GetBaseItem().VendingIds.Count < 1 || Item.InteractingUser != 0)
        {
            return;
        }
        RoomUser User = Item.GetRoom().GetRoomUserByHabbo(Session.GetHabbo().Id);
        if (User != null)
        {
            if (!Item.GetRoom().TilesTouching(User.X, User.Y, Item.X, Item.Y))
            {
                User.MoveTo(Item.SquareInFront);
                return;
            }
            Item.InteractingUser = Session.GetHabbo().Id;
            User.CanWalk = false;
            User.ClearMovement(Update: true);
            User.SetRot(Rotation.Calculate(User.X, User.Y, Item.X, Item.Y));
            Item.ReqUpdate(2);
            Item.ExtraData = "1";
            Item.UpdateState(inDb: false, inRoom: true);
        }
    }
}
